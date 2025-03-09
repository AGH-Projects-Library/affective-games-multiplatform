using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour 
{
	public static AudioManager audioManager;

	bool flagChecked = false;

     void Awake () 
	 {
         MakeThisTheOnlyAudioManager();
     }
 
     void MakeThisTheOnlyAudioManager()
	 {

         if(audioManager == null)
		 {
             DontDestroyOnLoad(gameObject);
             audioManager = this;
         }

         else
		 {
             if(audioManager != this)
			 {
                 Destroy (gameObject);
             }
         }
	}

	void Update () 
	{
		if ((SceneManager.GetActiveScene().buildIndex >= 1) && !flagChecked)
		{
			LogManager.logManager.AddEvent(Time.time, "BackgroundMusic;Start");
			gameObject.GetComponent<AudioSource>().enabled = true;
			flagChecked = true;
		}
	}
}
