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
