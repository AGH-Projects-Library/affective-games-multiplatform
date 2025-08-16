using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : iSingleton<GameStateManager>
{
    [SerializeField] private int zeroLevel = 2;
    [SerializeField] private float zeroLevelWait = 105f;
    [SerializeField] private float levelUpDelay = 3f;

    private float levelUpTimer;
    private bool zeroLevelTriggered;

    private new void Awake()
    {
        base.Awake();
        ScoreManager.OnLevelUpReached += HandleLevelUp;
    }

    private void Update()
    {
        if (IsZeroLevelTimeout()) TryLoadNextLevelForZeroLevel();
        if (IsGameEnded()) EndGame();
    }

    private void HandleLevelUp()
    {
        DestroyAllEnemiesAndPickups();
        levelUpTimer += Time.deltaTime;
        if (levelUpTimer > levelUpDelay)
        {
            LogManager.Log(Time.time, $"Score;LvlEnd;Level;{SceneManager.GetActiveScene().buildIndex};Value;{ScoreManager.Score}");
            SceneSwapper.LoadNextScene();
        }
    }

    private bool IsZeroLevelTimeout() =>
        SceneManager.GetActiveScene().buildIndex == zeroLevel && Time.timeSinceLevelLoad > zeroLevelWait;

    private void TryLoadNextLevelForZeroLevel()
    {
        if (zeroLevelTriggered) return;
        if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0 ||
            GameObject.FindGameObjectsWithTag("PickUp").Length == 0)
        {
            zeroLevelTriggered = true;
            SceneSwapper.LoadNextScene();
        }
    }

    private void DestroyAllEnemiesAndPickups()
    {
        DestroyGameObjectsWithTag("Enemy");
        DestroyGameObjectsWithTag("PickUp");
    }

    private void DestroyGameObjectsWithTag(string tag)
    {
        foreach (var obj in GameObject.FindGameObjectsWithTag(tag))
            Destroy(obj);
    }

    private bool IsGameEnded() => Time.time > 1200;

    private void EndGame()
    {
        // UserManager.Instance.UpdateScore(SceneManager.GetActiveScene().buildIndex, ScoreManager.Score);
        LogManager.Log(Time.time, $"Score;GameEnd;Level;{SceneManager.GetActiveScene().buildIndex};Value;{ScoreManager.Score}");
        SceneSwapper.LoadEndGameScene();
    }
}
