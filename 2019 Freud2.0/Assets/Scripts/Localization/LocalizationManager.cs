using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [SerializeField] private LocalizationData localizationData;
    public GameLanguage CurrentLanguage { get; private set; } = GameLanguage.English;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetLanguage(GameLanguage lang) => CurrentLanguage = lang;
    public string GetText(string key) => localizationData.GetText(key, CurrentLanguage);
}