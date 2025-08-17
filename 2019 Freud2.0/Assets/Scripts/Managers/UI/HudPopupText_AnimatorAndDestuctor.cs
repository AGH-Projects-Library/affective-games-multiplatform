using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudPopupText_AnimatorAndDestuctor : MonoBehaviour
{
    [SerializeField] private TMP_Text _alertText;
    [SerializeField] private float _duration = 2f;
    [SerializeField] private AnimationCurve _transparencyCurve = AnimationCurve.Linear(0, 1, 1, 0);
    private float _startTime;
    
    public void DisplayAlert(string message, float duration)
    {
        _alertText.text = message;
        _duration = duration;
        _startTime = Time.time;
        UpdateAlertTextColor();
    }

    private float ElapsedTime => Time.time - _startTime;

    private void Update()
    {
        UpdateAlertTextColor();

        if (ElapsedTime >= _duration)
        {
            Destroy(gameObject, 0.1f);
            _alertText.enabled = false;
        }
    }

    private void UpdateAlertTextColor()
    {
        var color = _alertText.color;
        color.a = _transparencyCurve.Evaluate(ElapsedTime / _duration);
        _alertText.color = color;
    }
}
