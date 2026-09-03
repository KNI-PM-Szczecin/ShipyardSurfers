using System;

public static class EventBus
{
    public static event Action DeathEvent;
    public static event Action<bool> WallSideHitEvent;
    public static event Action WallFrontHitEvent;
    public static event Action CoinPickedUpEvent;
    public static event Action<PowerUpType> PowerUpPickedUpEvent;
    public static event Action<PowerUpEffect> PowerUpActivatedEvent;
    public static event Action<PowerUpEffect> PowerUpDeactivatedEvent;

    public static void PlayerDeath()
    {
        DeathEvent?.Invoke();
    }

    public static void WallSideHit(bool isOnRight)
    {
        WallSideHitEvent?.Invoke(isOnRight);
    }

    public static void FrontWallHit()
    {
        WallFrontHitEvent?.Invoke();
    }

    public static void CoinPickedUp()
    {
        CoinPickedUpEvent?.Invoke();
    }

    public static void PowerUpPickedUp(PowerUpType type)
    {
        PowerUpPickedUpEvent?.Invoke(type);
    }

    public static void PowerUpActivated(PowerUpEffect effect)
    {
        PowerUpActivatedEvent?.Invoke(effect);
    }

    public static void PowerUpDeactivated(PowerUpEffect effect)
    {
        PowerUpDeactivatedEvent?.Invoke(effect);
    }
}
