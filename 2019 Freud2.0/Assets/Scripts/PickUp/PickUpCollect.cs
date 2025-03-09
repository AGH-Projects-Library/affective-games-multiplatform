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
