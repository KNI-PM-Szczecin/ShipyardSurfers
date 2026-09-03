using System.Collections.Generic;
using UnityEngine;

public class PowerUpUIManager : MonoBehaviour
{
    [SerializeField] private PowerUpTimerSlider _sliderPrefab;
    [SerializeField] private Transform _container;
    [SerializeField] private PowerUpVisual[] _visuals;

    private readonly Dictionary<PowerUpType, PowerUpTimerSlider> _activeSliders =
        new Dictionary<PowerUpType, PowerUpTimerSlider>();

    private void OnEnable()
    {
        EventBus.PowerUpActivatedEvent += ShowSlider;
        EventBus.PowerUpDeactivatedEvent += HideSlider;
    }

    private void OnDisable()
    {
        EventBus.PowerUpActivatedEvent -= ShowSlider;
        EventBus.PowerUpDeactivatedEvent -= HideSlider;
    }

    private void ShowSlider(PowerUpEffect effect)
    {
        if (_activeSliders.ContainsKey(effect.Type)) return;

        PowerUpTimerSlider slider = Instantiate(_sliderPrefab, _container);
        slider.Bind(effect, GetVisual(effect.Type));
        _activeSliders.Add(effect.Type, slider);
    }

    private void HideSlider(PowerUpEffect effect)
    {
        if (!_activeSliders.TryGetValue(effect.Type, out PowerUpTimerSlider slider)) return;

        _activeSliders.Remove(effect.Type);
        if (slider != null) Destroy(slider.gameObject);
    }

    private PowerUpVisual GetVisual(PowerUpType type)
    {
        foreach (PowerUpVisual visual in _visuals)
        {
            if (visual.Type == type) return visual;
        }

        return new PowerUpVisual { Type = type, Label = type.ToString(), Color = Color.white };
    }
}
