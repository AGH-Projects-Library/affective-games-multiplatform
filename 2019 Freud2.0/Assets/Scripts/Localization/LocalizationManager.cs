using UnityEngine;

namespace Localization
{
    public class LocalizationManager : iSingleton<LocalizationManager>
    {

        [SerializeField] private LocalizationData localizationData;
        public GameLanguage CurrentLanguage { get; private set; } = GameLanguage.English;

        private new void Awake()
        {
            InitInstance();
            DontDestroyOnLoad(gameObject);
        }
        
        public static void SetLanguage(GameLanguage lang)
        {
            if (Instance == null) { Debug.LogError("LocalizationManager instance is not initialized."); return; }
            Instance.CurrentLanguage = lang;
            Debug.Log($"Language set to: {lang}");
        }
        
        public static bool TryGetText(string key, out string text)
        {
            if (!InstanceExists()) {
                text = "NO LOCALIZATION MANAGER";
                return false; }
            return Instance.localizationData.TryGetText(key, Instance.CurrentLanguage, out text);
        }
    }
}