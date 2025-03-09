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
    float zeroLevelWait = 105f;

    UserManager.LanguageOption lang;

    void Awake ()
    {
        LogManager.logManager.AddEvent(Time.time, "Scene;Load;ID;" + SceneManager.GetActiveScene().buildIndex);

        lang = UserManager.lang;

        text = GetComponent <Text> ();
        
        // F2
        // if(lang.Equals(UserManager.LanguageOption._English))
        // {
        //     text.text = "TRAGIC";
        // }
        // else if (lang.Equals(UserManager.LanguageOption._Polish))
        // {
        //     text.text = "TRAGICZNIE";
        // }

        if (SceneManager.GetActiveScene().buildIndex > 2)
        {
            scoreToLvlUp = Random.Range((scoreToLvlUp - 100), scoreToLvlUp);
            LogManager.logManager.AddEvent(Time.time, "Score;ToLevelUp;Value;" + scoreToLvlUp);
        }

        score = 0;
    }

    void Update ()
    {
        if(Input.GetKey(KeyCode.P))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;P");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        text.text = score.ToString();

        // F2
        // string current = text.text;

        // if(score < (1 * scoreToLvlUp / 5))
        // {
        //     if(lang.Equals(UserManager.LanguageOption._English))
        //     {
        //         text.text = "TRAGIC";
        //     }
        //     else if (lang.Equals(UserManager.LanguageOption._Polish))
        //     {
        //         text.text = "TRAGICZNIE";
        //     }   
        // }
        
        // else if(score < (2 * scoreToLvlUp / 5))
        // {
        //     if(lang.Equals(UserManager.LanguageOption._English))
        //     {
        //         text.text = "POORLY";
        //     }
        //     else if (lang.Equals(UserManager.LanguageOption._Polish))
        //     {
        //         text.text = "KIEPSKO";
        //     }   
        // }

        // else if(score < (3 * scoreToLvlUp / 5))
        // {
        //     if(lang.Equals(UserManager.LanguageOption._English))
        //     {
        //         text.text = "DECENTLY";
        //     }
        //     else if (lang.Equals(UserManager.LanguageOption._Polish))
        //     {
        //         text.text = "PRZYZWOICIE";
        //     }   
        // }

        // else if(score < (4 * scoreToLvlUp / 5))
        // {
        //     if(lang.Equals(UserManager.LanguageOption._English))
        //     {
        //         text.text = "GREAT";
        //     }
        //     else if (lang.Equals(UserManager.LanguageOption._Polish))
        //     {
        //         text.text = "WSPANIALE";
        //     }   
        // }

        // else if(score < (5 * scoreToLvlUp / 5))
        // {
        //     if(lang.Equals(UserManager.LanguageOption._English))
        //     {
        //         text.text = "FANTASTIC";
        //     }
        //     else if (lang.Equals(UserManager.LanguageOption._Polish))
        //     {
        //         text.text = "FANTASTYCZNIE";
        //     }   
        // }

        // else
        // {
        if (SceneManager.GetActiveScene().buildIndex != zeroLevel && score >= scoreToLvlUp)
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
                LogManager.logManager.AddEvent(Time.time, "Score;LvlEnd;Level;" + SceneManager.GetActiveScene().buildIndex + ";Value;" + ScoreManager.score);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }

        if (SceneManager.GetActiveScene().buildIndex == zeroLevel && Time.timeSinceLevelLoad > zeroLevelWait)
        {
            GameObject [] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            GameObject [] pickups = GameObject.FindGameObjectsWithTag("PickUp");
            
            if (enemies.Length == 0 || pickups.Length == 0)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }

        }

        // F2
        // if (!current.Equals(text.text))
        // {
        //     LogManager.logManager.AddEvent(Time.time, "TextScore;ChangeTo;" + text.text);
        // }
    }
}