using System;
using System.ComponentModel;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Tooltip("Starting speed")]
    public float StartGameSpeed = 0.3f;
    [Tooltip("Speed is multiplied by this value every 10 seconds")]
    [Range(1f, 10f)]
    public float GameSpeedMultiplayer = 1.3f;
    [Tooltip("Game speed cannot exceed this value")]
    public float MaxGameSpeed = 3.5f;

    [Header("Motion control")]
    [Tooltip("Starting speed used when camera motion control is enabled")]
    public float MotionControlStartGameSpeed = 9f;
    [Tooltip("Speed multiplier per interval used when camera motion control is enabled")]
    [Range(1f, 10f)]
    public float MotionControlGameSpeedMultiplayer = 1.008f;

    public Observable<float> Score = new Observable<float>(0f);

    public float ScoreMultiplier { get; set; } = 1f;

    private float _currentSpeed;
    private float _speedMultiplier;
    private float _lastSpeedChange;
    private const float SPEED_CHANGE_INTERVAL = 1;
    private const float SCORE_ITERATOR_VALUE = 0.5f;
    private const float COIN_SCORE_VALUE = 3f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        bool motionControl = IsMotionControlEnabled();
        _currentSpeed = motionControl ? MotionControlStartGameSpeed : StartGameSpeed;
        _speedMultiplier = motionControl ? MotionControlGameSpeedMultiplayer : GameSpeedMultiplayer;
        SegmentMover.MoveSpeed = _currentSpeed;
        WaterSpeed.instance.ResetSpeed();
        WaterSpeed.instance.SetInterval(SPEED_CHANGE_INTERVAL);
        _lastSpeedChange = Time.time;

        EventBus.DeathEvent += StopGame;
        EventBus.CoinPickedUpEvent += AddCoinScore;
    }

    private void AddCoinScore()
    {
        Score.Value += COIN_SCORE_VALUE * ScoreMultiplier;
    }

    private void Update()
    {
        Score.Value += SCORE_ITERATOR_VALUE * _currentSpeed * ScoreMultiplier * Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (_lastSpeedChange + SPEED_CHANGE_INTERVAL > Time.time) { return; }
        if (_currentSpeed > MaxGameSpeed) { return; }

        _currentSpeed = _currentSpeed * _speedMultiplier;
        WaterSpeed.instance.IncreaseSpeed(_speedMultiplier);
        SegmentMover.MoveSpeed = _currentSpeed;
        _lastSpeedChange = Time.time;
    }

    private static bool IsMotionControlEnabled()
    {
        MotionControlService service = MotionControlService.Instance;
        return service != null && service.Settings != null && service.Settings.Enabled;
    }

    private void StopGame()
    {
        _currentSpeed = 0f;
        SegmentMover.MoveSpeed = 0;
        _speedMultiplier = 0f;
    }

    private void OnDestroy()
    {
        EventBus.DeathEvent -= StopGame;
        EventBus.CoinPickedUpEvent -= AddCoinScore;
    }
}
