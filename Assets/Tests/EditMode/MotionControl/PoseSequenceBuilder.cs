using System.Collections.Generic;
using UnityEngine;

public class PoseSequenceBuilder
{
    public const float FRAME_INTERVAL = 1f / 30f;
    public const float DEFAULT_HIP_LINE = -1.25f;
    public const float DEFAULT_SHOULDER_WIDTH = 0.25f;

    private readonly List<NormalizedPose> _frames = new List<NormalizedPose>();
    private float _time;
    private Vector2 _shoulderCenter = new Vector2(0.5f, 0.4f);

    public IReadOnlyList<NormalizedPose> Frames => _frames;

    public PoseSequenceBuilder WithShoulderCenter(Vector2 center)
    {
        _shoulderCenter = center;
        return this;
    }

    public PoseSequenceBuilder Hold(Vector2 leftWrist, Vector2 rightWrist, float duration)
    {
        int frames = Mathf.Max(1, Mathf.RoundToInt(duration / FRAME_INTERVAL));
        for (int i = 0; i < frames; i++) Add(leftWrist, rightWrist);
        return this;
    }

    public PoseSequenceBuilder Move(Vector2 leftFrom, Vector2 rightFrom, Vector2 leftTo, Vector2 rightTo, float duration)
    {
        int frames = Mathf.Max(1, Mathf.RoundToInt(duration / FRAME_INTERVAL));
        for (int i = 1; i <= frames; i++)
        {
            float t = (float)i / frames;
            Add(Vector2.Lerp(leftFrom, leftTo, t), Vector2.Lerp(rightFrom, rightTo, t));
        }
        return this;
    }

    public PoseSequenceBuilder MoveWithBody(Vector2 leftFrom, Vector2 rightFrom, Vector2 leftTo, Vector2 rightTo, Vector2 shoulderDelta, float duration)
    {
        Vector2 shoulderStart = _shoulderCenter;
        int frames = Mathf.Max(1, Mathf.RoundToInt(duration / FRAME_INTERVAL));
        for (int i = 1; i <= frames; i++)
        {
            float t = (float)i / frames;
            _shoulderCenter = shoulderStart + shoulderDelta * t;
            Add(Vector2.Lerp(leftFrom, leftTo, t), Vector2.Lerp(rightFrom, rightTo, t));
        }
        return this;
    }

    public static NormalizedPose Create(Vector2 leftWrist, Vector2 rightWrist, float time, Vector2 shoulderCenter, float hipLine = DEFAULT_HIP_LINE)
    {
        var pose = new NormalizedPose
        {
            IsValid = true,
            Timestamp = time,
            ShoulderCenter = shoulderCenter,
            ShoulderWidth = DEFAULT_SHOULDER_WIDTH,
            HipLine = hipLine
        };

        for (int i = 0; i < KeypointIndex.COUNT; i++) pose.Confidences[i] = 0.9f;

        pose.Points[KeypointIndex.LEFT_SHOULDER] = new Vector2(-0.5f, 0f);
        pose.Points[KeypointIndex.RIGHT_SHOULDER] = new Vector2(0.5f, 0f);
        pose.Points[KeypointIndex.LEFT_HIP] = new Vector2(-0.35f, hipLine);
        pose.Points[KeypointIndex.RIGHT_HIP] = new Vector2(0.35f, hipLine);
        pose.Points[KeypointIndex.LEFT_ELBOW] = ElbowFor(pose.Points[KeypointIndex.LEFT_SHOULDER], leftWrist);
        pose.Points[KeypointIndex.RIGHT_ELBOW] = ElbowFor(pose.Points[KeypointIndex.RIGHT_SHOULDER], rightWrist);
        pose.Points[KeypointIndex.LEFT_WRIST] = leftWrist;
        pose.Points[KeypointIndex.RIGHT_WRIST] = rightWrist;
        return pose;
    }

    private static Vector2 ElbowFor(Vector2 shoulder, Vector2 wrist)
    {
        bool armExtended = Mathf.Abs(wrist.y) > 0.9f;
        if (armExtended)
        {
            return Vector2.Lerp(shoulder, wrist, 0.5f);
        }

        return new Vector2(Mathf.Lerp(shoulder.x, wrist.x, 0.1f), wrist.y - 0.4f);
    }

    private void Add(Vector2 leftWrist, Vector2 rightWrist)
    {
        _time += FRAME_INTERVAL;
        _frames.Add(Create(leftWrist, rightWrist, _time, _shoulderCenter));
    }
}
