using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEditor;
public class ScoreManager : MonoBehaviour
{
    public static int score;
    public int scoreToLvlUp = 300;

    Text text;

    float timer = 0f;
    float waitTime = 3f;

    int zeroLevel = 2;
    float zeroLevelWait = 115f;

    UserManager.LanguageOption lang;

    void Awake ()
    {
        LogManager.logManager.AddEvent(Time.time, "Scene;Load;" + SceneManager.GetActiveScene().buildIndex);

        lang = UserManager.lang;

        text = GetComponent <Text> ();
        
        if(lang.Equals(UserManager.LanguageOption._English))
        {
            text.text = "TRAGIC";
        }
        else if (lang.Equals(UserManager.LanguageOption._Polish))
        {
            text.text = "TRAGICZNIE";
        }

        if (SceneManager.GetActiveScene().buildIndex > 2)
        {
            scoreToLvlUp = Random.Range((scoreToLvlUp - 100), scoreToLvlUp);
        }

        score = 0;
    }

    void Update ()
    {
        if(Input.GetKey(KeyCode.P))
        {
            LogManager.logManager.AddEvent(Time.time, "KeyPress;P");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        string current = text.text;

        if(score < (1 * scoreToLvlUp / 5))
        {
            if(lang.Equals(UserManager.LanguageOption._English))
            {
                text.text = "TRAGIC";
            }
            else if (lang.Equals(UserManager.LanguageOption._Polish))
            {
                text.text = "TRAGICZNIE";
            }   
        }
        
        else if(score < (2 * scoreToLvlUp / 5))
        {
            if(lang.Equals(UserManager.LanguageOption._English))
            {
                text.text = "POORLY";
            }
            else if (lang.Equals(UserManager.LanguageOption._Polish))
            {
                text.text = "KIEPSKO";
            }   
        }

        else if(score < (3 * scoreToLvlUp / 5))
        {
            if(lang.Equals(UserManager.LanguageOption._English))
            {
                text.text = "DECENTLY";
            }
            else if (lang.Equals(UserManager.LanguageOption._Polish))
            {
                text.text = "PRZYZWOICIE";
            }   
        }

        else if(score < (4 * scoreToLvlUp / 5))
        {
            if(lang.Equals(UserManager.LanguageOption._English))
            {
                text.text = "GREAT";
            }
            else if (lang.Equals(UserManager.LanguageOption._Polish))
            {
                text.text = "WSPANIALE";
            }   
        }

        else if(score < (5 * scoreToLvlUp / 5))
        {
            if(lang.Equals(UserManager.LanguageOption._English))
            {
                text.text = "FANTASTIC";
            }
            else if (lang.Equals(UserManager.LanguageOption._Polish))
            {
                text.text = "FANTASTYCZNIE";
            }   
        }

        else
        {
            GameObject [] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
                EnemyHealth enemyHealth = enemy.GetComponent <EnemyHealth> ();
                enemyHealth.TakeDamageLvlEnd(enemyHealth.currentHealth);
            }

            timer += Time.deltaTime;

            if(timer > waitTime)
            {
                UserManager.userManager.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, score);
                if (SceneManager.GetActiveScene().buildIndex != zeroLevel || Time.timeSinceLevelLoad > zeroLevelWait)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
                }
            }
        }

        if (!current.Equals(text.text))
        {
            LogManager.logManager.AddEvent(Time.time, "TextScore;ChangeTo;" + text.text);
        }
    }
}