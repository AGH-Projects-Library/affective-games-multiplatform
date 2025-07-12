using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{
    Transform player;
    PlayerHealth playerHealth;
    EnemyHealth enemyHealth;
    UnityEngine.AI.NavMeshAgent nav;

    int zeroLvlIndex = 2;


    void Awake ()
    {
        player = GameObject.FindGameObjectWithTag ("Player").transform;
        playerHealth = player.GetComponent <PlayerHealth> ();
        enemyHealth = GetComponent <EnemyHealth> ();

        nav = GetComponent <UnityEngine.AI.NavMeshAgent> ();

        if (SceneManager.GetActiveScene().buildIndex == zeroLvlIndex)
        {
            nav.speed = 0f; // should not move in the begining of tutorial
        }
        else 
        {
            nav.speed = 3f;
        }
    }


    void Update ()
    {

        if(enemyHealth.currentHealth > 0 && playerHealth.currentHealth > 0)
        {
            nav.SetDestination (player.position);
        }
        
        else
        {
           nav.enabled = false;
        }
    }

    public void SetSpeed(float x)
    {
        nav.speed = x;
    }
}
