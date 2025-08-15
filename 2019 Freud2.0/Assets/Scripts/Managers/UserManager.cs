using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UserManager : MonoBehaviour 
{
    public static UserManager Instance { get; private set; }
    public enum Language { English, Polish };

    [SerializeField] public Language language = Language.Polish;
    string userName;
    string userID;
    string persDataPath;
    string userPath;
    int lvls = 0;
    int[] scoreLvl;
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
            userID = readtext.ReadLine() ?? "0000";
            userName = "Zawodnik" + userID;
            readtext.Close();
        }
        
        LogManager.logManager.AddEvent(Time.time, "UserName;" + userName);
        LogManager.logManager.AddEvent(Time.time, "UserID;" + userID);

        userPath += userID + "\\";
        Directory.CreateDirectory(userPath);

        scoreLvl = new int[lvls];
        Array.Clear(scoreLvl, 0, scoreLvl.Length);

        used = false;

        MakeThisTheOnlyUserManager();
    }
    
    void Update()
    {
        if (IsGameEnded() && !used)
        {   
            used = true;
            UpdateScore(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
            LogManager.logManager.AddEvent(Time.time, $"Score;GameEnd;Level;{SceneManager.GetActiveScene().buildIndex};Value;{ScoreManager.score}");
            SceneManager.LoadScene("ScoreBoard");
        }
    }

    private void MakeThisTheOnlyUserManager() => Instance == null ? (DontDestroyOnLoad(gameObject), Instance = this) : Instance != this && Destroy(gameObject);
    public void ResetScore() => Array.Clear(scoreLvl, 0, scoreLvl.Length);
    public void UpdateScore(int index, int score) => score > scoreLvl[index] && (scoreLvl[index] = score);
    public int GetTotalScore() => (scoreLvl[2] = 0, scoreLvl.Sum());
    public string GetUserName() => userName;
    public string GetUserNumber() => userID;
    public string GetUserPath() => userPath;
    public void ChangeLanguage(int x) => language = (Language)x;
    private bool IsGameEnded() => Time.time > 1200;

    private System.Collections.Generic.List<String> _stimuliMinus = new System.Collections.Generic.List<string>();
    private System.Collections.Generic.List<String> _stimuliPlus = new System.Collections.Generic.List<string>();
    void ReadStimuli()
    {
        StreamReader readtext = new StreamReader("Assets/Resources/Affective/auditory.csv");

        while (readtext.Peek() >= 0)
        {
            string [] line = readtext.ReadLine().Split(',');

            switch (line[0])
            {
                case "s-":
                    _stimuliMinus.Add(line[1]);
                    break;

                case "s+":
                    _stimuliPlus.Add(line[1]);
                    break;
            }
        }

        readtext.Close();

        Shuffle(_stimuliMinus);
        Shuffle(_stimuliPlus);
    }

    void Shuffle<T>(System.Collections.Generic.List<T> ts) {
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
