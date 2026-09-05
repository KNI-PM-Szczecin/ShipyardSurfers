using UnityEngine;

public class OneEuroFilter
{
    private readonly SmoothingSettings _settings;
    private float _previousValue;
    private float _previousDerivative;
    private float _previousTime;
    private bool _initialized;

    public OneEuroFilter(SmoothingSettings settings)
    {
        _settings = settings;
    }

    public void Reset()
    {
        _initialized = false;
    }

    public float Filter(float value, float time)
    {
        if (!_initialized)
        {
            _initialized = true;
            _previousValue = value;
            _previousDerivative = 0f;
            _previousTime = time;
            return value;
        }

        float deltaTime = time - _previousTime;
        if (deltaTime <= 0f)
        {
            return _previousValue;
        }

        float rawDerivative = (value - _previousValue) / deltaTime;
        float derivative = Lerp(_previousDerivative, rawDerivative, Alpha(_settings.DerivativeCutoff, deltaTime));
        float cutoff = _settings.MinCutoff + _settings.Beta * Mathf.Abs(derivative);
        float filtered = Lerp(_previousValue, value, Alpha(cutoff, deltaTime));

        _previousValue = filtered;
        _previousDerivative = derivative;
        _previousTime = time;
        return filtered;
    }

    private static float Alpha(float cutoff, float deltaTime)
    {
        float tau = 1f / (2f * Mathf.PI * cutoff);
        return 1f / (1f + tau / deltaTime);
    }

    private static float Lerp(float from, float to, float alpha)
    {
        return from + alpha * (to - from);
    }
}
