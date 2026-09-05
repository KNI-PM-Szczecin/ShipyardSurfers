public class TrackingStateMachine
{
    private readonly TrackingTimings _timings;
    private float _lastFrameTime = float.NegativeInfinity;
    private float _lastValidPoseTime = float.NegativeInfinity;
    private float _warmupUntil = float.NegativeInfinity;
    private float _lostSince;
    private int _consecutiveValid;

    public TrackingState State { get; private set; } = TrackingState.Disabled;

    public TrackingStateMachine(TrackingTimings timings)
    {
        _timings = timings;
    }

    public void Enable(float now)
    {
        _lastValidPoseTime = float.NegativeInfinity;
        _consecutiveValid = 0;
        State = TrackingState.NoCamera;
        NotifyCameraStarted(now);
    }

    public void Disable()
    {
        State = TrackingState.Disabled;
    }

    public void NotifyCameraStarted(float now)
    {
        _lastFrameTime = now;
        _warmupUntil = now + _timings.CameraWarmup;
    }

    public void Update(float now, bool frameArrived, bool resultArrived, bool poseValid)
    {
        if (State == TrackingState.Disabled) return;

        if (frameArrived) _lastFrameTime = now;

        if (resultArrived)
        {
            _consecutiveValid = poseValid ? _consecutiveValid + 1 : 0;
            if (poseValid) _lastValidPoseTime = now;
        }

        if (now >= _warmupUntil && now - _lastFrameTime > _timings.NoCameraTimeout)
        {
            State = TrackingState.NoCamera;
            return;
        }

        switch (State)
        {
            case TrackingState.NoCamera:
                if (frameArrived) State = TrackingState.Searching;
                break;
            case TrackingState.Searching:
                if (_consecutiveValid >= _timings.ValidFramesToTrack) State = TrackingState.Tracking;
                break;
            case TrackingState.Tracking:
                if (now - _lastValidPoseTime > _timings.LostTimeout)
                {
                    State = TrackingState.Lost;
                    _lostSince = now;
                }
                break;
            case TrackingState.Lost:
                if (_consecutiveValid >= _timings.ValidFramesToTrack) State = TrackingState.Tracking;
                else if (now - _lostSince > _timings.LostToSearchingTimeout) State = TrackingState.Searching;
                break;
        }
    }
}
