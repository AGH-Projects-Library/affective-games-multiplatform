using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class GunController : MonoBehaviour
{
	[SerializeField] private Transform m_shootPoint = null;
	[SerializeField] private GameObject m_bulletPrefab = null;
	[SerializeField] private float m_shootSpeed = 10f;
	[SerializeField] private GameObject m_particlesPrefab = null;
	[SerializeField] private RectTransform m_reloadCanvas = null;
	[SerializeField] private Image m_reloadImage = null;
	[SerializeField] private float m_reloadTime = 0.5f;
	[SerializeField] private GameObject m_reloadPrefab = null;
	[SerializeField] private AnimationCurve m_blinkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 0f);
	[SerializeField] private float m_blinkTime = 0.5f;
	[SerializeField] private float m_blinkResize = 1.5f;

	private float m_timeFromLastShot = 0f;
	private Vector3 m_reloadCanvasStartScale = Vector3.one;
	private Coroutine m_blinkCoroutine = null;

	[ContextMenu("Test Shoot")]
	private void ShootEditor()
	{
		Shoot(null);
	}

    public void Shoot(ActivateEventArgs _)
	{
		if (m_timeFromLastShot > m_reloadTime)
		{
			var bullet = Instantiate(m_bulletPrefab, m_shootPoint.position, Quaternion.identity, null).GetComponent<Bullet>();
			bullet.Initialize(m_shootPoint.forward, m_shootSpeed);
			Instantiate(m_particlesPrefab, m_shootPoint.position, m_shootPoint.rotation, null);
			m_timeFromLastShot = 0f;
		}
		else
		{
			Instantiate(m_reloadPrefab, m_shootPoint.position, Quaternion.identity, null);
			
			if (m_blinkCoroutine != null)
				StopCoroutine(m_blinkCoroutine);

			m_blinkCoroutine = StartCoroutine(BlinkUI());
		}
	}

	private void Start()
	{
		m_reloadCanvasStartScale = m_reloadCanvas.localScale;
	}

	private void Update()
	{
		m_timeFromLastShot += Time.deltaTime;
		m_reloadImage.fillAmount = Mathf.Clamp01(m_timeFromLastShot / m_reloadTime);
	}

	private IEnumerator BlinkUI()
	{
		float timer = 0f;

		while (timer < m_blinkTime)
		{
			timer += Time.deltaTime;

			m_reloadCanvas.localScale = m_reloadCanvasStartScale * (1f + m_blinkCurve.Evaluate(timer / m_blinkTime) * m_blinkResize);

			yield return null;
		}

		m_reloadCanvas.localScale = m_reloadCanvasStartScale;

		m_blinkCoroutine = null;
	}
}
