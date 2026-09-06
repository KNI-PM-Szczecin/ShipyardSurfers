using UnityEngine;

public class PoseNormalizer
{
    public const float DEFAULT_HIP_LINE = -1.25f;

    private readonly PoseValidityThresholds _thresholds;
    private float _previousShoulderWidth = -1f;

    public bool Mirror { get; set; } = true;

    public PoseNormalizer(PoseValidityThresholds thresholds)
    {
        _thresholds = thresholds;
    }

    public void Reset()
    {
        _previousShoulderWidth = -1f;
    }

    public bool TryNormalize(PoseFrame frame, NormalizedPose target)
    {
        target.IsValid = false;
        target.Timestamp = frame.Timestamp;

        if (!frame.HasPerson || frame.Confidence < _thresholds.MinPersonConfidence)
        {
            return false;
        }

        Keypoint leftShoulder = frame.Keypoints[KeypointIndex.LEFT_SHOULDER];
        Keypoint rightShoulder = frame.Keypoints[KeypointIndex.RIGHT_SHOULDER];
        if (!MeetsConfidence(leftShoulder, rightShoulder, _thresholds.MinShoulderConfidence)) return false;
        if (!MeetsConfidence(frame.Keypoints[KeypointIndex.LEFT_ELBOW], frame.Keypoints[KeypointIndex.RIGHT_ELBOW], _thresholds.MinElbowConfidence)) return false;
        if (!MeetsConfidence(frame.Keypoints[KeypointIndex.LEFT_WRIST], frame.Keypoints[KeypointIndex.RIGHT_WRIST], _thresholds.MinWristConfidence)) return false;
        bool hipsVisible = MeetsConfidence(frame.Keypoints[KeypointIndex.LEFT_HIP], frame.Keypoints[KeypointIndex.RIGHT_HIP], _thresholds.MinHipConfidence);

        float shoulderWidth = Vector2.Distance(leftShoulder.Position, rightShoulder.Position);
        if (shoulderWidth < _thresholds.MinShoulderWidthFraction * frame.ImageWidth)
        {
            return false;
        }

        if (_previousShoulderWidth > 0f && Mathf.Abs(shoulderWidth - _previousShoulderWidth) > _thresholds.MaxShoulderWidthChange * _previousShoulderWidth)
        {
            _previousShoulderWidth = shoulderWidth;
            return false;
        }

        _previousShoulderWidth = shoulderWidth;

        Vector2 center = (leftShoulder.Position + rightShoulder.Position) * 0.5f;
        float xSign = Mirror ? -1f : 1f;

        for (int i = 0; i < KeypointIndex.COUNT; i++)
        {
            Vector2 delta = frame.Keypoints[i].Position - center;
            target.Points[i] = new Vector2(delta.x * xSign / shoulderWidth, -delta.y / shoulderWidth);
            target.Confidences[i] = frame.Keypoints[i].Confidence;
        }

        target.ShoulderCenter = new Vector2(center.x / frame.ImageWidth, center.y / frame.ImageHeight);
        target.ShoulderWidth = shoulderWidth / frame.ImageWidth;
        target.HipLine = hipsVisible
            ? (target.Points[KeypointIndex.LEFT_HIP].y + target.Points[KeypointIndex.RIGHT_HIP].y) * 0.5f
            : DEFAULT_HIP_LINE;
        target.IsValid = true;
        return true;
    }

    private static bool MeetsConfidence(Keypoint a, Keypoint b, float min)
    {
        return a.Confidence >= min && b.Confidence >= min;
    }
}
