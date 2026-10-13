using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputState : MonoBehaviour, IInputState, IAsyncInitializable
{
    [Serializable]
    private class LockEntry
    {
        public LockType Type;
        public bool IsLocked;
        public List<string> Keys = new();
    }

    [SerializeField] private List<LockEntry> _lockEntries = new();

    private readonly Dictionary<LockType, HashSet<string>> _locks = new();
    public enum LockType { Move, Camera, Cursor, Menu }

    private bool isLoaded;

    private void Awake()
    {
        isLoaded = true;
    }

    public IEnumerator InitializeAsync()
    {
        yield return new WaitUntil(() => isLoaded);
    }

    public bool GetLockState(LockType type)
    {
        return _locks.TryGetValue(type, out var set) && set.Count > 0;
    }

    public void AddLock(LockType type, string key)
    {
        if (!_locks.TryGetValue(type, out var set))
        {
            set = new HashSet<string>();
            _locks[type] = set;
        }

        set.Add(key);
        SyncEntries();
    }

    public void RemoveLock(LockType type, string key)
    {
        if (_locks.TryGetValue(type, out var set))
            set.Remove(key);

        SyncEntries();
    }

    private void SyncEntries()
    {
        _lockEntries.Clear();

        foreach (LockType type in Enum.GetValues(typeof(LockType)))
        {
            var entry = new LockEntry { Type = type };

            if (_locks.TryGetValue(type, out var set))
            {
                entry.IsLocked = set.Count > 0;
                entry.Keys.AddRange(set);
            }
            else
            {
                entry.IsLocked = false;
            }

            _lockEntries.Add(entry);
        }
    }
}
