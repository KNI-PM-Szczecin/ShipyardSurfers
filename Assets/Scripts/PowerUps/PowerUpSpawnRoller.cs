using System;
using UnityEngine;

public static class PowerUpSpawnRoller
{
    private const float MAX_ROLL = 100f;

    private static readonly PowerUpType[] AllTypes = (PowerUpType[])Enum.GetValues(typeof(PowerUpType));

    public static bool TryRoll(int coinCount, out int coinIndex, out PowerUpType type)
    {
        for (coinIndex = 0; coinIndex < coinCount; coinIndex++)
        {
            if (TryRoll(out type)) return true;
        }

        coinIndex = -1;
        type = default;
        return false;
    }

    private static bool TryRoll(out PowerUpType type)
    {
        float roll = UnityEngine.Random.Range(0f, MAX_ROLL);
        float cumulativeChance = 0f;

        foreach (PowerUpType candidate in AllTypes)
        {
            cumulativeChance += PowerUpSettings.GetSpawnChance(candidate);
            if (roll < cumulativeChance)
            {
                type = candidate;
                return true;
            }
        }

        type = default;
        return false;
    }
}
