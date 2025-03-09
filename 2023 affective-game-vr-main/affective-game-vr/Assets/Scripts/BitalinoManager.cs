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
