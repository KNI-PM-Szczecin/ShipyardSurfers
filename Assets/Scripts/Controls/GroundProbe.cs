using UnityEngine;

public readonly struct GroundHit
{
    public GroundHit(float standingY, bool isSlope)
    {
        StandingY = standingY;
        IsSlope = isSlope;
    }

    public float StandingY { get; }

    public bool IsSlope { get; }
}

public sealed class GroundProbe
{
    private const float FlatSurfaceMinUpY = 0.95f;

    private readonly int _layerMask;
    private readonly float _radius;
    private readonly float _standingOffset;
    private readonly float _startHeight;
    private readonly float _castLength;

    public GroundProbe(int layerMask, float radius, float standingOffset, float startHeight, float extraLength)
    {
        _layerMask = layerMask;
        _radius = radius;
        _standingOffset = standingOffset;
        _startHeight = startHeight;
        _castLength = startHeight + extraLength;
    }

    public bool TryProbe(Vector3 pivotPosition, float lookAhead, out GroundHit hit)
    {
        Vector3 origin = pivotPosition + Vector3.up * _startHeight + Vector3.forward * lookAhead;

        if (!Physics.SphereCast(origin, _radius, Vector3.down, out RaycastHit raycastHit,
                _castLength, _layerMask, QueryTriggerInteraction.Ignore))
        {
            hit = default;
            return false;
        }

        bool isSlope = raycastHit.collider.transform.up.y < FlatSurfaceMinUpY;
        hit = new GroundHit(raycastHit.point.y + _standingOffset, isSlope);
        return true;
    }
}
