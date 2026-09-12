using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BoatAnimator : MonoBehaviour
{
    public const string BANK_LEFT_TRIGGER = "BankLeft";
    public const string BANK_RIGHT_TRIGGER = "BankRight";
    public const string ROLLING_PARAMETER = "Rolling";
    public const string JUMP_TRIGGER = "Jump";
    public const string BACKFLIP_TRIGGER = "Backflip";
    public const string DEAD_PARAMETER = "Dead";
    public const string GROUNDED_PARAMETER = "Grounded";

    [SerializeField, Range(0f, 1f), Tooltip("Chance that a jump plays the backflip instead of the plain hop")]
    private float _backflipChance = 0.2f;

    private Animator _animator;

    public float BackflipChance
    {
        get => _backflipChance;
        set => _backflipChance = Mathf.Clamp01(value);
    }

    private void Awake() => _animator = GetComponent<Animator>();

    private void OnEnable()
    {
        EventBus.PlayerLaneChangeStartedEvent += OnLaneChange;
        EventBus.PlayerJumpStartedEvent += OnJump;
        EventBus.PlayerRollStartedEvent += OnRollStarted;
        EventBus.PlayerRollEndedEvent += OnRollEnded;
        EventBus.PlayerGroundedChangedEvent += OnGroundedChanged;
        EventBus.DeathEvent += OnDeath;
        _animator.SetBool(GROUNDED_PARAMETER, true);
    }

    private void OnDisable()
    {
        EventBus.PlayerLaneChangeStartedEvent -= OnLaneChange;
        EventBus.PlayerJumpStartedEvent -= OnJump;
        EventBus.PlayerRollStartedEvent -= OnRollStarted;
        EventBus.PlayerRollEndedEvent -= OnRollEnded;
        EventBus.PlayerGroundedChangedEvent -= OnGroundedChanged;
        EventBus.DeathEvent -= OnDeath;
    }

    private void OnGroundedChanged(bool grounded) => _animator.SetBool(GROUNDED_PARAMETER, grounded);

    private void OnLaneChange(int direction)
    {
        _animator.ResetTrigger(BANK_LEFT_TRIGGER);
        _animator.ResetTrigger(BANK_RIGHT_TRIGGER);
        _animator.SetTrigger(direction > 0 ? BANK_RIGHT_TRIGGER : BANK_LEFT_TRIGGER);
    }

    private void OnJump()
    {
        _animator.ResetTrigger(JUMP_TRIGGER);
        _animator.ResetTrigger(BACKFLIP_TRIGGER);
        _animator.SetTrigger(Random.value < _backflipChance ? BACKFLIP_TRIGGER : JUMP_TRIGGER);
    }

    private void OnRollStarted() => _animator.SetBool(ROLLING_PARAMETER, true);

    private void OnRollEnded() => _animator.SetBool(ROLLING_PARAMETER, false);

    private void OnDeath() => _animator.SetBool(DEAD_PARAMETER, true);
}
