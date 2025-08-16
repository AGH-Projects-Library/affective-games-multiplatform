using System.Collections;
using Localization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Text textGO;
    [SerializeField] private string keyGameOverText = "gameover.text";
    [SerializeField] private string nameScoreBoard = "ScoreBoard";

    [SerializeField] private float timeCD = 3.0f;
    [SerializeField] private string animationName = "GameOver";

    private Animator anim;
    private bool flagUsed = false;

    private void MakeThisTheOnlyGameOverManager()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);
    }

    void Awake()
    {
        MakeThisTheOnlyGameOverManager();
        anim = GetComponent<Animator>();
        textGO.text = LocalizationManager.GetText(keyGameOverText);
    }

    void Update()
    {
        if (IsPlayerDead() && IsTimeUp()) RestartLevel();
        else if (IsPlayerDead() && !flagUsed) HandleGameOver();
        else if (Input.GetKey(KeyCode.B)) GoToScoreBoard();
    }

    private bool IsPlayerDead() => playerHealth.currentHealth <= 0;
    private bool IsTimeUp() => timeCD <= 0;

    private void RestartLevel()
    {
        // UserManager.Instance.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleGameOver()
    {
        flagUsed = true;
        anim.SetTrigger(animationName);
        timeCD = 4.0f;
        StartCoroutine(LoseTime());
    }

    private void GoToScoreBoard()
    {
        // UserManager.Instance.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
        SceneManager.LoadScene(nameScoreBoard);
    }

    private IEnumerator LoseTime()
    {
        while (true)
        {
            string txt = LocalizationManager.GetText(keyGameOverText) + timeCD + "s";
            textGO.text = txt;
            yield return new WaitForSeconds(1);
            timeCD -= 1;
        }
    }
}
