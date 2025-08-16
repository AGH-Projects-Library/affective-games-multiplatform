using System.Collections;
using System.Collections.Generic;
using Localization;
using UnityEngine;
using UnityEngine.UI;

public class ManageZeroLvl : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private PlayerShooting playerShooting;
    [SerializeField] private string keyAlertPrefix = "zeroLvl.alert";

    private float startTime = 0.5f;
    private float endTime = 6f;
    private float timer = 0f;
    private bool used = false;
    private int i;

    private List<string> alerts = new List<string>();
    private GameObject[] elementsHUD;

    void Start()
    {
        timer -= startTime;
        i = 0;

        alerts.Clear();
        for (int idx = 1; ; idx++)
        {
            string key = $"{keyAlertPrefix}{idx}";
            string localized = LocalizationManager.GetText(key);
            if (localized.StartsWith("[MISSING:]"))
            {
                LogManager.Log(Time.time, $"Found {idx - 1} alerts for zero level tutorial.");
                break;
            }
            alerts.Add(localized);
        }

        elementsHUD = GameObject.FindGameObjectsWithTag("TurnOff");
        foreach (var el in elementsHUD)
            el.SetActive(false);
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (i < alerts.Count)
            ShowAlerts(endTime - 1.0f, alerts[i]);

        if (i == 2) elementsHUD[1].SetActive(true);
        else if (i == 3) { elementsHUD[2].SetActive(true); elementsHUD[2].GetComponent<Text>().text = "0"; }
        else if (i == 4) elementsHUD[0].SetActive(true);
        else if (i == 5) { playerMovement.enabled = true; cameraFollow.enabled = true; }
        else if (i == 10) playerShooting.enabled = true;
        else if (i == 14)
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var enemy in enemies) enemy.GetComponent<EnemyMovement>().SetSpeed(1);
        }
    }

    void ShowAlerts(float time, string alert)
    {
        if (timer > startTime && !used)
        {
            // StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(time, alert));
            i++;
            used = true;
        }

        if (timer > startTime + time)
        {
            used = false;
            timer = 0f;
        }
    }
}
