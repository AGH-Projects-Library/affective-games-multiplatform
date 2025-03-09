using UnityEngine;

public class Bullet : MonoBehaviour
{
	[SerializeField] private Rigidbody m_rigidbody = null;
	[SerializeField] private GameObject m_particlesGroundPrefab = null;
	[SerializeField] private float m_maxLifetime = 5f;

	private float m_timer = 0f;
	private bool m_hitSth = false;

    public void Initialize(Vector3 direction, float speed)
	{
		m_rigidbody.AddForce(direction * speed, ForceMode.VelocityChange);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (m_hitSth)
			return;

		if (other.TryGetComponent<IDamageable>(out var damageable))
		{
			damageable.Damage(1);
		}
		else
		{
			Instantiate(m_particlesGroundPrefab, other.ClosestPoint(transform.position), Quaternion.identity, null);
		}

		m_hitSth = true;
		Destroy(gameObject);
	}

	private void Update()
	{
		if (m_timer > m_maxLifetime)
		{
			Destroy(gameObject);
			return;
		}

		m_timer += Time.deltaTime;
	}
}
