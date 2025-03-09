using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class EnemiesManager : MonoBehaviour
{
	[SerializeField] private float m_timeBetweenEnemiesSpeedEvent = 2f;
	[SerializeField] private EnemiesSpawner[] m_spawners = null;
	[SerializeField] private Wave[] m_waves = null;
	[SerializeField] private GameObject m_enemyPrefab = null;
	[SerializeField] private float m_minEnemySpeed = 1f;
	[SerializeField] private float m_maxEnemySpeed = 3.5f;
	[SerializeField] private float m_speedPassFunctionCoef = 0.3f;
	[SerializeField] private float m_minSpawnFrequencyMultiplier = 0.7f;
	[SerializeField] private float m_maxSpawnFrequencyMultiplier = 1.3f;
	[SerializeField] private AudioSource m_audioSource = null;
	[SerializeField] private AudioClip m_nextWaveClip = null;
	[SerializeField] private AudioClip m_endWaveClip = null;
	[SerializeField] private AudioClip m_endGameClip = null;
	[SerializeField] private TMP_Text m_wavesAffectiveText = null;
	[SerializeField] private TMP_Text m_wavesNormalText = null;
	[SerializeField] private string[] m_wavesNames = new string[] {
						"Prepare to defend your castle",
						"Something bad is comming...",
						"You're halfway through!",
						"It's almost over!",
						"Just a little longer",
						"You defended your castle!"};

	public bool GameStarted { get; set; } = false;
	public float EnemySpeed { get; set; } = 1f;
	
	private float m_timer = 0f;
	private float m_timeSinceLastSpawn = 0f;
	private float m_nextEnemiesSpeedEventTime = 0f;
	private int m_currentWaveNumber = -1;
	private Wave m_currentWave = null;
	private readonly List<EnemiesSpawner> m_spawnersInUse = new();
	private int m_spawnedEnemies = 0;
	private int m_enemiesSpawnedInWave = 0;
	private Coroutine m_startNextWaveCoroutine = null;
	private readonly List<float> m_edaValuesInWave = new();
	private readonly List<float> m_hrValuesInWave = new();
	private float m_edaAvgInWave = 0f;
	private float m_hrAvgInWave = 0f;
	private readonly List<Enemy> m_enemies = new();

	[ContextMenu("Calculate Min Duration")]
	private void CalculateMinDuration()
	{
		float duration = m_waves.Sum(wave => wave.Duration + wave.Break);

		Debug.Log($"{duration}s = {Mathf.Floor(duration / 60f)}:{duration % 60f}m");
	}

	private void Update()
	{
		if (!GameStarted)
			return;

		var bitalino = BitalinoManager.Instance;

		if (m_nextEnemiesSpeedEventTime <= Time.time)
		{
			m_nextEnemiesSpeedEventTime = Time.time + m_timeBetweenEnemiesSpeedEvent;

			if (GameManager.Instance.IsAffective)
			{
				var e = Mathf.Exp(m_speedPassFunctionCoef * (bitalino.HrAvg - bitalino.HrCalib));
				EnemySpeed = m_minEnemySpeed + ((m_maxEnemySpeed - m_minEnemySpeed) / (1 + e));
			}
			else
			{
				EnemySpeed = Mathf.Lerp(m_minEnemySpeed, m_maxEnemySpeed, (float)m_currentWaveNumber / (float)m_waves.Length);
			}

			bitalino.AddEvent(EventStrings.GetEnemiesSpeedString(EnemySpeed));

			m_edaValuesInWave.Add(bitalino.EdaAvg);
			m_hrValuesInWave.Add(bitalino.HrAvg);
		}

		if (m_currentWave.Duration < m_timer)
		{
			if (m_spawnedEnemies <= 0 && m_startNextWaveCoroutine == null)
				m_startNextWaveCoroutine = StartCoroutine(StartNextWaveAfterBreak());

			return;
		}

		float t = 1f;
		if (GameManager.Instance.IsAffective)
			t = Mathf.InverseLerp(bitalino.EdaMin, bitalino.EdaMax, m_edaAvgInWave);

		if (m_currentWave.BaseTimeBetweenSpawns * Mathf.Lerp(m_minSpawnFrequencyMultiplier, m_maxSpawnFrequencyMultiplier, t) < m_timeSinceLastSpawn)
		{
			SpawnEnemy();
		}

		m_timeSinceLastSpawn += Time.deltaTime;
		m_timer += Time.deltaTime;
	}

	[ContextMenu("Start Game")]
	public void StartSpawning()
	{
		GameStarted = true;

		m_nextEnemiesSpeedEventTime = Time.time + m_timeBetweenEnemiesSpeedEvent;

		StartNextWave(true);
	}

	public void ResetEnemies()
	{
		var copy = new Enemy[m_enemies.Count];
		m_enemies.CopyTo(copy);

		foreach (var enemy in copy)
			enemy.SelfDestruct();
	}

	private IEnumerator StartNextWaveAfterBreak()
	{
		m_audioSource.PlayOneShot(m_endWaveClip);
		GameManager.Instance.StopWaveMusic();

		BitalinoManager.Instance.AddEvent(EventStrings.GetWaveEndString(m_currentWaveNumber, m_enemiesSpawnedInWave, m_currentWave.BaseNumOfSpawnersInUse, Castle.Instance.DamageReceivedInRound));
		m_enemiesSpawnedInWave = 0;
		Castle.Instance.DamageReceivedInRound = 0;

		yield return new WaitForSeconds(m_currentWave.Break);

		StartNextWave();

		m_startNextWaveCoroutine = null;
	}

	private void StartNextWave(bool isFirstRound = false)
	{
		m_currentWaveNumber++;
		m_timer = 0f;

		float lastEdaAvgInWave = m_edaAvgInWave;
		m_edaAvgInWave = isFirstRound ? BitalinoManager.Instance.EdaMax : m_edaValuesInWave.Average();
		m_edaValuesInWave.Clear();

		float lastHrAvgInWave = m_hrAvgInWave;
		m_hrAvgInWave = isFirstRound ? BitalinoManager.Instance.HrCalib : m_hrValuesInWave.Average();
		m_hrValuesInWave.Clear();

		if (m_currentWaveNumber >= m_waves.Length)
		{
			m_wavesNormalText.text = "Thank you for playing!";
			m_wavesAffectiveText.text = "Thank you for playing!";

			m_audioSource.PlayOneShot(m_endGameClip);

			GameStarted = false;
			return;
		}
		else

		m_audioSource.PlayOneShot(m_nextWaveClip);
		GameManager.Instance.StartWaveMusic();

		m_currentWave = m_waves[m_currentWaveNumber];

		m_wavesNormalText.text = $"{m_currentWaveNumber + 1} / {m_waves.Length}";
		m_wavesAffectiveText.text = m_wavesNames[Mathf.FloorToInt(m_currentWaveNumber / m_waves.Length)];

		m_spawnersInUse.Clear();

		foreach (var spawner in m_spawners)
			m_spawnersInUse.Add(spawner);

		var spawnersModifier = 0;
		if (GameManager.Instance.IsAffective && !isFirstRound)
		{
			if (m_hrAvgInWave < lastHrAvgInWave && m_edaAvgInWave < lastEdaAvgInWave)
				spawnersModifier += 1;
			else if (m_hrAvgInWave > lastHrAvgInWave && m_edaAvgInWave > lastEdaAvgInWave)
				spawnersModifier -= 1;
		}

		int spawnersInUseCount = Mathf.Clamp(m_currentWave.BaseNumOfSpawnersInUse + spawnersModifier, 1, m_spawnersInUse.Count);
		while (m_spawnersInUse.Count > spawnersInUseCount)
			m_spawnersInUse.RemoveAt(Random.Range(0, m_spawnersInUse.Count));
	}

	private void SpawnEnemy()
	{
		m_timeSinceLastSpawn = 0f;

		var spawner = m_spawnersInUse[Random.Range(0, m_spawnersInUse.Count)];

		var enemy = Instantiate(m_enemyPrefab, spawner.SpawnPosition.position, Quaternion.LookRotation(spawner.SpawnPosition.forward), null).GetComponent<Enemy>();

		enemy.Initialize(this, spawner.PathToPlayer);
		enemy.OnDeath += ProcessEnemyDeath;

		m_enemies.Add(enemy);

		m_spawnedEnemies++;
		m_enemiesSpawnedInWave++;
	}

	private void ProcessEnemyDeath(Enemy enemy)
	{
		m_spawnedEnemies--;
		m_enemies.Remove(enemy);
	}
}
