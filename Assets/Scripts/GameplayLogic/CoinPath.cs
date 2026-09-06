using UnityEngine;

public readonly struct CoinPath
{
    private readonly float _startY;
    private readonly float _endY;
    private readonly float _arcHeight;
    private readonly float _startT;
    private readonly float _endT;

    private CoinPath(float startY, float endY, float arcHeight, float startT, float endT)
    {
        _startY = startY;
        _endY = endY;
        _arcHeight = arcHeight;
        _startT = startT;
        _endT = endT;
    }

    public static CoinPath Flat(float y) => new CoinPath(y, y, 0f, 0f, 1f);

    public static CoinPath Arc(float baseY, float peakY) => new CoinPath(baseY, baseY, Mathf.Max(0f, peakY - baseY), 0f, 1f);

    public static CoinPath Slope(float startY, float endY, float startT, float endT) => new CoinPath(startY, endY, 0f, startT, endT);

    public float EvaluateY(float t) => Mathf.Lerp(_startY, _endY, t) + _arcHeight * 4f * t * (1f - t);

    public float EvaluateT(float t) => Mathf.Lerp(_startT, _endT, t);
}
