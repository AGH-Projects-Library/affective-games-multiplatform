using UnityEngine;
using System.Collections;

public class BlinkBehaviour : MonoBehaviour {

	public static bool isBitalinoAcquiring;

	private Light m_light;

	// Use this for initialization
	void Awake () {
	
		m_light = this.GetComponent<Light> ();
	}
	
	// Update is called once per frame
	void Update () {
	
		float blinkDelay = isBitalinoAcquiring ? 0.25f : 2f;
		float lerp = Mathf.PingPong (Time.time, blinkDelay) / blinkDelay;

		m_light.intensity = Mathf.Lerp (0, 8, lerp);
	}
}
