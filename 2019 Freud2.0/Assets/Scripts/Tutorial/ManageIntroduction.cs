using System.Collections;
using Localization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class ManageIntroduction : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text textShowed;

    [Header("Input Settings")]
    [SerializeField] private string fireButton2 = "Fire2";
    [SerializeField] private string fireButton3 = "Fire3";
    [FormerlySerializedAs("keyS")] [SerializeField] private KeyCode skipKey = KeyCode.S;
    [SerializeField] private KeyCode quitKey = KeyCode.Escape;
    [SerializeField] private KeyCode startCountdownKey = KeyCode.Space;

    [Header("Localization Keys")]
    [SerializeField] private string keyWelcome = "intro.welcome";
    [SerializeField] private string keyTimeLabel = "intro.timeLabel";

    [Header("Timing Settings")]
    [SerializeField] private float timeToStart = 30f;
    [SerializeField] private float timeWait = 1f;
    [SerializeField] private float countdownStep = 1f;

    private int timeCD;
    private Coroutine countdownRoutine;

    private void Start(){
        timeCD=Mathf.CeilToInt(timeToStart);
        UpdateWelcomeText();
        // StartCountdown();
    }

    private void Update(){
        HandleLanguageInput();
        HandleSkip();
        HandleQuit();
        HandleStartCountdownInput();
    }

    private void HandleLanguageInput(){
        if (Input.GetButtonDown(fireButton2)) SetLanguage(GameLanguage.English);
        if (Input.GetButtonDown(fireButton3)) SetLanguage(GameLanguage.Polish);
    }

    public void SetLanguageEnglish()=>SetLanguage(GameLanguage.English);
    public void SetLanguagePolish()=>SetLanguage(GameLanguage.Polish);

    private void SetLanguage(GameLanguage lang){
        LocalizationManager.SetLanguage(lang);
        RefreshText();
    }

    private void RefreshText(){if (countdownRoutine==null) UpdateWelcomeText(); else SetCountdownText();}
    private void HandleSkip(){if (countdownRoutine!=null && Input.GetKeyDown(skipKey)) LoadLvl_0();}
    private void HandleQuit(){if (Input.GetKeyDown(quitKey)) Application.Quit();}
    private void HandleStartCountdownInput(){if (Input.GetKeyDown(startCountdownKey)) StartCountdown();}
    private void UpdateWelcomeText()
    {
        if (LocalizationManager.TryGetText(keyWelcome, out var text)) textShowed.text = text;
        else textShowed.text = keyWelcome;
    }
    public void LoadLvl_0()=>StartCoroutine(WaitAndLoad(SceneSwapper.MainMenuSceneIndex+2));

    private IEnumerator WaitAndLoad(int sceneIndex){
        yield return new WaitForSeconds(timeWait);
        SceneSwapper.LoadScene(sceneIndex);
    }

    public void StartCountdown(){
        if (countdownRoutine!=null) StopCoroutine(countdownRoutine);
        timeCD=Mathf.CeilToInt(timeToStart);
        countdownRoutine=StartCoroutine(Countdown());
    }

    private IEnumerator Countdown(){
        while (timeCD>=0){
            SetCountdownText();
            timeCD--;
            yield return new WaitForSeconds(countdownStep);
        }
        LoadLvl_0();
    }

    private void SetCountdownText()
    {
        textShowed.text = LocalizationManager.TryGetText(keyTimeLabel, out var label) ? $"{label} {timeCD}s" : $"{keyTimeLabel} {timeCD}s";
    }
}
