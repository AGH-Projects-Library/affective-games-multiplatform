using UnityEngine;

public abstract class EnemySpawner : SingletonBase<EnemySpawner>
{
    [SerializeField] protected float spawnInterval = 3f;
    [SerializeField] protected float invokeDelay = 5f;
    [SerializeField] protected PlayerHealth playerHealth;
    [SerializeField] protected Transform[] spawnPoints;
    [SerializeField] public int maxEnemies = 8;

    protected virtual void Start(){ if (hasInstance) InvokeRepeating(nameof(SpawnTick), invokeDelay, spawnInterval); }
    void SpawnTick(){ if (CanSpawn()) Spawn(CreateEnemy(), PickPoint()); } 
    protected bool CanSpawn()=> playerHealth.currentHealth>0f && GameObject.FindGameObjectsWithTag("Enemy").Length<maxEnemies; // one-liner
    protected Transform PickPoint()=> spawnPoints[Random.Range(0, spawnPoints.Length)];
    protected abstract GameObject CreateEnemy(); // factory method
    protected virtual void Spawn(GameObject prefab, Transform p){ Instantiate(prefab, p.position, p.rotation); LogManager.Log(Time.time,$"Enemy;Spawn;Type;{-1};ID;{gameObject.GetInstanceID()};SpawnPoint;{p.name};Mechanic;RegularSpawn"); }
}

public class EnemyManager : EnemySpawner
{
    [SerializeField] private GameObject enemyPrefab;
    protected override GameObject CreateEnemy()=> enemyPrefab; // one-liner
}
