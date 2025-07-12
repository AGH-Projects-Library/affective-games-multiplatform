using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public PlayerHealth playerHealth;

    Animator anim;
    string nameAnimation = "GameOver";
    
    string nameScoreBoard = "ScoreBoard";

    bool flagUsed = false;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }


    void Update()
    {
        if (playerHealth.currentHealth <= 0)
        {
            if(flagUsed)
            {
                LogManager.logManager.AddEvent(Time.time, "Animation;GameOver");
                flagUsed = true;
            }
            anim.SetTrigger(nameAnimation);

            if (Input.GetKey(KeyCode.B))
            {
                UserManager.userManager.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
                LogManager.logManager.AddEvent(Time.time, "KeyPress;B");
                SceneManager.LoadScene(nameScoreBoard);
            }
        }
    }
}
