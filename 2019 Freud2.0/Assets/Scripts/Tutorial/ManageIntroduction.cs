using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class ManageIntroduction : MonoBehaviour
{
    [SerializeField] private Text textShowed;
    [SerializeField] private UnityEvent OnIntroductionFinished;
    [SerializeField] private UnityEvent OnLanguageChanged;
    [SerializeField] private string fireButton2 = "Fire2";
    [SerializeField] private string fireButton3 = "Fire3";
    [SerializeField] private KeyCode keyS = KeyCode.S;

    [SerializeField] private string keyWelcomeEng = "intro.welcome.eng";
    [SerializeField] private string keyWelcomePl = "intro.welcome.pl";
    [SerializeField] private string keyTimeLabel = "intro.timeLabel";

    [SerializeField] private float timeToStart = 30;
    [SerializeField] private float timeWait = 1f;
    private int timeCD;
    private GameLanguage currentLang;

    void Start()
    {
        timeCD = (int)timeToStart;
        StartCoroutine(LoseTime());

        currentLang = LocalizationManager.Instance.CurrentLanguage;
        ChangeLanguage();
    }

    void Update()
    {
        if (Input.GetButtonDown(fireButton2))
        {
            LocalizationManager.Instance.SetLanguage(GameLanguage.English);
            ChangeLanguage();
            OnLanguageChanged?.Invoke();
        }

        if (Input.GetButtonDown(fireButton3))
        {
            LocalizationManager.Instance.SetLanguage(GameLanguage.Polish);
            ChangeLanguage();
            OnLanguageChanged?.Invoke();
        }

        if (Input.GetKeyDown(keyS)) LoadLvl0();

        if (timeToStart < Time.timeSinceLevelLoad) LoadLvl0();

        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
    }

    void ChangeLanguage()
    {
        currentLang = LocalizationManager.Instance.CurrentLanguage;
        string welcome = currentLang == GameLanguage.English ?
            LocalizationManager.Instance.GetText(keyWelcomeEng) :
            LocalizationManager.Instance.GetText(keyWelcomePl);
        textShowed.text = welcome;
    }

    public void LoadTutorial()
    {
        StartCoroutine(WaitAndLoad(SceneManager.GetActiveScene().buildIndex + 3));
        OnIntroductionFinished?.Invoke();
    }

    public void LoadLvl0()
    {
        StartCoroutine(WaitAndLoad(SceneManager.GetActiveScene().buildIndex + 2));
        OnIntroductionFinished?.Invoke();
    }

    IEnumerator WaitAndLoad(int sceneIndex)
    {
        yield return new WaitForSeconds(timeWait);
        SceneManager.LoadScene(sceneIndex);
    }

    IEnumerator LoseTime()
    {
        while (true)
        {
            string txt = LocalizationManager.Instance.GetText(keyTimeLabel) + timeCD + "s";
            textShowed.text = txt;
            timeCD -= 1;
            yield return new WaitForSeconds(1);
        }
    }
}
