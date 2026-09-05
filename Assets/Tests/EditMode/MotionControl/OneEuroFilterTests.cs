using NUnit.Framework;

public class OneEuroFilterTests
{
    [Test]
    public void ConstantSignalPassesThrough()
    {
        var filter = new OneEuroFilter(new SmoothingSettings());

        for (int i = 0; i < 20; i++)
        {
            Assert.That(filter.Filter(0.7f, i / 30f), Is.EqualTo(0.7f).Within(0.0001f));
        }
    }

    [Test]
    public void StepSignalConvergesToNewValue()
    {
        var filter = new OneEuroFilter(new SmoothingSettings());
        filter.Filter(0f, 0f);

        float value = 0f;
        for (int i = 1; i <= 60; i++) value = filter.Filter(1f, i / 30f);

        Assert.That(value, Is.EqualTo(1f).Within(0.01f));
    }

    [Test]
    public void FastMotionIsFollowedMoreClosely()
    {
        var slowSettings = new SmoothingSettings { MinCutoff = 1.5f, Beta = 0f };
        var responsiveSettings = new SmoothingSettings { MinCutoff = 1.5f, Beta = 1f };
        var slow = new OneEuroFilter(slowSettings);
        var responsive = new OneEuroFilter(responsiveSettings);
        slow.Filter(0f, 0f);
        responsive.Filter(0f, 0f);

        float slowValue = slow.Filter(5f, 1f / 30f);
        float responsiveValue = responsive.Filter(5f, 1f / 30f);

        Assert.That(responsiveValue, Is.GreaterThan(slowValue));
    }
}
