using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] public int maxHealth = 100;
    [SerializeField] public int healthRegeneration = 5;
    [SerializeField] public float regenerationTime = 10f;
    public Slider healthSlider;
    [SerializeField] public Image damageImage;
    public AudioClip deathClip;
    public float flashSpeed = 5f;
    public Color flashColour = new Color(1f, 0f, 0f, 0.1f);

    public Animator anim;
    public AudioSource playerAudio;
    [SerializeField] private PlayerMovement playerMovement;
    public PlayerShooting playerShooting;
    public bool isDead;
    public bool damaged;

    public float timer;
    public int currentHealth;

    void Awake ()
    {
        timer = 0f;
        playerAudio = GetComponent<AudioSource>();
        playerMovement = GetComponent<PlayerMovement>();
        playerShooting = GetComponentInChildren<PlayerShooting>();
        currentHealth = maxHealth;
        LogManager.Instance.AddEvent(Time.time, "Slider;Health;StartingValue;" + currentHealth);
    }

    void Update ()
    {
        if (IsEscapePressed)
        {
            LogManager.Instance.AddEvent(Time.time, "Key;Escape");
            Application.Quit(); // ignored in UnityEditor
        }

        timer += Time.deltaTime;
        if (CanRegenHealth)
        {
            currentHealth += healthRegeneration;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            UpdateHealthSlider();

            LogManager.Instance.AddEvent(Time.time, "Player;Health;IncreaseTo;" + currentHealth);
            LogManager.Instance.AddEvent(Time.time, "Slider;Health;DecreaseTo;" + currentHealth);

            timer = 0f;
        }

        if (IsDamaged)
        {
            damageImage.color = flashColour;
        }
        else
        {
            damageImage.color = Color.Lerp(damageImage.color, Color.clear, flashSpeed * Time.deltaTime);
        }
        SetDamaged(false);

        if (IsDead)
        {
            DisablePlayerMovementAndShooting();

            if (Input.GetKeyDown(KeyCode.R))
            {
                LogManager.Instance.AddEvent(Time.time, "Key;R");
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1); // load tutorial of current level
            }

            else if (Input.GetKeyDown(KeyCode.C))
            {
                LogManager.Instance.AddEvent(Time.time, "Key;C");
                Application.Quit(); // ignored in UnityEditor
            }
        }
    }

    private bool IsEscapePressed => Input.GetKey(KeyCode.Escape);
    private bool CanRegenHealth => timer > regenerationTime && currentHealth < maxHealth;
    private bool IsDamaged => damaged;
    private bool IsDead => currentHealth <= 0;

    public void TakeDamage(int amount, int i)
    {
        ResetTimer();
        SetDamaged(true);
        currentHealth -= amount;
        LogManager.Instance.AddEvent(Time.time, $"Player;Health;DecreaseTo;{currentHealth};By;Enemy;ID;{i}");
        UpdateHealthSlider();

        if (IsDead)
        {
            Death();
        }
    }

    void ResetTimer() => timer = 0f;

    void SetDamaged(bool value) => damaged = value;

    void UpdateHealthSlider() => healthSlider.value = currentHealth;

    void Death()
    {
        LogManager.Instance.AddEvent(Time.time, $"Player;Death;PositionX;{gameObject.transform.position.x};PositionY;{gameObject.transform.position.y};PositionZ;{gameObject.transform.position.z};RotationX;{gameObject.transform.rotation.x};RotationY;{gameObject.transform.rotation.y};RotationZ;{gameObject.transform.rotation.z};RotationW;{gameObject.transform.rotation.w}");
        
        isDead = true;
        // playerShooting.DisableEffects();
        anim.SetTrigger("Die");
        playerAudio.clip = deathClip;
        playerAudio.Play();
        DisablePlayerMovementAndShooting();
    }

    void DisablePlayerMovementAndShooting()
    {
        playerMovement.enabled = false;
        playerShooting.enabled = false;
    }
}
