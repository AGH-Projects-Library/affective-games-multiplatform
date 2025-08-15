using System.Collections;
using UnityEngine;

public class PickUpManager : MonoBehaviour
{
    [SerializeField] private float spawnDelay = 7f;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject pickUp;
    [SerializeField] private Transform[] spawnPoints;

    private int currentSpawnPointIndex;

    private bool IsPlayerAlive => playerHealth.currentHealth > 0f;

    private void Start()
    {
        RandomizeArray(spawnPoints);
        currentSpawnPointIndex = 0;
        StartCoroutine(SpawnPickUps());
    }

    private IEnumerator SpawnPickUps()
    {
        yield return new WaitForSeconds(5f);
        while (IsPlayerAlive)
        {
            SpawnPickUp();
            LogManager.Instance.AddEvent(Time.time, "PickUp;Spawn;ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[currentSpawnPointIndex].name);
            currentSpawnPointIndex = (currentSpawnPointIndex + 1) % spawnPoints.Length;

            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private void SpawnPickUp()
    {
        Instantiate(pickUp, spawnPoints[currentSpawnPointIndex].position, spawnPoints[currentSpawnPointIndex].rotation);
    }

    private void RandomizeArray<T>(T[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--) {
            int r = Random.Range(0, i);
            T tmp = arr[i];
            arr[i] = arr[r];
            arr[r] = tmp;
        }
    }
}
