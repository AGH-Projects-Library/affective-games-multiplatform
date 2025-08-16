using UnityEngine;
using UnityEngine.Events;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    public static int Score { get; private set; }

    public static event UnityAction<int> OnScoreUpdated;
    public static event UnityAction OnLevelUpReached;

    [SerializeField] private int scoreToLevelUp = 300;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);
        Score = 0;

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex > 2)
            scoreToLevelUp = Random.Range(scoreToLevelUp - 100, scoreToLevelUp);

        LogManager.Log(Time.time, $"Score;ToLevelUp;Value;{scoreToLevelUp}");
    }

    public static void AddScore(int value)
    {
        Score += value;
        OnScoreUpdated?.Invoke(Score);

        if (Score >= Instance.scoreToLevelUp)
            OnLevelUpReached?.Invoke();
    }
}