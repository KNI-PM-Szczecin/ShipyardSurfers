using NUnit.Framework;
using UnityEngine;

public class YoloPoseDecoderTests
{
    private const int ANCHORS = 4;

    private static float[] BuildOutput(float[] confidences)
    {
        var output = new float[YoloPoseDecoder.VALUES_PER_ANCHOR * ANCHORS];
        for (int a = 0; a < ANCHORS; a++)
        {
            output[0 * ANCHORS + a] = 100f + a;
            output[1 * ANCHORS + a] = 200f + a;
            output[2 * ANCHORS + a] = 50f;
            output[3 * ANCHORS + a] = 120f;
            output[4 * ANCHORS + a] = confidences[a];
            for (int k = 0; k < KeypointIndex.COUNT; k++)
            {
                int row = 5 + k * 3;
                output[row * ANCHORS + a] = 10f * k + a;
                output[(row + 1) * ANCHORS + a] = 20f * k + a;
                output[(row + 2) * ANCHORS + a] = 0.5f + 0.1f * a;
            }
        }
        return output;
    }

    [Test]
    public void PicksAnchorWithHighestConfidence()
    {
        var decoder = new YoloPoseDecoder(0.5f);
        var frame = new PoseFrame();
        float[] output = BuildOutput(new[] { 0.6f, 0.95f, 0.7f, 0.1f });

        bool decoded = decoder.TryDecode(output, ANCHORS, 320, 320, 1f, frame);

        Assert.That(decoded, Is.True);
        Assert.That(frame.HasPerson, Is.True);
        Assert.That(frame.Confidence, Is.EqualTo(0.95f));
        Assert.That(frame.BoundingBox.center, Is.EqualTo(new Vector2(101f, 201f)));
        Assert.That(frame.BoundingBox.size, Is.EqualTo(new Vector2(50f, 120f)));
        Assert.That(frame.Keypoints[KeypointIndex.LEFT_WRIST].Position, Is.EqualTo(new Vector2(91f, 181f)));
        Assert.That(frame.Keypoints[KeypointIndex.LEFT_WRIST].Confidence, Is.EqualTo(0.6f).Within(0.0001f));
    }

    [Test]
    public void ReturnsFalseWhenNothingPassesThreshold()
    {
        var decoder = new YoloPoseDecoder(0.5f);
        var frame = new PoseFrame();
        float[] output = BuildOutput(new[] { 0.1f, 0.2f, 0.3f, 0.49f });

        Assert.That(decoder.TryDecode(output, ANCHORS, 320, 320, 1f, frame), Is.False);
        Assert.That(frame.HasPerson, Is.False);
        Assert.That(frame.Timestamp, Is.EqualTo(1f));
    }

    [Test]
    public void ReturnsFalseForMalformedOutput()
    {
        var decoder = new YoloPoseDecoder(0.5f);
        var frame = new PoseFrame();

        Assert.That(decoder.TryDecode(new float[10], ANCHORS, 320, 320, 1f, frame), Is.False);
        Assert.That(decoder.TryDecode(null, ANCHORS, 320, 320, 1f, frame), Is.False);
    }
}
