using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GestureRecognizerTests
{
    private static readonly Vector2 ReadyLeft = new Vector2(-0.35f, -0.55f);
    private static readonly Vector2 ReadyRight = new Vector2(0.35f, -0.55f);
    private static readonly Vector2 HangingLeft = new Vector2(-0.3f, -1.5f);
    private static readonly Vector2 HangingRight = new Vector2(0.3f, -1.5f);
    private static readonly Vector2 RaisedLeft = new Vector2(-0.4f, 0.9f);
    private static readonly Vector2 RaisedRight = new Vector2(0.4f, 0.9f);
    private static readonly Vector2 DroppedLeft = new Vector2(-0.3f, -1.6f);
    private static readonly Vector2 DroppedRight = new Vector2(0.3f, -1.6f);
    private static readonly Vector2 SwungOutLeft = new Vector2(-1.6f, -0.55f);
    private static readonly Vector2 SwungOutRight = new Vector2(1.6f, -0.55f);

    private static GestureRecognizer CreateRecognizer()
    {
        return new GestureRecognizer(GestureRecognizer.DefaultRules(), new GestureThresholds(), new PoseHistory(64));
    }

    private static List<GestureType> Run(GestureRecognizer recognizer, IReadOnlyList<NormalizedPose> frames)
    {
        var fired = new List<GestureType>();
        foreach (NormalizedPose frame in frames)
        {
            if (recognizer.TryRecognize(frame, out GestureType gesture)) fired.Add(gesture);
        }
        return fired;
    }

    private static PoseSequenceBuilder Ready()
    {
        return new PoseSequenceBuilder().Hold(ReadyLeft, ReadyRight, 0.5f);
    }

    [Test]
    public void RestingWithHangingArms_NeverFires()
    {
        var frames = new PoseSequenceBuilder().Hold(HangingLeft, HangingRight, 10f).Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.Empty);
    }

    [Test]
    public void HoldingReadyPose_DoesNotFire()
    {
        var frames = new PoseSequenceBuilder().Hold(ReadyLeft, ReadyRight, 10f).Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.Empty);
    }

    [Test]
    public void RaisingBothHands_FiresJumpExactlyOnce()
    {
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, RaisedLeft, RaisedRight, 0.2f)
            .Hold(RaisedLeft, RaisedRight, 1.5f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.EqualTo(new[] { GestureType.Jump }));
    }

    [Test]
    public void RaisingHandsToShoulderHeight_FiresJump()
    {
        Vector2 leftTo = new Vector2(-0.4f, 0.05f);
        Vector2 rightTo = new Vector2(0.4f, 0.05f);
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, leftTo, rightTo, 0.25f)
            .Hold(leftTo, rightTo, 1f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.EqualTo(new[] { GestureType.Jump }));
    }

    [Test]
    public void RaisingHandsOnlyToChestTop_DoesNotFire()
    {
        Vector2 leftTo = new Vector2(-0.4f, -0.3f);
        Vector2 rightTo = new Vector2(0.4f, -0.3f);
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, leftTo, rightTo, 0.1f)
            .Hold(leftTo, rightTo, 1f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.Empty);
    }

    [Test]
    public void SlowlyRaisingHands_DoesNotFire()
    {
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, RaisedLeft, RaisedRight, 2f)
            .Hold(RaisedLeft, RaisedRight, 0.5f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.Empty);
    }

    [Test]
    public void JumpThenReturnToReady_CanJumpAgain()
    {
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, RaisedLeft, RaisedRight, 0.2f)
            .Hold(RaisedLeft, RaisedRight, 0.5f)
            .Move(RaisedLeft, RaisedRight, ReadyLeft, ReadyRight, 0.3f)
            .Hold(ReadyLeft, ReadyRight, 0.5f)
            .Move(ReadyLeft, ReadyRight, RaisedLeft, RaisedRight, 0.2f)
            .Hold(RaisedLeft, RaisedRight, 0.5f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.EqualTo(new[] { GestureType.Jump, GestureType.Jump }));
    }

    [Test]
    public void DroppingHandsBelowElbowsFromReadyPose_FiresRoll()
    {
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, DroppedLeft, DroppedRight, 0.2f)
            .Hold(DroppedLeft, DroppedRight, 1f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.EqualTo(new[] { GestureType.Roll }));
    }

    [Test]
    public void DroppingHandsFromHangingPose_DoesNotRoll()
    {
        var frames = new PoseSequenceBuilder()
            .Hold(HangingLeft, HangingRight, 0.5f)
            .Move(HangingLeft, HangingRight, DroppedLeft, DroppedRight, 0.1f)
            .Hold(DroppedLeft, DroppedRight, 1f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.Empty);
    }

    [Test]
    public void LoweringHandsSlightlyBelowReady_DoesNotRoll()
    {
        Vector2 leftTo = ReadyLeft + new Vector2(0f, -0.3f);
        Vector2 rightTo = ReadyRight + new Vector2(0f, -0.3f);
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, leftTo, rightTo, 0.1f)
            .Hold(leftTo, rightTo, 1f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.Empty);
    }

    [Test]
    public void RollThenBackToReady_CanRollAgain()
    {
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, DroppedLeft, DroppedRight, 0.2f)
            .Hold(DroppedLeft, DroppedRight, 0.5f)
            .Move(DroppedLeft, DroppedRight, ReadyLeft, ReadyRight, 0.3f)
            .Hold(ReadyLeft, ReadyRight, 0.5f)
            .Move(ReadyLeft, ReadyRight, DroppedLeft, DroppedRight, 0.2f)
            .Hold(DroppedLeft, DroppedRight, 0.5f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.EqualTo(new[] { GestureType.Roll, GestureType.Roll }));
    }

    [Test]
    public void SwingingLeftHandOutward_FiresLaneLeft()
    {
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, SwungOutLeft, ReadyRight, 0.2f)
            .Hold(SwungOutLeft, ReadyRight, 0.5f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.EqualTo(new[] { GestureType.LaneLeft }));
    }

    [Test]
    public void SwingingRightHandOutward_FiresLaneRight()
    {
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, ReadyLeft, SwungOutRight, 0.2f)
            .Hold(ReadyLeft, SwungOutRight, 0.5f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.EqualTo(new[] { GestureType.LaneRight }));
    }

    [Test]
    public void RotatingForearmOutwardWithStillElbow_FiresLaneRight()
    {
        Vector2 rightTo = new Vector2(1.05f, -0.55f);
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, ReadyLeft, rightTo, 0.25f)
            .Hold(ReadyLeft, rightTo, 0.5f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.EqualTo(new[] { GestureType.LaneRight }));
    }

    [Test]
    public void SwingingRightHandOutwardAndSlightlyUp_FiresLaneRight()
    {
        Vector2 rightTo = new Vector2(1.5f, -0.1f);
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, ReadyLeft, rightTo, 0.2f)
            .Hold(ReadyLeft, rightTo, 0.5f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.EqualTo(new[] { GestureType.LaneRight }));
    }

    [Test]
    public void SwingingRightHandOutwardAndSlightlyDown_FiresLaneRight()
    {
        Vector2 rightTo = new Vector2(1.5f, -1.0f);
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, ReadyLeft, rightTo, 0.2f)
            .Hold(ReadyLeft, rightTo, 0.5f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.EqualTo(new[] { GestureType.LaneRight }));
    }

    [Test]
    public void ReturningHandSlightlyHighOrLow_StillRearmsLane()
    {
        Vector2 highReady = new Vector2(0.35f, -0.2f);
        Vector2 lowReady = new Vector2(0.35f, -1.2f);
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, ReadyLeft, SwungOutRight, 0.2f)
            .Hold(ReadyLeft, SwungOutRight, 0.4f)
            .Move(ReadyLeft, SwungOutRight, ReadyLeft, highReady, 0.3f)
            .Hold(ReadyLeft, highReady, 0.5f)
            .Move(ReadyLeft, highReady, ReadyLeft, SwungOutRight, 0.2f)
            .Hold(ReadyLeft, SwungOutRight, 0.4f)
            .Move(ReadyLeft, SwungOutRight, ReadyLeft, lowReady, 0.3f)
            .Hold(ReadyLeft, lowReady, 0.5f)
            .Move(ReadyLeft, lowReady, ReadyLeft, SwungOutRight, 0.2f)
            .Hold(ReadyLeft, SwungOutRight, 0.4f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.EqualTo(new[] { GestureType.LaneRight, GestureType.LaneRight, GestureType.LaneRight }));
    }

    [Test]
    public void SwingingRightHandInward_DoesNotFire()
    {
        Vector2 rightTo = new Vector2(-0.8f, -0.55f);
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, ReadyLeft, rightTo, 0.2f)
            .Hold(ReadyLeft, rightTo, 0.5f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.Empty);
    }

    [Test]
    public void SlowlySwingingHandOutward_DoesNotFire()
    {
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, ReadyLeft, SwungOutRight, 1.5f)
            .Hold(ReadyLeft, SwungOutRight, 0.5f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.Empty);
    }

    [Test]
    public void WholeBodyStepSideways_DoesNotFireLane()
    {
        var frames = Ready()
            .MoveWithBody(ReadyLeft, ReadyRight, ReadyLeft, ReadyRight, new Vector2(0.2f, 0f), 0.2f)
            .Hold(ReadyLeft, ReadyRight, 0.5f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.Empty);
    }

    [Test]
    public void SwingingWhileStepping_DoesNotFireLane()
    {
        var frames = Ready()
            .MoveWithBody(ReadyLeft, ReadyRight, ReadyLeft, SwungOutRight, new Vector2(0.2f, 0f), 0.2f)
            .Hold(ReadyLeft, SwungOutRight, 0.5f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.Empty);
    }

    [Test]
    public void DiagonalRaise_FiresOnlyJump()
    {
        Vector2 leftTo = RaisedLeft + new Vector2(1.2f, 0f);
        Vector2 rightTo = RaisedRight + new Vector2(1.2f, 0f);
        var frames = Ready()
            .Move(ReadyLeft, ReadyRight, leftTo, rightTo, 0.2f)
            .Hold(leftTo, rightTo, 1f)
            .Frames;

        Assert.That(Run(CreateRecognizer(), frames), Is.EqualTo(new[] { GestureType.Jump }));
    }

    [Test]
    public void InvalidPoses_AreIgnored()
    {
        var recognizer = CreateRecognizer();
        NormalizedPose pose = PoseSequenceBuilder.Create(RaisedLeft, RaisedRight, 1f, Vector2.zero);
        pose.IsValid = false;

        Assert.That(recognizer.TryRecognize(pose, out _), Is.False);
    }

    [Test]
    public void Reset_RequiresNeutralAgainBeforeFiring()
    {
        var recognizer = CreateRecognizer();
        Run(recognizer, Ready().Frames);
        Assert.That(recognizer.IsArmed(GestureChannel.Vertical), Is.True);

        recognizer.Reset();
        Assert.That(recognizer.IsArmed(GestureChannel.Vertical), Is.False);
    }
}
