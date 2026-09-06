using UnityEngine;

public class PoseSmoother
{
    private readonly int[] _indices;
    private readonly OneEuroFilter[] _xFilters;
    private readonly OneEuroFilter[] _yFilters;

    public PoseSmoother(SmoothingSettings settings, int[] keypointIndices)
    {
        _indices = keypointIndices;
        _xFilters = new OneEuroFilter[_indices.Length];
        _yFilters = new OneEuroFilter[_indices.Length];

        for (int i = 0; i < _indices.Length; i++)
        {
            _xFilters[i] = new OneEuroFilter(settings);
            _yFilters[i] = new OneEuroFilter(settings);
        }
    }

    public void Reset()
    {
        for (int i = 0; i < _indices.Length; i++)
        {
            _xFilters[i].Reset();
            _yFilters[i].Reset();
        }
    }

    public void Smooth(NormalizedPose pose)
    {
        for (int i = 0; i < _indices.Length; i++)
        {
            int index = _indices[i];
            Vector2 point = pose.Points[index];
            pose.Points[index] = new Vector2(_xFilters[i].Filter(point.x, pose.Timestamp), _yFilters[i].Filter(point.y, pose.Timestamp));
        }

        pose.HipLine = (pose.Points[KeypointIndex.LEFT_HIP].y + pose.Points[KeypointIndex.RIGHT_HIP].y) * 0.5f;
    }
}
