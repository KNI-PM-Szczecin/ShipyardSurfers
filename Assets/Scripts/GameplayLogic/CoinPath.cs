using UnityEngine;

public readonly struct CoinPath
{
    private readonly float _startZ;
    private readonly float _peakZ;
    private readonly float _endZ;
    private readonly float _startY;
    private readonly float _peakY;
    private readonly float _endY;
    private readonly float _startT;
    private readonly float _endT;

    private CoinPath(float startZ, float startY, float peakZ, float peakY, float endZ, float endY, float startT, float endT)
    {
        _startZ = startZ;
        _startY = startY;
        _peakZ = peakZ;
        _peakY = peakY;
        _endZ = endZ;
        _endY = endY;
        _startT = startT;
        _endT = endT;
    }

    public static CoinPath Flat(float y) => new CoinPath(0f, y, 1f, y, 2f, y, 0f, 1f);

    public static CoinPath Slope(float startZ, float startY, float endZ, float endY, float startT)
        => new CoinPath(startZ, startY, endZ, endY, endZ, endY, startT, 1f);

    public static CoinPath Jump(float startZ, float groundY, float peakZ, float peakY, float endZ)
        => new CoinPath(startZ, groundY, peakZ, peakY, endZ, groundY, 0f, 1f);

    public float EvaluateY(float z)
    {
        if (z <= _startZ) return _startY;
        if (z < _peakZ) return Mathf.Lerp(_startY, _peakY, Mathf.InverseLerp(_startZ, _peakZ, z));
        if (z < _endZ) return Mathf.Lerp(_peakY, _endY, Mathf.InverseLerp(_peakZ, _endZ, z));

        return _endY;
    }

    public float EvaluateT(float t) => Mathf.Lerp(_startT, _endT, t);
}
