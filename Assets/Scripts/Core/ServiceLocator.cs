using System;
using System.Collections.Generic;
using UnityEngine;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services
        = new Dictionary<Type, object>();

    public static void Register<T>(T service) where T : class
    {
        var type = typeof(T);
        if (_services.ContainsKey(type))
        {
            _services[type] = service;
            return;
        }
        _services.Add(type, service);
    }

    public static T Get<T>() where T : class
    {
        var type = typeof(T);
        if (_services.TryGetValue(type, out var service))
            return (T)service;

        throw new InvalidOperationException();
    }

    public static bool TryGet<T>(out T service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out var raw))
        {
            service = (T)raw;
            return true;
        }
        service = null;
        return false;
    }

    public static void Unregister<T>() where T : class
    {
        var type = typeof(T);
        if (_services.Remove(type))
            Debug.Log($"[ServiceLocator] Remove: {type.Name}");
    }

    public static bool IsRegistered<T>() where T : class
        => _services.ContainsKey(typeof(T));

    public static void Clear()
    {
        _services.Clear();
    }
}