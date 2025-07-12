using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; set; }

	[SerializeField] private bool m_isAffective = true;
	public bool IsAffective => m_isAffective;

    [SerializeField] private Enemy m_startEnemy = null;
	[SerializeField] private EnemiesManager m_enemiesManager = null;
    [SerializeField] private InputActionReference m_resetGunInput = null;
    [SerializeField] private InputActionReference m_resetEnemiesInput = null;
	[SerializeField] private Rigidbody m_gunRigidbody = null;
    [SerializeField] private Transform m_gunReturnPoint = null;
	[SerializeField] private AudioSource m_audioSource = null;
	[SerializeField] private float m_startMusicDelay = 3f;
	[SerializeField] private float m_startMusicFadeTime = 5f;
	[SerializeField] private float m_stopMusicFadeTime = 2f;

	private void Awake()
	{
		Instance = this;
	}

	private void OnEnable()
	{
        m_resetGunInput.action.performed += ResetGun;
		m_resetEnemiesInput.action.performed += ResetEnemies;
	}

	private void OnDisable()
	{
        m_resetGunInput.action.performed -= ResetGun;
		m_resetEnemiesInput.action.performed -= ResetEnemies;
	}

	private void Start()
    {
        m_startEnemy.OnDeath += StartFirstRound;
    }

	public void StartWaveMusic()
	{
		StopAllCoroutines();

		m_audioSource.Stop();

		StartCoroutine(StartWaveMusicInternal());
	}

	private IEnumerator StartWaveMusicInternal()
	{
		yield return new WaitForSeconds(m_startMusicDelay);

		float volume = 0f;

		m_audioSource.volume = volume;
		m_audioSource.Play();

		yield return null;

		while (volume < 1f)
		{
			volume += Time.deltaTime / m_startMusicFadeTime;

			volume = Mathf.Clamp01(volume);

			m_audioSource.volume = volume;

			yield return null;
		}
	}

	public void StopWaveMusic()
	{
		StopAllCoroutines();

		StartCoroutine(StopWaveMusicInternal());
	}

	private IEnumerator StopWaveMusicInternal()
	{
		float volume = 1f;

		m_audioSource.volume = volume;

		yield return null;

		while (volume > 0f)
		{
			volume -= Time.deltaTime / m_stopMusicFadeTime;

			volume = Mathf.Clamp01(volume);

			m_audioSource.volume = volume;

			yield return null;
		}
	}

	private void ResetGun(InputAction.CallbackContext obj)
    {
		m_gunRigidbody.velocity = Vector3.zero;
		m_gunRigidbody.position = m_gunReturnPoint.position;
	}

	private void ResetEnemies(InputAction.CallbackContext obj)
	{
		m_enemiesManager.ResetEnemies();
	}

	private void StartFirstRound(Enemy enemy)
	{
        m_enemiesManager.StartSpawning();
    }
}
