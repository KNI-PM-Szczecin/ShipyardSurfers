public interface IGestureRule
{
    GestureType Type { get; }
    GestureChannel Channel { get; }
    bool ShouldFire(PoseHistory history, GestureThresholds thresholds);
}
