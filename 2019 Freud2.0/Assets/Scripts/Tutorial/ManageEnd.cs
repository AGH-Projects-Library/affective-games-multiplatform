using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;

public class ManageEnd : MonoBehaviour {
    [Header("UI")]

	public Text textCD;
	// Key mappings
	public KeyCode keyEscape = KeyCode.Escape;
	public KeyCode keySkipToScoreBoard = KeyCode.T;
    
	// Editor-wirable events
	public UnityEvent OnScoreBoardRequested = new UnityEvent();

	// Language/time display customization
	[Tooltip("Text label shown before the countdown (English)")]
	public string timeLabelENG = "Time to show results: ";
	[Tooltip("Text label shown before the countdown (Polish)")]
	public string timeLabelPL = "Czas za jaki pokażemy Ci wyniki: ";

	// Publicly editable timing and scene-wiring
	[SerializeField] private float timeToStart = 15f;
	[SerializeField] private string nameScoreBoard = "ScoreBoard";

	private int timeCD;
	private UserManager.Language currentLang = UserManager.Instance.language;

	// Backing for displaying countdown
	string currentTimeLabel;

	 void Awake()
	{
		// Init language/text labels
		currentLang = UserManager.Instance.language;
		currentTimeLabel = (currentLang == UserManager.Language.English) ? timeLabelENG : timeLabelPL;

		// Countdown
		timeCD = (int)timeToStart;
		StartCoroutine(LoseTime());

		LogManager.logManager.AddEvent(Time.time, "Load;Scene;ID" + SceneManager.GetActiveScene().buildIndex);
	}

    void Update()
    {
        if (CheckIfTimeToScoreBoard()) TriggerScoreBoard();
        if (CheckIfEscapePressed()) Application.Quit();
        if (CheckIfSkipToScoreBoard()) TriggerScoreBoard();
    }

    private bool CheckIfTimeToScoreBoard() => timeCD < 0;
    private bool CheckIfEscapePressed() => Input.GetKey(keyEscape);
    private bool CheckIfSkipToScoreBoard() => Input.GetKeyDown(keySkipToScoreBoard);

    public void ShowScoreBoard() 
    {   
        // Fire event for editor wiring
        OnScoreBoardRequested?.Invoke();
        if (currentLang != UserManager.Instance.language)
        {
            UpdateLanguage(UserManager.Instance.language);
        }
        // Fallback if no listeners wired: load directly
        if (OnScoreBoardRequested == null || OnScoreBoardRequested.GetPersistentEventCount() == 0)
        {
            SceneManager.LoadScene(nameScoreBoard);
        }
	}

    private void UpdateLanguage(UserManager.Language lang)
    {
        currentLang = lang;
        currentTimeLabel = (currentLang == UserManager.Language.English) ? timeLabelENG : timeLabelPL;
        textCD.text = currentTimeLabel + timeCD + "s";
    }

    IEnumerator LoseTime()
	{
		while(true)
		{
            string txt = currentTimeLabel.Replace("Time to show results:", "Time:") + timeCD + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;End;CountDown;Text;ChangeTo;" + txt);
            yield return new WaitForSeconds(1);
			textCD.text = txt;
			timeCD -= 1;
		}
	}
}
