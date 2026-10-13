using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour, IAudioManager, IAsyncInitializable
{
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float musicVolume = 1f;
    [SerializeField] private float SFXVolume = 1f;
    [SerializeField] private float UIVolume = 1f;

    private Bus masterBus;
    private Bus musicBus;
    private Bus SFXBus;
    private Bus UIBus;

    private List<EventInstance> eventInstances;
    private List<StudioEventEmitter> eventEmitters;

    public EventInstance ambienceEventInstance;
    public EventInstance musicEventInstance;

    private ISettingsManager settingsManager;

    private FMOD.System coreSystem;
    private int lastAppliedOutputDeviceIndex = -1;
    private int lastKnownDriverCount = -1;
    private Coroutine deviceWatchRoutine;

    private bool isLoaded;

    private void Awake()
    {
        eventInstances = new List<EventInstance>();
        eventEmitters = new List<StudioEventEmitter>();

        masterBus = RuntimeManager.GetBus("bus:/");
        SFXBus = RuntimeManager.GetBus("bus:/SFX");
        musicBus = RuntimeManager.GetBus("bus:/Music");
        UIBus = RuntimeManager.GetBus("bus:/UI");

        coreSystem = RuntimeManager.CoreSystem;

        isLoaded = true;
    }

    public IEnumerator InitializeAsync()
    {
        yield return new WaitUntil(() => isLoaded);
        settingsManager = ServiceLocator.Get<ISettingsManager>();
        if (settingsManager != null)
        {
            SubscribeVolume(SettingsKeys.MasterVolume);
            SubscribeVolume(SettingsKeys.MusicVolume);
            SubscribeVolume(SettingsKeys.SFXVolume);
            SubscribeVolume(SettingsKeys.UIVolume);

            var outputDeviceParam = settingsManager.GetParameter<IndexedParameter>(SettingsKeys.OutputDevice);
            if (outputDeviceParam != null)
                outputDeviceParam.OnChanged += ApplyOutputDeviceFromSettings;
        }
        deviceWatchRoutine = StartCoroutine(WatchDeviceListChanges());
    }

    private void SubscribeVolume(string key)
    {
        var p = settingsManager.GetParameter<SettingsParameter<float>>(key);
        if (p != null) p.OnChanged += ChangeAllVolumeValue;
    }

    private void OnDisable()
    {
        if (settingsManager != null)
        {
            settingsManager.OnParametersChanged -= ChangeAllVolumeValue;

            IndexedParameter outputDeviceParam = settingsManager.GetParameter<IndexedParameter>(SettingsKeys.OutputDevice);
            if (outputDeviceParam != null)
            {
                outputDeviceParam.OnChanged -= ApplyOutputDeviceFromSettings;
            }
        }

        if (deviceWatchRoutine != null)
        {
            StopCoroutine(deviceWatchRoutine);
            deviceWatchRoutine = null;
        }
    }

    private void Start()
    {
        ChangeAllVolumeValue();
        ApplyOutputDeviceFromSettings();
    }

    private void ChangeAllVolumeValue()
    {
        GetAllSavedVolumes();
        UpdateAllVolumeValue();
    }

    private void GetAllSavedVolumes()
    {
        if (settingsManager == null) return;
        masterVolume = settingsManager.GetParametersValue<float>(SettingsKeys.MasterVolume);
        musicVolume = settingsManager.GetParametersValue<float>(SettingsKeys.MusicVolume);
        SFXVolume = settingsManager.GetParametersValue<float>(SettingsKeys.SFXVolume);
        UIVolume = settingsManager.GetParametersValue<float>(SettingsKeys.UIVolume);
    }

    private void UpdateAllVolumeValue()
    {
        masterBus.setVolume(masterVolume);
        musicBus.setVolume(musicVolume);
        SFXBus.setVolume(SFXVolume);
        UIBus.setVolume(UIVolume);
    }

    private void ApplyOutputDeviceFromSettings()
    {
        if (settingsManager == null) return;

        var outputDeviceParam = settingsManager.GetParameter<IndexedParameter>(SettingsKeys.OutputDevice);

        if (outputDeviceParam == null) return;

        SetOutputDevice(outputDeviceParam.Index);
    }

    public bool SetOutputDevice(int deviceIndex)
    {
        FMOD.RESULT result = coreSystem.getNumDrivers(out int numDrivers);
        if (result != FMOD.RESULT.OK || numDrivers == 0)
        {
            Debug.LogWarning("[AudioManager] Failed to retrieve the list of audio devices, or no devices are present.");
            return false;
        }

        int safeIndex = Mathf.Clamp(deviceIndex, 0, numDrivers - 1);

        FMOD.RESULT setResult = coreSystem.setDriver(safeIndex);
        if (setResult != FMOD.RESULT.OK)
        {
            return false;
        }

        lastAppliedOutputDeviceIndex = safeIndex;
        Debug.Log($"[AudioManager] Divice switched to {safeIndex}.");
        return true;
    }

    public List<string> GetOutputDeviceNames()
    {
        List<string> devices = new List<string>();

        coreSystem.getNumDrivers(out int numDrivers);
        for (int i = 0; i < numDrivers; i++)
        {
            coreSystem.getDriverInfo(i, out string name, 256, out _, out _, out _, out _);
            devices.Add(name);
        }

        return devices;
    }

    private IEnumerator WatchDeviceListChanges()
    {
        WaitForSeconds wait = new WaitForSeconds(2f);

        coreSystem.getNumDrivers(out lastKnownDriverCount);

        while (true)
        {
            yield return wait;

            FMOD.RESULT result = coreSystem.getNumDrivers(out int currentDriverCount);
            if (result != FMOD.RESULT.OK) continue;

            if (currentDriverCount != lastKnownDriverCount)
            {
                lastKnownDriverCount = currentDriverCount;

                if (lastAppliedOutputDeviceIndex >= currentDriverCount)
                {
                    SetOutputDevice(0);
                }
                else
                {
                    ApplyOutputDeviceFromSettings();
                }
            }
        }
    }

    public void InitializeAmbience(EventReference ambienceEventReference)
    {
        ambienceEventInstance = CreateInstance(ambienceEventReference);
        ambienceEventInstance.start();
    }

    public void InitializeMusic(EventReference musicEventReference)
    {
        musicEventInstance = CreateInstance(musicEventReference);
        musicEventInstance.start();
    }

    public void SetAmbienceParameter(string parameterName, float parameterValue, bool ignoreSpeed = false)
    {
        ambienceEventInstance.setParameterByName(parameterName, parameterValue, ignoreSpeed);
    }

    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    public EventInstance CreateInstance(EventReference eventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        eventInstances.Add(eventInstance);
        return eventInstance;
    }

    public StudioEventEmitter InitializeEventEmitter(EventReference eventReference, GameObject emitterGameObject)
    {
        StudioEventEmitter emitter = emitterGameObject.GetComponent<StudioEventEmitter>();
        emitter.EventReference = eventReference;
        eventEmitters.Add(emitter);
        return emitter;
    }

    public void CleanUp()
    {
        foreach (EventInstance eventInstance in eventInstances)
        {
            eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            eventInstance.release();
        }

        foreach (StudioEventEmitter emitter in eventEmitters)
        {
            emitter.Stop();
        }
    }

    private void OnDestroy()
    {
        CleanUp();
    }
}