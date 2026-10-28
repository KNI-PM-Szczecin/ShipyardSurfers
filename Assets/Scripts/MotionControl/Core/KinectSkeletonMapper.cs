using UnityEngine;

public static class KinectSkeletonMapper
{
    public const int JOINT_COUNT = 20;

    public const int HIP_CENTER = 0;
    public const int SPINE = 1;
    public const int SHOULDER_CENTER = 2;
    public const int HEAD = 3;
    public const int SHOULDER_LEFT = 4;
    public const int ELBOW_LEFT = 5;
    public const int WRIST_LEFT = 6;
    public const int HAND_LEFT = 7;
    public const int SHOULDER_RIGHT = 8;
    public const int ELBOW_RIGHT = 9;
    public const int WRIST_RIGHT = 10;
    public const int HAND_RIGHT = 11;
    public const int HIP_LEFT = 12;
    public const int KNEE_LEFT = 13;
    public const int ANKLE_LEFT = 14;
    public const int FOOT_LEFT = 15;
    public const int HIP_RIGHT = 16;
    public const int KNEE_RIGHT = 17;
    public const int ANKLE_RIGHT = 18;
    public const int FOOT_RIGHT = 19;

    public const int JOINT_NOT_TRACKED = 0;
    public const int JOINT_INFERRED = 1;
    public const int JOINT_TRACKED = 2;

    public const float TRACKED_CONFIDENCE = 1f;
    public const float INFERRED_CONFIDENCE = 0.5f;

    private const float MIN_DEPTH = 0.1f;

    private static readonly int[] JointForKeypoint =
    {
        HEAD, -1, -1, -1, -1,
        SHOULDER_LEFT, SHOULDER_RIGHT,
        ELBOW_LEFT, ELBOW_RIGHT,
        WRIST_LEFT, WRIST_RIGHT,
        HIP_LEFT, HIP_RIGHT,
        KNEE_LEFT, KNEE_RIGHT,
        ANKLE_LEFT, ANKLE_RIGHT
    };

    // Kinect skeleton space is right-handed with +x towards the sensor's left (the player's right), so the
    // projection flips x to produce what an unmirrored camera sees: the player's left shoulder on the image right,
    // the same layout the YOLO keypoints use.
    public static Vector2 Project(Vector3 skeletonPoint, int imageWidth, int imageHeight, float focalLengthPixels)
    {
        float z = Mathf.Max(skeletonPoint.z, MIN_DEPTH);
        float u = imageWidth * 0.5f - skeletonPoint.x * focalLengthPixels / z;
        float v = imageHeight * 0.5f - skeletonPoint.y * focalLengthPixels / z;
        return new Vector2(u, v);
    }

    public static float ConfidenceOf(int jointTrackingState)
    {
        switch (jointTrackingState)
        {
            case JOINT_TRACKED: return TRACKED_CONFIDENCE;
            case JOINT_INFERRED: return INFERRED_CONFIDENCE;
            default: return 0f;
        }
    }

    public static void Map(Vector3[] joints, int[] jointStates, float timestamp, int imageWidth, int imageHeight, float focalLengthPixels, PoseFrame target)
    {
        target.Clear(timestamp, imageWidth, imageHeight);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        for (int k = 0; k < KeypointIndex.COUNT; k++)
        {
            int joint = JointForKeypoint[k];
            if (joint < 0)
            {
                target.Keypoints[k] = default;
                continue;
            }

            Vector2 position = Project(joints[joint], imageWidth, imageHeight, focalLengthPixels);
            float confidence = ConfidenceOf(jointStates[joint]);
            target.Keypoints[k] = new Keypoint(position, confidence);

            if (confidence <= 0f) continue;
            min = Vector2.Min(min, position);
            max = Vector2.Max(max, position);
        }

        target.HasPerson = true;
        target.Confidence = TRACKED_CONFIDENCE;
        target.BoundingBox = min.x <= max.x ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : default;
    }
}
