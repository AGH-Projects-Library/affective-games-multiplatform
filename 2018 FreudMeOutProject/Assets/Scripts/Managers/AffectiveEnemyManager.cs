using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AffectiveEnemyManager : MonoBehaviour 
{

	public int affectiveSpawnTimeMedium = 60;
    public int affectiveSpawnTimeMediumMax = 80;
	public int affectiveSpawnTimeHard = 90;
    public int affectiveSpawnTimeHardMax = 180;
    public int preparationTime = 5;
    public int neccessaryTimes = 3;
	public float spawnTime = 3f;
	public float invokeTime = 5f;
    public PlayerHealth playerHealth;
    public GameObject[] enemy;
    public Transform[] spawnPoints;

    
    bool bitalinoUseFlag;
    bool boredom = false;
    bool checkedMedium = false;
    bool checkedHard = false;
    int spawnMax;

    float timerSpawnMax = 0f;
    float remainTime = 5f;

    float alertTime = 5f;
    string alertMessageAffectivePl = "Twoje emocje zwabiły potwory!\nNadchodzą posiłki! Uważaj!";
    string alertMessageNonAffectivePl = "Nadchodzą kolejne potwory!\nUważaj!";
    string alertMessageAffectiveEng = "Your emotions has lured monsters!\nThey are coming! Watch out!";
    string alertMessageNonAffectiveEng = "More and more monsters are coming!\nWatch out!";

    string alertMessageAffective = "";
    string alertMessageNonAffective = "";

    void Awake()
    {
        if(UserManager.lang.Equals(UserManager.LanguageOption._English))
		{
			alertMessageAffective = alertMessageAffectiveEng;
			alertMessageNonAffective = alertMessageNonAffectiveEng;
		}

		else if(UserManager.lang.Equals(UserManager.LanguageOption._Polish))
		{
            alertMessageAffective = alertMessageAffectivePl;
			alertMessageNonAffective = alertMessageNonAffectivePl;
		}

        bitalinoUseFlag = BitalinoController.bitalinoController.bitalinoUse;
    }


    void Start ()
    {
        InvokeRepeating ("Spawn", invokeTime, spawnTime);
    }


    void Update()
    {
        if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeMedium - remainTime))
        {
            timerSpawnMax += Time.deltaTime;
        }

        if (!bitalinoUseFlag)
        {
            if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeMedium - preparationTime) && !checkedMedium)
            {
                LogManager.logManager.AddEvent(Time.time, "Alert;NonAffectiveSpawn;Activation");
                StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessageNonAffective));
                checkedMedium = true;
            }

            else if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeHard - preparationTime) && !checkedHard)
            {
                LogManager.logManager.AddEvent(Time.time, "Alert;NonAffectiveSpawn;Activation");
                StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessageNonAffective));
                checkedHard = true;
            }
        }


        if (bitalinoUseFlag)
        {
            if((Time.timeSinceLevelLoad >= (affectiveSpawnTimeMedium - remainTime)) &&  timerSpawnMax > remainTime)
            {
                float HRAverage = BitalinoController.bitalinoController.HRAverage;
                float HRMax = BitalinoController.bitalinoController.HRMax;
                float HRMin = BitalinoController.bitalinoController.HRMin;
                float HRMean = (HRMax + HRMin) / 2;

                float EDAAverage = BitalinoController.bitalinoController.EDAAverage;
                float EDAMax = BitalinoController.bitalinoController.EDAMax;
                float EDAMin = BitalinoController.bitalinoController.EDAMin;
                float EDAMean = (EDAMax + EDAMin) / 2;

                if ((HRAverage >= HRMean) && (EDAAverage >= EDAMean))
                {
                    spawnMax = 0;
                }

                else if (((HRAverage < HRMean) && (EDAAverage >= EDAMean)) || ((HRAverage >= HRMean) && (EDAAverage < EDAMean)))
                {
                    spawnMax = 2;
                }

                else if ((HRAverage < HRMean) && (EDAAverage < EDAMean))
                {
                    spawnMax = 5;
                }
                
                LogManager.logManager.AddEvent(Time.time, "AffectiveSpawn;SpawnMax;SetTo;" + spawnMax);

                timerSpawnMax = 0f;
            }

            if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeMedium - preparationTime) && !checkedMedium)
            {
                checkedMedium = true;
                boredom = false;
                StartCoroutine(CheckBoredom(0));
            }

            else if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeHard - preparationTime) && !checkedHard)
            {
                checkedHard = true;
                boredom = false;
                StartCoroutine(CheckBoredom(1));
            }
        }   
    }


    IEnumerator CheckBoredom(int x)
    {
        int sum = 0;
        for (int i = 0; i < preparationTime; i++)
        {
            if ((BitalinoController.bitalinoController.EDAAverage < BitalinoController.bitalinoController.EDAMax) && (BitalinoController.bitalinoController.HRAverage <= ((BitalinoController.bitalinoController.HRMax + BitalinoController.bitalinoController.HRMax) / 2)))
            {
                sum++;
            }

            yield return new WaitForSeconds(1f);
        }

        if (sum >= neccessaryTimes)
        {
            boredom = true;
            checkedMedium = true;
            LogManager.logManager.AddEvent(Time.time, "Alert;AffectiveSpawn;Activation");
            LogManager.logManager.AddEvent(Time.time, "AffectiveSpawn;Active;Sum;" + sum);
            StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessageAffective));
        }

        else
        {
            LogManager.logManager.AddEvent(Time.time, "AffectiveSpawn;NotActive;Sum;" + sum);
            if (x == 0)
            {
                affectiveSpawnTimeMedium += preparationTime;
                if (affectiveSpawnTimeMedium < affectiveSpawnTimeMediumMax)
                {
                    checkedMedium = false;
                }
            }
            
            else if (x == 1)
            {
                affectiveSpawnTimeHard += preparationTime;
                if (affectiveSpawnTimeHard < affectiveSpawnTimeHardMax)
                {
                    checkedMedium = false;
                }
            }
        }
    }
    

    void Spawn ()
    {
        if(playerHealth.currentHealth <= 0f)
        {
            return;
        }

        int enemyIndex = Random.Range (0, enemy.Length);

        // without biofeedback
        if((Time.timeSinceLevelLoad >= affectiveSpawnTimeMedium) && !bitalinoUseFlag)
        {
            int spawnPointIndex = Random.Range (0, (int)(spawnPoints.Length / Random.Range (1, 3)));
            Instantiate (enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
            LogManager.logManager.AddEvent(Time.time, "Enemy;" + enemyIndex + ";NonAffectiveSpawn;AtSpawnPoint;" + spawnPointIndex);
        }

        if((Time.timeSinceLevelLoad >= affectiveSpawnTimeHard) && !bitalinoUseFlag)
        {
            int spawnPointIndex = Random.Range (0, (int)(spawnPoints.Length / Random.Range (1, 2)));
            Instantiate (enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
            LogManager.logManager.AddEvent(Time.time, "Enemy;" + enemyIndex + ";NonAffectiveSpawn;AtSpawnPoint;" + spawnPointIndex);
        }

        // with biofeedback
        if((Time.timeSinceLevelLoad >= affectiveSpawnTimeMedium) && bitalinoUseFlag)
        {
            if (boredom)
            {
                int spawnPointIndex = Random.Range (0, spawnMax);
                Instantiate (enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
                LogManager.logManager.AddEvent(Time.time, "Enemy;" + enemyIndex + ";AffectiveSpawn;AtSpawnPoint;" + spawnPointIndex);
            }
        }

        if((Time.timeSinceLevelLoad >= affectiveSpawnTimeHard) && bitalinoUseFlag)
        {
            if (boredom)
            {
                int spawnPointIndex = Random.Range (0, spawnMax);
                Instantiate (enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
                LogManager.logManager.AddEvent(Time.time, "Enemy;" + enemyIndex + ";AffectiveSpawn;AtSpawnPoint;" + spawnPointIndex);
            }
        }

    }
}