using System;
using System.Collections.Generic;

public static class EventBus
{
    public static event EventHandler DeathEvent;
    public static event EventHandler<bool> WallSideHitEvent;
    public static event EventHandler WallFrontHitEvent;

    public static void PlayerDeath()
    {
        DeathEvent?.Invoke(null, EventArgs.Empty);
    }

    public static void WallSideHit(bool isOnRight)
    {
        WallSideHitEvent?.Invoke(null, isOnRight);
    }

    public static void FrontWallHit()
    {
        WallFrontHitEvent?.Invoke(null, EventArgs.Empty);
    }
}