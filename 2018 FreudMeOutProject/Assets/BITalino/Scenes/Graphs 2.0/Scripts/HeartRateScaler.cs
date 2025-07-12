using UnityEngine;
using System.Collections;

public class HeartRateScaler : MonoBehaviour {


	// Update is called once per frame
	void Update () {
	
		float s = GraphVisualizer.s_heartRate / 15;

		this.transform.localScale = new Vector3 (s , s, s);
	}
}
