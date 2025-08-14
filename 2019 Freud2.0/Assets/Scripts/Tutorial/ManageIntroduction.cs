using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ManageIntroduction : MonoBehaviour {

	// UI
	public Text textShowed;

	// Public UI hooks for editor wiring
	public UnityEvent OnIntroductionFinished;
	public UnityEvent OnLanguageChanged;
	// Configurable input mappings (inspector-friendly)
	public string fireButton2 = "Fire2"; // mapped to English welcome progression
	public string fireButton3 = "Fire3"; // mapped to Polish progression
	public KeyCode keyS = KeyCode.S;     // quick-load level 0 shortcut

	// Language-specific welcome text
	[Tooltip("Welcome text for English")]
	public string welcomeTextEng = "Welcome in the Freud2.0!";
	[Tooltip("Welcome text for Polish")]
	public string welcomeTextPl = "Witaj w Freud2.0!";

	// Language-aware strings
	[Tooltip("Time label when the game will start (English)")]
	public string timeLabelENG = "Time to level begin: ";
	[Tooltip("Time label when the game will start (Polish)")]
	public string timeLabelPL = "Czas do rozpoczęcia poziomu: ";

	// Timing controls
	public float timeToStart = 30;
	public float timeWait = 1f;
	int timeCD;
	string txtTime = "";
	// language tracking
	UserManager.LanguageOption lang;
 
	[SerializeField] public LanguageOption currentLang;
	[System.Serializable]
	public enum LanguageOption { English, Polish }
	// Expose a couple of helper for editor wiring
	public UnityEvent OnCalibrationSkip;

	[SerializeField] public string txtTimePL;
	[SerializeField] public string txtTimeENG;

	[Tooltip("Welcome text shown on startup")]
	public string welcomeTextLabelENG = "Welcome in the Freud2.0!";
	[Tooltip("Welcome text shown on startup (Polish)")]
	public string welcomeTextLabelPL = "Witaj w Freud2.0!";

	// A small reference to a language manager if you have one
	// (we'll keep existing behavior and just expose hooks)

	public float timeCalibrationCheck = 0.5f;

	void Awake()
    {
        timeCD = (int)timeToStart;
	    StartCoroutine(LoseTime());

        // Log start load
        LogManager.logManager.AddEvent(Time.time, "Scene;Load;" + SceneManager.GetActiveScene().buildIndex);

        // initial language
        lang = UserManager.lang;
        ChangeLanguage();
    }

    void Update() 
    {
        if(Input.GetButtonDown (fireButton2)) // use public mapping for Fire2
        {
            LogManager.logManager.AddEvent(Time.time, "Key;X");
            lang = UserManager.LanguageOption._English;
            UserManager.lang = lang;
            ChangeLanguage();
            OnLanguageChanged?.Invoke();
        }

        if(Input.GetButtonDown (fireButton3)) // use public mapping for Fire3
        {
            LogManager.logManager.AddEvent(Time.time, "Key;O");
            lang = UserManager.LanguageOption._Polish;
            UserManager.lang = lang;
            ChangeLanguage();
            OnLanguageChanged?.Invoke();
        }

        if (Input.GetKeyDown(keyS)) // use public key binding for S
		{
            LogManager.logManager.AddEvent(Time.time, "Key;S");
			LoadLvl0();
		}

        // Auto-load into lvl 0 after countdown
        if (CheckIfTimeToStartElapsed()) LoadLvl0();

        if (Input.GetKeyDown(KeyCode.Escape))
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
			// EditorApplication.isPlaying = false;
		}

        if (lang != UserManager.lang)
        {
            lang = UserManager.lang;
            ChangeLanguage();
            OnLanguageChanged?.Invoke();
        }
    }

    void ChangeLanguage()
    {
        switch(lang)
        {
            case UserManager.LanguageOption._English:
            {
                textShowed.text = welcomeTextLabelENG;
                currentLang = UserManager.LanguageOption._English;
                currentLang = UserManager.LanguageOption._English;
                currentTimeLabel = timeLabelENG;
                txtTimeENG = timeLabelENG;
                // time label for countdown
                // If you want to update any other UI texts, set them here
                break;
            }

            case UserManager.LanguageOption._Polish:
            {
                textShowed.text = welcomeTextLabelPL;
                currentLang = UserManager.LanguageOption._Polish;
                currentTimeLabel = timeLabelPL;
                txtTimePL = timeLabelPL;
                break;
            }
        }
    }

	// Call to advance to the gameplay after intro
	public void LoadTutorial ()
    {
        TurnOffButtons();
        StartCoroutine(Wait());
        OnIntroductionFinished?.Invoke();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 3);
	}

	public void LoadLvl0 ()
    {
        TurnOffButtons();
        StartCoroutine(Wait());
        OnIntroductionFinished?.Invoke();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
	}

	// Helpers
    bool CheckIfTimeToStartElapsed() { return timeToStart < Time.timeSinceLevelLoad; }
    void TurnOffButtons () { /* keep; minimal cleanup placeholder for now */ }
    IEnumerator Wait() { yield return new WaitForSeconds(timeWait); }

	// Coroutine for countdown display
	IEnumerator LoseTime()
	{
		while(true)
		{
            string toShow = txtTime + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;Introduction;CountDown;Text;ChangeTo;" + toShow);
			textShowed.text = toShow;
			timeCD -= 1;
			yield return new WaitForSeconds(1);
		}
	}
}
