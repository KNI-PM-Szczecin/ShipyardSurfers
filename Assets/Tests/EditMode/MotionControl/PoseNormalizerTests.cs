using NUnit.Framework;
using UnityEngine;

public class PoseNormalizerTests
{
    private const int WIDTH = 320;
    private const int HEIGHT = 320;

    private static PoseFrame BuildFrame(float confidence = 0.9f, float shoulderWidth = 80f, float hipConfidence = 0.9f)
    {
        var frame = new PoseFrame();
        frame.Clear(1f, WIDTH, HEIGHT);
        frame.HasPerson = true;
        frame.Confidence = 0.9f;

        Vector2 center = new Vector2(160f, 100f);
        for (int i = 0; i < KeypointIndex.COUNT; i++) frame.Keypoints[i] = new Keypoint(center, confidence);

        frame.Keypoints[KeypointIndex.LEFT_SHOULDER] = new Keypoint(center + new Vector2(shoulderWidth * 0.5f, 0f), confidence);
        frame.Keypoints[KeypointIndex.RIGHT_SHOULDER] = new Keypoint(center - new Vector2(shoulderWidth * 0.5f, 0f), confidence);
        frame.Keypoints[KeypointIndex.LEFT_HIP] = new Keypoint(center + new Vector2(30f, 100f), hipConfidence);
        frame.Keypoints[KeypointIndex.RIGHT_HIP] = new Keypoint(center + new Vector2(-30f, 100f), hipConfidence);
        frame.Keypoints[KeypointIndex.LEFT_WRIST] = new Keypoint(center + new Vector2(80f, -40f), confidence);
        frame.Keypoints[KeypointIndex.RIGHT_WRIST] = new Keypoint(center + new Vector2(-80f, 60f), confidence);
        return frame;
    }

    [Test]
    public void NormalizesRelativeToShouldersWithYUp()
    {
        var normalizer = new PoseNormalizer(new PoseValidityThresholds());
        var pose = new NormalizedPose();

        Assert.That(normalizer.TryNormalize(BuildFrame(), pose), Is.True);
        Assert.That(pose.IsValid, Is.True);
        Assert.That(pose.LeftWrist.x, Is.EqualTo(-1f).Within(0.0001f));
        Assert.That(pose.LeftWrist.y, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(pose.HipLine, Is.EqualTo(-1.25f).Within(0.0001f));
        Assert.That(pose.ShoulderCenter, Is.EqualTo(new Vector2(0.5f, 100f / HEIGHT)));
        Assert.That(pose.ShoulderWidth, Is.EqualTo(0.25f).Within(0.0001f));
    }

    [Test]
    public void HiddenHipsFallBackToDefaultHipLine()
    {
        var normalizer = new PoseNormalizer(new PoseValidityThresholds());
        var pose = new NormalizedPose();

        Assert.That(normalizer.TryNormalize(BuildFrame(hipConfidence: 0.1f), pose), Is.True);
        Assert.That(pose.HipLine, Is.EqualTo(PoseNormalizer.DEFAULT_HIP_LINE));
    }

    [Test]
    public void MapsCameraAxisIntoBodySpaceSoOutwardIsAlwaysPositive()
    {
        var normalizer = new PoseNormalizer(new PoseValidityThresholds());
        var pose = new NormalizedPose();

        normalizer.TryNormalize(BuildFrame(), pose);

        Assert.That(pose.LeftWrist.x, Is.EqualTo(-1f).Within(0.0001f));
        Assert.That(pose.RightWrist.x, Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void RejectsLowConfidenceKeypoints()
    {
        var normalizer = new PoseNormalizer(new PoseValidityThresholds());
        var pose = new NormalizedPose();

        Assert.That(normalizer.TryNormalize(BuildFrame(confidence: 0.2f), pose), Is.False);
        Assert.That(pose.IsValid, Is.False);
    }

    [Test]
    public void RejectsTooSmallPerson()
    {
        var normalizer = new PoseNormalizer(new PoseValidityThresholds());
        var pose = new NormalizedPose();

        Assert.That(normalizer.TryNormalize(BuildFrame(shoulderWidth: 20f), pose), Is.False);
    }

    [Test]
    public void RejectsSuddenShoulderWidthChangeOnce()
    {
        var normalizer = new PoseNormalizer(new PoseValidityThresholds());
        var pose = new NormalizedPose();

        Assert.That(normalizer.TryNormalize(BuildFrame(shoulderWidth: 80f), pose), Is.True);
        Assert.That(normalizer.TryNormalize(BuildFrame(shoulderWidth: 140f), pose), Is.False);
        Assert.That(normalizer.TryNormalize(BuildFrame(shoulderWidth: 140f), pose), Is.True);
    }
}
