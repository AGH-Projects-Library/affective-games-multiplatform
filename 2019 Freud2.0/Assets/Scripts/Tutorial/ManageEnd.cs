using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ManageEnd : MonoBehaviour {

	// UI
	public Text textShowed;

	public Text textCD;
	// Key mappings
	public KeyCode keyEscape = KeyCode.Escape;
	public KeyCode keySkipToScoreBoard = KeyCode.T;
    
	// Editor-wirable events
	public UnityEvent OnScoreBoardRequested;

	// Language/time display customization
	[Tooltip("Text label shown before the countdown (English)")]
	public string timeLabelENG = "Time to show results: ";
	[Tooltip("Text label shown before the countdown (Polish)")]
	public string timeLabelPL = "Czas za jaki pokażemy Ci wyniki: ";

	// Publicly editable timing and scene-wiring
	public float timeToStart = 15f;
	int timeCD;
	int index;
	string nameScoreBoard = "ScoreBoard";

	// For internal language switching
	UserManager.LanguageOption currentLang;

	// Optional: if you want to prefill scoreboard text lists in inspector later
	// List<string> texts = new List<string> ();

	// Backing for displaying countdown
	string currentTimeLabel;

	// Internal helper signals
	string txtTimeENG;
	string txtTimePL;

	// Guard: allow editor to wire behavior if nothing wired
	// rest of fields preserved for backward compatibility

	// End of fields

	 void Awake()
	{
		// Init language/text labels
		currentLang = UserManager.lang;
		currentTimeLabel = (currentLang == UserManager.LanguageOption._English) ? timeLabelENG : timeLabelPL;
		txtTimeENG = timeLabelENG;
		txtTimePL = timeLabelPL;

		// Countdown
		timeCD = (int)timeToStart;
		StartCoroutine(LoseTime());

		// Simple log of scene load
		LogManager.logManager.AddEvent(Time.time, "Load;Scene;ID" + SceneManager.GetActiveScene().buildIndex);

		// Initialize display to the chosen language
		// (keep existing text setup for end screen)
		// If needed, you can populate texts here in the future
		index = 0;
		// Texts[] could be filled via inspector if desired
		// textShowed.text = texts[index];

	}

    void Update()
    {
        // Time to show scoreboard?
        if (CheckIfTimeToScoreBoard()) TriggerScoreBoard();

        // Escape to quit
        if (CheckIfEscapePressed())
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Esc");
			Application.Quit(); // ignored in UnityEditor
		}

        // end screen skip:
        if (CheckIfSkipToScoreBoard())
        {
            LogManager.logManager.AddEvent(Time.time, "Key;T");
        	TriggerScoreBoard();
        }

        // Language change in editor or at runtime
        if (currentLang != UserManager.lang)
        {
            currentLang = UserManager.lang;
            currentTimeLabel = (currentLang == UserManager.LanguageOption._English) ? timeLabelENG : timeLabelPL;
            // Update any on-screen label immediately
            textCD.text = currentTimeLabel + timeCD + "s";
        }

    }

    public void ShowScoreBoard() 
    {   
        // Fire event for editor wiring
        OnScoreBoardRequested?.Invoke();
        // Fallback if no listeners wired: load directly
        if (OnScoreBoardRequested == null || OnScoreBoardRequested.GetPersistentEventCount() == 0)
        {
            SceneManager.LoadScene(nameScoreBoard);
        }
	}

    IEnumerator LoseTime()
	{
		while(true)
		{
            string txt = currentTimeLabel.Replace("Time to show results:", "Time:") + timeCD.ToString() + "s";
            // If you want to log more precisely, adapt the message format here
            LogManager.logManager.AddEvent(Time.time, "Game;End;CountDown;Text;ChangeTo;" + txt);
            yield return new WaitForSeconds(1);
			textCD.text = txt;
			timeCD -= 1;
		}
	}

    // Checks
    bool CheckIfTimeToScoreBoard() { return timeCD < 0; }
    bool CheckIfEscapePressed() { return Input.GetKey(keyEscape); }
    bool CheckIfSkipToScoreBoard() { return Input.GetKeyDown(keySkipToScoreBoard); }

    void TriggerScoreBoard()
    {
        OnScoreBoardRequested?.Invoke();
        if (OnScoreBoardRequested == null || OnScoreBoardRequested.GetPersistentEventCount() == 0)
        {
            SceneManager.LoadScene(nameScoreBoard);
        }
    }
}
