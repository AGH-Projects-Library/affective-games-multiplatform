using System.Collections;
using System.Collections.Generic;
using Localization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ManageTutorials : MonoBehaviour
{
    [SerializeField] private Text textShowed;
    [SerializeField] private Text textCD;
    [SerializeField] private KeyCode skipLevelKey = KeyCode.T;
    [SerializeField] private KeyCode skipTutorialKey = KeyCode.U;
    [SerializeField] private KeyCode quitKey = KeyCode.Escape;
    [SerializeField] private float timeToStart = 30;

    [SerializeField] private string keyTimeLabel = "tutorial.timeLabel";

    private List<string> texts = new List<string>();
    private int index;
    private int timeCD;

    private void Awake()
    {
        timeCD = (int)timeToStart;
        StartCoroutine(LoseTime());
        index = 0;
        UpdateTexts(SceneManager.GetActiveScene().buildIndex);
        if (texts.Count > 0) textShowed.text = texts[index];
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

        if (timeToStart < Time.timeSinceLevelLoad)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

        if (Input.GetKeyDown(quitKey))
            Application.Quit();

        if (Input.GetKeyDown(skipLevelKey))
            StartCoroutine("CountDown");

        else if (Input.GetKeyDown(skipTutorialKey))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
    }

    public void RunGame() => StartCoroutine(CountDown());

    IEnumerator CountDown()
    {
        index = -1;
        for (int i = 3; i >= 0; i--)
        {
            textShowed.text = i.ToString();
            yield return new WaitForSeconds(i == 0 ? 0.25f : 1f);
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void UpdateTexts(int lvl)
    {
        texts.Clear();
        for (int i = 1; ; i++)
        {
            string key = $"tutorial.level{lvl}.line{i}";
            string localized = LocalizationManager.GetText(key);
            if (localized.StartsWith("[MISSING:")) break;
            texts.Add(localized);
        }
    }

    private IEnumerator LoseTime()
    {
        while (true)
        {
            string txt = LocalizationManager.GetText(keyTimeLabel) + timeCD + "s";
            textCD.text = txt;
            timeCD -= 1;
            yield return new WaitForSeconds(1);
        }
    }
}
