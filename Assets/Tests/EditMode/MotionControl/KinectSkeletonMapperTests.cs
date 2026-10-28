using NUnit.Framework;
using UnityEngine;

public class KinectSkeletonMapperTests
{
    private const int WIDTH = 640;
    private const int HEIGHT = 480;
    private const float FOCAL = 531.15f;
    private const float DEPTH = 2.5f;

    // A player facing the sensor: skeleton +x points to the sensor's left, which is the player's right.
    private static Vector3[] StandingSkeleton()
    {
        var joints = new Vector3[KinectSkeletonMapper.JOINT_COUNT];
        for (int i = 0; i < joints.Length; i++) joints[i] = new Vector3(0f, 0f, DEPTH);

        joints[KinectSkeletonMapper.HEAD] = new Vector3(0f, 0.6f, DEPTH);
        joints[KinectSkeletonMapper.SHOULDER_LEFT] = new Vector3(-0.18f, 0.4f, DEPTH);
        joints[KinectSkeletonMapper.SHOULDER_RIGHT] = new Vector3(0.18f, 0.4f, DEPTH);
        joints[KinectSkeletonMapper.ELBOW_LEFT] = new Vector3(-0.25f, 0.15f, DEPTH);
        joints[KinectSkeletonMapper.ELBOW_RIGHT] = new Vector3(0.25f, 0.15f, DEPTH);
        joints[KinectSkeletonMapper.WRIST_LEFT] = new Vector3(-0.28f, -0.1f, DEPTH);
        joints[KinectSkeletonMapper.WRIST_RIGHT] = new Vector3(0.55f, 0.4f, DEPTH);
        joints[KinectSkeletonMapper.HIP_LEFT] = new Vector3(-0.1f, -0.15f, DEPTH);
        joints[KinectSkeletonMapper.HIP_RIGHT] = new Vector3(0.1f, -0.15f, DEPTH);
        return joints;
    }

    private static int[] States(int state)
    {
        var states = new int[KinectSkeletonMapper.JOINT_COUNT];
        for (int i = 0; i < states.Length; i++) states[i] = state;
        return states;
    }

    [Test]
    public void PlayersLeftShoulderLandsOnImageRightLikeAnUnmirroredCamera()
    {
        var frame = new PoseFrame();
        KinectSkeletonMapper.Map(StandingSkeleton(), States(KinectSkeletonMapper.JOINT_TRACKED), 1f, WIDTH, HEIGHT, FOCAL, frame);

        Assert.That(frame.HasPerson, Is.True);
        Assert.That(frame.Keypoints[KeypointIndex.LEFT_SHOULDER].Position.x, Is.GreaterThan(WIDTH * 0.5f));
        Assert.That(frame.Keypoints[KeypointIndex.RIGHT_SHOULDER].Position.x, Is.LessThan(WIDTH * 0.5f));
        Assert.That(frame.Keypoints[KeypointIndex.NOSE].Position.y, Is.LessThan(frame.Keypoints[KeypointIndex.LEFT_SHOULDER].Position.y));
    }

    [Test]
    public void ProjectsWithPinholeModel()
    {
        Vector2 point = KinectSkeletonMapper.Project(new Vector3(0.5f, 0.25f, 2f), WIDTH, HEIGHT, FOCAL);

        Assert.That(point.x, Is.EqualTo(320f - 0.5f * FOCAL / 2f).Within(0.001f));
        Assert.That(point.y, Is.EqualTo(240f - 0.25f * FOCAL / 2f).Within(0.001f));
    }

    [Test]
    public void ConfidenceFollowsJointTrackingState()
    {
        int[] states = States(KinectSkeletonMapper.JOINT_TRACKED);
        states[KinectSkeletonMapper.WRIST_LEFT] = KinectSkeletonMapper.JOINT_INFERRED;
        states[KinectSkeletonMapper.KNEE_RIGHT] = KinectSkeletonMapper.JOINT_NOT_TRACKED;

        var frame = new PoseFrame();
        KinectSkeletonMapper.Map(StandingSkeleton(), states, 1f, WIDTH, HEIGHT, FOCAL, frame);

        Assert.That(frame.Keypoints[KeypointIndex.LEFT_WRIST].Confidence, Is.EqualTo(KinectSkeletonMapper.INFERRED_CONFIDENCE));
        Assert.That(frame.Keypoints[KeypointIndex.RIGHT_KNEE].Confidence, Is.EqualTo(0f));
        Assert.That(frame.Keypoints[KeypointIndex.LEFT_EYE].Confidence, Is.EqualTo(0f));
        Assert.That(frame.Keypoints[KeypointIndex.RIGHT_SHOULDER].Confidence, Is.EqualTo(KinectSkeletonMapper.TRACKED_CONFIDENCE));
    }

    [Test]
    public void NormalizedPoseKeepsBodySidesForGestureRules()
    {
        var frame = new PoseFrame();
        KinectSkeletonMapper.Map(StandingSkeleton(), States(KinectSkeletonMapper.JOINT_TRACKED), 1f, WIDTH, HEIGHT, FOCAL, frame);

        var normalizer = new PoseNormalizer(new PoseValidityThresholds { MinShoulderWidthFraction = 0.04f });
        var pose = new NormalizedPose();

        Assert.That(normalizer.TryNormalize(frame, pose), Is.True);
        Assert.That(pose.RightWrist.x, Is.GreaterThan(1f), "the player's right arm stretched out sideways points to body +x");
        Assert.That(pose.RightWrist.y, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(pose.LeftWrist.x, Is.LessThan(0f));
        Assert.That(pose.HipLine, Is.LessThan(0f));
    }
}
