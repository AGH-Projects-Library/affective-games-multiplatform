using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public interface IPlayerState { void Enter(PlayerHealth ctx); void Tick(PlayerHealth ctx); } // minimal

public class PlayerAliveState : IPlayerState
{
    public void Enter(PlayerHealth ctx){ ctx.isDead=false; }
    public void Tick(PlayerHealth ctx)
    {
        if (ctx.timer>ctx.regenerationTime && ctx.currentHealth<ctx.maxHealth){ ctx.currentHealth = Mathf.Min(ctx.currentHealth+ctx.healthRegeneration, ctx.maxHealth); ctx.NotifyHealth(); ctx.timer=0f; }
        if (ctx.currentHealth<=0) ctx.SwitchState(ctx.deadState);
    }
}

public class PlayerRegeneratingState : IPlayerState
{
    public void Enter(PlayerHealth ctx){} // no-op
    public void Tick(PlayerHealth ctx){ /* reserved if regen becomes its own behavior */ }
}

public class PlayerDeadState : IPlayerState
{
    public void Enter(PlayerHealth ctx)
    {
        ctx.isDead=true;
        ctx.anim.SetTrigger("Die");
        ctx.playerAudio.clip = ctx.deathClip;
        ctx.playerAudio.Play();
        ctx.playerMovement.enabled=false; ctx.playerShooting.enabled=false;
        ctx.OnDied?.Invoke();
    }
    public void Tick(PlayerHealth ctx){} // no-op
}

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] public int maxHealth = 100;
    [SerializeField] public int healthRegeneration = 5;
    [SerializeField] public float regenerationTime = 10f;
    public Slider healthSlider;
    [SerializeField] public Image damageImage;
    public AudioClip deathClip;
    public float flashSpeed = 5f;
    public Color flashColour = new Color(1f,0f,0f,0.1f);

    public Animator anim;
    public AudioSource playerAudio;
    [SerializeField] public PlayerMovement playerMovement;
    public PlayerShooting playerShooting;

    public bool isDead;
    public bool damaged;
    public float timer;
    public int currentHealth;

    public Action<int> OnHealthChanged;
    public Action OnDied;

    IPlayerState state;
    [HideInInspector] public readonly IPlayerState aliveState = new PlayerAliveState();
    [HideInInspector] public readonly IPlayerState regenState = new PlayerRegeneratingState();
    [HideInInspector] public readonly IPlayerState deadState = new PlayerDeadState();

    void Awake()
    {
        playerAudio = GetComponent<AudioSource>();
        playerMovement = GetComponent<PlayerMovement>();
        playerShooting = GetComponentInChildren<PlayerShooting>();
        currentHealth = maxHealth; timer=0f; LogManager.Log(Time.time,$"Slider;Health;StartingValue;{currentHealth}");
        SwitchState(aliveState);
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Escape)) { LogManager.Log(Time.time,"Key;Escape"); Application.Quit(); }
        timer += Time.deltaTime;
        damageImage.color = damaged ? flashColour : Color.Lerp(damageImage.color, Color.clear, flashSpeed * Time.deltaTime);
        damaged=false;
        state.Tick(this);
        if (isDead)
        {
            if (Input.GetKeyDown(KeyCode.R)) { LogManager.Log(Time.time,"Key;R"); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex-1); }
            else if (Input.GetKeyDown(KeyCode.C)) { LogManager.Log(Time.time,"Key;C"); Application.Quit(); }
        }
    }

    public void SwitchState(IPlayerState next){ state=next; state.Enter(this); }

    public void TakeDamage(int amount,int attackerId)
    {
        timer=0f; damaged=true; currentHealth-=amount;
        LogManager.Log(Time.time,$"Player;Health;DecreaseTo;{currentHealth};By;Enemy;ID;{attackerId}");
        NotifyHealth();
        if (currentHealth<=0) SwitchState(deadState);
    }

    void NotifyHealth(){ healthSlider.value=currentHealth; OnHealthChanged?.Invoke(currentHealth); } // one-liner
}
