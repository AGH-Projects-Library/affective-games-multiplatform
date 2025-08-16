using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int startingHealth = 100;
    public int currentHealth;
    [SerializeField] private float sinkSpeed = 0.07f;
    [SerializeField] private int scoreValue = 10;
    public AudioClip deathClip;


    Animator anim;
    AudioSource enemyAudio;
    ParticleSystem hitParticles;
    CapsuleCollider capsuleCollider;
    private bool isDead;
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


    private void Update() { if (isSinking) transform.Translate(-Vector3.up * sinkSpeed * Time.deltaTime); }

    private bool IsDead() => isDead;

    public void TakeDamageSuper (int amount)
    {
        if(isDead)
            return;

        enemyAudio.Play ();

        currentHealth -= amount;

        if(currentHealth <= 0 && !IsDead())
        {
            LogManager.Instance.AddEvent(Time.time, "Enemy;Death;By;SuperPower;ID;" + gameObject.GetInstanceID() + ";PositionX;" + gameObject.transform.position.x + ";PositionY;" + gameObject.transform.position.y + ";PositionZ;" + gameObject.transform.position.z + ";RotationX;" + gameObject.transform.rotation.x + ";RotationY;" + gameObject.transform.rotation.y + ";RotationZ;" + gameObject.transform.rotation.z + ";RotationW;" + gameObject.transform.rotation.w);
            TypicalDeath ();
            AddScore();
        }
    }

    public void TakeDamageLevelEnd (int amount)
    {
        if(isDead)
            return;

        // enemyAudio.Play (); // F2

        currentHealth -= amount;

        if(currentHealth <= 0 && !IsDead())
        {
            LogManager.Instance.AddEvent(Time.time, "Enemy;Death;By;LevelEnd;ID;" + gameObject.GetInstanceID() + ";PositionX;" + gameObject.transform.position.x + ";PositionY;" + gameObject.transform.position.y + ";PositionZ;" + gameObject.transform.position.z + ";RotationX;" + gameObject.transform.rotation.x + ";RotationY;" + gameObject.transform.rotation.y + ";RotationZ;" + gameObject.transform.rotation.z + ";RotationW;" + gameObject.transform.rotation.w);
            Death ();
            // AddScore();
        }
    }


    private bool CanTakeDamage() => !IsDead();

    public void TakeDamage (int amount, Vector3 hitPoint)
    {
        if(isDead)
            return;

        enemyAudio.Play ();

        currentHealth -= amount;

        LogManager.Instance.AddEvent(Time.time, "Enemy;Health;DecreaseTo;" + currentHealth + ";ID;" + gameObject.GetInstanceID());
            
        hitParticles.transform.position = hitPoint;
        hitParticles.Play();

        if(currentHealth <= 0)
        {
            LogManager.Instance.AddEvent(Time.time, "Enemy;Death;By;Gun;ID;" + gameObject.GetInstanceID() +  ";PositionX;" + gameObject.transform.position.x + ";PositionY;" + gameObject.transform.position.y + ";PositionZ;" + gameObject.transform.position.z + ";RotationX;" + gameObject.transform.rotation.x + ";RotationY;" + gameObject.transform.rotation.y + ";RotationZ;" + gameObject.transform.rotation.z + ";RotationW;" + gameObject.transform.rotation.w);
            TypicalDeath ();
            AddScore();
        }
    }

    private void TypicalDeath()
    {
        isDead = true;

        anim.SetTrigger ("Dead");

        enemyAudio.clip = deathClip;
        enemyAudio.Play ();
    }


    private void Death()
    {
        isDead = true;

        anim.SetTrigger ("Dead");

        enemyAudio.clip = deathClip;
    }


    private void StartSinking()
    {
        GetComponent <UnityEngine.AI.NavMeshAgent> ().enabled = false;
        GetComponent <Rigidbody> ().isKinematic = true;
        isSinking = true;
        
        capsuleCollider.isTrigger = true;

        if (gameObject.tag.Equals("Enemy"))
        {
            Destroy (gameObject, destroyTime);
        }

        Destroy (gameObject, destroyTimeMax);
    }

    private void AddScore()
    {
        ScoreManager.score += scoreValue;
        LogManager.Instance.AddEvent(Time.time, "Score;Update;Value;" + scoreValue);
    }
}
