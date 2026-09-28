using NUnit.Framework;

public class DispatchPlannerTests
{
    private const int LAYERS = 300;

    [Test]
    public void WithoutFrameTime_DispatchesEverythingInOneFrame()
    {
        var planner = new DispatchPlanner(0.05f);

        Assert.That(planner.FramesForDispatch(), Is.EqualTo(1));
        Assert.That(planner.LayersPerFrame(LAYERS), Is.EqualTo(LAYERS));
    }

    [Test]
    public void SixtyFps_SpreadsFiftyMsWindowOverThreeFrames()
    {
        var planner = new DispatchPlanner(0.05f);
        planner.ReportFrameTime(1f / 60f);

        Assert.That(planner.FramesForDispatch(), Is.EqualTo(3));
        Assert.That(planner.LayersPerFrame(LAYERS), Is.EqualTo(100));
    }

    [Test]
    public void SlowFrames_DispatchEverythingInOneFrame()
    {
        var planner = new DispatchPlanner(0.05f);
        planner.ReportFrameTime(0.04f);

        Assert.That(planner.FramesForDispatch(), Is.EqualTo(1));
        Assert.That(planner.LayersPerFrame(LAYERS), Is.EqualTo(LAYERS));
    }

    [Test]
    public void FastFrames_SpreadOverMoreFramesButNeverBelowOneLayer()
    {
        var planner = new DispatchPlanner(0.05f);
        planner.ReportFrameTime(1f / 240f);

        Assert.That(planner.FramesForDispatch(), Is.EqualTo(12));
        Assert.That(planner.LayersPerFrame(2), Is.EqualTo(1));
    }

    [Test]
    public void FrameTimeIsSmoothedAndIgnoresNonPositiveDeltas()
    {
        var planner = new DispatchPlanner(0.05f);
        for (int i = 0; i < 100; i++) planner.ReportFrameTime(1f / 60f);
        planner.ReportFrameTime(0f);
        planner.ReportFrameTime(-1f);
        planner.ReportFrameTime(0.02f);

        Assert.That(planner.FrameTime, Is.GreaterThan(1f / 60f).And.LessThan(0.02f));
    }

    [Test]
    public void ZeroWindow_AlwaysDispatchesInOneFrame()
    {
        var planner = new DispatchPlanner(0f);
        planner.ReportFrameTime(1f / 144f);

        Assert.That(planner.FramesForDispatch(), Is.EqualTo(1));
    }
}
