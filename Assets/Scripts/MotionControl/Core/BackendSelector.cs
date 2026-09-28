using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class BackendSelector<TBackend>
{
    private readonly TBackend[] _candidates;
    private readonly int _warmupSamples;
    private readonly int _measuredSamples;
    private readonly float _requiredGain;
    private readonly float[] _medians;
    private readonly List<float> _samples = new List<float>();

    private int _trial;
    private int _warmupLeft;

    public TBackend Current { get; private set; }
    public bool IsDecided { get; private set; }

    public BackendSelector(IReadOnlyList<TBackend> candidates, int warmupSamples, int measuredSamples, float requiredGain)
    {
        if (candidates == null || candidates.Count == 0) throw new ArgumentException("At least one backend candidate is required.", nameof(candidates));

        _candidates = new TBackend[candidates.Count];
        for (int i = 0; i < candidates.Count; i++) _candidates[i] = candidates[i];
        _warmupSamples = Math.Max(0, warmupSamples);
        _measuredSamples = Math.Max(1, measuredSamples);
        _requiredGain = Math.Max(0f, requiredGain);
        _medians = new float[_candidates.Length];
        for (int i = 0; i < _medians.Length; i++) _medians[i] = float.PositiveInfinity;

        Current = _candidates[0];
        _warmupLeft = _warmupSamples;
        IsDecided = _candidates.Length == 1;
    }

    public float MedianOf(TBackend backend)
    {
        int index = Array.IndexOf(_candidates, backend);
        return index >= 0 ? _medians[index] : float.PositiveInfinity;
    }

    public bool ReportLatency(float latencyMs)
    {
        if (IsDecided) return false;

        if (_warmupLeft > 0)
        {
            _warmupLeft--;
            return false;
        }

        _samples.Add(latencyMs);
        if (_samples.Count < _measuredSamples) return false;

        _medians[_trial] = Median(_samples);
        return AdvanceTrial();
    }

    public bool ReportFailure()
    {
        if (IsDecided) return false;

        _medians[_trial] = float.PositiveInfinity;
        return AdvanceTrial();
    }

    public string Summary()
    {
        var builder = new StringBuilder();
        for (int i = 0; i < _candidates.Length; i++)
        {
            if (i > 0) builder.Append(", ");
            builder.Append(_candidates[i]).Append(' ');
            builder.Append(float.IsPositiveInfinity(_medians[i]) ? "n/a" : _medians[i].ToString("0.0", CultureInfo.InvariantCulture) + " ms");
        }

        builder.Append(" -> ").Append(Current);
        return builder.ToString();
    }

    private bool AdvanceTrial()
    {
        _samples.Clear();
        _trial++;

        if (_trial < _candidates.Length)
        {
            Current = _candidates[_trial];
            _warmupLeft = _warmupSamples;
            return true;
        }

        IsDecided = true;
        TBackend best = ChooseBest();
        bool changed = !EqualityComparer<TBackend>.Default.Equals(best, Current);
        Current = best;
        return changed;
    }

    private TBackend ChooseBest()
    {
        int best = 0;
        float threshold = _medians[0] * (1f - _requiredGain);

        for (int i = 1; i < _candidates.Length; i++)
        {
            if (_medians[i] >= threshold) continue;

            best = i;
            threshold = _medians[i];
        }

        if (float.IsPositiveInfinity(_medians[best]))
        {
            for (int i = 0; i < _candidates.Length; i++)
            {
                if (!float.IsPositiveInfinity(_medians[i])) return _candidates[i];
            }
        }

        return _candidates[best];
    }

    private static float Median(List<float> samples)
    {
        var sorted = samples.ToArray();
        Array.Sort(sorted);
        int middle = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) * 0.5f;
    }
}
