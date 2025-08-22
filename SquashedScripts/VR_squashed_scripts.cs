--- AudioSourceController.cs ---
using UnityEngine;

public class AudioSourceController : MonoBehaviour
{
    [SerializeField] private AudioSource m_audioSource = null;

    private void Update()
    {
        if (!m_audioSource.isPlaying)
            Destroy(gameObject);
    }
}

--- BitalinoManager.cs ---
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using MathNet.Filtering;
using System.Linq;
using UnityEngine.Audio;

public class BitalinoManager : MonoBehaviour
{
	public static BitalinoManager Instance { get; set; }

	private static readonly List<string> DOMAINS = new() { "BTH" };
	private static readonly string SAMPLES_FILENAME = "samples";
	private static readonly string EVENTS_FILENAME = "events";

	// https://www.bitalino.com/storage/uploads/media/revolution-ecg-sensor-datasheet-revb-1.pdf
	private static readonly float OPERATING_VOLTAGE = 3.3f;
	private static readonly float SENSOR_GAIN = 1100;

	private PluxDeviceManager m_pluxDevManager = null;

	[SerializeField] private GameObject m_canvas = null;
	[SerializeField] private Button m_scanButton = null;
	[SerializeField] private Button m_connectButton = null;
	[SerializeField] private Button m_disconnectButton = null;
	[SerializeField] private Button m_startAcquisitionButton = null;
	[SerializeField] private Button m_stopAcquisitionButton = null;
	[SerializeField] private Dropdown m_deviceDropdown = null;
	[SerializeField] private Text m_outputMsgText = null;
	[SerializeField] private Text m_batteryLevelText = null;

	[Tooltip("Desired sampling rate that will be used during the data acquisition stage." +
		"The used units are in Hz (samples/s) (10, 100, 300?)")]
	[SerializeField] private int m_samplingRate = 100;

	[Tooltip("Analog - to - Digital Converter(ADC) resolution. This parameter defines how precise are the " +
				"digital sampled values when compared with the ideal real case scenario. (10 was set for Bitalino)")]
	[SerializeField] private int m_resolution = 10;

	[Tooltip("Select channels to gather data from. Only 2 and 5 were set by default but Bitalino has 6?")]
	[SerializeField] private List<int> m_channels = new() { 2, 5 };

	[SerializeField] private string m_saveFileName = "test";

	[SerializeField] private int m_calibrationSamples = 10000; // about 10 seconds with 1000 sampling rate
	[SerializeField] private float m_ecgPeakThreshold = 1.5f;
	[SerializeField] private int m_samplesForAverageCalc = 5000;
	[SerializeField] private AudioMixer m_mixer = null;
	[SerializeField] private float m_minVolume = -20f;

	public float EcgMax { get; private set; } = float.MinValue;
	public float EcgMin { get; private set; } = float.MaxValue;
	public float EcgAvg { get; private set; } = 0f;
	public float EdaMax { get; private set; } = float.MinValue;
	public float EdaMin { get; private set; } = float.MaxValue;
	public float EdaAvg { get; private set; } = 0f;
	public float HrAvg { get; private set; } = 0f;
	public float HrCalib { get; private set; } = 0f;

	private StreamWriter m_samplesWriter = null;
	private StreamWriter m_eventsWriter = null;
	private bool m_isCalibrating = true;
	private float m_calibratedSamples = 0f;

	private readonly LinkedList<float> m_ecgDatas = new();
	private readonly LinkedList<float> m_edaDatas = new();
	private int m_collectedDataCount = 0;

	[ContextMenu("Set Hr Calibrated")]
	private void SetHrCalibrated()
	{
		HrCalib = 65f;
	}

	[ContextMenu("Increase Hr Calibrated")]
	private void IncreaseHrCalibrated()
	{
		HrCalib += 2f;
	}

	[ContextMenu("Decrease Hr Calibrated")]
	private void DecreaseHrCalibrated()
	{
		HrCalib -= 2f;
	}

	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
		m_canvas.SetActive(GameManager.Instance.IsAffective);

		if (!GameManager.Instance.IsAffective)
			return;

		m_pluxDevManager = new PluxDeviceManager(ScanResults, ConnectionDone, AcquisitionStarted, OnDataReceived, OnEventDetected, OnExceptionRaised);

		// Important call for debug purposes - creates a log file in the root directory of the project.
		m_pluxDevManager.WelcomeFunctionUnity();

		m_scanButton.onClick.AddListener(ScanButtonFunction);
		m_connectButton.onClick.AddListener(ConnectButtonFunction);
		m_disconnectButton.onClick.AddListener(DisconnectButtonFunction);
		m_startAcquisitionButton.onClick.AddListener(StartButtonFunction);
		m_stopAcquisitionButton.onClick.AddListener(StopButtonFunction);
	}

	private void Update()
	{
		if (m_isCalibrating)
			return;

		CalculateAveragesAndHR();

		Debug.Log($"ECG: {EcgMin}, {EcgMax}, {EcgAvg}, EDA: {EdaMin}, {EdaMax}, {EdaAvg}, HR: {HrCalib}, {HrAvg}");

		SetMasterVolume();
	}

	private void OnApplicationQuit()
	{
		try
		{
			if (m_pluxDevManager != null)
			{
				m_pluxDevManager.DisconnectPluxDev();
				Debug.Log("Application ending after " + Time.time + " seconds");
			}
		}
		catch (Exception)
		{
			Debug.Log("Device already disconnected when the Application Quit.");
		}
	}

	private void OnDestroy()
	{
		CloseWriters();
	}

	public void AddEvent(string eventName)
	{
		if (m_eventsWriter == null)
			return;

		m_eventsWriter.WriteLine($"{Time.time};{eventName}");
	}

	#region GUIEvents
	/// <summary>
	/// Method called when the "Scan for Devices" button is pressed.
	/// </summary>
	public void ScanButtonFunction()
	{
		// Search for PLUX devices
		m_pluxDevManager.GetDetectableDevicesUnity(DOMAINS);

		// Disable the "Scan for Devices" button.
		m_scanButton.interactable = false;
	}

	/// <summary>
	/// Method called when the "Connect to Device" button is pressed.
	/// </summary>
	public void ConnectButtonFunction()
	{
		// Disable Connect button.
		m_connectButton.interactable = false;

		// Connect to the device selected in the Dropdown list.
		m_pluxDevManager.PluxDev(m_deviceDropdown.options[m_deviceDropdown.value].text);
	}

	/// <summary>
	/// Method called when the "Disconnect Device" button is pressed. 
	/// </summary>
	public void DisconnectButtonFunction()
	{
		// Disconnect from the device.
		m_pluxDevManager.DisconnectPluxDev();

		// Reboot GUI elements state.
		RebootGUI();
	}

	/// <summary>
	/// Method called when the "Start Acquisition" button is pressed.
	/// </summary>
	public void StartButtonFunction()
	{
		// BITalino (2 Analog sensors)
		// Starting a real-time acquisition from:
		// >>> BITalino [Channels A2 and A5 active]
		m_pluxDevManager.StartAcquisitionUnity(m_samplingRate, m_channels, m_resolution);
	}

	/// <summary>
	/// Method called when the "Stop Acquisition" button is pressed.
	/// </summary>
	public void StopButtonFunction()
	{
		// Stop the real-time acquisition.
		m_pluxDevManager.StopAcquisitionUnity();

		// Enable the "Start Acquisition" button and disable the "Stop Acquisition" button.
		m_startAcquisitionButton.interactable = true;
		m_stopAcquisitionButton.interactable = false;

		CloseWriters();
	}
	#endregion

	#region Callbacks

	/// <summary>
	/// Callback that receives the list of PLUX devices found during the Bluetooth scan.
	/// </summary>
	/// <param name="listDevices">Devices found</param>
	public void ScanResults(List<string> listDevices)
	{
		// Enable the "Scan for Devices" button.
		m_scanButton.interactable = true;

		if (listDevices.Count > 0)
		{
			// Update list of devices.
			m_deviceDropdown.ClearOptions();
			m_deviceDropdown.AddOptions(listDevices);

			// Enable the Dropdown and the Connect button.
			m_deviceDropdown.interactable = true;
			m_connectButton.interactable = true;

			// Show an informative message about the number of detected devices.
			m_outputMsgText.text = "Scan completed.\nNumber of devices found: " + listDevices.Count;
		}
		else
		{
			// Show an informative message stating the none devices were found.
			m_outputMsgText.text = "Bluetooth device scan didn't found any valid devices.";
		}
	}

	/// <summary>
	/// Callback invoked once the connection with a PLUX device was established.
	/// </summary>
	/// <param name="connected">A boolean flag stating if the connection was established with success (true) or not (false).</param>
	public void ConnectionDone(bool connected)
	{
		if (connected)
		{
			// Disable some GUI elements.
			m_scanButton.interactable = false;
			m_deviceDropdown.interactable = false;
			m_connectButton.interactable = false;

			m_startAcquisitionButton.interactable = true;
			m_disconnectButton.interactable = true;

			m_batteryLevelText.text = $"Battery: {m_pluxDevManager.GetBatteryUnity()}%";
		}
		else
		{
			// Enable Connect button.
			m_connectButton.interactable = true;

			// Show an informative message stating the connection with the device was not established with success.
			m_outputMsgText.text = "It was not possible to establish a connection with the device. Please, try to repeat the connection procedure.";
		}
	}

	/// <summary>
	/// Callback invoked once the data streaming between the PLUX device and the computer is started.
	/// </summary>
	/// <param name="acquisitionStarted">A boolean flag stating if the acquisition was started with success (true) or not (false).</param>
	/// <param name="exceptionRaised">A boolean flag that identifies if an exception was raised and should be presented in the GUI (true) or not (false).</param>
	/// <param name="exceptionMessage"></param>
	public void AcquisitionStarted(bool acquisitionStarted, bool exceptionRaised = false, string exceptionMessage = "")
	{
		if (acquisitionStarted)
		{
			// Enable the "Stop Acquisition" button and disable the "Start Acquisition" button.
			m_startAcquisitionButton.interactable = false;
			m_stopAcquisitionButton.interactable = true;

			CreateWriters();
		}
		else
		{
			// Present an informative message about the error.
			m_outputMsgText.text = !exceptionRaised ? "It was not possible to start a real-time data acquisition. Please, try to repeat the scan/connect/start workflow." : exceptionMessage;

			// Reboot GUI.
			RebootGUI();
		}
	}

	/// <summary>
	/// Callback invoked every time an exception is raised in the PLUX API Plugin.
	/// </summary>
	/// <param name="exceptionCode">ID number of the exception to be raised.</param>
	/// <param name="exceptionDescription">Descriptive message about the exception.</param>
	public void OnExceptionRaised(int exceptionCode, string exceptionDescription)
	{
		if (m_pluxDevManager.IsAcquisitionInProgress())
		{
			// Present an informative message about the error.
			m_outputMsgText.text = exceptionDescription;

			// Reboot GUI.
			RebootGUI();
		}
	}

	/// <summary>
	/// Callback that receives the data acquired from the PLUX devices that are streaming real-time data.
	/// </summary>
	/// <param name="nSeq">Number of sequence identifying the number of the current package of data.</param>
	/// <param name="data">Package of data containing the RAW data samples collected from each active channel ([sample_first_active_channel, sample_second_active_channel,...]).</param>
	public void OnDataReceived(int nSeq, int[] data)
	{
		string outputString = $"{nSeq};";
		foreach (var channelData in data)
			outputString += $"{channelData};";

		// ECG
		// Valid range [-1.5mV, 1.5mV] @ VCC = 3.3V
		float ecg_v = ((data[0] / Mathf.Pow(2, m_resolution) - 0.5f) * OPERATING_VOLTAGE) / SENSOR_GAIN;
		float ecg_mv = ecg_v * 1000f;

		// EDA
		// Valid range [0uS, 25uS]
		float eda_us = ((data[1] / Mathf.Pow(2, m_resolution)) * OPERATING_VOLTAGE) / 0.132f;
		//float eda_s = eda_us * Mathf.Pow(10, 6);
		float eda_s = eda_us;

		var filter = OnlineFilter.CreateBandpass(ImpulseResponse.Finite, m_samplingRate, 0.67, 5.0, 6);

		float ecgProcessed = (float)filter.ProcessSample(ecg_mv);

		outputString += $"{ecgProcessed};";
		m_samplesWriter.WriteLine(outputString);

		if (m_collectedDataCount < m_samplesForAverageCalc)
			m_collectedDataCount++;
		else
		{
			m_ecgDatas.RemoveFirst();
			m_edaDatas.RemoveFirst();
		}

		m_ecgDatas.AddLast(ecgProcessed);
		m_edaDatas.AddLast(eda_s);

		if (m_isCalibrating)
		{
			Calibrate(ecgProcessed, eda_s);

			return;
		}

		UpdateMinMax(ecgProcessed, eda_s);
	}

	private readonly List<float> m_calibrationEcg = new();
	private void Calibrate(float ecgData, float edaData)
	{
		m_outputMsgText.text = $"Calibrating... {Mathf.FloorToInt((float)m_calibratedSamples / (float)m_calibrationSamples * 100f)}%";
		
		m_calibratedSamples++;

		if (m_calibratedSamples > m_calibrationSamples / 4 && m_calibratedSamples < m_calibrationSamples)
		{
			UpdateMinMax(ecgData, edaData);
			CalculateAveragesAndHR();

			m_calibrationEcg.Add(ecgData);
		}
		else if (m_calibratedSamples >= m_calibrationSamples)
		{
			m_isCalibrating = false;

			var hrs = GetHrs(m_calibrationEcg);

			HrCalib = hrs.Count > 0 ? Mathf.Clamp(hrs.Average(), 40, 150) : 0;

			m_canvas.SetActive(false);

			m_calibrationEcg.Clear();
		}
	}

	private void UpdateMinMax(float ecgData, float edaData)
	{
		if (ecgData > EcgMax)
			EcgMax = ecgData;

		if (edaData > EdaMax)
			EdaMax = edaData;

		if (ecgData < EcgMin)
			EcgMin = ecgData;

		if (edaData < EdaMin)
			EdaMin = edaData;
	}

	private void CalculateAveragesAndHR()
	{
		if (m_ecgDatas.Count > 0)
			EcgAvg = m_ecgDatas.Average();

		if (m_edaDatas.Count > 0)
			EdaAvg = m_edaDatas.Average();

		var hrs = GetHrs(m_ecgDatas);

		var avg = hrs.Count > 0 ? Mathf.Clamp(hrs.Average(), 40, 150) : 0;

		if (HrAvg == 0)
			HrAvg = avg;
		else
			HrAvg = Mathf.Clamp(Mathf.MoveTowards(HrAvg, avg, 5f * Time.deltaTime), 40, 150);
	}

	private List<float> GetHrs(IEnumerable<float> datas)
	{
		bool peakFound = false;
		bool firstPeakFound = false;
		float peakTimer = 1f / (float)m_samplingRate;
		List<float> hrs = new();

		var threshold = Mathf.Lerp(EcgAvg, EcgMax, m_ecgPeakThreshold);

		foreach (var data in datas)
		{
			if (data > threshold && !peakFound)
			{
				float hr = 60f / peakTimer;

				if (hr > 40f && hr < 150f && firstPeakFound)
					hrs.Add(hr);

				if (!firstPeakFound)
				{
					firstPeakFound = true;

					peakTimer = 0f;
					peakFound = true;
				}
			}

			if (data < threshold && peakFound)
				peakFound = false;

			peakTimer += 1f / (float)m_samplingRate;
		}

		return hrs;
	}

	private void SetMasterVolume()
	{
		if (!GameManager.Instance.IsAffective)
			return;

		float t = Mathf.InverseLerp(EdaMin, EdaMax, EdaAvg);
		m_mixer.SetFloat("Volume", Mathf.Lerp(m_minVolume, 0f, t));
	}

	/// <summary>
	/// Callback that receives the events raised from the PLUX devices that are streaming real-time data.
	/// </summary>
	/// <param name="pluxEvent">Event object raised by the PLUX API.</param>
	public void OnEventDetected(PluxDeviceManager.PluxEvent pluxEvent)
	{
		if (pluxEvent is PluxDeviceManager.PluxDisconnectEvent)
		{
			// Present an error message.
			m_outputMsgText.text =
				"The connection between the computer and the PLUX device was interrupted due to the following event: " +
				(pluxEvent as PluxDeviceManager.PluxDisconnectEvent).reason;

			// Securely stop the real-time acquisition.
			m_pluxDevManager.StopAcquisitionUnity(-1);

			// Reboot GUI.
			RebootGUI();
		}
		else if (pluxEvent is PluxDeviceManager.PluxDigInUpdateEvent)
		{
			PluxDeviceManager.PluxDigInUpdateEvent digInEvent = (pluxEvent as PluxDeviceManager.PluxDigInUpdateEvent);
			Console.WriteLine("Digital Input Update Event Detected on channel " + digInEvent.channel + ". Current state: " + digInEvent.state);
		}
	}
	#endregion

	private void CreateWriters()
	{
		if (m_samplesWriter == null)
		{
			CreateWriter(ref m_samplesWriter, SAMPLES_FILENAME);
			m_samplesWriter.WriteLine("time;ecg;eda;ecg_p;");
		}

		if (m_eventsWriter == null)
		{
			CreateWriter(ref m_eventsWriter, EVENTS_FILENAME);
			m_eventsWriter.WriteLine("time;event;");
		}
	}

	private void CreateWriter(ref StreamWriter writer, string filename)
	{
		int i = 0;

		string path = GetDataFilePath(i, filename);

		while (File.Exists(path))
		{
			i++;
			path = GetDataFilePath(i, filename);
		}

		var file = File.Create(path);

		writer = new StreamWriter(file);
	}

	private void CloseWriters()
	{
		if (m_samplesWriter != null)
		{
			m_samplesWriter.Close();
			m_samplesWriter = null;
		}

		if (m_eventsWriter != null)
		{
			m_eventsWriter.Close();
			m_eventsWriter = null;
		}
	}

	private string GetDataFilePath(int i, string filename)
	{
		return Application.dataPath + "/" + m_saveFileName + "_" + filename + i.ToString() + ".csv";
	}

	private void RebootGUI()
	{
		m_scanButton.interactable = true;
		m_connectButton.interactable = false;
		m_disconnectButton.interactable = false;
		m_startAcquisitionButton.interactable = false;
		m_stopAcquisitionButton.interactable = false;
		m_deviceDropdown.interactable = false;
	}
}

--- Bullet.cs ---
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

--- Castle.cs ---
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Castle : MonoBehaviour
{
	public static Castle Instance { get; set; }

	[SerializeField] private int m_hp = 10;
	[SerializeField] private TMP_Text m_castleHpText = null;
	[SerializeField] private Image m_castleHpImage = null;
	[SerializeField] private string[] m_hpNames = new string[] {
					"Your castle has fallen!",
					"The gate barelly holds!",
					"The gate is damaged!",
					"Your castle is under attack!",
					"Your castle is untouched."};

	private int m_maxHp = 10;

	public int DamageReceivedInRound { get; set; } = 0;

	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
		m_maxHp = m_hp;

		SetUI(1f);
	}

	public void Damage(int damage)
	{
		m_hp -= damage;

		float hpPercent = (float)m_hp / (float)m_maxHp;

		SetUI(hpPercent);

		DamageReceivedInRound++;
	}

	private void SetUI(float hpPercent)
	{
		m_castleHpText.text = m_hpNames[Mathf.Clamp(Mathf.FloorToInt(hpPercent * (m_hpNames.Length - 1)), 0, m_hpNames.Length)];

		m_castleHpImage.fillAmount = hpPercent;
	}
}

--- DamageableProxy.cs ---
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

--- EnemiesManager.cs ---
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

--- EnemiesSpawner.cs ---
using System;
using UnityEngine;

[Serializable]
public class EnemiesSpawner
{
	[SerializeField] private Transform m_spawnPosition = null;
	public Transform SpawnPosition => m_spawnPosition;

	[SerializeField] private Transform[] m_pathToPlayer = null;
	public Transform[] PathToPlayer => m_pathToPlayer;
}
--- Enemy.cs ---
using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
	[SerializeField] private Rigidbody m_rigidbody = null;
	[SerializeField] private Collider m_collider = null;
	[SerializeField] private Animator m_animator = null;
	[SerializeField] private GameObject m_getHitParticles = null;
	[SerializeField] private GameObject m_explodeParticles = null;
	[SerializeField] private int m_hp = 1;
	[SerializeField] private float m_distanceToReachPathPoint = 0.5f;
	[SerializeField] private float m_rotationSpeed = 0.1f;
	[SerializeField] private float m_timeToDestroy = 2f;
	[SerializeField] private GameObject m_audioSourcePrefab = null;
	[SerializeField] private AudioClip m_getHitClip = null;
	[SerializeField] private AudioClip m_explodeClip = null;
	[SerializeField] private AudioClip[] m_spawnClips = null;

	public Action<Enemy> OnDeath { get; set; }

	private EnemiesManager m_enemiesManager = null;
	private Transform[] m_pathToPlayer = null;
	private int m_currentPathPointNumber = 0;
	private Transform m_targetPathPoint = null;
	private bool m_canMove = false;

	private void FixedUpdate()
	{
		if (!m_canMove)
			return;

		var vectorToTarget = m_targetPathPoint.position - transform.position;
		vectorToTarget.y = 0f;
		if (vectorToTarget.sqrMagnitude <= m_distanceToReachPathPoint * m_distanceToReachPathPoint)
		{
			m_currentPathPointNumber++;

			if (m_currentPathPointNumber >= m_pathToPlayer.Length)
			{
				Castle.Instance.Damage(1);

				Explode();

				return;
			}

			m_targetPathPoint = m_pathToPlayer[m_currentPathPointNumber];
		}

		var newPosition = Vector3.MoveTowards(transform.position, m_targetPathPoint.position, m_enemiesManager.EnemySpeed * Time.deltaTime);
		newPosition.y = 0f;

		m_rigidbody.MovePosition(newPosition);
		m_rigidbody.MoveRotation(Quaternion.RotateTowards(m_rigidbody.rotation, Quaternion.LookRotation(vectorToTarget.normalized), m_rotationSpeed * Time.deltaTime));
	}

	public void Initialize(EnemiesManager enemiesManager, Transform[] pathToPlayer)
	{
		m_enemiesManager = enemiesManager;
		m_pathToPlayer = pathToPlayer;
		m_targetPathPoint = m_pathToPlayer[0];

		m_canMove = true;

		PlaySound(m_spawnClips[UnityEngine.Random.Range(0, m_spawnClips.Length)]);

		m_animator.SetBool(UnityEngine.Random.Range(0f, 1f) > 0.5f ? "Walk" : "Run", true);
	}

	public void Damage(int damage)
	{
		m_hp -= damage;
		m_animator.SetTrigger("GetHit");

		Instantiate(m_getHitParticles, transform.position, Quaternion.identity, null);

		PlaySound(m_getHitClip);

		if (m_hp <= 0)
		{
			m_canMove = false;

			OnDeath?.Invoke(this);

			m_animator.SetBool("Die", true);

			m_collider.enabled = false;

			StartCoroutine(DestroyAfterTime());
		}
	}

	public void SelfDestruct()
	{
		OnDeath?.Invoke(this);

		Destroy(gameObject);
	}

	private void Explode()
	{
		Instantiate(m_explodeParticles, transform.position, Quaternion.identity, null);
		
		OnDeath?.Invoke(this);

		PlaySound(m_explodeClip);

		Destroy(gameObject);
	}

	private void PlaySound(AudioClip clip)
	{
		var audioSource = Instantiate(m_audioSourcePrefab, transform.position, Quaternion.identity, null).GetComponent<AudioSource>();
		audioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
		audioSource.PlayOneShot(clip);
	}

	private IEnumerator DestroyAfterTime()
	{
		yield return new WaitForSeconds(m_timeToDestroy);

		Destroy(gameObject);
	}
}

--- EventStrings.cs ---
public class EventStrings
{
    public static string GetWaveEndString(int waveNum, int enemiesSpawned, int spawnPoints, int damageReceived)
    {
        return $"Wave: {waveNum}; SpawnedEnemies: {enemiesSpawned}; SpawnPoints: {spawnPoints}; DamageReceived: {damageReceived};";
    }

    public static string GetEnemiesSpeedString(float speed)
    {
        return $"EnemiesSpeed: {speed};";
    }
}

--- GameManager.cs ---
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

--- GunController.cs ---
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

--- Wave.cs ---
using System;
using UnityEngine;

[Serializable]
public class Wave
{
	[SerializeField] private float m_baseTimeBetweenSpawns = 5f;
	public float BaseTimeBetweenSpawns => m_baseTimeBetweenSpawns;

	[SerializeField] private int m_baseNumOfSpawnersInUse = 1;
	public int BaseNumOfSpawnersInUse => m_baseNumOfSpawnersInUse;

	[SerializeField, Tooltip("In seconds")] private float m_duration = 60f;
	public float Duration => m_duration;

	[SerializeField, Tooltip("In seconds")] private float m_break = 5f;
	public float Break => m_break;
}
--- IDamageable.cs ---
public interface IDamageable
{
	public void Damage(int damage);
}
--- PluxDeviceManager.cs ---
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;

public class PluxDeviceManager
{
    // Declaration of DllImport statements for accessing the functions inside our native PLUX .dll
    [DllImport("plux_unity_interface")]
    private static extern int WelcomeFunction();
    [DllImport("plux_unity_interface")]
    private static extern void PluxDevUnity(string macAddress);
    [DllImport("plux_unity_interface")]
    private static extern void DisconnectPluxDevUnity();
    [DllImport("plux_unity_interface", CallingConvention = CallingConvention.Cdecl)]
    private static extern void StartAcquisitionBySources(int samplingRate, [In] IntPtr sourcesArray, int nbrSources);
    [DllImport("plux_unity_interface")]
    private static extern void StartAcquisitionByNbr(int samplingRate, int numberOfChannel, int resolution);
    [DllImport("plux_unity_interface")]
    private static extern void StartAcquisition(int samplingRate, string activeChannels, int resolution);
    [DllImport("plux_unity_interface")]
    private static extern void StartAcquisitionMuscleBan(int samplingRate, string activeChannels, int resolution, int freqDivisor);
    [DllImport("plux_unity_interface")]
    private static extern void StartLoop();
    [DllImport("plux_unity_interface")]
    private static extern void StopAcquisition();
    [DllImport("plux_unity_interface")]
    private static extern void InterruptAcquisition();
    [DllImport("plux_unity_interface")]
    private static extern int SendDataTo(IntPtr dataIn);
    [DllImport("plux_unity_interface")]
    private static extern int GetNbrChannels();
    [DllImport("plux_unity_interface")]
    private static extern bool GetCommunicationFlag();
    [DllImport("plux_unity_interface")]
    private static extern int GetBattery();
    [DllImport("plux_unity_interface")]
    private static extern void GetDetectableDevices(string domain);
    [DllImport("plux_unity_interface")]
    private static extern void GetAllDetectableDevices();
    [DllImport("plux_unity_interface")]
    private static extern int GetProductId();
    [DllImport("plux_unity_interface")]
    private static extern System.IntPtr GetDeviceType();
    [DllImport("plux_unity_interface")]
    private static extern void SetNewDeviceFoundHandler(IntPtr handlerFunction);
    [DllImport("plux_unity_interface")]
    private static extern void SetOnRawDataHandler(OnRawFrameReceived handlerFunction);
    [DllImport("plux_unity_interface")]
    private static extern void SetOnExceptionRaisedHandler(OnExceptionRaised handlerFunction);
    [DllImport("plux_unity_interface")]
    private static extern void SetOnEventDetectedHandlers(OnDisconnectEventRaised disconnectEventHandlerFunction, OnDigInUpdateEventRaised digInUpdateEventHandlerFunction);
    [DllImport("plux_unity_interface")]
    private static extern void SetParameter(int port, int index, [In] IntPtr data, int dataLen);

    // Declaration of a Plux::Source structure shared with the .dll.
    [StructLayout(LayoutKind.Sequential)]
    public struct PluxSource
    {
        public int port;
        public int freqDivisor;
        public int nBits;
        public int chMask;

        // Constructor responsible for the creation of a Plux::Source.
        // port -> Source port (1...8 for analog ports). Default value is zero.
        // freqDivisor -> Source frequency divisor from acquisition base frequency (>= 1). Default value is 1.
        // nBits -> Source sampling resolution in bits (8 or 16). Default value is 16.
        // chMask -> Bitmask of source channels to sample (bit 0 is channel 0, etc). Default value is 1 (channel 0 only).
        public PluxSource(int port = 0, int freqDivisor = 1, int nBits = 16, int chMask = 1)
        {
            this.port = port;
            this.freqDivisor = freqDivisor;
            this.nBits = nBits;
            this.chMask = chMask;
        }
    }

    // Declaration of the Plux::Event class.
    public class PluxEvent
    {
        // Enumerator defining the types of events that can be raised by the PLUX API.
        public enum PluxEvents
        {
            DigInUpdate = 3, // Digital Input Updated
            Disconnect = 8 // Disconnect Event
        }

        public PluxEvents type;

        // Constructor responsible for the creation of a Plux::Event.
        // type -> PluxEvents enumerator key that identifies the type of event under analysis.
        public PluxEvent(PluxEvents type)
        {
            this.type = type;
        }

    }

    // Declaration of a Plux::DigInUpdateEvent structure shared with the .dll.
    public class PluxDigInUpdateEvent : PluxEvent
    {
        // Event timestamp class.
        public struct PluxClock
        {
            // Enumerator defining the available clock sources used in the PluxDigInUpdateEvent.
            public enum ClockSources
            {
                None,
                RTC,
                FrameCount,
                Bluetooth
            }

            public ClockSources source;
            public int value;

            // Constructor responsible for the creation of a Plux::Clock.
            // source -> Clock source for the current timestamp.
            // value -> Timestamp value.
            public PluxClock(ClockSources source = ClockSources.None, int value = 0)
            {
                this.source = source;
                this.value = value;
            }
        }

        public PluxClock timestamp;
        public int channel;
        public bool state;


        // Constructor responsible for the creation of a Plux::EvtDigInUpdate.
        // timestamp -> Event timestamp.
        // channel -> The digital input which changed state, starting at zero.
        // state -> New state of digital port input. If true, new state is High, otherwise it is Low.
        public PluxDigInUpdateEvent(PluxClock timestamp, int channel, bool state) : base(PluxEvents.DigInUpdate)
        {
            this.timestamp = timestamp;
            this.channel = channel;
            this.state = state;
        }
    }

    // Declaration of a Plux::EvtDisconnect structure shared with the .dll.
    public class PluxDisconnectEvent : PluxEvent
    {
        /// Disconnect reason enumeration.
        public enum PluxDisconnectReason
        {
            Timeout = 1,         // Connection timeout has elapsed.
            ButtonPressed = 2,   // Device button was pressed.
            BatDischarged = 4,   // Device battery is discharged.
        };

        public PluxDisconnectReason reason;


        // Constructor responsible for the creation of a Plux::EvtDigInUpdate.
        // reason -> Reason for the device disconnection.
        public PluxDisconnectEvent(PluxDisconnectReason reason) : base(PluxEvents.Disconnect)
        {
            this.reason = reason;
        }
    }

    // Delegates (needed for callback purposes).
    public delegate void OnRawFrame(int nSeq, int[] dataIn);
    public delegate void OnRawFrameReceived(int nSeq, IntPtr dataIn, int dataInSize);
    public delegate void OnNewDeviceFound(string newDevice);
    public delegate void ScanResults(List<string> listDevices);
    public delegate void ConnectionDone(bool connectionStatus);
    public delegate void AcquisitionStarted(bool acquisitionStatus, bool exceptionRaised = false, string exceptioDescription = "");
    public delegate void OnExceptionRaised(int exceptionCode, string exceptionDescription);
    public delegate void OnEventDetected(PluxEvent pluxEvent);
    public delegate void OnDisconnectEventRaised(PluxDisconnectEvent.PluxDisconnectReason reason);
    public delegate void OnDigInUpdateEventRaised(PluxDigInUpdateEvent.PluxClock.ClockSources clockSource, int clockValue, int channel, bool state);

    // [Generic Variables]
    private Thread ScanningThread;
    private Thread ConnectionThread;
    private Thread AcquisitionThread;
    private ScanResults ScanResultsCallback;
    private ConnectionDone ConnectionDoneCallback;
    private AcquisitionStarted AcquisitionStartedCallback;
    private static Lazy<List<String>> PluxDevsFound = null;
    private bool DeviceConnected = false;
    private int SamplingRate;
    private string ActiveChannelsStr = "";
    private bool AcquisitionStopped = true;
    private static CallbackManager callbackPointer;
    //private BufferAcqSamples BufferedSamples = new BufferAcqSamples();
    private static Lazy<BufferAcqSamples> LazyObject = null;
    private BufferAcqSamples BufferedSamples;
    private volatile object DoubleCheckLock = null;
    private int currThreadNumber = 0;

    // Contructor.
    // scanResultsCallback -> Callback function that will be invoked once the Bluetooth scan for PLUX devices ends.
    // connectionDoneCallback -> Callback function that will be invoked once a connection with a PLUX device is established.
    // acquisitionStartedCallback -> Callback function that will be invoked when the acqusition start request attempt was completed with success or not.
    // onDataReceivedCallback -> Callback function invoked every time a new package of RAW data samples is transmitted by the API.
    // onEventDetectedCallback -> Callback invoked when an event is raised by the PLUX API.
    // onExceptionRaisedCallback -> Callback invoked when an exception is raised by the PLUX API.
    public PluxDeviceManager(ScanResults scanResultsCallback, ConnectionDone connectionDoneCallback, AcquisitionStarted acquisitionStartedCallback, OnRawFrame onDataReceivedCallback, OnEventDetected onEventDetectedCallback, OnExceptionRaised onExceptionRaisedCallback)
    {
        LazyObject = new Lazy<BufferAcqSamples>(InitBufferedSamplesObject);
        PluxDevsFound = new Lazy<List<String>>(InitiListDevFound);

        // Scan callback.
        this.ScanResultsCallback = new ScanResults(scanResultsCallback);

        // On connection successful callback.
        this.ConnectionDoneCallback = new ConnectionDone(connectionDoneCallback);

        // Storage of the AcquisitionStarted callback.
        this.AcquisitionStartedCallback = new AcquisitionStarted(acquisitionStartedCallback);

        // Initialization of the variable storing the callback responsible for receiving the devices found during the scan.
        OnNewDeviceFound onNewDeviceFoundHandler = new OnNewDeviceFound(OnNewDeviceFoundHandler);
        GCHandle onNewDeviceFoundGCHandler = GCHandle.Alloc(onNewDeviceFoundHandler);
        SetNewDeviceFoundHandler(Marshal.GetFunctionPointerForDelegate(onNewDeviceFoundHandler));

        // Initialization of the variable storing the callback responsible for receiving the streamed data.
        OnRawFrameReceived onRawDataHandler = new OnRawFrameReceived(OnRawFrameHandler);
        GCHandle onRawDataGCHandler = GCHandle.Alloc(onRawDataHandler);
        SetOnRawDataHandler(onRawDataHandler);

        // Initialization of the variable storing the callback responsible for receiving the exceptions raised in the PLUX API .dll.
        OnExceptionRaised onExceptionRaisedHandler = new OnExceptionRaised(OnExceptionRaisedHandler);
        GCHandle onExceptionRaisedGCHandler = GCHandle.Alloc(onExceptionRaisedHandler);
        SetOnExceptionRaisedHandler(onExceptionRaisedHandler);

        // Initialization of the variables storing the callbacks responsible for receiving the events raised in the PLUX API .dll.
        OnDisconnectEventRaised onDisconnectEventHandler = new OnDisconnectEventRaised(OnDisconnectEventHandler);
        GCHandle onDisconnectEventGCHandler = GCHandle.Alloc(onDisconnectEventHandler);
        OnDigInUpdateEventRaised onDigInEventHandler = new OnDigInUpdateEventRaised(OnDigInEventHandler);
        GCHandle onDigInEventGCHandler = GCHandle.Alloc(onDigInEventHandler);
        SetOnEventDetectedHandlers(onDisconnectEventHandler, onDigInEventHandler);

        // Initialise helper object that manages threads creating during the scanning and connection processes.
        var unitDispatcher = UnityThreadHelper.Dispatcher;

        // Specification of the callback function (defined on this/the user Unity script) which will receive the acquired data
        // samples as inputs.
        GCHandle onDataReceivedGCHandler = GCHandle.Alloc(onDataReceivedCallback);
        GCHandle onEventDetectedGCHandler = GCHandle.Alloc(onEventDetectedCallback);
        SetCallbackHandler(onDataReceivedCallback, onEventDetectedCallback, onExceptionRaisedCallback);
    }

    // [Redefinition of the imported methods ensuring that they are accessible on other scripts]

    // A simple function used to check if the .dll generated during the build process was successfully imported by Unity.
    public int WelcomeFunctionUnity()
    {
        return WelcomeFunction();
    }

    // Method used to establish a connection between PLUX devices and computer.
    // Behaves like an object constructor.
    // macAddress -> Device unique identifier, i.e., mac-address.
    public void PluxDev(string macAddress)
    {
        Console.WriteLine("Scanning Thread State: " + ScanningThread.ThreadState);
        Console.WriteLine("Selected Device being received: " + macAddress);
        
        // Creation of new thread to manage the connection stage.
        ConnectionThread = new Thread(() => ConnectToPluxDev(macAddress)); ;
        ConnectionThread.Name = "CONNECTION_" + currThreadNumber;
        currThreadNumber++;
        ConnectionThread.Start();
        Console.WriteLine("Connection Thread Started with Success !");
    }

    // Auxiliary method intended to establish a Bluetooth connection between the computer and PLUX device.
    // macAddress -> Device unique identifier, i.e., mac-address.
    private void ConnectToPluxDev(string macAddress)
    {
        try
        {
            PluxDevUnity(macAddress);

            // Check if the connection was established with success.
            DeviceConnected = !IsExceptionInBuffer() ? true : false;
            
            // Send data (connection status) to the MAIN THREAD.
            UnityThreadHelper.Dispatcher.Dispatch(() => ConnectionDoneCallback(DeviceConnected));
        }
        catch (Exception exc)
        {
            Debug.Log("Exception being raised: " + exc.StackTrace);
        }
    }

    // In this method a disconnect attempt between the computer and the PLUX device will be executed.
    // If a real-time acquisition is in progress, then, the API will try to stop it before the disconnect command.
    public void DisconnectPluxDev()
    {
        if (AcquisitionThread != null)
        {
            Console.WriteLine("Thread Unity Forced to Close");
            if (AcquisitionStopped == false)
            {
                Console.WriteLine("Forcing the acquisition stop");
                StopAcquisitionUnity();
            }
        }

        // Check if the device was previously been connected.
        if (DeviceConnected)
        {
            DisconnectPluxDevUnity();
            DeviceConnected = false;
        }
    }

    // Class method used to Start a Real-Time acquisition through Plux::Source configuration:
    // samplingRate -> Desired sampling rate that will be used during the data acquisition stage.
    //                 The used units are in Hz (samples/s)
    // sourcesArray -> List of Sources that define which channels are active and its internal configurations, 
    //				   namely the resolution and frequency divisor.
    public void StartAcquisitionBySourcesUnity(int samplingRate, PluxSource[] sourcesArray)
    {
        // Reboot BufferedSamples object.
        BufferAcqSamples bufferedSamples = LazyObject.Value;
        lock (bufferedSamples)
        {
            bufferedSamples.reinitialise();
        }

        if (!bufferedSamples.getUncaughtExceptionState())
        {
            // >>> Garbage collector memory management.
            GCHandle pinnedArray = GCHandle.Alloc(sourcesArray, GCHandleType.Pinned);
            // >>> Convert to a memory address.
            IntPtr ptr = pinnedArray.AddrOfPinnedObject();
            // >>> Call correspondent .dll method to start the real-time acquisition.
            StartAcquisitionBySources(samplingRate, ptr, sourcesArray.Length);
            // >>> Releasing memory.
            pinnedArray.Free();

            // Start Communication Loop.
            StartLoopUnity();
        }
        else
        {
            throw new Exception("Unable to start a real-time acquisition. It is probable that the connection between the computer and the PLUX device was broke");
        }

        // Update global flag.
        AcquisitionStopped = false;
    }

    // Class method used to Start a Real-Time acquisition:
    // samplingRate -> Desired sampling rate that will be used during the data acquisition stage.
    //                 The used units are in Hz (samples/s)
    // listChannels -> A list where there are specified the active channels. Each entry contains a port number of an active channel.
    // resolution -> Analog-to-Digital Converter (ADC) resolution. This parameter defines how precise are the digital sampled values when
    //               compared with the ideal real case scenario.
    public void StartAcquisitionUnity(int samplingRate, List<int> listChannels, int resolution)
    {
        // Conversion of List of active channels to a string format.
        for (int i = 0; i < 11; i++)
        {
            if (listChannels.Contains(i + 1))
            {
                ActiveChannelsStr += "1";
            }
            else
            {
                ActiveChannelsStr += "0";
            }
        }

        // Reboot BufferedSamples object.
        BufferAcqSamples bufferedSamples = LazyObject.Value;
        lock (bufferedSamples)
        {
            bufferedSamples.reinitialise();
        }

        if (!bufferedSamples.getUncaughtExceptionState())
        {
            // Start of acquisition.
            StartAcquisition(samplingRate, ActiveChannelsStr, resolution);

            // Start Communication Loop.
            StartLoopUnity();
        }
        else
        {
            throw new Exception("Unable to start a real-time acquisition. It is probable that the connection between the computer and the PLUX device was broke");
        }

        // Update global flag.
        AcquisitionStopped = false;
    }

    // Class method used to Start a Real-Time acquisition:
    // samplingRate -> Desired sampling rate that will be used during the data acquisition stage.
    //                 The used units are in Hz (samples/s)
    // numberOfChannels -> Number of the active channel that will be used during data acquisition.
    //                    With bitalino this value should be between 1 and 6 while for biosignalsplux it is possible to collect data
    //                    from up to 8 channels (simultaneously).
    // resolution -> Analog-to-Digital Converter (ADC) resolution. This parameter defines how precise are the digital sampled values when
    //               compared with the ideal real case scenario.
    public void StartAcquisitionByNbrUnity(int samplingRate, int numberOfChannels, int resolution)
    {
        // Start of the real-time acquisition.
        StartAcquisitionByNbr(samplingRate, numberOfChannels, resolution);

        // Start Communication Loop.
        StartLoopUnity();

        // Update global flag.
        AcquisitionStopped = false;
    }

    // Class method used to Start a Real-Time acquisition (on muscleBAN):
    // samplingRate -> Desired sampling rate that will be used during the data acquisition stage.
    //                 The used units are in Hz (samples/s)
    // listChannels -> A list where there are specified the active channels. Each entry contains a port number of an active channel.
    // resolution -> Analog-to-Digital Converter (ADC) resolution. This parameter defines how precise are the digital sampled values when
    //               compared with the ideal real case scenario.
    // freqDivisor -> Frequency divisor, i.e., acquired data will be subsampled accordingly to this parameter. If freqDivisor = 10, it means that each set of 10 acquired samples
    //                will trigger the communication of a single sample (through the communication loop).
    public void StartAcquisitionMuscleBanUnity(int samplingRate, List<int> listChannels, int resolution, int freqDivisor)
    {
        // Conversion of List of active channels to a string format.
        for (int i = 0; i < 8; i++)
        {
            if (listChannels.Contains(i + 1))
            {
                ActiveChannelsStr += "1";
            }
            else
            {
                ActiveChannelsStr += "0";
            }
        }

        // Start of acquisition.
        StartAcquisitionMuscleBan(samplingRate, ActiveChannelsStr, resolution, freqDivisor);
        
        // Start Communication Loop.
        StartLoopUnity();

        // Update global flag.
        AcquisitionStopped = false;
    }

    // Trigger the start of the communication loop (between PLUX device and computer).
    private void StartLoopUnity()
    {
        if(!IsExceptionInBuffer()) { 
            // Creation of new thread to manage the communication loop.
            AcquisitionThread = new Thread(StartLoop);
            AcquisitionThread.Name = "ACQUISITION_" + currThreadNumber;
            currThreadNumber++;
            AcquisitionThread.Start();
            Console.WriteLine("Acquisition Thread Started with Success !");

            // Inform the frontend about the successful start of the real-time acquisition.
            AcquisitionStartedCallback(true);
        }
        else
        {
            // Inform the frontend about the failure in the start of the real-time acquisition.
            AcquisitionStartedCallback(false);
        }
    }

    // Method used to check if any unhandled exception was raised until the moment.
    // raiseException -> A Boolean flag stating if an exception should be explicitly raised (true) or silently flagged (false).
    private bool IsExceptionInBuffer(bool raiseException = false)
    {
        // Lock is an essential step to ensure that variables shared by the same thread will not be accessed at the same time.
        BufferAcqSamples bufferedSamples = LazyObject.Value;
        lock (bufferedSamples)
        {
            if (bufferedSamples.getUncaughtExceptionState())
            {
                bufferedSamples.deactUncaughtException();
                if (raiseException)
                {
                    throw new ExternalException(
                        "An exception with unknown origin was raised, but it is not fatal. It is probable that the device connection was lost...");
                }
                else
                {
                    return true;
                }
            }

            return false;
        }
    }

    // Callback function responsible for receiving the devices found during the Bluetooth scan.
    // newDeviceFound -> MAC-Address of the device found during the scan.
    private void OnNewDeviceFoundHandler(string newDeviceFound)
    {
        try
        {
            // Store the device into a class variable.
            PluxDevsFound.Value.Add(newDeviceFound);
        }
        catch (OutOfMemoryException exception)
        {
            Debug.Log("OutOfMemory Exception raised: " + exception);
        }
        catch (Exception exception)
        {
            Debug.Log("Unexpected Exception raised: " + exception);
        }
    }

    // Callback function responsible for receiving the acquired data samples from the communication loop started by StartLoopUnity().
    // nSeq -> Sequence number, i.e., the number of the acquired data sample.
    // data -> Pointer to an array containing the sample value for each active channel, for example, if we are conducting an acquisition with 3 channels, the first three entries of "data" will contain the values that we desired.
    // dataInSize -> Size of the array referenced in the data pointer.
    private void OnRawFrameHandler(int nSeq, IntPtr data, int dataInSize)
    {
        lock (callbackPointer)
        {
            // Convert our data pointer to an array format.
            int[] dataArray = new int[dataInSize];
            Marshal.Copy(data, dataArray, 0, dataInSize);

            // Check if an exception was raised.
            if (!IsExceptionInBuffer()) { 
                // Send data (RAW frames) to the MAIN THREAD.
                UnityThreadHelper.Dispatcher.Dispatch(() => callbackPointer.onRawFrameReference(nSeq, dataArray));
            }
        }
    }

    // Callback function responsible for receiving the info about the exceptions raised in the PLUX API .dll file.
    // exceptionCode -> ID number of the exception to be raised.
    // exceptionDescription -> Descriptive message about the exception.
    private void OnExceptionRaisedHandler(int exceptionCode, string exceptionDescription)
    {
        lock (callbackPointer)
        {
            BufferAcqSamples bufferedSamples = LazyObject.Value;
            lock (bufferedSamples)
            {
                bufferedSamples.actUncaughtException();
                Debug.Log("Exception being raised in the PLUX C++ API Wrapper:\n" + exceptionCode + " | " + exceptionDescription);

                // Inform the GUI about the raise of an exception.
                UnityThreadHelper.Dispatcher.Dispatch(() => callbackPointer.OnExceptionRaisedReference(exceptionCode, exceptionDescription));
            }
        }
    }

    // Callback intended to communicate information about a "Disconnect" event detected in this wrapper.
    // reason -> Reason for the device disconnection.
    private void OnDisconnectEventHandler(PluxDisconnectEvent.PluxDisconnectReason reason)
    {
        lock (callbackPointer)
        {
            // Send data (event) to the MAIN THREAD.
            UnityThreadHelper.Dispatcher.Dispatch(() => callbackPointer.onEventDetectedReference(new PluxDisconnectEvent(reason)));
        }
    }

    // Callback intended to communicate information about a "Digital Input Update" event detected in this wrapper.
    // clockSource -> Clock source for the current timestamp.
    // clockValue -> Timestamp value.
    // channel -> The digital input which changed state, starting at zero.
    // state -> New state of digital port input. If true, new state is High, otherwise it is Low.
    private void OnDigInEventHandler(PluxDigInUpdateEvent.PluxClock.ClockSources clockSource, int clockValue, int channel, bool state)
    {
        lock (callbackPointer)
        {
            // Send data (event) to the MAIN THREAD.
            UnityThreadHelper.Dispatcher.Dispatch(() => callbackPointer.onEventDetectedReference(new PluxDigInUpdateEvent(new PluxDigInUpdateEvent.PluxClock(clockSource, clockValue), channel, state)));
        }
    }

    // Class method used to interrupt the real-time communication loop.
    private void InterruptAcquisitionUnity()
    {
        InterruptAcquisition();
    }

    // Method dedicated to stop the real-time acquisition.
    // forceStop -> An identifier that specify when the stop command was voluntarily sent by the user (>=0) or forced  by an event or exception (-1, -2...).
    // RETURN (bool): A flag identifying when the acquisition was stopped in a forced way (true) or triggered by the user (false).
    public bool StopAcquisitionUnity(int forceStop=0)
    {
        // Returned variable.
        bool forceFlag = false;

        // Check if the StopButtonFunction was invoked by the user (button click) or after a Disconnect Event was triggered.
        if (AcquisitionThread != null)
        {
            if (forceStop >= 0)
            {
                // Interrupt real-time communication loop.
                Console.WriteLine("Communication Flag (Before Interrupt): " + GetCommunicationFlag());
                InterruptAcquisition();

                // Wait for the communication of the flag stating the end of the communication loop.
                bool communicationFlag = GetCommunicationFlag();
                Console.WriteLine("Communication Flag (After Interrupt): " + GetCommunicationFlag());
                while (communicationFlag == true)
                {
                    communicationFlag = GetCommunicationFlag();
                }

                Console.WriteLine("Communication Flag (After Loop): " + GetCommunicationFlag());

                // Stop acquisition.
                StopAcquisition();
                Console.WriteLine("Thread State: " + AcquisitionThread.ThreadState);
                AcquisitionThread.Abort();
                Console.WriteLine("Thread State (After Aborting): " + AcquisitionThread.ThreadState);
            }
            else
            {
                // Close Thread.
                Console.WriteLine("Thread State: " + AcquisitionThread.ThreadState + "_" + Thread.CurrentThread.Name);
                AcquisitionThread.Abort();
                Console.WriteLine("Thread State (After Aborting): " + AcquisitionThread.ThreadState);

                // Disconnect device if a forced stop occurred.
                if (forceStop == -1)
                {
                    //DisconnectPluxDev();
                }

                // Update forceFlag.
                forceFlag = true;

                // Debug Message.
                Console.WriteLine("Real-Time Data Acquisition stopped due to the lost of connection with PLUX Device.");
            }

            // Reboot variables.
            ActiveChannelsStr = "";

            // Update global flag.
            AcquisitionStopped = true;

            // Reboot AcquisitionThread
            AcquisitionThread = null;
        }

        // Clear the buffer containing the packages of collected data.
        RebootDataBuffer();

        return forceFlag;
    }

    // Class method intended to find the list of detectable devices through Bluetooth communication.
    // domains -> Array of strings that defines which domains will be used while searching for PLUX devices 
    //            [Valid Options: "BTH" -> classic Bluetooth; "BLE" -> Bluetooth Low Energy; "USB" -> Through USB connection cable]
    public void GetDetectableDevicesUnity(List<string> domains)
    {
        // Creation of new thread to manage the scanning stage.
        ScanningThread = new Thread(() => ScanPluxDevs(domains)); ;
        ScanningThread.Name = "SCANNING_" + currThreadNumber;
        currThreadNumber++;
        ScanningThread.Start();
        Console.WriteLine("Scanning Thread Started with Success !");
    }

    // Auxiliary function that manages the scanning process.
    // domains -> Array of strings that defines which domains will be used while searching for PLUX devices 
    //            [Valid Options: "BTH" -> classic Bluetooth; "BLE" -> Bluetooth Low Energy; "USB" -> Through USB connection cable]
    private void ScanPluxDevs(List<string> domains)
    {
        try
        {
            // Search for BLE and BTH devices.
            List<string> listDevices = new List<string>();
            List<String> devicesFound = PluxDevsFound.Value;

            // Clear previous content of the device list.
            devicesFound.Clear();
            for (int domainNbr = 0; domainNbr < domains.Count; domainNbr++)
            {
                try
                {
                    // List of available Devices.
                    GetDetectableDevices(domains[domainNbr]);
                }
                catch (OutOfMemoryException exception)
                {
                    Debug.Log("OutOfMemory Exception raised [Point 1]: " + exception);
                }
                catch (Exception exception)
                {
                    Debug.Log("Unexpected Exception raised: " + exception);
                }
            }

            // Send data (list of devices found) to the MAIN THREAD.
            UnityThreadHelper.Dispatcher.Dispatch(() => ScanResultsCallback(devicesFound));
        }
        catch (ExecutionEngineException exc)
        {
            Debug.Log("Exception found while scanning: \n" + exc.Message + "\n" + exc.StackTrace);
            BufferAcqSamples bufferedSamples = LazyObject.Value;
            lock (bufferedSamples)
            {
                bufferedSamples.actUncaughtException();
            }
        }
    }

    // Definition of the callback function responsible for managing the acquired data (which is defined on users Unity script).
    // onRawFrameHandler -> Callback function invoked every time a new package of RAW data samples is transmitted by the API.
    // onEventDetectedHandler -> Callback invoked when an event is raised by the PLUX API.
    // onExceptionRaisedHandler -> Callback invoked when an exception is raised by the PLUX API.
    private bool SetCallbackHandler(OnRawFrame onRawFrameHandler, OnEventDetected onEventDetectedHandler, OnExceptionRaised onExceptionRaisedHandler)
    {
        callbackPointer = new CallbackManager(onRawFrameHandler, onEventDetectedHandler, onExceptionRaisedHandler);
        return true;
    }

    // "Setting" method intended to define the value of a specific parameter. the type of the connected device.
    // port -> Sensor port number for a sensor parameter, or zero for a system parameter.
    // index -> Index of the parameter to set within the sensor or system.
    // data -> List containing the values to assign to the parameter under analysis.
    public void SetParameter(int port, int index, int[] data)
    {
        // >>> Garbage collector memory management.
        GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
        // >>> Convert to a memory address.
        IntPtr ptr = pinnedArray.AddrOfPinnedObject();
        // >>> Call correspondent .dll method to set the parameter value.
        SetParameter(port, index, ptr, data.Length);
        // >>> Releasing memory.
        pinnedArray.Free();
    }

    // "Getter" method for determination of the number of used channels during the acquisition.
    public int GetNbrChannelsUnity()
    {
        return GetNbrChannels();
    }

    // "Getter" method for checking the state of the communication flag.
    public bool GetCommunicationFlagUnity()
    {
        return GetCommunicationFlag();
    }

    // "Getter" method dedicated to check the battery level of the device.
    public int GetBatteryUnity()
    {
        return GetBattery();
    }

    // "Getter" method intended to check the product ID of the connected device.
    public int GetProductIdUnity()
    {
        return GetProductId();
    }

    // "Getter" method intended to check if a real-time acquisition is currently running.
    public bool IsAcquisitionInProgress()
    {
        return AcquisitionThread != null;
    }

    // "Getter" method intended to check the type of the connected device.
    public string GetDeviceTypeUnity()
    {
        return Marshal.PtrToStringAnsi(GetDeviceType());
    }

    // Class that manages the reference to callbackPointer.
    public class CallbackManager
    {
        public OnRawFrame onRawFrameReference;
        public OnEventDetected onEventDetectedReference;
        public OnExceptionRaised OnExceptionRaisedReference;
        public CallbackManager(OnRawFrame onRawFrameHandler, OnEventDetected onEventDetectedHandler, OnExceptionRaised onExceptionRaisedHandler)
        {
            onRawFrameReference = onRawFrameHandler;
            onEventDetectedReference = onEventDetectedHandler;
            OnExceptionRaisedReference = onExceptionRaisedHandler;
        }
    }

    // Auxiliary subclass that works as a buffer of received samples.
    public class BufferAcqSamples
    {
        private int comCounter = 0;
        private int[][] packagesOfData;
        private int maxNbrSamples = 10000;
        private bool rebootOnNextPackage = false;
        private bool uncaugthException = false;
        private int lastNSeq = -1;

        // Class constructor.
        public BufferAcqSamples()
        {
            packagesOfData = new int[maxNbrSamples][]; // Stores 10 seconds of data in data acquisitions of 1000 Hz sampling rate.
        }

        // An important method that ensures the reinitialisation of the class variables.
        public void reinitialise()
        {
            comCounter = 0;
            packagesOfData = new int[maxNbrSamples][];
            rebootOnNextPackage = false;
            uncaugthException = false;
            lastNSeq = -1;
        }

        // Add samples to the buffer.
        // nSeq -> Sequence number that univocally identifies the package.
        // newPackage -> Package of data to be added to the memory data structure of this object.
        public void addSamples(int nSeq, int[] newPackage)
        {
            // Check if the new package of data is the valid one, i.e., if it is the one immediately after the last received package.
            if (nSeq <= lastNSeq || nSeq > lastNSeq + 1)
            {
                actUncaughtException();
            }
            else
            {
                lastNSeq = nSeq;
            }
            
            // Reboot buffer if the controlling flag is true.
            if (rebootOnNextPackage)
            {
                // Reboot flag.
                rebootOnNextPackage = false;

                // Re-Initialise array.
                packagesOfData = new int[maxNbrSamples][];
                restart();
            }

            // Check if the maximum capacity of the buffer was reached.
            if (comCounter == maxNbrSamples)
            {
                // Shift data.
                Array.Copy(packagesOfData, 1, packagesOfData, 0, packagesOfData.Length - 1);

                // Decrement counter.
                decrement();
            }
            packagesOfData[comCounter] = newPackage;

            // Update counter.
            increment();
        }

        // Activate UncaughtException flag.
        public void actUncaughtException()
        {
            uncaugthException = true;
        }

        // Deactivate UncaughtException flag.
        public void deactUncaughtException()
        {
            uncaugthException = false;
        }

        // Increment the counter value.
        private void increment()
        {
            comCounter++;
        }

        // Decrement the counter value.
        private void decrement()
        {
            comCounter--;
        }
           
        // Method used to reboot the object memory.
        public void reboot()
        {
            // Reboot flag.
            rebootOnNextPackage = false;

            // Re-Initialise array.
            packagesOfData = new int[maxNbrSamples][];
        }

        // Restart counter.
        private void restart()
        {
            comCounter = 0;
        }

        // Get the counter value.
        public int getCounter()
        {
            return comCounter;
        }

        // Get available packages of data.
        // rebootMemory -> When true the stored data inside BufferedSamples object is re-initialized.
        public int[][] getPackages(bool rebootMemory)
        {
            // Check if the array is not empty.
            if (packagesOfData[0] == null)
            {
                return null;
            }
            // Send the filled section of the array.
            else if (comCounter != maxNbrSamples)
            {
                int[][] tempArray = new int[comCounter][];
                Array.Copy(packagesOfData, 0, tempArray, 0, comCounter);

                // Update flag.
                rebootOnNextPackage = rebootMemory;

                return tempArray;
            }
            else
            {
                // Update flag.
                rebootOnNextPackage = rebootMemory;

                return packagesOfData;
            }
        }

        // Get Uncaught Exception state.
        public bool getUncaughtExceptionState()
        {
            return uncaugthException;
        }
    }

    /**
     * Auxiliary method used to reboot the buffer responsible for storing the packages of collected data.
     */
    public void RebootDataBuffer()
    {
        // Clear the buffer containing the packages of collected data.
        // Lock is an essential step to ensure that variables shared by the same thread will not be accessed at the same time.
        BufferAcqSamples bufferedSamples = LazyObject.Value;
        lock (bufferedSamples)
        {
            bufferedSamples.reboot();
        }
    }

    // Auxiliary method that ensures a secure lock.
    // https://www.pluralsight.com/guides/lock-statement-best-practices
    private object InitializeIfNeeded()
    {
        if (DoubleCheckLock == null)
        {
            lock (BufferedSamples)
            {
                if (DoubleCheckLock == null)
                {
                    DoubleCheckLock = true;
                }
            }
        }

        return DoubleCheckLock;
    }

    // Factory for our Multi-Thread lazy object.
    static BufferAcqSamples InitBufferedSamplesObject()
    {
        BufferAcqSamples lazyComponent = new BufferAcqSamples();
        return lazyComponent;
    }

    static List<String> InitiListDevFound()
    {
        List<String> devFound = new List<string>();
        return devFound;
    }
}

--- ActionExtension.cs ---
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace UnityThreading
{
	public static class ActionExtension
	{
        /// <summary>
        /// Starts the Action as async Task.
        /// </summary>
        /// <returns>The task.</returns>
		public static Task RunAsync(this Action that)
		{
			return that.RunAsync(UnityThreadHelper.TaskDistributor);
		}

        /// <summary>
        /// Starts the Action as async Task on the given TaskDistributor.
        /// </summary>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <returns>The task.</returns>
		public static Task RunAsync(this Action that, TaskDistributor target)
		{
			return target.Dispatch(that);
		}

        /// <summary>
        /// Converts the Action into an inactive Task.
        /// </summary>
        /// <returns>The task.</returns>
		public static Task AsTask(this Action that)
		{
			return Task.Create(that);
		}

        /// <summary>
        /// Starts the Func as async Task.
        /// </summary>
        /// <returns>The task.</returns>
		public static Task<T> RunAsync<T>(this Func<T> that)
		{
			return that.RunAsync(UnityThreadHelper.TaskDistributor);
		}

        /// <summary>
        /// Starts the Func as async Task on the given TaskDistributor.
        /// </summary>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <returns>The task.</returns>
		public static Task<T> RunAsync<T>(this Func<T> that, TaskDistributor target)
		{
			return target.Dispatch(that);
		}

        /// <summary>
        /// Converts the Func into an inactive Task.
        /// </summary>
        /// <returns>The task.</returns>
		public static Task<T> AsTask<T>(this Func<T> that)
		{
			return new Task<T>(that);
		}
	}
}

--- Channel.cs ---
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace UnityThreading
{
	public class Channel<T> : IDisposable
	{
		private List<T> buffer = new List<T>();
		private object setSyncRoot = new object();
		private object getSyncRoot = new object();
		private object disposeRoot = new object();
		private ManualResetEvent setEvent = new ManualResetEvent(false);
		private ManualResetEvent getEvent = new ManualResetEvent(true);
		private ManualResetEvent exitEvent = new ManualResetEvent(false);
		private bool disposed = false;

		public int BufferSize { get; private set; }

		public Channel()
			: this(1)
		{
		}

		public Channel(int bufferSize)
		{
			if (bufferSize < 1)
				throw new ArgumentOutOfRangeException("bufferSize", "Must be greater or equal to 1.");

			this.BufferSize = bufferSize;
		}

		~Channel()
		{
			Dispose();
		}

		public void Resize(int newBufferSize)
		{
			if (newBufferSize < 1)
				throw new ArgumentOutOfRangeException("newBufferSize", "Must be greater or equal to 1.");

			lock (setSyncRoot)
			{
				if (disposed)
					return;

				var result = WaitHandle.WaitAny(new WaitHandle[] { exitEvent, getEvent });
				if (result == 0)
					return;

				buffer.Clear();

				if (newBufferSize != BufferSize)
					BufferSize = newBufferSize;
			}
		}

		public bool Set(T value)
		{
			return Set(value, int.MaxValue);
		}

		public bool Set(T value, int timeoutInMilliseconds)
		{
			lock (setSyncRoot)
			{
				if (disposed)
					return false;
			
				var result = WaitHandle.WaitAny(new WaitHandle[] { exitEvent, getEvent }, timeoutInMilliseconds);
				if (result == WaitHandle.WaitTimeout || result == 0)
					return false;

				buffer.Add(value);
				if (buffer.Count == BufferSize)
				{
					setEvent.Set();
					getEvent.Reset();
				}

				return true;
			}
		}

		public T Get()
		{
			return Get(int.MaxValue, default(T));
		}

		public T Get(int timeoutInMilliseconds, T defaultValue)
		{
			lock (getSyncRoot)
			{
				if (disposed)
					return defaultValue;

				var result = WaitHandle.WaitAny(new WaitHandle[] { exitEvent, setEvent }, timeoutInMilliseconds);
				if (result == WaitHandle.WaitTimeout || result == 0)
					return defaultValue;

				var value = buffer[0];
				buffer.RemoveAt(0);
				if (buffer.Count == 0)
				{
					getEvent.Set();
					setEvent.Reset();
				}

				return value;
			}
		}

		public void Close()
		{
			lock (disposeRoot)
			{
				if (disposed)
					return;

				exitEvent.Set();
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (disposed)
				return;

			lock (disposeRoot)
			{
				exitEvent.Set();

				lock (getSyncRoot)
				{
					lock (setSyncRoot)
					{
						setEvent.Close();
						setEvent = null;

						getEvent.Close();
						getEvent = null;

						exitEvent.Close();
						exitEvent = null;

						disposed = true;
					}
				}
			}
		}

		#endregion
	}

	public class Channel : Channel<object>
	{
	}
}

--- Dispatcher.cs ---
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace UnityThreading
{
    public abstract class DispatcherBase : IDisposable
    {
        protected int lockCount = 0;
		protected object taskListSyncRoot = new object();
        protected Queue<Task> taskList = new Queue<Task>();
        protected Queue<Task> delayedTaskList = new Queue<Task>();
        protected ManualResetEvent dataEvent = new ManualResetEvent(false);

        public DispatcherBase()
        {

        }
		
        public bool IsWorking
		{
			get
			{
				return dataEvent.InterWaitOne(0);
			}
		}

        public bool AllowAccessLimitationChecks;

        /// <summary>
        /// Set the task reordering system
        /// </summary>
        public TaskSortingSystem TaskSortingSystem;

		/// <summary>
		/// Returns the currently existing task count. Early aborted tasks will count too.
		/// </summary>
        public virtual int TaskCount
        {
            get
            {
				lock (taskListSyncRoot)
                    return taskList.Count;
            }
        }

        public void Lock()
        {
            lock (taskListSyncRoot)
            {
                lockCount++;
            }
        }

        public void Unlock()
        {
            lock (taskListSyncRoot)
            {
                lockCount--;
                if (lockCount == 0 && delayedTaskList.Count > 0)
                {
                    while (delayedTaskList.Count > 0)
                        taskList.Enqueue(delayedTaskList.Dequeue());

                    if (TaskSortingSystem == UnityThreading.TaskSortingSystem.ReorderWhenAdded ||
                    TaskSortingSystem == UnityThreading.TaskSortingSystem.ReorderWhenExecuted)
                        ReorderTasks();

                    TasksAdded();
                }
            }
        }

		/// <summary>
		/// Creates a new Task based upon the given action.
		/// </summary>
		/// <typeparam name="T">The return value of the task.</typeparam>
		/// <param name="function">The function to process at the dispatchers thread.</param>
		/// <returns>The new task.</returns>
		public Task<T> Dispatch<T>(Func<T> function)
        {
			CheckAccessLimitation();

            var task = new Task<T>(function);
            AddTask(task);
            return task;
        }

		/// <summary>
		/// Creates a new Task based upon the given action.
		/// </summary>
		/// <param name="action">The action to process at the dispatchers thread.</param>
		/// <returns>The new task.</returns>
        public Task Dispatch(Action action)
        {
			CheckAccessLimitation();

            var task = Task.Create(action);
            AddTask(task);
            return task;
        }

        /// <summary>
        /// Dispatches a given Task.
        /// </summary>
        /// <param name="action">The action to process at the dispatchers thread.</param>
        /// <returns>The new task.</returns>
        public Task Dispatch(Task task)
        {
            CheckAccessLimitation();

            AddTask(task);
            return task;
        }

		internal virtual void AddTask(Task task)
        {
            lock (taskListSyncRoot)
            {
                if (lockCount > 0)
                {
                    delayedTaskList.Enqueue(task);
                    return;
                }

                taskList.Enqueue(task);
                
                if (TaskSortingSystem == UnityThreading.TaskSortingSystem.ReorderWhenAdded ||
                    TaskSortingSystem == UnityThreading.TaskSortingSystem.ReorderWhenExecuted)
					 ReorderTasks();
            }
			TasksAdded();
        }

        internal void AddTasks(IEnumerable<Task> tasks)
        {
            lock (taskListSyncRoot)
            {
                if (lockCount > 0)
                {
                    foreach (var task in tasks)
                        delayedTaskList.Enqueue(task);
                    return;
                }

                foreach (var task in tasks)
                    taskList.Enqueue(task);

                if (TaskSortingSystem == UnityThreading.TaskSortingSystem.ReorderWhenAdded || TaskSortingSystem == UnityThreading.TaskSortingSystem.ReorderWhenExecuted)
					 ReorderTasks();
            }
			TasksAdded();
        }

        internal virtual void TasksAdded()
        {
            dataEvent.Set();
        }

		protected void ReorderTasks()
        {
			taskList = new Queue<Task>(taskList.OrderBy(t => t.Priority));
        }

		internal IEnumerable<Task> SplitTasks(int divisor)
        {
			if (divisor == 0)
				divisor = 2;
			var count = TaskCount / divisor;
            return IsolateTasks(count);
        }

        internal IEnumerable<Task> IsolateTasks(int count)
        {
			Queue<Task> newTasks = new Queue<Task>();

			if (count == 0)
				count = taskList.Count;

            lock (taskListSyncRoot)
            {
				for (int i = 0; i < count && i < taskList.Count; ++i)
					newTasks.Enqueue(taskList.Dequeue());
                
				//if (TaskSortingSystem == TaskSortingSystem.ReorderWhenExecuted)
				//    taskList = ReorderTasks(taskList);

				if (TaskCount == 0)
					dataEvent.Reset();
            }

			return newTasks;
        }

		protected abstract void CheckAccessLimitation();

        #region IDisposable Members

        public virtual void Dispose()
        {
			while (true)
			{
				Task currentTask;
                lock (taskListSyncRoot)
				{
                    if (taskList.Count != 0)
						currentTask = taskList.Dequeue();
                    else
                        break;
				}
				currentTask.Dispose();
			}

			dataEvent.Close();
			dataEvent = null;
        }

        #endregion
    }

	public class NullDispatcher : DispatcherBase
	{
		public static NullDispatcher Null = new NullDispatcher();
		protected override void CheckAccessLimitation()
		{
		}

		internal override void AddTask(Task task)
		{
			task.DoInternal();
		}
	}

    public class Dispatcher : DispatcherBase
    {
        [ThreadStatic]
        private static Task currentTask;

        [ThreadStatic]
        internal static Dispatcher currentDispatcher;
        
        protected static Dispatcher mainDispatcher;

		/// <summary>
		/// Returns the task which is currently being processed. Use this only inside a task operation.
		/// </summary>
        public static Task CurrentTask
        {
            get
            {
                if (currentTask == null)
                    throw new InvalidOperationException("No task is currently running.");

                return currentTask;
            }
        }

		/// <summary>
		/// Returns the Dispatcher instance of the current thread. When no instance has been created an exception will be thrown.
		/// </summary>
        /// 
        public static Dispatcher Current
        {
            get
			{
				if (currentDispatcher == null)
					throw new InvalidOperationException("No Dispatcher found for the current thread, please create a new Dispatcher instance before calling this property.");
				return currentDispatcher; 
			}
            set
            {
                if (currentDispatcher != null)
                    currentDispatcher.Dispose();
                currentDispatcher = value;
            }
        }

        /// <summary>
        /// Returns the Dispatcher instance of the current thread.
        /// </summary>
        /// 
        public static Dispatcher CurrentNoThrow
        {
            get
            {
                return currentDispatcher;
            }
        }

		/// <summary>
		/// Returns the first created Dispatcher instance, in most cases this will be the Dispatcher for the main thread. When no instance has been created an exception will be thrown.
		/// </summary>
        public static Dispatcher Main
        {
            get
            {
				if (mainDispatcher == null)
					throw new InvalidOperationException("No Dispatcher found for the main thread, please create a new Dispatcher instance before calling this property.");

                return mainDispatcher;
            }
        }

        /// <summary>
        /// Returns the first created Dispatcher instance.
        /// </summary>
        public static Dispatcher MainNoThrow
        {
            get
            {
                return mainDispatcher;
            }
        }

		/// <summary>
		/// Creates a new function based upon an other function which will handle exceptions. Use this to wrap safe functions for tasks.
		/// </summary>
		/// <typeparam name="T">The return type of the function.</typeparam>
		/// <param name="function">The orignal function.</param>
		/// <returns>The safe function.</returns>
		public static Func<T> CreateSafeFunction<T>(Func<T> function)
		{
			return () =>
				{
					try
					{
						return function();
					}
					catch
					{
						CurrentTask.Abort();
						return default(T);
					}
				};
		}

		/// <summary>
        /// Creates a new action based upon an other action which will handle exceptions. Use this to wrap safe action for tasks.
		/// </summary>
		/// <param name="function">The orignal action.</param>
		/// <returns>The safe action.</returns>
		public static Action CreateSafeAction<T>(Action action)
		{
			return () =>
			{
				try
				{
					action();
				}
				catch
				{
					CurrentTask.Abort();
				}
			};
		}

		/// <summary>
		/// Creates a Dispatcher, if a Dispatcher has been created in the current thread an exception will be thrown.
		/// </summary>
		public Dispatcher()
			: this(true)
		{
		}

        /// <summary>
        /// Creates a Dispatcher, if a Dispatcher has been created when setThreadDefaults is set to true in the current thread an exception will be thrown.
        /// </summary>
        /// <param name="setThreadDefaults">If set to true the new dispatcher will be set as threads default dispatcher.</param>
		public Dispatcher(bool setThreadDefaults)
        {
			if (!setThreadDefaults)
				return;

            if (currentDispatcher != null)
				throw new InvalidOperationException("Only one Dispatcher instance allowed per thread.");

			currentDispatcher = this;

            if (mainDispatcher == null)
                mainDispatcher = this;
        }

		/// <summary>
		/// Processes all remaining tasks. Call this periodically to allow the Dispatcher to handle dispatched tasks.
        /// Only call this inside the thread you want the tasks to process to be processed.
		/// </summary>
        public void ProcessTasks()
        {
			if (dataEvent.InterWaitOne(0))
				ProcessTasksInternal();
        }

		/// <summary>
		/// Processes all remaining tasks and returns true when something has been processed and false otherwise.
		/// This method will block until th exitHandle has been set or tasks should be processed.
        /// Only call this inside the thread you want the tasks to process to be processed.
		/// </summary>
		/// <param name="exitHandle">The handle to indicate an early abort of the wait process.</param>
		/// <returns>False when the exitHandle has been set, true otherwise.</returns>
        public bool ProcessTasks(WaitHandle exitHandle)
        {
            var result = WaitHandle.WaitAny(new WaitHandle[] { exitHandle, dataEvent });
			if (result == 0)
                return false;
            ProcessTasksInternal();
            return true;
        }

		/// <summary>
		/// Processed the next available task.
        /// Only call this inside the thread you want the tasks to process to be processed.
		/// </summary>
		/// <returns>True when a task to process has been processed, false otherwise.</returns>
        public bool ProcessNextTask()
        {
			Task task;
			lock (taskListSyncRoot)
			{
				if (taskList.Count == 0)
					return false;
				task = taskList.Dequeue();
			}

			ProcessSingleTask(task);

            if (TaskCount == 0)
                dataEvent.Reset();

            return true;
        }

		/// <summary>
		/// Processes the next available tasks and returns true when it has been processed and false otherwise.
		/// This method will block until th exitHandle has been set or a task should be processed.
        /// Only call this inside the thread you want the tasks to process to be processed.
		/// </summary>
		/// <param name="exitHandle">The handle to indicate an early abort of the wait process.</param>
		/// <returns>False when the exitHandle has been set, true otherwise.</returns>
        public bool ProcessNextTask(WaitHandle exitHandle)
        {
            var result = WaitHandle.WaitAny(new WaitHandle[] { exitHandle, dataEvent });
			if (result == 0)
                return false;

			Task task;
			lock (taskListSyncRoot)
			{
				if (taskList.Count == 0)
					return false;
				task = taskList.Dequeue();
			}

			ProcessSingleTask(task);
			if (TaskCount == 0)
				dataEvent.Reset();

            return true;
        }

        private void ProcessTasksInternal()
        {
			List<Task> tmpCopy;
            lock (taskListSyncRoot)
            {
				tmpCopy = new List<Task>(taskList);
				taskList.Clear();
			}

			while (tmpCopy.Count != 0)
			{
				var task = tmpCopy[0];
				tmpCopy.RemoveAt(0);
				ProcessSingleTask(task);
			}

            if (TaskCount == 0)
                dataEvent.Reset();
		}

        private void ProcessSingleTask(Task task)
        {
            RunTask(task);

            if (TaskSortingSystem == TaskSortingSystem.ReorderWhenExecuted)
				lock (taskListSyncRoot)
					 ReorderTasks();
        }

		internal void RunTask(Task task)
		{
			var oldTask = currentTask;
			currentTask = task;
			currentTask.DoInternal();
			currentTask = oldTask;
		}

		protected override void CheckAccessLimitation()
		{
			if (AllowAccessLimitationChecks && currentDispatcher == this)
				throw new InvalidOperationException("Dispatching a Task with the Dispatcher associated to the current thread is prohibited. You can run these Tasks without the need of a Dispatcher.");
		}

        #region IDisposable Members

		/// <summary>
		/// Disposes all dispatcher resources and remaining tasks.
		/// </summary>
        public override void Dispose()
        {
			while (true)
			{
				lock (taskListSyncRoot)
				{
                    if (taskList.Count != 0)
						currentTask = taskList.Dequeue();
                    else
                        break;
				}
				currentTask.Dispose();
			}

			dataEvent.Close();
			dataEvent = null;

			if (currentDispatcher == this)
				currentDispatcher = null;
			if (mainDispatcher == this)
				mainDispatcher = null;
        }

        #endregion
    }
}

--- EnumerableExtension.cs ---
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityThreading
{
    public static class EnumerableExtension
    {
        /// <summary>
        /// Performs the given Action parallel for each element in the enumerable.
        /// </summary>
        /// <param name="action">The action to perform for each element.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task> ParallelForEach<T>(this IEnumerable<T> that, Action<T> action)
        {
            return that.ParallelForEach(action, null);
        }

        /// <summary>
        /// Performs the given Action parallel for each element in the enumerable.
        /// </summary>
        /// <param name="action">The action to perform for each element.</param>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task> ParallelForEach<T>(this IEnumerable<T> that, Action<T> action, TaskDistributor target)
        {
            return (IEnumerable<Task>)that.ParallelForEach(element => { action(element); return default(UnityThreading.Task.Unit); }, target);
        }

        /// <summary>
        /// Performs the given Func parallel for each element in the enumerable.
        /// </summary>
        /// <param name="action">The Func to perform for each element.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task<TResult>> ParallelForEach<TResult, T>(this IEnumerable<T> that, Func<T, TResult> action)
        {
            return that.ParallelForEach(action);
        }

        /// <summary>
        /// Performs the given Func parallel for each element in the enumerable.
        /// </summary>
        /// <param name="action">The Func to perform for each element.</param>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task<TResult>> ParallelForEach<TResult, T>(this IEnumerable<T> that, Func<T, TResult> action, TaskDistributor target)
        {
            var result = new List<Task<TResult>>();
            foreach (var element in that)
            {
                var tmp = element;
                var task = Task.Create(() => action(tmp)).Run(target);
                result.Add(task);
            }
            return result;
        }

        /// <summary>
        /// Performs the given Action sequential for each element in the enumerable.
        /// </summary>
        /// <param name="action">The Action to perform for each element.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task> SequentialForEach<T>(this IEnumerable<T> that, Action<T> action)
        {
            return that.SequentialForEach(action, null);
        }

        /// <summary>
        /// Performs the given Action sequential for each element in the enumerable.
        /// </summary>
        /// <param name="action">The Action to perform for each element.</param>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task> SequentialForEach<T>(this IEnumerable<T> that, Action<T> action, TaskDistributor target)
        {
            return (IEnumerable<Task>)that.SequentialForEach(element => { action(element); return default(UnityThreading.Task.Unit); }, target);
        }

        /// <summary>
        /// Performs the given Func sequential for each element in the enumerable.
        /// </summary>
        /// <param name="action">The Func to perform for each element.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task<TResult>> SequentialForEach<TResult, T>(this IEnumerable<T> that, Func<T, TResult> action)
        {
            return that.SequentialForEach(action);
        }

        /// <summary>
        /// Performs the given Func sequential for each element in the enumerable.
        /// </summary>
        /// <param name="action">The Func to perform for each element.</param>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task<TResult>> SequentialForEach<TResult, T>(this IEnumerable<T> that, Func<T, TResult> action, TaskDistributor target)
        {
            var result = new List<Task<TResult>>();
            Task lastTask = null;
            foreach (var element in that)
            {
                var tmp = element;
                var task = Task.Create(() => action(tmp));
                if (lastTask == null)
                    task.Run(target);
                else
                    lastTask.WhenEnded(() => task.Run(target));
                lastTask = task;
                result.Add(task);
            }
            return result;
        }
    }
}

--- EnumeratorExtension.cs ---
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace UnityThreading
{
	public static class EnumeratorExtension
	{
        /// <summary>
        /// Starts the Enumerator as async Task on the given TaskDistributor.
        /// </summary>
        /// <returns>The task.</returns>
		public static Task RunAsync(this IEnumerator that)
		{
			return that.RunAsync(UnityThreadHelper.TaskDistributor);
		}

        /// <summary>
        /// Starts the Enumerator as async Task on the given TaskDistributor.
        /// </summary>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <returns>The task.</returns>
		public static Task RunAsync(this IEnumerator that, TaskDistributor target)
		{
			return target.Dispatch(Task.Create(that));
		}
	}
}

--- ObjectExtension.cs ---
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityThreading
{
	public static class ObjectExtension
	{
        /// <summary>
        /// Starts the given Method as async Task on the given TaskDistributor.
        /// </summary>
        /// <param name="args">Optional arguments passed to the method.</param>
        /// <returns>The task.</returns>
		public static Task RunAsync(this object that, string methodName, params object[] args)
		{
			return that.RunAsync<object>(methodName, null, args);
		}

        /// <summary>
        /// Starts the given Method as async Task on the given TaskDistributor.
        /// </summary>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <param name="args">Optional arguments passed to the method.</param>
        /// <returns>The task.</returns>
        public static Task RunAsync(this object that, string methodName, TaskDistributor target, params object[] args)
		{
			return that.RunAsync<object>(methodName, target, args);
		}

        /// <summary>
        /// Starts the given Method as async Task on the given TaskDistributor.
        /// </summary>
        /// <param name="args">Optional arguments passed to the method.</param>
        /// <returns>The task.</returns>
		public static Task<T> RunAsync<T>(this object that, string methodName, params object[] args)
		{
			return that.RunAsync<T>(methodName, null, args);
		}

        /// <summary>
        /// Starts the given Method as async Task on the given TaskDistributor.
        /// </summary>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <param name="args">Optional arguments passed to the method.</param>
        /// <returns>The task.</returns>
        public static Task<T> RunAsync<T>(this object that, string methodName, TaskDistributor target, params object[] args)
		{
			return Task.Create<T>(that, methodName, args).Run(target);
		}
	}
}

--- SwitchTo.cs ---
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityThreading
{
	public class SwitchTo
	{
		public enum TargetType
		{ 
			Main,
			Thread
		}

		public TargetType Target { get; private set; }

		private SwitchTo(TargetType target)
		{
			Target = target;
		}

        /// <summary>
        /// Changes the context of the following commands to the MainThread when yielded.
        /// </summary>
		public static readonly SwitchTo MainThread = new SwitchTo(TargetType.Main);

        /// <summary>
        /// Changes the context of the following commands to the WorkerThread when yielded.
        /// </summary>
		public static readonly SwitchTo Thread = new SwitchTo(TargetType.Thread);
	}
}

--- Task.cs ---
using System.Collections.Generic;
using System;
using System.Threading;
using System.Collections;

namespace UnityThreading
{
    public enum TaskSortingSystem
    {
        NeverReorder,
        ReorderWhenAdded,
        ReorderWhenExecuted
    }

	public delegate void TaskEndedEventHandler(Task sender);

    public abstract class Task
    {
        /// <summary>
        /// Empty Struct which works as the Void type.
        /// </summary>
		public struct Unit { }

        public Task()
        {
        }

        ~Task()
        {
            this.Dispose();
        }

		private object syncRoot = new object();
		private event TaskEndedEventHandler taskEnded;
		private bool hasEnded = false;

        public string Name;

        /// <summary>
        /// Change this when you work with a prioritzable Dispatcher or TaskDistributor to change the execution order
        /// lower values will be executed first.
        /// </summary>
        public volatile int Priority;

        /// <summary>
        /// Will be called when the task has been finished (success or failure or aborted).
        /// This event will be fired at the thread the task was running at.
        /// </summary>
		public event TaskEndedEventHandler TaskEnded
		{
			add
			{
				lock (syncRoot)
				{
					if (endingEvent.InterWaitOne(0))
					{
						value(this);
						return;
					}
					taskEnded += value;
				}
			}
			remove
			{
				lock (syncRoot)
					taskEnded -= value;
			}
		}

		private void End()
		{
			lock (syncRoot)
			{
				endingEvent.Set();

				if (taskEnded != null)
					taskEnded(this);

				endedEvent.Set();
				if (current == this)
					current = null;
				hasEnded = true;
			}
		}

        private ManualResetEvent abortEvent = new ManualResetEvent(false);
        private ManualResetEvent endedEvent = new ManualResetEvent(false);
        private ManualResetEvent endingEvent = new ManualResetEvent(false);
		private bool hasStarted = false;

		protected abstract IEnumerator Do();

		[ThreadStatic]
		private static Task current;

		/// <summary>
		/// Returns the currently ThreadBase instance which is running in this thread.
		/// </summary>
		public static Task Current { get { return current; } }

		/// <summary>
		/// Returns true if the task should abort. If a Task should abort and has not yet been started
		/// it will never start but indicate an end and failed state.
		/// </summary>
        public bool ShouldAbort
        {
            get
			{
				return abortEvent.InterWaitOne(0); 
			}
        }

		/// <summary>
		/// Returns true when processing of this task has been ended or has been skipped due early abortion.
		/// </summary>
        public bool HasEnded
        {
            get 
			{
				return hasEnded || endedEvent.InterWaitOne(0); 
			}
        }

        /// <summary>
		/// Returns true when processing of this task is ending.
		/// </summary>
        public bool IsEnding
        {
            get 
			{
				return endingEvent.InterWaitOne(0); 
			}
        }
        

		/// <summary>
		/// Returns true when the task has successfully been processed. Tasks which throw exceptions will
		/// not be set to a failed state, also any exceptions will not be catched, the user needs to add
		/// checks for these kind of situation.
		/// </summary>
        public bool IsSucceeded
        {
            get
            {
				return endingEvent.InterWaitOne(0) && !abortEvent.InterWaitOne(0);
            }
        }

		/// <summary>
		/// Returns true if the task should abort and has been ended. This value will not been set to true
		/// in case of an exception while processing this task. The user needs to add checks for these kind of situation.
		/// </summary>
        public bool IsFailed
        {
            get
            {
				return endingEvent.InterWaitOne(0) && abortEvent.InterWaitOne(0);
            }
        }

		/// <summary>
		/// Notifies the task to abort and sets the task state to failed. The task needs to check ShouldAbort if the task should abort.
		/// </summary>
        public void Abort()
        {
			abortEvent.Set();
			if (!hasStarted)
			{
				End();
			}
        }

		/// <summary>
		/// Notifies the task to abort and sets the task state to failed. The task needs to check ShouldAbort if the task should abort.
		/// This method will wait until the task has been aborted/ended.
		/// </summary>
        public void AbortWait()
		{
			Abort();
			if (!hasStarted)
				return;
			Wait();
        }

		/// <summary>
		/// Notifies the task to abort and sets the task state to failed. The task needs to check ShouldAbort if the task should abort.
		/// This method will wait until the task has been aborted/ended or the given timeout has been reached.
		/// </summary>
		/// <param name="seconds">Time in seconds this method will max wait.</param>
        public void AbortWaitForSeconds(float seconds)
        {
			Abort();
			if (!hasStarted)
				return;
			WaitForSeconds(seconds);
        }

		/// <summary>
		/// Blocks the calling thread until the task has been ended.
		/// </summary>
        public void Wait()
        {
			if (hasEnded)
				return;
			Priority--;
			endedEvent.WaitOne();
        }

		/// <summary>
		/// Blocks the calling thread until the task has been ended or the given timeout value has been reached.
		/// </summary>
		/// <param name="seconds">Time in seconds this method will max wait.</param>
        public void WaitForSeconds(float seconds)
        {
			if (hasEnded)
				return;
			Priority--;
			endedEvent.InterWaitOne(TimeSpan.FromSeconds(seconds));
        }

		/// <summary>
		/// Blocks the calling thread until the task has been ended and returns the return value of the task as the given type.
		/// Use this method only for Tasks with return values (functions)!
		/// </summary>
		/// <returns>The return value of the task as the given type.</returns>
		public abstract TResult Wait<TResult>();

		/// <summary>
		/// Blocks the calling thread until the task has been ended and returns the return value of the task as the given type.
		/// Use this method only for Tasks with return values (functions)!
		/// </summary>
		/// <param name="seconds">Time in seconds this method will max wait.</param>
		/// <returns>The return value of the task as the given type.</returns>
		public abstract TResult WaitForSeconds<TResult>(float seconds);

		public abstract object RawResult { get; }

		/// <summary>
		/// Blocks the calling thread until the task has been ended and returns the return value of the task as the given type.
		/// Use this method only for Tasks with return values (functions)!
		/// </summary>
		/// <param name="seconds">Time in seconds this method will max wait.</param>
		/// <param name="defaultReturnValue">The default return value which will be returned when the task has failed.</param>
		/// <returns>The return value of the task as the given type.</returns>
		public abstract TResult WaitForSeconds<TResult>(float seconds, TResult defaultReturnValue);

        internal void DoInternal()
        {
			current = this;
			hasStarted = true;
            if (!ShouldAbort)
            {
                try
                {
                    var enumerator = Do();
                    if (enumerator == null)
                    {
                        End();
                        return;
                    }

                    RunEnumerator(enumerator);
                }
                catch (Exception exception)
                {
                    this.Abort();
#if !NO_UNITY
                    if (string.IsNullOrEmpty(Name))
                        UnityEngine.Debug.LogError("Error while processing task:\n" + exception.ToString());
                    else
                        UnityEngine.Debug.LogError("Error while processing task '" + Name +"':\n" + exception.ToString());
#endif
                }
            }

			End();
        }

		private void RunEnumerator(IEnumerator enumerator)
		{
			var currentThread = UnityThreading.ThreadBase.CurrentThread;
			do
			{
				if (enumerator.Current is Task)
				{
					var task = (Task)enumerator.Current;
					currentThread.DispatchAndWait(task);
				}
				else if (enumerator.Current is SwitchTo)
				{
					var switchTo = (SwitchTo)enumerator.Current;
					if (switchTo.Target == SwitchTo.TargetType.Main && currentThread != null)
					{
						var task = Task.Create(() =>
						{
							if (enumerator.MoveNext() && !ShouldAbort)
								RunEnumerator(enumerator);
						});
						currentThread.DispatchAndWait(task);
					}
					else if (switchTo.Target == SwitchTo.TargetType.Thread && currentThread == null)
					{
						return;
					}
				}
			}
			while (enumerator.MoveNext() && !ShouldAbort);
		}

        private bool disposed = false;

		/// <summary>
		/// Disposes this task and waits for completion if its still running.
		/// </summary>
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            if (hasStarted)
                Wait();
            endingEvent.Close();
            endedEvent.Close();
			abortEvent.Close();
        }

        /// <summary>
        /// Starts the task on the given DispatcherBase (Dispatcher or TaskDistributor).
        /// </summary>
        /// <param name="target">The DispatcherBase to work on.</param>
        /// <returns>This task</returns>
		public Task Run(DispatcherBase target)
		{
			if (target == null)
				return Run();
			target.Dispatch(this);
			return this;
		}

        /// <summary>
        /// Starts the task.
        /// </summary>
        /// <returns>This task</returns>
		public Task Run()
		{
#if NO_UNITY
			Run(UnityThreadHelper.TaskDistributor);
#else
			Run(UnityThreadHelper.TaskDistributor);
#endif
			return this;
		}

		public static Task Create(Action<Task> action)
		{
			return new Task<Unit>(action);
		}

		public static Task Create(Action action)
		{
			return new Task<Unit>(action);
		}

		public static Task<T> Create<T>(Func<Task, T> func)
		{
			return new Task<T>(func);
		}

		public static Task<T> Create<T>(Func<T> func)
		{
			return new Task<T>(func);
		}

		public static Task Create(IEnumerator enumerator)
		{
			return new Task<IEnumerator>(enumerator);
		}

		public static Task<T> Create<T>(Type type, string methodName, params object[] args)
		{
			return new Task<T>(type, methodName, args);
		}

		public static Task<T> Create<T>(object that, string methodName, params object[] args)
		{
			return new Task<T>(that, methodName, args);
		}

	}

    public class Task<T> : Task
    {
		private Func<Task, T> function;
        private T result;

		public Task(Func<Task, T> function)
		{
			this.function = function;
		}

		public Task(Func<T> function)
		{
			this.function = t => function();
		}

		public Task(Action<Task> action)
		{
			this.function = t => { action(t); return default(T); };
		}

		public Task(Action action)
		{
			this.function = t => { action(); return default(T); };
		}

		public Task(IEnumerator enumerator)
		{
			this.function = t => { return (T)enumerator; };
		}

		public Task(Type type, string methodName, params object[] args)
		{
			var methodInfo = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Static);
			if (methodInfo == null)
				throw new ArgumentException("methodName", "Fitting method with the given name was not found.");

			this.function = t => { return (T)methodInfo.Invoke(null, args); };
		}

		public Task(object that, string methodName, params object[] args)
		{
			var methodInfo = that.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.InvokeMethod);
			if (methodInfo == null)
				throw new ArgumentException("methodName", "Fitting method with the given name was not found.");

			this.function = t => { return (T)methodInfo.Invoke(that, args); };
		}

		protected override IEnumerator Do()
        {
             result = function(this);
                if (result is IEnumerator)
                    return (IEnumerator)result;
                return null;
        }

		public override TResult Wait<TResult>()
		{
			Priority--;
			return (TResult)(object)Result;
		}

		public override TResult WaitForSeconds<TResult>(float seconds)
		{
			Priority--;
			return WaitForSeconds(seconds, default(TResult));
		}

		public override TResult WaitForSeconds<TResult>(float seconds, TResult defaultReturnValue)
		{
			if (!HasEnded)
				WaitForSeconds(seconds);
			if (IsSucceeded)
				return (TResult)(object)result;
			return defaultReturnValue;
		}
        
        public override object RawResult
        {
            get
            {
                if (!IsEnding)
                    Wait();
                return result;
            }
        }

		public T Result
		{
			get
			{
				if (!IsEnding)
					Wait();
				return result;
			}
		}

		public new Task<T> Run(DispatcherBase target)
		{
			((Task)this).Run(target);
			return this;
		}

		public new Task<T> Run()
		{
			((Task)this).Run();
			return this;
		}
    }
}


--- TaskDistributer.cs ---
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using System.Collections;

namespace UnityThreading
{
    public class TaskDistributor : DispatcherBase
	{
        private TaskWorker[] workerThreads;

        internal WaitHandle NewDataWaitHandle { get { return dataEvent; } }

		private static TaskDistributor mainTaskDistributor;

		/// <summary>
		/// Returns the first created TaskDistributor instance. When no instance has been created an exception will be thrown.
		/// </summary>
		public static TaskDistributor Main
		{
			get
			{
				if (mainTaskDistributor == null)
					throw new InvalidOperationException("No default TaskDistributor found, please create a new TaskDistributor instance before calling this property.");

				return mainTaskDistributor;
			}
		}

		/// <summary>
		/// Returns the first created TaskDistributor instance.
		/// </summary>
		public static TaskDistributor MainNoThrow
		{
			get
			{
				return mainTaskDistributor;
			}
		}

		/// <summary>
		/// Creates a new instance of the TaskDistributor with ProcessorCount x2 worker threads.
		/// The task distributor will auto start his worker threads.
		/// </summary>
        public TaskDistributor(string name)
			: this(name, 0)
        {
        }

        public override int TaskCount
        {
            get
            {
                var count = base.TaskCount;
                lock (workerThreads)
                {
                    for (var i = 0; i < workerThreads.Length; ++i)
                    {
                        count += workerThreads[i].Dispatcher.TaskCount;
                    }
                }
                return count;
            }
        }

		/// <summary>
		/// Creates a new instance of the TaskDistributor.
		/// The task distributor will auto start his worker threads.
		/// </summary>
		/// <param name="workerThreadCount">The number of worker threads, a value below one will create ProcessorCount x2 worker threads.</param>
		public TaskDistributor(string name, int workerThreadCount)
			: this(name, workerThreadCount, true)
		{
		}

		/// <summary>
		/// Creates a new instance of the TaskDistributor.
		/// </summary>
        /// <param name="workerThreadCount">The number of worker threads, a value below one will create ProcessorCount x2 worker threads.</param>
		/// <param name="autoStart">Should the instance auto start the worker threads.</param>
		public TaskDistributor(string name, int workerThreadCount, bool autoStart)
			: base()
		{
			this.name = name;
            if (workerThreadCount <= 0)
				workerThreadCount = ThreadBase.AvailableProcessors * 2;

			workerThreads = new TaskWorker[workerThreadCount];
			lock (workerThreads)
			{
				for (var i = 0; i < workerThreadCount; ++i)
					workerThreads[i] = new TaskWorker(name, this);
			}

			if (mainTaskDistributor == null)
				mainTaskDistributor = this;

			if (autoStart)
				Start();
		}

		/// <summary>
		/// Starts the TaskDistributor if its not currently running.
		/// </summary>
		public void Start()
		{
			lock (workerThreads)
			{
				for (var i = 0; i < workerThreads.Length; ++i)
				{
					if (!workerThreads[i].IsAlive)
					{
						workerThreads[i].Start();
					}
				}
			}
		}

        public void SpawnAdditionalWorkerThread()
        {
            lock (workerThreads)
            {
                Array.Resize(ref workerThreads, workerThreads.Length + 1);
                workerThreads[workerThreads.Length - 1] = new TaskWorker(name, this);
				workerThreads[workerThreads.Length - 1].Priority = priority;
                workerThreads[workerThreads.Length - 1].Start();
            }
        }

        /// <summary>
        /// Amount of additional spawnable worker threads.
        /// </summary>
		public int MaxAdditionalWorkerThreads = 0;

		private string name;

        internal void FillTasks(Dispatcher target)
        {
			target.AddTasks(this.IsolateTasks(1));
        }

		protected override void CheckAccessLimitation()
		{
			if (MaxAdditionalWorkerThreads > 0 || !AllowAccessLimitationChecks)
                return;

			if (ThreadBase.CurrentThread != null &&
				ThreadBase.CurrentThread is TaskWorker &&
				((TaskWorker)ThreadBase.CurrentThread).TaskDistributor == this)
			{
				throw new InvalidOperationException("Access to TaskDistributor prohibited when called from inside a TaskDistributor thread. Dont dispatch new Tasks through the same TaskDistributor. If you want to distribute new tasks create a new TaskDistributor and use the new created instance. Remember to dispose the new instance to prevent thread spamming.");
			}
		}

        internal override void TasksAdded()
        {
			if (MaxAdditionalWorkerThreads > 0 &&
				(workerThreads.All(worker => worker.Dispatcher.TaskCount > 0 || worker.IsWorking) || this.taskList.Count > workerThreads.Length))
            {
				Interlocked.Decrement(ref MaxAdditionalWorkerThreads);
                SpawnAdditionalWorkerThread();
            }

			base.TasksAdded();
        }

        #region IDisposable Members

		private bool isDisposed = false;
		/// <summary>
		/// Disposes all TaskDistributor, worker threads, resources and remaining tasks.
		/// </summary>
        public override void Dispose()
        {
			if (isDisposed)
				return;

			while (true)
			{
				Task currentTask;
                lock (taskListSyncRoot)
                {
                    if (taskList.Count != 0)
						currentTask = taskList.Dequeue();
                    else
                        break;
                }
				currentTask.Dispose();
			}

			lock (workerThreads)
			{
				for (var i = 0; i < workerThreads.Length; ++i)
					workerThreads[i].Dispose();
				workerThreads = new TaskWorker[0];
			}

			dataEvent.Close();
			dataEvent = null;

			if (mainTaskDistributor == this)
				mainTaskDistributor = null;

			isDisposed = true;
        }

        #endregion

		private ThreadPriority priority = ThreadPriority.BelowNormal;
		public ThreadPriority Priority
		{
			get { return priority; }
			set
			{
				priority = value;
				foreach (var worker in workerThreads)
					worker.Priority = value;
			}
		}
	}

    internal sealed class TaskWorker : ThreadBase
    {
		public Dispatcher Dispatcher;
		public TaskDistributor TaskDistributor { get; private set; }

		public bool IsWorking
		{
			get
			{
				return Dispatcher.IsWorking;
			}
		}

		public TaskWorker(string name, TaskDistributor taskDistributor)
            : base(name, false)
        {
			this.TaskDistributor = taskDistributor;
			this.Dispatcher = new Dispatcher(false);
		}

        protected override IEnumerator Do()
        {
            while (!exitEvent.InterWaitOne(0))
            {
                if (!Dispatcher.ProcessNextTask())
                {
					TaskDistributor.FillTasks(Dispatcher);
                    if (Dispatcher.TaskCount == 0)
                    {
						var result = WaitHandle.WaitAny(new WaitHandle[] { exitEvent, TaskDistributor.NewDataWaitHandle });
						if (result == 0)
                            return null;
						TaskDistributor.FillTasks(Dispatcher);
                    }
                }
            }
            return null;
        }

        public override void Dispose()
        {
            base.Dispose();
			if (Dispatcher != null)
				Dispatcher.Dispose();
			Dispatcher = null;
        }
	}
}


--- TaskExtension.cs ---
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityThreading
{
	public static class TaskExtension
	{
		/// <summary>
		/// Sets the name of the task
		/// </summary>
		public static Task WithName(this Task task, string name)
		{
			task.Name = name;
			return task;
		}

		/// <summary>
		/// Sets the name of the task
		/// </summary>
		public static Task<T> WithName<T>(this Task<T> task, string name)
		{
			task.Name = name;
			return task;
		}

        /// <summary>
        /// Waits for the completion of all tasks in the Enumerable.
        /// </summary>
		public static void WaitAll(this IEnumerable<Task> tasks)
		{
			foreach (var task in tasks)
				task.Wait();
		}

		/// <summary>
		/// Starts the given Task when the tasks ended successfully.
		/// </summary>
		/// <param name="followingTask">The task to start.</param>
		/// <param name="target">The DispatcherBase to start the following task on.</param>
		/// <returns>The tasks.</returns>
		public static IEnumerable<Task> Then(this IEnumerable<Task> that, Task followingTask, DispatcherBase target)
		{
			var remaining = that.Count();
			var syncRoot = new object();

			foreach (var task in that)
			{
				task.WhenFailed(() =>
				{
					if (followingTask.ShouldAbort)
						return;
					followingTask.Abort();
				});
				task.WhenSucceeded(() =>
				{
					if (followingTask.ShouldAbort)
						return;

					lock (syncRoot)
					{
						remaining--;
						if (remaining == 0)
						{
							if (target != null)
								followingTask.Run(target);
							else if (ThreadBase.CurrentThread is TaskWorker)
								followingTask.Run(((TaskWorker)ThreadBase.CurrentThread).TaskDistributor);
							else
								followingTask.Run();
						}
					}
				});
			}
			return that;
		}

		/// <summary>
		/// Starts the given Action when all Tasks ended successfully.
		/// </summary>
		/// <param name="action">The action to start.</param>
		/// <param name="target">The DispatcherBase to start the following action on.</param>
		/// <returns>The tasks.</returns>
		public static IEnumerable<Task> WhenSucceeded(this IEnumerable<Task> that, Action action, DispatcherBase target)
		{
			var remaining = that.Count();
			var syncRoot = new object();

			foreach (var task in that)
			{
				task.WhenSucceeded(() =>
				{
					lock (syncRoot)
					{
						remaining--;
						if (remaining == 0)
						{
							if (target == null)
								action();
							else
								target.Dispatch(() => { action(); });
						}
					}
				});
			}
			return that;
		}

		/// <summary>
		/// Starts the given Action when one task has not successfully ended
		/// </summary>
		/// <param name="action">The action to start.</param>
		/// <param name="target">The DispatcherBase to start the following action on.</param>
		/// <returns>The tasks.</returns>
		public static IEnumerable<Task> WhenFailed(this IEnumerable<Task> that, Action action, DispatcherBase target)
		{
			var hasFailed = false;
			var syncRoot = new object();
			foreach (var task in that)
			{
				task.WhenFailed(() =>
				{
					lock (syncRoot)
					{
						if (hasFailed)
							return;
						hasFailed = true;

						if (target == null)
							action();
						else
							target.Dispatch(() => { action(); });
					}
				});
			}
			return that;
		}

        /// <summary>
        /// Invokes the given action with the set result of the task when the task succeeded.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task OnResult(this Task task, Action<object> action)
		{
			return task.OnResult(action, null);
		}

        /// <summary>
        /// Invokes the given action with the set result of the task when the task succeeded.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="target">The DispatcherBase to perform the action on.</param>
        /// <returns>This task.</returns>
		public static Task OnResult(this Task task, Action<object> action, DispatcherBase target)
		{
			return task.WhenSucceeded(t => action(t.RawResult), target);
		}

        /// <summary>
        /// Invokes the given action with the set result of the task when the task succeeded.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task OnResult<T>(this Task task, Action<T> action)
		{
			return task.OnResult<T>(action, null);
		}

        /// <summary>
        /// Invokes the given action with the set result of the task when the task succeeded.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="target">The DispatcherBase to perform the action on.</param>
        /// <returns>This task.</returns>
		public static Task OnResult<T>(this Task task, Action<T> action, DispatcherBase target)
		{
			return task.WhenSucceeded(t => action((T)t.RawResult), target);
		}

        /// <summary>
        /// Invokes the given action with the set result of the task when the task succeeded.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task<T> OnResult<T>(this Task<T> task, Action<T> action)
		{
			return task.OnResult<T>(action, null);
		}

        /// <summary>
        /// Invokes the given action with the set result of the task when the task succeeded.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="target">The DispatcherBase to perform the action on.</param>
        /// <returns>This task.</returns>
		public static Task<T> OnResult<T>(this Task<T> task, Action<T> action, DispatcherBase actionTarget)
		{
			return task.WhenSucceeded<T>(t => action(t.Result), actionTarget);
		}

		#region Succeeded

        /// <summary>
        /// The given Action will be performed when the task succeeds.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task<T> WhenSucceeded<T>(this Task<T> task, Action action)
		{
			return task.WhenSucceeded<T>(t => action(), null);
		}

        /// <summary>
        /// The given Action will be performed when the task succeeds.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task<T> WhenSucceeded<T>(this Task<T> task, Action<Task<T>> action)
		{
			return task.WhenSucceeded<T>(action, null);
		}

        /// <summary>
        /// The given Action will be performed when the task succeeds.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="target">The DispatcherBase to perform the action on.</param>
        /// <returns>This task.</returns>
		public static Task<T> WhenSucceeded<T>(this Task<T> task, Action<Task<T>> action, DispatcherBase target)
		{
			Action<Task<T>> perform = t =>
			{
				if (target == null)
					action(t);
				else
					target.Dispatch(() => { if (t.IsSucceeded) action(t); });
			};

			return task.WhenEnded<T>(t => { if (t.IsSucceeded) perform(t); }, null);
		}

        /// <summary>
        /// The given Action will be performed when the task succeeds.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task WhenSucceeded(this Task task, Action action)
		{
			return task.WhenEnded(t => {if (t.IsSucceeded) action(); } );
		}

        /// <summary>
        /// The given Action will be performed when the task succeeds.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task WhenSucceeded(this Task task, Action<Task> action)
		{
			return task.WhenEnded(t => { if (t.IsSucceeded) action(t); });
		}

        /// <summary>
        /// The given Action will be performed when the task succeeds.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="target">The DispatcherBase to perform the action on.</param>
        /// <returns>This task.</returns>
		public static Task WhenSucceeded(this Task task, Action<Task> action, DispatcherBase actiontargetTarget)
		{
			Action<Task> perform = t =>
			{
				if (actiontargetTarget == null)
					action(t);
				else
					actiontargetTarget.Dispatch(() => { if (t.IsSucceeded) action(t); });
			};

			return task.WhenEnded(t => { if (t.IsSucceeded) perform(t); }, null);
		}

		#endregion

		#region Failed

        /// <summary>
        /// The given Action will be performed when the task fails.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task<T> WhenFailed<T>(this Task<T> task, Action action)
		{
			return task.WhenFailed<T>(t => action(), null);
		}

        /// <summary>
        /// The given Action will be performed when the task fails.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task<T> WhenFailed<T>(this Task<T> task, Action<Task<T>> action)
		{
			return task.WhenFailed<T>(action, null);
		}

        /// <summary>
        /// The given Action will be performed when the task fails.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="target">The DispatcherBase to perform the action on.</param>
        /// <returns>This task.</returns>
		public static Task<T> WhenFailed<T>(this Task<T> task, Action<Task<T>> action, DispatcherBase target)
		{
			return task.WhenEnded<T>(t => { if (t.IsFailed) action(t); }, target);
		}

        /// <summary>
        /// The given Action will be performed when the task fails.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task WhenFailed(this Task task, Action action)
		{
			return task.WhenEnded(t => { if (t.IsFailed) action(); });
		}

        /// <summary>
        /// The given Action will be performed when the task fails.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task WhenFailed(this Task task, Action<Task> action)
		{
			return task.WhenEnded(t => { if (t.IsFailed) action(t); });
		}

        /// <summary>
        /// The given Action will be performed when the task fails.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="target">The DispatcherBase to perform the action on.</param>
        /// <returns>This task.</returns>
		public static Task WhenFailed(this Task task, Action<Task> action, DispatcherBase target)
		{
			return task.WhenEnded(t => { if (t.IsFailed) action(t); }, target);
		}

		#endregion

		#region Ended

        /// <summary>
        /// The given Action will be performed when the task ends.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task<T> WhenEnded<T>(this Task<T> task, Action action)
		{
			return task.WhenEnded<T>(t => action(), null);
		}

        /// <summary>
        /// The given Action will be performed when the task ends.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task<T> WhenEnded<T>(this Task<T> task, Action<Task<T>> action)
		{
			return task.WhenEnded<T>(action, null);
		}

        /// <summary>
        /// The given Action will be performed when the task ends.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="target">The DispatcherBase to perform the action on.</param>
        /// <returns>This task.</returns>
		public static Task<T> WhenEnded<T>(this Task<T> task, Action<Task<T>> action, DispatcherBase target)
		{
			task.TaskEnded += t =>
			{
				if (target == null)
					action(task);
				else
					target.Dispatch(() => action(task));
			};

			return task;
		}

        /// <summary>
        /// The given Action will be performed when the task ends.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task WhenEnded(this Task task, Action action)
		{
			return task.WhenEnded(t => action());
		}

        /// <summary>
        /// The given Action will be performed when the task ends.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>This task.</returns>
		public static Task WhenEnded(this Task task, Action<Task> action)
		{
			return task.WhenEnded(t => action(t), null);
		}

        /// <summary>
        /// The given Action will be performed when the task ends.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="target">The DispatcherBase to perform the action on.</param>
        /// <returns>This task.</returns>
		public static Task WhenEnded(this Task task, Action<Task> action, DispatcherBase target)
		{
			task.TaskEnded += (t) =>
			{
				if (target == null)
					action(task);
				else
					target.Dispatch(() => action(task));
			};

			return task;
		}

		#endregion

        /// <summary>
        /// Starts the given Task when this Task ended successfully.
        /// </summary>
        /// <param name="followingTask">The task to start.</param>
        /// <returns>This task.</returns>
		public static Task Then(this Task that, Task followingTask)
		{
			TaskDistributor target = null;
			if (ThreadBase.CurrentThread is TaskWorker)
				target = ((TaskWorker)ThreadBase.CurrentThread).TaskDistributor;

			return that.Then(followingTask, target);
		}

        /// <summary>
        /// Starts the given Task when this Task ended successfully.
        /// </summary>
        /// <param name="followingTask">The task to start.</param>
        /// <param name="target">The DispatcherBase to start the following task on.</param>
        /// <returns>This task.</returns>
		public static Task Then(this Task that, Task followingTask, DispatcherBase target)
		{
			that.WhenFailed(() =>
			{
				followingTask.Abort();
			});
			that.WhenSucceeded(() =>
			{
				if (target != null)
					followingTask.Run(target);
				else if (ThreadBase.CurrentThread is TaskWorker)
					followingTask.Run(((TaskWorker)ThreadBase.CurrentThread).TaskDistributor);
				else
					followingTask.Run();
			});
			return that;
		}

        /// <summary>
        /// Starts this Task when the other Task ended successfully.
        /// </summary>
        /// <param name="followingTask">The task to await.</param>
        /// <returns>This task.</returns>
		public static Task Await(this Task that, Task taskToWaitFor)
		{
			taskToWaitFor.Then(that);
			return that;
		}

        /// <summary>
        /// Starts this Task when the other Task ended successfully.
        /// </summary>
        /// <param name="followingTask">The task to await.</param>
        /// <param name="target">The DispatcherBase to start this task on.</param>
        /// <returns>This task.</returns>
		public static Task Await(this Task that, Task taskToWaitFor, DispatcherBase target)
		{
			taskToWaitFor.Then(that, target);
			return that;
		}

        /// <summary>
        /// Converts this Task.
        /// </summary>
        /// <param name="that"></param>
        /// <returns>The converted task.</returns>
		public static Task<T> As<T>(this Task that)
		{
			return (Task<T>)that;
		}

        /// <summary>
        /// Starts the given Action when any Task in the Enumerable has ended.
        /// </summary>
        /// <param name="action">The action to start.</param>
        /// <returns>This Enumerable of Tasks.</returns>
		public static IEnumerable<Task> ContinueWhenAnyEnded(this IEnumerable<Task> tasks, Action action)
		{
			return tasks.ContinueWhenAnyEnded(t => action());
		}

        /// <summary>
        /// Starts the given Action when any Task in the Enumerable has ended.
        /// </summary>
        /// <param name="action">The action to start.</param>
        /// <returns>This Enumerable of Tasks.</returns>
		public static IEnumerable<Task> ContinueWhenAnyEnded(this IEnumerable<Task> tasks, Action<Task> action)
		{
			var syncRoot = new object();
			var done = false;
			foreach (var task in tasks)
			{
				task.WhenEnded(t =>
				{
					lock (syncRoot)
					{
						if (done)
							return;

						done = true;
						action(t);
					}
				});
			}

			return tasks;
		}

        /// <summary>
        /// Starts the given Action when all Tasks in the Enumerable have ended.
        /// </summary>
        /// <param name="action">The action to start.</param>
        /// <returns>This Enumerable of Tasks.</returns>
		public static IEnumerable<Task> ContinueWhenAllEnded(this IEnumerable<Task> tasks, Action action)
		{
			return tasks.ContinueWhenAllEnded(t => action());
		}

        /// <summary>
        /// Starts the given Action when all Tasks in the Enumerable have ended.
        /// </summary>
        /// <param name="action">The action to start.</param>
        /// <returns>This Enumerable of Tasks.</returns>
		public static IEnumerable<Task> ContinueWhenAllEnded(this IEnumerable<Task> tasks, Action<IEnumerable<Task>> action)
		{
			var count = tasks.Count();

			if (count == 0)
				action(new Task[0]);

			var finishedTasks = new List<Task>();
			var syncRoot = new object();

			foreach (var task in tasks)
			{
				task.WhenEnded(t =>
				{
					lock (syncRoot)
					{
						finishedTasks.Add(task);
						if (finishedTasks.Count == count)
							action(finishedTasks);
					}
				});
			}

			return tasks;
		}
	}
}

--- Thread.cs ---
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Diagnostics;

namespace UnityThreading
{
	public abstract class ThreadBase : IDisposable
	{
		public static int AvailableProcessors
		{
			get
			{
#if !NO_UNITY
                return UnityEngine.SystemInfo.processorCount;
#else
				return Environment.ProcessorCount;
#endif
			}
		}

        protected Dispatcher targetDispatcher;
        protected Thread thread;
        protected ManualResetEvent exitEvent = new ManualResetEvent(false);

        [ThreadStatic]
        private static ThreadBase currentThread;
		private string threadName;

		/// <summary>
		/// Returns the currently ThreadBase instance which is running in this thread.
		/// </summary>
        public static ThreadBase CurrentThread { get { return currentThread; } }
        
        public ThreadBase(string threadName)
			: this(threadName, true)
        {
        }

        public ThreadBase(string threadName, bool autoStartThread)
			: this(threadName, Dispatcher.CurrentNoThrow, autoStartThread)
        {
        }

        public ThreadBase(string threadName, Dispatcher targetDispatcher)
			: this(threadName, targetDispatcher, true)
		{
		}

        public ThreadBase(string threadName, Dispatcher targetDispatcher, bool autoStartThread)
        {
			this.threadName = threadName;
            this.targetDispatcher = targetDispatcher;
            if (autoStartThread)
                Start();
        }

		/// <summary>
		/// Returns true if the thread is working.
		/// </summary>
        public bool IsAlive { get { return thread == null ? false : thread.IsAlive; } }

		/// <summary>
		/// Returns true if the thread should stop working.
		/// </summary>
        public bool ShouldStop { get { return exitEvent.InterWaitOne(0); } }

		/// <summary>
		/// Starts the thread.
		/// </summary>
        public void Start()
        {
            if (thread != null)
                Abort();

            exitEvent.Reset();
            thread = new Thread(DoInternal);
			thread.Name = threadName;
			thread.Priority = priority;

            thread.Start();
        }

		/// <summary>
		/// Notifies the thread to stop working.
		/// </summary>
        public void Exit()
        {
            if (thread != null)
                exitEvent.Set();
        }

		/// <summary>
		/// Notifies the thread to stop working.
		/// </summary>
        public void Abort()
        {
            Exit();
            if (thread != null)
				thread.Join();
        }

		/// <summary>
		/// Notifies the thread to stop working and waits for completion for the given ammount of time.
		/// When the thread soes not stop after the given timeout the thread will be terminated.
		/// </summary>
		/// <param name="seconds">The time this method will wait until the thread will be terminated.</param>
        public void AbortWaitForSeconds(float seconds)
        {
            Exit();
            if (thread != null)
            {
                thread.Join((int)(seconds * 1000));
                if (thread.IsAlive)
                    thread.Abort();
            }
        }

		/// <summary>
		/// Creates a new Task for the target Dispatcher (default: the main Dispatcher) based upon the given function.
		/// </summary>
		/// <typeparam name="T">The return value of the task.</typeparam>
		/// <param name="function">The function to process at the dispatchers thread.</param>
		/// <returns>The new task.</returns>
        public Task<T> Dispatch<T>(Func<T> function)
        {
            return targetDispatcher.Dispatch(function);
        }

		/// <summary>
		/// Creates a new Task for the target Dispatcher (default: the main Dispatcher) based upon the given function.
		/// This method will wait for the task completion and returns the return value.
		/// </summary>
		/// <typeparam name="T">The return value of the task.</typeparam>
		/// <param name="function">The function to process at the dispatchers thread.</param>
		/// <returns>The return value of the tasks function.</returns>
        public T DispatchAndWait<T>(Func<T> function)
        {
			var task = this.Dispatch(function);
            task.Wait();
            return task.Result;
        }

		/// <summary>
		/// Creates a new Task for the target Dispatcher (default: the main Dispatcher) based upon the given function.
		/// This method will wait for the task completion or the timeout and returns the return value.
		/// </summary>
		/// <typeparam name="T">The return value of the task.</typeparam>
		/// <param name="function">The function to process at the dispatchers thread.</param>
		/// <param name="timeOutSeconds">Time in seconds after the waiting process will stop.</param>
		/// <returns>The return value of the tasks function.</returns>
        public T DispatchAndWait<T>(Func<T> function, float timeOutSeconds)
        {
            var task = this.Dispatch(function);
            task.WaitForSeconds(timeOutSeconds);
            return task.Result;
        }

		/// <summary>
		/// Creates a new Task for the target Dispatcher (default: the main Dispatcher) based upon the given action.
		/// </summary>
		/// <param name="action">The action to process at the dispatchers thread.</param>
		/// <returns>The new task.</returns>
        public Task Dispatch(Action action)
        {
            return targetDispatcher.Dispatch(action);
        }

		/// <summary>
		/// Creates a new Task for the target Dispatcher (default: the main Dispatcher) based upon the given action.
		/// This method will wait for the task completion.
		/// </summary>
		/// <param name="action">The action to process at the dispatchers thread.</param>
        public void DispatchAndWait(Action action)
        {
            var task = this.Dispatch(action);
            task.Wait();
        }

		/// <summary>
		/// Creates a new Task for the target Dispatcher (default: the main Dispatcher) based upon the given action.
		/// This method will wait for the task completion or the timeout.
		/// </summary>
		/// <param name="action">The action to process at the dispatchers thread.</param>
		/// <param name="timeOutSeconds">Time in seconds after the waiting process will stop.</param>
		public void DispatchAndWait(Action action, float timeOutSeconds)
        {
			var task = this.Dispatch(action);
            task.WaitForSeconds(timeOutSeconds);
        }

        /// <summary>
        /// Dispatches the given task to the target Dispatcher (default: the main Dispatcher).
        /// </summary>
        /// <param name="taskBase">The task to process at the dispatchers thread.</param>
        /// <returns>The new task.</returns>
        public Task Dispatch(Task taskBase)
        {
            return targetDispatcher.Dispatch(taskBase);
        }

        /// <summary>
        /// Dispatches the given task to the target Dispatcher (default: the main Dispatcher).
        /// This method will wait for the task completion.
        /// </summary>
        /// <param name="taskBase">The task to process at the dispatchers thread.</param>
        public void DispatchAndWait(Task taskBase)
        {
            var task = this.Dispatch(taskBase);
            task.Wait();
        }

        /// <summary>
        /// Dispatches the given task to the target Dispatcher (default: the main Dispatcher).
        /// This method will wait for the task completion or the timeout.
        /// </summary>
        /// <param name="taskBase">The task to process at the dispatchers thread.</param>
        /// <param name="timeOutSeconds">Time in seconds after the waiting process will stop.</param>
        public void DispatchAndWait(Task taskBase, float timeOutSeconds)
        {
            var task = this.Dispatch(taskBase);
            task.WaitForSeconds(timeOutSeconds);
        }

        protected void DoInternal()
        {
            currentThread = this;

            var enumerator = Do();
            if (enumerator == null)
            {
                return;
            }

			RunEnumerator(enumerator);
        }

		private void RunEnumerator(IEnumerator enumerator)
		{
			do
			{
				if (enumerator.Current is Task)
				{
					var task = (Task)enumerator.Current;
					this.DispatchAndWait(task);
				}
				else if (enumerator.Current is SwitchTo)
				{
					var switchTo = (SwitchTo)enumerator.Current;
					if (switchTo.Target == SwitchTo.TargetType.Main && CurrentThread != null)
					{
						var task = Task.Create(() =>
							{
								if (enumerator.MoveNext() && !ShouldStop)
									RunEnumerator(enumerator);
							});
						this.DispatchAndWait(task);
					}
					else if (switchTo.Target == SwitchTo.TargetType.Thread && CurrentThread == null)
					{
						return;
					}
				}
			}
			while (enumerator.MoveNext() && !ShouldStop);
		}

        protected abstract IEnumerator Do();

        #region IDisposable Members

		/// <summary>
		/// Disposes the thread and all resources.
		/// </summary>
        public virtual void Dispose()
        {
            AbortWaitForSeconds(1.0f);
        }

        #endregion

		private ThreadPriority priority = ThreadPriority.BelowNormal;
		public ThreadPriority Priority
		{
			get { return priority; }
			set
			{
				priority = value;
				if (thread != null)
					thread.Priority = priority;
			}
		}
    }

    public sealed class ActionThread : ThreadBase
    {
        private Action<ActionThread> action;

        /// <summary>
        /// Creates a new Thread which runs the given action.
        /// The thread will start running after creation.
        /// </summary>
        /// <param name="action">The action to run.</param>
        public ActionThread(Action<ActionThread> action)
            : this(action, true)
        {
        }

        /// <summary>
        /// Creates a new Thread which runs the given action.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="autoStartThread">Should the thread start after creation.</param>
        public ActionThread(Action<ActionThread> action, bool autoStartThread)
            : base("ActionThread", Dispatcher.Current, false)
        {
            this.action = action;
            if (autoStartThread)
                Start();
        }

        protected override IEnumerator Do()
        {
            action(this);
            return null;
        }
    }

    public sealed class EnumeratableActionThread : ThreadBase
    {
        private Func<ThreadBase, IEnumerator> enumeratableAction;

        /// <summary>
        /// Creates a new Thread which runs the given enumeratable action.
        /// The thread will start running after creation.
        /// </summary>
        /// <param name="action">The enumeratable action to run.</param>
        public EnumeratableActionThread(Func<ThreadBase, IEnumerator> enumeratableAction)
            : this(enumeratableAction, true)
        {
        }

        /// <summary>
        /// Creates a new Thread which runs the given enumeratable action.
        /// </summary>
        /// <param name="action">The enumeratable action to run.</param>
        /// <param name="autoStartThread">Should the thread start after creation.</param>
        public EnumeratableActionThread(Func<ThreadBase, IEnumerator> enumeratableAction, bool autoStartThread)
			: base("EnumeratableActionThread", Dispatcher.Current, false)
        {
            this.enumeratableAction = enumeratableAction;
            if (autoStartThread)
                Start();
        }

        protected override IEnumerator Do()
        {
            return enumeratableAction(this);
        }
    }

	public sealed class TickThread : ThreadBase
	{
		private Action action;
		private int tickLengthInMilliseconds;
        private ManualResetEvent tickEvent = new ManualResetEvent(false);


        /// <summary>
        /// Creates a new Thread which runs the given action.
        /// The thread will start running after creation.
        /// </summary>
        /// <param name="action">The enumeratable action to run.</param>
        /// <param name="tickLengthInMilliseconds">Time between ticks.</param>
        public TickThread(Action action, int tickLengthInMilliseconds)
			: this(action, tickLengthInMilliseconds, true)
        {
        }

        /// <summary>
        /// Creates a new Thread which runs the given action.
        /// </summary>
        /// <param name="action">The action to run.</param>
		/// <param name="tickLengthInMilliseconds">Time between ticks.</param>
        /// <param name="autoStartThread">Should the thread start after creation.</param>
        public TickThread(Action action, int tickLengthInMilliseconds, bool autoStartThread)
            : base("TickThread", Dispatcher.CurrentNoThrow, false)
        {
			this.tickLengthInMilliseconds = tickLengthInMilliseconds;
            this.action = action;
            if (autoStartThread)
                Start();
        }

        protected override IEnumerator Do()
        {
            while (!exitEvent.InterWaitOne(0))
            {
				action();

                var result = WaitHandle.WaitAny(new WaitHandle[] { exitEvent, tickEvent }, tickLengthInMilliseconds);
				if (result == 0)
                    return null;
			}
            return null;
        }
	}
}

--- UnityThreadHelper.cs ---
using System.Linq;
using System.Collections.Generic;
using System.Collections;

#if !NO_UNITY
using UnityEngine;
#endif

#if !NO_UNITY
[ExecuteInEditMode]
public class UnityThreadHelper : MonoBehaviour
#else
public class UnityThreadHelper
#endif
{
    private static UnityThreadHelper instance = null;
    private static object syncRoot = new object();

    public static void EnsureHelper()
    {
        lock (syncRoot)
        {
#if !NO_UNITY
            if (null == (object)instance)
            {
                instance = FindObjectOfType(typeof(UnityThreadHelper)) as UnityThreadHelper;
                if (null == (object)instance)
                {
                    var go = new GameObject("[UnityThreadHelper]");
                    go.hideFlags = HideFlags.NotEditable | HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    instance = go.AddComponent<UnityThreadHelper>();
                    instance.EnsureHelperInstance();
                }
            }
#else
		    if (null == instance)
		    {
			    instance = new UnityThreadHelper();
			    instance.EnsureHelperInstance();
		    }
#endif
        }
    }

    private static UnityThreadHelper Instance
    {
        get
        {
            EnsureHelper();
            return instance;
        }
    }

    /// <summary>
    /// Returns the GUI/Main Dispatcher.
    /// </summary>
    public static UnityThreading.Dispatcher Dispatcher
    {
        get
        {
            return Instance.CurrentDispatcher;
        }
    }

    /// <summary>
    /// Returns the TaskDistributor.
    /// </summary>
    public static UnityThreading.TaskDistributor TaskDistributor
    {
        get
        {
            return Instance.CurrentTaskDistributor;
        }
    }

    private UnityThreading.Dispatcher dispatcher;
    public UnityThreading.Dispatcher CurrentDispatcher
    {
        get
        {
            return dispatcher;
        }
    }

    private UnityThreading.TaskDistributor taskDistributor;
    public UnityThreading.TaskDistributor CurrentTaskDistributor
    {
        get
        {
            return taskDistributor;
        }
    }

    private void EnsureHelperInstance()
    {
		dispatcher = UnityThreading.Dispatcher.MainNoThrow ?? new UnityThreading.Dispatcher();
		taskDistributor = UnityThreading.TaskDistributor.MainNoThrow ?? new UnityThreading.TaskDistributor("TaskDistributor");
    }

    /// <summary>
    /// Creates new thread which runs the given action. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The action which the new thread should run.</param>
    /// <param name="autoStartThread">True when the thread should start immediately after creation.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ActionThread CreateThread(System.Action<UnityThreading.ActionThread> action, bool autoStartThread)
    {
        Instance.EnsureHelperInstance();

        System.Action<UnityThreading.ActionThread> actionWrapper = currentThread =>
            {
                try
                {
                    action(currentThread);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError(ex);
                }
            };
        var thread = new UnityThreading.ActionThread(actionWrapper, autoStartThread);
        Instance.RegisterThread(thread);
        return thread;
    }

    /// <summary>
    /// Creates new thread which runs the given action and starts it after creation. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The action which the new thread should run.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ActionThread CreateThread(System.Action<UnityThreading.ActionThread> action)
    {
        return CreateThread(action, true);
    }

    /// <summary>
    /// Creates new thread which runs the given action. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The action which the new thread should run.</param>
    /// <param name="autoStartThread">True when the thread should start immediately after creation.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ActionThread CreateThread(System.Action action, bool autoStartThread)
    {
        return CreateThread((thread) => action(), autoStartThread);
    }

    /// <summary>
    /// Creates new thread which runs the given action and starts it after creation. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The action which the new thread should run.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ActionThread CreateThread(System.Action action)
    {
        return CreateThread((thread) => action(), true);
    }

    #region Enumeratable

    /// <summary>
    /// Creates new thread which runs the given action. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The enumeratable action which the new thread should run.</param>
    /// <param name="autoStartThread">True when the thread should start immediately after creation.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ThreadBase CreateThread(System.Func<UnityThreading.ThreadBase, IEnumerator> action, bool autoStartThread)
    {
        Instance.EnsureHelperInstance();

        var thread = new UnityThreading.EnumeratableActionThread(action, autoStartThread);
        Instance.RegisterThread(thread);
        return thread;
    }

    /// <summary>
    /// Creates new thread which runs the given action and starts it after creation. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The enumeratable action which the new thread should run.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ThreadBase CreateThread(System.Func<UnityThreading.ThreadBase, IEnumerator> action)
    {
        return CreateThread(action, true);
    }

    /// <summary>
    /// Creates new thread which runs the given action. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The enumeratable action which the new thread should run.</param>
    /// <param name="autoStartThread">True when the thread should start immediately after creation.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ThreadBase CreateThread(System.Func<IEnumerator> action, bool autoStartThread)
    {
        System.Func<UnityThreading.ThreadBase, IEnumerator> wrappedAction = (thread) => { return action(); };
        return CreateThread(wrappedAction, autoStartThread);
    }

    /// <summary>
    /// Creates new thread which runs the given action and starts it after creation. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The action which the new thread should run.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ThreadBase CreateThread(System.Func<IEnumerator> action)
    {
        System.Func<UnityThreading.ThreadBase, IEnumerator> wrappedAction = (thread) => { return action(); };
        return CreateThread(wrappedAction, true);
    }

    #endregion

    List<UnityThreading.ThreadBase> registeredThreads = new List<UnityThreading.ThreadBase>();
        
	private void RegisterThread(UnityThreading.ThreadBase thread)
    {
        if (registeredThreads.Contains(thread))
        {
            return;
        }

        registeredThreads.Add(thread);
    }

#if !NO_UNITY

    void OnDestroy()
    {
        foreach (var thread in registeredThreads)
            thread.Dispose();

        if (dispatcher != null)
            dispatcher.Dispose();
        dispatcher = null;

        if (taskDistributor != null)
            taskDistributor.Dispose();
        taskDistributor = null;

        if (instance == this)
            instance = null;
    }

    void Update()
    {
        if (dispatcher != null)
            dispatcher.ProcessTasks();

        var finishedThreads = registeredThreads.Where(thread => !thread.IsAlive).ToArray();
        foreach (var finishedThread in finishedThreads)
        {
            finishedThread.Dispose();
            registeredThreads.Remove(finishedThread);
        }
    }
#endif
}

--- WaitOneExtension.cs ---
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if !NO_UNITY
using UnityEngine;
#endif

namespace UnityThreading
{
	public static class WaitOneExtension
	{
#if UNITY_WEBPLAYER
		private static System.Reflection.MethodInfo WaitOneMilliseconds;
		private static System.Reflection.MethodInfo WaitOneTimeSpan;

		static WaitOneExtension()
		{
			var type = typeof(System.Threading.ManualResetEvent);
			WaitOneMilliseconds = type.GetMethod("WaitOne", new System.Type[1] { typeof(int) });
			WaitOneTimeSpan = type.GetMethod("WaitOne", new System.Type[1] { typeof(TimeSpan) });
		}


		public static bool InterWaitOne(this System.Threading.ManualResetEvent that, int ms)
		{
			return (bool)WaitOneMilliseconds.Invoke(that, new object[1] { ms });
		}

		public static bool InterWaitOne(this System.Threading.ManualResetEvent that, TimeSpan duration)
		{
			return (bool)WaitOneTimeSpan.Invoke(that, new object[1] { duration });
		}
#else
		public static bool InterWaitOne(this System.Threading.ManualResetEvent that, int ms)
		{
			return that.WaitOne(ms, false);
		}

		public static bool InterWaitOne(this System.Threading.ManualResetEvent that, TimeSpan duration)
		{
			return that.WaitOne(duration, false);
		}
#endif
	}
}
