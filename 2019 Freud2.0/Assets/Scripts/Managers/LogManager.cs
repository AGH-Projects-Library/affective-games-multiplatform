using System;
using System.IO;
using UnityEngine;

public class LogManager : iSingleton<LogManager>
{
    [SerializeField] private string _fileName = "events.csv";
    [SerializeField] private bool doNotSaveLogs = true;
    private double _startTime;
    
    public struct LogEvent
    {
        public float Time;
        public string eventType;
        public LogEvent(float t, string eT) { Time = t; eventType = eT; }
    }

    private System.Collections.Generic.List<LogEvent> _events = new System.Collections.Generic.List<LogEvent>();

    public static void Log(float time,string eventType)
    {
        if(!InstanceExists()) return;
        Instance.AddEvent(time, eventType);
    }
    
    private void AddEvent(float time, string eventType) =>_events.Add(new LogEvent(time * 1000, eventType));
    
    private void OnDestroy()
    {
        if (doNotSaveLogs) return;
        StreamWriter writer = File.AppendText(_fileName);
        writer.WriteLine(_startTime.ToString() + ";" + "UnixTime");
        foreach (var e in _events) { writer.WriteLine(e.Time + ";" + e.eventType); }
        writer.Close();
    }
}
