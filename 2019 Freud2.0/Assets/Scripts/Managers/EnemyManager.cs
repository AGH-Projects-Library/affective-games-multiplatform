using System.IO;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
	public float spawnTime = 3f;
	public float invokeTime = 5f;
	public PlayerHealth playerHealth;
	public GameObject enemy;
	public Transform[] spawnPoints;

	public static int maxEnemies = 8;

	string persDataPath;

	// Pilot: keep existing behavior, but expose maxEnemies in inspector for tweaking
	// and keep existing file I/O logic intact for now.
 

    void Start ()
    {
        persDataPath = Application.persistentDataPath;
 


        if (File.Exists(persDataPath + "\\enemiesAmount.txt")) 
        {
            StreamReader readtext = new StreamReader(persDataPath + "\\enemiesAmount.txt");
            maxEnemies = int.Parse(readtext.ReadLine());
            readtext.Close();
        }
        else
        {
            maxEnemies = 8;
        }

        InvokeRepeating("Spawn", invokeTime, spawnTime);
    }


    void Spawn()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (playerHealth.currentHealth <= 0f || enemies.Length >= maxEnemies)
        {
            return;
        }

        int spawnPointIndex = Random.Range(0, spawnPoints.Length);

        Instantiate(enemy, spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
        LogManager.logManager.AddEvent(Time.time, "Enemy;Spawn;Type;" + "-1" + ";ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[spawnPointIndex].name + ";Mechanic;" + "RegularSpawn");
    }
}
