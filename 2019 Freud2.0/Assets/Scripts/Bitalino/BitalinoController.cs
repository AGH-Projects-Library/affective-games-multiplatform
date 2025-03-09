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