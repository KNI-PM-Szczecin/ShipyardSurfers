using System.Collections.Generic;

public class GestureRecognizer
{
    private class ChannelState
    {
        public bool Armed;
        public float CooldownUntil;
        public int NeutralFrames;
    }

    private readonly IGestureRule[] _rules;
    private readonly GestureThresholds _thresholds;
    private readonly PoseHistory _history;
    private readonly Dictionary<GestureChannel, ChannelState> _channels = new Dictionary<GestureChannel, ChannelState>
    {
        { GestureChannel.Vertical, new ChannelState() },
        { GestureChannel.Horizontal, new ChannelState() }
    };

    private float _lastFireTime = float.NegativeInfinity;
    private float _enabledAfter;
    private bool _hasSample;

    public GestureRecognizer(IGestureRule[] rules, GestureThresholds thresholds, PoseHistory history)
    {
        _rules = rules;
        _thresholds = thresholds;
        _history = history;
    }

    public static IGestureRule[] DefaultRules()
    {
        return new IGestureRule[]
        {
            new JumpRule(),
            new RollRule(),
            new LaneSwipeRule(GestureType.LaneLeft),
            new LaneSwipeRule(GestureType.LaneRight)
        };
    }

    public bool IsArmed(GestureChannel channel) => _channels[channel].Armed;

    public void Reset()
    {
        _history.Clear();
        _hasSample = false;
        _lastFireTime = float.NegativeInfinity;

        foreach (ChannelState state in _channels.Values)
        {
            state.Armed = false;
            state.CooldownUntil = 0f;
            state.NeutralFrames = 0;
        }
    }

    public bool TryRecognize(NormalizedPose pose, out GestureType gesture)
    {
        gesture = default;
        if (!pose.IsValid) return false;

        if (!_hasSample)
        {
            _hasSample = true;
            _enabledAfter = pose.Timestamp + _thresholds.ArmDelayAfterReset;
        }

        _history.Push(pose);
        UpdateArming(pose);

        if (pose.Timestamp < _enabledAfter) return false;
        if (pose.Timestamp - _lastFireTime < _thresholds.GlobalMinInterval) return false;

        for (int i = 0; i < _rules.Length; i++)
        {
            IGestureRule rule = _rules[i];
            ChannelState state = _channels[rule.Channel];
            if (!state.Armed || pose.Timestamp < state.CooldownUntil) continue;
            if (!rule.ShouldFire(_history, _thresholds)) continue;

            Fire(rule, state, pose.Timestamp);
            gesture = rule.Type;
            return true;
        }

        return false;
    }

    private void UpdateArming(NormalizedPose pose)
    {
        bool neutral = NeutralPose.IsNeutral(pose, _thresholds);

        foreach (ChannelState state in _channels.Values)
        {
            if (state.Armed) continue;

            state.NeutralFrames = neutral ? state.NeutralFrames + 1 : 0;
            if (state.NeutralFrames >= _thresholds.NeutralFramesToArm)
            {
                state.Armed = true;
            }
        }
    }

    private void Fire(IGestureRule rule, ChannelState state, float time)
    {
        state.Armed = false;
        state.NeutralFrames = 0;
        state.CooldownUntil = time + (rule.Channel == GestureChannel.Vertical ? _thresholds.VerticalCooldown : _thresholds.HorizontalCooldown);
        _lastFireTime = time;
    }
}
