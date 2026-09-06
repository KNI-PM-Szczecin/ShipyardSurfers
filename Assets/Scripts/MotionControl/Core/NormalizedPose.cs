using UnityEngine;

public class NormalizedPose
{
    public readonly Vector2[] Points = new Vector2[KeypointIndex.COUNT];
    public readonly float[] Confidences = new float[KeypointIndex.COUNT];

    public bool IsValid;
    public float Timestamp;
    public Vector2 ShoulderCenter;
    public float ShoulderWidth;
    public float HipLine;

    public Vector2 LeftWrist => Points[KeypointIndex.LEFT_WRIST];
    public Vector2 RightWrist => Points[KeypointIndex.RIGHT_WRIST];
    public Vector2 LeftElbow => Points[KeypointIndex.LEFT_ELBOW];
    public Vector2 RightElbow => Points[KeypointIndex.RIGHT_ELBOW];

    public void CopyFrom(NormalizedPose other)
    {
        for (int i = 0; i < KeypointIndex.COUNT; i++)
        {
            Points[i] = other.Points[i];
            Confidences[i] = other.Confidences[i];
        }

        IsValid = other.IsValid;
        Timestamp = other.Timestamp;
        ShoulderCenter = other.ShoulderCenter;
        ShoulderWidth = other.ShoulderWidth;
        HipLine = other.HipLine;
    }
}
