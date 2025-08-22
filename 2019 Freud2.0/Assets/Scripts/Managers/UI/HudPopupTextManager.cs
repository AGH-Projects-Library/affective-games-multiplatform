using System;
using System.Collections;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.UI;

public class HudPopupTextManager : SingletonBase<HudPopupTextManager>
{
    public GameObject prefab_alertDisplayer;
    public RectTransform parentCanvas;
    public static void ShowAlert(string message, float duration)
    {
        if (!InstanceExists()) return; 
        GameObject alertDisplayer = Instantiate(Instance.prefab_alertDisplayer, Instance.parentCanvas);
        alertDisplayer.GetComponent<HudPopupText_AnimatorAndDestuctor>().DisplayAlert(message, duration);
        DontDestroyOnLoad(alertDisplayer);
    }
    
}
