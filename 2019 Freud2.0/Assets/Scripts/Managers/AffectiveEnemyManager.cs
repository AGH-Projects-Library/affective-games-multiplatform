using System.Collections;
using UnityEngine;

public class AffectiveEnemyManager : MonoBehaviour 
{
    [SerializeField] private int affectiveSpawnTimeMedium = 60;
    [SerializeField] private int affectiveSpawnTimeMediumMax = 80;
    [SerializeField] private int affectiveSpawnTimeHard = 90;
    [SerializeField] private int affectiveSpawnTimeHardMax = 180;
    [SerializeField] private int preparationTime = 5;
    [SerializeField] private float spawnTime = 3f;
    [SerializeField] private float invokeTime = 5f;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject[] enemy;
    [SerializeField] private Transform[] spawnPoints;

    private bool checkedMedium = false;
    private bool checkedHard = false;
    private float timerSpawnMax = 0f;
    private float remainTime = 5f;
    private float alertTime = 5f;
    private string alertMessageNonAffectivePl = "Nadchodzą kolejne potwory!\nUważaj!";
    private string alertMessageNonAffectiveEng = "More and more monsters are coming!\nWatch out!";
    private string alertMessageNonAffective = "";

    private void Awake()
    {
        alertMessageNonAffective = UserManager.lang.Equals(UserManager.LanguageOption._English) ? alertMessageNonAffectiveEng : alertMessageNonAffectivePl;
    }

    private void Start() => InvokeRepeating("Spawn", invokeTime, spawnTime);

    private void Update()
    {
        if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeMedium - remainTime)) timerSpawnMax += Time.deltaTime;
        if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeMedium - preparationTime) && !checkedMedium) ShowAlert(alertMessageNonAffective, checkedMedium = true);
        else if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeHard - preparationTime) && !checkedHard) ShowAlert(alertMessageNonAffective, checkedHard = true);
    }

    private void Spawn()
    {
        if (playerHealth.currentHealth <= 0f || GameObject.FindGameObjectsWithTag("Enemy").Length >= EnemyManager.maxEnemies) return;
        int enemyIndex = Random.Range(0, enemy.Length);
        if (Time.timeSinceLevelLoad >= affectiveSpawnTimeMedium) SpawnEnemy(enemyIndex, Random.Range(0, (int)(spawnPoints.Length / Random.Range(1, 3))), "AdditionalMediumSpawn");
        if (Time.timeSinceLevelLoad >= affectiveSpawnTimeHard) SpawnEnemy(enemyIndex, Random.Range(0, (int)(spawnPoints.Length / Random.Range(1, 2))), "AdditionalHardSpawn");
    }

    private void SpawnEnemy(int enemyIndex, int spawnPointIndex, string mechanic)
    {
        Instantiate(enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
        LogManager.logManager.AddEvent(Time.time, $"Enemy;Spawn;Type;{enemyIndex};ID;{gameObject.GetInstanceID()};SpawnPoint;{spawnPoints[spawnPointIndex].name};Mechanic;{mechanic}");
    }

    private void ShowAlert(string message, bool checkedFlag)
    {
        LogManager.logManager.AddEvent(Time.time, $"Alert;Show;Time;{alertTime};Content;{message.Replace("\n", "")}");
        StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, message));
    }
}
