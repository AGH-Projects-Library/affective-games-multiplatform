using UnityEngine;

namespace Localization
{
    public class LocalizationManager : MonoBehaviour
    {
        private static LocalizationManager Instance { get; set; }

        [SerializeField] private LocalizationData localizationData;
        public GameLanguage CurrentLanguage { get; private set; } = GameLanguage.English;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        public static void SetLanguage(GameLanguage lang)
        {
            if (Instance == null) { Debug.LogError("LocalizationManager instance is not initialized."); return; }
            Instance.CurrentLanguage = lang;
            Debug.Log($"Language set to: {lang}");
        }
 
        //for everyone else
        public static void SetLanguageEnglish() => SetLanguage(GameLanguage.English);
        public static void SetLanguagePolish() => SetLanguage(GameLanguage.Polish);
        // public static string GetText(string key) => localizationData.GetText(key, CurrentLanguage);
        public static string GetText(string key)
        {
            if (Instance == null) { Debug.LogError("LocalizationManager instance is not initialized."); return $"[MISSING:{key}]"; }
            return Instance.localizationData.GetText(key, Instance.CurrentLanguage);
        }
    }
}