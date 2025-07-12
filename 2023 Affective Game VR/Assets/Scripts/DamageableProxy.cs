using TNRD;
using UnityEngine;

public class DamageableProxy : MonoBehaviour, IDamageable
{
	[SerializeField] private SerializableInterface<IDamageable> m_damageable = null;

	public void Damage(int damage)
	{
        m_damageable.Value.Damage(damage);
    }
}
