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
	
	string pathScores = "Assets\\Datalogs\\Scores.csv";
	Dictionary<int, List<string>> bestPlayers = new Dictionary<int, List<string>> ();

	string tagIndestructible = "DontDestroyObject";
	string tagBITalino = "BITalino";

	int indexIntroduction = 0;
	int indexFirstLevel = 3;

	void Awake()
	{
		ReadBestPlayers();
		AddNewPlayer();
		PrintBestPlayers();
		UserManager.userManager.ScoreZero();
		LogManager.logManager.AddEvent(Time.time, "ScoreBoard;Show");
		WriteBestPlayers();
	}

	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Escape))
        {
            LogManager.logManager.AddEvent(Time.time, "KeyPress;Esc");
			Application.Quit(); // ignored in UnityEditor
			EditorApplication.isPlaying = false;
        }

		else if(Input.GetKeyDown(KeyCode.R))
        {
			LogManager.logManager.AddEvent(Time.time, "KeyPress;R");
			DestroyIndestructible();
	        SceneManager.LoadScene(indexIntroduction);
        }

		else if(Input.GetKeyDown(KeyCode.O))
        {
			LogManager.logManager.AddEvent(Time.time, "KeyPress;O");
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

	void ReadBestPlayers()
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


	void AddNewPlayer()
	{
		bool bitalinoUse = BitalinoController.bitalinoController.bitalinoUse;
		string use = bitalinoUse ? "1" : "0";
		string currentName = UserManager.userManager.GetUserName() + " (" + use + ")";
		int currentScore = UserManager.userManager.GetCumulatedScore();

		if (!bestPlayers.ContainsKey(currentScore))
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


	void PrintBestPlayers()
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


	void WriteBestPlayers()
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

		writer.Close();
	}
}