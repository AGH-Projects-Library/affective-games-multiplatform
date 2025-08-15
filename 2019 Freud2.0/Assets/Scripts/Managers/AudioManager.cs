using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour 
{
    public static AudioManager audioManager;
    private bool flagChecked;

    private void Awake() => MakeThisTheOnlyAudioManager();

    private void MakeThisTheOnlyAudioManager()
    {
        if (audioManager == null)
        {
            DontDestroyOnLoad(gameObject);
            audioManager = this;
        }
        else if (audioManager != this)
        {
            Destroy (gameObject);
        }
    }

    private void Update() 
    {
        if ((SceneManager.GetActiveScene().buildIndex >= 1) && !flagChecked)
        {
            flagChecked = true;
        }
    }
}
