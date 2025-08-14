using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class PlayerShooting : MonoBehaviour
{
    public int damagePerShot = 20;
    public int damagePerShotMin = 10;
    public int damagePerShotMax = 20;
    public float timeBetweenBullets = 0.2f;
    public float timeBetweenBulletsMin = 0.2f;
    public float timeBetweenBulletsMax = 0.8f;
    public float range = 100f;
    public float rangeMin = 40f;
    public float rangeMax = 100f;
    public float ultraPower = 100;
    public float ultraPowerMax = 100;

    public GameObject enemy;
    public GameObject[] enemies;
    public Slider powerSlider;
    public Image superpowerImage;

    // Configurable input binding for shooting
    public string shootButtonR2 = "R2";

    float timer = 0f;
    Ray shootRay = new Ray();
    RaycastHit shootHit;
    int shootableMask;
    ParticleSystem gunParticles;
    LineRenderer gunLine;
    AudioSource gunAudio;
    Light gunLight;
    float effectsDisplayTime = 0.2f;
    
    public float timePressed = 0f;
    float timePressedMax = 5f;
    bool wasPressed = false;

    // F2
    // float timeMuscle = 0f;
    // float timeMuscleSum = 0f;
    // float timeMuscleMax = 5f;
    // bool wasMuscle = false;

    float regenerationTimer = 0f;
    float regenerationTime = 10f;

    bool superpowered = false;

    float timeAlpha = 0f;
    float duration = 10000000; //lerp
    float smoothness = 0.02f; //lerp

    float ultraPowerRegeneration = 5f;
    float rangeRenegeration = 10f;
    float timeBetweenBulletsRegeneration = 0.1f;
    int damagePerShotRegeneration = 1;

    // F2
    // bool bitalinoUseFlag;

    int currentSceneNumber;
    int secondLevelIndex = 5;

    // F2
    // public float EMGScale = 1.5f;
    // public float timeMuscleSumMin = 0.3f;

    void Awake ()
    {
		currentSceneNumber = SceneManager.GetActiveScene().buildIndex;

        shootableMask = LayerMask.GetMask ("Shootable");
        gunParticles = GetComponent<ParticleSystem> ();
        gunLine = GetComponent <LineRenderer> ();
        gunAudio = GetComponent<AudioSource> ();
        gunLight = GetComponent<Light> ();
        powerSlider.value = ultraPower;

        // bitalinoUseFlag = BitalinoController.bitalinoController.bitalinoUse;

        LogManager.logManager.AddEvent(Time.time, "Player;Range;StartingValue;" + range);
        LogManager.logManager.AddEvent(Time.time, "Player;TimeBetweenTwoBullets;StartingValue;" + timeBetweenBullets);
        LogManager.logManager.AddEvent(Time.time, "Player;DamagePerShot;StartingValue;" + damagePerShot);
        LogManager.logManager.AddEvent(Time.time, "Player;SuperPower;StartingValue;" + ultraPower);
        LogManager.logManager.AddEvent(Time.time, "Slider;SuperPower;StartingValue;" + ultraPower);
    }


    void Update ()
    {
        timer += Time.deltaTime;
        regenerationTimer += Time.deltaTime;

		if(Input.GetButton(shootButtonR2) && timer >= timeBetweenBullets && Time.timeScale != 0) // configurable shoot button
        {
            LogManager.logManager.AddEvent(Time.time, "Key;" + "R2");
            Shoot ();
        }

        if(timer >= timeBetweenBullets * effectsDisplayTime)
        {
            DisableEffects ();
        }

        if(regenerationTimer > regenerationTime && ultraPower < ultraPowerMax)
        {
            ultraPower += ultraPowerRegeneration;
            if(ultraPower > ultraPowerMax)
            {
                ultraPower = ultraPowerMax;
            }
            powerSlider.value = ultraPower;
            LogManager.logManager.AddEvent(Time.time, "Slider;SuperPower;IncreaseTo;" + ultraPower);

            timeBetweenBullets -= timeBetweenBulletsRegeneration;
            if(timeBetweenBullets < timeBetweenBulletsMin)
            {
                timeBetweenBullets = timeBetweenBulletsMin;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;TimeBetweenTwoBullets;DecreaseTo;" + timeBetweenBullets);

            damagePerShot += damagePerShotRegeneration;
            if(damagePerShot > damagePerShotMax)
            {
                damagePerShot = damagePerShotMax;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;DamagePerShot;IncreaseTo;" + damagePerShot);

            range += rangeRenegeration;
            if(range > rangeMax)
            {
                range = rangeMax;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;Range;IncreaseTo;" + range);

            regenerationTimer = 0f;
        }

        if (currentSceneNumber >= secondLevelIndex)
        {
            // F2
            // if (bitalinoUseFlag)
            // {
            //     MuscleStrainTimer();
            //     UseSuperPower(timeMuscle, timeMuscleMax, wasMuscle);
            //     timeMuscle = 0f;
            //     wasMuscle = false; 
            // }

            // else
            // {
                KeyPressedTimer();
                UseSuperPower(timePressed, timePressedMax, wasPressed);
                // timePressed = 0; // why is working without it and not working with it?
                wasPressed = false;
            // }
        }
    }

    void UseSuperPower(float timeSuper, float timeMax, bool was)
    {
        if((timeSuper > 0 && was)  && ultraPower > 0)
        {
            LogManager.logManager.AddEvent(Time.time, "Player;SuperPower;Time;" + timeSuper);

            float ultraPowerused = ultraPower * (Math.Min(timeSuper, timeMax) / timeMax);
            ultraPower -= ultraPowerused;
            powerSlider.value = ultraPower;
            LogManager.logManager.AddEvent(Time.time, "Slider;SuperPower;DecreaseTo;" + ultraPower);

            timeBetweenBullets += timeBetweenBulletsRegeneration;
            if(timeBetweenBullets > timeBetweenBulletsMax)
            {
                timeBetweenBullets = timeBetweenBulletsMax;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;TimeBetweenTwoBullets;IncreaseTo;" + timeBetweenBullets);

            damagePerShot -= damagePerShotRegeneration;
            if(damagePerShot < damagePerShotMin)
            {
                damagePerShot = damagePerShotMin;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;DamagePerShot;DecreaseTo;" + damagePerShot);

            range -= rangeRenegeration;
            if(range < rangeMin)
            {
                range = rangeMin;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;Range;DecreaseTo;" + range);
            
            timeAlpha = timeSuper;
            regenerationTimer = 0;

            timeSuper = 0;
            was = false;

            enemies = GameObject.FindGameObjectsWithTag("Enemy");

            int killed = UnityEngine.Random.Range (0, Math.Min(2 * (int)ultraPowerused, enemies.Length));

            LogManager.logManager.AddEvent(Time.time, "Player;SuperPower;Killed;" + killed);

            for(int i = 0; i < killed; i++)
            {
                enemy = enemies[i];
                EnemyHealth enemyHealth = enemy.GetComponent <EnemyHealth> ();
                if (i > 4)
                {
                    enemyHealth.AddScore();
                    enemyHealth.TakeDamageLvlEnd(enemyHealth.currentHealth);
                }

                else{
                    enemyHealth.TakeDamageSuper(enemyHealth.currentHealth);
                }

            }

            superpowered = true;

        }

        if(superpowered)
        {
            float alpha = Math.Min(timeAlpha, timeMax)/timeMax;
            LogManager.logManager.AddEvent(Time.time, "Player;SuperPower;Alpha;" + alpha);
            Color flashColour = new Color(1f, 0.588235f, 0f, alpha);
            superpowerImage.color = flashColour;
            StartCoroutine(LerpColor());
        }
        superpowered = false;

    }

    IEnumerator LerpColor()
    {
        float progress = 0; 
        float increment = smoothness / duration;
        while(progress < 1)
        {
            superpowerImage.color = Color.Lerp(superpowerImage.color, Color.clear, progress);
            progress += increment;
            yield return new WaitForSeconds(smoothness);
        }

    }

    void KeyPressedTimer()
    {
        if(Input.GetButtonDown("L2"))
        {
            timePressed = Time.time;
        }
         
        if(Input.GetButtonUp("L2") && !wasPressed)
        {
            timePressed = Time.time - timePressed;
            wasPressed = true;
            LogManager.logManager.AddEvent(Time.time, "Key;L2;Time;" + timePressed);
        }
    }

    // F2
    // void MuscleStrainTimer()
    // {
    //     if(BitalinoController.bitalinoController.EMGAverage > EMGScale * BitalinoController.bitalinoController.EMGCalibrated)
    //     {
    //         timeMuscleSum += Time.deltaTime;
    //     }
    //     else if(timeMuscleSum > timeMuscleSumMin)
    //     {
    //         timeMuscle = timeMuscleSum;
    //         wasMuscle = true;
    //         timeMuscleSum = 0f;
    //     }

    // }

    public void DisableEffects ()
    {
        gunLine.enabled = false;
        gunLight.enabled = false;
    }


    void Shoot ()
    {
        timer = 0f;

        gunAudio.Play ();

        gunLight.enabled = true;

        gunParticles.Stop ();
        gunParticles.Play ();

        gunLine.enabled = true;
        gunLine.SetPosition (0, transform.position);

        shootRay.origin = transform.position;
        shootRay.direction = transform.forward;

        if(Physics.Raycast (shootRay, out shootHit, range, shootableMask))
        {
            EnemyHealth enemyHealth = shootHit.collider.GetComponent <EnemyHealth> ();
            if(enemyHealth != null)
            {
                enemyHealth.TakeDamage (damagePerShot, shootHit.point);
            }
            gunLine.SetPosition (1, shootHit.point);
        }
        else
        {
            gunLine.SetPosition (1, shootRay.origin + shootRay.direction * range);
        }
    }
}
