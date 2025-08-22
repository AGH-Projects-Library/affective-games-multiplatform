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

    public static BitalinoController bitalinoController;

	public int ECGChannel = 0;
	public int EMGChannel = 1;
	public int EDAChannel = 2;

	public BitalinoManager bitalinoManager;
	public BitalinoReader bitalinoReader;

	[Range(0f, 10f)]
    public float cutoffLowpass = 5f;
    [Range(0f, 1f)]
    public float cutoffHighpass = 0.67f;

	[Range(0,2000)]
    public int calibrationSamples = 1000;
    public float calibrationPreTime = 10;

    public float EMGAverage;
	public float EDAAverage;
    public float HRAverage;

	public float EMGCalibrated = 0;
	public float EDACalibrated = 0;
    public float HRCalibrated = 0;
	bool calibrated;

	float ECGMax;
	float ECGPeakThreshold = 0f;
	float ECGPeakTimer = 0f;
	bool ECGPeakFound = false;
	LinkedList<float> HRs = new LinkedList<float>();
	float HRAv;

    public float EMGMin = 0f;
    public float EDAMin = 0f;
    public float HRMin = 0f;
    public float EMGMax = 0f;
    public float EDAMax = 0f;
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
    public bool bitalinoUse = false;
        
    [HideInInspector]
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
        // LogManager.logManager.AddEvent(Time.time, "BITalino;SamplingFrequency;" + bitalinoManager.SamplingFrequency);
        // LogManager.logManager.AddEvent(Time.time, "BITalino;BufferSize;" + bitalinoReader.BufferSize);
        
        string channels = "";
        string analogs = "";

        foreach (AnalogChannel AC in bitalinoManager.analogAndChannels)
        {
            channels += AC.sensor + ";";
            analogs += AC.analog + ";";
        }

        channels.Remove(channels.Length - 1);
        analogs.Remove(analogs.Length - 1);

        // LogManager.logManager.AddEvent(Time.time, "BITalino;Channels;" + channels);
        // LogManager.logManager.AddEvent(Time.time, "BITalino;Analogs;" + analogs);
        // LogManager.logManager.AddEvent(Time.time, "BITalino;BaudRate;" + bitalinoManager.scriptSerialPort.baudRate);
        // LogManager.logManager.AddEvent(Time.time, "BITalino;Port;" + bitalinoManager.scriptSerialPort.portName);
        // LogManager.logManager.AddEvent(Time.time, "BITalino;Parity;" + bitalinoManager.scriptSerialPort.parity);
        // LogManager.logManager.AddEvent(Time.time, "BITalino;DataBits;" + bitalinoManager.scriptSerialPort.dataBits);
        // LogManager.logManager.AddEvent(Time.time, "BITalino;StopBits;" + bitalinoManager.scriptSerialPort.stopBits);

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
            // LogManager.logManager.AddEvent(Time.time, "BITalino;Calibration;Done;Baseline");
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

        // F2
        // List<BITalinoFrame> calibrationFrames = new List<BITalinoFrame>();

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

	public Camera cameraMain;
	public GameObject player;
	public GameObject mainLight;
	PlayerHealth playerHealth;

	public Image imageCrossHair;
	public float cameraTime = 200f;
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

	// Update is called once per frame
	void Update ()
	{
		// if (playerHealth.isDead)
		// {
		// 	player.GetComponent<PlayerMovement>().enabled = false;
		// 	player.GetComponent<CharacterController>().enabled = false;

		// 	player.transform.Find("Player").gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
		// 	player.transform.Find("Gun").gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;

		// 	mainLight.gameObject.SetActive(true);
		// 	player.GetComponent<Light>().enabled = false;

		// 	cameraMain.gameObject.SetActive(true);
		// 	player.transform.Find("MainCameraFPS").gameObject.SetActive(false);

		// 	imageCrossHair.gameObject.GetComponent<Image>().enabled = false;	
		// }

		// timer += Time.deltaTime;

		// if((currentSceneNumber == thirdLvlIndex) && (timer >= (cameraTime - waitBeginTime)) && (timer <= (cameraTime - (2 * waitBeginTime / 3))) && !fps && !usedFirst)
		// {
		// 	usedFirst = true;
		// 	LogManager.logManager.AddEvent(Time.time, "Alert;CameraChange;1");
		// 	StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp((waitBeginTime / 3), firstAlert));
		// 	elementsHUD[0].gameObject.SetActive(fps);
		// }

		// else if((currentSceneNumber == thirdLvlIndex) && (timer >= (cameraTime - (2 * waitBeginTime / 3))) && (timer <= (cameraTime - (1 * waitBeginTime / 3))) && !fps && !usedSecond)
		// {
		// 	usedSecond = true;
		// 	LogManager.logManager.AddEvent(Time.time, "Alert;CameraChange;2");
		// 	StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp((waitBeginTime / 3), secondAlert));
		// 	elementsHUD[1].gameObject.SetActive(fps);
		// }

		// else if((currentSceneNumber == thirdLvlIndex) && (timer >= (cameraTime - (1 * waitBeginTime / 3))) && (timer <= (cameraTime)) && !fps && !usedThird)
		// {
		// 	usedThird = true;
		// 	LogManager.logManager.AddEvent(Time.time, "Alert;CameraChange;3");
		// 	StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp((waitBeginTime / 3), thirdAlert));
		// 	elementsHUD[2].gameObject.SetActive(fps);

		// 	StartCoroutine(Wait());
		// }

		// if((currentSceneNumber == thirdLvlIndex) && (timer >= (cameraTime - waitBeginTime)) && (timer <= (cameraTime - (2 * waitBeginTime / 3))) && fps && !usedFirst)
		// {
		// 	usedFirst = true;
		// 	LogManager.logManager.AddEvent(Time.time, "Alert;CameraChange;FPS1");
		// 	StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp((waitBeginTime / 3), firstAlertFPS));
		// 	elementsHUD[0].gameObject.SetActive(fps);
		// }

		// else if((currentSceneNumber == thirdLvlIndex) && (timer >= (cameraTime - (2 * waitBeginTime / 3))) && (timer <= (cameraTime - (1 * waitBeginTime / 3))) && fps && !usedSecond)
		// {
		// 	usedSecond = true;
		// 	LogManager.logManager.AddEvent(Time.time, "Alert;CameraChange;FPS2");
		// 	StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp((waitBeginTime / 3), secondAlertFPS));
		// 	elementsHUD[1].gameObject.SetActive(fps);
		// }

		// else if((currentSceneNumber == thirdLvlIndex) && (timer >= (cameraTime - (1 * waitBeginTime / 3))) && (timer <= (cameraTime)) && fps && !usedThird)
		// {
		// 	usedThird = true;
		// 	LogManager.logManager.AddEvent(Time.time, "Alert;CameraChange;FPS3");
		// 	StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp((waitBeginTime / 3), thirdAlertFPS));
		// 	elementsHUD[2].gameObject.SetActive(fps);

		// 	StartCoroutine(Wait());
		// }

		// if (Input.GetKeyUp(KeyCode.M) && !playerHealth.isDead) 
		// {

		// 	LogManager.logManager.AddEvent(Time.time, "KeyPress;M");
			
		// 	fps = !fps;
		// 	changeCam();

		// 	timer = 0f;
		// }

		// if ((currentSceneNumber == thirdLvlIndex) && (timer >= cameraTime) && !playerHealth.isDead)
		// {
		// 	fps = !fps;
		// 	changeCam();

		// 	timer = 0f;
		// }

		// if (Input.GetKeyDown(KeyCode.I))
		// {
		// 	LogManager.logManager.AddEvent(Time.time, "KeyPress;I");
		// 	Cursor.lockState = CursorLockMode.None;
		// 	dontChange = true;
		// }

		// else if(!fps || playerHealth.isDead && !dontChange)
		// {
		// 	Cursor.lockState = CursorLockMode.None;
		// }

		// else if(fps && !dontChange)
		// {
		// 	Cursor.lockState = CursorLockMode.Locked;
		// }
	}

	IEnumerator Wait()
	{
		yield return new WaitForSeconds(turnOffTime);
	}

	void changeCam()
	{
		string camerName = fps ? "FPS" : "Izomorphic";

		// LogManager.logManager.AddEvent(Time.time, "Camera;ChangeTo;" + camerName);

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
using System.Collections.Generic;
using UnityEngine;

public class CamMouseLook : MonoBehaviour {

    public float speed = 5f;
    public float turn = 3f;
    public float limit_up = 30;
    public float limit_down = 30;
    public float limit_three = 60;
    GameObject parentObj;

    // F2 
    // Rigidbody otherRb;
    Vector3 angles;
 
    void Awake () 
    {
        Cursor.lockState = CursorLockMode.Locked;
        parentObj = this.transform.parent.gameObject;
        
        // F2
        // otherRb = parentObj.GetComponent <Rigidbody> ();
        // float xstart = Input.mousePosition.x;
        // float ystart = Input.mousePosition.y;
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
using UnityEngine;

public class CharacterController : MonoBehaviour 
{
	public float speed = 10f;
	Animator anim;

	void Awake()
    {
        anim = GetComponent<Animator> ();
    }

	// Use this for initialization
	void Start () {
		Cursor.lockState = CursorLockMode.Locked;		
	}
	
	// Update is called once per frame
	void FixedUpdate () {

		float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

		float translation = v * speed;
		float straffe = h * speed;
		translation *= Time.deltaTime;
		straffe *= Time.deltaTime;

		transform.Translate(straffe, 0, translation);

		Animating(h,v);

		if(Input.GetKeyDown("escape"))
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
    public float timeBetweenAttacks = 2f;
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
    public int startingHealth = 100;
    public int currentHealth;
    public float sinkSpeed = 0.07f;
    public int scoreValue = 10;
    public AudioClip deathClip;


    Animator anim;
    AudioSource enemyAudio;
    ParticleSystem hitParticles;
    CapsuleCollider capsuleCollider;
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

        // enemyAudio.Play (); // F2

        currentHealth -= amount;

        if(currentHealth <= 0)
        {
            LogManager.logManager.AddEvent(Time.time, "Enemy;Death;By;LevelEnd;ID;" + gameObject.GetInstanceID() + ";PositionX;" + gameObject.transform.position.x + ";PositionY;" + gameObject.transform.position.y + ";PositionZ;" + gameObject.transform.position.z + ";RotationX;" + gameObject.transform.rotation.x + ";RotationY;" + gameObject.transform.rotation.y + ";RotationZ;" + gameObject.transform.rotation.z + ";RotationW;" + gameObject.transform.rotation.w);
            Death ();
            // AddScore();
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

	public int affectiveSpawnTimeMedium = 60;
    public int affectiveSpawnTimeMediumMax = 80;
	public int affectiveSpawnTimeHard = 90;
    public int affectiveSpawnTimeHardMax = 180;
    public int preparationTime = 5;

    // F2
    // public int neccessaryTimes = 3;

	public float spawnTime = 3f;
	public float invokeTime = 5f;
    public PlayerHealth playerHealth;
    public GameObject[] enemy;
    public Transform[] spawnPoints;

    // F2    
    // bool bitalinoUseFlag;
    // bool boredom = false;

    bool checkedMedium = false;
    bool checkedHard = false;

    // F2
    // int spawnMax;

    float timerSpawnMax = 0f;
    float remainTime = 5f;

    float alertTime = 5f;

    string alertMessageNonAffectivePl = "Nadchodzą kolejne potwory!\nUważaj!";
    
    // F2
    // string alertMessageAffectiveEng = "Your emotions has lured monsters!\nThey are coming! Watch out!";
    // string alertMessageAffectivePl = "Twoje emocje zwabiły potwory!\nNadchodzą posiłki! Uważaj!";
    
    string alertMessageNonAffectiveEng = "More and more monsters are coming!\nWatch out!";

    // F2
    // string alertMessageAffective = "";

    string alertMessageNonAffective = "";

    void Awake()
    {
        if(UserManager.lang.Equals(UserManager.LanguageOption._English))
		{
            // F2
			// alertMessageAffective = alertMessageAffectiveEng;
			alertMessageNonAffective = alertMessageNonAffectiveEng;
		}

		else if(UserManager.lang.Equals(UserManager.LanguageOption._Polish))
		{
            // F2
            // alertMessageAffective = alertMessageAffectivePl;
			alertMessageNonAffective = alertMessageNonAffectivePl;
		}

        // bitalinoUseFlag = BitalinoController.bitalinoController.bitalinoUse;
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

        // F2
        // if (!bitalinoUseFlag)
        // {
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
        // }


        // F2
        // if (bitalinoUseFlag)
        // {
        //     if((Time.timeSinceLevelLoad >= (affectiveSpawnTimeMedium - remainTime)) &&  timerSpawnMax > remainTime)
        //     {
        //         float HRAverage = BitalinoController.bitalinoController.HRAverage;
        //         float HRMax = BitalinoController.bitalinoController.HRMax;
        //         float HRMin = BitalinoController.bitalinoController.HRMin;
        //         float HRMean = (HRMax + HRMin) / 2;

        //         float EDAAverage = BitalinoController.bitalinoController.EDAAverage;
        //         float EDAMax = BitalinoController.bitalinoController.EDAMax;
        //         float EDAMin = BitalinoController.bitalinoController.EDAMin;
        //         float EDAMean = (EDAMax + EDAMin) / 2;

        //         if ((HRAverage >= HRMean) && (EDAAverage >= EDAMean))
        //         {
        //             spawnMax = 0;
        //         }

        //         else if (((HRAverage < HRMean) && (EDAAverage >= EDAMean)) || ((HRAverage >= HRMean) && (EDAAverage < EDAMean)))
        //         {
        //             spawnMax = 2;
        //         }

        //         else if ((HRAverage < HRMean) && (EDAAverage < EDAMean))
        //         {
        //             spawnMax = 5;
        //         }
                
        //         LogManager.logManager.AddEvent(Time.time, "AffectiveSpawn;SpawnMax;SetTo;" + spawnMax);

        //         timerSpawnMax = 0f;
        //     }

        //     if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeMedium - preparationTime) && !checkedMedium)
        //     {
        //         checkedMedium = true;
        //         boredom = false;
        //         StartCoroutine(CheckBoredom(0));
        //     }

        //     else if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeHard - preparationTime) && !checkedHard)
        //     {
        //         checkedHard = true;
        //         boredom = false;
        //         StartCoroutine(CheckBoredom(1));
        //     }
        // }   
    }

    // F2
    // IEnumerator CheckBoredom(int x)
    // {
    //     int sum = 0;
    //     for (int i = 0; i < preparationTime; i++)
    //     {
    //         if ((BitalinoController.bitalinoController.EDAAverage < BitalinoController.bitalinoController.EDAMax) && (BitalinoController.bitalinoController.HRAverage <= ((BitalinoController.bitalinoController.HRMax + BitalinoController.bitalinoController.HRMax) / 2)))
    //         {
    //             sum++;
    //         }

    //         yield return new WaitForSeconds(1f);
    //     }

    //     if (sum >= neccessaryTimes)
    //     {
    //         boredom = true;
    //         checkedMedium = true;
    //         LogManager.logManager.AddEvent(Time.time, "Alert;AffectiveSpawn;Activation");
    //         LogManager.logManager.AddEvent(Time.time, "AffectiveSpawn;Active;Sum;" + sum);
    //         StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessageAffective));
    //     }

    //     else
    //     {
    //         LogManager.logManager.AddEvent(Time.time, "AffectiveSpawn;NotActive;Sum;" + sum);
    //         if (x == 0)
    //         {
    //             affectiveSpawnTimeMedium += preparationTime;
    //             if (affectiveSpawnTimeMedium < affectiveSpawnTimeMediumMax)
    //             {
    //                 checkedMedium = false;
    //             }
    //         }
            
    //         else if (x == 1)
    //         {
    //             affectiveSpawnTimeHard += preparationTime;
    //             if (affectiveSpawnTimeHard < affectiveSpawnTimeHardMax)
    //             {
    //                 checkedMedium = false;
    //             }
    //         }
    //     }
    // }
    

    void Spawn ()
    {
        GameObject [] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        if(playerHealth.currentHealth <= 0f || enemies.Length >= EnemyManager.maxEnemies)
        {
            return;
        }

        int enemyIndex = Random.Range (0, enemy.Length);

        // without biofeedback
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

        // F2
        // with biofeedback
        // if((Time.timeSinceLevelLoad >= affectiveSpawnTimeMedium) && bitalinoUseFlag)
        // {
        //     if (boredom)
        //     {
        //         int spawnPointIndex = Random.Range (0, spawnMax);
        //         Instantiate (enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
        //         LogManager.logManager.AddEvent(Time.time, "Enemy;" + enemyIndex + ";AffectiveSpawn;AtSpawnPoint;" + spawnPointIndex);
        //     }
        // }

        // if((Time.timeSinceLevelLoad >= affectiveSpawnTimeHard) && bitalinoUseFlag)
        // {
        //     if (boredom)
        //     {
        //         int spawnPointIndex = Random.Range (0, spawnMax);
        //         Instantiate (enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
        //         LogManager.logManager.AddEvent(Time.time, "Enemy;" + enemyIndex + ";AffectiveSpawn;AtSpawnPoint;" + spawnPointIndex);
        //     }
        // }

    }
}
--- AudioManager.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour 
{
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
			// LogManager.logManager.AddEvent(Time.time, "BackgroundMusic;Start");
			// gameObject.GetComponent<AudioSource>().enabled = true;
			flagChecked = true;
		}
	}
}

--- EndManager.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndManager : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

--- EnemyManager.cs ---
using System.IO;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public float spawnTime = 3f;
    public float invokeTime = 5f;
    public PlayerHealth playerHealth;
    public GameObject enemy;
    public Transform[] spawnPoints;

    public static int maxEnemies = 8;

    string persDataPath;

    // void Awake()
    // {
    //     maxEnemies = Random.Range(10,15);
    // }


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

        InvokeRepeating ("Spawn", invokeTime, spawnTime);
    }


    void Spawn ()
    {
        GameObject [] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if(playerHealth.currentHealth <= 0f || enemies.Length >= maxEnemies)
        {
            return;
        }

        int spawnPointIndex = Random.Range (0, spawnPoints.Length);

        Instantiate (enemy, spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
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
    public PlayerHealth playerHealth;

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

	public PlayerHealth playerHealth;
	public EnemyHealth mrNightmareHealth;
	public AffectiveEnemyManager affectiveEnemyManager;
	public GameObject[] enemy;
    public Transform[] spawnPoints;

	public int healthLvlOne = 900;
	public int healthLvlTwo = 600;
	public int healthLvlThree = 300;
	public int healthLvlFour = 30;

	bool usedLvlOne = false;
	bool usedLvlTwo = false;
	bool usedLvlThree = false;
	bool usedLvlFour = false;

	
	public int hellpNumberLvlOne = 5;
	public int hellpNumberLvlTwo = 7;
	public int hellpNumberLvlThree = 9;
	public int hellpNumberLvlFour = 11;
	public float hellpWaitLvlOne = 3f;
	public float hellpWaitLvlTwo = 2f;
	public float hellpWaitLvlThree = 4f;
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
			// playerHealth.enemyMovementChangeTime = Time.timeSinceLevelLoad; //?
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
    public static int score;
    public int scoreToLvlUp = 300;

    Text text;

    float timer = 0f;
    float waitTime = 3f;

    int zeroLevel = 2;
    float zeroLevelWait = 105f;

    UserManager.LanguageOption lang;

    void Awake ()
    {
        LogManager.logManager.AddEvent(Time.time, "Scene;Load;ID;" + SceneManager.GetActiveScene().buildIndex);

        lang = UserManager.lang;

        text = GetComponent <Text> ();
        
        // F2
        // if(lang.Equals(UserManager.LanguageOption._English))
        // {
        //     text.text = "TRAGIC";
        // }
        // else if (lang.Equals(UserManager.LanguageOption._Polish))
        // {
        //     text.text = "TRAGICZNIE";
        // }

        if (SceneManager.GetActiveScene().buildIndex > 2)
        {
            scoreToLvlUp = Random.Range((scoreToLvlUp - 100), scoreToLvlUp);
            LogManager.logManager.AddEvent(Time.time, "Score;ToLevelUp;Value;" + scoreToLvlUp);
        }

        score = 0;
    }

    void Update ()
    {
        if(Input.GetKey(KeyCode.P))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;P");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        text.text = score.ToString();

        // F2
        // string current = text.text;

        // if(score < (1 * scoreToLvlUp / 5))
        // {
        //     if(lang.Equals(UserManager.LanguageOption._English))
        //     {
        //         text.text = "TRAGIC";
        //     }
        //     else if (lang.Equals(UserManager.LanguageOption._Polish))
        //     {
        //         text.text = "TRAGICZNIE";
        //     }   
        // }
        
        // else if(score < (2 * scoreToLvlUp / 5))
        // {
        //     if(lang.Equals(UserManager.LanguageOption._English))
        //     {
        //         text.text = "POORLY";
        //     }
        //     else if (lang.Equals(UserManager.LanguageOption._Polish))
        //     {
        //         text.text = "KIEPSKO";
        //     }   
        // }

        // else if(score < (3 * scoreToLvlUp / 5))
        // {
        //     if(lang.Equals(UserManager.LanguageOption._English))
        //     {
        //         text.text = "DECENTLY";
        //     }
        //     else if (lang.Equals(UserManager.LanguageOption._Polish))
        //     {
        //         text.text = "PRZYZWOICIE";
        //     }   
        // }

        // else if(score < (4 * scoreToLvlUp / 5))
        // {
        //     if(lang.Equals(UserManager.LanguageOption._English))
        //     {
        //         text.text = "GREAT";
        //     }
        //     else if (lang.Equals(UserManager.LanguageOption._Polish))
        //     {
        //         text.text = "WSPANIALE";
        //     }   
        // }

        // else if(score < (5 * scoreToLvlUp / 5))
        // {
        //     if(lang.Equals(UserManager.LanguageOption._English))
        //     {
        //         text.text = "FANTASTIC";
        //     }
        //     else if (lang.Equals(UserManager.LanguageOption._Polish))
        //     {
        //         text.text = "FANTASTYCZNIE";
        //     }   
        // }

        // else
        // {
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

        // F2
        // if (!current.Equals(text.text))
        // {
        //     LogManager.logManager.AddEvent(Time.time, "TextScore;ChangeTo;" + text.text);
        // }
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
	public static UserManager userManager;

    public enum LanguageOption
    {
		_English = 0,
		_Polish = 1
	};

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

    // public void GainUserName(Text txt)
    // {
    //     LogManager.logManager.AddEvent(Time.time, "InputEnter;Name;" + txt.text);

    //     userName = txt.text;
    // }

    // public void GainUserNumber(Text txt)
    // {
    //     LogManager.logManager.AddEvent(Time.time, "InputEnter;Name;" + txt.text);
    //     LogManager.logManager.AddEvent(Time.time, "InputEnter;Number;" + txt.text);
    //     userPath = userPath + userNumber + "\\";
    //     Directory.CreateDirectory(userPath);

    //     userNumber = txt.text;
    //     userPath = userPath + userNumber + "\\";
    //     Directory.CreateDirectory(userPath);
    // }

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
        // float t = Time.time;

        // // if (x == 0)
        // // {
        // //     LogManager.logManager.AddEvent(t, "ButtonClick;English");
        // // }
        // // else if (x == 1)
        // // {
        // //     LogManager.logManager.AddEvent(t, "ButtonClick;Polish");
        // // }

        lang = (LanguageOption)x;
    }

    public static List<String> stimuliMinus = new List<string>();
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

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
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
	// void OnCollisionEnter(Collision col)
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

	public float spawnTime = 7f;
    public float invokeTime = 5f;
    public PlayerHealth playerHealth;
    public GameObject pickUp;
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
    public int startingHealth = 100;
    public int currentHealth;
    public int healthRegeneration = 5;
    public float regenerationTime = 10f;
    public Slider healthSlider;
    public Image damageImage;
    public AudioClip deathClip;
    public float flashSpeed = 5f;
    public Color flashColour = new Color(1f, 0f, 0f, 0.1f);


    Animator anim;
    AudioSource playerAudio;
    PlayerMovement playerMovement;
    PlayerShooting playerShooting;
    public bool isDead;
    bool damaged;

    float timer;

    int disguisedHealth;


    //enemy movement

    // F2
    // string tagEnemy = "Enemy";
    
    // public float enemyMovementChangeTime = 90f;

    // F2
    // float remainTime = 2f;
    // float timerMovement = 0f;

    // F2
    // float speedReduced = 2f;
    // float speedBase = 3f;

    // F2
    // float accSpeed = 0.03f;
    // float speed = 0f;

    // F2
    // float scaleLess = 1.5f;
    // float scaleMore = 1.3f;
    // float HRAv;
    // float HRMax;
    // float HRMin;

    // F2
    // bool used = false;
    // float alertTime = 5f;
    // string alertMessageAffectivePl = "Potwory wyczuwają Twoje emocje!\nDososowują swoją prędkość!\nUważaj!";
    // string alertMessageNonAffectivePl = "Potwory mają Cię dość!\nBędą poruszać się szybciej!\nUważaj!";
    // string alertMessageAffectiveEng = "Monsters feel your emotions!\nThey are adjusting their speed!\nWatch out!";
    // string alertMessageNonAffectiveEng = "Monsters are sick of you!\nThey are going to move faster!\nWatch out!";
    // string alertMessageAffective = "";
    // string alertMessageNonAffective = "";

    // int zeroLevel = 2; // F2

    // bool bitalonoUseFlag;     // F2

    void Awake ()
    {
        // F2
        // if (UserManager.lang.Equals(UserManager.LanguageOption._English))
        // {
        //     alertMessageAffective = alertMessageAffectiveEng;
        //     alertMessageNonAffective = alertMessageNonAffectiveEng;
        // }
        // else if (UserManager.lang.Equals(UserManager.LanguageOption._Polish))
        // {
        //     alertMessageAffective = alertMessageAffectivePl;
        //     alertMessageNonAffective = alertMessageNonAffectivePl;
        // }


        // F2
        // bitalonoUseFlag = BitalinoController.bitalinoController.bitalinoUse;
        // speed = speedBase;
        
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


        if (Input.GetKey(KeyCode.Escape))
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
			// EditorApplication.isPlaying = false;
		}

        // F2
        // if (SceneManager.GetActiveScene().buildIndex > zeroLevel)
        // {
        //     SetEnemyMovement();
        // }

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

            // F2
            // changeHealth(currentHealth);
            
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
                // EditorApplication.isPlaying = false;
            }
        } 

    }

    // F2
    // void SetEnemyMovement()
    // {
    //     if (Time.timeSinceLevelLoad > (enemyMovementChangeTime - remainTime))
    //     {
    //         timerMovement += Time.deltaTime;
    //     }

    //     if ((Time.timeSinceLevelLoad > (enemyMovementChangeTime - remainTime)) && (timerMovement > remainTime) && (currentHealth > 0) && bitalonoUseFlag)
    //     {
    //         if (!used)
    //         {
    //             LogManager.logManager.AddEvent(Time.time, "Alert;Enemy;SpeedAdjust");
    //             StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessageAffective));
    //             used = true;
    //         }

    //         HRAv = BitalinoController.bitalinoController.HRAverage;
    //         HRMax = BitalinoController.bitalinoController.HRMin;
    //         HRMin = BitalinoController.bitalinoController.HRMax;

    //         float k = 0.3f;
    //         int L = 5;
    //         int yScale = 2; 
            
    //         speed = yScale + L / (1 + Mathf.Exp(k * (HRAv - ((HRMax + HRMin) / 2)))); // belongs to [2, 7]
    //         LogManager.logManager.AddEvent(Time.time, "Enemy;Speed;ChangeTo;" + speed);
    //         GameObject [] enemies = GameObject.FindGameObjectsWithTag(tagEnemy);
	// 	    foreach (GameObject enemy in enemies)
	// 	    {
    //             enemy.GetComponent<EnemyMovement>().SetSpeed(speed);
    //         }

    //         timerMovement = 0f;
    //     }

    //     else if ((Time.timeSinceLevelLoad > (enemyMovementChangeTime - remainTime)) && (timerMovement > remainTime) && (currentHealth > 0) && !bitalonoUseFlag)
    //     {
    //         if (!used)
    //         {
    //             LogManager.logManager.AddEvent(Time.time, "Alert;Enemy;SpeedChange");
    //             StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, alertMessageNonAffective));
    //             used = true;
    //         }

    //         speed += accSpeed;
    //         LogManager.logManager.AddEvent(Time.time, "Enemy;Speed;ChangeTo;" + speed);
    //         GameObject [] enemies = GameObject.FindGameObjectsWithTag(tagEnemy);
	// 	    foreach (GameObject enemy in enemies)
	// 	    {
    //             enemy.GetComponent<EnemyMovement>().SetSpeed(speed);
    //         }

    //         timerMovement = 0f;
    //     }
    // }


    public void TakeDamage (int amount, int i)
    {
        timer = 0f;

        damaged = true;

        currentHealth -= amount;
        healthSlider.value = currentHealth;
        
        LogManager.logManager.AddEvent(Time.time, "Player;Health;DecreaseTo;" + currentHealth + ";By;Enemy;ID;" + i);
        LogManager.logManager.AddEvent(Time.time, "Slider;Health;DecreaseTo;" + currentHealth);

        // F2
        // changeHealth(currentHealth);

        playerAudio.Play ();

        if(currentHealth <= 0 && !isDead)
        {
            Death ();
        }
    }

    // F2
    // void changeHealth (int currentHealth)
    // {
    //     int previousHealth = disguisedHealth;

    //     if(currentHealth >= (4 * startingHealth / 5))
    //     {
    //         disguisedHealth = 5 * startingHealth / 5;
    //     }
        
    //     else if(currentHealth >= (3 * startingHealth / 5))
    //     {
    //         disguisedHealth = (4 * startingHealth / 5);
    //     }

    //     else if(currentHealth >= (2 * startingHealth / 5))
    //     {
    //         disguisedHealth = (3 * startingHealth / 5);   
    //     }

    //     else if(currentHealth >= (1 * startingHealth / 5))
    //     {
    //         disguisedHealth = (2 * startingHealth / 5);   
    //     }

    //     else
    //     {
    //         disguisedHealth = (1 * startingHealth / 5);
    //     }

    //     healthSlider.value = disguisedHealth;
        
    //     if (disguisedHealth > previousHealth)
    //     {
    //         LogManager.logManager.AddEvent(Time.time, "Slider;Health;IncreaseTo;" + disguisedHealth);
    //     }
    //     else if (disguisedHealth < previousHealth)
    //     {
    //         LogManager.logManager.AddEvent(Time.time, "Slider;Health;DecreaseTo;" + disguisedHealth);
    //     }
    // }


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
    public float speed = 6f;
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
        // mapping from keyboard, values = {-1,0,1}
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float r = Input.GetAxisRaw("Mouse X");
        float t = Input.GetAxisRaw("Mouse Y");

        LogManager.logManager.AddEvent(Time.time, "Joystick;Left;Horizontal;" + h + ";Vertical;" + v);
        LogManager.logManager.AddEvent(Time.time, "Joystick;Right;Horizontal;" + r + ";Vertical;" + t);

        // Move(h, v);
        // Turning();
        TurningAndMoving(r, t, h, v);
        Animating(h, v);
    }

    // void Move (float h, float v)
    // {
    //     movement.Set(h, 0f, v);
    //     movement = movement.normalized * speed *  Time.deltaTime;

    //     playerRigidbody.MovePosition(transform.position + movement);
    // }

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
        
        // transform.Rotate(rotate * Time.deltaTime * speedRotor * 1000) ;

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
    public int damagePerShot = 20;
    public int damagePerShotMin = 10;
    public int damagePerShotMax = 20;
    public float timeBetweenBullets = 0.2f;
    public float timeBetweenBulletsMin = 0.2f;
    public float timeBetweenBulletsMax = 0.8f;
    public float range = 100f;
    public float rangeMin = 40f;
    public float rangeMax = 100f;
    public float ultraPower = 100;
    public float ultraPowerMax = 100;

    public GameObject enemy;
    public GameObject[] enemies;
    public Slider powerSlider;
    public Image superpowerImage;

    float timer = 0f;
    Ray shootRay = new Ray();
    RaycastHit shootHit;
    int shootableMask;
    ParticleSystem gunParticles;
    LineRenderer gunLine;
    AudioSource gunAudio;
    Light gunLight;
    float effectsDisplayTime = 0.2f;
    
    public float timePressed = 0f;
    float timePressedMax = 5f;
    bool wasPressed = false;

    // F2
    // float timeMuscle = 0f;
    // float timeMuscleSum = 0f;
    // float timeMuscleMax = 5f;
    // bool wasMuscle = false;

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

    // F2
    // bool bitalinoUseFlag;

    int currentSceneNumber;
    int secondLevelIndex = 5;

    // F2
    // public float EMGScale = 1.5f;
    // public float timeMuscleSumMin = 0.3f;

    void Awake ()
    {
		currentSceneNumber = SceneManager.GetActiveScene().buildIndex;

        shootableMask = LayerMask.GetMask ("Shootable");
        gunParticles = GetComponent<ParticleSystem> ();
        gunLine = GetComponent <LineRenderer> ();
        gunAudio = GetComponent<AudioSource> ();
        gunLight = GetComponent<Light> ();
        powerSlider.value = ultraPower;

        // bitalinoUseFlag = BitalinoController.bitalinoController.bitalinoUse;

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

		if(Input.GetButton("R2") && timer >= timeBetweenBullets && Time.timeScale != 0)
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
            // F2
            // if (bitalinoUseFlag)
            // {
            //     MuscleStrainTimer();
            //     UseSuperPower(timeMuscle, timeMuscleMax, wasMuscle);
            //     timeMuscle = 0f;
            //     wasMuscle = false; 
            // }

            // else
            // {
                KeyPressedTimer();
                UseSuperPower(timePressed, timePressedMax, wasPressed);
                // timePressed = 0; // why is working without it and not working with it?
                wasPressed = false;
            // }
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

    // F2
    // void MuscleStrainTimer()
    // {
    //     if(BitalinoController.bitalinoController.EMGAverage > EMGScale * BitalinoController.bitalinoController.EMGCalibrated)
    //     {
    //         timeMuscleSum += Time.deltaTime;
    //     }
    //     else if(timeMuscleSum > timeMuscleSumMin)
    //     {
    //         timeMuscle = timeMuscleSum;
    //         wasMuscle = true;
    //         timeMuscleSum = 0f;
    //     }

    // }

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
using UnityEditor;

public class ManageEnd : MonoBehaviour {

	public Text textShowed;

    public Text textCD;
    

	List<string> texts = new List<string> ();
    int index;

    string nameScoreBoard = "ScoreBoard";

    int timeCD;

    public float timeToStart = 15;

    string txtTime = "";
    string txtTimePL = "Czas za jaki pokażemy Ci wyniki: ";

    string txtTimeENG = "Time to show results: ";

    void Awake()
    {
        timeCD = (int)timeToStart;
	    StartCoroutine("LoseTime");

        LogManager.logManager.AddEvent(Time.time, "Load;Scene;ID" + SceneManager.GetActiveScene().buildIndex);

        if (UserManager.lang.Equals(UserManager.LanguageOption._English))
        {
            txtTime = txtTimeENG;
            texts.Add("You win!\n\nYour Nightmare has been defeated, and all of its followers fled!\nIt is time for your tranquil dream.\nAnd peaceful return of memory.\n\nLet’s practice.\nWe will dictate our bank account numer to you now...");
        }
        else if (UserManager.lang.Equals(UserManager.LanguageOption._Polish))
        {
            txtTime = txtTimePL;
            texts.Add("WYGRAŁEŚ!\n\nTwój koszmar został pokonany\na wszyscy jego popelcznicy ucielki w popłochu!\nPora na spokojny sen.\nI spokojny powrót pamięci.\n\nPoćwiczmy.\nPodyktujemy Ci teraz numer naszego rachunku bankowego...");
        }

        index = 0;
        textShowed.text = texts[index];
    }

    void Update()
    {
        if (timeCD < 0)
        {
            SceneManager.LoadScene(nameScoreBoard);
        }

        if (Input.GetKey(KeyCode.Escape))
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Esc");
			Application.Quit(); // ignored in UnityEditor
			// EditorApplication.isPlaying = false;
		}

        // end screen skip:
        if (Input.GetKeyDown(KeyCode.T))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;T");
        	SceneManager.LoadScene(nameScoreBoard);
        }

    }

    public void ShowScoreBoard() 
    {   
        // LogManager.logManager.AddEvent(Time.time, "ButtonClick;BestPlayers");
        SceneManager.LoadScene(nameScoreBoard);
	}

    IEnumerator LoseTime()
	{
		while(true)
		{
            string txt = txtTime + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;End;CountDown;Text;ChangeTo;" + txt);
            yield return new WaitForSeconds(1);
			textCD.text = txt;
			timeCD -= 1;
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
using UnityEditor;

public class ManageIntroduction : MonoBehaviour {

	public Text textShowed;
    // public Button affectiveButton;
    // public Button nonaffectiveButton;
    public Button polishButton;
    public Button englishButton;
    // public InputField nameInputField;
    // public InputField numberInputField;

    public float timeCalibrationCheck = 0.5f;
    public float timeWait = 1f;

    // F2
    // public float timeLoadTutorial = 2f;
    // float timer = 0f;

    string txtWelecomePl = "Witaj we Freud2.0!\n\n To Twoja kolejna sesja terapeutyczna,\njesteś tu ze względu na problemy z pamięcią.\nNasza terapia opiera się na psychoanalizie,\nktórej autorem jest Zygmunt Freud.\nW jego koncepcji psychika działa na trzech poziomach\n Świadomości, przedświadomości i nieświadomości.\nNajwiększy wpływ na późniejsze życie ma dzieciństwo.\nNo, więc właśnie tam sie udajemy.\nDo dzieciństwa poprzez wszystkie trzy poziomy, ale najpierw tutorial."; //F2

    // F2
    // string txtPreCalibrationPl = "Zaraz rozpocznie się kalibracja.\nUłóż ręce jak do grania i wyprostuj się.";
    // string txtCalibrationPl = "Postaraj się odprężyć.\nOddychaj powoli głębokimi oddechami\ni postaraj się nie ruszać.\nKalibracja potrwa około 30 sekund.";
    // string txtCalibratedPl = "Skalibrowano!";

    string txtWelecomeEng = "Welcome in the Freud2.0!\n\n This is your next therapeutic session,\nyou are here because of problems with memory.\nOur therapy is based on psychoanalysis,\ncreated by Sigmund Freud.\nIn his concept, the psyche works on three levels.\n The consciousness, the preconscious and the unconscious.\nChildhood has great impact on later life.\nNow, that's where we go.\nTo childhood through all three levels, but first the tutorial."; //F2

    // F2
    // string txtPreCalibrationEng = "The calibration will begin soon.\nArrange your hands as if you were playing and straighten up.";
    // string txtCalibrationEng = "Try to relax.\nBreathe deeply and try not to move.\nCalibrations takes around 30 seconds.";
    // string txtCalibratedEng = "Calibrated!";

    string txtWelecome = "";

    // F2
    // // string txtPreCalibration = "";
    // // string txtCalibration = "";
    // // string txtCalibrated = "";

    // string txtAffectiveButtonPl = "Z pętlą afektywną";
    // string txtNonAffectiveButtonPl = "Graj!"; //F2

    // string txtNameInputPl = "Nazwa";
    // string txtNumberInputPl = "Numer";

    // F2
    // string txtAffectiveButtonEng = "With affective loop";
    // string txtNonAffectiveButtonEng = "Play!"; //F2
    // string txtNameInputEng = "Name";
    // string txtNumberInputEng = "Number";

    UserManager.LanguageOption lang;

    public float timeToStart = 30;

	int timeCD;
    
    public Text txt;

    string txtTime = "";

    string txtTimePL = "Gra rozpocznie się za: ";
    string txtTimeEng = "The game will start in: ";


    void Awake()
    {
        timeCD = (int)timeToStart;
	    StartCoroutine("LoseTime");

        // float t1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        float t2 = Time.time;

        LogManager.logManager.AddEvent(t2, "Scene;Load;" + "ID;" + SceneManager.GetActiveScene().buildIndex);

        lang = UserManager.lang;
        ChangeLanguage();
    }

    void Update() 
    {
        if(Input.GetButtonDown ("Fire2"))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;X");
            lang = UserManager.LanguageOption._English;
            UserManager.lang = lang;
            ChangeLanguage();
        }

        if(Input.GetButtonDown ("Fire3"))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;O");
            lang = UserManager.LanguageOption._Polish;
            UserManager.lang = lang;
            ChangeLanguage();
        }

        if (Input.GetKeyDown(KeyCode.S))
		{
            LogManager.logManager.AddEvent(Time.time, "Key;S");
			LoadLvl0();
            // LoadTutorial();
		}

        if (timeToStart < Time.timeSinceLevelLoad)
        {
            LoadLvl0();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
			// EditorApplication.isPlaying = false;
		}

        if (!lang.Equals(UserManager.lang))
        {
            lang = UserManager.lang;
            ChangeLanguage();
        }

        // if(BitalinoController.bitalinoController != null && BitalinoController.bitalinoController.FinishedCalibration)
        // {
        //     textShowed.text = txtCalibrated;
        //     timer += Time.deltaTime;

        //     if (timer > timeLoadTutorial)
        //     {
        //         LoadTutorial();
        //     }
        // }
    }

    void ChangeLanguage()
    {
        switch(lang)
        {
            case UserManager.LanguageOption._English:
            {
                txtWelecome = txtWelecomeEng;
                // F2
                // txtPreCalibration = txtPreCalibrationEng;
                // txtCalibrated = txtCalibratedEng;
                // txtCalibration = txtCalibrationEng;

                textShowed.text = txtWelecome;
                // F2
                // affectiveButton.GetComponentInChildren<Text>().text = txtAffectiveButtonEng;
                // nonaffectiveButton.GetComponentInChildren<Text>().text = txtNonAffectiveButtonEng;
                // nameInputField.GetComponentInChildren<Text>().text = txtNameInputEng;
                // numberInputField.GetComponentInChildren<Text>().text = txtNumberInputEng;

                txtTime = txtTimeEng;
                break;
            }

            case UserManager.LanguageOption._Polish:
            {
                txtWelecome = txtWelecomePl;
                // F2
                // txtPreCalibration = txtPreCalibrationPl;
                // txtCalibrated = txtCalibratedPl;
                // txtCalibration = txtCalibrationPl;
                
                textShowed.text = txtWelecome;
                // F2
                // affectiveButton.GetComponentInChildren<Text>().text = txtAffectiveButtonPl;
                // nonaffectiveButton.GetComponentInChildren<Text>().text = txtNonAffectiveButtonPl;
                // nameInputField.GetComponentInChildren<Text>().text = txtNameInputPl;
                // numberInputField.GetComponentInChildren<Text>().text = txtNumberInputPl;

                txtTime = txtTimePL;
                break;
            }
        }
    }

    // public void MangeNonAffective()
    // {
    //     LogManager.logManager.AddEvent(Time.time, "ButtonClick;WithoutAffectiveLoop");
    //     LoadTutorial();
    // }

	public void LoadTutorial ()
    {
        TurnOffButtons();
        StartCoroutine(Wait());

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 3);
	}

    public void LoadLvl0 ()
    {
        TurnOffButtons();
        StartCoroutine(Wait());

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
	}

    // F2
    // public void LoadCalibration ()
    // {
    //     LogManager.logManager.AddEvent(Time.time, "ButtonClick;WithAffectiveLoop");

    //     TurnOffButtons();
	// 	textShowed.text = txtPreCalibration;
        
    //     StartCoroutine(Calibrate());
	// }

    void TurnOffButtons ()
    {
    //     affectiveButton.gameObject.SetActive(false);
    //     nonaffectiveButton.gameObject.SetActive(false);
        polishButton.gameObject.SetActive(false);
        englishButton.gameObject.SetActive(false);
    //     nameInputField.gameObject.SetActive(false);
    //     numberInputField.gameObject.SetActive(false);
    }

    IEnumerator Wait()
    {
        yield return new WaitForSeconds(timeWait);
    }

    IEnumerator LoseTime()
	{
		while(true)
		{
            string toShow = txtTime + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;Introduction;CountDown;Text;ChangeTo;" + toShow);
			txt.text = toShow;
			timeCD -= 1;
			yield return new WaitForSeconds(1);
		}
	}

    // F2
    // IEnumerator Calibrate()
    // {
    //     if (!BitalinoController.bitalinoController.FinishedCalibration)
    //     {
    //         yield return new WaitForSeconds(timeWait);
    //         LogManager.logManager.AddEvent(Time.time, "BITalino;StartReading");
    //         StartCoroutine(BitalinoController.bitalinoController.StartReading());
    //         yield return new WaitForSeconds(timeWait);

    //         yield return new WaitForSeconds(BitalinoController.bitalinoController.calibrationPreTime);
    //         textShowed.text = txtCalibration;
            
    //         while (!BitalinoController.bitalinoController.FinishedCalibration)
    //         {
    //             yield return new WaitForSeconds(timeCalibrationCheck);
    //         }

    //         StartCoroutine(Wait());
    //     }
    // }
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
	public Text textBestPlayers;

	public Text closeTxt;
	
	string pathScores;
	Dictionary<int, List<string>> bestPlayers = new Dictionary<int, List<string>> ();

	string tagIndestructible = "DontDestroyObject";
	string tagBITalino = "BITalino";

	int indexIntroduction = 0;
	int indexFirstLevel = 3;

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

		if(Input.GetKeyDown(KeyCode.Escape))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
			// EditorApplication.isPlaying = false;
        }

		else if(Input.GetKeyDown(KeyCode.R))
        {
			LogManager.logManager.AddEvent(Time.time, "Key;R");
			DestroyIndestructible();
	        SceneManager.LoadScene(indexIntroduction);
        }

		else if(Input.GetKeyDown(KeyCode.S))
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
	    // F2
		// bool bitalinoUse = BitalinoController.bitalinoController.bitalinoUse;
		// string use = bitalinoUse ? "1" : "0";
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

	public Text textShowed;

    public Text textCD;
    
    // public Button nextButton;
    // public Button previousButton;
    // public Button playButton;

	List<string> texts = new List<string> ();
    int index;

    // bool bitalinoUseFlag = false;     // F2

    UserManager.LanguageOption lang;

    string txtNextButtonPl = "Następny";
    string txtPreviousButtonPl = "Poprzedni";
    string txtPlayButtonPl = "Graj!";
    string txtNextButtonEng = "Next";
    string txtPreviousButtonEng = "Previous";
    string txtPlayButtonEng = "Play!";

    public float timeToStart = 30;

	int timeCD;

    string txtTime = "";
    string txtTimePL = "Czas do rozpoczęcia poziomu: ";

    string txtTimeENG = "Time to level begin: ";


    public void changeText (int x) 
    {
        this.index += x;
		textShowed.text = texts[this.index];

        // float t = Time.time;
        // if (x > 0)
        // {
        //     LogManager.logManager.AddEvent(t, "ButtonClick;Next");
        // }
        // else if (x < 0)
        // {
        //     LogManager.logManager.AddEvent(t, "ButtonClick;Previous");
        // }
	}

    void Awake()
    {
        LogManager.logManager.AddEvent(Time.time, "Scene;Load;ID;" + SceneManager.GetActiveScene().buildIndex);

        lang = UserManager.lang;

        timeCD = (int)timeToStart;
	    StartCoroutine("LoseTime");
        
        // nextButton.gameObject.SetActive(false);
        // previousButton.gameObject.SetActive(false);
        // playButton.gameObject.SetActive(false);

        // if (lang.Equals(UserManager.LanguageOption._English))
        // {
        //     nextButton.GetComponentInChildren<Text>().text = txtNextButtonEng;
        //     previousButton.GetComponentInChildren<Text>().text = txtPreviousButtonEng;
        //     playButton.GetComponentInChildren<Text>().text = txtPlayButtonEng;
        // }
        // else if (lang.Equals(UserManager.LanguageOption._Polish))
        // {
        //     nextButton.GetComponentInChildren<Text>().text = txtNextButtonPl;
        //     previousButton.GetComponentInChildren<Text>().text = txtPreviousButtonPl;
        //     playButton.GetComponentInChildren<Text>().text = txtPlayButtonPl;
        // }

        this.index = 0;

        // bitalinoUseFlag = BitalinoController.bitalinoController.bitalinoUse;     // F2

        texts = new List<string> ();
        int currentSceneNumber = SceneManager.GetActiveScene().buildIndex;
        // TextsUpdate(bitalinoUseFlag, currentSceneNumber);     // F2
        TextsUpdate(currentSceneNumber);
        textShowed.text = texts[this.index];

        // playButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
		{
            LogManager.logManager.AddEvent(Time.time, "Key;S");
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
		}

        if(timeToStart < Time.timeSinceLevelLoad)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }


        if (Input.GetKeyDown(KeyCode.Escape))
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
			// EditorApplication.isPlaying = false;
		}

        // if (this.index == 0)
        // {
        //     nextButton.gameObject.SetActive(true);
        //     previousButton.gameObject.SetActive(false);
        // }

        // else if (this.index == (texts.Count - 2))
        // {
        //     nextButton.gameObject.SetActive(true);
        // }

        // else if (this.index == (texts.Count - 1))
        // {
        //     nextButton.gameObject.SetActive(false);
        //     playButton.gameObject.SetActive(true); // if user gets to the end of tutorial, remain visible
        // }

        // if (this.index >= 1)
        // {
        //     previousButton.gameObject.SetActive(true);
        // }

        // tutorial skip:
        if (Input.GetKeyDown(KeyCode.T))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;T");
            StartCoroutine("CountDown");
        }

        else if (Input.GetKeyDown(KeyCode.U))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;U");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
        }

        // if (this.index == -1)
        // {
        //     nextButton.gameObject.SetActive(false);
        //     previousButton.gameObject.SetActive(false);
        //     playButton.gameObject.SetActive(false);
        // }
    }

    public void runGame () 
    {   
        // LogManager.logManager.AddEvent(Time.time, "ButtonClick;Play");
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

    // F2
    // void TextsUpdate(bool usingBitalino, int lvl)
    void TextsUpdate(int lvl)
    {
        // if (usingBitalino)
        // {
        //     if(lang.Equals(UserManager.LanguageOption._Polish))
        //     {
        //         if (lvl == 1)
        //         {
        //             texts.Add("Witamy na kolejnej sesji.\nCo tak niewyraźnie wyglądasz?\nNie poznajesz nas. Ok, w porządku. Zaczniemy od początku.");
        //             texts.Add("Przyszedłeś tutaj ze względu na problemy z pamięcią, a my jesteśmy Twoimi terapeutami.\nBrzmi znajomo? Nie? No, dobra.");
        //             texts.Add("Tutaj jest nasza umowa, a tutaj Twój podpis. Zaznaczyliśmy najważniejszy fragment.\n\n\"Zgadzam się na niekonwencjonalne metody terapii psychologicznej.\"\n" + UserManager.userManager.GetUserName());
        //             texts.Add("Ok, skoro już wszystko jasne, przypomnimy Ci kilka zagadnień z psychologii.\nNasza terapia opiera się na psychoanalizie, której autorem jest Zygmunt Freud.");
        //             texts.Add("W jego koncepcji psychika działa na trzech poziomach. Świadomości, przedświadomości i nieświadomości.\nNajwiększy wpływ na późniejsze życie ma dzieciństwo.");
        //             texts.Add("No, więc właśnie tam sie udajemy.\nDo dzieciństwa poprzez wszystkie trzy poziomy.\nZakładaj sprzęt i lecimy, nie ma czasu! ");
        //             texts.Add("Tak, tak, te elektrody pozwolą Ci pozostać z nami w kontakcie.\nNie bój się, robiliśmy to miliony razy.");
        //         }

        //         else if (lvl == 3)
        //         {
        //             texts.Add("Witaj ponownie!\nNo, następnym razem lepiej się pospiesz. Wiesz ile te terapie kosztują w dzisiejszych czasach.");
        //             texts.Add("Dobra, chyba nie ma sensu rozwodzić się nad tym, co Cię czeka.\nUdasz się do świadomości. Jest to najbardziej zewnętrzny poziom, oznacza to, że twoja kontrola będzie największa.");
        //             texts.Add("Uważaj na potwory, szczególnie te święcące najciemniejszym światłem.\n Pamiętaj, że przeciwnicy mogą Cię wyczuwać. Im bardziej jesteś wyluzowyany, tym więcej ich po Ciebie przyjdzie. ");
        //             texts.Add("Aha, będą też się szybciej poruszać.\nAle przecież masz pistolet!\nSpokojnie!");
        //             texts.Add("Na tym poziomie znowu spotkasz swoje ulubione zajączki, które tak chętnie dokarmiałeś sałatą.\nNie wiemy, co im się stało.\nTo Twoja głowa.\nW każdym razie zwykłą sałatą na pewno ich nie przekupisz.");
        //         }

        //         else if (lvl == 5)
        //         {
        //             texts.Add("No, no, gratulacje!\nOd razu widać, że bardziej się starasz!\n\"Musimy zejść głębiej!\"\nPamiętasz Incepcję? A, no fakt. Masz probelmy z pamięcią. Musimy to sobie gdzieś zapisać.");
        //             texts.Add("Teraz pora na przedświadomość. Czyli wszystko, co tłumisz.\nKażdy płacz, szukanie pocieszenia w objęciach ulubionego misia.\nTylko, że teraz te misie nie są już takie słodkie.");
        //             texts.Add("Ale spokojnie, spokojnie.\nPrzecież masz Supermoc! Wystarczy, że napniesz swój biceps!\nPrzez kilka sekund, góra pięć. Im dłużej tym więcej wrogów powalisz.");
        //             texts.Add("Możesz też nie powalić żadnego.\nNo cóż, w przedświadomości nie mamy pełnej kontroli.\nAha, moc potrzebuje chwili, żeby rozejść się po wszystkich przeciwnikach. Nie denerwuj się!");
        //             texts.Add("Niestety są też inne minusy.\nUżycie supermocy na pewno Cię trochę zmęczy.\nNie będziesz mieć siły na takie szybkie i mocne strzały.");
        //             texts.Add("Ale dość gadania.\nLecimyyy!");
        //         }

        //         else if (lvl == 7)
        //         {
        //             texts.Add("Wow, to było wspaniałe!\nRobisz postępy!\nChyba pora na nieświadomość.");
        //             texts.Add("Są tutaj wszystkie zdarzenia, które wyparłeś z górnych warstw.\nWśród nich Twoje spotkanie ze słoniami. Dobrze, że nic Ci się wtedy nie stało. Było blisko.");
        //             texts.Add("Słonie trochę zdziczały od tego czasu. Są bardzo niebezpieczne.\nA, jest też druga sprawa.\nTo w końcu nieświadomość.\nMożemy mieć problemy z łącznością.");
        //             texts.Add("Ale nic się nie bój, zostaniemy z Tobą tak długo jak będziemy mogli.\nA poza tym, masz Supermoc.\nSam powiedz, że jest super, a potem wskakuj!");
        //         }

        //         else if (lvl == 9)
        //         {
        //             texts.Add("Eee, sorki za te komplikacje.\nAle poszło Ci świetnie!\n Ktoś tu chce pozbyć się swoich lęków!\nI to nam się podoba!");
        //             texts.Add("Tak, tak! Dobrze rozumiesz, Twoje problemy z pamięcią wiążą się bezpośrednio z Twoimi największymi lękami. Wiesz, co to oznacza.");
        //             texts.Add("Pan Koszmarek.");
        //             texts.Add("Siedzi tam już od dłuższego czasu.\nPora wykurzyć go z mieszkania i zaznać trochę spokoju.");
        //             texts.Add("Kiedy będziesz z nim walczyć, pamietaj, że wszystkie potworki popierają jego rządy.\nJeśli go skrzywdzisz, przybędą mu na pomoc.");
        //             texts.Add("To lecimy!\nMy tutaj z bezpiecznej odległości będziemy się przyglądać.\nW razie jakby były jakieś problemy... No, coś wtedy wymyślimy.");
        //         }
        //     }

        //     else if(lang.Equals(UserManager.LanguageOption._English))
        //     {
        //         if (lvl == 1)
        //         {
        //             texts.Add("Welcome to the next session.\nWhy so agitated?\nYou do not recognize us...\nThat is alright.\nWe will start from the beginning.");
        //             texts.Add("You have come here because of your troubles with memory,\nand we are your therapists.\nSounds familiar? No?\nHave a look at this, then.");
        //             texts.Add("Here is our agreement, and this is your signature. We have highlighed the most important part.\n\n\"I hereby agree for\nunconventional methods of\npsychological treatment.\"\n" + UserManager.userManager.GetUserName());
        //             texts.Add("Ok, if everything is clear now,\nlet’s remind you of some\npsychological basics.\nOur therapy is grounded in psychoanalisys, developed by Sigmund Freud");
        //             texts.Add("According to his theory, human psychology works on three levels: conscious, preconscious,\nand unconscious.\nThe greatest influence on later life comes from one’s childhood.");
        //             texts.Add("So, this is where we are heading now.\nTo the childhood,\nthrough all three levels.\nGet your gear and get going,\nthere is no time to waste!");
        //             texts.Add("Yes, yes, those electrodes will allow you to stay in touch with us.\nWorry not, we have been doing this\na million times.");
        //         }

        //         else if (lvl == 3)
        //         {
        //             texts.Add("Hello again!\nWell, hurry up a little next time, will you?\nYou know, how these therapies\nare expensive these times.");
        //             texts.Add("Alright, there is no point in pondering on what is still\nahead of you.\nYou will head to the consciousness.\nIt is the most external level,\nwhich means that your control\nwill be most unrestricted.");
        //             texts.Add("Beware of monsters, especially those emitting the darkest light.\n Remember, that the enemies\ncan sense you.\nThe more relaxed you are,\nthe more of them will come at you.");
        //             texts.Add("Ah, and they will also come faster.\nRelax, you have a gun!");
        //             texts.Add("On this level, you will meet your favorite bunnies - which you have fed with lettuce so eagerly.\nWe do not know, what happened.\nIt is your head.\nAnyway, it seems that\nthey cannot be bribed with lettuce.");
        //         }

        //         else if (lvl == 5)
        //         {
        //             texts.Add("Well, well, congratulations!\nIt is clear that you are\ntrying harder now!\n\"We have to go deper!\"\nDo you remember Inception?\nAh, right,\nyou have troubles with memory.\nWe need to take a note on this.");
        //             texts.Add("Now, it is time for preconsciousness. That is everything,\nthat you suppress.\nEvery weep, every comfort\nthat you seek in your favorite\n teddy’s embrace.\nThe difference is that the teddies\nare not that cute anymore.");
        //             texts.Add("But rest easy.\nYou have your Superpower!\nYou just have to flex your biceps!\nFor a few seconds, up to five.\nThe longer, the more enemies\nyou will knock down.");
        //             texts.Add("Sometimes you will not\nknock down anyone.\nNo wonder, you do not have your\nfull control in the preconscious.\nBy the way, the power needs some time to spread among all of your enemies. Don’t get too frantic!");
        //             texts.Add("Unfortunately,\nthere are also other drawbacks.\nUsing your superpower\nwill tire you a little.\nYou will not be able to attack as fast and as strong as before using it,\nfor a while.");
        //             texts.Add("But enough talking.\nLet’s goo!");
        //         }

        //         else if (lvl == 7)
        //         {
        //             texts.Add("Wow, that was amazing! \nYou are making progress!\nLooks like it is time\nfor the unconscious.");
        //             texts.Add("Here are all the events that you have repressed from the previous levels.\nIncluding encounter with elephants.\nGood thing\nthat you got out unharmed.\nThat was close.");
        //             texts.Add("The elephants have run wild a bit from that time.\nThey are very dangerous.\nAh, there is one more thing.\nThis is the unconscious, after all.\nThere might be some\ncommunication issues.");
        //             texts.Add("But do not worry, we will stay\nwith you as long as we can.\nBesides, you have your Superpower.\nJust admit yourself,\nhow awesome it is,\nand go get them!");
        //         }

        //         else if (lvl == 9)
        //         {
        //             texts.Add("Err, sorry for those disturbances.\nYou did great, though!\nLooks like someone wants\nto get rid of his fears!\n Way to go!");
        //             texts.Add("Yes, yes! You understand correctly! Your troubles with memory\nare directly connected\nwith your biggest fears.\nYou know what that means.");
        //             texts.Add("Mr Nightmare.");
        //             texts.Add("He has been lingering there\nfor quite a long time.\nIt is time to smoke him out\nand find peace again.");
        //             texts.Add("When you will fight him,\nremember that he is supported\nby all of the other monsters.\nIf you harm him,\nthey will come to his aid.");
        //             texts.Add("Let’s go! \nWe will watch you\nfrom a safe distance.\nIf you will get in trouble...\nWell,\nwe will figure something out then.");
        //         }
        //     }
        // }

        // else
        // {
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
                    texts.Add("Przez cały czas będzemy w kontakcie.\nNie bój się, robiliśmy to miliony razy.");
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
                    texts.Add("Ktoś tu chce pozbyć się swoich lęków!\nI to nam się podoba!\nTak, tak! Dobrze rozumiesz,\nTwoje problemy z pamięcią wiążą się bezpośrednio\nz Twoimi największymi lękami.\nWiesz, co to oznacza.\nPan Koszmarek.\nSiedzi tam już od dłuższego czasu.\nPora wykurzyć go z mieszkania i zaznać trochę spokoju.\nPamietaj, że wszystkie potworki popierają jego rządy.\nJeśli go skrzywdzisz, przybędą mu na pomoc.");
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
        // }
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

	public PlayerMovement playerMovement;
	public CameraFollow cameraFollow;
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

			// F2
			// if (lang.Equals(UserManager.LanguageOption._English))
			// {
			// 	elementsHUD[2].GetComponent<Text>().text = "TRAGIC";
			// }
			// else if (lang.Equals(UserManager.LanguageOption._Polish))
			// {
			// 	elementsHUD[2].GetComponent<Text>().text = "TRAGICZNIE";
			// }
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

