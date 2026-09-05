using NUnit.Framework;

public class TrackingStateMachineTests
{
    private static TrackingStateMachine Create()
    {
        var machine = new TrackingStateMachine(new TrackingTimings());
        machine.Enable(0f);
        return machine;
    }

    [Test]
    public void StartsInNoCameraAndMovesToSearchingOnFirstFrame()
    {
        var machine = Create();
        Assert.That(machine.State, Is.EqualTo(TrackingState.NoCamera));

        machine.Update(0.1f, true, false, false);

        Assert.That(machine.State, Is.EqualTo(TrackingState.Searching));
    }

    [Test]
    public void TracksAfterEnoughValidPoses()
    {
        var machine = Create();
        machine.Update(0.1f, true, false, false);

        machine.Update(0.2f, true, true, true);
        machine.Update(0.3f, true, true, true);
        Assert.That(machine.State, Is.EqualTo(TrackingState.Searching));

        machine.Update(0.4f, true, true, true);
        Assert.That(machine.State, Is.EqualTo(TrackingState.Tracking));
    }

    [Test]
    public void LosesPlayerAfterTimeoutAndRecovers()
    {
        var machine = Create();
        for (int i = 1; i <= 4; i++) machine.Update(i * 0.1f, true, true, true);
        Assert.That(machine.State, Is.EqualTo(TrackingState.Tracking));

        machine.Update(0.9f, true, true, false);
        Assert.That(machine.State, Is.EqualTo(TrackingState.Tracking));

        machine.Update(1.2f, true, true, false);
        Assert.That(machine.State, Is.EqualTo(TrackingState.Lost));

        for (int i = 0; i < 3; i++) machine.Update(1.3f + i * 0.1f, true, true, true);
        Assert.That(machine.State, Is.EqualTo(TrackingState.Tracking));
    }

    [Test]
    public void FallsBackToNoCameraWithoutFrames()
    {
        var machine = Create();
        for (int i = 1; i <= 4; i++) machine.Update(i * 0.1f, true, true, true);

        machine.Update(10f, false, false, false);

        Assert.That(machine.State, Is.EqualTo(TrackingState.NoCamera));
    }

    [Test]
    public void CameraWarmupSuppressesNoCamera()
    {
        var machine = Create();
        for (int i = 1; i <= 4; i++) machine.Update(i * 0.1f, true, true, true);

        machine.NotifyCameraStarted(1f);
        machine.Update(5f, false, false, false);
        Assert.That(machine.State, Is.Not.EqualTo(TrackingState.NoCamera));

        machine.Update(10.1f, false, false, false);
        Assert.That(machine.State, Is.EqualTo(TrackingState.NoCamera));
    }

    [Test]
    public void DisabledIgnoresUpdates()
    {
        var machine = Create();
        machine.Disable();

        machine.Update(1f, true, true, true);

        Assert.That(machine.State, Is.EqualTo(TrackingState.Disabled));
    }
}
