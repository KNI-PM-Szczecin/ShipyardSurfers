using UnityEngine;

[RequireComponent(typeof(MovmentController))]
public class SuperJumpEffect : PowerUpEffect
{
    private MovmentController _movement;

    public override PowerUpType Type => PowerUpType.SuperJump;

    private void Awake()
    {
        _movement = GetComponent<MovmentController>();
    }

    protected override void OnActivated()
    {
        _movement.JumpHeightMultiplier = PowerUpSettings.SUPER_JUMP_HEIGHT_MULTIPLIER;
    }

    protected override void OnDeactivated()
    {
        _movement.JumpHeightMultiplier = 1f;
    }
}
