using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneSwapper : MonoBehaviour
{
    public static SceneSwapper Instance { get; private set; }

    public const int MainMenuSceneIndex = 0;
    public const int EndGameSceneIndex = 11;
    public const int ScoreBoardSceneIndex = 12;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);
    }

    public static void LoadNextScene() => LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    public static void LoadScene(int buildIndex) => SceneManager.LoadScene(buildIndex);
    public static void LoadEndGameScene() => SceneManager.LoadScene(EndGameSceneIndex);
}