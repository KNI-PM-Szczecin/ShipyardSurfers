using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines.Interpolators;

public class MovmentController : MonoBehaviour
{
    static public Action<bool> WallHitAction;

    [Header("Track placement")]
    [SerializeField] private Transform[] _trackTransforms;
    private int _activeTrack;
    private InputAction _moveAction;

    [Header("Track changing")]
    [SerializeField] private float _trackChangeDuration = 0.3f;
    [SerializeField] private float _wallBounceDuration = 0.15f;
    [SerializeField] private float _rollDuration = 0.3f;
    [Space]
    [Header("Jumping")]
    [SerializeField] private float _jumpDuration = 0.2f;
    [SerializeField] private float _jumpHeight = 1f;
    [SerializeField] private float _fallSpeed = 1f;
    [SerializeField] private float _rayLength = 1f;
    [Space]
    [Header("Roll")]
    [SerializeField] private float _rollHeight = -0.5f;
    [SerializeField] private float _rollFallMultiplier = 2f;
    [Space]
    [Header("RampsHandling")]
    [SerializeField] private LayerMask _playerMask;
    [SerializeField] private float _rayStartHeight = 1f;
    [SerializeField] private float _stepTolerance = 0.05f;

    private bool _trackChangeLock = false;
    private float _trackChangeTime = 0;
    private bool _rollLock = false;
    private float _rollTime = 0;
    private float _initialY;

    private Coroutine _trackChangeCoroutine = null;
    private Coroutine _jumpCoroutine = null;
    private Coroutine _fallCoroutine = null;
    private Coroutine _rollCoroutine = null;

    void Start()
    {
        if (_trackTransforms.Length == 0) {
            print("ERROR: there are no tracks");
        }

        int middleTrackIndex = Mathf.RoundToInt(_trackTransforms.Length / 2);
        float middleTrackX = _trackTransforms[middleTrackIndex].position.x;
        gameObject.transform.position = new Vector3(middleTrackX, 0, 0);
        _activeTrack = middleTrackIndex;

        _moveAction = InputSystem.actions.FindAction("Move");
        WallHitAction += OnWallHit;
        _initialY = transform.position.y;
    }


    void Update()
    {
        Vector2 moveState = _moveAction.ReadValue<Vector2>();

        if (moveState.x != 0 && !_trackChangeLock ||
            moveState.x != 0 && _trackChangeLock && _trackChangeTime + _trackChangeDuration < Time.time)
        {
            changeTrack(moveState.x > 0 ? true : false, _trackChangeDuration);
        }

        if (moveState.y > 0 && canJump())
        {
            print("Jump");
            _jumpCoroutine = StartCoroutine(jump());
        }

        if (moveState.y < 0)
        {
            roll();
        }

        if (_jumpCoroutine == null && !_rollLock)
        {
            if (TryGetGroundY(out float groundY))
            {
                float dy = groundY - transform.position.y;

                if (dy >= -_stepTolerance)         
                {
                    if (_fallCoroutine != null) { StopCoroutine(_fallCoroutine); _fallCoroutine = null; }
                    SetY(groundY);
                }
                else if (_fallCoroutine == null)
                {
                    _fallCoroutine = StartCoroutine(fall(_fallSpeed));
                }
            }
            else if (_fallCoroutine == null)
            {
                _fallCoroutine = StartCoroutine(fall(_fallSpeed));
            }
        }
    }

    // hitting a wall from a side will be detected via trigger so it doesn't have to be handled here at all
    private void changeTrack(bool increment, float duration)
    {
        if (_trackChangeCoroutine != null)
        {
            StopCoroutine(_trackChangeCoroutine);
        }

        _trackChangeTime = Time.time;
        _trackChangeLock = true;
        _activeTrack = increment ? ++_activeTrack : --_activeTrack;
        _trackChangeCoroutine = StartCoroutine(
            changeTrackAsync(_trackTransforms[_activeTrack].position.x, duration));
    }

    private void OnWallHit(bool increment)
    {
        changeTrack(increment, _wallBounceDuration);
    }

    private IEnumerator changeTrackAsync(float x, float duration)
    {
        float elapsed = 0;
        float startX = transform.position.x;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newX = Mathf.Lerp(startX, x, elapsed / duration);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);

            yield return null;
        }

        _trackChangeLock = false;
        _trackChangeCoroutine = null;
    }

    private IEnumerator jump()
    {
        float startY = transform.position.y;
        yield return LerpY(startY, startY + _jumpHeight, _jumpDuration);
        _jumpCoroutine = null;
    }

    private bool canJump() => TryGetGroundY(out _);

    private IEnumerator fall(float speed)
    {
        while (true)
        {
            if (TryGetGroundY(out float groundY) && transform.position.y <= groundY)
            {
                SetY(groundY);
                break;
            }
            SetY(transform.position.y - speed * Time.deltaTime);
            yield return null;
        }
        _fallCoroutine = null;
    }

    private void roll()
    {
        if (_rollLock) return;

        _rollTime = Time.time;
        _rollLock = true;

        if (_jumpCoroutine != null)
        {
            StopCoroutine(_jumpCoroutine);
            _jumpCoroutine = null;
        }

        if (_rollCoroutine != null) StopCoroutine(_rollCoroutine);
        _rollCoroutine = StartCoroutine(rollAsync());
    }

    private IEnumerator rollAsync()
    {
        if (!canJump())
        {
            if (_fallCoroutine != null) StopCoroutine(_fallCoroutine);
            _fallCoroutine = StartCoroutine(fall(_fallSpeed * _rollFallMultiplier));
            yield return _fallCoroutine;
            _fallCoroutine = null;
        }

        float targetY = _initialY + _rollHeight;

        yield return LerpY(transform.position.y, targetY, _rollDuration);
        yield return LerpY(transform.position.y, _initialY, _rollDuration);

        transform.position = new Vector3(transform.position.x, _initialY, transform.position.z);
        _rollLock = false;
        _rollCoroutine = null;
    }

    private IEnumerator LerpY(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newY = Mathf.Lerp(from, to, elapsed / duration);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            yield return null;
        }
    }

    private void SetY(float y) =>
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

    private bool TryGetGroundY(out float groundY)
    {
        Vector3 origin = transform.position + Vector3.up * _rayStartHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                            _rayStartHeight + _rayLength, ~_playerMask))
        {
            groundY = hit.point.y+1;
            return true;
        }
        groundY = 0f;
        return false;
    }

    private void OnDestroy()
    {
        WallHitAction-= OnWallHit;
    }
}
