using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour 
{
    public static AudioManager Instance { get; private set; }
    private bool flagChecked;

    private void Awake() => MakeThisTheOnlyAudioManager();

    private void MakeThisTheOnlyAudioManager()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);
    }

    private void Update() 
    {
        if ((SceneManager.GetActiveScene().buildIndex >= 1) && !flagChecked)
        {
            flagChecked = true;
        }
    }
}
