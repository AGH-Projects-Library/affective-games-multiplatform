using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StimuliManager : MonoBehaviour
{
    private List<string> stimuliMinus = new();
    private List<string> stimuliPlus = new();

    public void LoadStimuli(string path)
    {
        using StreamReader reader = new StreamReader(path);
        while (reader.Peek() >= 0)
        {
            string[] line = reader.ReadLine().Split(',');
            if (line[0] == "s-") stimuliMinus.Add(line[1]);
            else if (line[0] == "s+") stimuliPlus.Add(line[1]);
        }

        Shuffle(stimuliMinus);
        Shuffle(stimuliPlus);
    }

    private void Shuffle<T>(List<T> list)
    {
        int count = list.Count;
        for (int i = 0; i < count - 1; i++)
        {
            int r = Random.Range(i, count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    public string GetNextMinus() => PopNext(stimuliMinus);
    public string GetNextPlus() => PopNext(stimuliPlus);

    private string PopNext(List<string> list)
    {
        if (list.Count == 0) return null;
        string item = list[0];
        list.RemoveAt(0);
        return item;
    }
}