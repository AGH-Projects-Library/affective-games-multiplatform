using System.IO;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public float spawnTime = 3f;
    public float invokeTime = 5f;
    public PlayerHealth playerHealth;
    public GameObject enemy;
    public Transform[] spawnPoints;

    [SerializeField] private int maxEnemies = 8;

    void Start()
    {
        if (File.Exists(Application.persistentDataPath + "\\enemiesAmount.txt"))
        {
            StreamReader readtext = new StreamReader(Application.persistentDataPath + "\\enemiesAmount.txt");
            maxEnemies = int.Parse(readtext.ReadLine());
            readtext.Close();
        }
        else
        {
            maxEnemies = 8;
        }

        InvokeRepeating(nameof(Spawn), invokeTime, spawnTime);
    }

    private void Spawn()
    {
        if (playerHealth.currentHealth <= 0f || GameObject.FindGameObjectsWithTag("Enemy").Length >= maxEnemies)
            return;

        int spawnPointIndex = Random.Range(0, spawnPoints.Length);

        Instantiate(enemy, spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
        LogManager.logManager.AddEvent(Time.time, "Enemy;Spawn;Type;" + "-1" + ";ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[spawnPointIndex].name + ";Mechanic;" + "RegularSpawn");
    }
}
