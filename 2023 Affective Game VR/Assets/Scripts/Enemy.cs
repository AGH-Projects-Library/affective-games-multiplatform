using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
	[SerializeField] private Rigidbody m_rigidbody = null;
	[SerializeField] private Collider m_collider = null;
	[SerializeField] private Animator m_animator = null;
	[SerializeField] private GameObject m_getHitParticles = null;
	[SerializeField] private GameObject m_explodeParticles = null;
	[SerializeField] private int m_hp = 1;
	[SerializeField] private float m_distanceToReachPathPoint = 0.5f;
	[SerializeField] private float m_rotationSpeed = 0.1f;
	[SerializeField] private float m_timeToDestroy = 2f;
	[SerializeField] private GameObject m_audioSourcePrefab = null;
	[SerializeField] private AudioClip m_getHitClip = null;
	[SerializeField] private AudioClip m_explodeClip = null;
	[SerializeField] private AudioClip[] m_spawnClips = null;

	public Action<Enemy> OnDeath { get; set; }

	private EnemiesManager m_enemiesManager = null;
	private Transform[] m_pathToPlayer = null;
	private int m_currentPathPointNumber = 0;
	private Transform m_targetPathPoint = null;
	private bool m_canMove = false;

	private void FixedUpdate()
	{
		if (!m_canMove)
			return;

		var vectorToTarget = m_targetPathPoint.position - transform.position;
		vectorToTarget.y = 0f;
		if (vectorToTarget.sqrMagnitude <= m_distanceToReachPathPoint * m_distanceToReachPathPoint)
		{
			m_currentPathPointNumber++;

			if (m_currentPathPointNumber >= m_pathToPlayer.Length)
			{
				Castle.Instance.Damage(1);

				Explode();

				return;
			}

			m_targetPathPoint = m_pathToPlayer[m_currentPathPointNumber];
		}

		var newPosition = Vector3.MoveTowards(transform.position, m_targetPathPoint.position, m_enemiesManager.EnemySpeed * Time.deltaTime);
		newPosition.y = 0f;

		m_rigidbody.MovePosition(newPosition);
		m_rigidbody.MoveRotation(Quaternion.RotateTowards(m_rigidbody.rotation, Quaternion.LookRotation(vectorToTarget.normalized), m_rotationSpeed * Time.deltaTime));
	}

	public void Initialize(EnemiesManager enemiesManager, Transform[] pathToPlayer)
	{
		m_enemiesManager = enemiesManager;
		m_pathToPlayer = pathToPlayer;
		m_targetPathPoint = m_pathToPlayer[0];

		m_canMove = true;

		PlaySound(m_spawnClips[UnityEngine.Random.Range(0, m_spawnClips.Length)]);

		m_animator.SetBool(UnityEngine.Random.Range(0f, 1f) > 0.5f ? "Walk" : "Run", true);
	}

	public void Damage(int damage)
	{
		m_hp -= damage;
		m_animator.SetTrigger("GetHit");

		Instantiate(m_getHitParticles, transform.position, Quaternion.identity, null);

		PlaySound(m_getHitClip);

		if (m_hp <= 0)
		{
			m_canMove = false;

			OnDeath?.Invoke(this);

			m_animator.SetBool("Die", true);

			m_collider.enabled = false;

			StartCoroutine(DestroyAfterTime());
		}
	}

	public void SelfDestruct()
	{
		OnDeath?.Invoke(this);

		Destroy(gameObject);
	}

	private void Explode()
	{
		Instantiate(m_explodeParticles, transform.position, Quaternion.identity, null);
		
		OnDeath?.Invoke(this);

		PlaySound(m_explodeClip);

		Destroy(gameObject);
	}

	private void PlaySound(AudioClip clip)
	{
		var audioSource = Instantiate(m_audioSourcePrefab, transform.position, Quaternion.identity, null).GetComponent<AudioSource>();
		audioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
		audioSource.PlayOneShot(clip);
	}

	private IEnumerator DestroyAfterTime()
	{
		yield return new WaitForSeconds(m_timeToDestroy);

		Destroy(gameObject);
	}
}
