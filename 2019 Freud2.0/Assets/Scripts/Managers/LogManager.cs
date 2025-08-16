using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class LogManager : MonoBehaviour
{
    public static LogManager Instance { get; private set; }
    [SerializeField] private string _fileName = "events.csv";

    private double _startTime;

    public struct LogEvent
    {
        public float Time;
        public string eventType;

        public LogEvent(float t, string eT)
        {
            Time = t;
            eventType = eT;
        }
    }

    private List<LogEvent> _events = new List<LogEvent>();

    private void Awake()
    {
        MakeThisTheOnlyLogManager();
        _startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private void MakeThisTheOnlyLogManager()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);
    }

    private void OnDestroy()
    {
        StreamWriter writer = File.AppendText(UserManager.Instance.GetUserPath() + _fileName);
        writer.WriteLine(_startTime.ToString() + ";" + "UnixTime");
        foreach (var e in _events) { writer.WriteLine(e.Time + ";" + e.eventType); }
        writer.Close();
    }

    public void AddEvent(float time, string eventType)
    { _events.Add(new LogEvent(time * 1000, eventType)); }
    
    public static void Log(float time,string eventType)
    {
        if (Instance == null) { Debug.LogError("LogManager instance is not initialized."); return; }
        Instance.AddEvent(time, eventType);
    }
}
