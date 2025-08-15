using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    [SerializeField] private UnityEngine.UI.Text textGO;
    float timeCD = 3.0f;
    
    [SerializeField] private string textGOvPL = "Przegrałeś!\nRestart poziomu za: ";
    [SerializeField] private string textGOvENG = "You lost!\nThe level will restart in: ";
    [SerializeField] private UserManager.LanguageOption lang;

    Animator anim;
    [SerializeField] private string animationName = "GameOver";
    
    string nameScoreBoard = "ScoreBoard";

    bool flagUsed = false;

    void Awake()
    {
        anim = GetComponent<Animator>();
        textGO.text = GetGameOverText();
    }

    private string GetGameOverText()
    {
        return lang == UserManager.LanguageOption._Polish ? textGOvPL : textGOvENG;
    }


    void Update()
    {
        if (playerHealth.currentHealth <= 0 && timeCD <= 0) UpdateScoreAndRestartLevel();
        else if (playerHealth.currentHealth <= 0 && !flagUsed) HandleGameOver();
        else if (Input.GetKey(KeyCode.B)) HandleScoreboardLoad();
    }

    private bool IsPlayerDead() => playerHealth.currentHealth <= 0;
    private bool IsTimeUp() => timeCD <= 0;

    private void UpdateScoreAndRestartLevel()
    {
        UserManager.userManager.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
        LogManager.logManager.AddEvent(Time.time, "Score;PlayerDeath;Level;" + SceneManager.GetActiveScene().buildIndex + ";Value;" + ScoreManager.score);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleGameOver()
    {
        flagUsed = true;
        anim.SetTrigger(animationName);
        LogManager.logManager.AddEvent(Time.time, "Game;Over;Animation;PlayerDeath");
        timeCD = 4.0f;
        StartCoroutine(LoseTime());
    }
    private void HandleScoreboardLoad()
    {
        UserManager.userManager.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
        LogManager.logManager.AddEvent(Time.time, "Key;B");
        SceneManager.LoadScene(nameScoreBoard);
    }

    private IEnumerator LoseTime() {
        while (true) {
            string txt = GetGameOverText() + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;Over;CountDown;Text;ChangeTo;" + txt.Replace("\n", ""));
            textGO.text = txt;
            yield return new WaitForSeconds(1);
            timeCD -= 1;
        }
    }
}
