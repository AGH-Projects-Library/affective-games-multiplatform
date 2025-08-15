using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MrNightmareEnemyManager : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private EnemyHealth mrNightmareHealth;
    [SerializeField] private AffectiveEnemyManager affectiveEnemyManager;
    [SerializeField] private GameObject[] enemy;
    public Transform[] spawnPoints;

    [SerializeField] private int[] healthLevels = { 900, 600, 300, 30 };
    [SerializeField] private bool[] usedLevels = new bool[4];

    [SerializeField] private int[] helpNumbers = { 5, 7, 9, 11 };
    [SerializeField] private List<float> helpWaits = new List<float> { 3f, 2f, 4f, 1f };

    [field: SerializeField] public float AlertTime { get; private set; } = 5f;
    [SerializeField] private string[] alertMessages = { "Przyzwano sojuszników! Uważaj!", "Supporters are coming! Watch out!" };
    private string alertMessage;

    [SerializeField] private List<float> timers = new List<float> { 0f, 0f, 0f, 0f };

    [SerializeField] private bool enemySpeedFlag, affectiveSpawnMediumFlag, affectiveSpawnHardFlag;

    private bool ShouldSpawnEnemy()
    {
        return playerHealth.currentHealth > 0f && mrNightmareHealth.currentHealth <= healthLevels[0] && mrNightmareHealth.currentHealth > 0f && (
            (usedLevels[3] && timers[3] > (helpWaits[3] + 1f) && !affectiveSpawnHardFlag) ||
            (!usedLevels[3] && mrNightmareHealth.currentHealth <= healthLevels[3]) ||
            (usedLevels[2] && timers[2] > (helpWaits[2] + 1f) && !enemySpeedFlag) ||
            (!usedLevels[2] && mrNightmareHealth.currentHealth <= healthLevels[2]) ||
            (usedLevels[1] && timers[1] > (helpWaits[1] + 1f) && !affectiveSpawnMediumFlag) ||
            (!usedLevels[1] && mrNightmareHealth.currentHealth <= healthLevels[1]) ||
            (!usedLevels[0] && mrNightmareHealth.currentHealth <= healthLevels[0])
        );
    }

    private void SpawnEnemies()
    {
        if (ShouldSpawnEnemy())
        {
            UpdateTimers();
            if (ShouldSpawnHardEnemies()) { SpawnHardEnemies(); }
            else if (ShouldSpawnEnemiesAtLevel3()) { SpawnEnemiesAtLevel3(); }
            else if (ShouldSpeedUpEnemies()) { SpeedUpEnemies(); }
            else if (ShouldSpawnEnemiesAtLevel2()) { SpawnEnemiesAtLevel2(); }
            else if (ShouldSpawnMediumEnemies()) { SpawnMediumEnemies(); }
            else if (ShouldSpawnEnemiesAtLevel1()) { SpawnEnemiesAtLevel1(); }
            else if (ShouldSpawnEnemiesAtLevel0()) { SpawnEnemiesAtLevel0(); }
        }
    }

    private void SpawnEnemiesAtLevel(int level)
    {
        usedLevels[level] = true;
        StartCoroutine(CallForHelp(helpNumbers[level], helpWaits[level], "MrNightmareSpawn"));
        LogManager.logManager.AddEvent(Time.time, "Alert;Show;Time;" + AlertTime + ";Content;" + alertMessage.Replace("\n", ""));
        StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(AlertTime, alertMessage));
    }

    private IEnumerator CallForHelp(int number, float waitTime, string mechanic)
    {
        for (int i = 0; i < number; i++)
        {
            int enemyIndex = Random.Range(0, enemy.Length);
            int spawnPointIndex = Random.Range(0, spawnPoints.Length);
            Instantiate(enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
            LogManager.logManager.AddEvent(Time.time, "Enemy;Spawn;Type;" + enemyIndex + ";ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[spawnPointIndex].name + ";Mechanic;" + mechanic);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private void Awake() => alertMessage = UserManager.lang.Equals(UserManager.LanguageOption._English) ? alertMessages[1] : alertMessages[0];

    private bool ShouldSpawnHardEnemies()
    {
        return usedLevels[3] && timers[3] > (helpWaits[3] + 1f) && !affectiveSpawnHardFlag;
    }

    private void SpawnHardEnemies()
    {
        affectiveSpawnHardFlag = true;
        affectiveEnemyManager.invokeTime = 0f;
        affectiveEnemyManager.affectiveSpawnTimeHard = (int)Time.timeSinceLevelLoad;
        affectiveEnemyManager.affectiveSpawnTimeHardMax = (int)Time.timeSinceLevelLoad + 20;
    }

    private bool ShouldSpawnEnemiesAtLevel3()
    {
        return !usedLevels[3] && mrNightmareHealth.currentHealth <= healthLevels[3];
    }

    private void SpawnEnemiesAtLevel3()
    {
        SpawnEnemiesAtLevel(3);
    }

    private bool ShouldSpeedUpEnemies()
    {
        return usedLevels[2] && timers[2] > (helpWaits[2] + 1f) && !enemySpeedFlag;
    }

    private void SpeedUpEnemies()
    {
        enemySpeedFlag = true;
    }

    private bool ShouldSpawnEnemiesAtLevel2()
    {
        return !usedLevels[2] && mrNightmareHealth.currentHealth <= healthLevels[2];
    }

    private void SpawnEnemiesAtLevel2()
    {
        timers[2] = 0f;
        SpawnEnemiesAtLevel(2);
    }

    private bool ShouldSpawnMediumEnemies() => usedLevels[1] && timers[1] > (helpWaits[1] + 1f) && !affectiveSpawnMediumFlag;
    private void SpawnMediumEnemies()
    {
        affectiveSpawnMediumFlag = true;
        affectiveEnemyManager.invokeTime = 0f;
        affectiveEnemyManager.affectiveSpawnTimeMedium = (int)Time.timeSinceLevelLoad;
        affectiveEnemyManager.affectiveSpawnTimeMediumMax = (int)Time.timeSinceLevelLoad + 20;
    }

    private bool ShouldSpawnEnemiesAtLevel1()
    {
        return !usedLevels[1] && mrNightmareHealth.currentHealth <= healthLevels[1];
    }

    private void SpawnEnemiesAtLevel1()
    {
        SpawnEnemiesAtLevel(1);
    }

    private bool ShouldSpawnEnemiesAtLevel0()
    {
        return !usedLevels[0] && mrNightmareHealth.currentHealth <= healthLevels[0];
    }

    private void SpawnEnemiesAtLevel0()
    {
        SpawnEnemiesAtLevel(0);
    }

    private void UpdateTimers()
    {
        for (int i = 0; i < usedLevels.Length; i++)
        {
            if (usedLevels[i])
                this.timers[i] += Time.deltaTime;
        }
    }
}
