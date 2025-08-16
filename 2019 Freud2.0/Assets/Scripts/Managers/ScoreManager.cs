using UnityEngine;
using UnityEngine.Events;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    public static int Score { get; private set; }

    public static event UnityAction<int> OnScoreUpdated;

    [Header("Level Settings")]
    [SerializeField] private int scoreToLevelUp = 300;
    [SerializeField] private int zeroLevel = 2;
    [SerializeField] private float zeroLevelWait = 105f;
    [SerializeField] private float levelUpDelay = 3f;

    [Header("Controls")]
    [SerializeField] private KeyCode skipLevelKey = KeyCode.P;

    private float levelUpTimer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Score = 0;

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex > 2)
            scoreToLevelUp = Random.Range(scoreToLevelUp - 100, scoreToLevelUp);

        LogManager.Log(Time.time, $"Score;ToLevelUp;Value;{scoreToLevelUp}");
    }

    private void Update()
    {
        if (Input.GetKey(skipLevelKey)) SceneSwapper.Instance.LoadNextScene();
        if (IsLevelUpConditionMet()) HandleLevelUp();
        if (IsZeroLevelTimeout()) TryLoadNextLevelForZeroLevel();
    }
    private bool IsLevelUpConditionMet() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex != zeroLevel && Score >= scoreToLevelUp;
    private void HandleLevelUp()
    {
        DestroyAllEnemiesAndPickups();
        levelUpTimer += Time.deltaTime;
        if (levelUpTimer > levelUpDelay) FinishLevelUp();
    }
    private void FinishLevelUp()
    {
        SaveScore();
        SceneSwapper.Instance.LoadNextScene();
    }
    private bool IsZeroLevelTimeout() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == zeroLevel && Time.timeSinceLevelLoad > zeroLevelWait;
    private void TryLoadNextLevelForZeroLevel()
    {
        if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0 ||
            GameObject.FindGameObjectsWithTag("PickUp").Length == 0)
            SceneSwapper.Instance.LoadNextScene();
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
    private void SaveScore() => LogManager.Log(Time.time, $"Score;LvlEnd;Level;{UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex};Value;{Score}");
    public static void AddScore(int value)
    {
        Score += value;
        OnScoreUpdated?.Invoke(Score);
    }
}

// Below code is used to paste to the LLM so that it knows how to generate other classes that will be compatible with this one

// List all the public and private variables, methods (with parameters if any), and properties
/***
class ScoreManager {
    +Instance: ScoreManager
    +Score: int
    +OnScoreUpdated: UnityAction\<int>
    -scoreToLevelUp: int
    -zeroLevel: int
    -zeroLevelWait: float
    -levelUpDelay: float
    -skipLevelKey: KeyCode
    -levelUpTimer: float

    +Awake(): void
    +Update(): void
    -IsLevelUpConditionMet(): bool
    -HandleLevelUp(): void
    -FinishLevelUp(): void
    -IsZeroLevelTimeout(): bool
    -TryLoadNextLevelForZeroLevel(): void
    -DestroyAllEnemiesAndPickups(): void
    -DestroyGameObjectsWithTag(tag: string): void
    -SaveScore(): void
    +AddScore(value: int): void
}
@enduml
***/