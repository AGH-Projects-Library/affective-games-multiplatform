using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;

public class ManageScoreBoard : MonoBehaviour 
{
	public Text textBestPlayers;

	public Text closeTxt;
	// Key mappings (editable in inspector)
	public KeyCode keyEscape = KeyCode.Escape;
	public KeyCode keyR = KeyCode.R;
	public KeyCode keyS = KeyCode.S;
	
	string pathScores = Path.Combine(UserManager.Instance.GetUserPath(), "Scores.csv");
	Dictionary<int, List<string>> bestPlayers = new Dictionary<int, List<string>> ();

	string tagIndestructible = "DontDestroyObject";
	string tagBITalino = "BITalino";

	int indexIntroduction = 0;
	int indexFirstLevel = 3;

	public float timeToStart = 15;

	float timeCD;

	string closeText = "";

	string persDataPath;

	void Awake()
	{
		persDataPath = Application.persistentDataPath;
		pathScores = persDataPath + "\\Scores.csv";
		
		timeCD = (int)timeToStart;
	    StartCoroutine("LoseTime");

		string namePlayer = UserManager.userManager.GetUserName();
		int scorePlayer = UserManager.userManager.GetCumulatedScore();

		if(UserManager.Instance.language == UserManager.Language.English)
		{
			closeText = String.Format("<color=red>Your name: {0}.\n Your score: {1}.\n</color> The game will end in: ", namePlayer, scorePlayer);
		}

		else if(UserManager.Instance.language == UserManager.Language.Polish)
		{
			closeText = String.Format("<color=red>Twoja nazwa: {0}.\n Twój wynik: {1}.\n</color> Gra zakończy się za: ", namePlayer, scorePlayer);
		}

		ReadBestPlayers(UserManager.Instance.language);
		AddNewPlayer(namePlayer, scorePlayer, UserManager.Instance.language);
		PrintBestPlayers(UserManager.Instance.language);
		UserManager.userManager.ResetScore();
		LogManager.logManager.AddEvent(Time.time, "ScoreBoard;Show");
		WriteBestPlayers(UserManager.Instance.language);

		
	}

	void Update()
	{
		if (timeCD < 0)
        {
            Application.Quit();
        }

		if(Input.GetKeyDown(keyEscape))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
			// EditorApplication.isPlaying = false;
        }

		else if(Input.GetKeyDown(keyR))
        {
			LogManager.logManager.AddEvent(Time.time, "Key;R");
			DestroyIndestructible();
	        SceneManager.LoadScene(indexIntroduction);
        }

		else if(Input.GetKeyDown(keyS))
        {
			LogManager.logManager.AddEvent(Time.time, "Key;S");
	        SceneManager.LoadScene(indexFirstLevel);
        }
	}

	void DestroyIndestructible()
	{
		GameObject [] indestructible = GameObject.FindGameObjectsWithTag(tagIndestructible);
		foreach (GameObject i in indestructible)
		{
			Destroy(i);
		}

		Destroy(GameObject.FindWithTag(tagBITalino));
	}

	void ReadBestPlayers(UserManager.Language lang)
	{
		if (File.Exists(pathScores))
		{
			StreamReader reader = new StreamReader(pathScores);
			string line;
			while((line = reader.ReadLine()) != null)  
			{
				string[] elements = line.Split(';');

				int score = 0;
				Int32.TryParse(elements[0], out score);

				List<string> names = new List<string> ();
				for (int i = 1; i < (elements.Length - 1); i++)
				{
					names.Add(elements[i]);
				}

				bestPlayers.Add(score, names);
			}

			reader.Close();
		}
	}


	void AddNewPlayer(string cN, int cS, UserManager.Language lang)
	{
	    // F2
		// bool bitalinoUse = BitalinoController.bitalinoController.bitalinoUse;
		// string use = bitalinoUse ? "1" : "0";
		string currentName = cN; // + " (" + use + ")";     // F2
		int currentScore = cS;

		if (!bestPlayers.ContainsKey(currentScore) || lang != UserManager.Instance.language)
		{
			List<string> players = new List<string> ();
			players.Add(currentName);
			bestPlayers.Add(currentScore, players);
		}

		else
		{
			List<string> players = new List<string> ();
			players = bestPlayers[currentScore];
			bestPlayers.Remove(currentScore);
			players.Add(currentName);
			bestPlayers.Add(currentScore, players);
		}
	}


	void PrintBestPlayers(UserManager.Language lang)
	{
		string bests = "";
		int i = 0;

		List<int> keyList = bestPlayers.Keys.ToList();
		keyList.Sort();
		keyList.Reverse();
		
		foreach (int key in keyList)
		{
			foreach (string el in bestPlayers[key])
			{
				i++;
				bests += String.Format("{0}. {1} {2}\n" , i, el, key); 
			}
		}

		textBestPlayers.text = bests;
	}


	void WriteBestPlayers(UserManager.Language lang)
	{
		if(File.Exists(pathScores))
		{
			File.Delete(pathScores);
		}

		StreamWriter writer = new StreamWriter(pathScores);
        
		List<int> keyList = bestPlayers.Keys.ToList();
		foreach (int key in keyList)
		{
			string bests = "";
			foreach (string el in bestPlayers[key])
			{
				bests += String.Format("{0};", el);
			}

			string line = String.Format("{0};", key);
			line += bests;
			
			writer.WriteLine(line);
		}

		writer.Dispose();
	}

	IEnumerator LoseTime()
	{
		while(true)
		{
			string txt = closeText + timeCD.ToString() + "s";
			LogManager.logManager.AddEvent(Time.time, "Game;ScoreBoard;CountDown;Text;ChangeTo;" + txt);

            yield return new WaitForSeconds(1);
			closeTxt.text = txt;
			timeCD -= 1;
		}
	}
}
