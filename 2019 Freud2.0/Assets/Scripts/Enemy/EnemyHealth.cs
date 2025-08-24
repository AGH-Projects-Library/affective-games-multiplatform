using System;
using UnityEngine;

public interface IEnemyState { void Enter(EnemyHealth ctx); void Update(EnemyHealth ctx); void TakeDamage(EnemyHealth ctx, int amount, Vector3? hitPoint, string cause); } // single-responsibility

public class EnemyAliveState : IEnemyState
{
    public void Enter(EnemyHealth ctx){ ctx.isDead=false; }
    public void Update(EnemyHealth ctx){ if (ctx.isSinking) ctx.transform.Translate(-Vector3.up * ctx.sinkSpeed * Time.deltaTime); }
    public void TakeDamage(EnemyHealth ctx,int amount,Vector3? hit,string cause)
    {
        if (ctx.isDead) return;
        ctx.enemyAudio.Play();
        ctx.currentHealth -= amount;
        ctx.OnHealthChanged?.Invoke(ctx.currentHealth);
        if (hit.HasValue){ ctx.hitParticles.transform.position = hit.Value; ctx.hitParticles.Play(); }
        LogManager.Log(Time.time,$"Enemy;Health;DecreaseTo;{ctx.currentHealth};ID;{ctx.gameObject.GetInstanceID()}");
        if (ctx.currentHealth<=0){ LogManager.Log(Time.time,$"Enemy;Death;By;{cause};ID;{ctx.gameObject.GetInstanceID()}"); ctx.SwitchState(ctx.deadState); }
    }
}

public class EnemyDeadState : IEnemyState
{
    public void Enter(EnemyHealth ctx)
    {
        ctx.isDead=true;
        ctx.anim.SetTrigger("Dead");
        ctx.enemyAudio.clip = ctx.deathClip;
        ctx.enemyAudio.Play();
        ctx.OnDied?.Invoke();
        ctx.AddScore();
        ctx.SwitchState(ctx.sinkingState);
    }
    public void Update(EnemyHealth ctx){} // no-op
    public void TakeDamage(EnemyHealth ctx,int amount,Vector3? hit,string cause){} // no-op
}

public class EnemySinkingState : IEnemyState
{
    public void Enter(EnemyHealth ctx)
    {
        ctx.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled=false;
        ctx.GetComponent<Rigidbody>().isKinematic=true;
        ctx.capsuleCollider.isTrigger=true;
        ctx.isSinking=true;
        if (ctx.CompareTag("Enemy")) GameObject.Destroy(ctx.gameObject, ctx.destroyTime);
        GameObject.Destroy(ctx.gameObject, ctx.destroyTimeMax);
    }
    public void Update(EnemyHealth ctx){ ctx.transform.Translate(-Vector3.up * ctx.sinkSpeed * Time.deltaTime); }
    public void TakeDamage(EnemyHealth ctx,int amount,Vector3? hit,string cause){} // no-op
}

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int startingHealth = 100;
    [SerializeField] public float sinkSpeed = 0.07f;
    [SerializeField] private int scoreValue = 10;
    public AudioClip deathClip;

    [HideInInspector] public Animator anim;
    [HideInInspector] public AudioSource enemyAudio;
    [HideInInspector] public ParticleSystem hitParticles;
    [HideInInspector] public CapsuleCollider capsuleCollider;

    public int currentHealth;
    public bool isDead;
    public bool isSinking;

    public float destroyTime = 3f;
    public float destroyTimeMax = 10f;

    public event Action<int> OnHealthChanged;
    public event Action OnDied;

    IEnemyState state;
    [HideInInspector] public readonly IEnemyState aliveState = new EnemyAliveState();
    [HideInInspector] public readonly IEnemyState deadState = new EnemyDeadState();
    [HideInInspector] public readonly IEnemyState sinkingState = new EnemySinkingState();

    void Awake()
    {
        anim = GetComponent<Animator>();
        enemyAudio = GetComponent<AudioSource>();
        hitParticles = GetComponentInChildren<ParticleSystem>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        currentHealth = startingHealth;
        SwitchState(aliveState);
    }
    void Update(){ state.Update(this); }

    public void SwitchState(IEnemyState next){ state = next; state.Enter(this); }

    // Facade for callers:
    public void TakeDamageGun(int amount, Vector3 hitPoint)=> state.TakeDamage(this, amount, hitPoint, "Gun");
    public void TakeDamageSuper(int amount)=> state.TakeDamage(this, amount, null, "SuperPower");
    public void TakeDamageLevelEnd(int amount)=> state.TakeDamage(this, amount, null, "LevelEnd");

    public void AddScore(){ ScoreManager.AddScore(scoreValue); LogManager.Log(Time.time,$"Score;Update;Value;{scoreValue}"); } // one-liner
}
