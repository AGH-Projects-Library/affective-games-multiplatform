using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Localization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ManageScoreBoard : MonoBehaviour
{
    [SerializeField] private Text textBestPlayers;
    [SerializeField] private Text closeTxt;

    [SerializeField] private KeyCode keyEscape = KeyCode.Escape;
    [SerializeField] private KeyCode keyR = KeyCode.R;
    [SerializeField] private KeyCode keyS = KeyCode.S;

    [SerializeField] private string keyCloseText = "scoreboard.close";

    private string pathScores;
    private Dictionary<int, List<string>> bestPlayers = new Dictionary<int, List<string>>();

    private int indexIntroduction = 0;
    private int indexFirstLevel = 3;

    [SerializeField] private float timeToStart = 15;
    private float timeCD;

    private string persDataPath;

    void Awake()
    {
        persDataPath = Application.persistentDataPath;
        pathScores = Path.Combine(persDataPath, "Scores.csv");

        timeCD = (int)timeToStart;
        StartCoroutine(LoseTime());

        // string namePlayer = UserManager.Instance.GetUserName();
        // int scorePlayer = UserManager.Instance.GetCumulatedScore();

        string localizedTemplate = LocalizationManager.GetText(keyCloseText);
        // string closeText = string.Format(localizedTemplate, namePlayer, scorePlayer);

        ReadBestPlayers();
        // AddNewPlayer(namePlayer, scorePlayer);
        PrintBestPlayers();
        // UserManager.Instance.ScoreZero();
        // closeTxt.text = closeText;
    }

    void Update()
    {
        if (timeCD < 0) Application.Quit();

        if (Input.GetKeyDown(keyEscape)) Application.Quit();
        else if (Input.GetKeyDown(keyR))
        {
            DestroyIndestructible();
            SceneManager.LoadScene(indexIntroduction);
        }
        else if (Input.GetKeyDown(keyS))
        {
            SceneManager.LoadScene(indexFirstLevel);
        }
    }

    void DestroyIndestructible()
    {
        var indestructible = GameObject.FindGameObjectsWithTag("DontDestroyObject");
        foreach (var i in indestructible) Destroy(i);
        var bitalino = GameObject.FindWithTag("BITalino");
        if (bitalino != null) Destroy(bitalino);
    }

    void ReadBestPlayers()
    {
        if (File.Exists(pathScores))
        {
            using (var reader = new StreamReader(pathScores))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] elements = line.Split(';');
                    if (!int.TryParse(elements[0], out int score)) continue;
                    var names = elements.Skip(1).Where(e => !string.IsNullOrEmpty(e)).ToList();
                    bestPlayers[score] = names;
                }
            }
        }
    }

    void AddNewPlayer(string name, int score)
    {
        if (!bestPlayers.ContainsKey(score)) bestPlayers[score] = new List<string>();
        bestPlayers[score].Add(name);
    }

    void PrintBestPlayers()
    {
        var bests = "";
        int i = 0;
        foreach (var kvp in bestPlayers.OrderByDescending(k => k.Key))
        {
            foreach (var player in kvp.Value)
            {
                i++;
                bests += $"{i}. {player} {kvp.Key}\n";
            }
        }
        textBestPlayers.text = bests;
    }

    void WriteBestPlayers()
    {
        using (var writer = new StreamWriter(pathScores, false))
        {
            foreach (var kvp in bestPlayers)
            {
                string line = $"{kvp.Key};" + string.Join(";", kvp.Value) + ";";
                writer.WriteLine(line);
            }
        }
    }

    private System.Collections.IEnumerator LoseTime()
    {
        while (true)
        {
            closeTxt.text = closeTxt.text.Split(new[] { timeCD + "s" }, StringSplitOptions.None)[0] + timeCD + "s";
            timeCD -= 1;
            yield return new WaitForSeconds(1);
        }
    }
}
