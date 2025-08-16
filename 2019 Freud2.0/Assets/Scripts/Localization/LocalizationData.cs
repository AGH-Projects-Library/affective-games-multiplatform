using System.Collections.Generic;
using UnityEngine;

namespace Localization
{
    public enum GameLanguage { English, Polish }

    [System.Serializable]
    public class LocalizationEntry
    {
        public string key;
        [TextArea(2, 10)] public string english;
        [TextArea(2, 10)] public string polish;
    }

    [CreateAssetMenu(fileName = "LocalizationData", menuName = "Localization/Data")]
    public partial class LocalizationData : ScriptableObject
    {
        [SerializeField] public List<LocalizationEntry> entries = new List<LocalizationEntry>();
        
        public bool TryGetText(string key, GameLanguage lang, out string text)
        {
            var entry = entries.Find(e => e.key == key);
            if (entry == null) { text = $"[MISSING:{key}]"; return false; }
            text = lang == GameLanguage.English ? entry.english : entry.polish;
            return true;
        }

        public void AddOrUpdate(string key, string polish, string english)
        {
            var entry = entries.Find(e => e.key == key);
            if (entry == null)
                entries.Add(new LocalizationEntry { key = key, english = english, polish = polish });
            else
            {
                entry.english = english;
                entry.polish = polish;
            }
        }
    }
}