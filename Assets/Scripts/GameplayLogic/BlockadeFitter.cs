using UnityEngine;

[DisallowMultipleComponent]
public class BlockadeFitter : MonoBehaviour
{
    [SerializeField, Tooltip("Visual unit repeated along the blockade instead of stretching it")]
    private Transform _tile;
    [SerializeField, Min(0.01f)] private float _tileDepth = 1f;
    [SerializeField, Tooltip("Colliders resized to the blockade depth and centred on the pivot")]
    private BoxCollider[] _depthColliders;
    [SerializeField] private BoxCollider _frontWall;

    public float TileDepth => _tileDepth;

    public void Fit(float depth)
    {
        if (depth <= 0f) return;

        FitColliders(depth);
        FitTiles(depth);
    }

    private void FitColliders(float depth)
    {
        if (_depthColliders != null)
        {
            foreach (BoxCollider collider in _depthColliders)
            {
                if (collider == null) continue;

                Vector3 size = collider.size;
                collider.size = new Vector3(size.x, size.y, depth);
                SetLocalZ(collider.transform, 0f);
            }
        }

        if (_frontWall != null)
        {
            SetLocalZ(_frontWall.transform, -(depth + _frontWall.size.z) * 0.5f);
        }
    }

    private void FitTiles(float depth)
    {
        if (_tile == null) return;

        int count = Mathf.Max(1, Mathf.RoundToInt(depth / _tileDepth));
        float slotDepth = depth / count;
        float squeeze = slotDepth / _tileDepth;

        for (int i = 0; i < count; i++)
        {
            Transform tile = i == 0 ? _tile : Instantiate(_tile, _tile.parent);
            Vector3 scale = tile.localScale;
            tile.localScale = new Vector3(scale.x, scale.y, scale.z * squeeze);
            SetLocalZ(tile, -depth * 0.5f + (i + 0.5f) * slotDepth);
        }
    }

    private static void SetLocalZ(Transform target, float z)
    {
        Vector3 position = target.localPosition;
        target.localPosition = new Vector3(position.x, position.y, z);
    }
}
