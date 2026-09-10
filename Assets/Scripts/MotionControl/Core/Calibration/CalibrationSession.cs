using System;

public enum CalibrationPhase
{
    Countdown,
    Prompt,
    Rest,
    Finished
}

public sealed class CalibrationSession
{
    private const float MIN_PHASE_DURATION = 0.05f;

    private readonly CalibrationPlan _plan;
    private readonly float _countdownDuration;
    private readonly float _promptDuration;
    private readonly float _restDuration;

    private float _elapsed;

    public CalibrationSession(CalibrationPlan plan, float countdownDuration, float promptDuration, float restDuration)
    {
        _plan = plan;
        _countdownDuration = Math.Max(MIN_PHASE_DURATION, countdownDuration);
        _promptDuration = Math.Max(MIN_PHASE_DURATION, promptDuration);
        _restDuration = Math.Max(MIN_PHASE_DURATION, restDuration);
    }

    public CalibrationPhase Phase { get; private set; } = CalibrationPhase.Countdown;

    public int StepIndex { get; private set; }

    public GestureType CurrentGesture => _plan.Gesture(StepIndex);

    public int Round => _plan.Round(StepIndex);

    public int StepInRound => _plan.StepInRound(StepIndex);

    public int StepCount => _plan.Count;

    public int StepNumber => Math.Min(StepIndex + 1, _plan.Count);

    public bool IsRecording => Phase == CalibrationPhase.Prompt;

    public bool IsSampling => Phase == CalibrationPhase.Prompt || Phase == CalibrationPhase.Rest;

    public bool IsFinished => Phase == CalibrationPhase.Finished;

    public float SecondsLeft => Math.Max(0f, PhaseDuration - _elapsed);

    public float PhaseElapsed => _elapsed;

    private float PhaseDuration
    {
        get
        {
            switch (Phase)
            {
                case CalibrationPhase.Countdown: return _countdownDuration;
                case CalibrationPhase.Prompt: return _promptDuration;
                case CalibrationPhase.Rest: return _restDuration;
                default: return 0f;
            }
        }
    }

    public void Tick(float deltaTime)
    {
        if (Phase == CalibrationPhase.Finished) return;

        _elapsed += Math.Max(0f, deltaTime);

        while (Phase != CalibrationPhase.Finished && _elapsed >= PhaseDuration)
        {
            _elapsed -= PhaseDuration;
            Advance();
        }
    }

    private void Advance()
    {
        switch (Phase)
        {
            case CalibrationPhase.Countdown:
                Phase = CalibrationPhase.Prompt;
                return;

            case CalibrationPhase.Prompt:
                Phase = CalibrationPhase.Rest;
                return;

            case CalibrationPhase.Rest:
                StepIndex++;
                Phase = StepIndex >= _plan.Count ? CalibrationPhase.Finished : CalibrationPhase.Prompt;
                return;
        }
    }
}
