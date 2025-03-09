using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MrNightmareEnemyManger : MonoBehaviour {

	public PlayerHealth playerHealth;
	public EnemyHealth mrNightmareHealth;
	public AffectiveEnemyManager affectiveEnemyManager;
	public GameObject[] enemy;
    public Transform[] spawnPoints;

	public int healthLvlOne = 900;
	public int healthLvlTwo = 600;
	public int healthLvlThree = 300;
	public int healthLvlFour = 30;

	bool usedLvlOne = false;
	bool usedLvlTwo = false;
	bool usedLvlThree = false;
	bool usedLvlFour = false;

	
	public int hellpNumberLvlOne = 5;
	public int hellpNumberLvlTwo = 7;
	public int hellpNumberLvlThree = 9;
	public int hellpNumberLvlFour = 11;
	public float hellpWaitLvlOne = 3f;
	public float hellpWaitLvlTwo = 2f;
	public float hellpWaitLvlThree = 4f;
	public float hellpWaitLvlFour = 1f;

	float alertTime = 5f;
	string alertMessagePl = "Przyzwano sojuszników! Uważaj!";
	string alertMessageEng = "Supporters are coming! Watch out!";

	string alertMessage = "";

	float timerLvlThree = 0f;
	float timerLvlTwo = 0f;
	float timerLvlFour = 0f;
	bool enemySpeedFlag = false;
	bool affectiveSpawnMediumFlag = false;
	bool affectiveSpawnHardFlag = false;


	void Awake()
	{
		if(UserManager.lang.Equals(UserManager.LanguageOption._English))
		{
			alertMessage = alertMessageEng;
		}

		else if(UserManager.lang.Equals(UserManager.LanguageOption._Polish))
		{
			alertMessage = alertMessagePl;
		}
	}

	void Update ()
	{
		Spawn();
	}


    void Spawn ()
    {
		if (usedLvlFour)
		{
			timerLvlFour += Time.deltaTime;
		}
		else if (usedLvlThree)
		{
			timerLvlThree += Time.deltaTime;
		}
		else if (usedLvlTwo)
		{
			timerLvlTwo += Time.deltaTime;
		}


        if(playerHealth.currentHealth <= 0f || mrNightmareHealth.currentHealth > healthLvlOne || mrNightmareHealth.currentHealth <= 0f)
        {
            return;
        }

		else if (usedLvlFour && timerLvlFour > (hellpWaitLvlFour + 1f) && !affectiveSpawnHardFlag)
		{
			affectiveSpawnHardFlag = true;
			affectiveEnemyManager.invokeTime = 0f;
			affectiveEnemyManager.affectiveSpawnTimeHard = (int)Time.timeSinceLevelLoad;
			affectiveEnemyManager.affectiveSpawnTimeHardMax = (int)Time.timeSinceLevelLoad + 20;
		}

		else if (mrNightmareHealth.currentHealth <= healthLvlFour && !usedLvlFour)
		{
			usedLvlFour = true;
			StartCoroutine(CallForHellp(hellpNumberLvlFour, hellpWaitLvlFour));

			LogManager.logManager.AddEvent(Time.time, "Alert;Show;Time;" + alertTime + ";Content;" + alertMessage.Replace("\n", ""));
			StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessage));
		}

		else if (usedLvlThree && timerLvlThree > (hellpWaitLvlThree + 1f) && !enemySpeedFlag)
		{
			enemySpeedFlag = true;
			// playerHealth.enemyMovementChangeTime = Time.timeSinceLevelLoad; //?
		}

		else if (mrNightmareHealth.currentHealth <= healthLvlThree && !usedLvlThree)
		{
			usedLvlThree = true;
			timerLvlThree = 0f;
			StartCoroutine(CallForHellp(hellpNumberLvlThree, hellpWaitLvlThree));

			LogManager.logManager.AddEvent(Time.time, "Alert;Show;Time;" + alertTime + ";Content;" + alertMessage.Replace("\n", ""));
			StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessage));
		}

		else if (usedLvlTwo && timerLvlTwo > (hellpWaitLvlTwo + 1f) && !affectiveSpawnMediumFlag)
		{
			affectiveSpawnMediumFlag = true;
			affectiveEnemyManager.invokeTime = 0f;
			affectiveEnemyManager.affectiveSpawnTimeMedium = (int)Time.timeSinceLevelLoad;
			affectiveEnemyManager.affectiveSpawnTimeMediumMax = (int)Time.timeSinceLevelLoad + 20;
		}

		else if (mrNightmareHealth.currentHealth <= healthLvlTwo && !usedLvlTwo)
		{
			usedLvlTwo = true;
			StartCoroutine(CallForHellp(hellpNumberLvlTwo, hellpWaitLvlTwo));

			LogManager.logManager.AddEvent(Time.time, "Alert;Show;Time;" + alertTime + ";Content;" + alertMessage.Replace("\n", ""));
			StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessage));
		}

		else if (mrNightmareHealth.currentHealth <= healthLvlOne && !usedLvlOne)
		{
			usedLvlOne = true;
			StartCoroutine(CallForHellp(hellpNumberLvlOne, hellpWaitLvlOne));

			LogManager.logManager.AddEvent(Time.time, "Alert;Show;Time;" + alertTime + ";Content;" + alertMessage.Replace("\n", ""));
			StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessage));
		}
	}

	IEnumerator CallForHellp(int number, float waitTime)
    {
        int i = 0;
        while(i < number)
        {
			i++;
	        int enemyIndex = Random.Range (0, enemy.Length);
			int spawnPointIndex = Random.Range (0, spawnPoints.Length);
            
			Instantiate (enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
			LogManager.logManager.AddEvent(Time.time, "Enemy;Spawn;Type;" + enemyIndex + ";ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[spawnPointIndex].name + ";Mechanic;" + "MrNightmareSpawn");

			yield return new WaitForSeconds(waitTime);
        }
    }
}