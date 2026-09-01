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

    public Observable<float> Score = new Observable<float>(0f);

    private float _currentSpeed;
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
        SegmentMover.MoveSpeed = StartGameSpeed;
        _currentSpeed = StartGameSpeed;
        _lastSpeedChange = Time.time;

        EventBus.DeathEvent += StopGame;
        EventBus.CoinPickedUpEvent += AddCoinScore;
    }

    private void AddCoinScore(System.Object o, EventArgs e)
    {
        Score.Value += COIN_SCORE_VALUE;
    }

    private void Update()
    {
        Score.Value += SCORE_ITERATOR_VALUE * _currentSpeed * Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (_lastSpeedChange + SPEED_CHANGE_INTERVAL > Time.time) { return; }
        if (_currentSpeed > MaxGameSpeed) { return; }

        _currentSpeed = _currentSpeed * GameSpeedMultiplayer;
        SegmentMover.MoveSpeed = _currentSpeed;
        _lastSpeedChange = Time.time;

        
    }

    private void StopGame(System.Object o, EventArgs e)
    {
        _currentSpeed = 0f;
        SegmentMover.MoveSpeed = 0;
        GameSpeedMultiplayer = 0f;
    }

    private void OnDestroy()
    {
        EventBus.DeathEvent -= StopGame;
        EventBus.CoinPickedUpEvent -= AddCoinScore;
    }
}
