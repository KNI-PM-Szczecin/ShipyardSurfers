using UnityEngine;

public class LaneSwipeRule : IGestureRule
{
    private readonly int _wrist;
    private readonly int _elbow;
    private readonly float _sign;

    public GestureType Type { get; }
    public GestureChannel Channel => GestureChannel.Horizontal;

    public LaneSwipeRule(GestureType type)
    {
        Type = type;
        bool right = type == GestureType.LaneRight;
        _wrist = right ? KeypointIndex.RIGHT_WRIST : KeypointIndex.LEFT_WRIST;
        _elbow = right ? KeypointIndex.RIGHT_ELBOW : KeypointIndex.LEFT_ELBOW;
        _sign = right ? 1f : -1f;
    }

    public bool ShouldFire(PoseHistory history, GestureThresholds thresholds)
    {
        if (!history.TryGetDisplacement(_wrist, thresholds.LaneWindow, out Vector2 wristMotion)) return false;

        float outward = wristMotion.x * _sign;
        if (outward < thresholds.LaneMinDisplacement) return false;
        if (Mathf.Abs(wristMotion.y) > thresholds.LaneMaxVerticalRatio * outward) return false;
        if (!ForearmPointsOutward(history.Latest, thresholds)) return false;
        if (ShoulderShiftedTooFar(history, thresholds, Mathf.Abs(wristMotion.x))) return false;

        float velocity = history.PeakVelocity(_wrist, thresholds.LaneWindow, new Vector2(_sign, 0f));
        return velocity >= thresholds.LaneMinVelocity;
    }

    private bool ForearmPointsOutward(NormalizedPose pose, GestureThresholds thresholds)
    {
        float extension = (pose.Points[_wrist].x - pose.Points[_elbow].x) * _sign;
        return extension >= thresholds.LaneMinForearmExtension;
    }

    private static bool ShoulderShiftedTooFar(PoseHistory history, GestureThresholds thresholds, float wristSwing)
    {
        NormalizedPose latest = history.Latest;
        NormalizedPose past = history.SampleAtOrBefore(latest.Timestamp - thresholds.LaneWindow);
        if (past == null) return true;

        float shiftInShoulderWidths = Mathf.Abs(latest.ShoulderCenter.x - past.ShoulderCenter.x) / Mathf.Max(latest.ShoulderWidth, 0.0001f);
        return shiftInShoulderWidths > thresholds.LaneMaxShoulderShiftRatio * wristSwing;
    }
}
