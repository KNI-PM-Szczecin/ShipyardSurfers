using UnityEngine;

public class PoseHistory
{
    private readonly NormalizedPose[] _samples;
    private int _head = -1;
    private int _count;

    public PoseHistory(int capacity)
    {
        _samples = new NormalizedPose[capacity];
        for (int i = 0; i < capacity; i++)
        {
            _samples[i] = new NormalizedPose();
        }
    }

    public int Count => _count;

    public NormalizedPose Latest => _count > 0 ? _samples[_head] : null;

    public NormalizedPose this[int age]
    {
        get
        {
            if (age < 0 || age >= _count) return null;
            int index = (_head - age + _samples.Length) % _samples.Length;
            return _samples[index];
        }
    }

    public void Clear()
    {
        _head = -1;
        _count = 0;
    }

    public void Push(NormalizedPose pose)
    {
        _head = (_head + 1) % _samples.Length;
        _samples[_head].CopyFrom(pose);
        if (_count < _samples.Length) _count++;
    }

    public NormalizedPose SampleAtOrBefore(float time)
    {
        for (int age = 0; age < _count; age++)
        {
            NormalizedPose sample = this[age];
            if (sample.Timestamp <= time) return sample;
        }

        return null;
    }

    public bool TryGetDisplacement(int keypoint, float window, out Vector2 displacement)
    {
        displacement = default;
        NormalizedPose latest = Latest;
        if (latest == null) return false;

        NormalizedPose past = SampleAtOrBefore(latest.Timestamp - window);
        if (past == null) return false;

        displacement = latest.Points[keypoint] - past.Points[keypoint];
        return true;
    }

    public float PeakVelocity(int keypoint, float window, Vector2 direction)
    {
        NormalizedPose latest = Latest;
        if (latest == null || _count < 2) return 0f;

        float peak = 0f;
        float cutoff = latest.Timestamp - window;

        for (int age = 0; age + 1 < _count; age++)
        {
            NormalizedPose newer = this[age];
            NormalizedPose older = this[age + 1];
            if (newer.Timestamp < cutoff) break;

            float deltaTime = newer.Timestamp - older.Timestamp;
            if (deltaTime <= 0f) continue;

            float velocity = Vector2.Dot(newer.Points[keypoint] - older.Points[keypoint], direction) / deltaTime;
            if (velocity > peak) peak = velocity;
        }

        return peak;
    }

    public bool AnyWithin(float window, PosePredicate predicate)
    {
        NormalizedPose latest = Latest;
        if (latest == null) return false;

        float cutoff = latest.Timestamp - window;
        for (int age = 0; age < _count; age++)
        {
            NormalizedPose sample = this[age];
            if (sample.Timestamp < cutoff) break;
            if (predicate(sample)) return true;
        }

        return false;
    }

    public bool LatestConsecutive(int frames, PosePredicate predicate)
    {
        if (_count < frames) return false;

        for (int age = 0; age < frames; age++)
        {
            if (!predicate(this[age])) return false;
        }

        return true;
    }
}

public delegate bool PosePredicate(NormalizedPose pose);
