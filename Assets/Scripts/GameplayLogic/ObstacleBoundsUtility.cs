using UnityEngine;

public static class ObstacleBoundsUtility
{
    public static bool TryGetGameplayBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Collider collider in root.GetComponentsInChildren<Collider>())
        {
            if (collider.isTrigger || !collider.enabled) continue;

            Encapsulate(ref bounds, ref found, ColliderBounds(collider));
        }

        return found || RendererBoundsUtility.TryGetVisibleBounds(root, out bounds);
    }

    public static Bounds ColliderBounds(Collider collider)
    {
        if (collider is BoxCollider box) return TransformedBox(box.transform, box.center, box.size);

        return collider.bounds;
    }

    private static Bounds TransformedBox(Transform space, Vector3 center, Vector3 size)
    {
        Vector3 extents = size * 0.5f;
        var bounds = new Bounds(space.TransformPoint(center), Vector3.zero);

        for (int corner = 0; corner < 8; corner++)
        {
            var offset = new Vector3(
                (corner & 1) == 0 ? -extents.x : extents.x,
                (corner & 2) == 0 ? -extents.y : extents.y,
                (corner & 4) == 0 ? -extents.z : extents.z);

            bounds.Encapsulate(space.TransformPoint(center + offset));
        }

        return bounds;
    }

    private static void Encapsulate(ref Bounds bounds, ref bool found, Bounds addition)
    {
        if (found)
        {
            bounds.Encapsulate(addition);
        }
        else
        {
            bounds = addition;
            found = true;
        }
    }
}
