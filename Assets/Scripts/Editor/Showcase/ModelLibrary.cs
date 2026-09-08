using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public enum KenneyKit
{
    Pirate,
    Watercraft,
    Factory
}

public static class ModelLibrary
{
    public const string KENNEY_ROOT = "Assets/ThirdParty/Kenney";
    public const string CRANE_GLB_PATH = "Assets/ThirdParty/PolyPizza/Crane_J-Toastie.glb";

    public static string KitFolder(KenneyKit kit)
    {
        switch (kit)
        {
            case KenneyKit.Pirate: return $"{KENNEY_ROOT}/PirateKit";
            case KenneyKit.Watercraft: return $"{KENNEY_ROOT}/WatercraftKit";
            default: return $"{KENNEY_ROOT}/FactoryKit";
        }
    }

    public static GameObject Spawn(KenneyKit kit, string modelName, Transform parent)
    {
        string path = $"{KitFolder(kit)}/{modelName}.fbx";
        return SpawnAsset(path, parent);
    }

    public static GameObject SpawnAsset(string assetPath, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) throw new FileNotFoundException($"Model '{assetPath}' not found or not imported as a GameObject");

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = Path.GetFileNameWithoutExtension(assetPath);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        return instance;
    }

    public static bool TryGetBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;

            if (found)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            else
            {
                bounds = renderer.bounds;
                found = true;
            }
        }

        return found;
    }

    public static Vector3 Size(GameObject root)
    {
        return TryGetBounds(root, out Bounds bounds) ? bounds.size : Vector3.zero;
    }

    public static void FitHeight(GameObject root, float targetHeight) => FitAxis(root, 1, targetHeight);

    public static void FitWidth(GameObject root, float targetWidth) => FitAxis(root, 0, targetWidth);

    public static void FitDepth(GameObject root, float targetDepth) => FitAxis(root, 2, targetDepth);

    public static void FitWithin(GameObject root, float maxWidth, float maxHeight, float maxDepth)
    {
        if (!TryGetBounds(root, out Bounds bounds)) return;

        float factor = Mathf.Min(maxWidth / Mathf.Max(bounds.size.x, 0.0001f),
            maxHeight / Mathf.Max(bounds.size.y, 0.0001f),
            maxDepth / Mathf.Max(bounds.size.z, 0.0001f));
        root.transform.localScale *= factor;
    }

    public static void StretchDepth(GameObject root, float targetDepth)
    {
        if (!TryGetBounds(root, out Bounds bounds) || bounds.size.z <= 0.0001f) return;

        Vector3 scale = root.transform.localScale;
        root.transform.localScale = new Vector3(scale.x, scale.y, scale.z * (targetDepth / bounds.size.z));
    }

    public static void PlaceBottomCenter(GameObject root, Vector3 worldBottomCenter)
    {
        if (!TryGetBounds(root, out Bounds bounds)) return;

        Vector3 currentBottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        root.transform.position += worldBottomCenter - currentBottomCenter;
    }

    public static void PlaceBottomAt(GameObject root, float worldY)
    {
        if (!TryGetBounds(root, out Bounds bounds)) return;

        root.transform.position += Vector3.up * (worldY - bounds.min.y);
    }

    public static void DeactivateChildrenNamed(GameObject root, params string[] namePrefixes)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root.transform) continue;

            foreach (string prefix in namePrefixes)
            {
                if (child.name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    child.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }

    public static void ScaleHeightTo(GameObject root, float targetHeight)
    {
        Vector3 size = Size(root);
        if (size.y <= 0.0001f) return;

        Vector3 scale = root.transform.localScale;
        root.transform.localScale = new Vector3(scale.x, scale.y * (targetHeight / size.y), scale.z);
    }

    public static float DeckTopY(GameObject root, float centralFraction = 0.3f)
    {
        if (!TryGetBounds(root, out Bounds bounds)) return 0f;

        float centerX = bounds.center.x;
        float centralHalfWidth = bounds.extents.x * centralFraction;
        float deckTop = bounds.min.y;
        bool found = false;

        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null || !filter.gameObject.activeInHierarchy) continue;

            foreach (Vector3 vertex in filter.sharedMesh.vertices)
            {
                Vector3 world = filter.transform.TransformPoint(vertex);
                if (Mathf.Abs(world.x - centerX) <= centralHalfWidth) continue;

                deckTop = Mathf.Max(deckTop, world.y);
                found = true;
            }
        }

        return found ? deckTop : bounds.max.y;
    }

    public static void OrientLongAxisAlongZ(GameObject root)
    {
        Vector3 size = Size(root);
        if (size.x > size.z) root.transform.Rotate(Vector3.up, 90f, Space.World);
    }

    public static void OrientLongAxisAlongX(GameObject root)
    {
        Vector3 size = Size(root);
        if (size.z > size.x) root.transform.Rotate(Vector3.up, 90f, Space.World);
    }

    public static void OrientSlopeTowardsPositiveZ(GameObject root)
    {
        var top = new List<Vector3>();
        var bottom = new List<Vector3>();
        var vertices = new List<Vector3>();

        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;
            foreach (Vector3 vertex in filter.sharedMesh.vertices)
            {
                vertices.Add(filter.transform.TransformPoint(vertex));
            }
        }

        if (vertices.Count == 0) return;

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        foreach (Vector3 vertex in vertices)
        {
            minY = Mathf.Min(minY, vertex.y);
            maxY = Mathf.Max(maxY, vertex.y);
        }

        float band = (maxY - minY) * 0.2f;
        foreach (Vector3 vertex in vertices)
        {
            if (vertex.y >= maxY - band) top.Add(vertex);
            if (vertex.y <= minY + band) bottom.Add(vertex);
        }

        Vector3 slope = Centroid(top) - Centroid(bottom);
        slope.y = 0f;
        if (slope.sqrMagnitude < 0.0001f) return;

        float yaw = Vector3.SignedAngle(slope, Vector3.forward, Vector3.up);
        root.transform.Rotate(Vector3.up, yaw, Space.World);
    }

    private static Vector3 Centroid(List<Vector3> points)
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 point in points) sum += point;
        return points.Count > 0 ? sum / points.Count : Vector3.zero;
    }

    private static void FitAxis(GameObject root, int axis, float target)
    {
        if (!TryGetBounds(root, out Bounds bounds)) return;

        float current = bounds.size[axis];
        if (current <= 0.0001f) return;

        root.transform.localScale *= target / current;
    }
}
