using UnityEngine;

public readonly struct RollProfile
{
    private readonly float _transitionDuration;
    private readonly float _holdDuration;
    private readonly float _depth;

    public RollProfile(float transitionDuration, float holdDuration, float depth)
    {
        _transitionDuration = Mathf.Max(0.0001f, transitionDuration);
        _holdDuration = Mathf.Max(0f, holdDuration);
        _depth = depth;
    }

    public float TotalDuration => _transitionDuration * 2f + _holdDuration;

    public float Evaluate(float elapsed)
    {
        if (elapsed < _transitionDuration)
        {
            return _depth * (elapsed / _transitionDuration);
        }

        float riseStart = _transitionDuration + _holdDuration;
        if (elapsed < riseStart)
        {
            return _depth;
        }

        return _depth * Mathf.Clamp01(1f - (elapsed - riseStart) / _transitionDuration);
    }
}
