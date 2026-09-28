using UnityEngine;

public class DispatchPlanner
{
    private const float FRAME_TIME_SMOOTHING = 0.15f;
    private const float FRAME_FIT_TOLERANCE = 0.001f;

    private readonly float _windowSeconds;
    private float _frameTime;
    private bool _hasFrameTime;

    public DispatchPlanner(float windowSeconds)
    {
        _windowSeconds = Mathf.Max(0f, windowSeconds);
    }

    public float FrameTime => _frameTime;

    public void ReportFrameTime(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        _frameTime = _hasFrameTime ? _frameTime + (deltaTime - _frameTime) * FRAME_TIME_SMOOTHING : deltaTime;
        _hasFrameTime = true;
    }

    public int FramesForDispatch()
    {
        if (!_hasFrameTime || _frameTime <= 0f) return 1;

        return Mathf.Max(1, Mathf.FloorToInt(_windowSeconds / _frameTime + FRAME_FIT_TOLERANCE));
    }

    public int LayersPerFrame(int layerCount)
    {
        return Mathf.Max(1, Mathf.CeilToInt((float)layerCount / FramesForDispatch()));
    }
}
