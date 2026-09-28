using System;
using NUnit.Framework;

public class BackendSelectorTests
{
    private enum Backend
    {
        Gpu,
        Cpu
    }

    private static BackendSelector<Backend> Create(int warmup = 1, int samples = 3, float gain = 0.2f)
    {
        return new BackendSelector<Backend>(new[] { Backend.Gpu, Backend.Cpu }, warmup, samples, gain);
    }

    private static bool Feed(BackendSelector<Backend> selector, params float[] latencies)
    {
        bool changed = false;
        foreach (float latency in latencies) changed |= selector.ReportLatency(latency);
        return changed;
    }

    [Test]
    public void SingleCandidate_IsDecidedImmediately()
    {
        var selector = new BackendSelector<Backend>(new[] { Backend.Gpu }, 2, 5, 0.2f);

        Assert.That(selector.IsDecided, Is.True);
        Assert.That(selector.Current, Is.EqualTo(Backend.Gpu));
        Assert.That(selector.ReportLatency(10f), Is.False);
    }

    [Test]
    public void NoCandidates_Throws()
    {
        Assert.Throws<ArgumentException>(() => new BackendSelector<Backend>(Array.Empty<Backend>(), 1, 1, 0f));
    }

    [Test]
    public void SwitchesToAlternateAfterFirstTrial()
    {
        var selector = Create();

        Assert.That(Feed(selector, 100f, 50f, 50f), Is.False);
        Assert.That(selector.ReportLatency(50f), Is.True);
        Assert.That(selector.Current, Is.EqualTo(Backend.Cpu));
        Assert.That(selector.IsDecided, Is.False);
    }

    [Test]
    public void KeepsConfiguredBackendWhenAlternateIsNotClearlyFaster()
    {
        var selector = Create();
        Feed(selector, 100f, 50f, 50f, 50f);

        Assert.That(Feed(selector, 1f, 45f, 45f), Is.False);
        Assert.That(selector.ReportLatency(45f), Is.True);
        Assert.That(selector.IsDecided, Is.True);
        Assert.That(selector.Current, Is.EqualTo(Backend.Gpu));
    }

    [Test]
    public void PicksAlternateWhenItIsClearlyFaster()
    {
        var selector = Create();
        Feed(selector, 100f, 50f, 50f, 50f);

        Assert.That(Feed(selector, 1f, 20f, 20f, 20f), Is.False);
        Assert.That(selector.IsDecided, Is.True);
        Assert.That(selector.Current, Is.EqualTo(Backend.Cpu));
        Assert.That(selector.MedianOf(Backend.Gpu), Is.EqualTo(50f));
        Assert.That(selector.MedianOf(Backend.Cpu), Is.EqualTo(20f));
    }

    [Test]
    public void WarmupSamplesAreIgnored()
    {
        var selector = Create(warmup: 2, samples: 1);
        Feed(selector, 1000f, 1000f, 30f);
        Feed(selector, 1f, 1f, 100f);

        Assert.That(selector.IsDecided, Is.True);
        Assert.That(selector.Current, Is.EqualTo(Backend.Gpu));
        Assert.That(selector.MedianOf(Backend.Gpu), Is.EqualTo(30f));
    }

    [Test]
    public void MedianIgnoresOutliers()
    {
        var selector = Create(warmup: 0, samples: 5);
        Feed(selector, 40f, 900f, 41f, 39f, 42f);
        Feed(selector, 60f, 60f, 60f, 60f, 60f);

        Assert.That(selector.MedianOf(Backend.Gpu), Is.EqualTo(41f));
        Assert.That(selector.Current, Is.EqualTo(Backend.Gpu));
    }

    [Test]
    public void FailedCandidateIsSkipped()
    {
        var selector = Create(warmup: 0, samples: 2);

        Assert.That(selector.ReportFailure(), Is.True);
        Assert.That(selector.Current, Is.EqualTo(Backend.Cpu));

        Feed(selector, 80f, 80f);
        Assert.That(selector.IsDecided, Is.True);
        Assert.That(selector.Current, Is.EqualTo(Backend.Cpu));
    }

    [Test]
    public void FallsBackToFirstWorkingCandidateWhenLastFails()
    {
        var selector = Create(warmup: 0, samples: 2);
        Feed(selector, 80f, 80f);

        Assert.That(selector.ReportFailure(), Is.True);
        Assert.That(selector.IsDecided, Is.True);
        Assert.That(selector.Current, Is.EqualTo(Backend.Gpu));
    }

    [Test]
    public void SummaryListsMediansAndChoice()
    {
        var selector = Create(warmup: 0, samples: 1);
        Feed(selector, 50f, 20f);

        Assert.That(selector.Summary(), Is.EqualTo("Gpu 50.0 ms, Cpu 20.0 ms -> Cpu"));
    }
}
