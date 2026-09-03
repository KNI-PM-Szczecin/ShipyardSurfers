using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    [SerializeField] private PowerUpType _type;

    private bool _pickedUp;

    private void OnTriggerEnter(Collider other)
    {
        if (_pickedUp || !other.CompareTag("Player")) return;

        _pickedUp = true;
        EventBus.PowerUpPickedUp(_type);
        Destroy(gameObject);
    }
}
