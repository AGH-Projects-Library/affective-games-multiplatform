using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UserManager : MonoBehaviour
{
    public static UserManager Instance { get; private set; }

    // private string userName;
    // private string userID;
    // private string userPath;
    // private int[] scoreLvl;

    // private void Awake()
    // {
    //     if (Instance == null) Instance = this;
    //     else { Destroy(gameObject); return; }
    //
    //     DontDestroyOnLoad(gameObject);
    //     InitializeUser();
    // }
    //
    // private void InitializeUser()
    // {
    //     string persDataPath = Application.persistentDataPath;
    //     userPath = persDataPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\";
    //
    //     Cursor.visible = false;
    //     Screen.fullScreen = true;
    //
    //     userID = "0000";
    //     userName = "Zawodnik0000";
    //
    //     string idFile = persDataPath + "\\currentNumber.txt";
    //     if (File.Exists(idFile))
    //     {
    //         using (StreamReader reader = new StreamReader(idFile))
    //             userID = reader.ReadLine() ?? "0000";
    //         userName = "Zawodnik" + userID;
    //     }
    //
    //     LogManager.Log(Time.time, "UserName;" + userName);
    //     LogManager.Log(Time.time, "UserID;" + userID);
    //
    //     userPath += userID + "\\";
    //     Directory.CreateDirectory(userPath);
    //
    //     int lvls = SceneManager.sceneCountInBuildSettings;
    //     scoreLvl = new int[lvls];
    //     Array.Clear(scoreLvl, 0, scoreLvl.Length);
    // }
    //
    // public void UpdateScore(int index, int score)
    // {
    //     if (index < scoreLvl.Length)
    //     {
    //         scoreLvl[index] += score;
    //         LogManager.Log(Time.time, $"Score;Update;Level;{index};Value;{score}");
    //     }
    //     else Debug.LogWarning("Index out of bounds for scoreLvl array.");
    // }
    //
    // public int GetTotalScore() => scoreLvl.Sum();
    // public string GetUserName() => userName;
    // public string GetUserNumber() => userID;
    // public string GetUserPath() => userPath;
    // public void ResetScore() => Array.Clear(scoreLvl, 0, scoreLvl.Length);
}
