using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class MovmentController : MonoBehaviour
{
    private enum VerticalState
    {
        Grounded,
        Jumping,
        Falling,
        Rolling
    }

    [Header("Track placement")]
    [SerializeField] private Transform[] _trackTransforms;

    [Header("Track changing")]
    [SerializeField] private float _trackChangeDuration = 0.12f;
    [SerializeField] private float _wallBounceDuration = 0.15f;
    [SerializeField, Tooltip("Hitting a side wall again within this many seconds after a bounce kills the player.")]
    private float _stumbleDuration = 1f;
    [SerializeField] private LayerMask _obsticleMask = 1 << 6;
    [SerializeField] private float _sideCollisionDistance = 0.4f;

    [Header("Jumping")]
    [SerializeField] private float _jumpDuration = 0.2f;
    [SerializeField] private float _jumpHeight = 1f;
    [SerializeField] private float _fallSpeed = 1f;
    [SerializeField, Tooltip("A flat surface at most this far above the player still counts as ground to land on.")]
    private float _landTolerance = 0.5f;
    [SerializeField, Tooltip("A jump pressed this many seconds before landing still triggers on touchdown.")]
    private float _jumpBufferDuration = 0.15f;

    [Header("Roll")]
    [SerializeField, Tooltip("Time to crouch down and, separately, to stand back up.")]
    private float _rollDuration = 0.12f;
    [SerializeField, Tooltip("Time spent fully crouched between going down and standing up.")]
    private float _rollHoldDuration = 0.45f;
    [SerializeField] private float _rollHeight = -0.5f;
    [SerializeField] private float _rollFallMultiplier = 2f;

    [Header("Ground detection")]
    [SerializeField, FormerlySerializedAs("_playerMask"), Tooltip("Layers that never count as ground.")]
    private LayerMask _groundIgnoredLayers;
    [SerializeField] private float _rayStartHeight = 1f;
    [SerializeField] private float _rayLength = 1f;
    [SerializeField] private float _stepTolerance = 0.05f;

    private const float TRACK_SNAP_EPSILON = 0.1f;
    private const float DEFAULT_LANE_WIDTH = 3f;
    private const float INPUT_THRESHOLD = 0.5f;

    public static JumpArc Arc { get; private set; } = JumpArc.Default;

    public float JumpHeightMultiplier { get; set; } = 1f;
    public float LaneWidth { get; private set; } = DEFAULT_LANE_WIDTH;

    private InputAction _moveAction;
    private Rigidbody _rigidbody;
    private GroundProbe _groundProbe;
    private readonly InputBuffer _jumpBuffer = new InputBuffer();
    private RollProfile _rollProfile;

    private int _activeTrack;
    private int _lastLaneInputDirection;
    private int _lastVerticalInputDirection;
    private bool _trackChangeLock;
    private bool _lastChangeIncrement;
    private float _stumbleEndTime;
    private Coroutine _trackChangeCoroutine;

    private VerticalState _verticalState = VerticalState.Grounded;
    private float _jumpStartY;
    private float _jumpTargetHeight;
    private float _jumpRiseDuration;
    private float _jumpElapsed;
    private float _fallSpeedMultiplier = 1f;
    private bool _rollQueued;
    private float _rollElapsed;
    private float _standingY;

    private bool _isDead;

    private bool IsStumbling => Time.time < _stumbleEndTime;

    private static float GroundLookAhead => SegmentMover.MoveSpeed * Time.fixedDeltaTime;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        Arc = new JumpArc(_jumpDuration, _jumpHeight, _fallSpeed);

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        float standingOffset = capsule.height * 0.5f - capsule.center.y;
        _groundProbe = new GroundProbe(~_groundIgnoredLayers.value, capsule.radius, standingOffset,
            _rayStartHeight, _rayLength);
    }

    private void Start()
    {
        if (_trackTransforms == null || _trackTransforms.Length == 0)
        {
            Debug.LogError("There are no tracks assigned to the player.", this);
            enabled = false;
            return;
        }

        int middleTrackIndex = _trackTransforms.Length / 2;
        transform.position = new Vector3(_trackTransforms[middleTrackIndex].position.x, 0f, 0f);
        _activeTrack = middleTrackIndex;
        _standingY = transform.position.y;

        if (_trackTransforms.Length > 1)
        {
            LaneWidth = Mathf.Abs(_trackTransforms[1].position.x - _trackTransforms[0].position.x);
        }

        _moveAction = InputSystem.actions.FindAction("Move");

        EventBus.WallSideHitEvent += OnSideWallHit;
        EventBus.WallFrontHitEvent += OnFrontWallHit;
        EventBus.KillZoneHitEvent += OnKillZoneHit;
    }

    private void OnDestroy()
    {
        EventBus.WallSideHitEvent -= OnSideWallHit;
        EventBus.WallFrontHitEvent -= OnFrontWallHit;
        EventBus.KillZoneHitEvent -= OnKillZoneHit;
    }

    private void FixedUpdate()
    {
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();

        HandleLaneInput(moveInput.x);
        if (_isDead) return;

        HandleVerticalInput(moveInput.y);
        UpdateVerticalMotion(Time.fixedDeltaTime);
    }

    #region Lane changes

    private void HandleLaneInput(float horizontal)
    {
        int direction = ReadDirection(horizontal);
        bool pressed = direction != 0 && direction != _lastLaneInputDirection;
        _lastLaneInputDirection = direction;

        if (!pressed) return;

        ChangeTrack(direction > 0, _trackChangeDuration);
    }

    private static int ReadDirection(float axis)
    {
        if (axis >= INPUT_THRESHOLD) return 1;
        if (axis <= -INPUT_THRESHOLD) return -1;
        return 0;
    }

    private void ChangeTrack(bool increment, float duration, bool isBounce = false)
    {
        if (_isDead) return;

        int targetTrack = isBounce
            ? FindNeighbouringTrack(transform.position.x, increment)
            : _activeTrack + (increment ? 1 : -1);

        if (targetTrack < 0 || targetTrack >= _trackTransforms.Length)
        {
            HandleDoubleSideHit(targetTrack);
            return;
        }

        if (_trackChangeCoroutine != null) StopCoroutine(_trackChangeCoroutine);

        _activeTrack = targetTrack;
        _trackChangeLock = true;
        _lastChangeIncrement = increment;
        _trackChangeCoroutine = StartCoroutine(ChangeTrackRoutine(_trackTransforms[targetTrack].position.x, duration));
        EventBus.PlayerLaneChangeStarted(increment ? 1 : -1);
    }

    private int FindNeighbouringTrack(float x, bool increment)
    {
        int closest = increment ? _trackTransforms.Length : -1;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < _trackTransforms.Length; i++)
        {
            float delta = _trackTransforms[i].position.x - x;
            if (increment ? delta <= TRACK_SNAP_EPSILON : delta >= -TRACK_SNAP_EPSILON) continue;

            float distance = Mathf.Abs(delta);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = i;
            }
        }

        return closest;
    }

    private IEnumerator ChangeTrackRoutine(float targetX, float duration)
    {
        float elapsed = 0f;
        float startX = transform.position.x;
        Vector3 sideDirection = Vector3.right * (targetX > startX ? 1f : -1f);

        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;

            Vector3 origin = transform.position + Vector3.up * _rayStartHeight;
            if (Physics.Raycast(origin, sideDirection, _sideCollisionDistance, _obsticleMask, QueryTriggerInteraction.Ignore))
            {
                Die();
                yield break;
            }

            float newX = Mathf.Lerp(startX, targetX, elapsed / duration);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);

            yield return new WaitForFixedUpdate();
        }

        _trackChangeLock = false;
        _trackChangeCoroutine = null;
    }

    private void OnSideWallHit(bool bounceRight)
    {
        if (_isDead) return;
        if (_trackChangeLock && bounceRight == _lastChangeIncrement) return;

        if (IsStumbling)
        {
            Die();
            return;
        }

        _stumbleEndTime = Time.time + _stumbleDuration;
        CancelJumpIfAirborne();
        ChangeTrack(bounceRight, _wallBounceDuration, isBounce: true);
    }

    private void HandleDoubleSideHit(int targetTrack)
    {
        int trackId = targetTrack < 0 ? 1 : _trackTransforms.Length - 2;
        transform.position = new Vector3(_trackTransforms[trackId].position.x, transform.position.y, transform.position.z);
        Die();
    }

    #endregion

    #region Vertical motion

    private void HandleVerticalInput(float vertical)
    {
        int direction = ReadDirection(vertical);
        bool pressed = direction != 0 && direction != _lastVerticalInputDirection;
        _lastVerticalInputDirection = direction;

        if (!pressed) return;

        if (direction > 0)
        {
            TryStartJump();
        }
        else
        {
            TryStartRoll();
        }
    }

    private void UpdateVerticalMotion(float deltaTime)
    {
        switch (_verticalState)
        {
            case VerticalState.Grounded:
                UpdateGrounded();
                break;
            case VerticalState.Jumping:
                UpdateJumping(deltaTime);
                break;
            case VerticalState.Falling:
                UpdateFalling(deltaTime);
                break;
            case VerticalState.Rolling:
                UpdateRolling(deltaTime);
                break;
        }
    }

    private void TryStartJump()
    {
        if (_verticalState == VerticalState.Rolling) EndRoll();

        if (_verticalState != VerticalState.Grounded)
        {
            _jumpBuffer.Press(Time.time);
            return;
        }

        StartJump();
    }

    private void StartJump()
    {
        _jumpBuffer.Clear();
        _jumpStartY = transform.position.y;
        _jumpTargetHeight = _jumpHeight * JumpHeightMultiplier;
        _jumpRiseDuration = _jumpDuration * JumpHeightMultiplier;
        _jumpElapsed = 0f;
        SetVerticalState(VerticalState.Jumping);
        EventBus.PlayerJumpStarted();
    }

    private static bool IsGrounded(VerticalState state) => state == VerticalState.Grounded || state == VerticalState.Rolling;

    private void SetVerticalState(VerticalState state)
    {
        bool wasGrounded = IsGrounded(_verticalState);
        _verticalState = state;

        bool grounded = IsGrounded(state);
        if (grounded != wasGrounded) EventBus.PlayerGroundedChanged(grounded);
    }

    private void TryStartRoll()
    {
        switch (_verticalState)
        {
            case VerticalState.Rolling:
                return;

            case VerticalState.Grounded:
                if (_groundProbe.TryProbe(transform.position, GroundLookAhead, out GroundHit hit) && hit.IsSlope) return;
                StartRoll();
                return;

            default:
                StartFall(_rollFallMultiplier);
                _rollQueued = true;
                return;
        }
    }

    private void UpdateGrounded()
    {
        if (_groundProbe.TryProbe(transform.position, GroundLookAhead, out GroundHit hit) && hit.StandingY >= transform.position.y - _stepTolerance)
        {
            SetY(hit.StandingY);
            return;
        }

        StartFall(1f);
    }

    private void UpdateJumping(float deltaTime)
    {
        _jumpElapsed += deltaTime;
        SetY(Mathf.Lerp(_jumpStartY, _jumpStartY + _jumpTargetHeight, _jumpElapsed / _jumpRiseDuration));

        if (_jumpElapsed >= _jumpRiseDuration) StartFall(1f);
    }

    private void StartFall(float speedMultiplier)
    {
        _fallSpeedMultiplier = speedMultiplier;
        SetVerticalState(VerticalState.Falling);
    }

    private void CancelJumpIfAirborne()
    {
        if (_verticalState == VerticalState.Jumping) StartFall(1f);
    }

    private void UpdateFalling(float deltaTime)
    {
        float currentY = transform.position.y;
        float nextY = currentY - _fallSpeed * _fallSpeedMultiplier * deltaTime;

        if (_groundProbe.TryProbe(transform.position, GroundLookAhead, out GroundHit hit) && hit.StandingY >= nextY && CanLandOn(hit, currentY))
        {
            Land(hit);
            return;
        }

        SetY(nextY);
    }

    private bool CanLandOn(GroundHit hit, float currentY) => hit.IsSlope || hit.StandingY - currentY <= _landTolerance;

    private void Land(GroundHit hit)
    {
        SetY(hit.StandingY);
        _standingY = hit.StandingY;
        _fallSpeedMultiplier = 1f;
        SetVerticalState(VerticalState.Grounded);

        if (_rollQueued)
        {
            _rollQueued = false;
            _jumpBuffer.Clear();
            if (!hit.IsSlope) StartRoll();
            return;
        }

        if (_jumpBuffer.TryConsume(Time.time, _jumpBufferDuration)) StartJump();
    }

    private void StartRoll()
    {
        _rollProfile = new RollProfile(_rollDuration, _rollHoldDuration, _rollHeight);
        _standingY = transform.position.y;
        _rollElapsed = 0f;
        SetVerticalState(VerticalState.Rolling);
        EventBus.PlayerRollStarted();
    }

    private void UpdateRolling(float deltaTime)
    {
        Vector3 standingPosition = new Vector3(transform.position.x, _standingY, transform.position.z);
        bool hasGround = _groundProbe.TryProbe(standingPosition, GroundLookAhead, out GroundHit hit)
                         && hit.StandingY >= _standingY - _stepTolerance;

        if (!hasGround)
        {
            EndRoll();
            StartFall(1f);
            return;
        }

        if (hit.IsSlope)
        {
            EndRoll();
            return;
        }

        _standingY = hit.StandingY;
        _rollElapsed += deltaTime;

        if (_rollElapsed >= _rollProfile.TotalDuration)
        {
            EndRoll();
            return;
        }

        SetY(_standingY + _rollProfile.Evaluate(_rollElapsed));
    }

    private void EndRoll()
    {
        SetY(_standingY);
        SetVerticalState(VerticalState.Grounded);
        EventBus.PlayerRollEnded();
    }

    private void SetY(float y) =>
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

    #endregion

    #region Death

    private void OnFrontWallHit() => Die();

    private void OnKillZoneHit() => Die();

    private void Die()
    {
        if (_isDead) return;

        _isDead = true;
        StopAllCoroutines();
        _trackChangeCoroutine = null;
        FreezeRigidbody();

        EventBus.PlayerDeath();
        enabled = false;
    }

    private void FreezeRigidbody()
    {
        if (_rigidbody.isKinematic) return;

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;
    }

    #endregion
}
