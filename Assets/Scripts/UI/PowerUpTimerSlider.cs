using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpTimerSlider : MonoBehaviour
{
    private const string SECONDS_FORMAT = "{0:0.0} s";

    [SerializeField] private Slider _slider;
    [SerializeField] private Image _fill;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private TMP_Text _timeText;

    private PowerUpEffect _effect;

    public void Bind(PowerUpEffect effect, PowerUpVisual visual)
    {
        _effect = effect;
        Present(visual, effect.Duration, effect.RemainingTime.Value);
        _effect.RemainingTime.Subscribe(UpdateValue);
    }

    public void Present(PowerUpVisual visual, float duration, float remainingTime)
    {
        _slider.minValue = 0f;
        _slider.maxValue = Mathf.Max(duration, Mathf.Epsilon);

        if (_fill != null) _fill.color = visual.Color;
        if (_icon != null)
        {
            _icon.sprite = visual.Icon;
            _icon.enabled = visual.Icon != null;
        }

        if (_label != null) _label.text = visual.Label;
        UpdateValue(remainingTime);
    }

    private void UpdateValue(float remainingTime)
    {
        _slider.value = remainingTime;
        if (_timeText != null) _timeText.text = string.Format(SECONDS_FORMAT, remainingTime);
    }

    private void OnDestroy()
    {
        _effect?.RemainingTime.Unsubscribe(UpdateValue);
    }
}
