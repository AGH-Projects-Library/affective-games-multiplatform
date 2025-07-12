using UnityEngine;
using System.Collections;

public class GraphVisualizer : MonoBehaviour {

	#region MEMBER VARIABLES (DECLARATIONS)

	public BitalinoReader bitalino;
	
	public enum FunctionOption { 
		_Linear, 
		_Exponential, 
		_Parabola, 
		_Sine,
		_Ripple,
		_Bitalino
	};
	public FunctionOption function;

	private delegate float FunctionDelegate (Vector3 p, float t, int d, float s);
	private static FunctionDelegate[] m_functionDelegates = {
		Linear,
		Exponential,
		Parabola,
		Sine,
		Ripple
	};

	public enum ChannelOption {
        _ECG,
        _EMG,
		_EDA,
		_LUX,
		_ACC,
		_BATT
	};
	public ChannelOption channel;

	public enum RepresentationOption {
		_2D,
		_3D,
		_4D
	};
	public RepresentationOption representation;

	public bool applyBandpassFilter;
	
	[Range (0, 5f)]
	public float highpassCutoffFrequency;

	[Range (5f, 100f)]
	public float lowpassCutoffFrequency;

	[Range (25, 100)]
	public int resolution = 100;

	[Range (1f, 5f)]
	public float scale = 2;
	
	[Range (0.05f, 0.2f)]
	public float pointSize = 0.1f;

	private float m_fs;

	// private ParticleSystem.Particle[] m_points;
	// private ParticleSystem m_graphVisualizer;

	private float[] m_channelGains;

	private bool m_isBitalinoAcquiring;

	private float m_maximum;
	
    private float[] m_heartRates = new float[100];

	public static float s_heartRate;
	[SerializeField] private float m_heartRate = 2;

	private bool m_peakFlag = false;
	private float m_peakThreshold = 0;
	private float m_peakTimer = 0;

	private AudioSource m_beepSound;

	// public GameObject threshold;
	
	#endregion

	// Use this for initialization
	void Awake () {
	
		// m_graphVisualizer = this.GetComponent<ParticleSystem> ();

		m_channelGains = new float[] { 1, 25, 100, 1, 1 };

		m_fs = bitalino.manager.SamplingFrequency;

		m_beepSound = this.GetComponent<AudioSource> ();
	}
	
	// Update is called once per frame
	void Update () {

		m_isBitalinoAcquiring = bitalino.asStart && function == FunctionOption._Bitalino;

		DrawGraph ();
	}

	// Draws the specified Function / Signal
	void DrawGraph () {

		print ((int)channel);

		BlinkBehaviour.isBitalinoAcquiring = m_isBitalinoAcquiring;

		int nDimensions = (int) representation + 1;
		int actualResolution = m_isBitalinoAcquiring ? bitalino.BufferSize * resolution / (100 * nDimensions * nDimensions * nDimensions) : resolution;
		int nPointsPerDimension = actualResolution / nDimensions;
		float increment = scale / nPointsPerDimension;
		int nPoints = (int) Mathf.Pow (nPointsPerDimension, nDimensions);

		// m_points = new ParticleSystem.Particle [nPoints];

		if (nDimensions == 1) {

			this.transform.LookAt (Vector3.forward);
		}
		else {

			this.transform.Rotate (Vector3.up, nDimensions * Time.deltaTime);
		}

		BITalinoFrame[] bitalinoBuffer = m_isBitalinoAcquiring ? bitalino.getBuffer () : null;
    
		int n = m_heartRates.Length;
		float previousSignal = 0;
		float previousFilteredSignal_lp = 0;
		float previousFilteredSignal_hp = 0;
		float t = Time.timeSinceLevelLoad;
		int index = 0;

		for (int k = 0; k < (nDimensions > 1 ? nPointsPerDimension : 1); k++) {

			for (int j = 0; j < (nDimensions > 2 ? nPointsPerDimension : 1); j++) {
			
				for (int i = 0; i < (nDimensions > 0 ? nPointsPerDimension : 1); i++) {

					Vector3 p = new Vector3 (i, j, k) * increment;
					float alpha = 1f;

					float F = 0;

					if (m_isBitalinoAcquiring) {

						float signal = (float) bitalinoBuffer[i].GetAnalogValue ((int) channel);
						signal = (signal - 50) * scale / m_channelGains[(int) channel];
					
						F = signal;

						if (applyBandpassFilter) {

							float alpha_hp = m_fs / (2 * Mathf.PI * highpassCutoffFrequency + m_fs);

							F = i == 0 ? 0 : alpha_hp * (previousFilteredSignal_hp + signal - previousSignal);
							
							previousFilteredSignal_hp = F;

							float alpha_lp = 2 * Mathf.PI * lowpassCutoffFrequency / (2 * Mathf.PI * lowpassCutoffFrequency + m_fs);

							F = i == 0 ? 0 : alpha_lp * F + (1 - alpha_lp) * previousFilteredSignal_lp;

							previousFilteredSignal_lp = F;
						}

						if (F > m_maximum) {

							m_maximum = F;
						}

						m_peakThreshold = m_maximum * 0.7f;

						if (F < m_peakThreshold && m_peakTimer > 1.5f) {

							m_maximum -= 0.01f;
						}

						if (F > m_peakThreshold * 1.1f && !m_peakFlag) {

							if (i > nPointsPerDimension - 50 && !m_beepSound.isPlaying && nDimensions == 1) {

								m_beepSound.Play ();
							}

							float heartRate = 60 / m_peakTimer;

							if (heartRate > 40 && heartRate < 130) {

								for (int w = n - 1; w > 0; w--) {

									m_heartRates[w] = m_heartRates[w-1];
								}

								m_heartRates[0] = heartRate;
							}

							m_peakTimer = 0;
								
							m_peakFlag = true;
						}

						if (F < m_peakThreshold * 0.9f && m_peakFlag) {

							m_peakFlag = false;
						}

						m_peakTimer += 1 / m_fs;

						previousSignal = i == 0 ? 0 : signal;
					}

					else if (function != FunctionOption._Bitalino) {

						FunctionDelegate f = m_functionDelegates [(int) function];

						F = f (p, t, nDimensions, scale);
					}

					if (nDimensions == 3) {

						alpha = F;
					}
					else {

						p.y = F;
					}

					// m_points [index].position = p - new Vector3 ((float) scale / 2, nDimensions == 3 ? (scale - 1) /2 : 0, nDimensions > 1 ? (float) scale / 2 : 0);
					// m_points [index].startColor = new Color (0f, scale - p.z, scale - p.z, alpha);
					// m_points [index++].startSize = pointSize;
				}
			}
		}

		if (m_isBitalinoAcquiring) {

			float sum = 0;
			for (int w = 0; w < n; w++) {

				sum += m_heartRates[w];
			}

			s_heartRate = sum / n;
			m_heartRate = s_heartRate;
				
			// threshold.transform.localScale = new Vector3 (scale, 0.01f, 0.01f);
			// threshold.transform.position = new Vector3 (0, m_peakThreshold, 0.5f);
		}

		// threshold.SetActive (m_isBitalinoAcquiring && nDimensions == 1);

		// m_graphVisualizer.SetParticles (m_points, m_points.Length);
	}

	#region FUNCTION DELEGATES

	private static float Linear (Vector3 p, float t, int d, float s) {

		float f = 0;

		switch (d) {

		case 1:
			f = p.x + 0.1f * Mathf.Sin (76 * 2 * Mathf.PI * p.x * 1 + t) - (s - 1) /2;
			break;

		case 2:
			f = (p.x + p.z) / 2 + 0.1f * Mathf.Sin (10 * 2 * Mathf.PI * p.x * p.z + t) - (s - 1) /2;
			break;

		case 3:
			f = s - p.x - p.y - p.z + s / 2 * Mathf.Sin(t);
			break;
		}

		return f;
	}
	
	private static float Exponential (Vector3 p, float t, int d, float s) {

		float power = 1 + Mathf.PingPong (t, 3);

		float f = 0;
		
		switch (d) {
			
		case 1:
			f = Mathf.Pow (p.x, power);
			break;
			
		case 2:
			f = Mathf.Pow (p.x, power) - Mathf.Pow (p.z, power);
			break;
			
		case 3:
			f = s - p.x * p.x - p.y * p.y - p.z * p.z + s / 2 * Mathf.Sin(t);
			break;
		}
		
		return f;
	}
	
	private static float Parabola (Vector3 p, float t, int d, float s) {

		float pingpong = Mathf.PingPong (t, 5) / 2.5f;

		float f = 0;
		
		switch (d) {
			
		case 1:
			f = pingpong * Mathf.Pow (2 * p.x - s, 2);
			break;
			
		case 2:
			f = pingpong * Mathf.Pow (2 * p.x - s, 2) * Mathf.Pow (2 * p.z - s, 2);
			break;

		case 3:
			f = s - pingpong * (Mathf.Pow (2 * p.x - s, 2) - Mathf.Pow (2 * p.y - s, 2) - Mathf.Pow (2 * p.z - s, 2));
			break;
		}
		
		return f;
	}
	
	private static float Sine (Vector3 p, float t, int d, float s) {

		float f = 0;
		
		switch (d) {
			
		case 1:			
		case 2:
			f = 0.50f + 0.25f * Mathf.Sin (4f * Mathf.PI * p.x + 4f * t) * Mathf.Sin (2f * Mathf.PI * p.z + t) +
				0.10f * Mathf.Cos (3f * Mathf.PI * p.x + 5f * t) * Mathf.Cos (5f * Mathf.PI * p.z + 3f * t) +
				0.15f * Mathf.Sin (Mathf.PI * p.x + 0.6f * t);
			break;
			
		case 3:
			float x = Mathf.Sin (2 * Mathf.PI * p.x);
			float y = Mathf.Sin (2 * Mathf.PI * p.y);
			float z = Mathf.Sin (2 * Mathf.PI * p.z + (p.y > 0.5f ? t : -t));

			f = Mathf.Pow (x, 2) * Mathf.Pow (y, 2) * Mathf.Pow (z, 2);
			break;
		}
		
		return f;
	}
	
	private static float Ripple (Vector3 p, float t, int d, float s) {

		float squareRadius = (p - new Vector3 (d > 0 ? 1 : 0, d > 2 ? 1 : 0, d > 1 ? 1 : 0) * s / 2).sqrMagnitude;

		float f = 0;

		switch (d) {
			
		case 1:			
		case 2:
		
			float frequency = 2 * Mathf.PI * (s_heartRate != 0 ? s_heartRate / 3 : 7.5f) * squareRadius;

			f = 0.5f + s * Mathf.Sin (frequency - 2f * t) / (2f + 100f * squareRadius);
			break;
			
		case 3:
			f = Mathf.Sin (4f * Mathf.PI * squareRadius - 2f * t);
			break;
		}

		return f;
	}

	#endregion
}
