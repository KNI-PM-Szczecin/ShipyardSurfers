using UnityEngine;

public class PoseFrame
{
    public readonly Keypoint[] Keypoints = new Keypoint[KeypointIndex.COUNT];

    public bool HasPerson;
    public float Confidence;
    public Rect BoundingBox;
    public float Timestamp;
    public int ImageWidth;
    public int ImageHeight;

    public void Clear(float timestamp, int imageWidth, int imageHeight)
    {
        HasPerson = false;
        Confidence = 0f;
        BoundingBox = default;
        Timestamp = timestamp;
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;
    }
}
