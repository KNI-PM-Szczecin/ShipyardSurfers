using UnityEngine;

public class Coin : MonoBehaviour
{
    private bool _pickedUp;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Collect();
    }

    public void Collect()
    {
        if (_pickedUp) return;

        _pickedUp = true;
        EventBus.CoinPickedUp();
        Destroy(gameObject);
    }
}
