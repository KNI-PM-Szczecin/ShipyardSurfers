using System;
using System.Collections.Generic;

public static class EventBus
{
    public static event EventHandler DeathEvent;

    public static void PlayerDeath()
    {
        DeathEvent?.Invoke(null, EventArgs.Empty);
    }
}