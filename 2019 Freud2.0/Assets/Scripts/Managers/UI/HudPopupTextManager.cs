using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HudPopupTextManager : MonoBehaviour
{
    private static HudPopupTextManager Instance;
    public GameObject prefab_alertDisplayer;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    bool InstanceCheck()
    {
        string thisClassName = this.GetType().Name;
        if (Instance == null) { Debug.LogError($"{thisClassName} instance is not initialized."); return false; }
        if (prefab_alertDisplayer == null) { Debug.LogError($"{thisClassName} prefab_alertDisplayer is not assigned."); return false; }
        return true;
    }
    private void Start()
    {
        throw new NotImplementedException();
    }

    public static void ShowAlert(string message, float duration)
    {
        if (!Instance.InstanceCheck()) return;
        GameObject alertDisplayer = Instantiate(Instance.prefab_alertDisplayer);
        alertDisplayer.GetComponent<HudPopupText_AnimatorAndDestuctor>().DisplayAlert(message, duration);
        DontDestroyOnLoad(alertDisplayer);
    }
    
}
