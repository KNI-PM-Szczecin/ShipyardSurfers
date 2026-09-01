using UnityEngine;

public class Coin : MonoBehaviour
{
    private bool _pickedUp;

    private void OnTriggerEnter(Collider other)
    {
        if (_pickedUp || !other.CompareTag("Player")) return;

        _pickedUp = true;
        EventBus.CoinPickedUp();
        Destroy(gameObject);
    }
}
