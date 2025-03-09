using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEditor;

public class ManageEnd : MonoBehaviour {

	public Text textShowed;
    
    public Button nextButton;
    public Button previousButton;
	public Button scoreBoardButton;

    string txtNextButtonPl = "Następny";
    string txtPreviousButtonPl = "Poprzedni";
    string txtScoreBoardButtonPl = "Najlepsi gracze";
    string txtNextButtonEng = "Next";
    string txtPreviousButtonEng = "Previous";
    string txtScoreBoardButtonEng = "Best players";


	List<string> texts = new List<string> ();
    int index;

    string nameScoreBoard = "ScoreBoard";

    public void changeText (int x) 
    {
        float t = Time.time;
        if (x > 0)
        {
            LogManager.logManager.AddEvent(t, "ButtonClick;Next");
        }
        else if (x < 0)
        {
            LogManager.logManager.AddEvent(t, "ButtonClick;Previous");
        }

		index += x;
		textShowed.text = texts[index];
	}

    void Awake()
    {
        LogManager.logManager.AddEvent(Time.time, "LoadScene;" + SceneManager.GetActiveScene().buildIndex);

        if (UserManager.lang.Equals(UserManager.LanguageOption._English))
        {
            texts.Add("You win!\n");
            texts.Add("Your Nightmare has been defeated, and all of its followers fled!");
            texts.Add("It is time for your tranquil dream.\nAnd peaceful return of memory.");
            texts.Add("Let’s practice.\nWe will dictate our bank account numer to you now...");
            
            nextButton.GetComponentInChildren<Text>().text = txtNextButtonEng;
            previousButton.GetComponentInChildren<Text>().text = txtPreviousButtonEng;
            scoreBoardButton.GetComponentInChildren<Text>().text = txtScoreBoardButtonEng;
        }
        else if (UserManager.lang.Equals(UserManager.LanguageOption._Polish))
        {
            texts.Add("WYGRAŁEŚ!\n");
		    texts.Add("Twój koszmar został pokonany, a wszyscy jego popelcznicy ucielki w popłochu!");
		    texts.Add("Pora na spokojny sen.\nI spokojny powrót pamięci");
            texts.Add("Poćwiczmy.\nPodyktujemy Ci teraz numer naszego rachunku bankowego...");

            nextButton.GetComponentInChildren<Text>().text = txtNextButtonPl;
            previousButton.GetComponentInChildren<Text>().text = txtPreviousButtonPl;
            scoreBoardButton.GetComponentInChildren<Text>().text = txtScoreBoardButtonPl;
        }

        nextButton.gameObject.SetActive(false);
        previousButton.gameObject.SetActive(false);
		scoreBoardButton.gameObject.SetActive(false);

        index = 0;
        textShowed.text = texts[index];
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
		{
			LogManager.logManager.AddEvent(Time.time, "KeyPress;Esc");
			Application.Quit(); // ignored in UnityEditor
			EditorApplication.isPlaying = false;
		}

        if (index == 0)
        {
            nextButton.gameObject.SetActive(true);
            previousButton.gameObject.SetActive(false);
        }

        else if (index == (texts.Count - 2))
        {
            nextButton.gameObject.SetActive(true);
        }

        else if (index == (texts.Count - 1))
        {
            nextButton.gameObject.SetActive(false);
            scoreBoardButton.gameObject.SetActive(true); // if user gets to the end of end screen, remain visible
        }

        if (index >= 1)
        {
            previousButton.gameObject.SetActive(true);
        }

        // end screen skip:
        if (Input.GetKeyDown(KeyCode.T))
        {
            LogManager.logManager.AddEvent(Time.time, "KeyPress;T");
        	SceneManager.LoadScene(nameScoreBoard);
        }

        if (index == -1)
        {
            nextButton.gameObject.SetActive(false);
            previousButton.gameObject.SetActive(false);
            scoreBoardButton.gameObject.SetActive(false);
        }
    }

    public void ShowScoreBoard() 
    {   
        LogManager.logManager.AddEvent(Time.time, "ButtonClick;BestPlayers");
        SceneManager.LoadScene(nameScoreBoard);
	}
}