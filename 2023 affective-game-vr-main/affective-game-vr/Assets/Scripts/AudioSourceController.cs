using UnityEngine;

public class AudioSourceController : MonoBehaviour
{
    [SerializeField] private AudioSource m_audioSource = null;

    private void Update()
    {
        if (!m_audioSource.isPlaying)
            Destroy(gameObject);
    }
}
