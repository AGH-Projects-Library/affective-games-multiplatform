using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public PlayerHealth playerHealth;

    public Text textGO;
    float timeCD = 3.0f;

    string textGOv;
    string textGOvPL = "Przegrałeś!\nRestart poziomu za: ";
    string textGOvENG = "You lost!\nThe level will restart in: ";

    UserManager.LanguageOption lang;

    Animator anim;
    string nameAnimation = "GameOver";
    
    string nameScoreBoard = "ScoreBoard";

    bool flagUsed = false;

    void Awake()
    {
        anim = GetComponent<Animator>();

        lang = UserManager.lang;

        if(lang.Equals(UserManager.LanguageOption._Polish))
        {
            textGOv = textGOvPL; 
        }

        else if(lang.Equals(UserManager.LanguageOption._English))
        {
            textGOv = textGOvENG; 
        }

        textGO.text = textGOv;

    }


    void Update()
    {
        if (playerHealth.currentHealth <= 0)
        {
            if (timeCD < 0)
            {
                UserManager.userManager.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
                LogManager.logManager.AddEvent(Time.time, "Score;PlayerDeath;Level;" + SceneManager.GetActiveScene().buildIndex + ";Value;" + ScoreManager.score);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   
            }

            if(!flagUsed)
            {
                flagUsed = true;
                anim.SetTrigger(nameAnimation);
                LogManager.logManager.AddEvent(Time.time, "Game;Over;Animation;PlayerDeath");
                timeCD = 4.0f;
	            StartCoroutine("LoseTime");
            }

            if (Input.GetKey(KeyCode.B))
            {
                UserManager.userManager.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
                LogManager.logManager.AddEvent(Time.time, "Key;B");
                SceneManager.LoadScene(nameScoreBoard);
            }

        }
    }

    IEnumerator LoseTime()
	{
		while(true)
		{
            string txt = textGOv + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;Over;CountDown;Text;ChangeTo;" + txt.Replace("\n", ""));
            yield return new WaitForSeconds(1);
			textGO.text = txt;
			timeCD -= 1;
		}
	}
}
