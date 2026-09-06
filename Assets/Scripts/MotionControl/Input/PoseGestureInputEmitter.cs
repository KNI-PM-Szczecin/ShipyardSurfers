using System;
using UnityEngine.InputSystem;

public class PoseGestureInputEmitter : IDisposable
{
    private readonly float _holdDuration;
    private PoseGestureDevice _device;
    private float _releaseAt;
    private bool _held;

    public bool IsAttached => _device != null;

    public PoseGestureInputEmitter(float holdDuration)
    {
        _holdDuration = holdDuration;
    }

    public void Attach()
    {
        if (_device != null) return;
        _device = InputSystem.AddDevice<PoseGestureDevice>();
    }

    public void Detach()
    {
        if (_device == null) return;
        InputSystem.RemoveDevice(_device);
        _device = null;
        _held = false;
    }

    public void Press(GestureType gesture, float now)
    {
        if (_device == null) return;

        InputSystem.QueueStateEvent(_device, PoseGestureState.Pressed(BitFor(gesture)));
        _releaseAt = now + _holdDuration;
        _held = true;
    }

    public void Update(float now)
    {
        if (!_held || _device == null || now < _releaseAt) return;

        InputSystem.QueueStateEvent(_device, PoseGestureState.Released);
        _held = false;
    }

    public void Dispose() => Detach();

    private static int BitFor(GestureType gesture)
    {
        switch (gesture)
        {
            case GestureType.Jump: return PoseGestureState.UP_BIT;
            case GestureType.Roll: return PoseGestureState.DOWN_BIT;
            case GestureType.LaneLeft: return PoseGestureState.LEFT_BIT;
            case GestureType.LaneRight: return PoseGestureState.RIGHT_BIT;
            default: throw new ArgumentOutOfRangeException(nameof(gesture), gesture, null);
        }
    }
}
