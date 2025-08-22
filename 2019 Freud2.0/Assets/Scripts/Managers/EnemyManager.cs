using System.IO;
using UnityEngine;

public class EnemyManager : SingletonBase<EnemyManager>
{
    public float spawnTime = 3f;
    public float invokeTime = 5f;
    public PlayerHealth playerHealth;
    public GameObject enemy;
    public Transform[] spawnPoints;

    [SerializeField] private int _maxEnemies = 8;
    public int maxEnemies
    {
        get => maxEnemies;
        set => maxEnemies = value;
    }
    
    private void Awake()
    {
        InitInstance();
    }

    void Start()
    {
        if (!InstanceExists())
            return;
        InvokeRepeating(nameof(Spawn), invokeTime, spawnTime);
    }

    private void Spawn()
    {
        if (playerHealth.currentHealth <= 0f || GameObject.FindGameObjectsWithTag("Enemy").Length >= maxEnemies)
            return;

        int spawnPointIndex = Random.Range(0, spawnPoints.Length);

        Instantiate(enemy, spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
        LogManager.Log(Time.time, "Enemy;Spawn;Type;" + "-1" + ";ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[spawnPointIndex].name + ";Mechanic;" + "RegularSpawn");
    }
}
