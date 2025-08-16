using System;
using UnityEngine;

public abstract class iSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance => _instance;

    public void Awake()
    {
        InitInstance();
    }

    protected void InitInstance()
    {
        if (_instance == null)
            _instance = this as T;
        else
        {
            Debug.LogWarning($"{this.GetType().Name} instance already exists. Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    
    protected static bool InstanceExists()
    {
        if (_instance) return true;
        string thisClassName = typeof(T).Name;
        Debug.LogError($"{thisClassName} instance is not initialized.");
        return false;
    }
}
