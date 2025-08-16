using System.Collections;
using Localization;
using UnityEngine;

using System.Collections;
using UnityEngine;
using Localization;

public class AffectiveEnemyManager : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private float alertTime = 5f;
    [SerializeField] private string keyAlertNonAffective = "alert.moreMonsters";

    public void SpawnMediumWave()
    {
        ShowAlert();
        StartCoroutine(SpawnWave("AdditionalMediumSpawn"));
    }

    public void SpawnHardWave()
    {
        ShowAlert();
        StartCoroutine(SpawnWave("AdditionalHardSpawn"));
    }

    private IEnumerator SpawnWave(string mechanic)
    {
        int enemyCount = Random.Range(3, 6); // configurable range
        for (int i = 0; i < enemyCount; i++)
        {
            if (playerHealth.currentHealth <= 0f) yield break;
            if (GameObject.FindGameObjectsWithTag("Enemy").Length >= EnemyManager.Instance.maxEnemies) yield break;

            int enemyIndex = Random.Range(0, enemyPrefabs.Length);
            int spawnIndex = Random.Range(0, spawnPoints.Length);

            Instantiate(enemyPrefabs[enemyIndex], spawnPoints[spawnIndex].position, spawnPoints[spawnIndex].rotation);
            LogManager.Log(Time.time, $"Enemy;Spawn;Type;{enemyIndex};SpawnPoint;{spawnPoints[spawnIndex].name};Mechanic;{mechanic}");

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void ShowAlert()
    {
        if (LocalizationManager.TryGetText(keyAlertNonAffective, out var alertText))
            HudPopupTextManager.ShowAlert(alertText, alertTime);
    }
}

