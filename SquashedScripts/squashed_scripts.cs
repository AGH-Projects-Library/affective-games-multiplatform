--- CameraFollow.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

	public Transform target;
	public float smoothing = 5f; //little bit of la
	Vector3 offset;

	void Start()
	{
		offset = transform.position - target.position;
	}

	void FixedUpdate()
	{
		Vector3 targetCamPos = target.position + offset;
		transform.position = Vector3.Lerp(transform.position, targetCamPos, smoothing * Time.deltaTime);
	}
}

--- CamMouseLook.cs ---
using System.Collections;
using UnityEngine;

public class CamMouseLook : MonoBehaviour {
    public float turnSpeed = 3f;
    public float limitUp = 30f;
    public float limitDown = 30f;
    public float limitThree = 60f;
    private GameObject parentObj;
    private Vector3 angles;

    void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
        parentObj = this.transform.parent.gameObject;
    }

    void FixedUpdate() {
        float xturn = Input.GetAxis("Mouse X");
        float yturn = Input.GetAxis("Mouse Y");

        parentObj.transform.Rotate(0f, xturn * turnSpeed, 0f);

        if (IsWithinLimits(angles.x)) {
            transform.Rotate(-yturn * turnSpeed, 0f, 0f);
        }
        else if (angles.x < limitDown) {
            angles.x = limitDown + 1;
            transform.localEulerAngles = angles;
        }
        else if (angles.x < limitThree) {
            angles.x = limitUp - 1;
            transform.localEulerAngles = angles;
        }
        else {
            angles.x = limitDown + 1;
            transform.localEulerAngles = angles;
        }

        angles = transform.localEulerAngles;
    }

    private bool IsWithinLimits(float angle) {
        return angle <= limitUp && angle >= limitDown;
    }
}

--- CharacterController.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    private Animator anim;

    [SerializeField] private KeyCode keyEscape = KeyCode.Escape;

    private void Awake() => anim = GetComponent<Animator>();

    private void Start() {
        Cursor.lockState = CursorLockMode.Locked;		
    }
	
    void FixedUpdate() {

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float translation = v * speed;
        float straffe = h * speed;
        translation *= Time.deltaTime;
        straffe *= Time.deltaTime;

        transform.Translate(straffe, 0, translation);

        Animating(h, v);

        // Use configurable keyEscape instead of hard-coded string
        if (IsEscapePressed())
            Cursor.lockState = CursorLockMode.None;
    }

    private bool IsEscapePressed() => Input.GetKeyDown(keyEscape);

    private void Animating(float h, float v)
    {
        anim.SetBool("IsWalking", IsWalking(h, v));
    }

    private bool IsWalking(float h, float v) => h != 0f || v != 0f;
}

--- EnemyAttack.cs ---
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private float timeBetweenAttacks = 2f;
    [SerializeField] private int attackDamage = 10;

    Animator anim;
    GameObject player;
    PlayerHealth playerHealth;
    EnemyHealth enemyHealth;
    bool _playerInRange;
    float _timer;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerHealth = player.GetComponent<PlayerHealth>();
        enemyHealth = GetComponent<EnemyHealth>();
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        _playerInRange = other.gameObject == player;
    }

    private void OnTriggerExit(Collider other)
    {
        _playerInRange = false;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (CanAttack()) Attack();
        if (PlayerIsDead()) anim.SetTrigger("PlayerDead");
    }

    private bool CanAttack()
        => _timer >= timeBetweenAttacks && _playerInRange && enemyHealth.currentHealth > 0 && playerHealth.currentHealth > 0;

    private void Attack()
        => playerHealth.TakeDamage(attackDamage, gameObject.GetInstanceID());

    private bool PlayerIsDead()
        => playerHealth.currentHealth <= 0;
}

--- EnemyHealth.cs ---
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int startingHealth = 100;
    public int currentHealth;
    [SerializeField] private float sinkSpeed = 0.07f;
    [SerializeField] private int scoreValue = 10;
    public AudioClip deathClip;


    Animator anim;
    AudioSource enemyAudio;
    ParticleSystem hitParticles;
    CapsuleCollider capsuleCollider;
    private bool isDead;
    bool isSinking;

    float destroyTime = 3f;
    float destroyTimeMax = 10f;

    void Awake ()
    {
        anim = GetComponent <Animator> ();
        enemyAudio = GetComponent <AudioSource> ();
        hitParticles = GetComponentInChildren <ParticleSystem> ();
        capsuleCollider = GetComponent <CapsuleCollider> ();

        currentHealth = startingHealth;
    }


    private void Update() { if (isSinking) transform.Translate(-Vector3.up * sinkSpeed * Time.deltaTime); }

    private bool IsDead() => isDead;

    public void TakeDamageSuper (int amount)
    {
        if(isDead)
            return;

        enemyAudio.Play ();

        currentHealth -= amount;

        if(currentHealth <= 0 && !IsDead())
        {
            LogManager.Log(Time.time, "Enemy;Death;By;SuperPower;ID;" + gameObject.GetInstanceID() + ";PositionX;" + gameObject.transform.position.x + ";PositionY;" + gameObject.transform.position.y + ";PositionZ;" + gameObject.transform.position.z + ";RotationX;" + gameObject.transform.rotation.x + ";RotationY;" + gameObject.transform.rotation.y + ";RotationZ;" + gameObject.transform.rotation.z + ";RotationW;" + gameObject.transform.rotation.w);
            TypicalDeath ();
            AddScore();
        }
    }

    public void TakeDamageLevelEnd (int amount)
    {
        if(isDead)
            return;

        // enemyAudio.Play (); // F2

        currentHealth -= amount;

        if(currentHealth <= 0 && !IsDead())
        {
            LogManager.Log(Time.time, "Enemy;Death;By;LevelEnd;ID;" + gameObject.GetInstanceID() + ";PositionX;" + gameObject.transform.position.x + ";PositionY;" + gameObject.transform.position.y + ";PositionZ;" + gameObject.transform.position.z + ";RotationX;" + gameObject.transform.rotation.x + ";RotationY;" + gameObject.transform.rotation.y + ";RotationZ;" + gameObject.transform.rotation.z + ";RotationW;" + gameObject.transform.rotation.w);
            Death ();
            // AddScore();
        }
    }


    private bool CanTakeDamage() => !IsDead();

    public void TakeDamage (int amount, Vector3 hitPoint)
    {
        if(isDead)
            return;

        enemyAudio.Play ();

        currentHealth -= amount;

        LogManager.Log(Time.time, "Enemy;Health;DecreaseTo;" + currentHealth + ";ID;" + gameObject.GetInstanceID());
            
        hitParticles.transform.position = hitPoint;
        hitParticles.Play();

        if(currentHealth <= 0)
        {
            LogManager.Log(Time.time, "Enemy;Death;By;Gun;ID;" + gameObject.GetInstanceID() +  ";PositionX;" + gameObject.transform.position.x + ";PositionY;" + gameObject.transform.position.y + ";PositionZ;" + gameObject.transform.position.z + ";RotationX;" + gameObject.transform.rotation.x + ";RotationY;" + gameObject.transform.rotation.y + ";RotationZ;" + gameObject.transform.rotation.z + ";RotationW;" + gameObject.transform.rotation.w);
            TypicalDeath ();
            AddScore();
        }
    }

    private void TypicalDeath()
    {
        isDead = true;

        anim.SetTrigger ("Dead");

        enemyAudio.clip = deathClip;
        enemyAudio.Play ();
    }


    private void Death()
    {
        isDead = true;

        anim.SetTrigger ("Dead");

        enemyAudio.clip = deathClip;
    }


    private void StartSinking()
    {
        GetComponent <UnityEngine.AI.NavMeshAgent> ().enabled = false;
        GetComponent <Rigidbody> ().isKinematic = true;
        isSinking = true;
        
        capsuleCollider.isTrigger = true;

        if (gameObject.tag.Equals("Enemy"))
        {
            Destroy (gameObject, destroyTime);
        }

        Destroy (gameObject, destroyTimeMax);
    }

    private void AddScore()
    {
        ScoreManager.AddScore(scoreValue);
        LogManager.Log(Time.time, "Score;Update;Value;" + scoreValue);
    }
}

--- EnemyMovement.cs ---
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

--- LocalizationData.cs ---
using System.Collections.Generic;
using UnityEngine;

namespace Localization
{
    public enum GameLanguage { English, Polish }

    [System.Serializable]
    public class LocalizationEntry
    {
        public string key;
        [TextArea(2, 10)] public string english;
        [TextArea(2, 10)] public string polish;
    }

    [CreateAssetMenu(fileName = "LocalizationData", menuName = "Localization/Data")]
    public partial class LocalizationData : ScriptableObject
    {
        //the other part of partial script is in LocalizationData.Populate.cs, but it was ommited due to only having constants
        [SerializeField] public List<LocalizationEntry> entries = new List<LocalizationEntry>();
        
        public bool TryGetText(string key, GameLanguage lang, out string text)
        {
            var entry = entries.Find(e => e.key == key);
            if (entry == null) { text = $"[MISSING:{key}]"; return false; }
            text = lang == GameLanguage.English ? entry.english : entry.polish;
            return true;
        }

        public void AddOrUpdate(string key, string polish, string english)
        {
            var entry = entries.Find(e => e.key == key);
            if (entry == null)
                entries.Add(new LocalizationEntry { key = key, english = english, polish = polish });
            else
            {
                entry.english = english;
                entry.polish = polish;
            }
        }
    }
}
--- LocalizationManager.cs ---
using UnityEngine;

namespace Localization
{
    public class LocalizationManager : SingletonBase<LocalizationManager>
    {

        [SerializeField] private LocalizationData localizationData;
        public GameLanguage CurrentLanguage { get; private set; } = GameLanguage.English;

        private new void Awake()
        {
            InitInstance();
            DontDestroyOnLoad(gameObject);
        }
        
        public static void SetLanguage(GameLanguage lang)
        {
            if (!hasInstance) { Debug.LogError("LocalizationManager instance is not initialized."); return; }
            Instance.CurrentLanguage = lang;
            Debug.Log($"Language set to: {lang}");
        }
        
        public static bool TryGetText(string key, out string text)
        {
            if (!hasInstance) {
                text = "NO LOCALIZATION MANAGER";
                return false; }
            return Instance.localizationData.TryGetText(key, Instance.CurrentLanguage, out text);
        }
    }
}
--- AffectiveEnemyManager.cs ---
using System.Collections;
using Localization;
using UnityEngine;

using System.Collections;
using UnityEngine;
using Localization;

public class AffectiveEnemyManager : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private float alertTime = 5f;
    [SerializeField] private string keyAlertNonAffective = "alert.moreMonsters";

    public void SpawnMediumWave()
    {
        ShowAlert();
        StartCoroutine(SpawnWave("AdditionalMediumSpawn"));
    }

    public void SpawnHardWave()
    {
        ShowAlert();
        StartCoroutine(SpawnWave("AdditionalHardSpawn"));
    }

    private IEnumerator SpawnWave(string mechanic)
    {
        int enemyCount = Random.Range(3, 6); // configurable range
        for (int i = 0; i < enemyCount; i++)
        {
            if (playerHealth.currentHealth <= 0f) yield break;
            if (GameObject.FindGameObjectsWithTag("Enemy").Length >= EnemyManager.Instance.maxEnemies) yield break;

            int enemyIndex = Random.Range(0, enemyPrefabs.Length);
            int spawnIndex = Random.Range(0, spawnPoints.Length);

            Instantiate(enemyPrefabs[enemyIndex], spawnPoints[spawnIndex].position, spawnPoints[spawnIndex].rotation);
            LogManager.Log(Time.time, $"Enemy;Spawn;Type;{enemyIndex};SpawnPoint;{spawnPoints[spawnIndex].name};Mechanic;{mechanic}");

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void ShowAlert()
    {
        if (LocalizationManager.TryGetText(keyAlertNonAffective, out var alertText))
            HudPopupTextManager.ShowAlert(alertText, alertTime);
    }
}


--- EnemyManager.cs ---
using System.IO;
using UnityEngine;

public class EnemyManager : SingletonBase<EnemyManager>
{
    public float spawnTime = 3f;
    public float invokeTime = 5f;
    public PlayerHealth playerHealth;
    public GameObject enemy;
    public Transform[] spawnPoints;

    [SerializeField] private int _maxEnemies = 8;
    public int maxEnemies
    {
        get => maxEnemies;
        set => maxEnemies = value;
    }
    
    private void Awake()
    {
        InitInstance();
    }

    void Start()
    {
        if(hasInstance) InvokeRepeating(nameof(Spawn), invokeTime, spawnTime);
    }

    private void Spawn()
    {
        if (playerHealth.currentHealth <= 0f || GameObject.FindGameObjectsWithTag("Enemy").Length >= maxEnemies)
            return;

        int spawnPointIndex = Random.Range(0, spawnPoints.Length);

        Instantiate(enemy, spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
        LogManager.Log(Time.time, "Enemy;Spawn;Type;" + "-1" + ";ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[spawnPointIndex].name + ";Mechanic;" + "RegularSpawn");
    }
}

--- GameOverManager.cs ---
using System.Collections;
using System.Collections.Generic;
using Localization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : SingletonBase<GameOverManager>
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Text textGO;
    [SerializeField] private string keyGameOverText = "gameover.text";
    [SerializeField] private string nameScoreBoard = "ScoreBoard";

    [SerializeField] private float timeCD = 3.0f;
    [SerializeField] private string animationName = "GameOver";

    private Animator anim;
    private bool flagUsed = false;

    private void Awake()
    {
        InitInstance();
        if (!anim) anim = GetComponent<Animator>();
        // textGO.text = LocalizationManager.TryGetText(keyGameOverText, out string localizedText) ? localizedText : keyGameOverText;
    }

    void Update()
    {
        if (IsPlayerDead() && IsTimeUp()) RestartLevel();
        else if (IsPlayerDead() && !flagUsed) HandleGameOver();
        else if (Input.GetKey(KeyCode.B)) GoToScoreBoard();
    }

    private bool IsPlayerDead() => playerHealth.currentHealth <= 0;
    private bool IsTimeUp() => timeCD <= 0;

    private void RestartLevel()
    {
        // UserManager.Instance.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleGameOver()
    {
        flagUsed = true;
        anim.SetTrigger(animationName);
        timeCD = 4.0f;
        StartCoroutine(LoseTime());
    }

    private void GoToScoreBoard()
    {
        // UserManager.Instance.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
        SceneManager.LoadScene(nameScoreBoard);
    }
    
    private IEnumerator LoseTime()
    {
        while (true)
        {
            string txt = (LocalizationManager.TryGetText(keyGameOverText, out string localizedText) ? localizedText : keyGameOverText) + timeCD + "s";
            textGO.text = txt;
            yield return new WaitForSeconds(1);
            timeCD -= 1;
        }
    }
}

--- GameStateManager.cs ---
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : SingletonBase<GameStateManager>
{
    [SerializeField] private int zeroLevel = 2;
    [SerializeField] private float zeroLevelWait = 105f;
    [SerializeField] private float levelUpDelay = 3f;

    private float levelUpTimer;
    private bool zeroLevelTriggered;

    private new void Awake()
    {
        base.Awake();
        ScoreManager.OnLevelUpReached += HandleLevelUp;
    }

    private void Update()
    {
        if (IsZeroLevelTimeout()) TryLoadNextLevelForZeroLevel();
        if (IsGameEnded()) EndGame();
    }

    private void HandleLevelUp()
    {
        DestroyAllEnemiesAndPickups();
        levelUpTimer += Time.deltaTime;
        if (levelUpTimer > levelUpDelay)
        {
            LogManager.Log(Time.time, $"Score;LvlEnd;Level;{SceneManager.GetActiveScene().buildIndex};Value;{ScoreManager.Score}");
            SceneSwapper.LoadNextScene();
        }
    }

    private bool IsZeroLevelTimeout() =>
        SceneManager.GetActiveScene().buildIndex == zeroLevel && Time.timeSinceLevelLoad > zeroLevelWait;

    private void TryLoadNextLevelForZeroLevel()
    {
        if (zeroLevelTriggered) return;
        if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0 ||
            GameObject.FindGameObjectsWithTag("PickUp").Length == 0)
        {
            zeroLevelTriggered = true;
            SceneSwapper.LoadNextScene();
        }
    }

    private void DestroyAllEnemiesAndPickups()
    {
        DestroyGameObjectsWithTag("Enemy");
        DestroyGameObjectsWithTag("PickUp");
    }

    private void DestroyGameObjectsWithTag(string tag)
    {
        foreach (var obj in GameObject.FindGameObjectsWithTag(tag))
            Destroy(obj);
    }

    private bool IsGameEnded() => Time.time > 1200;

    private void EndGame()
    {
        // UserManager.Instance.UpdateScore(SceneManager.GetActiveScene().buildIndex, ScoreManager.Score);
        LogManager.Log(Time.time, $"Score;GameEnd;Level;{SceneManager.GetActiveScene().buildIndex};Value;{ScoreManager.Score}");
        SceneSwapper.LoadEndGameScene();
    }
}

--- LogManager.cs ---
using System;
using System.IO;
using UnityEngine;

public class LogManager : SingletonBase<LogManager>
{
    [SerializeField] private string _fileName = "events.csv";
    [SerializeField] private bool doNotSaveLogs = true;
    private double _startTime;
    
    public struct LogEvent
    {
        public float Time;
        public string eventType;
        public LogEvent(float t, string eT) { Time = t; eventType = eT; }
    }

    private System.Collections.Generic.List<LogEvent> _events = new System.Collections.Generic.List<LogEvent>();

    private new void Awake() => InitInstance(dontDestroyOnLoad: true);
    
    public static void Log(float time,string eventType)
    {
        if(hasInstance) Instance.AddEvent(time, eventType);
    }
    
    private void AddEvent(float time, string eventType) =>_events.Add(new LogEvent(time * 1000, eventType));
    
    private void OnDestroy()
    {
        if (doNotSaveLogs) return;
        StreamWriter writer = File.AppendText(_fileName);
        writer.WriteLine(_startTime.ToString() + ";" + "UnixTime");
        foreach (var e in _events) { writer.WriteLine(e.Time + ";" + e.eventType); }
        writer.Close();
    }
}

--- MrNightmareEnemyManger.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Localization;

public class MrNightmareEnemyManager : SingletonBase<MrNightmareEnemyManager>
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private EnemyHealth mrNightmareHealth;
    [SerializeField] private AffectiveEnemyManager affectiveEnemyManager;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    [SerializeField] private float alertTime = 5f;
    [SerializeField] private string keyNightmareSupportAlert = "alert.nightmareSupport";

    private List<BossPhase> phases = new List<BossPhase>();

    public new void Awake()
    {
        base.Awake();
        InitPhases();
    }
    private void InitPhases()
    {
        phases.Add(new BossPhase(900, 5, 3f, SpecialEvent.None));
        phases.Add(new BossPhase(600, 7, 2f, SpecialEvent.MediumSpawn));
        phases.Add(new BossPhase(300, 9, 4f, SpecialEvent.SpeedUp));
        phases.Add(new BossPhase(30, 11, 1f, SpecialEvent.HardSpawn));
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
        HudPopupTextManager.ShowAlert(LocalizationManager.TryGetText(keyNightmareSupportAlert, out var alertText) ? alertText : keyNightmareSupportAlert, alertTime);

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

--- SceneSwapper.cs ---
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneSwapper
{
    public const int MainMenuSceneIndex = 0;
    public const int EndGameSceneIndex = 11;
    public const int ScoreBoardSceneIndex = 12;

    public static void LoadNextScene() => LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    public static void LoadScene(int buildIndex) => SceneManager.LoadScene(buildIndex);
    public static void LoadEndGameScene() => SceneManager.LoadScene(EndGameSceneIndex);
}

--- SingletonBase.cs ---
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class SingletonBase<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance => _instance;
    public void Awake()
    {
        InitInstance();
    }

    protected void InitInstance(bool dontDestroyOnLoad = false)
    {
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
        
        if (_instance == null)
            _instance = this as T;
        else
        {
            Debug.LogWarning($"{this.GetType().Name} instance already exists. Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    private static Dictionary<Type, bool> messagePrinted = new Dictionary<Type, bool>();
    public static bool hasInstance => _instance != null;
    private static bool InstanceExists()
    {
        if (_instance) return true;
        Type masterType = typeof(T);
        if (messagePrinted.TryGetValue(masterType, out bool alreadyPrinted) && alreadyPrinted) return false;
        string thisClassName = typeof(T).Name;
        Debug.LogError($"{thisClassName} instance is not initialized.");
        messagePrinted[masterType] = true;
        return false;
    }
}

--- StimuliManager.cs ---
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StimuliManager : MonoBehaviour
{
    private List<string> stimuliMinus = new();
    private List<string> stimuliPlus = new();

    public void LoadStimuli(string path)
    {
        using StreamReader reader = new StreamReader(path);
        while (reader.Peek() >= 0)
        {
            string[] line = reader.ReadLine().Split(',');
            if (line[0] == "s-") stimuliMinus.Add(line[1]);
            else if (line[0] == "s+") stimuliPlus.Add(line[1]);
        }

        Shuffle(stimuliMinus);
        Shuffle(stimuliPlus);
    }

    private void Shuffle<T>(List<T> list)
    {
        int count = list.Count;
        for (int i = 0; i < count - 1; i++)
        {
            int r = Random.Range(i, count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    public string GetNextMinus() => PopNext(stimuliMinus);
    public string GetNextPlus() => PopNext(stimuliPlus);

    private string PopNext(List<string> list)
    {
        if (list.Count == 0) return null;
        string item = list[0];
        list.RemoveAt(0);
        return item;
    }
}
--- UserManager.cs ---
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UserManager : MonoBehaviour
{
    public static UserManager Instance { get; private set; }

    // private string userName;
    // private string userID;
    // private string userPath;
    // private int[] scoreLvl;

    // private void Awake()
    // {
    //     if (Instance == null) Instance = this;
    //     else { Destroy(gameObject); return; }
    //
    //     DontDestroyOnLoad(gameObject);
    //     InitializeUser();
    // }
    //
    // private void InitializeUser()
    // {
    //     string persDataPath = Application.persistentDataPath;
    //     userPath = persDataPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\";
    //
    //     Cursor.visible = false;
    //     Screen.fullScreen = true;
    //
    //     userID = "0000";
    //     userName = "Zawodnik0000";
    //
    //     string idFile = persDataPath + "\\currentNumber.txt";
    //     if (File.Exists(idFile))
    //     {
    //         using (StreamReader reader = new StreamReader(idFile))
    //             userID = reader.ReadLine() ?? "0000";
    //         userName = "Zawodnik" + userID;
    //     }
    //
    //     LogManager.Log(Time.time, "UserName;" + userName);
    //     LogManager.Log(Time.time, "UserID;" + userID);
    //
    //     userPath += userID + "\\";
    //     Directory.CreateDirectory(userPath);
    //
    //     int lvls = SceneManager.sceneCountInBuildSettings;
    //     scoreLvl = new int[lvls];
    //     Array.Clear(scoreLvl, 0, scoreLvl.Length);
    // }
    //
    // public void UpdateScore(int index, int score)
    // {
    //     if (index < scoreLvl.Length)
    //     {
    //         scoreLvl[index] += score;
    //         LogManager.Log(Time.time, $"Score;Update;Level;{index};Value;{score}");
    //     }
    //     else Debug.LogWarning("Index out of bounds for scoreLvl array.");
    // }
    //
    // public int GetTotalScore() => scoreLvl.Sum();
    // public string GetUserName() => userName;
    // public string GetUserNumber() => userID;
    // public string GetUserPath() => userPath;
    // public void ResetScore() => Array.Clear(scoreLvl, 0, scoreLvl.Length);
}

--- ScoreManager.cs ---
using UnityEngine;
using UnityEngine.Events;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    public static int Score { get; private set; }

    public static event UnityAction<int> OnScoreUpdated;
    public static event UnityAction OnLevelUpReached;

    [SerializeField] private int scoreToLevelUp = 300;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);
        Score = 0;

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex > 2)
            scoreToLevelUp = Random.Range(scoreToLevelUp - 100, scoreToLevelUp);

        LogManager.Log(Time.time, $"Score;ToLevelUp;Value;{scoreToLevelUp}");
    }

    public static void AddScore(int value)
    {
        Score += value;
        OnScoreUpdated?.Invoke(Score);

        if (Score >= Instance.scoreToLevelUp)
            OnLevelUpReached?.Invoke();
    }
}
--- ScoreUI.cs ---
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    public UnityAction<int> OnScoreChanged;

    private void Awake()
    {
        if (scoreText == null) scoreText = GetComponent<Text>();
        OnScoreChanged += UpdateScoreText;
    }

    private void OnEnable() => ScoreManager.OnScoreUpdated += OnScoreChanged.Invoke;
    private void OnDisable() => ScoreManager.OnScoreUpdated -= OnScoreChanged.Invoke;

    private void UpdateScoreText(int newScore) => scoreText.text = newScore.ToString();
}
--- HudPopupTextManager.cs ---
using System;
using System.Collections;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.UI;

public class HudPopupTextManager : SingletonBase<HudPopupTextManager>
{
    public GameObject prefab_alertDisplayer;
    public RectTransform parentCanvas;
    public static void ShowAlert(string message, float duration)
    {
        if (!hasInstance) return; 
        GameObject alertDisplayer = Instantiate(Instance.prefab_alertDisplayer, Instance.parentCanvas);
        alertDisplayer.GetComponent<HudPopupText_AnimatorAndDestuctor>().DisplayAlert(message, duration);
        DontDestroyOnLoad(alertDisplayer);
    }
    
}

--- HudPopupText_AnimatorAndDestuctor.cs ---
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudPopupText_AnimatorAndDestuctor : MonoBehaviour
{
    [SerializeField] private TMP_Text _alertText;
    [SerializeField] private float _duration = 2f;
    [SerializeField] private AnimationCurve _transparencyCurve = AnimationCurve.Linear(0, 1, 1, 0);
    private float _startTime;
    
    public void DisplayAlert(string message, float duration)
    {
        _alertText.text = message;
        _duration = duration;
        _startTime = Time.time;
        UpdateAlertTextColor();
    }

    private float ElapsedTime => Time.time - _startTime;

    private void Update()
    {
        UpdateAlertTextColor();

        if (ElapsedTime >= _duration)
        {
            Destroy(gameObject, 0.1f);
            _alertText.enabled = false;
        }
    }

    private void UpdateAlertTextColor()
    {
        var color = _alertText.color;
        color.a = _transparencyCurve.Evaluate(ElapsedTime / _duration);
        _alertText.color = color;
    }
}

--- PickUpCollect.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpCollect : MonoBehaviour {

    [SerializeField] private int scoreValue = 2;
    [SerializeField] private float speedUp = 2f;
    [SerializeField] private float transformPoint = 8f;
    private AudioSource pickUpAudio;
    private bool isCollected;

    private void Awake() {
        isCollected = false;
        pickUpAudio = GetComponent<AudioSource>();
    }

    private void Update() {
        if (IsCollected())
        {
            transform.Translate(Vector3.up * speedUp * Time.deltaTime, Space.World);
            if (transform.position.y >= transformPoint)
            {
                LogManager.Log(Time.time, "PickUp;Destroyed;ID;" + gameObject.GetInstanceID());
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.name == "Player" && !IsCollected())
        {
            pickUpAudio.Play();
            ScoreManager.AddScore(scoreValue);
            HudPopupTextManager.ShowAlert("+" + scoreValue + " Score", 0.5f);
            LogManager.Log(Time.time, "Score;Update;Value;" + scoreValue);
            LogManager.Log(Time.time, "PickUp;Collected;ID;" + gameObject.GetInstanceID());
            SetCollected(true);
        }
    }

    private bool IsCollected() {
        return isCollected;
    }

    private void SetCollected(bool value) {
        isCollected = value;
    }
}

--- PickUpManager.cs ---
using System.Collections;
using UnityEngine;

public class PickUpManager : MonoBehaviour
{
    public static PickUpManager Instance { get; private set; }
    [SerializeField] private float spawnDelay = 7f;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject pickUp;
    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        MakeThisTheOnlyPickUpManager();
    }

    private int currentSpawnPointIndex;

    private bool IsPlayerAlive => playerHealth.currentHealth > 0f;

    private void Start()
    {
        RandomizeArray(spawnPoints);
        MakeThisTheOnlyPickUpManager();
        currentSpawnPointIndex = 0;
        StartCoroutine(SpawnPickUps());
    }

    private void MakeThisTheOnlyPickUpManager()
    {
        if (Instance == null)
            Instance = this;
    }

    private IEnumerator SpawnPickUps()
    {
        yield return new WaitForSeconds(5f);
        while (IsPlayerAlive)
        {
            SpawnPickUp();
            LogManager.Log(Time.time, "PickUp;Spawn;ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[currentSpawnPointIndex].name);
            currentSpawnPointIndex = (currentSpawnPointIndex + 1) % spawnPoints.Length;

            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private void SpawnPickUp()
    {
        Instantiate(pickUp, spawnPoints[currentSpawnPointIndex].position, spawnPoints[currentSpawnPointIndex].rotation);
    }

    private void RandomizeArray<T>(T[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--) {
            int r = Random.Range(0, i);
            T tmp = arr[i];
            arr[i] = arr[r];
            arr[r] = tmp;
        }
    }
}

--- PlayerHealth.cs ---
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] public int maxHealth = 100;
    [SerializeField] public int healthRegeneration = 5;
    [SerializeField] public float regenerationTime = 10f;
    public Slider healthSlider;
    [SerializeField] public Image damageImage;
    public AudioClip deathClip;
    public float flashSpeed = 5f;
    public Color flashColour = new Color(1f, 0f, 0f, 0.1f);

    public Animator anim;
    public AudioSource playerAudio;
    [SerializeField] private PlayerMovement playerMovement;
    public PlayerShooting playerShooting;
    public bool isDead;
    public bool damaged;

    public float timer;
    public int currentHealth;

    void Awake ()
    {
        timer = 0f;
        playerAudio = GetComponent<AudioSource>();
        playerMovement = GetComponent<PlayerMovement>();
        playerShooting = GetComponentInChildren<PlayerShooting>();
        currentHealth = maxHealth;
        LogManager.Log(Time.time, "Slider;Health;StartingValue;" + currentHealth);
    }

    void Update ()
    {
        if (IsEscapePressed)
        {
            LogManager.Log(Time.time, "Key;Escape");
            Application.Quit(); // ignored in UnityEditor
        }

        timer += Time.deltaTime;
        if (CanRegenHealth)
        {
            currentHealth += healthRegeneration;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            UpdateHealthSlider();

            LogManager.Log(Time.time, "Player;Health;IncreaseTo;" + currentHealth);
            LogManager.Log(Time.time, "Slider;Health;DecreaseTo;" + currentHealth);

            timer = 0f;
        }

        if (IsDamaged)
        {
            damageImage.color = flashColour;
        }
        else
        {
            damageImage.color = Color.Lerp(damageImage.color, Color.clear, flashSpeed * Time.deltaTime);
        }
        SetDamaged(false);

        if (IsDead)
        {
            DisablePlayerMovementAndShooting();

            if (Input.GetKeyDown(KeyCode.R))
            {
                LogManager.Log(Time.time, "Key;R");
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1); // load tutorial of current level
            }

            else if (Input.GetKeyDown(KeyCode.C))
            {
                LogManager.Log(Time.time, "Key;C");
                Application.Quit(); // ignored in UnityEditor
            }
        }
    }

    private bool IsEscapePressed => Input.GetKey(KeyCode.Escape);
    private bool CanRegenHealth => timer > regenerationTime && currentHealth < maxHealth;
    private bool IsDamaged => damaged;
    private bool IsDead => currentHealth <= 0;

    public void TakeDamage(int amount, int i)
    {
        ResetTimer();
        SetDamaged(true);
        currentHealth -= amount;
        LogManager.Log(Time.time, $"Player;Health;DecreaseTo;{currentHealth};By;Enemy;ID;{i}");
        UpdateHealthSlider();

        if (IsDead)
        {
            Death();
        }
    }

    void ResetTimer() => timer = 0f;

    void SetDamaged(bool value) => damaged = value;

    void UpdateHealthSlider() => healthSlider.value = currentHealth;

    void Death()
    {
        LogManager.Log(Time.time, $"Player;Death;PositionX;{gameObject.transform.position.x};PositionY;{gameObject.transform.position.y};PositionZ;{gameObject.transform.position.z};RotationX;{gameObject.transform.rotation.x};RotationY;{gameObject.transform.rotation.y};RotationZ;{gameObject.transform.rotation.z};RotationW;{gameObject.transform.rotation.w}");
        
        isDead = true;
        // playerShooting.DisableEffects();
        anim.SetTrigger("Die");
        playerAudio.clip = deathClip;
        playerAudio.Play();
        DisablePlayerMovementAndShooting();
    }

    void DisablePlayerMovementAndShooting()
    {
        playerMovement.enabled = false;
        playerShooting.enabled = false;
    }
}

--- PlayerMovement.cs ---
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 6f;
    public float speedRotor = 2f;

    Vector3 movement;

    Vector3 rotate;
    Animator anim;
    Rigidbody playerRigidbody;
    int  floorMask;
    [SerializeField] private float camRayLength = 100f;

    void Awake()
    {
        floorMask = LayerMask.GetMask("Floor");
        anim = GetComponent<Animator>();
        playerRigidbody = GetComponent<Rigidbody>();
    }
    
    void FixedUpdate() //called every physics step
    {
        // mapping from keyboard, values = {-1,0,1}
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float r = Input.GetAxisRaw("Mouse X");
        float t = Input.GetAxisRaw("Mouse Y");

        LogManager.Log(Time.time, "Joystick;Left;Horizontal;" + h + ";Vertical;" + v);
        LogManager.Log(Time.time, "Joystick;Right;Horizontal;" + r + ";Vertical;" + t);
        
        TurningAndMoving(r, t, h, v);
        Animating(h, v);
    }

    void TurningAndMoving(float r, float t, float h, float v)
    {
        float r_pos = HasSignificantInput(r) ? r : 0.0f;
        float t_pos = HasSignificantInput(t) ? t : 0.0f;

        rotate.Set(0.0f, r_pos, 0.0f);
        Quaternion deltaRotation = Quaternion.Euler(rotate * Time.deltaTime * speedRotor * 1000);
        playerRigidbody.MoveRotation(playerRigidbody.rotation * deltaRotation);

        movement.Set(h, 0f, v);
        movement = movement.normalized * speed * Time.deltaTime;
        playerRigidbody.MovePosition(transform.position + movement);
    }

    private bool HasSignificantInput(float input)
    {
        return Mathf.Abs(input) > 0.01f;
    }

    private bool IsWalking(float h, float v)
    {
        return h != 0f || v != 0f;
    }

    void Animating(float h, float v)
    {
        anim.SetBool("IsWalking", IsWalking(h, v));
    }
       
}

--- PlayerShooting.cs ---
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class PlayerShooting : MonoBehaviour
{
    public int damagePerShot = 20;
    public int damagePerShotMin = 10;
    public int damagePerShotMax = 20;
    public float timeBetweenBullets = 0.2f;
    public float timeBetweenBulletsMin = 0.2f;
    public float timeBetweenBulletsMax = 0.8f;
    public float range = 100f;
    public float rangeMin = 40f;
    public float rangeMax = 100f;
    public float ultraPower = 100f;
    public float ultraPowerMax = 100f;

    public Slider powerSlider;
    public Image superpowerImage;

    // Configurable input binding for shooting
    [SerializeField] private string shootButtonR2 = "R2";

    float timer = 0f;
    Ray shootRay = new Ray();
    RaycastHit shootHit;
    int shootableMask;
    ParticleSystem gunParticles;
    LineRenderer gunLine;
    AudioSource gunAudio;
    float effectsDisplayTime = 0.2f;
    
    private float timePressed = 0f;
    [SerializeField] private float timePressedMax = 5f;
    private bool isButtonPressed = false;

    [SerializeField] private float regenerationTime = 10f;

    float timeAlpha = 0f;
    float duration = 10000000; //lerp
    float smoothness = 0.02f; //lerp

    [SerializeField] private float ultraPowerRegeneration = 5f;
    [SerializeField] private float rangeRegeneration = 10f;
    [SerializeField] private int damagePerShotRegeneration = 1;

    int currentSceneNumber;
    [SerializeField] private int secondLevelIndex = 5;

    void Awake ()
    {
		currentSceneNumber = SceneManager.GetActiveScene().buildIndex;

        shootableMask = LayerMask.GetMask ("Shootable");
        gunParticles = GetComponent<ParticleSystem> ();
        gunLine = GetComponent <LineRenderer> ();
        gunAudio = GetComponent<AudioSource> ();
        powerSlider.value = ultraPower;
        
        LogManager.Log(Time.time, "Player;Range;StartingValue;" + range);
        LogManager.Log(Time.time, "Player;TimeBetweenTwoBullets;StartingValue;" + timeBetweenBullets);
        LogManager.Log(Time.time, "Player;DamagePerShot;StartingValue;" + damagePerShot);
        LogManager.Log(Time.time, "Player;SuperPower;StartingValue;" + ultraPower);
        LogManager.Log(Time.time, "Slider;SuperPower;StartingValue;" + ultraPower);
    }

  // TODO MAKE IT BACK ALIVE
  //   void Update ()
  //   {
  //       timer += Time.deltaTime;
  //       regenerationTimer += Time.deltaTime;
  //
		// if(IsShootButtonPressed() && CanShoot() && Time.timeScale != 0)
  //       {
  //           LogManager.Log(Time.time, "Key;" + "R2");
  //           Shoot ();
  //       }
  //
  //       if(timer >= timeBetweenBullets * effectsDisplayTime)
  //       {
  //           DisableGunEffects ();
  //       }
  //
  //       if(regenerationTimer > regenerationTime && ultraPower < ultraPowerMax)
  //       {
  //           ultraPower += ultraPowerRegeneration;
  //           if(ultraPower > ultraPowerMax)
  //           {
  //               ultraPower = ultraPowerMax;
  //           }
  //           UpdatePowerSlider();
  //           LogManager.Log(Time.time, "Slider;SuperPower;IncreaseTo;" + ultraPower);
  //           damagePerShot += damagePerShotRegeneration;
  //           if(damagePerShot > damagePerShotMax)
  //           {
  //               damagePerShot = damagePerShotMax;
  //           }
  //           LogManager.Log(Time.time, "Player;DamagePerShot;IncreaseTo;" + damagePerShot);
  //
  //           range += rangeRegeneration;
  //           if(range > rangeMax)
  //           {
  //               range = rangeMax;
  //           }
  //           LogManager.Log(Time.time, "Player;Range;IncreaseTo;" + range);
  //
  //           regenerationTimer = 0f;
  //       }
  //
  //       if (IsInSecondLevel())
  //       {
  //           UpdateButtonPressedTimer();
  //           UseSuperPower();
  //           isButtonPressed = false;
  //       }
  //   }

    private bool IsShootButtonPressed()
    {
        return Input.GetButton(shootButtonR2);
    }

    private bool IsSuperPowerButtonPressed(float timeSuper, float timeMax)
    {
        return timeSuper > 0 && isButtonPressed;
    }

    private void UseSuperPower()
    {
        if(IsSuperPowerButtonPressed(timePressed, timePressedMax) && ultraPower > 0)
        {
            LogManager.Log(Time.time, "Player;SuperPower;Time;" + timePressed);

            float ultraPowerused = ultraPower * (Math.Min(timePressed, timePressedMax) / timePressedMax);
            ultraPower -= ultraPowerused;
            UpdatePowerSlider();
            LogManager.Log(Time.time, "Slider;SuperPower;DecreaseTo;" + ultraPower);

            damagePerShot -= damagePerShotRegeneration;
            if(damagePerShot < damagePerShotMin)
            {
                damagePerShot = damagePerShotMin;
            }
            LogManager.Log(Time.time, "Player;DamagePerShot;DecreaseTo;" + damagePerShot);

            range -= rangeRegeneration;
            if(range < rangeMin)
            {
                range = rangeMin;
            }
            LogManager.Log(Time.time, "Player;Range;DecreaseTo;" + range);
            
            timeAlpha = timePressed;
            // regenerationTimer = 0;

            KillEnemies();
        }

        if(timeAlpha > 0)
        {
            float alpha = Math.Min(timeAlpha, timePressedMax) / timePressedMax;
            LogManager.Log(Time.time, "Player;SuperPower;Alpha;" + alpha);
            Color flashColour = new Color(1f, 0.588235f, 0f, alpha);
            superpowerImage.color = flashColour;
            StartCoroutine(LerpColor());
        }

    }
    
    IEnumerator LerpColor()
    {
        float progress = 0; 
        float increment = smoothness / duration;
        while(progress < 1)
        {
            superpowerImage.color = Color.Lerp(superpowerImage.color, Color.clear, progress);
            progress += increment;
            yield return new WaitForSeconds(smoothness);
        }

    }

    private void UpdateButtonPressedTimer()
    {
        if(Input.GetButtonDown("L2"))
        {
            timePressed = Time.time;
        }
         
        if(Input.GetButtonUp("L2") && !isButtonPressed)
        {
            timePressed = Time.time - timePressed;
            isButtonPressed = true;
            LogManager.Log(Time.time, "Key;L2;Time;" + timePressed);
        }
    }

    private void KillEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int killed = UnityEngine.Random.Range(0, Math.Min(2 * (int)(ultraPower / ultraPowerMax * 100), enemies.Length));
        LogManager.Log(Time.time, "Player;SuperPower;Killed;" + killed);

        for(int i = 0; i < killed; i++)
        {
            GameObject enemy = enemies[i];
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (i > 4)
            {
                // enemyHealth.AddScore();
                // enemyHealth.TakeDamageLvlEnd(enemyHealth.currentHealth);
            }
            else
            {
                enemyHealth.TakeDamageSuper(enemyHealth.currentHealth);
            }
        }
    }
    
    private void UpdatePowerSlider()
    {
        powerSlider.value = ultraPower;
    }
    
    private void DisableGunEffects()
    {
        gunLine.enabled = false;
    }

    private bool CanShoot()
    {
        return timer >= timeBetweenBullets;
    }

    private bool IsInSecondLevel()
    {
        return currentSceneNumber >= secondLevelIndex;
    }


    void Shoot ()
    {
        timer = 0f;

        gunAudio.Play ();

        gunParticles.Stop ();
        gunParticles.Play ();

        gunLine.enabled = true;
        gunLine.SetPosition (0, transform.position);

        shootRay.origin = transform.position;
        shootRay.direction = transform.forward;

        if(Physics.Raycast (shootRay, out shootHit, range, shootableMask))
        {
            EnemyHealth enemyHealth = shootHit.collider.GetComponent <EnemyHealth> ();
            if(enemyHealth != null)
            {
                enemyHealth.TakeDamage (damagePerShot, shootHit.point);
            }
            gunLine.SetPosition (1, shootHit.point);
        }
        else
        {
            gunLine.SetPosition (1, shootRay.origin + shootRay.direction * range);
        }
    }
}

--- ManageEnd.cs ---
using System.Collections;
using Localization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class ManageEnd : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text textCD;

    [SerializeField] private KeyCode keyEscape = KeyCode.Escape;
    [SerializeField] private KeyCode keySkipToScoreBoard = KeyCode.T;
    [SerializeField] private UnityEvent OnScoreBoardRequested = new UnityEvent();

    [SerializeField] private string keyTimeLabel = "end.timeLabel";

    [SerializeField] private float timeToStart = 15f;
    [SerializeField] private string nameScoreBoard = "ScoreBoard";

    private int timeCD;

    void Awake()
    {
        timeCD = (int)timeToStart;
        StartCoroutine(LoseTime());
    }

    void Update()
    {
        if (timeCD < 0) TriggerScoreBoard();
        if (Input.GetKey(keyEscape)) Application.Quit();
        if (Input.GetKeyDown(keySkipToScoreBoard)) TriggerScoreBoard();
    }

    public void TriggerScoreBoard()
    {
        OnScoreBoardRequested?.Invoke();
        if (OnScoreBoardRequested.GetPersistentEventCount() == 0)
            SceneManager.LoadScene(nameScoreBoard);
    }

    IEnumerator LoseTime()
    {
        while (true)
        {
            string txt = LocalizationManager.TryGetText(keyTimeLabel, out var t) ? t + timeCD + "s" : keyTimeLabel + ": " + timeCD + "s";
            textCD.text = txt;
            timeCD -= 1;
            yield return new WaitForSeconds(1);
        }
    }
}
--- ManageIntroduction.cs ---
using System.Collections;
using Localization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class ManageIntroduction : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text textShowed;

    [Header("Input Settings")]
    [SerializeField] private string fireButton2 = "Fire2";
    [SerializeField] private string fireButton3 = "Fire3";
    [FormerlySerializedAs("keyS")] [SerializeField] private KeyCode skipKey = KeyCode.S;
    [SerializeField] private KeyCode quitKey = KeyCode.Escape;
    [SerializeField] private KeyCode startCountdownKey = KeyCode.Space;

    [Header("Localization Keys")]
    [SerializeField] private string keyWelcome = "intro.welcome";
    [SerializeField] private string keyTimeLabel = "intro.timeLabel";

    [Header("Timing Settings")]
    [SerializeField] private float timeToStart = 30f;
    [SerializeField] private float timeWait = 1f;
    [SerializeField] private float countdownStep = 1f;

    private int timeCD;
    private Coroutine countdownRoutine;

    private void Start(){
        timeCD=Mathf.CeilToInt(timeToStart);
        UpdateWelcomeText();
        // StartCountdown();
    }

    private void Update(){
        HandleLanguageInput();
        HandleSkip();
        HandleQuit();
        HandleStartCountdownInput();
    }

    private void HandleLanguageInput(){
        if (Input.GetButtonDown(fireButton2)) SetLanguage(GameLanguage.English);
        if (Input.GetButtonDown(fireButton3)) SetLanguage(GameLanguage.Polish);
    }

    public void SetLanguageEnglish()=>SetLanguage(GameLanguage.English);
    public void SetLanguagePolish()=>SetLanguage(GameLanguage.Polish);

    private void SetLanguage(GameLanguage lang){
        LocalizationManager.SetLanguage(lang);
        RefreshText();
    }

    private void RefreshText(){if (countdownRoutine==null) UpdateWelcomeText(); else SetCountdownText();}
    private void HandleSkip(){if (countdownRoutine!=null && Input.GetKeyDown(skipKey)) LoadLvl_0();}
    private void HandleQuit(){if (Input.GetKeyDown(quitKey)) Application.Quit();}
    private void HandleStartCountdownInput(){if (Input.GetKeyDown(startCountdownKey)) StartCountdown();}
    private void UpdateWelcomeText()
    {
        if (LocalizationManager.TryGetText(keyWelcome, out var text)) textShowed.text = text;
        else textShowed.text = keyWelcome;
    }
    public void LoadLvl_0()=>StartCoroutine(WaitAndLoad(SceneSwapper.MainMenuSceneIndex+2));

    private IEnumerator WaitAndLoad(int sceneIndex){
        yield return new WaitForSeconds(timeWait);
        SceneSwapper.LoadScene(sceneIndex);
    }

    public void StartCountdown(){
        if (countdownRoutine!=null) StopCoroutine(countdownRoutine);
        timeCD=Mathf.CeilToInt(timeToStart);
        countdownRoutine=StartCoroutine(Countdown());
    }

    private IEnumerator Countdown(){
        while (timeCD>=0){
            SetCountdownText();
            timeCD--;
            yield return new WaitForSeconds(countdownStep);
        }
        LoadLvl_0();
    }

    private void SetCountdownText()
    {
        textShowed.text = LocalizationManager.TryGetText(keyTimeLabel, out var label) ? $"{label} {timeCD}s" : $"{keyTimeLabel} {timeCD}s";
    }
}

--- ManageScoreBoard.cs ---
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Localization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ManageScoreBoard : MonoBehaviour
{
    [SerializeField] private Text textBestPlayers;
    [SerializeField] private Text closeTxt;

    [SerializeField] private KeyCode keyEscape = KeyCode.Escape;
    [SerializeField] private KeyCode keyR = KeyCode.R;
    [SerializeField] private KeyCode keyS = KeyCode.S;

    [SerializeField] private string keyCloseText = "scoreboard.close";

    private string pathScores;
    private Dictionary<int, List<string>> bestPlayers = new Dictionary<int, List<string>>();

    private int indexIntroduction = 0;
    private int indexFirstLevel = 3;

    [SerializeField] private float timeToStart = 15;
    private float timeCD;

    private string persDataPath;

    void Awake()
    {
        persDataPath = Application.persistentDataPath;
        pathScores = Path.Combine(persDataPath, "Scores.csv");

        timeCD = (int)timeToStart;
        StartCoroutine(LoseTime());

        // string namePlayer = UserManager.Instance.GetUserName();
        // int scorePlayer = UserManager.Instance.GetCumulatedScore();
        string localizedTemplate = LocalizationManager.TryGetText(keyCloseText, out var temp) ? temp : keyCloseText;
        // string closeText = string.Format(localizedTemplate, namePlayer, scorePlayer);

        ReadBestPlayers();
        // AddNewPlayer(namePlayer, scorePlayer);
        PrintBestPlayers();
        // UserManager.Instance.ScoreZero();
        // closeTxt.text = closeText;
    }

    void Update()
    {
        if (timeCD < 0) Application.Quit();

        if (Input.GetKeyDown(keyEscape)) Application.Quit();
        else if (Input.GetKeyDown(keyR))
        {
            DestroyIndestructible();
            SceneManager.LoadScene(indexIntroduction);
        }
        else if (Input.GetKeyDown(keyS))
        {
            SceneManager.LoadScene(indexFirstLevel);
        }
    }

    void DestroyIndestructible()
    {
        var indestructible = GameObject.FindGameObjectsWithTag("DontDestroyObject");
        foreach (var i in indestructible) Destroy(i);
        var bitalino = GameObject.FindWithTag("BITalino");
        if (bitalino != null) Destroy(bitalino);
    }

    void ReadBestPlayers()
    {
        if (File.Exists(pathScores))
        {
            using (var reader = new StreamReader(pathScores))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] elements = line.Split(';');
                    if (!int.TryParse(elements[0], out int score)) continue;
                    var names = elements.Skip(1).Where(e => !string.IsNullOrEmpty(e)).ToList();
                    bestPlayers[score] = names;
                }
            }
        }
    }

    void AddNewPlayer(string name, int score)
    {
        if (!bestPlayers.ContainsKey(score)) bestPlayers[score] = new List<string>();
        bestPlayers[score].Add(name);
    }

    void PrintBestPlayers()
    {
        var bests = "";
        int i = 0;
        foreach (var kvp in bestPlayers.OrderByDescending(k => k.Key))
        {
            foreach (var player in kvp.Value)
            {
                i++;
                bests += $"{i}. {player} {kvp.Key}\n";
            }
        }
        textBestPlayers.text = bests;
    }

    void WriteBestPlayers()
    {
        using (var writer = new StreamWriter(pathScores, false))
        {
            foreach (var kvp in bestPlayers)
            {
                string line = $"{kvp.Key};" + string.Join(";", kvp.Value) + ";";
                writer.WriteLine(line);
            }
        }
    }

    private System.Collections.IEnumerator LoseTime()
    {
        while (true)
        {
            closeTxt.text = closeTxt.text.Split(new[] { timeCD + "s" }, StringSplitOptions.None)[0] + timeCD + "s";
            timeCD -= 1;
            yield return new WaitForSeconds(1);
        }
    }
}

--- ManageTutorials.cs ---
using System.Collections;
using System.Collections.Generic;
using Localization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ManageTutorials : MonoBehaviour
{
    [SerializeField] private Text textShowed;
    [SerializeField] private Text textCD;
    [SerializeField] private KeyCode skipLevelKey = KeyCode.T;
    [SerializeField] private KeyCode skipTutorialKey = KeyCode.U;
    [SerializeField] private KeyCode quitKey = KeyCode.Escape;
    [SerializeField] private float timeToStart = 30;

    [SerializeField] private string keyTimeLabel = "tutorial.timeLabel";

    private List<string> texts = new List<string>();
    private int index;
    private int timeCD;

    private void Awake()
    {
        timeCD = (int) timeToStart;
        StartCoroutine(LoseTime());
        index = 0;
        UpdateTexts(SceneManager.GetActiveScene().buildIndex);
        if (texts.Count > 0) textShowed.text = texts[index];
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

        if (timeToStart < Time.timeSinceLevelLoad)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

        if (Input.GetKeyDown(quitKey))
            Application.Quit();

        if (Input.GetKeyDown(skipLevelKey))
            StartCoroutine("CountDown");

        else if (Input.GetKeyDown(skipTutorialKey))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
    }

    public void RunGame() => StartCoroutine(CountDown());

    IEnumerator CountDown()
    {
        index = -1;
        for (int i = 3; i >= 0; i--)
        {
            textShowed.text = i.ToString();
            yield return new WaitForSeconds(i == 0 ? 0.25f : 1f);
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void UpdateTexts(int lvl)
    {
        texts.Clear();
        for (int i = 1;; i++)
        {
            string key = $"tutorial.level{lvl}.line{i}";
            if (!LocalizationManager.TryGetText(key, out string localized)) break;
            texts.Add(localized);
        }
    }

    private IEnumerator LoseTime()
    {
        while (true)
        {
            LocalizationManager.TryGetText(keyTimeLabel, out string label);
            string txt = label + timeCD + "s";
            textCD.text = txt;
            timeCD -= 1;
            yield return new WaitForSeconds(1);
        }
    }
}
--- ManageZeroLvl.cs ---
using System.Collections;
using System.Collections.Generic;
using Localization;
using UnityEngine;
using UnityEngine.UI;

public class ManageZeroLvl : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private PlayerShooting playerShooting;
    [SerializeField] private string keyAlertPrefix = "zeroLvl.alert";

    private float startTime = 0.5f;
    private float endTime = 6f;
    private float timer = 0f;
    private bool used = false;
    private int i;

    private List<string> alerts = new List<string>();
    private GameObject[] elementsHUD;

    void Start()
    {
        timer -= startTime;
        i = 0;

        alerts.Clear();
        for (int idx = 1; ; idx++)
        {
            string key = $"{keyAlertPrefix}{idx}";
            string localized;
            if (!LocalizationManager.TryGetText(key, out localized))
            {
                LogManager.Log(Time.time, $"Found {idx - 1} alerts for zero level tutorial.");
                break;
            }
            alerts.Add(localized);
        }
        
        elementsHUD = GameObject.FindGameObjectsWithTag("TurnOff");
        foreach (var el in elementsHUD)
            el.SetActive(false);
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (i < alerts.Count)
            ShowAlerts(endTime - 1.0f, alerts[i]);

        if (i == 2) elementsHUD[1].SetActive(true);
        else if (i == 3) { elementsHUD[2].SetActive(true); elementsHUD[2].GetComponent<Text>().text = "0"; }
        else if (i == 4) elementsHUD[0].SetActive(true);
        else if (i == 5) { playerMovement.enabled = true; cameraFollow.enabled = true; }
        else if (i == 10) playerShooting.enabled = true;
        else if (i == 14)
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var enemy in enemies) enemy.GetComponent<EnemyMovement>().SetSpeed(1);
        }
    }

    void ShowAlerts(float time, string alert)
    {
        if (timer > startTime && !used)
        {
            // StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(time, alert));
            HudPopupTextManager.ShowAlert(alert, time);
            i++;
            used = true;
        }

        if (timer > startTime + time)
        {
            used = false;
            timer = 0f;
        }
    }
}

