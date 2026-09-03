using UnityEngine;

[RequireComponent(typeof(GameManager))]
public class DoublePointsEffect : PowerUpEffect
{
    private GameManager _gameManager;

    public override PowerUpType Type => PowerUpType.DoublePoints;

    private void Awake()
    {
        _gameManager = GetComponent<GameManager>();
    }

    protected override void OnActivated()
    {
        _gameManager.ScoreMultiplier = PowerUpSettings.DOUBLE_POINTS_MULTIPLIER;
    }

    protected override void OnDeactivated()
    {
        _gameManager.ScoreMultiplier = 1f;
    }
}
