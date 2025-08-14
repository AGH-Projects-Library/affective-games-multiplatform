using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEditor;

public class PlayerHealth : MonoBehaviour
{
    public int startingHealth = 100;
    public int currentHealth;
    public int healthRegeneration = 5;
    public float regenerationTime = 10f;
    public Slider healthSlider;
    public Image damageImage;
    public AudioClip deathClip;
    public float flashSpeed = 5f;
    public Color flashColour = new Color(1f, 0f, 0f, 0.1f);
    // Key mapping for inspector-based customization
    public KeyCode keyEscape = KeyCode.Escape;


    Animator anim;
    AudioSource playerAudio;
    PlayerMovement playerMovement;
    PlayerShooting playerShooting;
    public bool isDead;
    bool damaged;

    float timer;

    int disguisedHealth;


    //enemy movement

    // F2
    // string tagEnemy = "Enemy";
    
    // public float enemyMovementChangeTime = 90f;

    // F2
    // float remainTime = 2f;
    // float timerMovement = 0f;

    // F2
    // float speedReduced = 2f;
    // float speedBase = 3f;

    // F2
    // float accSpeed = 0.03f;
    // float speed = 0f;

    // F2
    // float scaleLess = 1.5f;
    // float scaleMore = 1.3f;
    // float HRAv;
    // float HRMax;
    // float HRMin;

    // F2
    // bool used = false;
    // float alertTime = 5f;
    // string alertMessageAffectivePl = "Potwory wyczuwają Twoje emocje!\nDososowują swoją prędkość!\nUważaj!";
    // string alertMessageNonAffectivePl = "Potwory mają Cię dość!\nBędą poruszać się szybciej!\nUważaj!";
    // string alertMessageAffectiveEng = "Monsters feel your emotions!\nThey are adjusting their speed!\nWatch out!";
    // string alertMessageNonAffectiveEng = "Monsters are sick of you!\nThey are going to move faster!\nWatch out!";
    // string alertMessageAffective = "";
    // string alertMessageNonAffective = "";

    // int zeroLevel = 2; // F2

    // bool bitalonoUseFlag;     // F2

    void Awake ()
    {
        // F2
        // if (UserManager.lang.Equals(UserManager.LanguageOption._English))
        // {
        //     alertMessageAffective = alertMessageAffectiveEng;
        //     alertMessageNonAffective = alertMessageNonAffectiveEng;
        // }
        // else if (UserManager.lang.Equals(UserManager.LanguageOption._Polish))
        // {
        //     alertMessageAffective = alertMessageAffectivePl;
        //     alertMessageNonAffective = alertMessageNonAffectivePl;
        // }


        // F2
        // bitalonoUseFlag = BitalinoController.bitalinoController.bitalinoUse;
        // speed = speedBase;
        
        timer = 0f;
        anim = GetComponent <Animator> ();
        playerAudio = GetComponent <AudioSource> ();
        playerMovement = GetComponent <PlayerMovement> ();
        playerShooting = GetComponentInChildren <PlayerShooting> ();
        currentHealth = startingHealth;
        disguisedHealth = startingHealth;
        LogManager.logManager.AddEvent(Time.time, "Player;Health;StartingValue;" + currentHealth);
        LogManager.logManager.AddEvent(Time.time, "Slider;Health;StartingValue;" + currentHealth);
    }

    void Update ()
    {


        if (Input.GetKey(keyEscape))
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
			// EditorApplication.isPlaying = false;
		}

        // F2
        // if (SceneManager.GetActiveScene().buildIndex > zeroLevel)
        // {
        //     SetEnemyMovement();
        // }

        timer += Time.deltaTime;
        if (timer > regenerationTime && currentHealth < startingHealth)
        {
            currentHealth += healthRegeneration;
            if(currentHealth > startingHealth)
            {
                currentHealth = startingHealth;
            }
            healthSlider.value  = currentHealth;

            LogManager.logManager.AddEvent(Time.time, "Player;Health;IncreaseTo;" + currentHealth);
            LogManager.logManager.AddEvent(Time.time, "Slider;Health;DecreaseTo;" + currentHealth);

            // F2
            // changeHealth(currentHealth);
            
            timer = 0f;
        }

        if(damaged)
        {
            damageImage.color = flashColour;
        }
        else
        {
            damageImage.color = Color.Lerp (damageImage.color, Color.clear, flashSpeed * Time.deltaTime);
        }
        damaged = false;

        if(isDead)
        {
            playerMovement.enabled = false; //for third lvl, should add more here

            if(Input.GetKeyDown(KeyCode.R))
            {
                LogManager.logManager.AddEvent(Time.time, "Key;R");
                SceneManager.LoadScene (SceneManager.GetActiveScene().buildIndex - 1); // load tutorial of current level
            }

            else if (Input.GetKeyDown(KeyCode.C))
            {
                LogManager.logManager.AddEvent(Time.time, "Key;C");
                Application.Quit(); // ignored in UnityEditor
                // EditorApplication.isPlaying = false;
            }
        } 

    }

    // F2
    // void SetEnemyMovement()
    // {
    //     if (Time.timeSinceLevelLoad > (enemyMovementChangeTime - remainTime))
    //     {
    //         timerMovement += Time.deltaTime;
    //     }

    //     if ((Time.timeSinceLevelLoad > (enemyMovementChangeTime - remainTime)) && (timerMovement > remainTime) && (currentHealth > 0) && bitalonoUseFlag)
    //     {
    //         if (!used)
    //         {
    //             LogManager.logManager.AddEvent(Time.time, "Alert;Enemy;SpeedAdjust");
    //             StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessageAffective));
    //             used = true;
    //         }

    //         HRAv = BitalinoController.bitalinoController.HRAverage;
    //         HRMax = BitalinoController.bitalinoController.HRMin;
    //         HRMin = BitalinoController.bitalinoController.HRMax;

    //         float k = 0.3f;
    //         int L = 5;
    //         int yScale = 2; 
            
    //         speed = yScale + L / (1 + Mathf.Exp(k * (HRAv - ((HRMax + HRMin) / 2)))); // belongs to [2, 7]
    //         LogManager.logManager.AddEvent(Time.time, "Enemy;Speed;ChangeTo;" + speed);
    //         GameObject [] enemies = GameObject.FindGameObjectsWithTag(tagEnemy);
	// 	    foreach (GameObject enemy in enemies)
	// 	    {
    //             enemy.GetComponent<EnemyMovement>().SetSpeed(speed);
    //         }

    //         timerMovement = 0f;
    //     }

    //     else if ((Time.timeSinceLevelLoad > (enemyMovementChangeTime - remainTime)) && (timerMovement > remainTime) && (currentHealth > 0) && !bitalonoUseFlag)
    //     {
    //         if (!used)
    //         {
    //             LogManager.logManager.AddEvent(Time.time, "Alert;Enemy;SpeedChange");
    //             StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessageNonAffective));
    //             used = true;
    //         }

    //         speed += accSpeed;
    //         LogManager.logManager.AddEvent(Time.time, "Enemy;Speed;ChangeTo;" + speed);
    //         GameObject [] enemies = GameObject.FindGameObjectsWithTag(tagEnemy);
	// 	    foreach (GameObject enemy in enemies)
	// 	    {
    //             enemy.GetComponent<EnemyMovement>().SetSpeed(speed);
    //         }

    //         timerMovement = 0f;
    //     }
    // }


    public void TakeDamage (int amount, int i)
    {
        timer = 0f;

        damaged = true;

        currentHealth -= amount;
        healthSlider.value = currentHealth;
        
        LogManager.logManager.AddEvent(Time.time, "Player;Health;DecreaseTo;" + currentHealth + ";By;Enemy;ID;" + i);
        LogManager.logManager.AddEvent(Time.time, "Slider;Health;DecreaseTo;" + currentHealth);

        // F2
        // changeHealth(currentHealth);

        playerAudio.Play ();

        if(currentHealth <= 0 && !isDead)
        {
            Death ();
        }
    }

    // F2
    // void changeHealth (int currentHealth)
    // {
    //     int previousHealth = disguisedHealth;

    //     if(currentHealth >= (4 * startingHealth / 5))
    //     {
    //         disguisedHealth = 5 * startingHealth / 5;
    //     }
        
    //     else if(currentHealth >= (3 * startingHealth / 5))
    //     {
    //         disguisedHealth = (4 * startingHealth / 5);
    //     }

    //     else if(currentHealth >= (2 * startingHealth / 5))
    //     {
    //         disguisedHealth = (3 * startingHealth / 5);   
    //     }

    //     else if(currentHealth >= (1 * startingHealth / 5))
    //     {
    //         disguisedHealth = (2 * startingHealth / 5);   
    //     }

    //     else
    //     {
    //         disguisedHealth = (1 * startingHealth / 5);
    //     }

    //     healthSlider.value = disguisedHealth;
        
    //     if (disguisedHealth > previousHealth)
    //     {
    //         LogManager.logManager.AddEvent(Time.time, "Slider;Health;IncreaseTo;" + disguisedHealth);
    //     }
    //     else if (disguisedHealth < previousHealth)
    //     {
    //         LogManager.logManager.AddEvent(Time.time, "Slider;Health;DecreaseTo;" + disguisedHealth);
    //     }
    // }


    void Death ()
    {
        LogManager.logManager.AddEvent(Time.time, "Player;Death;" +  "PositionX;" + gameObject.transform.position.x + ";PositionY;" + gameObject.transform.position.y + ";PositionZ;" + gameObject.transform.position.z + ";RotationX;" + gameObject.transform.rotation.x + ";RotationY;" + gameObject.transform.rotation.y + ";RotationZ;" + gameObject.transform.rotation.z + ";RotationW;" + gameObject.transform.rotation.w);
        
        isDead = true;

        playerShooting.DisableEffects ();

        anim.SetTrigger ("Die");

        playerAudio.clip = deathClip;
        playerAudio.Play ();

        playerMovement.enabled = false;
        playerShooting.enabled = false;
    }
}
