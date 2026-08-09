using UnityEngine;

public class SegmentMover : MonoBehaviour
{
    public static float MoveSpeed = 2;

    private void FixedUpdate()
    {
        transform.position -= new Vector3(0, 0, MoveSpeed);
    }
}
