using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MrNightmareEnemyManager : MonoBehaviour
{
    public static MrNightmareEnemyManager Instance { get; private set; }
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private EnemyHealth mrNightmareHealth;
    [SerializeField] private AffectiveEnemyManager affectiveEnemyManager;
    [SerializeField] private GameObject[] enemy;
    [SerializeField] private Transform[] spawnPoints;

    [SerializeField] private int[] healthLevels = { 900, 600, 300, 30 };
    [SerializeField] private bool[] usedLevels = new bool[4];

    [SerializeField] private int[] helpNumbers = { 5, 7, 9, 11 };
    [SerializeField] private List<float> helpWaits = new List<float> { 3f, 2f, 4f, 1f };

    [SerializeField] private float alertTime = 5f;
    [SerializeField] private string keyNightmareSupportAlert = "alert.nightmareSupport";

    private List<float> timers = new List<float> { 0f, 0f, 0f, 0f };
    private bool enemySpeedFlag, affectiveSpawnMediumFlag, affectiveSpawnHardFlag;

    private void Awake()
    {
        MakeThisTheOnlyMrNightmareEnemyManager();
    }

    private void MakeThisTheOnlyMrNightmareEnemyManager()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (ShouldSpawnEnemy())
        {
            UpdateTimers();
            if (ShouldSpawnHardEnemies()) { SpawnHardEnemies(); }
            else if (ShouldSpawnEnemiesAtLevel3()) { SpawnEnemiesAtLevel(3); }
            else if (ShouldSpeedUpEnemies()) { SpeedUpEnemies(); }
            else if (ShouldSpawnEnemiesAtLevel2()) { SpawnEnemiesAtLevel(2); }
            else if (ShouldSpawnMediumEnemies()) { SpawnMediumEnemies(); }
            else if (ShouldSpawnEnemiesAtLevel1()) { SpawnEnemiesAtLevel(1); }
            else if (ShouldSpawnEnemiesAtLevel0()) { SpawnEnemiesAtLevel(0); }
        }
    }

    private bool ShouldSpawnEnemy()
    {
        return playerHealth.currentHealth > 0f &&
               mrNightmareHealth.currentHealth <= healthLevels[0] &&
               mrNightmareHealth.currentHealth > 0f &&
               ((!usedLevels[3] && mrNightmareHealth.currentHealth <= healthLevels[3]) ||
                (!usedLevels[2] && mrNightmareHealth.currentHealth <= healthLevels[2]) ||
                (!usedLevels[1] && mrNightmareHealth.currentHealth <= healthLevels[1]) ||
                (!usedLevels[0] && mrNightmareHealth.currentHealth <= healthLevels[0]));
    }

    private void SpawnEnemiesAtLevel(int level)
    {
        usedLevels[level] = true;
        StartCoroutine(CallForHelp(helpNumbers[level], helpWaits[level], "MrNightmareSpawn"));
        StartCoroutine(ImportantAlertManager.Instance.ShowAlertAndLerp(alertTime, LocalizationManager.Instance.GetText(keyNightmareSupportAlert)));
    }

    private IEnumerator CallForHelp(int number, float waitTime, string mechanic)
    {
        for (int i = 0; i < number; i++)
        {
            int enemyIndex = Random.Range(0, enemy.Length);
            int spawnPointIndex = Random.Range(0, spawnPoints.Length);
            Instantiate(enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private bool ShouldSpawnHardEnemies() => usedLevels[3] && timers[3] > (helpWaits[3] + 1f) && !affectiveSpawnHardFlag;
    private void SpawnHardEnemies()
    {
        affectiveSpawnHardFlag = true;
        affectiveEnemyManager.invokeTime = 0f;
        affectiveEnemyManager.affectiveSpawnTimeHard = (int)Time.timeSinceLevelLoad;
        affectiveEnemyManager.affectiveSpawnTimeHardMax = (int)Time.timeSinceLevelLoad + 20;
    }

    private bool ShouldSpawnEnemiesAtLevel3() => !usedLevels[3] && mrNightmareHealth.currentHealth <= healthLevels[3];
    private bool ShouldSpeedUpEnemies() => usedLevels[2] && timers[2] > (helpWaits[2] + 1f) && !enemySpeedFlag;
    private void SpeedUpEnemies() => enemySpeedFlag = true;
    private bool ShouldSpawnEnemiesAtLevel2() => !usedLevels[2] && mrNightmareHealth.currentHealth <= healthLevels[2];
    private bool ShouldSpawnMediumEnemies() => usedLevels[1] && timers[1] > (helpWaits[1] + 1f) && !affectiveSpawnMediumFlag;
    private void SpawnMediumEnemies()
    {
        affectiveSpawnMediumFlag = true;
        affectiveEnemyManager.invokeTime = 0f;
        affectiveEnemyManager.affectiveSpawnTimeMedium = (int)Time.timeSinceLevelLoad;
        affectiveEnemyManager.affectiveSpawnTimeMediumMax = (int)Time.timeSinceLevelLoad + 20;
    }
    private bool ShouldSpawnEnemiesAtLevel1() => !usedLevels[1] && mrNightmareHealth.currentHealth <= healthLevels[1];
    private bool ShouldSpawnEnemiesAtLevel0() => !usedLevels[0] && mrNightmareHealth.currentHealth <= healthLevels[0];

    private void UpdateTimers()
    {
        for (int i = 0; i < usedLevels.Length; i++)
            if (usedLevels[i]) timers[i] += Time.deltaTime;
    }
}
