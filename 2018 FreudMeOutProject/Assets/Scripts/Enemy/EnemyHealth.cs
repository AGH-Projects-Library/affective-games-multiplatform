using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int startingHealth = 100;
    public int currentHealth;
    public float sinkSpeed = 0.07f;
    public int scoreValue = 10;
    public AudioClip deathClip;


    Animator anim;
    AudioSource enemyAudio;
    ParticleSystem hitParticles;
    CapsuleCollider capsuleCollider;
    public bool isDead;
    bool isSinking;

    float destroyTime = 3f;
    float destroyTimeMax = 10f;

    void Awake ()
    {
        anim = GetComponent <Animator> ();
        enemyAudio = GetComponent <AudioSource> ();
        hitParticles = GetComponentInChildren <ParticleSystem> ();
        capsuleCollider = GetComponent <CapsuleCollider> ();

        currentHealth = startingHealth;
    }


    void Update ()
    {
        if (isSinking)
        {
            transform.Translate (-Vector3.up * sinkSpeed * Time.deltaTime);
        }
    }

    public void TakeDamageSuper (int amount)
    {
        if(isDead)
            return;

        enemyAudio.Play ();

        currentHealth -= amount;

        if(currentHealth <= 0)
        {
            LogManager.logManager.AddEvent(Time.time, "Enemy;DeathBy;SuperPower");
            Death ();
        }
    }

    public void TakeDamageLvlEnd (int amount)
    {
        if(isDead)
            return;

        enemyAudio.Play ();

        currentHealth -= amount;

        if(currentHealth <= 0)
        {
            LogManager.logManager.AddEvent(Time.time, "Enemy;DeathBy;LevelEnd");
            Death ();
        }
    }


    public void TakeDamage (int amount, Vector3 hitPoint)
    {
        if(isDead)
            return;

        enemyAudio.Play ();

        currentHealth -= amount;
            
        hitParticles.transform.position = hitPoint;
        hitParticles.Play();

        if(currentHealth <= 0)
        {
            LogManager.logManager.AddEvent(Time.time, "Enemy;DeathBy;Gun");
            Death ();
        }
    }


    void Death ()
    {
        isDead = true;

        anim.SetTrigger ("Dead");

        enemyAudio.clip = deathClip;
        enemyAudio.Play ();
    }


    public void StartSinking ()
    {
        GetComponent <UnityEngine.AI.NavMeshAgent> ().enabled = false;
        GetComponent <Rigidbody> ().isKinematic = true;
        isSinking = true;
        ScoreManager.score += scoreValue;
        
        capsuleCollider.isTrigger = true;

        if (gameObject.tag.Equals("Enemy"))
        {
            Destroy (gameObject, destroyTime);
        }

        Destroy (gameObject, destroyTimeMax);
    }
}
