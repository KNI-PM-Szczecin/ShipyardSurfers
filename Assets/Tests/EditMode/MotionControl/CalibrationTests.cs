using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CalibrationTests
{
    private const int ROUNDS = 3;
    private const int EXPECTED_COLUMNS = 13 + KeypointIndex.COUNT * 3;

    [Test]
    public void PlanRepeatsEveryGestureOncePerRound()
    {
        var plan = new CalibrationPlan(ROUNDS, new System.Random(1));

        Assert.That(plan.Count, Is.EqualTo(ROUNDS * CalibrationPlan.Gestures.Length));

        for (int round = 0; round < ROUNDS; round++)
        {
            var seen = new List<GestureType>();
            for (int step = 0; step < plan.StepsPerRound; step++)
            {
                int index = round * plan.StepsPerRound + step;
                Assert.That(plan.Round(index), Is.EqualTo(round + 1));
                Assert.That(plan.StepInRound(index), Is.EqualTo(step + 1));
                seen.Add(plan.Gesture(index));
            }

            Assert.That(seen, Is.EquivalentTo(CalibrationPlan.Gestures));
        }
    }

    [Test]
    public void PlanShufflesRoundsIndependently()
    {
        var plan = new CalibrationPlan(ROUNDS, new System.Random(7));
        var first = new List<GestureType>();
        var second = new List<GestureType>();

        for (int step = 0; step < plan.StepsPerRound; step++)
        {
            first.Add(plan.Gesture(step));
            second.Add(plan.Gesture(plan.StepsPerRound + step));
        }

        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    public void SessionWalksCountdownPromptRestForEveryStep()
    {
        var plan = new CalibrationPlan(1, new System.Random(3));
        var session = new CalibrationSession(plan, 10f, 3f, 1f);

        Assert.That(session.Phase, Is.EqualTo(CalibrationPhase.Countdown));
        Assert.That(session.IsRecording, Is.False);
        Assert.That(session.SecondsLeft, Is.EqualTo(10f).Within(0.001f));

        session.Tick(9f);
        Assert.That(session.Phase, Is.EqualTo(CalibrationPhase.Countdown));
        Assert.That(session.SecondsLeft, Is.EqualTo(1f).Within(0.001f));

        session.Tick(1.5f);
        Assert.That(session.Phase, Is.EqualTo(CalibrationPhase.Prompt));
        Assert.That(session.IsRecording, Is.True);
        Assert.That(session.StepNumber, Is.EqualTo(1));

        session.Tick(3f);
        Assert.That(session.Phase, Is.EqualTo(CalibrationPhase.Rest));
        Assert.That(session.IsRecording, Is.False);
        Assert.That(session.IsSampling, Is.True);

        session.Tick(1f);
        Assert.That(session.Phase, Is.EqualTo(CalibrationPhase.Prompt));
        Assert.That(session.StepNumber, Is.EqualTo(2));
    }

    [Test]
    public void SessionFinishesAfterLastStep()
    {
        var plan = new CalibrationPlan(ROUNDS, new System.Random(5));
        var session = new CalibrationSession(plan, 1f, 1f, 1f);

        session.Tick(1f + plan.Count * 2f + 0.1f);

        Assert.That(session.IsFinished, Is.True);
        Assert.That(session.IsRecording, Is.False);
        Assert.That(session.StepNumber, Is.EqualTo(plan.Count));
    }

    [Test]
    public void CsvUsesInvariantNumbersAndOneColumnPerKeypointAxis()
    {
        var log = new CalibrationCsvLog();
        var pose = new NormalizedPose { IsValid = true, Timestamp = 1.5f, ShoulderWidth = 0.25f };
        pose.Points[KeypointIndex.RIGHT_WRIST] = new Vector2(0.5f, -0.25f);
        pose.Confidences[KeypointIndex.RIGHT_WRIST] = 0.75f;

        log.AppendSample(1, 2, GestureType.LaneRight, CalibrationPhase.Prompt, 0, 0.5f, TrackingState.Tracking,
            GestureType.LaneRight, pose);

        string[] lines = log.ToCsv().Split('\n');
        Assert.That(log.SampleCount, Is.EqualTo(1));
        Assert.That(lines[0].Split(',').Length, Is.EqualTo(EXPECTED_COLUMNS));
        Assert.That(lines[1].Split(',').Length, Is.EqualTo(EXPECTED_COLUMNS));
        Assert.That(lines[0], Does.Contain("round,step,prompt,phase,sample"));
        Assert.That(lines[0], Does.Contain("right_wrist_x,right_wrist_y,right_wrist_c"));
        Assert.That(lines[1], Does.Contain("Prompt"));
        Assert.That(lines[1], Does.Contain("0.5"));
        Assert.That(lines[1], Does.Not.Contain("0,5"));
        Assert.That(lines[1], Does.Contain("LaneRight"));
    }
}
