using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UserManager : MonoBehaviour 
{
	public static UserManager userManager;

    public enum LanguageOption
    {
		_English = 0,
		_Polish = 1
	};

    public static LanguageOption lang = LanguageOption._English;

    string userName; 
    string userNumber;

    string userPath = "Assets\\Datalogs\\" + DateTime.Now.ToString("yyyyMMdd") + "\\";

    int lvls = 0;
    int [] scoreLvl;

    void Awake () 
	{
        lvls = SceneManager.sceneCountInBuildSettings;
        scoreLvl = new int[lvls];
        Array.Clear(scoreLvl, 0, scoreLvl.Length);

        MakeThisTheOnlyUserManager();
    }
 
    void MakeThisTheOnlyUserManager()
    {

        if(userManager == null)
        {
            DontDestroyOnLoad(gameObject);
            userManager = this;
        }

        else
        {
            if(userManager != this)
            {
                Destroy (gameObject);
            }
        }
	}

    public void GainUserName(Text txt)
    {
        LogManager.logManager.AddEvent(Time.time, "InputEnter;Name;" + txt.text);

        userName = txt.text;
    }

    public void GainUserNumber(Text txt)
    {
        LogManager.logManager.AddEvent(Time.time, "InputEnter;Number;" + txt.text);

        userNumber = txt.text;
        userPath = userPath + userNumber + "\\";
        Directory.CreateDirectory(userPath);
    }

    public void ScoreZero()
    {
        Array.Clear(scoreLvl, 0, scoreLvl.Length);   
    }

    public void ScoreUpdate(int index, int score)
    {
        scoreLvl[index] = score;
    }

    public int GetCumulatedScore()
    {
        scoreLvl[2] = 0; // main tutorial should not affect score
        return scoreLvl.Sum();
    }

    public string GetUserName()
    {
        return userName;
    }

    public string GetUserNumber()
    {
        return userNumber;
    }

    public string GetUserPath()
    {
        return userPath;
    }

    public void ChangeLanguage(int x)
    {
        float t = Time.time;

        if (x == 0)
        {
            LogManager.logManager.AddEvent(t, "ButtonClick;English");
        }
        else if (x == 1)
        {
            LogManager.logManager.AddEvent(t, "ButtonClick;Polish");
        }

        lang = (LanguageOption)x;
    }
}