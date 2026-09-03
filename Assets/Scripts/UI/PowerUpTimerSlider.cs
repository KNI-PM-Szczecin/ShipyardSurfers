using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpTimerSlider : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _fill;
    [SerializeField] private TMP_Text _label;

    private PowerUpEffect _effect;

    public void Bind(PowerUpEffect effect, PowerUpVisual visual)
    {
        _effect = effect;

        _slider.minValue = 0f;
        _slider.maxValue = effect.Duration;
        _fill.color = visual.Color;
        _label.text = visual.Label;

        _effect.RemainingTime.Subscribe(UpdateValue);
    }

    private void UpdateValue(float remainingTime)
    {
        _slider.value = remainingTime;
    }

    private void OnDestroy()
    {
        _effect?.RemainingTime.Unsubscribe(UpdateValue);
    }
}
