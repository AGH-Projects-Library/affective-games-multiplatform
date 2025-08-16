using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class SceneSwapper : MonoBehaviour
{
    public static SceneSwapper Instance { get; private set; }

    public UnityAction<int> OnSceneLoadRequested;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void LoadNextScene() => LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

    public void LoadScene(int buildIndex)
    {
        OnSceneLoadRequested?.Invoke(buildIndex);
        SceneManager.LoadScene(buildIndex);
    }
}

// Below code is used to paste to the LLM so that it knows how to generate other classes that will be compatible with this one

// List all the public and private variables, methods (with parameters if any), and properties
/***
@startuml
class SceneSwapper {
    +Instance: SceneSwapper
    +OnSceneLoadRequested: UnityAction<int>

    +Awake(): void
    +LoadNextScene(): void
    +LoadScene(buildIndex: int): void
}
@enduml
***/