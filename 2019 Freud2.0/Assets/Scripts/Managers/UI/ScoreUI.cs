using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    public UnityAction<int> OnScoreChanged;

    private void Awake()
    {
        if (scoreText == null) scoreText = GetComponent<Text>();
        OnScoreChanged += UpdateScoreText;
    }

    private void OnEnable() => ScoreManager.OnScoreUpdated += OnScoreChanged.Invoke;
    private void OnDisable() => ScoreManager.OnScoreUpdated -= OnScoreChanged.Invoke;

    private void UpdateScoreText(int newScore) => scoreText.text = newScore.ToString();
}