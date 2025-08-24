using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Localization;

public interface IBossPhase { bool ShouldEnter(MrNightmareEnemyManager ctx); IEnumerator Run(MrNightmareEnemyManager ctx); } // minimal

public class ThresholdPhase : IBossPhase
{
    readonly int hp; readonly int count; readonly float delay; readonly SpecialEvent ev;
    public bool Triggered{ get; private set; }
    public ThresholdPhase(int hp,int count,float delay,SpecialEvent ev){ this.hp=hp; this.count=count; this.delay=delay; this.ev=ev; }
    public bool ShouldEnter(MrNightmareEnemyManager ctx)=> !Triggered && ctx.mrNightmareHealth.currentHealth<=hp; // one-liner
    public IEnumerator Run(MrNightmareEnemyManager ctx)
    {
        Triggered=true;
        ctx.AlertSupport();
        yield return ctx.StartCoroutine(ctx.SpawnEnemies(count, delay));
        ctx.RunEvent(ev);
    }
}

public class MrNightmareEnemyManager : SingletonBase<MrNightmareEnemyManager>
{
    [SerializeField] public PlayerHealth playerHealth;
    [SerializeField] public EnemyHealth mrNightmareHealth;
    [SerializeField] public AffectiveEnemyManager affectiveEnemyManager;
    [SerializeField] public GameObject[] enemyPrefabs;
    [SerializeField] public Transform[] spawnPoints;
    [SerializeField] private float alertTime = 5f;
    [SerializeField] private string keyNightmareSupportAlert = "alert.nightmareSupport";

    readonly List<IBossPhase> phases = new();

    public new void Awake()
    {
        base.Awake(); 
        phases.Add(new ThresholdPhase(900,5,3f,SpecialEvent.None)); 
        phases.Add(new ThresholdPhase(600,7,2f,SpecialEvent.MediumSpawn));
        phases.Add(new ThresholdPhase(300,9,4f,SpecialEvent.SpeedUp));
        phases.Add(new ThresholdPhase(30,11,1f,SpecialEvent.HardSpawn));
    } // one-liner list

    void Update()
    {
        if (playerHealth.currentHealth<=0 || mrNightmareHealth.currentHealth<=0) return;
        foreach (var p in phases){ if (p.ShouldEnter(this)){ StartCoroutine(p.Run(this)); break; } }
    }

    public void AlertSupport(){ HudPopupTextManager.ShowAlert(LocalizationManager.TryGetText(keyNightmareSupportAlert, out var t)? t : keyNightmareSupportAlert, alertTime); } // one-liner
    public IEnumerator SpawnEnemies(int count,float delay)
    {
        for (int i=0;i<count;i++){ int e=Random.Range(0, enemyPrefabs.Length); int s=Random.Range(0, spawnPoints.Length); Instantiate(enemyPrefabs[e], spawnPoints[s].position, spawnPoints[s].rotation); yield return new WaitForSeconds(delay); }
    }
    public void RunEvent(SpecialEvent ev)
    {
        if (ev==SpecialEvent.SpeedUp) Debug.Log("Enemies speed up!");
        else if (ev==SpecialEvent.MediumSpawn) affectiveEnemyManager.SpawnMediumWave();
        else if (ev==SpecialEvent.HardSpawn) affectiveEnemyManager.SpawnHardWave();
    }
}

public enum SpecialEvent { None, SpeedUp, MediumSpawn, HardSpawn }
