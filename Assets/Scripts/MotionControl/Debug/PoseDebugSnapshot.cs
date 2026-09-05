using UnityEngine;

public class PoseDebugSnapshot
{
    public readonly Vector2[] Keypoints = new Vector2[KeypointIndex.COUNT];
    public readonly float[] Confidences = new float[KeypointIndex.COUNT];

    public TrackingState State;
    public bool HasPerson;
    public float PersonConfidence;
    public Rect BoundingBox;
    public bool PoseValid;
    public bool ArmedVertical;
    public bool ArmedHorizontal;
    public bool Mirror;
    public float PoseFps;
    public float InferenceMs;
    public float Now;
    public string LastGesture = string.Empty;
    public float LastGestureTime = -1f;
}
