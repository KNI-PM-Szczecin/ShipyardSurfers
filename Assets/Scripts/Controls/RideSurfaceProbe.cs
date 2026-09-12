using UnityEngine;

public readonly struct RideSurface
{
    public RideSurface(float height, bool isWater)
    {
        Height = height;
        IsWater = isWater;
    }

    public float Height { get; }

    public bool IsWater { get; }
}

public sealed class RideSurfaceProbe
{
    private readonly int _layerMask;
    private readonly int _waterLayer;
    private readonly float _startHeight;
    private readonly float _castLength;

    public RideSurfaceProbe(int layerMask, int waterLayer, float startHeight, float castLength)
    {
        _layerMask = layerMask;
        _waterLayer = waterLayer;
        _startHeight = startHeight;
        _castLength = castLength;
    }

    public bool TryProbe(Vector3 pivotPosition, out RideSurface surface)
    {
        Vector3 origin = pivotPosition + Vector3.up * _startHeight;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _castLength, _layerMask, QueryTriggerInteraction.Ignore))
        {
            surface = default;
            return false;
        }

        surface = new RideSurface(hit.point.y, hit.collider.gameObject.layer == _waterLayer);
        return true;
    }
}
