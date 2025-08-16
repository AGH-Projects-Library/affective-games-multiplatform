using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneSwapper
{
    public const int MainMenuSceneIndex = 0;
    public const int EndGameSceneIndex = 11;
    public const int ScoreBoardSceneIndex = 12;

    public static void LoadNextScene() => LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    public static void LoadScene(int buildIndex) => SceneManager.LoadScene(buildIndex);
    public static void LoadEndGameScene() => SceneManager.LoadScene(EndGameSceneIndex);
}
