using UnityEngine;

public class SideWallCollisionDetector : MonoBehaviour
{
    public bool IsOnRight;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MovmentController.WallHitAction?.Invoke(IsOnRight);
        }
    }
}
