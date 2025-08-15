using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private float timeBetweenAttacks = 2f;
    [SerializeField] private int attackDamage = 10;

    Animator anim;
    GameObject player;
    PlayerHealth playerHealth;
    EnemyHealth enemyHealth;
    bool _playerInRange;
    float _timer;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerHealth = player.GetComponent<PlayerHealth>();
        enemyHealth = GetComponent<EnemyHealth>();
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        _playerInRange = other.gameObject == player;
    }

    private void OnTriggerExit(Collider other)
    {
        _playerInRange = false;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (CanAttack()) Attack();
        if (PlayerIsDead()) anim.SetTrigger("PlayerDead");
    }

    private bool CanAttack()
        => _timer >= timeBetweenAttacks && _playerInRange && enemyHealth.currentHealth > 0 && playerHealth.currentHealth > 0;

    private void Attack()
        => playerHealth.TakeDamage(attackDamage, gameObject.GetInstanceID());

    private bool PlayerIsDead()
        => playerHealth.currentHealth <= 0;
}
