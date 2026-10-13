using System;
using FMODUnity;
using System.Collections;
using UnityEngine;
using static InputState;

public interface IGameService { }

public interface IAsyncInitializable
{
    public IEnumerator InitializeAsync();
}

public interface IAudioManager : IGameService
{
    public void InitializeAmbience(EventReference ambienceEventReference);
    public void InitializeMusic(EventReference musicEventReference);
    public void SetAmbienceParameter(string parameterName, float parameterValue, bool ignoreSpeed = false);
    public void PlayOneShot(EventReference sound, Vector3 worldPos);
}

public interface ISettingsManager : IGameService
{
    public event Action OnParametersChanged;
    public T GetParameter<T>(string name) where T : class;
    public T GetParametersValue<T>(string name);
    public void ResetToDefaults();
    public void Save();
}

public interface IInputManager : IGameService
{
    public T GetInput<T>(string inputName);
}

public interface IInputState : IGameService
{
    public bool GetLockState(LockType type);
    public void AddLock(LockType type, string key);
    public void RemoveLock(LockType type, string key);
}