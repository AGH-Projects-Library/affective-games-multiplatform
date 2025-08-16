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

    public static void ShowAlert(string message, float duration)
    {
        GameObject alertDisplayer = Instantiate(Instance.prefab_alertDisplayer);
        alertDisplayer.GetComponent<HudPopupText_AnimatorAndDestuctor>().DisplayAlert(message, duration);
        DontDestroyOnLoad(alertDisplayer);
    }
}
