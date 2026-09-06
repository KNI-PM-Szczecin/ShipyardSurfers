using UnityEngine;

public static class NeutralPose
{
    public static bool IsNeutral(NormalizedPose pose, GestureThresholds thresholds)
    {
        return IsWristNeutral(pose.LeftWrist, thresholds) && IsWristNeutral(pose.RightWrist, thresholds);
    }

    private static bool IsWristNeutral(Vector2 wrist, GestureThresholds thresholds)
    {
        return Mathf.Abs(wrist.x) <= thresholds.NeutralMaxWristX
               && wrist.y >= thresholds.NeutralMinWristY
               && wrist.y <= thresholds.NeutralMaxWristY;
    }
}
