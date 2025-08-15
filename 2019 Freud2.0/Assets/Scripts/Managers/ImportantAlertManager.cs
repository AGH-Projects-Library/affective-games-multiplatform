using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ImportantAlertManager : MonoBehaviour
{
    public static ImportantAlertManager importantAlertManager;

    void Awake()
    {
        MakeThisTheOnlyDontDestroyManager();
    }

    void MakeThisTheOnlyDontDestroyManager()
    {
        if (importantAlertManager == null)
        {
            DontDestroyOnLoad(gameObject);
            importantAlertManager = this;
        }
        else if (importantAlertManager != this)
        {
            Destroy(gameObject);
        }
    }

    public IEnumerator ShowAlertAndLerp(float time, string input)
    {
        Text txt = gameObject.GetComponent<Text>();
        txt.enabled = true;
        txt.text = input;
        Color startingColor = Color.green;
        txt.color = startingColor;
        time = time == 0.5f ? 1f : time - 0.5f;
        float inversedTime = 1 / time;
        for (float step = 0.0f; step < 1.0f; step += Time.deltaTime * inversedTime)
        {
            txt.color = Color.Lerp(startingColor, Color.clear, step);
            yield return null;
        }
        txt.enabled = false;
    }

    public IEnumerator ShowAlert(float time, string input)
    {
        Text txt = gameObject.GetComponent<Text>();
        txt.enabled = true;
        txt.text = input;
        yield return new WaitForSeconds(time);
        txt.enabled = false;
    }
}
