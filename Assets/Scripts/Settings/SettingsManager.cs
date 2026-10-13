using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsManager : MonoBehaviour, ISettingsManager, IAsyncInitializable
{
    private static readonly string[] LanguageOptions = { "English", "Russian", "Spain" };
    private static readonly string[] ScreenModeOptions = { "Fullscreen", "Windowed", "Borderless" };
    private static readonly string[] QualityOptions = { "Low", "Medium", "High" };
    private static readonly string[] VSyncOptions = { "Disable", "Enable" };
    private static readonly int[] FpsSteps = { 30, 60, 90, 120, 144, 180, 240, 360, 480 };

    private const string PrefsPrefix = "PostOffice.Settings.";
    private const string KeyHasSavedData = PrefsPrefix + "HasSavedData";

    public IndexedParameter Language { get; private set; }
    public SettingsParameter<float> Sensitivity { get; private set; }
    public SettingsParameter<float> MasterVolume { get; private set; }
    public SettingsParameter<float> MusicVolume { get; private set; }
    public SettingsParameter<float> SFXVolume { get; private set; }
    public SettingsParameter<float> UIVolume { get; private set; }
    public IndexedParameter ScreenResolution { get; private set; }
    public IndexedParameter ScreenMode { get; private set; }
    public IndexedParameter FpsLimit { get; private set; }
    public IndexedParameter VSync { get; private set; }
    public IndexedParameter TextureQuality { get; private set; }
    public IndexedParameter ShadowQuality { get; private set; }
    public IndexedParameter OutputDevice { get; private set; }

    public List<Resolution> FilteredResolutions { get; } = new List<Resolution>();
    public List<string> OutputDevices { get; } = new List<string>();

    public event Action OnParametersChanged;

    private readonly Dictionary<string, ISettingsParameter> parameters =
        new Dictionary<string, ISettingsParameter>(StringComparer.OrdinalIgnoreCase);

    private FMOD.System coreSystem;
    private bool isLoaded;

    private void Awake()
    {
        coreSystem = FMODUnity.RuntimeManager.CoreSystem;

        PopulateResolutionList();
        PopulateOutputDeviceList();

        RegisterParameters();
        LoadAll();
        ApplyRuntimeOnlySettings();

        isLoaded = true;
    }

    public IEnumerator InitializeAsync()
    {
        yield return new WaitUntil(() => isLoaded);
    }

    private void RegisterParameters()
    {
        string[] resolutionLabels = BuildResolutionLabels();
        int defaultResolutionIndex = Mathf.Max(0, Array.FindIndex(resolutionLabels, r => r == "1920x1080"));
        string[] outputDeviceLabels = OutputDevices.Count > 0 ? OutputDevices.ToArray() : new[] { "Default" };
        string[] fpsLabels = Array.ConvertAll(FpsSteps, fps => fps + "FPS");

        Register(Language = new IndexedParameter(SettingsKeys.Language, LanguageOptions));
        Register(Sensitivity = new SettingsParameter<float>(SettingsKeys.Sensitivity, 0.1f, v => Mathf.Clamp(v, 0.05f, 1f)));
        Register(MasterVolume = new SettingsParameter<float>(SettingsKeys.MasterVolume, 1f, v => Mathf.Clamp(v, 0f, 2f),v => AudioListener.volume = v));
        Register(MusicVolume = new SettingsParameter<float>(SettingsKeys.MusicVolume, 1f, v => Mathf.Clamp(v, 0f, 2f)));
        Register(SFXVolume = new SettingsParameter<float>(SettingsKeys.SFXVolume, 1f, v => Mathf.Clamp(v, 0f, 2f)));
        Register(UIVolume = new SettingsParameter<float>(SettingsKeys.UIVolume, 1f, v => Mathf.Clamp(v, 0f, 2f)));
        Register(OutputDevice = new IndexedParameter(SettingsKeys.OutputDevice, outputDeviceLabels, 0, ApplyOutputDevice));
        Register(ScreenMode = new IndexedParameter(SettingsKeys.FullscreenMode, ScreenModeOptions, 1, _ => ApplyScreenSettings()));
        Register(ScreenResolution = new IndexedParameter(SettingsKeys.ScreenResolution, resolutionLabels, defaultResolutionIndex, _ => ApplyScreenSettings()));
        Register(FpsLimit = new IndexedParameter(SettingsKeys.FPSLock, fpsLabels, GetClosestFpsIndex(120), i => Application.targetFrameRate = FpsSteps[i]));
        Register(VSync = new IndexedParameter(SettingsKeys.VSync, VSyncOptions, 0, ApplyVSync));
        Register(TextureQuality = new IndexedParameter(SettingsKeys.TextureQuality, QualityOptions, 2, ApplyTextureQuality));
        Register(ShadowQuality = new IndexedParameter(SettingsKeys.ShadowQuality, QualityOptions, 2, ApplyShadowQuality));
    }

    private void Register(ISettingsParameter parameter)
    {
        parameters[parameter.Name] = parameter;
        parameter.OnChanged += NotifyChanged;
    }

    private void NotifyChanged() => OnParametersChanged?.Invoke();

    private void LoadAll()
    {
        bool hasSavedData = PlayerPrefs.GetInt(KeyHasSavedData, 0) == 1;
        foreach (ISettingsParameter parameter in parameters.Values)
        {
            parameter.LoadFromPrefs(PrefsPrefix, hasSavedData);
        }
    }

    public void Save()
    {
        foreach (ISettingsParameter parameter in parameters.Values)
        {
            parameter.SaveToPrefs(PrefsPrefix);
        }

        PlayerPrefs.SetInt(KeyHasSavedData, 1);
        PlayerPrefs.Save();
    }

    public void ResetToDefaults()
    {
        foreach (ISettingsParameter parameter in parameters.Values)
        {
            parameter.Reset(silent: true);
        }

        ApplyRuntimeOnlySettings();

        foreach (ISettingsParameter parameter in parameters.Values)
        {
            parameter.ForceNotify();
        }

        OnParametersChanged?.Invoke();
    }

    public T GetParameter<T>(string name) where T : class
    {
        return parameters.TryGetValue(name, out ISettingsParameter parameter) ? parameter as T : null;
    }

    public T GetParametersValue<T>(string name)
    {
        if (!parameters.TryGetValue(name, out ISettingsParameter parameter)) return default;

        object boxed = parameter.BoxedValue;
        if (boxed is T value) return value;

        try
        {
            return (T)Convert.ChangeType(boxed, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    private void ApplyRuntimeOnlySettings()
    {
        AudioListener.volume = MasterVolume.Value;
        Application.targetFrameRate = FpsSteps[FpsLimit.Index];
        ApplyVSync(VSync.Index);
        ApplyTextureQuality(TextureQuality.Index);
        ApplyShadowQuality(ShadowQuality.Index);
        ApplyScreenSettings();
        ApplyOutputDevice(OutputDevice.Index);
    }

    private void ApplyScreenSettings()
    {
        if (FilteredResolutions.Count == 0) return;

        string[] parts = ScreenResolution.Current.Split('x');
        if (parts.Length != 2) return;
        if (!int.TryParse(parts[0], out int width) || !int.TryParse(parts[1], out int height)) return;

        FullScreenMode mode = ScreenMode.Index switch
        {
            1 => FullScreenMode.Windowed,
            2 => FullScreenMode.FullScreenWindow,
            _ => FullScreenMode.ExclusiveFullScreen
        };

        Screen.SetResolution(width, height, mode);
    }

    private void ApplyVSync(int index) => QualitySettings.vSyncCount = index;

    private void ApplyTextureQuality(int index) => QualitySettings.globalTextureMipmapLimit = QualityOptions.Length - 1 - index;

    private void ApplyShadowQuality(int index)
    {
        QualitySettings.shadowResolution = index switch
        {
            0 => ShadowResolution.Low,
            1 => ShadowResolution.Medium,
            _ => ShadowResolution.High
        };

        QualitySettings.shadows = index == 0 ? UnityEngine.ShadowQuality.HardOnly : UnityEngine.ShadowQuality.All;
    }

    private void ApplyOutputDevice(int index)
    {
        if (OutputDevices.Count == 0) return;
        coreSystem.setDriver(index);
    }

    private void PopulateResolutionList()
    {
        FilteredResolutions.Clear();
        Resolution[] all = Screen.resolutions;
        HashSet<string> seen = new HashSet<string>();

        for (int i = all.Length - 1; i >= 0; i--)
        {
            string key = all[i].width + "x" + all[i].height;
            if (seen.Add(key)) FilteredResolutions.Add(all[i]);
        }
    }

    private void PopulateOutputDeviceList()
    {
        OutputDevices.Clear();

        FMOD.RESULT result = coreSystem.getNumDrivers(out int numDrivers);
        if (result != FMOD.RESULT.OK || numDrivers == 0) return;

        for (int i = 0; i < numDrivers; i++)
        {
            FMOD.RESULT infoResult = coreSystem.getDriverInfo(i, out string name, 256, out _, out _, out _, out _);
            if (infoResult == FMOD.RESULT.OK) OutputDevices.Add(name);
        }
    }

    private string[] BuildResolutionLabels()
    {
        string[] labels = new string[FilteredResolutions.Count];
        for (int i = 0; i < FilteredResolutions.Count; i++)
        {
            Resolution r = FilteredResolutions[i];
            labels[i] = r.width + "x" + r.height;
        }
        return labels;
    }

    private int GetClosestFpsIndex(int target)
    {
        int closest = 0;
        int minDiff = int.MaxValue;

        for (int i = 0; i < FpsSteps.Length; i++)
        {
            int diff = Mathf.Abs(FpsSteps[i] - target);
            if (diff >= minDiff) continue;
            minDiff = diff;
            closest = i;
        }

        return closest;
    }
}
