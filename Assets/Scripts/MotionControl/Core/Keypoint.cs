using UnityEngine;

public struct Keypoint
{
    public Vector2 Position;
    public float Confidence;

    public Keypoint(Vector2 position, float confidence)
    {
        Position = position;
        Confidence = confidence;
    }
}
