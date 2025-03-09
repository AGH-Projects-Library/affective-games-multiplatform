using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEditor;

public class ManageEnd : MonoBehaviour {

	public Text textShowed;

    public Text textCD;
    

	List<string> texts = new List<string> ();
    int index;

    string nameScoreBoard = "ScoreBoard";

    int timeCD;

    public float timeToStart = 15;

    string txtTime = "";
    string txtTimePL = "Czas za jaki pokażemy Ci wyniki: ";

    string txtTimeENG = "Time to show results: ";

    void Awake()
    {
        timeCD = (int)timeToStart;
	    StartCoroutine("LoseTime");

        LogManager.logManager.AddEvent(Time.time, "Load;Scene;ID" + SceneManager.GetActiveScene().buildIndex);

        if (UserManager.lang.Equals(UserManager.LanguageOption._English))
        {
            txtTime = txtTimeENG;
            texts.Add("You win!\n\nYour Nightmare has been defeated, and all of its followers fled!\nIt is time for your tranquil dream.\nAnd peaceful return of memory.\n\nLet’s practice.\nWe will dictate our bank account numer to you now...");
        }
        else if (UserManager.lang.Equals(UserManager.LanguageOption._Polish))
        {
            txtTime = txtTimePL;
            texts.Add("WYGRAŁEŚ!\n\nTwój koszmar został pokonany\na wszyscy jego popelcznicy ucielki w popłochu!\nPora na spokojny sen.\nI spokojny powrót pamięci.\n\nPoćwiczmy.\nPodyktujemy Ci teraz numer naszego rachunku bankowego...");
        }

        index = 0;
        textShowed.text = texts[index];
    }

    void Update()
    {
        if (timeCD < 0)
        {
            SceneManager.LoadScene(nameScoreBoard);
        }

        if (Input.GetKey(KeyCode.Escape))
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Esc");
			Application.Quit(); // ignored in UnityEditor
			// EditorApplication.isPlaying = false;
		}

        // end screen skip:
        if (Input.GetKeyDown(KeyCode.T))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;T");
        	SceneManager.LoadScene(nameScoreBoard);
        }

    }

    public void ShowScoreBoard() 
    {   
        // LogManager.logManager.AddEvent(Time.time, "ButtonClick;BestPlayers");
        SceneManager.LoadScene(nameScoreBoard);
	}

    IEnumerator LoseTime()
	{
		while(true)
		{
            string txt = txtTime + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;End;CountDown;Text;ChangeTo;" + txt);
            yield return new WaitForSeconds(1);
			textCD.text = txt;
			timeCD -= 1;
		}
	}
}