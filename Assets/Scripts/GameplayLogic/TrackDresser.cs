using System.Collections;
using UnityEngine;

public sealed class TrackDresser
{
    private const string WALLS_NAME = "Walls";
    private const string BACKDROP_NAME = "Backdrop";
    private const float WALL_GAP = 0.05f;
    private const float MIN_SPACING = 0.4f;
    private const int MAX_PIECES_PER_PASS = 400;
    private const int PIECES_PER_STEP = 16;

    public void Dress(Transform segment, TrackApperenceSO look, float floorLocalY, float zNearLocal, float zFarLocal)
    {
        IEnumerator steps = DressSteps(segment, look, floorLocalY, zNearLocal, zFarLocal).GetEnumerator();
        while (steps.MoveNext()) { }
    }

    public IEnumerable DressSteps(Transform segment, TrackApperenceSO look, float floorLocalY, float zNearLocal, float zFarLocal)
    {
        if (look == null || segment == null) yield break;

        if (look.HasWallSegments)
        {
            Transform walls = CreateGroup(segment, WALLS_NAME);
            foreach (object step in PlaceWalls(walls, look, floorLocalY, zNearLocal, zFarLocal, -1f)) yield return step;
            foreach (object step in PlaceWalls(walls, look, floorLocalY, zNearLocal, zFarLocal, 1f)) yield return step;
        }

        if (look.BackdropProps == null || look.BackdropProps.Length == 0) yield break;

        Transform backdrop = CreateGroup(segment, BACKDROP_NAME);
        int passes = Mathf.Max(1, look.BackdropPasses);

        for (int pass = 0; pass < passes; pass++)
        {
            foreach (object step in PlaceBackdrop(backdrop, look, floorLocalY, zNearLocal, zFarLocal, -1f)) yield return step;
            foreach (object step in PlaceBackdrop(backdrop, look, floorLocalY, zNearLocal, zFarLocal, 1f)) yield return step;
        }
    }

    private static Transform CreateGroup(Transform segment, string name)
    {
        var group = new GameObject(name).transform;
        group.SetParent(segment, false);
        group.localPosition = Vector3.zero;
        group.localRotation = Quaternion.identity;
        group.localScale = Vector3.one;
        return group;
    }

    private static IEnumerable PlaceWalls(Transform parent, TrackApperenceSO look, float floorLocalY, float zNearLocal, float zFarLocal, float side)
    {
        float z = zNearLocal;
        int guard = 0;

        while (z < zFarLocal && guard++ < MAX_PIECES_PER_PASS)
        {
            GameObject prefab = look.WallSegmentPrefabs[Random.Range(0, look.WallSegmentPrefabs.Length)];
            if (prefab == null) yield break;

            GameObject piece = Object.Instantiate(prefab, parent);
            if (!RendererBoundsUtility.TryGetVisibleBounds(piece, out Bounds bounds) || bounds.size.z <= 0.01f)
            {
                Object.Destroy(piece);
                yield break;
            }

            Vector3 localCenter = parent.InverseTransformPoint(bounds.center);
            Vector3 localSize = parent.InverseTransformVector(bounds.size);
            float depth = Mathf.Abs(localSize.z);

            var target = new Vector3(side * (look.WallOffset + Mathf.Abs(localSize.x) * 0.5f),
                floorLocalY + Mathf.Abs(localSize.y) * 0.5f,
                z + depth * 0.5f);
            piece.transform.localPosition += target - localCenter;

            z += depth + WALL_GAP;

            if (guard % PIECES_PER_STEP == 0) yield return null;
        }
    }

    private static IEnumerable PlaceBackdrop(Transform parent, TrackApperenceSO look, float floorLocalY, float zNearLocal, float zFarLocal, float side)
    {
        float totalWeight = 0f;
        foreach (BackdropProp prop in look.BackdropProps) totalWeight += Mathf.Max(0f, prop.Weight);
        if (totalWeight <= 0f) yield break;

        float spacingMin = Mathf.Max(MIN_SPACING, look.BackdropSpacingMin);
        float spacingMax = Mathf.Max(spacingMin, look.BackdropSpacingMax);
        float wallOuterX = look.WallOffset + WallThickness(look);

        float z = zNearLocal + Random.Range(0f, spacingMax);
        int guard = 0;

        while (z < zFarLocal && guard++ < MAX_PIECES_PER_PASS)
        {
            Place(parent, Pick(look.BackdropProps, totalWeight), floorLocalY, wallOuterX, side, z);
            z += Random.Range(spacingMin, spacingMax);

            if (guard % PIECES_PER_STEP == 0) yield return null;
        }
    }

    private static void Place(Transform parent, BackdropProp prop, float floorLocalY, float wallOuterX, float side, float z)
    {
        if (prop.Prefab == null) return;

        GameObject instance = Object.Instantiate(prop.Prefab, parent);

        float scale = Random.Range(Mathf.Max(0.01f, prop.ScaleMin), Mathf.Max(0.01f, prop.ScaleMax));
        if (!Mathf.Approximately(scale, 1f)) instance.transform.localScale *= scale;
        if (prop.RandomYaw) instance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        if (!RendererBoundsUtility.TryGetVisibleBounds(instance, out Bounds bounds)) return;

        Vector3 localCenter = parent.InverseTransformPoint(bounds.center);
        Vector3 localSize = parent.InverseTransformVector(bounds.size);
        float distance = Random.Range(prop.MinDistance, Mathf.Max(prop.MinDistance, prop.MaxDistance));

        var target = new Vector3(side * (wallOuterX + distance + Mathf.Abs(localSize.x) * 0.5f),
            floorLocalY + prop.YOffset + Mathf.Abs(localSize.y) * 0.5f,
            z);
        instance.transform.localPosition += target - localCenter;
    }

    private static float WallThickness(TrackApperenceSO look)
    {
        if (!look.HasWallSegments) return 0f;

        GameObject sample = look.WallSegmentPrefabs[0];
        if (sample == null || !RendererBoundsUtility.TryGetVisibleBounds(sample, out Bounds bounds)) return 0f;

        return bounds.size.x;
    }

    private static BackdropProp Pick(BackdropProp[] props, float totalWeight)
    {
        float roll = Random.Range(0f, totalWeight);
        foreach (BackdropProp prop in props)
        {
            roll -= Mathf.Max(0f, prop.Weight);
            if (roll <= 0f) return prop;
        }

        return props[props.Length - 1];
    }
}
