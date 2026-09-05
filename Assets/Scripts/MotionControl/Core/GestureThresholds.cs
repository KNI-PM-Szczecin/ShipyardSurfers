using System;

[Serializable]
public class GestureThresholds
{
    public float VelocityWindow = 0.3f;
    public int ConfirmFrames = 2;

    public float JumpWristHeight = 0f;
    public float JumpMinVelocity = 1.5f;

    public float RollWristDropBelowElbows = 0.4f;
    public float RollMinVelocity = 1.8f;
    public float RollReadyLookback = 0.6f;

    public float LaneWindow = 0.3f;
    public float LaneMinDisplacement = 0.4f;
    public float LaneMinForearmExtension = 0.25f;
    public float LaneMinVelocity = 1.2f;
    public float LaneMaxVerticalRatio = 0.9f;
    public float LaneMaxShoulderShiftRatio = 0.35f;

    public float NeutralMaxWristX = 0.9f;
    public float NeutralMinWristY = -1.35f;
    public float NeutralMaxWristY = 0.1f;
    public int NeutralFramesToArm = 2;

    public float VerticalCooldown = 0.5f;
    public float HorizontalCooldown = 0.4f;
    public float GlobalMinInterval = 0.3f;
    public float ArmDelayAfterReset = 0.3f;
}
