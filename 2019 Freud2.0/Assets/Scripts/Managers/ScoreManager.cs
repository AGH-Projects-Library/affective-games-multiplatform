using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    public static int score;
    [SerializeField] private int scoreToLevelUp = 300;

    Text text;

    float timer = 0f;
    float waitTime = 3f;

    int zeroLevel = 2;
    [SerializeField] private float zeroLevelWait = 105f;

    // UserManager.Language lang;
    public KeyCode keyP = KeyCode.P;


    private void MakeThisTheOnlyScoreManager()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);
    }

    void Awake()
    {
        MakeThisTheOnlyScoreManager();
        LogManager.Instance.AddEvent(Time.time, "Scene;Load;ID;" + SceneManager.GetActiveScene().buildIndex);

        // lang = UserManager.lang;

        text = GetComponent<Text>();

        if (SceneManager.GetActiveScene().buildIndex > 2)
        {
            scoreToLevelUp = Random.Range((scoreToLevelUp - 100), scoreToLevelUp);
            LogManager.Instance.AddEvent(Time.time, "Score;ToLevelUp;Value;" + scoreToLevelUp);
        }

        score = 0;
    }

    void Update()
    {
        if (Input.GetKey(keyP))
        {
            LogManager.Instance.AddEvent(Time.time, "Key;P");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        text.text = score.ToString();

        if (ShouldLevelUp())
        {
            KillEnemiesAndPickups();

            timer += Time.deltaTime;

            if(timer > waitTime)
            {
                UpdateScore(SceneManager.GetActiveScene().buildIndex, score);
                LogManager.Instance.AddEvent(Time.time, "Score;LvlEnd;Level;" + SceneManager.GetActiveScene().buildIndex + ";Value;" + score);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }

        if (ShouldLoadNextLevel())
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            GameObject[] pickups = GameObject.FindGameObjectsWithTag("PickUp");

            if (enemies.Length == 0 || pickups.Length == 0)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }
    }

    private bool ShouldLevelUp()
    {
        return SceneManager.GetActiveScene().buildIndex != zeroLevel && score >= scoreToLevelUp;
    }

    private bool ShouldLoadNextLevel()
    {
        return SceneManager.GetActiveScene().buildIndex == zeroLevel && Time.timeSinceLevelLoad > zeroLevelWait;
    }

    private void UpdateScore(int level, int newScore)
    {
        // UserManager.Instance.ScoreUpdate(level, newScore);
    }

    private void KillEnemiesAndPickups()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject[] pickups = GameObject.FindGameObjectsWithTag("PickUp");

        foreach (GameObject enemy in enemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            // enemyHealth.TakeDamageLvlEnd(enemyHealth.currentHealth);
        }
    }
}
