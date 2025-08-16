using System.Collections;
using Localization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class ManageEnd : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text textCD;

    [SerializeField] private KeyCode keyEscape = KeyCode.Escape;
    [SerializeField] private KeyCode keySkipToScoreBoard = KeyCode.T;
    [SerializeField] private UnityEvent OnScoreBoardRequested = new UnityEvent();

    [SerializeField] private string keyTimeLabel = "end.timeLabel";

    [SerializeField] private float timeToStart = 15f;
    [SerializeField] private string nameScoreBoard = "ScoreBoard";

    private int timeCD;

    void Awake()
    {
        timeCD = (int)timeToStart;
        StartCoroutine(LoseTime());
    }

    void Update()
    {
        if (timeCD < 0) TriggerScoreBoard();
        if (Input.GetKey(keyEscape)) Application.Quit();
        if (Input.GetKeyDown(keySkipToScoreBoard)) TriggerScoreBoard();
    }

    public void TriggerScoreBoard()
    {
        OnScoreBoardRequested?.Invoke();
        if (OnScoreBoardRequested.GetPersistentEventCount() == 0)
            SceneManager.LoadScene(nameScoreBoard);
    }

    IEnumerator LoseTime()
    {
        while (true)
        {
            string txt = LocalizationManager.Instance.GetText(keyTimeLabel) + timeCD + "s";
            textCD.text = txt;
            timeCD -= 1;
            yield return new WaitForSeconds(1);
        }
    }
}