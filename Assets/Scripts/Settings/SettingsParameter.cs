using System;
using UnityEngine;

public interface ISettingsParameter
{
    string Name { get; }
    object BoxedValue { get; }
    event Action OnChanged;

    void LoadFromPrefs(string prefsPrefix, bool hasSavedData);
    void SaveToPrefs(string prefsPrefix);
    void Reset(bool silent);
    void ForceNotify();
}

public class SettingsParameter<T> : ISettingsParameter
{
    public string Name { get; }
    public T Value { get; private set; }
    public T DefaultValue { get; }
    public object BoxedValue => Value;

    public event Action OnChanged;

    private readonly Func<T, T> clamp;
    private readonly Action<T> onApply;

    public SettingsParameter(string name, T defaultValue, Func<T, T> clamp = null, Action<T> onApply = null)
    {
        Name = name;
        DefaultValue = defaultValue;
        this.clamp = clamp;
        this.onApply = onApply;
        Value = defaultValue;
    }

    public void Set(T value, bool silent = false)
    {
        Value = clamp != null ? clamp(value) : value;
        onApply?.Invoke(Value);
        if (!silent) OnChanged?.Invoke();
    }

    public void Reset(bool silent = false) => Set(DefaultValue, silent);

    public void ForceNotify() => OnChanged?.Invoke();

    public void LoadFromPrefs(string prefsPrefix, bool hasSavedData)
    {
        string key = prefsPrefix + Name;
        T loaded = DefaultValue;

        if (hasSavedData)
        {
            switch (DefaultValue)
            {
                case float defaultFloat:
                    loaded = (T)(object)PlayerPrefs.GetFloat(key, defaultFloat);
                    break;
                case int defaultInt:
                    loaded = (T)(object)PlayerPrefs.GetInt(key, defaultInt);
                    break;
                case string defaultString:
                    loaded = (T)(object)PlayerPrefs.GetString(key, defaultString);
                    break;
            }
        }

        Set(loaded, silent: true);
    }

    public void SaveToPrefs(string prefsPrefix)
    {
        string key = prefsPrefix + Name;

        switch (Value)
        {
            case float floatValue:
                PlayerPrefs.SetFloat(key, floatValue);
                break;
            case int intValue:
                PlayerPrefs.SetInt(key, intValue);
                break;
            case string stringValue:
                PlayerPrefs.SetString(key, stringValue);
                break;
        }
    }
}

public class IndexedParameter : ISettingsParameter
{
    public string Name { get; }
    public string[] Options { get; }
    public int Index { get; private set; }
    public int DefaultIndex { get; }
    public int Count => Options.Length;
    public string Current => Options[Index];
    public object BoxedValue => Current;

    public event Action OnChanged;

    private readonly Action<int> onApply;

    public IndexedParameter(string name, string[] options, int defaultIndex = 0, Action<int> onApply = null)
    {
        Name = name;
        Options = options;
        this.onApply = onApply;
        DefaultIndex = Wrap(defaultIndex);
        Index = DefaultIndex;
    }

    public void Set(int index, bool silent = false)
    {
        Index = Wrap(index);
        onApply?.Invoke(Index);
        if (!silent) OnChanged?.Invoke();
    }

    public void Step(int direction, bool silent = false) => Set(Index + direction, silent);

    public void Reset(bool silent = false) => Set(DefaultIndex, silent);

    public void ForceNotify() => OnChanged?.Invoke();

    public void LoadFromPrefs(string prefsPrefix, bool hasSavedData)
    {
        string key = prefsPrefix + Name;
        int loaded = hasSavedData ? PlayerPrefs.GetInt(key, DefaultIndex) : DefaultIndex;
        Set(loaded, silent: true);
    }

    public void SaveToPrefs(string prefsPrefix) => PlayerPrefs.SetInt(prefsPrefix + Name, Index);

    private int Wrap(int index)
    {
        if (Options.Length == 0) return 0;
        return ((index % Options.Length) + Options.Length) % Options.Length;
    }
}
