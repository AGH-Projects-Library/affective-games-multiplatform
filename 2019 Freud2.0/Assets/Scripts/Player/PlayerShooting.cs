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
    public float ultraPower = 100f;
    public float ultraPowerMax = 100f;

    public Slider powerSlider;
    public Image superpowerImage;

    // Configurable input binding for shooting
    [SerializeField] private string shootButtonR2 = "R2";

    float timer = 0f;
    Ray shootRay = new Ray();
    RaycastHit shootHit;
    int shootableMask;
    ParticleSystem gunParticles;
    LineRenderer gunLine;
    AudioSource gunAudio;
    float effectsDisplayTime = 0.2f;
    
    private float timePressed = 0f;
    [SerializeField] private float timePressedMax = 5f;
    private bool isButtonPressed = false;

    [SerializeField] private float regenerationTime = 10f;

    float timeAlpha = 0f;
    float duration = 10000000; //lerp
    float smoothness = 0.02f; //lerp

    [SerializeField] private float ultraPowerRegeneration = 5f;
    [SerializeField] private float rangeRegeneration = 10f;
    [SerializeField] private int damagePerShotRegeneration = 1;

    int currentSceneNumber;
    [SerializeField] private int secondLevelIndex = 5;

    void Awake ()
    {
		currentSceneNumber = SceneManager.GetActiveScene().buildIndex;

        shootableMask = LayerMask.GetMask ("Shootable");
        gunParticles = GetComponent<ParticleSystem> ();
        gunLine = GetComponent <LineRenderer> ();
        gunAudio = GetComponent<AudioSource> ();
        powerSlider.value = ultraPower;
        
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

		if(IsShootButtonPressed() && CanShoot() && Time.timeScale != 0)
        {
            LogManager.logManager.AddEvent(Time.time, "Key;" + "R2");
            Shoot ();
        }

        if(timer >= timeBetweenBullets * effectsDisplayTime)
        {
            DisableGunEffects ();
        }

        if(regenerationTimer > regenerationTime && ultraPower < ultraPowerMax)
        {
            ultraPower += ultraPowerRegeneration;
            if(ultraPower > ultraPowerMax)
            {
                ultraPower = ultraPowerMax;
            }
            UpdatePowerSlider();
            LogManager.logManager.AddEvent(Time.time, "Slider;SuperPower;IncreaseTo;" + ultraPower);
            damagePerShot += damagePerShotRegeneration;
            if(damagePerShot > damagePerShotMax)
            {
                damagePerShot = damagePerShotMax;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;DamagePerShot;IncreaseTo;" + damagePerShot);

            range += rangeRegeneration;
            if(range > rangeMax)
            {
                range = rangeMax;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;Range;IncreaseTo;" + range);

            regenerationTimer = 0f;
        }

        if (IsInSecondLevel())
        {
            UpdateButtonPressedTimer();
            UseSuperPower();
            isButtonPressed = false;
        }
    }

    private bool IsShootButtonPressed()
    {
        return Input.GetButton(shootButtonR2);
    }

    private bool IsSuperPowerButtonPressed(float timeSuper, float timeMax)
    {
        return timeSuper > 0 && isButtonPressed;
    }

    private void UseSuperPower()
    {
        if(IsSuperPowerButtonPressed(timePressed, timePressedMax) && ultraPower > 0)
        {
            LogManager.logManager.AddEvent(Time.time, "Player;SuperPower;Time;" + timePressed);

            float ultraPowerused = ultraPower * (Math.Min(timePressed, timePressedMax) / timePressedMax);
            ultraPower -= ultraPowerused;
            UpdatePowerSlider();
            LogManager.logManager.AddEvent(Time.time, "Slider;SuperPower;DecreaseTo;" + ultraPower);

            damagePerShot -= damagePerShotRegeneration;
            if(damagePerShot < damagePerShotMin)
            {
                damagePerShot = damagePerShotMin;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;DamagePerShot;DecreaseTo;" + damagePerShot);

            range -= rangeRegeneration;
            if(range < rangeMin)
            {
                range = rangeMin;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;Range;DecreaseTo;" + range);
            
            timeAlpha = timePressed;
            regenerationTimer = 0;

            KillEnemies();
        }

        if(timeAlpha > 0)
        {
            float alpha = Math.Min(timeAlpha, timePressedMax) / timePressedMax;
            LogManager.logManager.AddEvent(Time.time, "Player;SuperPower;Alpha;" + alpha);
            Color flashColour = new Color(1f, 0.588235f, 0f, alpha);
            superpowerImage.color = flashColour;
            StartCoroutine(LerpColor());
        }

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

    private void UpdateButtonPressedTimer()
    {
        if(Input.GetButtonDown("L2"))
        {
            timePressed = Time.time;
        }
         
        if(Input.GetButtonUp("L2") && !isButtonPressed)
        {
            timePressed = Time.time - timePressed;
            isButtonPressed = true;
            LogManager.logManager.AddEvent(Time.time, "Key;L2;Time;" + timePressed);
        }
    }

    private void KillEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int killed = UnityEngine.Random.Range(0, Math.Min(2 * (int)(ultraPower / ultraPowerMax * 100), enemies.Length));
        LogManager.logManager.AddEvent(Time.time, "Player;SuperPower;Killed;" + killed);

        for(int i = 0; i < killed; i++)
        {
            GameObject enemy = enemies[i];
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (i > 4)
            {
                enemyHealth.AddScore();
                enemyHealth.TakeDamageLvlEnd(enemyHealth.currentHealth);
            }
            else
            {
                enemyHealth.TakeDamageSuper(enemyHealth.currentHealth);
            }
        }
    }
    
    private void UpdatePowerSlider()
    {
        powerSlider.value = ultraPower;
    }
    
    private void DisableGunEffects()
    {
        gunLine.enabled = false;
    }

    private bool CanShoot()
    {
        return timer >= timeBetweenBullets;
    }

    private bool IsInSecondLevel()
    {
        return currentSceneNumber >= secondLevelIndex;
    }


    void Shoot ()
    {
        timer = 0f;

        gunAudio.Play ();

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
