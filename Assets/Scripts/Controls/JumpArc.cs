using UnityEngine;

public readonly struct JumpArc
{
    private const float MIN_VALUE = 0.01f;

    public JumpArc(float duration, float height, float fallSpeed)
    {
        Duration = Mathf.Max(duration, MIN_VALUE);
        Height = Mathf.Max(height, MIN_VALUE);
        FallSpeed = Mathf.Max(fallSpeed, MIN_VALUE);
    }

    public float Duration { get; }

    public float Height { get; }

    public float FallSpeed { get; }

    public static JumpArc Default => new JumpArc(0.15f, 3f, 5.5f);

    public float RiseDistance(float trackSpeed) => Duration * trackSpeed;

    public float FallDistance(float trackSpeed) => Height / FallSpeed * trackSpeed;

    public float TotalDistance(float trackSpeed) => RiseDistance(trackSpeed) + FallDistance(trackSpeed);

    public float HeightAt(float distanceFromStart, float trackSpeed)
    {
        if (distanceFromStart <= 0f) return 0f;

        float rise = RiseDistance(trackSpeed);
        if (distanceFromStart < rise) return Height * (distanceFromStart / rise);

        float fallen = distanceFromStart - rise;
        float fall = FallDistance(trackSpeed);
        return fallen >= fall ? 0f : Height * (1f - fallen / fall);
    }
}
