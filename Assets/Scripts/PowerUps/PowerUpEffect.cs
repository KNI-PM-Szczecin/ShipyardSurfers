using UnityEngine;

public abstract class PowerUpEffect : MonoBehaviour
{
    public abstract PowerUpType Type { get; }

    public Observable<float> RemainingTime { get; } = new Observable<float>(0f);

    public float Duration => PowerUpSettings.GetDuration(Type);

    public bool IsActive => RemainingTime.Value > 0f;

    protected virtual void OnEnable()
    {
        EventBus.PowerUpPickedUpEvent += OnPowerUpPickedUp;
    }

    protected virtual void OnDisable()
    {
        EventBus.PowerUpPickedUpEvent -= OnPowerUpPickedUp;
        if (IsActive) Deactivate();
    }

    protected virtual void Update()
    {
        if (!IsActive) return;

        RemainingTime.Value = Mathf.Max(0f, RemainingTime.Value - Time.deltaTime);
        if (!IsActive) Deactivate();
    }

    protected virtual void OnActivated() { }

    protected virtual void OnDeactivated() { }

    private void OnPowerUpPickedUp(PowerUpType type)
    {
        if (type != Type) return;

        bool wasActive = IsActive;
        RemainingTime.Value = Duration;

        if (wasActive) return;

        OnActivated();
        EventBus.PowerUpActivated(this);
    }

    private void Deactivate()
    {
        RemainingTime.Value = 0f;
        OnDeactivated();
        EventBus.PowerUpDeactivated(this);
    }
}
