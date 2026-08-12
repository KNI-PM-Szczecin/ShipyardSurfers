using UnityEngine;

public class FrontWallCollisionDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            EventBus.FrontWallHit();
        }
    }
}
