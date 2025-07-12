using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public float spawnTime = 3f;
    public float invokeTime = 5f;
    public PlayerHealth playerHealth;
    public GameObject enemy;
    public Transform[] spawnPoints;


    void Start ()
    {
        InvokeRepeating ("Spawn", invokeTime, spawnTime);
    }


    void Spawn ()
    {
        if(playerHealth.currentHealth <= 0f)
        {
            return;
        }

        int spawnPointIndex = Random.Range (0, spawnPoints.Length);

        Instantiate (enemy, spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
        LogManager.logManager.AddEvent(Time.time, "Enemy;0;RegularSpawn;AtSpawnPoint;" + spawnPointIndex);
    }
}
