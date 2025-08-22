--- BitalinoController.cs ---
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using MathNet.Filtering;
using System.IO;

public class BitalinoController : MonoBehaviour 
{

    [Tooltip("Set via Inspector: bitalinoController")]

    public static BitalinoController bitalinoController;

	[Tooltip("Set via Inspector: ECGChannel")]

	public int ECGChannel = 0;
	[Tooltip("Set via Inspector: EMGChannel")]
	public int EMGChannel = 1;
	[Tooltip("Set via Inspector: EDAChannel")]
	public int EDAChannel = 2;

	[Tooltip("Set via Inspector: bitalinoManager")]

	public BitalinoManager bitalinoManager;
	[Tooltip("Set via Inspector: bitalinoReader")]
	public BitalinoReader bitalinoReader;

	[Range(0f, 10f)]
    [Tooltip("Set via Inspector: cutoffLowpass")]
    public float cutoffLowpass = 5f;
    [Range(0f, 1f)]
    [Tooltip("Set via Inspector: cutoffHighpass")]
    public float cutoffHighpass = 0.67f;

	[Range(0,2000)]
    [Tooltip("Set via Inspector: calibrationSamples")]
    public int calibrationSamples = 1000;
    [Tooltip("Set via Inspector: calibrationPreTime")]
    public float calibrationPreTime = 10;

    [Tooltip("Set via Inspector: EMGAverage")]

    public float EMGAverage;
	[Tooltip("Set via Inspector: EDAAverage")]
	public float EDAAverage;
    [Tooltip("Set via Inspector: HRAverage")]
    public float HRAverage;

	[Tooltip("Set via Inspector: EMGCalibrated")]

	public float EMGCalibrated = 0;
	[Tooltip("Set via Inspector: EDACalibrated")]
	public float EDACalibrated = 0;
    [Tooltip("Set via Inspector: HRCalibrated")]
    public float HRCalibrated = 0;
	bool calibrated;

	float ECGMax;
	float ECGPeakThreshold = 0f;
	float ECGPeakTimer = 0f;
	bool ECGPeakFound = false;
	LinkedList<float> HRs = new LinkedList<float>();
	float HRAv;

    [Tooltip("Set via Inspector: EMGMin")]

    public float EMGMin = 0f;
    [Tooltip("Set via Inspector: EDAMin")]
    public float EDAMin = 0f;
    [Tooltip("Set via Inspector: HRMin")]
    public float HRMin = 0f;
    [Tooltip("Set via Inspector: EMGMax")]
    public float EMGMax = 0f;
    [Tooltip("Set via Inspector: EDAMax")]
    public float EDAMax = 0f;
    [Tooltip("Set via Inspector: HRMax")]
    public float HRMax = 0f;
    
    float cutoffEMGHigher = 3f;
    float cutoffEDALower = 0.2f;
    float cutoffEDAHigher = 0.2f;
    float cutoffHRLower = 15f;
    float cutoffHRHigher = 15f;

    List<float> allHR = new List<float>();
    List<float> allEMG = new List<float>();
    List<float> allEDA = new List<float>();

    List<float> allHRTime = new List<float>();
    List<float> allEMGTime = new List<float>();
    List<float> allEDATime = new List<float>();

    float allTime;

    
    [HideInInspector]
    [Tooltip("Set via Inspector: bitalinoUse")]
    public bool bitalinoUse = false;
        
    [HideInInspector]
    [Tooltip("Set via Inspector: bufferSize")]
    public int bufferSize;

    int zeroLevelIndex = 2;

    bool usedFlag = false;
	public bool FinishedCalibration
    {
        get
        {
            return bitalinoReader.asStart && calibrated;
        }
    }

    void Awake () 
	{
        
        string channels = "";
        string analogs = "";

        foreach (AnalogChannel AC in bitalinoManager.analogAndChannels)
        {
            channels += AC.sensor + ";";
            analogs += AC.analog + ";";
        }

        channels.Remove(channels.Length - 1);
        analogs.Remove(analogs.Length - 1);


        MakeThisTheOnlyDontDestroyManager();
    }
 
    void MakeThisTheOnlyDontDestroyManager()
	{

        if(bitalinoController == null)
		{
            DontDestroyOnLoad(gameObject);
            bitalinoController = this;
        }

        else
		{
            if(bitalinoController != this)
			{
                Destroy (gameObject);
            }
        }
	}
	
	void Update () 
	{
        if (FinishedCalibration && usedFlag)
        {
            usedFlag = true;
        }

		if (FinishedCalibration)
        {
            BITalinoFrame[] buffer = bitalinoReader.getBuffer();

            if (buffer != null && buffer.Length > 0)
            {
                allTime = Time.time;

                float current = EMGCalculateAverage(buffer);
                if (current < cutoffEMGHigher) 
                {
                    EMGAverage = current;
                    allEMG.Add(EMGAverage);
                    allEMGTime.Add(allTime);

                    EMGMin = Mathf.Min(EMGAverage, EMGMin);
                    EMGMax = Mathf.Max(EMGAverage, EMGMax);
                }
				
                allEDATime.Add(allTime);
                EDAAverage = EDACalculateAverage(buffer);
                allEDA.Add(EDAAverage);

                allHRTime.Add(allTime);
                ECGCalculateHR(buffer);
                allHR.Add(HRAverage); //?
                
                if (SceneManager.GetActiveScene().buildIndex >= zeroLevelIndex)
                {
                    if (EDAAverage > (EDACalibrated - cutoffEDALower))
                    {
                        EDAMin = Mathf.Min(EDAAverage, EDAMin);
                    }

                    if (EDAAverage < (EDACalibrated + cutoffEDAHigher))
                    {
                        EDAMax = Mathf.Max(EDAAverage, EDAMax);
                    }

                    if(HRAverage > (HRCalibrated - cutoffHRLower)) 
                    {
                        HRMin = Mathf.Min(HRAverage, HRMin);
                    }

                    if(HRAverage < (HRCalibrated + cutoffHRHigher)) 
                    {
                        HRMax = Mathf.Max(HRAverage, HRMax); 
                    }
                }
            }
        }
	}

	public IEnumerator StartReading()
    {
        bitalinoReader.enabled = true;
        bufferSize = bitalinoReader.BufferSize;

        bitalinoUse = true;

        while (!bitalinoReader.asStart)
		{
			yield return new WaitForSeconds(0.2f);
        }
        yield return new WaitForSeconds(calibrationPreTime);


        int EMGCalibrationSamples = 0;
        int EDAcalibrationSamples = 0;
        int ECGcalibrationSamples = 0;

        float HRAvSum = 0;

        for (int i = 0; i < calibrationSamples ; i++)
        {
            BITalinoFrame[] buffer = bitalinoReader.getBuffer();
            
            if (i < calibrationSamples / 4)
            {
                yield return new WaitForSeconds(0.01f);
            }

            float current = EMGCalculateAverage(buffer);
            if (current < 3f) 
            {
                EMGCalibrated += current;
                EMGCalibrationSamples++;
            }

			EDACalibrated += EDACalculateAverage(buffer);
            EDAcalibrationSamples ++;

            ECGCalculateHR(buffer.ToArray());
		    HRAvSum += HRAverage; //?
            ECGcalibrationSamples++;

            yield return new WaitForSeconds(0.01f);
        }

        EMGCalibrated = EMGCalibrated / EMGCalibrationSamples;
        EDACalibrated = EDACalibrated / EDAcalibrationSamples;
        HRCalibrated = HRAvSum / ECGcalibrationSamples;

        EMGMax = EMGCalibrated;
        EMGMin = EMGCalibrated;
        EDAMin = EDACalibrated;
        EDAMax = EDACalibrated;
        HRMax = HRCalibrated;
        HRMin = HRCalibrated;       

        calibrated = true;
    }

	float EMGCalculateAverage(BITalinoFrame[] buffer)
    {
        return buffer.Select(x => Mathf.Abs((float)x.GetAnalogValue(EMGChannel))).Average();
    }
	float EDACalculateAverage(BITalinoFrame[] buffer)
    {
        return buffer.Select(x => Mathf.Abs((float)x.GetAnalogValue(EDAChannel))).Average();
    }

	double [] ECGCalculateHR (BITalinoFrame[] buffer)
	{
		OnlineFilter filter = OnlineFilter.CreateBandpass(ImpulseResponse.Infinite, bitalinoManager.SamplingFrequency, cutoffLowpass, cutoffHighpass);
		double[] filteredSamples = filter.ProcessSamples(buffer.Select(x => x.GetAnalogValue(ECGChannel)).ToArray());
		double[] valuesHR = new double[buffer.Length];

		float sample = 0f;
        float samplingFrequency = bitalinoManager.SamplingFrequency;

        for (int i = 0; i < buffer.Length; i++)
        {
            sample = (float)filteredSamples[i];

            if (sample > ECGMax)
            {
                ECGMax = sample;
            }

            ECGPeakThreshold = ECGMax * 0.5f;

            if (sample < ECGPeakThreshold && ECGPeakTimer > 1.5f)
            {
                ECGMax -= 0.01f;
            }

            if (sample > ECGPeakThreshold * 1.1f && !ECGPeakFound)
            {
                float HR = 60 / ECGPeakTimer;

                if (HR > 40 && HR < 130)
                {
                    if (HRs.Count >= 100)
                    {
                        HRs.RemoveLast();
                    }

                    HRs.AddFirst(HR);
                    HRAv = HRs.Average();
                }

                ECGPeakTimer = 0;
                ECGPeakFound = true;
            }

            if (sample < ECGPeakThreshold && ECGPeakFound)
            {
                ECGPeakFound = false;
            }

            ECGPeakTimer += 1 / samplingFrequency;

            valuesHR[i] = HRs.Count == 0 ? 0 : HRs.First.Value;
        }

        HRAverage = HRAv;

        return valuesHR;
	}

    void OnDestroy()
    {
        Save(allEDA, allEDATime, "EDA");
        Save(allEMG, allEMGTime, "EMG");
        Save(allHR, allHRTime, "HR");

        SaveStatistics("Statistics");
    }

    void Save(List<float> values, List<float> times, string name)
    {
        StreamWriter writer = File.AppendText(UserManager.userManager.GetUserPath() + name + ".csv");
        for(int i = 0; i < Mathf.Min(values.Count, times.Count); i++)
        {
            writer.WriteLine((times[i] * 1000).ToString() + ";" + values[i].ToString());
        }
        writer.Close();
    }

    void SaveStatistics(string s)
    {
        StreamWriter writer = File.AppendText(UserManager.userManager.GetUserPath() + s + ".csv");
        
        writer.WriteLine(String.Format("HRCalibrated;{0}", HRCalibrated));
        writer.WriteLine(String.Format("HRMax;{0}", HRMax));
        writer.WriteLine(String.Format("HRMin;{0}", HRMin));

        writer.WriteLine(String.Format("EMGCalibrated;{0}", EMGCalibrated));
        writer.WriteLine(String.Format("EMGMax;{0}", EMGMax));
        writer.WriteLine(String.Format("EMGMin;{0}", EMGMin));
        
        writer.WriteLine(String.Format("EDACalibrated;{0}", EDACalibrated));
        writer.WriteLine(String.Format("EDAMax;{0}", EDAMax));
        writer.WriteLine(String.Format("EDAMin;{0}", EDAMin));

        writer.Close();
    }
}
--- CameraChange.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CameraChange : MonoBehaviour {

	[Tooltip("Set via Inspector: cameraMain")]

	public Camera cameraMain;
	[Tooltip("Set via Inspector: player")]
	public GameObject player;
	[Tooltip("Set via Inspector: mainLight")]
	public GameObject mainLight;
	PlayerHealth playerHealth;

	[Tooltip("Set via Inspector: imageCrossHair")]

	public Image imageCrossHair;
	[Tooltip("Set via Inspector: cameraTime")]
	public float cameraTime = 200f;
	[Tooltip("Set via Inspector: waitBeginTime")]
	public float waitBeginTime = 100000f;


	string firstAlertPl = "Utracono połączenie.";
	string secondAlertPl = "Wyłączanie interfejsu.";
	string thirdAlertPl = "Trzymaj się!";
	string firstAlertEng = "Connection lost.";
	string secondAlertEng = "Turning off the interface.";
	string thirdAlertEng = "Hold on!";

	string firstAlertFPSPl = "Przywrócono połączenie.";
	string secondAlertFPSPl = "Przywracanie interfejsu.";
	string thirdAlertFPSPl = "Witaj ponownie!";
	string firstAlertFPSEng = "Connection restored.";
	string secondAlertFPSEng = "Turning on the interface.";
	string thirdAlertFPSEng = "Hello again!";

	string firstAlertFPS = "";
	string secondAlertFPS = "";
	string thirdAlertFPS = "";
	string firstAlert = "";
	string secondAlert = "";
	string thirdAlert = "";


	[Tooltip("Set via Inspector: elementsHUD")]


	public GameObject [] elementsHUD;
	string turnOffTag = "TurnOff";
	float turnOffTime = 0.5f;

	float timer;
	bool fps;

	int currentSceneNumber;
	int thirdLvlIndex = 8;

	bool usedFirst = false;
	bool usedSecond = false;
	bool usedThird = false;

	bool dontChange = false;

	void Awake()
	{	
		if(UserManager.lang.Equals(UserManager.LanguageOption._English))
		{
			firstAlertFPS = firstAlertFPSEng;
			secondAlertFPS = secondAlertFPSEng;
			thirdAlertFPS = thirdAlertFPSEng;

			firstAlert = firstAlertEng;
			secondAlert = secondAlertEng;
			thirdAlert = thirdAlertEng;
		}

		else if(UserManager.lang.Equals(UserManager.LanguageOption._Polish))
		{
			firstAlertFPS = firstAlertFPSPl;
			secondAlertFPS = secondAlertFPSPl;
			thirdAlertFPS = thirdAlertFPSPl;

			firstAlert = firstAlertPl;
			secondAlert = secondAlertPl;
			thirdAlert = thirdAlertPl;
		}

		elementsHUD = GameObject.FindGameObjectsWithTag(turnOffTag);
		currentSceneNumber = SceneManager.GetActiveScene().buildIndex;
		timer = cameraTime - waitBeginTime;
		fps = false;

		playerHealth = player.GetComponent<PlayerHealth>();
	}

	void Update ()
	{















			






	}

	IEnumerator Wait()
	{
		yield return new WaitForSeconds(turnOffTime);
	}

	void changeCam()
	{
		string camerName = fps ? "FPS" : "Izomorphic";


		usedFirst = false;
		usedSecond = false;
		usedThird = false;

		foreach(GameObject element in elementsHUD)
		{
			element.gameObject.SetActive(!fps);
		}

		player.GetComponent<PlayerMovement>().enabled = !fps;
		player.GetComponent<CharacterController>().enabled = fps;

		player.transform.Find("Player").gameObject.GetComponent<SkinnedMeshRenderer>().enabled = !fps;
		player.transform.Find("Gun").gameObject.GetComponent<SkinnedMeshRenderer>().enabled = !fps;

		mainLight.gameObject.SetActive(!fps);
		player.GetComponent<Light>().enabled = fps;

		cameraMain.gameObject.SetActive(!fps);
		player.transform.Find("MainCameraFPS").gameObject.SetActive(fps);

		imageCrossHair.gameObject.GetComponent<Image>().enabled = fps;
	}
}
--- CameraFollow.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

	[Tooltip("Set via Inspector: target")]

	public Transform target;
	[Tooltip("Set via Inspector: smoothing")]
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
using System.Collections.Generic;
using UnityEngine;

public class CamMouseLook : MonoBehaviour {

    [Tooltip("Set via Inspector: speed")]

    public float speed = 5f;
    [Tooltip("Set via Inspector: turn")]
    public float turn = 3f;
    [Tooltip("Set via Inspector: limit_up")]
    public float limit_up = 30;
    [Tooltip("Set via Inspector: limit_down")]
    public float limit_down = 30;
    [Tooltip("Set via Inspector: limit_three")]
    public float limit_three = 60;
    GameObject parentObj;

    Vector3 angles;
 
    void Awake () 
    {
        Cursor.lockState = CursorLockMode.Locked;
        parentObj = this.transform.parent.gameObject;
        
    }

    void FixedUpdate () 
    {
        float xturn = Input.GetAxis("Mouse X");
        float yturn = Input.GetAxis ("Mouse Y");

        parentObj.transform.Rotate (0f, xturn * turn, 0f);
        if (angles.x <= limit_up && angles.x >= limit_down) 
        {
            transform.Rotate (-yturn * turn, 0f, 0f);
        }
        else if(angles.x < limit_down) 
        {
            angles.x = limit_down + 1;
            transform.localEulerAngles = angles;
        }

        else if (angles.x < limit_three) 
        {
            angles.x = limit_up - 1;
            transform.localEulerAngles= angles;
        }

        else
        {
            angles.x = limit_down + 1;
            transform.localEulerAngles = angles;
        }

        angles = transform.localEulerAngles;

    }
}
--- CharacterController.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour 
{
	[Tooltip("Set via Inspector: speed")]
	public float speed = 10f;
	Animator anim;

	[Tooltip("Set via Inspector: keyEscape")]

	public KeyCode keyEscape = KeyCode.Escape;

	void Awake()
    {
        anim = GetComponent<Animator> ();
    }

	void Start () {
		Cursor.lockState = CursorLockMode.Locked;		
	}
	
	void FixedUpdate () {

		float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

		float translation = v * speed;
		float straffe = h * speed;
		translation *= Time.deltaTime;
		straffe *= Time.deltaTime;

		transform.Translate(straffe, 0, translation);

		Animating(h,v);

		if(Input.GetKeyDown(keyEscape))
			Cursor.lockState = CursorLockMode.None;
		
	}

	void Animating (float h, float v)
    {
        bool walking = h != 0f || v != 0f;
        anim.SetBool("IsWalking", walking);
    }
}

--- EnemyAttack.cs ---
using UnityEngine;
using System.Collections;

public class EnemyAttack : MonoBehaviour
{
    [Tooltip("Set via Inspector: timeBetweenAttacks")]
    public float timeBetweenAttacks = 2f;
    [Tooltip("Set via Inspector: attackDamage")]
    public int attackDamage = 10;


    Animator anim;
    GameObject player;
    PlayerHealth playerHealth;
    EnemyHealth enemyHealth;
    bool playerInRange;
    float timer;


    void Awake ()
    {
        player = GameObject.FindGameObjectWithTag ("Player");
        playerHealth = player.GetComponent <PlayerHealth> ();
        enemyHealth = GetComponent<EnemyHealth>();
        anim = GetComponent <Animator> ();
    }


    void OnTriggerEnter (Collider other)
    {
        if(other.gameObject == player)
        {
            playerInRange = true;
        }
    }


    void OnTriggerExit (Collider other)
    {
        if(other.gameObject == player)
        {
            playerInRange = false;
        }
    }


    void Update ()
    {
        timer += Time.deltaTime;

        if(timer >= timeBetweenAttacks && playerInRange && enemyHealth.currentHealth > 0 && playerHealth.currentHealth > 0)
        {
            Attack ();
        }

        if(playerHealth.currentHealth <= 0)
        {
            anim.SetTrigger ("PlayerDead");
        }
    }


    void Attack ()
    {
        timer = 0f;

        if(playerHealth.currentHealth > 0)
        {
            playerHealth.TakeDamage (attackDamage, gameObject.GetInstanceID());
        }
    }
}

--- EnemyHealth.cs ---
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Tooltip("Set via Inspector: startingHealth")]
    public int startingHealth = 100;
    [Tooltip("Set via Inspector: currentHealth")]
    public int currentHealth;
    [Tooltip("Set via Inspector: sinkSpeed")]
    public float sinkSpeed = 0.07f;
    [Tooltip("Set via Inspector: scoreValue")]
    public int scoreValue = 10;
    [Tooltip("Set via Inspector: deathClip")]
    public AudioClip deathClip;


    Animator anim;
    AudioSource enemyAudio;
    ParticleSystem hitParticles;
    CapsuleCollider capsuleCollider;
    [Tooltip("Set via Inspector: isDead")]
    public bool isDead;
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


    void Update ()
    {
        if (isSinking)
        {
            transform.Translate (-Vector3.up * sinkSpeed * Time.deltaTime);
        }
    }

    public void TakeDamageSuper (int amount)
    {
        if(isDead)
            return;

        enemyAudio.Play ();

        currentHealth -= amount;

        if(currentHealth <= 0)
        {
            LogManager.logManager.AddEvent(Time.time, "Enemy;Death;By;SuperPower;ID;" + gameObject.GetInstanceID() + ";PositionX;" + gameObject.transform.position.x + ";PositionY;" + gameObject.transform.position.y + ";PositionZ;" + gameObject.transform.position.z + ";RotationX;" + gameObject.transform.rotation.x + ";RotationY;" + gameObject.transform.rotation.y + ";RotationZ;" + gameObject.transform.rotation.z + ";RotationW;" + gameObject.transform.rotation.w);
            TypicalDeath ();
            AddScore();
        }
    }

    public void TakeDamageLvlEnd (int amount)
    {
        if(isDead)
            return;


        currentHealth -= amount;

        if(currentHealth <= 0)
        {
            LogManager.logManager.AddEvent(Time.time, "Enemy;Death;By;LevelEnd;ID;" + gameObject.GetInstanceID() + ";PositionX;" + gameObject.transform.position.x + ";PositionY;" + gameObject.transform.position.y + ";PositionZ;" + gameObject.transform.position.z + ";RotationX;" + gameObject.transform.rotation.x + ";RotationY;" + gameObject.transform.rotation.y + ";RotationZ;" + gameObject.transform.rotation.z + ";RotationW;" + gameObject.transform.rotation.w);
            Death ();
        }
    }


    public void TakeDamage (int amount, Vector3 hitPoint)
    {
        if(isDead)
            return;

        enemyAudio.Play ();

        currentHealth -= amount;

        LogManager.logManager.AddEvent(Time.time, "Enemy;Health;DecreaseTo;" + currentHealth + ";ID;" + gameObject.GetInstanceID());
            
        hitParticles.transform.position = hitPoint;
        hitParticles.Play();

        if(currentHealth <= 0)
        {
            LogManager.logManager.AddEvent(Time.time, "Enemy;Death;By;Gun;ID;" + gameObject.GetInstanceID() +  ";PositionX;" + gameObject.transform.position.x + ";PositionY;" + gameObject.transform.position.y + ";PositionZ;" + gameObject.transform.position.z + ";RotationX;" + gameObject.transform.rotation.x + ";RotationY;" + gameObject.transform.rotation.y + ";RotationZ;" + gameObject.transform.rotation.z + ";RotationW;" + gameObject.transform.rotation.w);
            TypicalDeath ();
            AddScore();
        }
    }

    void TypicalDeath ()
    {
        isDead = true;

        anim.SetTrigger ("Dead");

        enemyAudio.clip = deathClip;
        enemyAudio.Play ();
    }


    void Death ()
    {
        isDead = true;

        anim.SetTrigger ("Dead");

        enemyAudio.clip = deathClip;
    }


    public void StartSinking ()
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

    public void AddScore()
    {
        ScoreManager.score += scoreValue;
        LogManager.logManager.AddEvent(Time.time, "Score;Update;Value;" + scoreValue);
    }
}

--- EnemyMovement.cs ---
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

--- AffectiveEnemyManager.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AffectiveEnemyManager : MonoBehaviour 
{

	[Tooltip("Set via Inspector: affectiveSpawnTimeMedium")]

	public int affectiveSpawnTimeMedium = 60;
    [Tooltip("Set via Inspector: affectiveSpawnTimeMediumMax")]
    public int affectiveSpawnTimeMediumMax = 80;
	[Tooltip("Set via Inspector: affectiveSpawnTimeHard")]
	public int affectiveSpawnTimeHard = 90;
    [Tooltip("Set via Inspector: affectiveSpawnTimeHardMax")]
    public int affectiveSpawnTimeHardMax = 180;
    [Tooltip("Set via Inspector: preparationTime")]
    public int preparationTime = 5;


	[Tooltip("Set via Inspector: spawnTime")]


	public float spawnTime = 3f;
	[Tooltip("Set via Inspector: invokeTime")]
	public float invokeTime = 5f;
    [Tooltip("Set via Inspector: playerHealth")]
    public PlayerHealth playerHealth;
    [Tooltip("Set via Inspector: enemy")]
    public GameObject[] enemy;
    [Tooltip("Set via Inspector: spawnPoints")]
    public Transform[] spawnPoints;


    bool checkedMedium = false;
    bool checkedHard = false;


    float timerSpawnMax = 0f;
    float remainTime = 5f;

    float alertTime = 5f;

    string alertMessageNonAffectivePl = "Nadchodzą kolejne potwory!\nUważaj!";
    
    
    string alertMessageNonAffectiveEng = "More and more monsters are coming!\nWatch out!";


    string alertMessageNonAffective = "";

    void Awake()
    {
        if(UserManager.lang.Equals(UserManager.LanguageOption._English))
		{
			alertMessageNonAffective = alertMessageNonAffectiveEng;
		}

		else if(UserManager.lang.Equals(UserManager.LanguageOption._Polish))
		{
			alertMessageNonAffective = alertMessageNonAffectivePl;
		}

    }


    void Start ()
    {
        InvokeRepeating ("Spawn", invokeTime, spawnTime);
    }


    void Update()
    {
        if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeMedium - remainTime))
        {
            timerSpawnMax += Time.deltaTime;
        }

            if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeMedium - preparationTime) && !checkedMedium)
            {
                LogManager.logManager.AddEvent(Time.time, "Alert;Show;Time;" + alertTime + ";Content;" + alertMessageNonAffective.Replace("\n", ""));
                StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessageNonAffective));
                checkedMedium = true;
            }

            else if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeHard - preparationTime) && !checkedHard)
            {
                LogManager.logManager.AddEvent(Time.time, "Alert;Show;Time;" + alertTime + ";Content;" + alertMessageNonAffective.Replace("\n", ""));
                StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessageNonAffective));
                checkedHard = true;
            }






                



    }




            
    

    void Spawn ()
    {
        GameObject [] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        if(playerHealth.currentHealth <= 0f || enemies.Length >= EnemyManager.maxEnemies)
        {
            return;
        }

        int enemyIndex = Random.Range (0, enemy.Length);

        if((Time.timeSinceLevelLoad >= affectiveSpawnTimeMedium)) // && !bitalinoUseFlag)     // F2
        {
            int spawnPointIndex = Random.Range (0, (int)(spawnPoints.Length / Random.Range (1, 3)));
            Instantiate (enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
            LogManager.logManager.AddEvent(Time.time, "Enemy;Spawn;Type;" + enemyIndex + ";ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[spawnPointIndex].name + ";Mechanic;" + "AdditionalMediumSpawn");
        }

        if((Time.timeSinceLevelLoad >= affectiveSpawnTimeHard)) // && !bitalinoUseFlag)     // F2
        {
            int spawnPointIndex = Random.Range (0, (int)(spawnPoints.Length / Random.Range (1, 2)));
            Instantiate (enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
            LogManager.logManager.AddEvent(Time.time, "Enemy;Spawn;Type;" + enemyIndex + ";ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[spawnPointIndex].name + ";Mechanic;" + "AdditionalHardSpawn");
        }



    }
}
--- AudioManager.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour 
{
	[Tooltip("Set via Inspector: audioManager")]
	public static AudioManager audioManager;

	bool flagChecked = false;

     void Awake () 
	 {
         MakeThisTheOnlyAudioManager();
     }
 
     void MakeThisTheOnlyAudioManager()
	 {

         if(audioManager == null)
		 {
             DontDestroyOnLoad(gameObject);
             audioManager = this;
         }

         else
		 {
             if(audioManager != this)
			 {
                 Destroy (gameObject);
             }
         }
	}

	void Update () 
	{
		if ((SceneManager.GetActiveScene().buildIndex >= 1) && !flagChecked)
		{
			flagChecked = true;
		}
	}
}

--- EndManager.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndManager : MonoBehaviour {

	void Start () {
		
	}
	
	void Update () {
		
	}
}

--- EnemyManager.cs ---
using System.IO;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
	[Tooltip("Set via Inspector: spawnTime")]
	public float spawnTime = 3f;
	[Tooltip("Set via Inspector: invokeTime")]
	public float invokeTime = 5f;
	[Tooltip("Set via Inspector: playerHealth")]
	public PlayerHealth playerHealth;
	[Tooltip("Set via Inspector: enemy")]
	public GameObject enemy;
	[Tooltip("Set via Inspector: spawnPoints")]
	public Transform[] spawnPoints;

	[Tooltip("Set via Inspector: maxEnemies")]

	public static int maxEnemies = 8;

	string persDataPath;

 

    void Start ()
    {
        persDataPath = Application.persistentDataPath;
 


        if (File.Exists(persDataPath + "\\enemiesAmount.txt")) 
        {
            StreamReader readtext = new StreamReader(persDataPath + "\\enemiesAmount.txt");
            maxEnemies = int.Parse(readtext.ReadLine());
            readtext.Close();
        }
        else
        {
            maxEnemies = 8;
        }

        InvokeRepeating("Spawn", invokeTime, spawnTime);
    }


    void Spawn()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (playerHealth.currentHealth <= 0f || enemies.Length >= maxEnemies)
        {
            return;
        }

        int spawnPointIndex = Random.Range(0, spawnPoints.Length);

        Instantiate(enemy, spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
        LogManager.logManager.AddEvent(Time.time, "Enemy;Spawn;Type;" + "-1" + ";ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[spawnPointIndex].name + ";Mechanic;" + "RegularSpawn");
    }
}

--- GameOverManager.cs ---
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    [Tooltip("Set via Inspector: playerHealth")]
    public PlayerHealth playerHealth;

    [Tooltip("Set via Inspector: textGO")]

    public Text textGO;
    float timeCD = 3.0f;

    string textGOv;
    string textGOvPL = "Przegrałeś!\nRestart poziomu za: ";
    string textGOvENG = "You lost!\nThe level will restart in: ";

    UserManager.LanguageOption lang;

    Animator anim;
    string nameAnimation = "GameOver";
    
    string nameScoreBoard = "ScoreBoard";

    bool flagUsed = false;

    void Awake()
    {
        anim = GetComponent<Animator>();

        lang = UserManager.lang;

        if(lang.Equals(UserManager.LanguageOption._Polish))
        {
            textGOv = textGOvPL; 
        }

        else if(lang.Equals(UserManager.LanguageOption._English))
        {
            textGOv = textGOvENG; 
        }

        textGO.text = textGOv;

    }


    void Update()
    {
        if (playerHealth.currentHealth <= 0)
        {
            if (timeCD < 0)
            {
                UserManager.userManager.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
                LogManager.logManager.AddEvent(Time.time, "Score;PlayerDeath;Level;" + SceneManager.GetActiveScene().buildIndex + ";Value;" + ScoreManager.score);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   
            }

            if(!flagUsed)
            {
                flagUsed = true;
                anim.SetTrigger(nameAnimation);
                LogManager.logManager.AddEvent(Time.time, "Game;Over;Animation;PlayerDeath");
                timeCD = 4.0f;
	            StartCoroutine("LoseTime");
            }

            if (Input.GetKey(KeyCode.B))
            {
                UserManager.userManager.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
                LogManager.logManager.AddEvent(Time.time, "Key;B");
                SceneManager.LoadScene(nameScoreBoard);
            }

        }
    }

    IEnumerator LoseTime()
	{
		while(true)
		{
            string txt = textGOv + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;Over;CountDown;Text;ChangeTo;" + txt.Replace("\n", ""));
            yield return new WaitForSeconds(1);
			textGO.text = txt;
			timeCD -= 1;
		}
	}
}

--- ImportantAlertManager.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImportantAlertManager : MonoBehaviour 
{

	[Tooltip("Set via Inspector: importantAlertManager")]

	public static ImportantAlertManager importantAlertManager;

  	void Awake () 
	{
         MakeThisTheOnlyDontDestroyManager();
    }
 
    void MakeThisTheOnlyDontDestroyManager()
	{

        if(importantAlertManager == null)
		{
            DontDestroyOnLoad(gameObject);
            importantAlertManager = this;
        }

        else
		{
            if(importantAlertManager != this)
			{
                Destroy (gameObject);
            }
        }
	}

	public IEnumerator ShowAlertAndLerp(float time, string input)
	{
		Text txt = gameObject.GetComponent<Text>();
		txt.enabled = true;

		txt.text = input;

		Color startingColor = Color.green;
		txt.color = startingColor;

		if (time == 0.5f)
		{
			time = 1f;
		}

		else
		{
			time -= 0.5f;
		}

    	float inversedTime = 1 / time;
		for(float step = 0.0f; step < 1.0f; step += Time.deltaTime * inversedTime)
    	{
			txt.color = Color.Lerp(startingColor, Color.clear, step);
    	    yield return null;
    	}

		txt.enabled = false;
	}

	public IEnumerator ShowAlert(float time, string input)
	{
		Text txt = gameObject.GetComponent<Text>();
		txt.enabled = true;

		txt.text = input;

		yield return new WaitForSeconds(time);

		txt.enabled = false;
	}
}
--- LogManager.cs ---
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LogManager : MonoBehaviour
{
    [Tooltip("Set via Inspector: logManager")]
    public static LogManager logManager;

    string fileName = "events.csv";

    double t1 = 0f;

    struct FmOEvent
    {
        float time;
        string eventType;

        public FmOEvent(float t, string eT)
        {
            time = t;
            eventType = eT;
        }

        public float GetTime()
        {
            return time;
        }

        public string GetEventType()
        {
            return eventType;
        }
    }

    List<FmOEvent> events = new List<FmOEvent>();

    void Awake () 
	{
        t1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        MakeThisTheOnlyDontDestroyManager();
    }
 
    void MakeThisTheOnlyDontDestroyManager()
	{
        if(logManager == null)
		{
            DontDestroyOnLoad(gameObject);
            logManager = this;
        }

        else
		{
            if(logManager != this)
			{
                Destroy (gameObject);
            }
        }
	}

    void OnDestroy()
    {
        StreamWriter writer = File.AppendText(UserManager.userManager.GetUserPath() + fileName);

        writer.WriteLine(t1.ToString() + ";" + "UnixTime");

        foreach (FmOEvent e in events)
        {
            writer.WriteLine((e.GetTime()) + ";" + e.GetEventType());
        }

        writer.Close();
    }

    public void AddEvent(float time, string eventType)
    {
        FmOEvent newEvent = new FmOEvent((time * 1000), eventType); // *1000 to get time in miliseconds
        events.Add(newEvent);
    }
}
--- MrNightmareEnemyManger.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MrNightmareEnemyManger : MonoBehaviour {

	[Tooltip("Set via Inspector: playerHealth")]

	public PlayerHealth playerHealth;
	[Tooltip("Set via Inspector: mrNightmareHealth")]
	public EnemyHealth mrNightmareHealth;
	[Tooltip("Set via Inspector: affectiveEnemyManager")]
	public AffectiveEnemyManager affectiveEnemyManager;
	[Tooltip("Set via Inspector: enemy")]
	public GameObject[] enemy;
    [Tooltip("Set via Inspector: spawnPoints")]
    public Transform[] spawnPoints;

	[Tooltip("Set via Inspector: healthLvlOne")]

	public int healthLvlOne = 900;
	[Tooltip("Set via Inspector: healthLvlTwo")]
	public int healthLvlTwo = 600;
	[Tooltip("Set via Inspector: healthLvlThree")]
	public int healthLvlThree = 300;
	[Tooltip("Set via Inspector: healthLvlFour")]
	public int healthLvlFour = 30;

	bool usedLvlOne = false;
	bool usedLvlTwo = false;
	bool usedLvlThree = false;
	bool usedLvlFour = false;

	
	[Tooltip("Set via Inspector: hellpNumberLvlOne")]

	
	public int hellpNumberLvlOne = 5;
	[Tooltip("Set via Inspector: hellpNumberLvlTwo")]
	public int hellpNumberLvlTwo = 7;
	[Tooltip("Set via Inspector: hellpNumberLvlThree")]
	public int hellpNumberLvlThree = 9;
	[Tooltip("Set via Inspector: hellpNumberLvlFour")]
	public int hellpNumberLvlFour = 11;
	[Tooltip("Set via Inspector: hellpWaitLvlOne")]
	public float hellpWaitLvlOne = 3f;
	[Tooltip("Set via Inspector: hellpWaitLvlTwo")]
	public float hellpWaitLvlTwo = 2f;
	[Tooltip("Set via Inspector: hellpWaitLvlThree")]
	public float hellpWaitLvlThree = 4f;
	[Tooltip("Set via Inspector: hellpWaitLvlFour")]
	public float hellpWaitLvlFour = 1f;

	float alertTime = 5f;
	string alertMessagePl = "Przyzwano sojuszników! Uważaj!";
	string alertMessageEng = "Supporters are coming! Watch out!";

	string alertMessage = "";

	float timerLvlThree = 0f;
	float timerLvlTwo = 0f;
	float timerLvlFour = 0f;
	bool enemySpeedFlag = false;
	bool affectiveSpawnMediumFlag = false;
	bool affectiveSpawnHardFlag = false;


	void Awake()
	{
		if(UserManager.lang.Equals(UserManager.LanguageOption._English))
		{
			alertMessage = alertMessageEng;
		}

		else if(UserManager.lang.Equals(UserManager.LanguageOption._Polish))
		{
			alertMessage = alertMessagePl;
		}
	}

	void Update ()
	{
		Spawn();
	}


    void Spawn ()
    {
		if (usedLvlFour)
		{
			timerLvlFour += Time.deltaTime;
		}
		else if (usedLvlThree)
		{
			timerLvlThree += Time.deltaTime;
		}
		else if (usedLvlTwo)
		{
			timerLvlTwo += Time.deltaTime;
		}


        if(playerHealth.currentHealth <= 0f || mrNightmareHealth.currentHealth > healthLvlOne || mrNightmareHealth.currentHealth <= 0f)
        {
            return;
        }

		else if (usedLvlFour && timerLvlFour > (hellpWaitLvlFour + 1f) && !affectiveSpawnHardFlag)
		{
			affectiveSpawnHardFlag = true;
			affectiveEnemyManager.invokeTime = 0f;
			affectiveEnemyManager.affectiveSpawnTimeHard = (int)Time.timeSinceLevelLoad;
			affectiveEnemyManager.affectiveSpawnTimeHardMax = (int)Time.timeSinceLevelLoad + 20;
		}

		else if (mrNightmareHealth.currentHealth <= healthLvlFour && !usedLvlFour)
		{
			usedLvlFour = true;
			StartCoroutine(CallForHellp(hellpNumberLvlFour, hellpWaitLvlFour));

			LogManager.logManager.AddEvent(Time.time, "Alert;Show;Time;" + alertTime + ";Content;" + alertMessage.Replace("\n", ""));
			StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessage));
		}

		else if (usedLvlThree && timerLvlThree > (hellpWaitLvlThree + 1f) && !enemySpeedFlag)
		{
			enemySpeedFlag = true;
		}

		else if (mrNightmareHealth.currentHealth <= healthLvlThree && !usedLvlThree)
		{
			usedLvlThree = true;
			timerLvlThree = 0f;
			StartCoroutine(CallForHellp(hellpNumberLvlThree, hellpWaitLvlThree));

			LogManager.logManager.AddEvent(Time.time, "Alert;Show;Time;" + alertTime + ";Content;" + alertMessage.Replace("\n", ""));
			StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessage));
		}

		else if (usedLvlTwo && timerLvlTwo > (hellpWaitLvlTwo + 1f) && !affectiveSpawnMediumFlag)
		{
			affectiveSpawnMediumFlag = true;
			affectiveEnemyManager.invokeTime = 0f;
			affectiveEnemyManager.affectiveSpawnTimeMedium = (int)Time.timeSinceLevelLoad;
			affectiveEnemyManager.affectiveSpawnTimeMediumMax = (int)Time.timeSinceLevelLoad + 20;
		}

		else if (mrNightmareHealth.currentHealth <= healthLvlTwo && !usedLvlTwo)
		{
			usedLvlTwo = true;
			StartCoroutine(CallForHellp(hellpNumberLvlTwo, hellpWaitLvlTwo));

			LogManager.logManager.AddEvent(Time.time, "Alert;Show;Time;" + alertTime + ";Content;" + alertMessage.Replace("\n", ""));
			StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessage));
		}

		else if (mrNightmareHealth.currentHealth <= healthLvlOne && !usedLvlOne)
		{
			usedLvlOne = true;
			StartCoroutine(CallForHellp(hellpNumberLvlOne, hellpWaitLvlOne));

			LogManager.logManager.AddEvent(Time.time, "Alert;Show;Time;" + alertTime + ";Content;" + alertMessage.Replace("\n", ""));
			StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessage));
		}
	}

	IEnumerator CallForHellp(int number, float waitTime)
    {
        int i = 0;
        while(i < number)
        {
			i++;
	        int enemyIndex = Random.Range (0, enemy.Length);
			int spawnPointIndex = Random.Range (0, spawnPoints.Length);
            
			Instantiate (enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
			LogManager.logManager.AddEvent(Time.time, "Enemy;Spawn;Type;" + enemyIndex + ";ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[spawnPointIndex].name + ";Mechanic;" + "MrNightmareSpawn");

			yield return new WaitForSeconds(waitTime);
        }
    }
}
--- ScoreManager.cs ---
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEditor;
public class ScoreManager : MonoBehaviour
{
    [Tooltip("Set via Inspector: score")]
    public static int score;
    [Tooltip("Set via Inspector: scoreToLvlUp")]
    public int scoreToLvlUp = 300;

    Text text;

    float timer = 0f;
    float waitTime = 3f;

    int zeroLevel = 2;
    float zeroLevelWait = 105f;

    UserManager.LanguageOption lang;
    [Tooltip("Set via Inspector: keyP")]
    public KeyCode keyP = KeyCode.P;

    void Awake ()
    {
        LogManager.logManager.AddEvent(Time.time, "Scene;Load;ID;" + SceneManager.GetActiveScene().buildIndex);

        lang = UserManager.lang;

        text = GetComponent <Text> ();
        

        if (SceneManager.GetActiveScene().buildIndex > 2)
        {
            scoreToLvlUp = Random.Range((scoreToLvlUp - 100), scoreToLvlUp);
            LogManager.logManager.AddEvent(Time.time, "Score;ToLevelUp;Value;" + scoreToLvlUp);
        }

        score = 0;
    }

    void Update ()
    {
        if(Input.GetKey(keyP))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;P");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        text.text = score.ToString();


        




        if (SceneManager.GetActiveScene().buildIndex != zeroLevel && score >= scoreToLvlUp)
        {
            GameObject [] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
                EnemyHealth enemyHealth = enemy.GetComponent <EnemyHealth> ();
                enemyHealth.TakeDamageLvlEnd(enemyHealth.currentHealth);
            }

            timer += Time.deltaTime;

            if(timer > waitTime)
            {
                UserManager.userManager.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, score);
                LogManager.logManager.AddEvent(Time.time, "Score;LvlEnd;Level;" + SceneManager.GetActiveScene().buildIndex + ";Value;" + ScoreManager.score);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }

        if (SceneManager.GetActiveScene().buildIndex == zeroLevel && Time.timeSinceLevelLoad > zeroLevelWait)
        {
            GameObject [] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            GameObject [] pickups = GameObject.FindGameObjectsWithTag("PickUp");
            
            if (enemies.Length == 0 || pickups.Length == 0)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }

        }

    }
}

--- UserManager.cs ---
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UserManager : MonoBehaviour 
{
	[Tooltip("Set via Inspector: userManager")]
	public static UserManager userManager;

    public enum LanguageOption
    {
		_English = 0,
		_Polish = 1
	};

    [Tooltip("Set via Inspector: lang")]

    public static LanguageOption lang = LanguageOption._Polish;

    string userName; 
    string userID;

    string persDataPath;
    string userPath;

    int lvls = 0;
    int [] scoreLvl;

    bool used;

    void Start () 
	{
        persDataPath = Application.persistentDataPath;
        userPath = persDataPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\";

        Cursor.visible = false;
		Screen.fullScreen = true;

        userID = "0000";
        userName = "Zawodnik0000";

        if (File.Exists(persDataPath + "\\currentNumber.txt")) 
        {
            StreamReader readtext = new StreamReader(persDataPath + "\\currentNumber.txt");
            userID = readtext.ReadLine();
            userName = "Zawodnik" + userID;
            readtext.Close();
        }

		LogManager.logManager.AddEvent(Time.time, "UserName;" + userName);
		LogManager.logManager.AddEvent(Time.time, "UserID;" + userID);

        userPath += userID + "\\";
        Directory.CreateDirectory(userPath);


        lvls = SceneManager.sceneCountInBuildSettings;
        scoreLvl = new int[lvls];
        Array.Clear(scoreLvl, 0, scoreLvl.Length);

        used = false;

        MakeThisTheOnlyUserManager();
    }

    void Update()
    {
        if (Time.time > 1200 && !used)
        {   
            used = true;
            ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
            LogManager.logManager.AddEvent(Time.time, "Score;GameEnd;Level;" + SceneManager.GetActiveScene().buildIndex + ";Value;" + ScoreManager.score);
            SceneManager.LoadScene("ScoreBoard");
        }
    }


    void MakeThisTheOnlyUserManager()
    {

        if(userManager == null)
        {
            DontDestroyOnLoad(gameObject);
            userManager = this;
        }

        else
        {
            if(userManager != this)
            {
                Destroy (gameObject);
            }
        }
	}





    public void ScoreZero()
    {
        Array.Clear(scoreLvl, 0, scoreLvl.Length);   
    }

    public void ScoreUpdate(int index, int score)
    {
        if (score > scoreLvl[index])
        {
            scoreLvl[index] = score;
        }

    }

    public int GetCumulatedScore()
    {
        scoreLvl[2] = 0; // main tutorial should not affect score
        return scoreLvl.Sum();
    }

    public string GetUserName()
    {
        return userName;
    }

    public string GetUserNumber()
    {
        return userID;
    }

    public string GetUserPath()
    {
        return userPath;
    }

    public void ChangeLanguage(int x)
    {


        lang = (LanguageOption)x;
    }

    [Tooltip("Set via Inspector: stimuliMinus")]

    public static List<String> stimuliMinus = new List<string>();
    [Tooltip("Set via Inspector: stimuliPlus")]
    public static List<String> stimuliPlus = new List<string>();

    void ReadStimuli()
	{
		StreamReader readtext = new StreamReader("Assets/Resources/Affective/auditory.csv");

		while (readtext.Peek() >= 0)
		{
			string [] line = readtext.ReadLine().Split(',');

			switch (line[0])
			{
				case "s-":
					
					stimuliMinus.Add(line[1]);
					break;

				case "s+":
					
					stimuliPlus.Add(line[1]);
					break;
			}
		}

		readtext.Close();

		Shuffle(stimuliMinus);
        Shuffle(stimuliPlus);
	}

	void Shuffle<T>(List<T> ts) {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i) {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }
}
--- PickUpCollect.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpCollect : MonoBehaviour {

	int scoreValue = 2;
	bool collected;

	int speedUp = 2;
	int transformPoint = 8;

	AudioSource pickUpAudio;


	void Awake() {
		collected = false;
		pickUpAudio = GetComponent <AudioSource> ();
	}

	void Start () {
		
	}
	
	void Update () {

		if(collected)
		{
			transform.Translate(Vector3.up * speedUp * Time.deltaTime, Space.World);
			if (transform.position.y >= transformPoint)
			{
				LogManager.logManager.AddEvent(Time.time, "PickUp;Destroyed;ID;" + gameObject.GetInstanceID());
				Destroy(gameObject);
			}
		}	
	}

	void OnTriggerEnter(Collider col)
    {
        if(col.gameObject.name == "Player" && !collected)
        {
			pickUpAudio.Play();
			
			ScoreManager.score += scoreValue;
			LogManager.logManager.AddEvent(Time.time, "Score;Update;Value;" + scoreValue);

			LogManager.logManager.AddEvent(Time.time, "PickUp;Collected;ID;" + gameObject.GetInstanceID());

			collected = true;

        }
    }
	
}

--- PickUpManager.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpManager : MonoBehaviour {

	[Tooltip("Set via Inspector: spawnTime")]

	public float spawnTime = 7f;
    [Tooltip("Set via Inspector: invokeTime")]
    public float invokeTime = 5f;
    [Tooltip("Set via Inspector: playerHealth")]
    public PlayerHealth playerHealth;
    [Tooltip("Set via Inspector: pickUp")]
    public GameObject pickUp;
    [Tooltip("Set via Inspector: spawnPoints")]
    public Transform[] spawnPoints;

	int spawnPointIndex;


    void Start ()
    {
		RandomizeArray(spawnPoints);
		spawnPointIndex = 0;	
        	
        StartCoroutine("Spawn");
    }

    IEnumerator Spawn()
    {
        yield return new WaitForSeconds(5.0f);
    
        while(true)
        {
            if(playerHealth.currentHealth <= 0f)
            {
                break;
            }

            Instantiate (pickUp, spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
            LogManager.logManager.AddEvent(Time.time, "PickUp;Spawn;ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[spawnPointIndex].name);

            spawnPointIndex++;
            if (spawnPointIndex >= spawnPoints.Length)
            {
                spawnPointIndex = 0;   
            }
            yield return new WaitForSeconds(7.0f);
        }
    }

	void RandomizeArray<T> (T[] arr)
	{
		for (int i = arr.Length - 1; i > 0; i--) 
		{
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
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEditor;

public class PlayerHealth : MonoBehaviour
{
    [Tooltip("Set via Inspector: startingHealth")]
    public int startingHealth = 100;
    [Tooltip("Set via Inspector: currentHealth")]
    public int currentHealth;
    [Tooltip("Set via Inspector: healthRegeneration")]
    public int healthRegeneration = 5;
    [Tooltip("Set via Inspector: regenerationTime")]
    public float regenerationTime = 10f;
    [Tooltip("Set via Inspector: healthSlider")]
    public Slider healthSlider;
    [Tooltip("Set via Inspector: damageImage")]
    public Image damageImage;
    [Tooltip("Set via Inspector: deathClip")]
    public AudioClip deathClip;
    [Tooltip("Set via Inspector: flashSpeed")]
    public float flashSpeed = 5f;
    [Tooltip("Set via Inspector: flashColour")]
    public Color flashColour = new Color(1f, 0f, 0f, 0.1f);
    [Tooltip("Set via Inspector: keyEscape")]
    public KeyCode keyEscape = KeyCode.Escape;


    Animator anim;
    AudioSource playerAudio;
    PlayerMovement playerMovement;
    PlayerShooting playerShooting;
    [Tooltip("Set via Inspector: isDead")]
    public bool isDead;
    bool damaged;

    float timer;

    int disguisedHealth;



    








    void Awake ()
    {


        
        timer = 0f;
        anim = GetComponent <Animator> ();
        playerAudio = GetComponent <AudioSource> ();
        playerMovement = GetComponent <PlayerMovement> ();
        playerShooting = GetComponentInChildren <PlayerShooting> ();
        currentHealth = startingHealth;
        disguisedHealth = startingHealth;
        LogManager.logManager.AddEvent(Time.time, "Player;Health;StartingValue;" + currentHealth);
        LogManager.logManager.AddEvent(Time.time, "Slider;Health;StartingValue;" + currentHealth);
    }

    void Update ()
    {


        if (Input.GetKey(keyEscape))
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
		}


        timer += Time.deltaTime;
        if (timer > regenerationTime && currentHealth < startingHealth)
        {
            currentHealth += healthRegeneration;
            if(currentHealth > startingHealth)
            {
                currentHealth = startingHealth;
            }
            healthSlider.value  = currentHealth;

            LogManager.logManager.AddEvent(Time.time, "Player;Health;IncreaseTo;" + currentHealth);
            LogManager.logManager.AddEvent(Time.time, "Slider;Health;DecreaseTo;" + currentHealth);

            
            timer = 0f;
        }

        if(damaged)
        {
            damageImage.color = flashColour;
        }
        else
        {
            damageImage.color = Color.Lerp (damageImage.color, Color.clear, flashSpeed * Time.deltaTime);
        }
        damaged = false;

        if(isDead)
        {
            playerMovement.enabled = false; //for third lvl, should add more here

            if(Input.GetKeyDown(KeyCode.R))
            {
                LogManager.logManager.AddEvent(Time.time, "Key;R");
                SceneManager.LoadScene (SceneManager.GetActiveScene().buildIndex - 1); // load tutorial of current level
            }

            else if (Input.GetKeyDown(KeyCode.C))
            {
                LogManager.logManager.AddEvent(Time.time, "Key;C");
                Application.Quit(); // ignored in UnityEditor
            }
        } 

    }




            






    public void TakeDamage (int amount, int i)
    {
        timer = 0f;

        damaged = true;

        currentHealth -= amount;
        healthSlider.value = currentHealth;
        
        LogManager.logManager.AddEvent(Time.time, "Player;Health;DecreaseTo;" + currentHealth + ";By;Enemy;ID;" + i);
        LogManager.logManager.AddEvent(Time.time, "Slider;Health;DecreaseTo;" + currentHealth);


        playerAudio.Play ();

        if(currentHealth <= 0 && !isDead)
        {
            Death ();
        }
    }


        




        


    void Death ()
    {
        LogManager.logManager.AddEvent(Time.time, "Player;Death;" +  "PositionX;" + gameObject.transform.position.x + ";PositionY;" + gameObject.transform.position.y + ";PositionZ;" + gameObject.transform.position.z + ";RotationX;" + gameObject.transform.rotation.x + ";RotationY;" + gameObject.transform.rotation.y + ";RotationZ;" + gameObject.transform.rotation.z + ";RotationW;" + gameObject.transform.rotation.w);
        
        isDead = true;

        playerShooting.DisableEffects ();

        anim.SetTrigger ("Die");

        playerAudio.clip = deathClip;
        playerAudio.Play ();

        playerMovement.enabled = false;
        playerShooting.enabled = false;
    }
}

--- PlayerMovement.cs ---
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Tooltip("Set via Inspector: speed")]
    public float speed = 6f;
    [Tooltip("Set via Inspector: speedRotor")]
    public float speedRotor = 2f;

    Vector3 movement;

    Vector3 rotate;
    Animator anim;
    Rigidbody playerRigidbody;
    int  floorMask;
    float camRayLength = 100f;    

    void Awake()
    {
        floorMask = LayerMask.GetMask("Floor");
        anim = GetComponent<Animator> ();
        playerRigidbody = GetComponent<Rigidbody> ();
    }

    void FixedUpdate() //called every physics step
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float r = Input.GetAxisRaw("Mouse X");
        float t = Input.GetAxisRaw("Mouse Y");

        LogManager.logManager.AddEvent(Time.time, "Joystick;Left;Horizontal;" + h + ";Vertical;" + v);
        LogManager.logManager.AddEvent(Time.time, "Joystick;Right;Horizontal;" + r + ";Vertical;" + t);

        TurningAndMoving(r, t, h, v);
        Animating(h, v);
    }



    void TurningAndMoving(float r, float t, float h, float v)
    {
        float r_pos = 0.0f;
        float t_pos = 0.0f;

        if(Mathf.Abs(r) > 0.01f)
        {
            r_pos = r;
        }

        if(Mathf.Abs(t) > 0.01f)
        {
            t_pos = t;
        }

        rotate.Set(0.0f, r_pos, 0.0f);
        

        movement.Set(h, 0f, v);
        movement = movement.normalized * speed *  Time.deltaTime;

        Quaternion deltaRotation = Quaternion.Euler(rotate * Time.deltaTime * speedRotor * 1000);
        playerRigidbody.MoveRotation(playerRigidbody.rotation * deltaRotation);

        playerRigidbody.MovePosition(transform.position + movement);
    
    }
    void Turning()
    {
        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit floorHit;

        if(Physics.Raycast(camRay, out floorHit, camRayLength, floorMask))
        {
            Vector3 playerToMouse = floorHit.point - transform.position;
            playerToMouse.y = 0f;

            Quaternion newRotation = Quaternion.LookRotation(playerToMouse);
            playerRigidbody.MoveRotation(newRotation);
        }
    }

    void Animating (float h, float v)
    {
        bool walking = h != 0f || v != 0f;
        anim.SetBool("IsWalking", walking);
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
    [Tooltip("Set via Inspector: damagePerShot")]
    public int damagePerShot = 20;
    [Tooltip("Set via Inspector: damagePerShotMin")]
    public int damagePerShotMin = 10;
    [Tooltip("Set via Inspector: damagePerShotMax")]
    public int damagePerShotMax = 20;
    [Tooltip("Set via Inspector: timeBetweenBullets")]
    public float timeBetweenBullets = 0.2f;
    [Tooltip("Set via Inspector: timeBetweenBulletsMin")]
    public float timeBetweenBulletsMin = 0.2f;
    [Tooltip("Set via Inspector: timeBetweenBulletsMax")]
    public float timeBetweenBulletsMax = 0.8f;
    [Tooltip("Set via Inspector: range")]
    public float range = 100f;
    [Tooltip("Set via Inspector: rangeMin")]
    public float rangeMin = 40f;
    [Tooltip("Set via Inspector: rangeMax")]
    public float rangeMax = 100f;
    [Tooltip("Set via Inspector: ultraPower")]
    public float ultraPower = 100;
    [Tooltip("Set via Inspector: ultraPowerMax")]
    public float ultraPowerMax = 100;

    [Tooltip("Set via Inspector: enemy")]

    public GameObject enemy;
    [Tooltip("Set via Inspector: enemies")]
    public GameObject[] enemies;
    [Tooltip("Set via Inspector: powerSlider")]
    public Slider powerSlider;
    [Tooltip("Set via Inspector: superpowerImage")]
    public Image superpowerImage;

    [Tooltip("Set via Inspector: shootButtonR2")]

    public string shootButtonR2 = "R2";

    float timer = 0f;
    Ray shootRay = new Ray();
    RaycastHit shootHit;
    int shootableMask;
    ParticleSystem gunParticles;
    LineRenderer gunLine;
    AudioSource gunAudio;
    Light gunLight;
    float effectsDisplayTime = 0.2f;
    
    [Tooltip("Set via Inspector: timePressed")]
    
    public float timePressed = 0f;
    float timePressedMax = 5f;
    bool wasPressed = false;


    float regenerationTimer = 0f;
    float regenerationTime = 10f;

    bool superpowered = false;

    float timeAlpha = 0f;
    float duration = 10000000; //lerp
    float smoothness = 0.02f; //lerp

    float ultraPowerRegeneration = 5f;
    float rangeRenegeration = 10f;
    float timeBetweenBulletsRegeneration = 0.1f;
    int damagePerShotRegeneration = 1;


    int currentSceneNumber;
    int secondLevelIndex = 5;


    void Awake ()
    {
		currentSceneNumber = SceneManager.GetActiveScene().buildIndex;

        shootableMask = LayerMask.GetMask ("Shootable");
        gunParticles = GetComponent<ParticleSystem> ();
        gunLine = GetComponent <LineRenderer> ();
        gunAudio = GetComponent<AudioSource> ();
        gunLight = GetComponent<Light> ();
        powerSlider.value = ultraPower;


        LogManager.logManager.AddEvent(Time.time, "Player;Range;StartingValue;" + range);
        LogManager.logManager.AddEvent(Time.time, "Player;TimeBetweenTwoBullets;StartingValue;" + timeBetweenBullets);
        LogManager.logManager.AddEvent(Time.time, "Player;DamagePerShot;StartingValue;" + damagePerShot);
        LogManager.logManager.AddEvent(Time.time, "Player;SuperPower;StartingValue;" + ultraPower);
        LogManager.logManager.AddEvent(Time.time, "Slider;SuperPower;StartingValue;" + ultraPower);
    }


    void Update ()
    {
        timer += Time.deltaTime;
        regenerationTimer += Time.deltaTime;

		if(Input.GetButton(shootButtonR2) && timer >= timeBetweenBullets && Time.timeScale != 0) // configurable shoot button
        {
            LogManager.logManager.AddEvent(Time.time, "Key;" + "R2");
            Shoot ();
        }

        if(timer >= timeBetweenBullets * effectsDisplayTime)
        {
            DisableEffects ();
        }

        if(regenerationTimer > regenerationTime && ultraPower < ultraPowerMax)
        {
            ultraPower += ultraPowerRegeneration;
            if(ultraPower > ultraPowerMax)
            {
                ultraPower = ultraPowerMax;
            }
            powerSlider.value = ultraPower;
            LogManager.logManager.AddEvent(Time.time, "Slider;SuperPower;IncreaseTo;" + ultraPower);

            timeBetweenBullets -= timeBetweenBulletsRegeneration;
            if(timeBetweenBullets < timeBetweenBulletsMin)
            {
                timeBetweenBullets = timeBetweenBulletsMin;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;TimeBetweenTwoBullets;DecreaseTo;" + timeBetweenBullets);

            damagePerShot += damagePerShotRegeneration;
            if(damagePerShot > damagePerShotMax)
            {
                damagePerShot = damagePerShotMax;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;DamagePerShot;IncreaseTo;" + damagePerShot);

            range += rangeRenegeration;
            if(range > rangeMax)
            {
                range = rangeMax;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;Range;IncreaseTo;" + range);

            regenerationTimer = 0f;
        }

        if (currentSceneNumber >= secondLevelIndex)
        {

                KeyPressedTimer();
                UseSuperPower(timePressed, timePressedMax, wasPressed);
                wasPressed = false;
        }
    }

    void UseSuperPower(float timeSuper, float timeMax, bool was)
    {
        if((timeSuper > 0 && was)  && ultraPower > 0)
        {
            LogManager.logManager.AddEvent(Time.time, "Player;SuperPower;Time;" + timeSuper);

            float ultraPowerused = ultraPower * (Math.Min(timeSuper, timeMax) / timeMax);
            ultraPower -= ultraPowerused;
            powerSlider.value = ultraPower;
            LogManager.logManager.AddEvent(Time.time, "Slider;SuperPower;DecreaseTo;" + ultraPower);

            timeBetweenBullets += timeBetweenBulletsRegeneration;
            if(timeBetweenBullets > timeBetweenBulletsMax)
            {
                timeBetweenBullets = timeBetweenBulletsMax;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;TimeBetweenTwoBullets;IncreaseTo;" + timeBetweenBullets);

            damagePerShot -= damagePerShotRegeneration;
            if(damagePerShot < damagePerShotMin)
            {
                damagePerShot = damagePerShotMin;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;DamagePerShot;DecreaseTo;" + damagePerShot);

            range -= rangeRenegeration;
            if(range < rangeMin)
            {
                range = rangeMin;
            }
            LogManager.logManager.AddEvent(Time.time, "Player;Range;DecreaseTo;" + range);
            
            timeAlpha = timeSuper;
            regenerationTimer = 0;

            timeSuper = 0;
            was = false;

            enemies = GameObject.FindGameObjectsWithTag("Enemy");

            int killed = UnityEngine.Random.Range (0, Math.Min(2 * (int)ultraPowerused, enemies.Length));

            LogManager.logManager.AddEvent(Time.time, "Player;SuperPower;Killed;" + killed);

            for(int i = 0; i < killed; i++)
            {
                enemy = enemies[i];
                EnemyHealth enemyHealth = enemy.GetComponent <EnemyHealth> ();
                if (i > 4)
                {
                    enemyHealth.AddScore();
                    enemyHealth.TakeDamageLvlEnd(enemyHealth.currentHealth);
                }

                else{
                    enemyHealth.TakeDamageSuper(enemyHealth.currentHealth);
                }

            }

            superpowered = true;

        }

        if(superpowered)
        {
            float alpha = Math.Min(timeAlpha, timeMax)/timeMax;
            LogManager.logManager.AddEvent(Time.time, "Player;SuperPower;Alpha;" + alpha);
            Color flashColour = new Color(1f, 0.588235f, 0f, alpha);
            superpowerImage.color = flashColour;
            StartCoroutine(LerpColor());
        }
        superpowered = false;

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

    void KeyPressedTimer()
    {
        if(Input.GetButtonDown("L2"))
        {
            timePressed = Time.time;
        }
         
        if(Input.GetButtonUp("L2") && !wasPressed)
        {
            timePressed = Time.time - timePressed;
            wasPressed = true;
            LogManager.logManager.AddEvent(Time.time, "Key;L2;Time;" + timePressed);
        }
    }



    public void DisableEffects ()
    {
        gunLine.enabled = false;
        gunLight.enabled = false;
    }


    void Shoot ()
    {
        timer = 0f;

        gunAudio.Play ();

        gunLight.enabled = true;

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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ManageEnd : MonoBehaviour {

	[Tooltip("Set via Inspector: textShowed")]

	public Text textShowed;

	[Tooltip("Set via Inspector: textCD")]

	public Text textCD;
	[Tooltip("Set via Inspector: keyEscape")]
	public KeyCode keyEscape = KeyCode.Escape;
	[Tooltip("Set via Inspector: keySkipToScoreBoard")]
	public KeyCode keySkipToScoreBoard = KeyCode.T;
    
	[Tooltip("Set via Inspector: OnScoreBoardRequested")]
    
	public UnityEvent OnScoreBoardRequested;

	[Tooltip("Text label shown before the countdown (English)")]
	[Tooltip("Set via Inspector: timeLabelENG")]
	public string timeLabelENG = "Time to show results: ";
	[Tooltip("Text label shown before the countdown (Polish)")]
	[Tooltip("Set via Inspector: timeLabelPL")]
	public string timeLabelPL = "Czas za jaki pokażemy Ci wyniki: ";

	[Tooltip("Set via Inspector: timeToStart")]

	public float timeToStart = 15f;
	int timeCD;
	int index;
	string nameScoreBoard = "ScoreBoard";

	UserManager.LanguageOption currentLang;


	string currentTimeLabel;

	string txtTimeENG;
	string txtTimePL;



	 void Awake()
	{
		currentLang = UserManager.lang;
		currentTimeLabel = (currentLang == UserManager.LanguageOption._English) ? timeLabelENG : timeLabelPL;
		txtTimeENG = timeLabelENG;
		txtTimePL = timeLabelPL;

		timeCD = (int)timeToStart;
		StartCoroutine(LoseTime());

		LogManager.logManager.AddEvent(Time.time, "Load;Scene;ID" + SceneManager.GetActiveScene().buildIndex);

		index = 0;

	}

    void Update()
    {
        if (CheckIfTimeToScoreBoard()) TriggerScoreBoard();

        if (CheckIfEscapePressed())
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Esc");
			Application.Quit(); // ignored in UnityEditor
		}

        if (CheckIfSkipToScoreBoard())
        {
            LogManager.logManager.AddEvent(Time.time, "Key;T");
        	TriggerScoreBoard();
        }

        if (currentLang != UserManager.lang)
        {
            currentLang = UserManager.lang;
            currentTimeLabel = (currentLang == UserManager.LanguageOption._English) ? timeLabelENG : timeLabelPL;
            textCD.text = currentTimeLabel + timeCD + "s";
        }

    }

    public void ShowScoreBoard() 
    {   
        OnScoreBoardRequested?.Invoke();
        if (OnScoreBoardRequested == null || OnScoreBoardRequested.GetPersistentEventCount() == 0)
        {
            SceneManager.LoadScene(nameScoreBoard);
        }
	}

    IEnumerator LoseTime()
	{
		while(true)
		{
            string txt = currentTimeLabel.Replace("Time to show results:", "Time:") + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;End;CountDown;Text;ChangeTo;" + txt);
            yield return new WaitForSeconds(1);
			textCD.text = txt;
			timeCD -= 1;
		}
	}

    bool CheckIfTimeToScoreBoard() { return timeCD < 0; }
    bool CheckIfEscapePressed() { return Input.GetKey(keyEscape); }
    bool CheckIfSkipToScoreBoard() { return Input.GetKeyDown(keySkipToScoreBoard); }

    void TriggerScoreBoard()
    {
        OnScoreBoardRequested?.Invoke();
        if (OnScoreBoardRequested == null || OnScoreBoardRequested.GetPersistentEventCount() == 0)
        {
            SceneManager.LoadScene(nameScoreBoard);
        }
    }
}

--- ManageIntroduction.cs ---
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ManageIntroduction : MonoBehaviour {

	[Tooltip("Set via Inspector: textShowed")]

	public Text textShowed;

	[Tooltip("Set via Inspector: OnIntroductionFinished")]

	public UnityEvent OnIntroductionFinished;
	[Tooltip("Set via Inspector: OnLanguageChanged")]
	public UnityEvent OnLanguageChanged;
	[Tooltip("Set via Inspector: fireButton2")]
	public string fireButton2 = "Fire2"; // mapped to English welcome progression
	[Tooltip("Set via Inspector: fireButton3")]
	public string fireButton3 = "Fire3"; // mapped to Polish progression
	[Tooltip("Set via Inspector: keyS")]
	public KeyCode keyS = KeyCode.S;     // quick-load level 0 shortcut

	[Tooltip("Welcome text for English")]
	[Tooltip("Set via Inspector: welcomeTextEng")]
	public string welcomeTextEng = "Welcome in the Freud2.0!";
	[Tooltip("Welcome text for Polish")]
	[Tooltip("Set via Inspector: welcomeTextPl")]
	public string welcomeTextPl = "Witaj w Freud2.0!";

	[Tooltip("Time label when the game will start (English)")]
	[Tooltip("Set via Inspector: timeLabelENG")]
	public string timeLabelENG = "Time to level begin: ";
	[Tooltip("Time label when the game will start (Polish)")]
	[Tooltip("Set via Inspector: timeLabelPL")]
	public string timeLabelPL = "Czas do rozpoczęcia poziomu: ";

	[Tooltip("Set via Inspector: timeToStart")]

	public float timeToStart = 30;
	[Tooltip("Set via Inspector: timeWait")]
	public float timeWait = 1f;
	int timeCD;
	string txtTime = "";
	UserManager.LanguageOption lang;
 
	[SerializeField] public LanguageOption currentLang;
	[System.Serializable]
	public enum LanguageOption { English, Polish }
	[Tooltip("Set via Inspector: OnCalibrationSkip")]
	public UnityEvent OnCalibrationSkip;

	[SerializeField] public string txtTimePL;
	[SerializeField] public string txtTimeENG;

	[Tooltip("Welcome text shown on startup")]
	[Tooltip("Set via Inspector: welcomeTextLabelENG")]
	public string welcomeTextLabelENG = "Welcome in the Freud2.0!";
	[Tooltip("Welcome text shown on startup (Polish)")]
	[Tooltip("Set via Inspector: welcomeTextLabelPL")]
	public string welcomeTextLabelPL = "Witaj w Freud2.0!";


	[Tooltip("Set via Inspector: timeCalibrationCheck")]


	public float timeCalibrationCheck = 0.5f;

	void Awake()
    {
        timeCD = (int)timeToStart;
	    StartCoroutine(LoseTime());

        LogManager.logManager.AddEvent(Time.time, "Scene;Load;" + SceneManager.GetActiveScene().buildIndex);

        lang = UserManager.lang;
        ChangeLanguage();
    }

    void Update() 
    {
        if(Input.GetButtonDown (fireButton2)) // use public mapping for Fire2
        {
            LogManager.logManager.AddEvent(Time.time, "Key;X");
            lang = UserManager.LanguageOption._English;
            UserManager.lang = lang;
            ChangeLanguage();
            OnLanguageChanged?.Invoke();
        }

        if(Input.GetButtonDown (fireButton3)) // use public mapping for Fire3
        {
            LogManager.logManager.AddEvent(Time.time, "Key;O");
            lang = UserManager.LanguageOption._Polish;
            UserManager.lang = lang;
            ChangeLanguage();
            OnLanguageChanged?.Invoke();
        }

        if (Input.GetKeyDown(keyS)) // use public key binding for S
		{
            LogManager.logManager.AddEvent(Time.time, "Key;S");
			LoadLvl0();
		}

        if (CheckIfTimeToStartElapsed()) LoadLvl0();

        if (Input.GetKeyDown(KeyCode.Escape))
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
		}

        if (lang != UserManager.lang)
        {
            lang = UserManager.lang;
            ChangeLanguage();
            OnLanguageChanged?.Invoke();
        }
    }

    void ChangeLanguage()
    {
        switch(lang)
        {
            case UserManager.LanguageOption._English:
            {
                textShowed.text = welcomeTextLabelENG;
                currentLang = UserManager.LanguageOption._English;
                currentLang = UserManager.LanguageOption._English;
                currentTimeLabel = timeLabelENG;
                txtTimeENG = timeLabelENG;
                break;
            }

            case UserManager.LanguageOption._Polish:
            {
                textShowed.text = welcomeTextLabelPL;
                currentLang = UserManager.LanguageOption._Polish;
                currentTimeLabel = timeLabelPL;
                txtTimePL = timeLabelPL;
                break;
            }
        }
    }

	public void LoadTutorial ()
    {
        TurnOffButtons();
        StartCoroutine(Wait());
        OnIntroductionFinished?.Invoke();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 3);
	}

	public void LoadLvl0 ()
    {
        TurnOffButtons();
        StartCoroutine(Wait());
        OnIntroductionFinished?.Invoke();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
	}

    bool CheckIfTimeToStartElapsed() { return timeToStart < Time.timeSinceLevelLoad; }
    void TurnOffButtons () { /* keep; minimal cleanup placeholder for now */ }
    IEnumerator Wait() { yield return new WaitForSeconds(timeWait); }

	IEnumerator LoseTime()
	{
		while(true)
		{
            string toShow = txtTime + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;Introduction;CountDown;Text;ChangeTo;" + toShow);
			textShowed.text = toShow;
			timeCD -= 1;
			yield return new WaitForSeconds(1);
		}
	}
}

--- ManageScoreBoard.cs ---
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;

public class ManageScoreBoard : MonoBehaviour 
{
	[Tooltip("Set via Inspector: textBestPlayers")]
	public Text textBestPlayers;

	[Tooltip("Set via Inspector: closeTxt")]

	public Text closeTxt;
	[Tooltip("Set via Inspector: keyEscape")]
	public KeyCode keyEscape = KeyCode.Escape;
	[Tooltip("Set via Inspector: keyR")]
	public KeyCode keyR = KeyCode.R;
	[Tooltip("Set via Inspector: keyS")]
	public KeyCode keyS = KeyCode.S;
	
	string pathScores;
	Dictionary<int, List<string>> bestPlayers = new Dictionary<int, List<string>> ();

	string tagIndestructible = "DontDestroyObject";
	string tagBITalino = "BITalino";

	int indexIntroduction = 0;
	int indexFirstLevel = 3;

	[Tooltip("Set via Inspector: timeToStart")]

	public float timeToStart = 15;

	float timeCD;

	string closeText = "";

	string persDataPath;

	void Awake()
	{
		persDataPath = Application.persistentDataPath;
		pathScores = persDataPath + "\\Scores.csv";
		
		timeCD = (int)timeToStart;
	    StartCoroutine("LoseTime");

		string namePlayer = UserManager.userManager.GetUserName();
		int scorePlayer = UserManager.userManager.GetCumulatedScore();

		if(UserManager.lang == UserManager.LanguageOption._English)
		{
			closeText = String.Format("<color=red>Your name: {0}.\n Your score: {1}.\n</color> The game will end in: ", namePlayer, scorePlayer);
		}

		else if(UserManager.lang == UserManager.LanguageOption._Polish)
		{
			closeText = String.Format("<color=red>Twoja nazwa: {0}.\n Twój wynik: {1}.\n</color> Gra zakończy się za: ", namePlayer, scorePlayer);
		}

		ReadBestPlayers();
		AddNewPlayer(namePlayer, scorePlayer);
		PrintBestPlayers();
		UserManager.userManager.ScoreZero();
		LogManager.logManager.AddEvent(Time.time, "ScoreBoard;Show");
		WriteBestPlayers();

		
	}

	void Update()
	{
		if (timeCD < 0)
        {
            Application.Quit();
        }

		if(Input.GetKeyDown(keyEscape))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
        }

		else if(Input.GetKeyDown(keyR))
        {
			LogManager.logManager.AddEvent(Time.time, "Key;R");
			DestroyIndestructible();
	        SceneManager.LoadScene(indexIntroduction);
        }

		else if(Input.GetKeyDown(keyS))
        {
			LogManager.logManager.AddEvent(Time.time, "Key;S");
	        SceneManager.LoadScene(indexFirstLevel);
        }
	}

	void DestroyIndestructible()
	{
		GameObject [] indestructible = GameObject.FindGameObjectsWithTag(tagIndestructible);
		foreach (GameObject i in indestructible)
		{
			Destroy(i);
		}

		Destroy(GameObject.FindWithTag(tagBITalino));
	}

	void ReadBestPlayers()
	{
		if (File.Exists(pathScores))
		{
			StreamReader reader = new StreamReader(pathScores);
			string line;
			while((line = reader.ReadLine()) != null)  
			{
				string[] elements = line.Split(';');

				int score = 0;
				Int32.TryParse(elements[0], out score);

				List<string> names = new List<string> ();
				for (int i = 1; i < (elements.Length - 1); i++)
				{
					names.Add(elements[i]);
				}

				bestPlayers.Add(score, names);
			}

			reader.Close();
		}
	}


	void AddNewPlayer(string cN, int cS)
	{
		string currentName = cN; // + " (" + use + ")";     // F2
		int currentScore = cS;

		if (!bestPlayers.ContainsKey(currentScore))
		{
			List<string> players = new List<string> ();
			players.Add(currentName);
			bestPlayers.Add(currentScore, players);
		}

		else
		{
			List<string> players = new List<string> ();
			players = bestPlayers[currentScore];
			bestPlayers.Remove(currentScore);
			players.Add(currentName);
			bestPlayers.Add(currentScore, players);
		}
	}


	void PrintBestPlayers()
	{
		string bests = "";
		int i = 0;

		List<int> keyList = bestPlayers.Keys.ToList();
		keyList.Sort();
		keyList.Reverse();
		
		foreach (int key in keyList)
		{
			foreach (string el in bestPlayers[key])
			{
				i++;
				bests += String.Format("{0}. {1} {2}\n" , i, el, key); 
			}
		}

		textBestPlayers.text = bests;
	}


	void WriteBestPlayers()
	{
		if(File.Exists(pathScores))
		{
			File.Delete(pathScores);
		}

		StreamWriter writer = new StreamWriter(pathScores);

		List<int> keyList = bestPlayers.Keys.ToList();
		foreach (int key in keyList)
		{
			string bests = "";
			foreach (string el in bestPlayers[key])
			{
				bests += String.Format("{0};", el);
			}

			string line = String.Format("{0};", key);
			line += bests;
			
			writer.WriteLine(line);
		}

		writer.Close();
	}

	IEnumerator LoseTime()
	{
		while(true)
		{
			string txt = closeText + timeCD.ToString() + "s";
			LogManager.logManager.AddEvent(Time.time, "Game;ScoreBoard;CountDown;Text;ChangeTo;" + txt);

            yield return new WaitForSeconds(1);
			closeTxt.text = txt;
			timeCD -= 1;
		}
	}
}

--- ManageTutorials.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEditor;

public class ManageTutorials : MonoBehaviour 
{

	[Tooltip("Set via Inspector: textShowed")]

	public Text textShowed;

    [Tooltip("Set via Inspector: textCD")]

    public Text textCD;
    
    [Tooltip("Set via Inspector: keyS")]
    
    public KeyCode keyS = KeyCode.S;
    [Tooltip("Set via Inspector: keyEscape")]
    public KeyCode keyEscape = KeyCode.Escape;
    [Tooltip("Set via Inspector: keyT")]
    public KeyCode keyT = KeyCode.T;
    [Tooltip("Set via Inspector: keyU")]
    public KeyCode keyU = KeyCode.U;
    
	List<string> texts = new List<string> ();
    int index;


    UserManager.LanguageOption lang;

    string txtNextButtonPl = "Następny";
    string txtPreviousButtonPl = "Poprzedni";
    string txtPlayButtonPl = "Graj!";
    string txtNextButtonEng = "Next";
    string txtPreviousButtonEng = "Previous";
    string txtPlayButtonEng = "Play!";

    [Tooltip("Set via Inspector: timeToStart")]

    public float timeToStart = 30;

	int timeCD;

    string txtTime = "";
    string txtTimePL = "Czas do rozpoczęcia poziomu: ";

    string txtTimeENG = "Time to level begin: ";



    public void changeText (int x) 
    {
        this.index += x;
		
	}

    void Awake()
    {
        LogManager.logManager.AddEvent(Time.time, "Scene;Load;ID;" + SceneManager.GetActiveScene().buildIndex);

        lang = UserManager.lang;

        timeCD = (int)timeToStart;
	    StartCoroutine("LoseTime");
        


        this.index = 0;


        texts = new List<string> ();
        int currentSceneNumber = SceneManager.GetActiveScene().buildIndex;
        TextsUpdate(currentSceneNumber);
        textShowed.text = texts[this.index];

    }

    void Update()
    {
        if (Input.GetKeyDown(keyS))
		{
            LogManager.logManager.AddEvent(Time.time, "Key;S");
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
		}

        if(timeToStart < Time.timeSinceLevelLoad)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }


        if (Input.GetKeyDown(keyEscape)) // use public binding for Escape
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
		}





        if (Input.GetKeyDown(keyT))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;T");
            StartCoroutine("CountDown");
        }

        else if (Input.GetKeyDown(keyU))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;U");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
        }

    }

    public void runGame () 
    {   
        StartCoroutine("CountDown");
	}

    IEnumerator CountDown()
    {
        this.index = -1;

        textShowed.text = "3".ToString();
        yield return new WaitForSeconds(1);

        textShowed.text = "2".ToString();
        yield return new WaitForSeconds(1);

        textShowed.text = "1".ToString();
        yield return new WaitForSeconds(1);
        
        textShowed.text = "0".ToString();
        yield return new WaitForSeconds(0.25f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    void TextsUpdate(int lvl)
    {

            if(lang.Equals(UserManager.LanguageOption._Polish))
            {
                txtTime = txtTimePL;

                if (lvl == 1)
                {
                    texts.Add("Witamy na klejnej sesji.\nCo tak niewyraźnie wyglądasz?\nNie poznajesz nas. Ok, w porządku. Zaczniemy od początku.");
                    texts.Add("Przyszedłeś tutaj ze względu na problemy z pamięcią, a my jesteśmy Twoimi terapeutami.\nBrzmi znajomo? Nie? No, dobra.");
                    texts.Add("Tutaj jest nasza umowa, a tutaj Twój podpis. Zaznaczyliśmy najważniejszy fragment.\n\n\"Zgadzam się na niekonwencjonalne metody terapii psychologicznej.\"\n" + UserManager.userManager.GetUserName());
                    texts.Add("Ok, skoro już wszystko jasne, przypomnimy Ci kilka zagadnień z psychologii.\nNasza terapia opiera się na psychoanalizie, której autorem jest Zygmunt Freud.");
                    texts.Add("W jego koncepcji psychika działa na trzech poziomach. Świadomości, przedświadomości i nieświadomości.\nNajwiększy wpływ na późniejsze życie ma dzieciństwo.");
                    texts.Add("No, więc właśnie tam sie udajemy.\nDo dzieciństwa poprzez wszystkie trzy poziomy.\nOdpalaj sprzęt i lecimy, nie ma czasu! ");
                    texts.Add("Przez cały czas będziemy w kontakcie.\nNie bój się, robiliśmy to miliony razy.");
                }

                else if (lvl == 3)
                {
                    texts.Add("Witaj ponownie!\nNo, następnym razem lepiej się pospiesz.\nWiesz ile te terapie kosztują w dzisiejszych czasach.\nTym razem udasz się do świadomości.\nJest to najbardziej zewnętrzny poziom.\nOznacza to, że twoja kontrola będzie największa.\nNa tym poziomie znowu spotkasz swoje ulubione zajączki,\nktóre tak chętnie dokarmiałeś sałatą.\nNie wiemy, co im się stało.\nTo Twoja głowa.\nW każdym razie zwykłą sałatą na pewno ich nie przekupisz.\nPamiętaj, że twoje zdrowie się regeneruje!");
                }

                else if (lvl == 5)
                {
                    texts.Add("No, no, gratulacje!\n\"Musimy zejść głębiej!\"\nPamiętasz Incepcję? A, no fakt. Masz probelmy z pamięcią.\nMusimy to sobie gdzieś zapisać.\nTeraz pora na przedświadomość. Czyli wszystko, co tłumisz.\nKażdy płacz, szukanie pocieszenia w objęciach ulubionego misia.\nTylko, że teraz te misie nie są już takie słodkie.\nAle masz Supermoc! Wystarczy, że przytrzymasz przycisk L2 z tyłu pada (lewy trigger)!\nPrzez kilka sekund, maksymalnie pięć.\nIm dłużej, tym więcej wrogów powalisz.\nNiestety, użycie supermocy na pewno Cię trochę zmęczy.\nNie będziesz mieć siły na takie szybkie i mocne strzały.\nPo czasie, wszystko wróci do normy. Zaczynajmy!");
                }

                else if (lvl == 7)
                {
                    texts.Add("Wow, to było wspaniałe! Robisz postępy!\nChyba pora na nieświadomość.\nSą tutaj wszystkie zdarzenia, które wyparłeś z górnych warstw.\nWśród nich Twoje spotkanie ze słoniami.\nDobrze, że nic Ci się wtedy nie stało. Było blisko.\nSłonie trochę zdziczały od tego czasu. Są bardzo niebezpieczne.\nTo w końcu nieświadomość.\nPamiętaj o tym, że Supermoc się regeneruje!\nPowodzenia!");
                }

                else if (lvl == 9)
                {
                    texts.Add("Ktoś tu chce pozbyć się swoich lęków!\nI to nam się podoba!\nTak, tak! Dobrze rozumiesz,\nTwoje problemy z pamięcią wiążą się bezpośrednio\nz Twoimi największym lękami.\nWiesz, co to oznacza.\nPan Koszmarek.\nSiedzi tam już od dłuższego czasu.\nPora wykurzyć go z mieszkania i zaznać trochę spokoju.\nPamietaj, że wszystkie potworki popierają jego rządy.\nJeśli go skrzywdzisz, przybędą mu na pomoc.");
                }
            }

            else if(lang.Equals(UserManager.LanguageOption._English))
            {
                txtTime = txtTimeENG;

                if (lvl == 1)
                {
                    texts.Add("Welcome to the next session.\nWhy so agitated?\nYou do not recognize us...\nThat is alright.\nWe will start from the beginning.");
                    texts.Add("You have come here because of your troubles with memory,\nand we are your therapists.\nSounds familiar? No?\nHave a look at this, then.");
                    texts.Add("Here is our agreement, and this is your signature. We have highlighed the most important part.\n\n\"I hereby agree for\nunconventional methods of\npsychological treatment.\"\n" + UserManager.userManager.GetUserName());
                    texts.Add("Ok, if everything is clear now,\nlet’s remind you of some\npsychological basics.\nOur therapy is grounded in psychoanalisys, developed by Sigmund Freud");
                    texts.Add("According to his theory, human psychology works on three levels: conscious, preconscious,\nand unconscious.\nThe greatest influence on later life comes from one’s childhood.");
                    texts.Add("So, this is where we are heading now.\nTo the childhood,\nthrough all three levels.\nGet your gear and get going,\nthere is no time to waste!");
                    texts.Add("We will be in touch all the time.\nWorry not, we have been doing this\na million times.");
                }

                else if (lvl == 3)
                {
                    texts.Add("Hello again!\nWell, next time better hurry up, will you?\nYou know, these therapies are expensive these days.\nThis time you’ll go to the consciousness.\nIt is the outermost level.\n This means that your control will be least impaired.\nOn this level, you’ll meet your favourite bunnies,\nwhich you fed lettuce so willingly.\nWe don’t know, what happened to them.\nIt’s your head, after all.\nAnyway, regular lettuce won’t work on them now.\nRemember that your health regenerates!");
                }

                else if (lvl == 5)
                {
                    texts.Add("Well, well, congratulations!\n\"We have to go deeper!\"\nRemember that movie, Inception? Ah, right. You have memory issues.\nWe have to write this down.\nNow it’s time for the preconsciousness. This is everything, that you suppress.\nEvery tear, every need of consolation in your favourite teddybear’s embrace.\nBut those teddys aren’t that cute anymore.\nBut you have your Superpower! You just have to press L2 (left trigger)!\n For a few seconds, 5 tops.\nThe longer, the more enemies you’ll strike down.\nSadly, every use of Superpower will tire you for a bit.\nYou won’t be able to shoot that fast and hard.\nIt will go back to normal after a while, though. Let’s begin!");
                }

                else if (lvl == 7)
                {
                    texts.Add("Wow, that was amazing! You’re making progress!\nIt’s time for the unsconsciousness.\nHere lay all the events, that you pushed away from the upper levels.\nIncluding your encounter with elephants.\nGood thing that you got out alright that time. That was close.\nElephants went wild a bit from that time. They’re very dangerous.\nIt’s the unconsciousness, after all.\nRemember that your Superpower regenerates!\nGood luck!");
                }

                else if (lvl == 9)
                {
                    texts.Add("Somebody wants to fight his fears!\nAnd that’s the attitude we like!\nYes, yes! You got it right.\nYour memory troubles are related directly \nto your biggest phobias.\n You know what it means.\nMister Nightmare.\nHe’s been sitting there for a long time.\nIt’s time to drive him out once and for all, and get some peace.\nRemember, all other monsters support his reign.\nIf you hurt him, they will rush to his aid.");
                }
            }
    }

    IEnumerator LoseTime()
	{
		while(true)
		{
            string txt = txtTime + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;TextLevel;CountDown;Text;ChangeTo;" + txt);
			textCD.text = txt;
			timeCD -= 1;
			yield return new WaitForSeconds(1);
		}
	}
}

--- ManageZeroLvl.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ManageZeroLvl : MonoBehaviour 
{

	[Tooltip("Set via Inspector: playerMovement")]

	public PlayerMovement playerMovement;
	[Tooltip("Set via Inspector: cameraFollow")]
	public CameraFollow cameraFollow;
	[Tooltip("Set via Inspector: playerShooting")]
	public PlayerShooting playerShooting;

	float startTime = 0.5f;
	float endTime = 6f;

	float timer = 0f;

	bool used = false;

	int i;
	List<string> alerts = new List<string> ();

	string tagTurnOff = "TurnOff";
	GameObject [] elementsHUD;

	UserManager.LanguageOption lang;

	void Awake () 
	{
		timer -= startTime;
		i = 0;

		lang = UserManager.lang;

        if (lang.Equals(UserManager.LanguageOption._English))
        {
            alerts.Add("Hello, it’s us again! \nWe’ll explain some things to you.");
            alerts.Add("This element indicates your health.");
            alerts.Add("This one shows your score.");
            alerts.Add("And here you can see your Superpower level.\nBut more on this later.");
            alerts.Add("We now unlock your movement.\nUse the analogue sticks!");
            alerts.Add("Left stick is for movement, \nright is for turning around.");
            alerts.Add("Go on, get moving! \nJust don’t break anything!");
            alerts.Add("In order to collect points you can pick up\n yellow shiny stars. \nJust walk through them.");
            alerts.Add("You can also kill the enemies.");
            alerts.Add("Just press \nR2 on the back of the game pad (right trigger). \nTo finish them off, shoot a few times.");
            alerts.Add("Remember, it’s all in your head. \nYou’ll never run out of ammo.");
            alerts.Add("You get less points for stars than \nfor killing the enemies.");
            alerts.Add("And pay attention to the score. \nIt keeps changing, you know.");
            alerts.Add("To get to the next level, \nyou need to collect required points.");
            alerts.Add("We won’t tell you how many exactly, though.");
            alerts.Add("To complete the training, collect all stars \nor kill all enemies.");
            alerts.Add("These ones are harmless. \nBut you better watch out for the real ones!");
            alerts.Add("The darker they are,\nthe stronger they hit you.");
            alerts.Add("Good luck and see you soon!");
        }
        else if (lang.Equals(UserManager.LanguageOption._Polish))
        {
            alerts.Add("Cześć, to znowu my!\nWyjaśnimy Ci kilka kwestii.");
			alerts.Add("Ten element wskazuje Twoje życie.");
			alerts.Add("Ten pokaże Ci Twój wynik.");
			alerts.Add("A tutaj widzisz SuperMoc.\nAle o tym będzie później.");
			alerts.Add("Odblokowujemy możliwość poruszania się.\nUżyj joysticków kontrolera!");
			alerts.Add("Lewy joystick to ruch,\nprawy joystick to obrót.");
			alerts.Add("No, śmiało, ruszaj się!\nTylko niczego nie zepsuj!");
			alerts.Add("Aby zdobywać punkty możesz zbierać\n żółte, świecące gwiazdki.\nPo prostu w nie wejdź.");
			alerts.Add("Możesz też zabijać przeciwników.");
			alerts.Add("Po prostu naciśnij\nprzycisk R2 z tyłu pada (prawy trigger).\nAby ich wykończyć, strzel kilka razy.");
			alerts.Add("Pamiętaj, że to Twoja głowa.\nAmunicja nie skończy się nigdy.");
			alerts.Add("Za gwiazdki dostaniesz mniej punktów niż\nza eliminowanie przeciwników.");
			alerts.Add("I zwróć uwagę na informację o punktach.\nBo wiesz, ona się zmienia.");
			alerts.Add("Aby awansować na kolejne poziomy\nmusisz zebrać wystarczającą liczbę punktów.");
			alerts.Add("Ale nie powiemy Ci ile.");
			alerts.Add("Aby zakończyć szkolenie zbierz wszystkie gwiazdki\nlub zniszcz wszystkich przeciwników.");
			alerts.Add("Ci tutaj są niegroźni.\nAle na prawdziwych lepiej uważaj!");
			alerts.Add("Im są ciemniejsi,\ntym są silniejsi.");
			alerts.Add("Powodzenia i do zobaczenia niedługo!");
        }

		elementsHUD = GameObject.FindGameObjectsWithTag(tagTurnOff);
		foreach (var el in elementsHUD)
		{
			el.gameObject.SetActive(false);
		}
	}
	
	void Update () 
	{
		timer += Time.deltaTime;
		
		if(i < alerts.Count)
		{
			ShowAlerts(endTime - 1.0f, alerts[i]);
		}

		if (i == 2)
		{
			elementsHUD[1].gameObject.SetActive(true);
		}

		else if (i == 3)
		{
			elementsHUD[2].gameObject.SetActive(true);
			elementsHUD[2].GetComponent<Text>().text = "0";

		}

		else if (i == 4)
		{
			elementsHUD[0].gameObject.SetActive(true);
		}

		else if(i == 5)
		{
			playerMovement.enabled = true;
			cameraFollow.enabled = true;
		}

		else if (i == 10)
		{
			playerShooting.enabled = true;	
		}

		else if (i == 14)
		{
			GameObject [] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
				enemy.GetComponent<EnemyMovement>().SetSpeed(1);
            }
		}
	}

	void ShowAlerts(float time, string alert)
	{
		if (timer > startTime && !used)
		{
			LogManager.logManager.AddEvent(Time.time, "Alert;Show;Time;" + time + ";Content;" + alert.Replace("\n", ""));
			StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(time, alert));
			i++;
			used = !used;
		}

		if (timer > startTime + time)
		{
			used = !used;
			timer = 0f;
		}
	}
}
