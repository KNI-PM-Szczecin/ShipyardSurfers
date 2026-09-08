using UnityEngine;

public class SegmentMover : MonoBehaviour
{
    public static float MoveSpeed = 0f;

    private void FixedUpdate()
    {
        transform.position -= new Vector3(0, 0, MoveSpeed * Time.fixedDeltaTime);
    }
}
