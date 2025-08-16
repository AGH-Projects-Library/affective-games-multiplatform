using System.Collections;
using UnityEngine;

public class AffectiveEnemyManager : MonoBehaviour
{
    public static AffectiveEnemyManager Instance { get; private set; }
    [SerializeField] public int affectiveSpawnTimeMedium = 60;
    [SerializeField] public int affectiveSpawnTimeMediumMax = 80;
    [SerializeField] public int affectiveSpawnTimeHard = 90;
    [SerializeField] public int affectiveSpawnTimeHardMax = 180;
    [SerializeField] public int preparationTime = 5;
    [SerializeField] public float spawnTime = 3f;
    [SerializeField] public float invokeTime = 5f;
    [SerializeField] public PlayerHealth playerHealth;
    [SerializeField] public GameObject[] enemy;
    [SerializeField] public Transform[] spawnPoints;

    [SerializeField] public float alertTime = 5f;
    [SerializeField] public string keyAlertNonAffective = "alert.moreMonsters";

    public bool checkedMedium = false;
    public bool checkedHard = false;
    public float timerSpawnMax = 0f;
    public float remainTime = 5f;

    private void Awake()
    {
        MakeThisTheOnlyAffectiveEnemyManager();
    }

    private void MakeThisTheOnlyAffectiveEnemyManager()
    {
        Instance = this;
    }

    private void Start() => InvokeRepeating("Spawn", invokeTime, spawnTime);

    private void Update()
    {
        if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeMedium - remainTime)) timerSpawnMax += Time.deltaTime;
        if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeMedium - preparationTime) && !checkedMedium)
            ShowAlert(LocalizationManager.Instance.GetText(keyAlertNonAffective), checkedMedium = true);
        else if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeHard - preparationTime) && !checkedHard)
            ShowAlert(LocalizationManager.Instance.GetText(keyAlertNonAffective), checkedHard = true);
    }

    private void Spawn()
    {
        if (playerHealth.currentHealth <= 0f || GameObject.FindGameObjectsWithTag("Enemy").Length >= EnemyManager.Instance.maxEnemies) return;
        int enemyIndex = Random.Range(0, enemy.Length);
        if (Time.timeSinceLevelLoad >= affectiveSpawnTimeMedium)
            SpawnEnemy(enemyIndex, Random.Range(0, spawnPoints.Length), "AdditionalMediumSpawn");
        if (Time.timeSinceLevelLoad >= affectiveSpawnTimeHard)
            SpawnEnemy(enemyIndex, Random.Range(0, spawnPoints.Length), "AdditionalHardSpawn");
    }

    private void SpawnEnemy(int enemyIndex, int spawnPointIndex, string mechanic)
    {
        Instantiate(enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
        LogManager.Instance.AddEvent(Time.time, $"Enemy;Spawn;Type;{enemyIndex};SpawnPoint;{spawnPoints[spawnPointIndex].name};Mechanic;{mechanic}");
    }

    private void ShowAlert(string message, bool checkedFlag)
    {
        StartCoroutine(ImportantAlertManager.Instance.ShowAlertAndLerp(alertTime, message));
    }
}
