using UnityEngine;

public static class PowerUpSettings
{
    public const float COIN_MAGNET_SPAWN_CHANCE = 2f;
    public const float SUPER_JUMP_SPAWN_CHANCE = 2f;
    public const float DOUBLE_POINTS_SPAWN_CHANCE = 2f;

    public const float COIN_MAGNET_DURATION = 8f;
    public const float SUPER_JUMP_DURATION = 8f;
    public const float DOUBLE_POINTS_DURATION = 10f;

    public const float MAGNET_RANGE_IN_CELLS = 1f;
    public const float MAGNET_RANGE_MARGIN = 0.5f;
    public const float MAGNET_VERTICAL_RANGE = 3f;
    public const float MAGNET_MIN_PULL_SPEED = 10f;
    public const float MAGNET_PULL_SPEED_FACTOR = 1.5f;
    public const float MAGNET_COLLECT_DISTANCE = 0.75f;

    public const float SUPER_JUMP_HEIGHT_MULTIPLIER = 1.6f;

    public const float DOUBLE_POINTS_MULTIPLIER = 2f;

    public static float GetSpawnChance(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.CoinMagnet: return COIN_MAGNET_SPAWN_CHANCE;
            case PowerUpType.SuperJump: return SUPER_JUMP_SPAWN_CHANCE;
            case PowerUpType.DoublePoints: return DOUBLE_POINTS_SPAWN_CHANCE;
            default:
                Debug.LogError($"Unknown PowerUpType {type} used in GetSpawnChance!");
                return 0f;
        }
    }

    public static float GetDuration(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.CoinMagnet: return COIN_MAGNET_DURATION;
            case PowerUpType.SuperJump: return SUPER_JUMP_DURATION;
            case PowerUpType.DoublePoints: return DOUBLE_POINTS_DURATION;
            default:
                Debug.LogError($"Unknown PowerUpType {type} used in GetDuration!");
                return 0f;
        }
    }
}
