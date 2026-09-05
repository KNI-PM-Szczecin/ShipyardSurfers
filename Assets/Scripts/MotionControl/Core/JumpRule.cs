using UnityEngine;

public class JumpRule : IGestureRule
{
    private GestureThresholds _thresholds;

    public GestureType Type => GestureType.Jump;
    public GestureChannel Channel => GestureChannel.Vertical;

    public bool ShouldFire(PoseHistory history, GestureThresholds thresholds)
    {
        _thresholds = thresholds;

        if (!history.LatestConsecutive(thresholds.ConfirmFrames, BothWristsRaised)) return false;

        float leftVelocity = history.PeakVelocity(KeypointIndex.LEFT_WRIST, thresholds.VelocityWindow, Vector2.up);
        float rightVelocity = history.PeakVelocity(KeypointIndex.RIGHT_WRIST, thresholds.VelocityWindow, Vector2.up);
        return leftVelocity >= thresholds.JumpMinVelocity && rightVelocity >= thresholds.JumpMinVelocity;
    }

    private bool BothWristsRaised(NormalizedPose pose)
    {
        return pose.LeftWrist.y >= _thresholds.JumpWristHeight && pose.RightWrist.y >= _thresholds.JumpWristHeight;
    }
}
