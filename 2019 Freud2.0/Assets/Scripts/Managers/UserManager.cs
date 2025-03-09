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

    public static LanguageOption lang = LanguageOption._Polish;

    string userName; 
    string userID;

    string persDataPath;
    string userPath;

    int lvls = 0;
    int [] scoreLvl;

    bool used;

    void Start () 
	{
        persDataPath = Application.persistentDataPath;
        userPath = persDataPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\";

        Cursor.visible = false;
		Screen.fullScreen = true;

        userID = "0000";
        userName = "Zawodnik0000";

        if (File.Exists(persDataPath + "\\currentNumber.txt")) 
        {
            StreamReader readtext = new StreamReader(persDataPath + "\\currentNumber.txt");
            userID = readtext.ReadLine();
            userName = "Zawodnik" + userID;
            readtext.Close();
        }

		LogManager.logManager.AddEvent(Time.time, "UserName;" + userName);
		LogManager.logManager.AddEvent(Time.time, "UserID;" + userID);

        userPath += userID + "\\";
        Directory.CreateDirectory(userPath);


        lvls = SceneManager.sceneCountInBuildSettings;
        scoreLvl = new int[lvls];
        Array.Clear(scoreLvl, 0, scoreLvl.Length);

        used = false;

        MakeThisTheOnlyUserManager();
    }

    void Update()
    {
        if (Time.time > 1200 && !used)
        {   
            used = true;
            ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
            LogManager.logManager.AddEvent(Time.time, "Score;GameEnd;Level;" + SceneManager.GetActiveScene().buildIndex + ";Value;" + ScoreManager.score);
            SceneManager.LoadScene("ScoreBoard");
        }
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

    // public void GainUserName(Text txt)
    // {
    //     LogManager.logManager.AddEvent(Time.time, "InputEnter;Name;" + txt.text);

    //     userName = txt.text;
    // }

    // public void GainUserNumber(Text txt)
    // {
    //     LogManager.logManager.AddEvent(Time.time, "InputEnter;Name;" + txt.text);
    //     LogManager.logManager.AddEvent(Time.time, "InputEnter;Number;" + txt.text);
    //     userPath = userPath + userNumber + "\\";
    //     Directory.CreateDirectory(userPath);

    //     userNumber = txt.text;
    //     userPath = userPath + userNumber + "\\";
    //     Directory.CreateDirectory(userPath);
    // }

    public void ScoreZero()
    {
        Array.Clear(scoreLvl, 0, scoreLvl.Length);   
    }

    public void ScoreUpdate(int index, int score)
    {
        if (score > scoreLvl[index])
        {
            scoreLvl[index] = score;
        }

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
        return userID;
    }

    public string GetUserPath()
    {
        return userPath;
    }

    public void ChangeLanguage(int x)
    {
        // float t = Time.time;

        // // if (x == 0)
        // // {
        // //     LogManager.logManager.AddEvent(t, "ButtonClick;English");
        // // }
        // // else if (x == 1)
        // // {
        // //     LogManager.logManager.AddEvent(t, "ButtonClick;Polish");
        // // }

        lang = (LanguageOption)x;
    }

    public static List<String> stimuliMinus = new List<string>();
    public static List<String> stimuliPlus = new List<string>();

    void ReadStimuli()
	{
		StreamReader readtext = new StreamReader("Assets/Resources/Affective/auditory.csv");

		while (readtext.Peek() >= 0)
		{
			string [] line = readtext.ReadLine().Split(',');

			switch (line[0])
			{
				case "s-":
					
					stimuliMinus.Add(line[1]);
					break;

				case "s+":
					
					stimuliPlus.Add(line[1]);
					break;
			}
		}

		readtext.Close();

		Shuffle(stimuliMinus);
        Shuffle(stimuliPlus);
	}

	void Shuffle<T>(List<T> ts) {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i) {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }
}