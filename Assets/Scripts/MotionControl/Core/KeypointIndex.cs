public static class KeypointIndex
{
    public const int COUNT = 17;

    public const int NOSE = 0;
    public const int LEFT_EYE = 1;
    public const int RIGHT_EYE = 2;
    public const int LEFT_EAR = 3;
    public const int RIGHT_EAR = 4;
    public const int LEFT_SHOULDER = 5;
    public const int RIGHT_SHOULDER = 6;
    public const int LEFT_ELBOW = 7;
    public const int RIGHT_ELBOW = 8;
    public const int LEFT_WRIST = 9;
    public const int RIGHT_WRIST = 10;
    public const int LEFT_HIP = 11;
    public const int RIGHT_HIP = 12;
    public const int LEFT_KNEE = 13;
    public const int RIGHT_KNEE = 14;
    public const int LEFT_ANKLE = 15;
    public const int RIGHT_ANKLE = 16;

    public static readonly string[] Names =
    {
        "nose", "left_eye", "right_eye", "left_ear", "right_ear",
        "left_shoulder", "right_shoulder", "left_elbow", "right_elbow",
        "left_wrist", "right_wrist", "left_hip", "right_hip",
        "left_knee", "right_knee", "left_ankle", "right_ankle"
    };

    public static readonly int[] UpperBody =
    {
        LEFT_SHOULDER, RIGHT_SHOULDER, LEFT_ELBOW, RIGHT_ELBOW, LEFT_WRIST, RIGHT_WRIST, LEFT_HIP, RIGHT_HIP
    };
}
