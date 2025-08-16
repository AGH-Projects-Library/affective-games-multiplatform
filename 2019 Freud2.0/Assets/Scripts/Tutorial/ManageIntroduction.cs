using System.Collections;
using Localization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class ManageIntroduction : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text textShowed;

    [Header("Input Settings")]
    [SerializeField] private string fireButton2 = "Fire2";
    [SerializeField] private string fireButton3 = "Fire3";
    [FormerlySerializedAs("keyS")] [SerializeField] private KeyCode skipKey = KeyCode.S;

    [Header("Localization Keys")]
    [SerializeField] private string keyWelcome = "intro.welcome";
    [SerializeField] private string keyTimeLabel = "intro.timeLabel";

    [Header("Timing Settings")]
    [SerializeField] private float timeToStart = 30f;
    [SerializeField] private float timeWait = 1f;

    private int timeCD;

    private void Start()
    {
        timeCD = Mathf.CeilToInt(timeToStart);
        UpdateWelcomeText();
        StartCoroutine(LoseTimeCoroutine());
    }

    private void Update()
    {
        HandleLanguageInput();
        HandleSkipOrTimeout();
        HandleQuit();
    }

    private void HandleLanguageInput()
    {
        if (Input.GetButtonDown(fireButton2)) SetLanguage(GameLanguage.English);
        if (Input.GetButtonDown(fireButton3)) SetLanguage(GameLanguage.Polish);
    }

    private void HandleSkipOrTimeout()
    {
        if (Input.GetKeyDown(skipKey) || Time.timeSinceLevelLoad >= timeToStart) LoadLvl0();
    }

    private void HandleQuit()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
    }

    private void SetLanguage(GameLanguage lang)
    {
        LocalizationManager.SetLanguage(lang);
        UpdateWelcomeText();
    }

    private void UpdateWelcomeText()
    {
        textShowed.text = LocalizationManager.GetText(keyWelcome);
    }

    public void LoadTutorial() => StartCoroutine(WaitAndLoad(SceneManager.GetActiveScene().buildIndex + 3));

    public void LoadLvl0() => StartCoroutine(WaitAndLoad(SceneSwapper.MainMenuSceneIndex + 2));

    private IEnumerator WaitAndLoad(int sceneIndex)
    {
        yield return new WaitForSeconds(timeWait);
        SceneManager.LoadScene(sceneIndex);
    }

    private IEnumerator LoseTimeCoroutine()
    {
        while (true)
        {
            textShowed.text = $"{LocalizationManager.GetText(keyTimeLabel)}{timeCD}s";
            timeCD--;
            yield return new WaitForSeconds(1f);
        }
    }
}
