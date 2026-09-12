using System;

public static class EventBus
{
    public static event Action DeathEvent;
    public static event Action<bool> WallSideHitEvent;
    public static event Action WallFrontHitEvent;
    public static event Action KillZoneHitEvent;
    public static event Action CoinPickedUpEvent;
    public static event Action<PowerUpType> PowerUpPickedUpEvent;
    public static event Action<PowerUpEffect> PowerUpActivatedEvent;
    public static event Action<PowerUpEffect> PowerUpDeactivatedEvent;
    public static event Action<GestureType> GestureDetectedEvent;
    public static event Action<int> PlayerLaneChangeStartedEvent;
    public static event Action PlayerJumpStartedEvent;
    public static event Action PlayerRollStartedEvent;
    public static event Action PlayerRollEndedEvent;
    public static event Action<bool> PlayerGroundedChangedEvent;

    public static void PlayerDeath()
    {
        DeathEvent?.Invoke();
    }

    public static void PlayerLaneChangeStarted(int direction)
    {
        PlayerLaneChangeStartedEvent?.Invoke(direction);
    }

    public static void PlayerJumpStarted()
    {
        PlayerJumpStartedEvent?.Invoke();
    }

    public static void PlayerRollStarted()
    {
        PlayerRollStartedEvent?.Invoke();
    }

    public static void PlayerRollEnded()
    {
        PlayerRollEndedEvent?.Invoke();
    }

    public static void PlayerGroundedChanged(bool grounded)
    {
        PlayerGroundedChangedEvent?.Invoke(grounded);
    }

    public static void WallSideHit(bool isOnRight)
    {
        WallSideHitEvent?.Invoke(isOnRight);
    }

    public static void FrontWallHit()
    {
        WallFrontHitEvent?.Invoke();
    }

    public static void KillZoneHit()
    {
        KillZoneHitEvent?.Invoke();
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

    public static void GestureDetected(GestureType gesture)
    {
        GestureDetectedEvent?.Invoke(gesture);
    }
}
