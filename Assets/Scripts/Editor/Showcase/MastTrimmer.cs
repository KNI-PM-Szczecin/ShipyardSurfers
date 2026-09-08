using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MastTrimmer
{
    public const string GENERATED_FOLDER = "Assets/ThirdParty/Generated";

    private const int SLICE_COUNT = 48;
    private const float DECK_AREA_FRACTION = 0.35f;
    private const float DECK_MARGIN = 0.05f;
    private const float CHILD_TOLERANCE = 0.02f;

    public static void TrimAboveDeck(GameObject ship, string assetName)
    {
        MeshFilter[] filters = ship.GetComponentsInChildren<MeshFilter>(true);
        MeshFilter hull = LargestMesh(filters);
        if (hull == null || hull.sharedMesh == null) return;

        float cutY = FindDeckY(hull.sharedMesh);
        Mesh trimmed = Trim(hull.sharedMesh, cutY, assetName);
        if (trimmed != null) hull.sharedMesh = trimmed;

        foreach (MeshFilter filter in filters)
        {
            if (filter == hull || filter.sharedMesh == null) continue;

            float top = filter.transform.localPosition.y + filter.sharedMesh.bounds.max.y;
            if (top > cutY + CHILD_TOLERANCE) filter.gameObject.SetActive(false);
        }
    }

    private static MeshFilter LargestMesh(MeshFilter[] filters)
    {
        MeshFilter largest = null;
        int mostVertices = 0;

        foreach (MeshFilter filter in filters)
        {
            if (filter.sharedMesh == null || filter.sharedMesh.vertexCount <= mostVertices) continue;

            mostVertices = filter.sharedMesh.vertexCount;
            largest = filter;
        }

        return largest;
    }

    private static float FindDeckY(Mesh mesh)
    {
        Bounds bounds = mesh.bounds;
        float span = bounds.size.y;
        float fullArea = bounds.size.x * bounds.size.z;
        if (span <= 0.0001f || fullArea <= 0.0001f) return bounds.max.y;

        var minX = new float[SLICE_COUNT];
        var maxX = new float[SLICE_COUNT];
        var minZ = new float[SLICE_COUNT];
        var maxZ = new float[SLICE_COUNT];
        for (int i = 0; i < SLICE_COUNT; i++)
        {
            minX[i] = float.MaxValue;
            maxX[i] = float.MinValue;
            minZ[i] = float.MaxValue;
            maxZ[i] = float.MinValue;
        }

        foreach (Vector3 vertex in mesh.vertices)
        {
            int slice = Mathf.Clamp(Mathf.FloorToInt((vertex.y - bounds.min.y) / span * SLICE_COUNT), 0, SLICE_COUNT - 1);
            minX[slice] = Mathf.Min(minX[slice], vertex.x);
            maxX[slice] = Mathf.Max(maxX[slice], vertex.x);
            minZ[slice] = Mathf.Min(minZ[slice], vertex.z);
            maxZ[slice] = Mathf.Max(maxZ[slice], vertex.z);
        }

        for (int i = SLICE_COUNT - 1; i >= 0; i--)
        {
            if (maxX[i] <= minX[i] || maxZ[i] <= minZ[i]) continue;
            if ((maxX[i] - minX[i]) * (maxZ[i] - minZ[i]) < fullArea * DECK_AREA_FRACTION) continue;

            return bounds.min.y + (i + 1f) / SLICE_COUNT * span + DECK_MARGIN;
        }

        return bounds.max.y;
    }

    private static Mesh Trim(Mesh source, float cutY, string assetName)
    {
        Vector3[] vertices = source.vertices;
        if (cutY >= source.bounds.max.y) return null;

        var keptIndex = new int[vertices.Length];
        for (int i = 0; i < keptIndex.Length; i++) keptIndex[i] = -1;

        var newVertices = new List<Vector3>();
        var newNormals = new List<Vector3>();
        var newUvs = new List<Vector2>();
        Vector3[] normals = source.normals;
        Vector2[] uvs = source.uv;

        var submeshes = new List<int[]>();
        for (int submesh = 0; submesh < source.subMeshCount; submesh++)
        {
            int[] triangles = source.GetTriangles(submesh);
            var kept = new List<int>(triangles.Length);

            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                if (vertices[triangles[t]].y > cutY || vertices[triangles[t + 1]].y > cutY || vertices[triangles[t + 2]].y > cutY) continue;

                for (int corner = 0; corner < 3; corner++)
                {
                    int original = triangles[t + corner];
                    if (keptIndex[original] < 0)
                    {
                        keptIndex[original] = newVertices.Count;
                        newVertices.Add(vertices[original]);
                        if (normals.Length == vertices.Length) newNormals.Add(normals[original]);
                        if (uvs.Length == vertices.Length) newUvs.Add(uvs[original]);
                    }

                    kept.Add(keptIndex[original]);
                }
            }

            submeshes.Add(kept.ToArray());
        }

        if (newVertices.Count == 0) return null;

        var mesh = new Mesh { name = assetName };
        mesh.SetVertices(newVertices);
        if (newNormals.Count == newVertices.Count) mesh.SetNormals(newNormals);
        if (newUvs.Count == newVertices.Count) mesh.SetUVs(0, newUvs);
        mesh.subMeshCount = submeshes.Count;
        for (int submesh = 0; submesh < submeshes.Count; submesh++) mesh.SetTriangles(submeshes[submesh], submesh);
        mesh.RecalculateBounds();

        return Save(mesh, assetName);
    }

    private static Mesh Save(Mesh mesh, string assetName)
    {
        EnsureFolder(GENERATED_FOLDER);
        string path = $"{GENERATED_FOLDER}/{assetName}.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
