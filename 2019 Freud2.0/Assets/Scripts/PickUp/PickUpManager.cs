using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpManager : MonoBehaviour {

	public float spawnTime = 7f;
    public float invokeTime = 5f;
    public PlayerHealth playerHealth;
    public GameObject pickUp;
    public Transform[] spawnPoints;

	int spawnPointIndex;


    void Start ()
    {
		RandomizeArray(spawnPoints);
		spawnPointIndex = 0;	
        	
        StartCoroutine("Spawn");
    }

    IEnumerator Spawn()
    {
        yield return new WaitForSeconds(5.0f);
    
        while(true)
        {
            if(playerHealth.currentHealth <= 0f)
            {
                break;
            }

            Instantiate (pickUp, spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
            LogManager.logManager.AddEvent(Time.time, "PickUp;Spawn;ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[spawnPointIndex].name);

            spawnPointIndex++;
            if (spawnPointIndex >= spawnPoints.Length)
            {
                spawnPointIndex = 0;   
            }
            yield return new WaitForSeconds(7.0f);
        }
    }

	void RandomizeArray<T> (T[] arr)
	{
		for (int i = arr.Length - 1; i > 0; i--) 
		{
     		int r = Random.Range(0, i);
     		T tmp = arr[i];
     		arr[i] = arr[r];
     		arr[r] = tmp;
   		}
	}
}
