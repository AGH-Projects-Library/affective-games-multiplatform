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