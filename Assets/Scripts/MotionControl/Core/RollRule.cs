using UnityEngine;

public class RollRule : IGestureRule
{
    private GestureThresholds _thresholds;

    public GestureType Type => GestureType.Roll;
    public GestureChannel Channel => GestureChannel.Vertical;

    public bool ShouldFire(PoseHistory history, GestureThresholds thresholds)
    {
        _thresholds = thresholds;

        if (!history.LatestConsecutive(thresholds.ConfirmFrames, BothWristsBelowElbows)) return false;
        if (!history.AnyWithin(thresholds.RollReadyLookback, WasInReadyPose)) return false;

        float leftVelocity = history.PeakVelocity(KeypointIndex.LEFT_WRIST, thresholds.VelocityWindow, Vector2.down);
        float rightVelocity = history.PeakVelocity(KeypointIndex.RIGHT_WRIST, thresholds.VelocityWindow, Vector2.down);
        return leftVelocity >= thresholds.RollMinVelocity && rightVelocity >= thresholds.RollMinVelocity;
    }

    private bool BothWristsBelowElbows(NormalizedPose pose)
    {
        float drop = _thresholds.RollWristDropBelowElbows;
        return pose.LeftWrist.y <= pose.LeftElbow.y - drop && pose.RightWrist.y <= pose.RightElbow.y - drop;
    }

    private bool WasInReadyPose(NormalizedPose pose)
    {
        return NeutralPose.IsNeutral(pose, _thresholds);
    }
}
