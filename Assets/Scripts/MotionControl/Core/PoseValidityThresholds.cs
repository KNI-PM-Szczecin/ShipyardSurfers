using System;

[Serializable]
public class PoseValidityThresholds
{
    public float MinPersonConfidence = 0.5f;
    public float MinShoulderConfidence = 0.5f;
    public float MinElbowConfidence = 0.4f;
    public float MinWristConfidence = 0.4f;
    public float MinHipConfidence = 0.4f;
    public float MinShoulderWidthFraction = 0.12f;
    public float MaxShoulderWidthChange = 0.25f;
}
