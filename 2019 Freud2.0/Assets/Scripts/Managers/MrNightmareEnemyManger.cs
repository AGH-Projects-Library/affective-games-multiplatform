using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Localization;

public class MrNightmareEnemyManager : MonoBehaviour
{
    public static MrNightmareEnemyManager Instance { get; private set; }

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private EnemyHealth mrNightmareHealth;
    [SerializeField] private AffectiveEnemyManager affectiveEnemyManager;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    [SerializeField] private float alertTime = 5f;
    [SerializeField] private string keyNightmareSupportAlert = "alert.nightmareSupport";

    private List<BossPhase> phases = new List<BossPhase>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitPhases();
    }

    private void Update()
    {
        if (playerHealth.currentHealth <= 0 || mrNightmareHealth.currentHealth <= 0) return;

        BossPhase next = GetNextPhase(mrNightmareHealth.currentHealth);
        if (next != null)
        {
            TriggerPhase(next);
        }

        UpdatePhaseTimers();
    }

    private void InitPhases()
    {
        phases.Add(new BossPhase(900, 5, 3f, SpecialEvent.None));
        phases.Add(new BossPhase(600, 7, 2f, SpecialEvent.MediumSpawn));
        phases.Add(new BossPhase(300, 9, 4f, SpecialEvent.SpeedUp));
        phases.Add(new BossPhase(30, 11, 1f, SpecialEvent.HardSpawn));
    }

    private BossPhase GetNextPhase(float currentHealth)
    {
        foreach (var phase in phases)
            if (!phase.Triggered && currentHealth <= phase.HealthThreshold)
                return phase;
        return null;
    }

    private void TriggerPhase(BossPhase phase)
    {
        phase.Triggered = true;
        StartCoroutine(SpawnEnemies(phase.SpawnCount, phase.SpawnDelay));
        HudPopupTextManager.ShowAlert(LocalizationManager.Instance.GetText(keyNightmareSupportAlert), alertTime);

        switch (phase.Event)
        {
            case SpecialEvent.SpeedUp:
                Debug.Log("Enemies speed up!");
                break;

            case SpecialEvent.MediumSpawn:
                affectiveEnemyManager.SpawnMediumWave();
                break;

            case SpecialEvent.HardSpawn:
                affectiveEnemyManager.SpawnHardWave();
                break;
        }
    }

    private IEnumerator SpawnEnemies(int count, float delay)
    {
        for (int i = 0; i < count; i++)
        {
            int enemyIndex = Random.Range(0, enemyPrefabs.Length);
            int spawnIndex = Random.Range(0, spawnPoints.Length);
            Instantiate(enemyPrefabs[enemyIndex], spawnPoints[spawnIndex].position, spawnPoints[spawnIndex].rotation);
            yield return new WaitForSeconds(delay);
        }
    }

    private void UpdatePhaseTimers()
    {
        foreach (var phase in phases)
            if (phase.Triggered) phase.Timer += Time.deltaTime;
    }
}

class BossPhase
{
    public int HealthThreshold;
    public int SpawnCount;
    public float SpawnDelay;
    public bool Triggered;
    public float Timer;
    public SpecialEvent Event;

    public BossPhase(int healthThreshold, int spawnCount, float spawnDelay, SpecialEvent e)
    {
        HealthThreshold = healthThreshold;
        SpawnCount = spawnCount;
        SpawnDelay = spawnDelay;
        Event = e;
        Triggered = false;
        Timer = 0f;
    }
}

enum SpecialEvent { None, SpeedUp, MediumSpawn, HardSpawn }