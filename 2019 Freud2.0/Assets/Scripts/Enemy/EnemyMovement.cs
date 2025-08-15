using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyMovement : MonoBehaviour
{
    public float speed = 3f;
    private Transform player;
    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth;
    private UnityEngine.AI.NavMeshAgent nav;

    void Awake ()
    {
        player = GameObject.FindGameObjectWithTag ("Player").transform;
        playerHealth = player.GetComponent <PlayerHealth> ();
        enemyHealth = GetComponent <EnemyHealth> ();

        nav = GetComponent <UnityEngine.AI.NavMeshAgent> ();

        nav.speed = SceneManager.GetActiveScene().buildIndex == 2 ? 0f : speed;
    }

    void Update ()
    {
        if (IsEnemyAndPlayerAlive())
            nav.SetDestination(player.position);
        else
            nav.enabled = false;
    }

    private bool IsEnemyAndPlayerAlive() => enemyHealth.currentHealth > 0 && playerHealth.currentHealth > 0;

    public void SetSpeed(float x) => nav.speed = x;
}
