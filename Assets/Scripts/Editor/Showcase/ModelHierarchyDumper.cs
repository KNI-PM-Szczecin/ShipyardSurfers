using System;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ModelHierarchyDumper
{
    private static readonly (KenneyKit kit, string model)[] Models =
    {
        (KenneyKit.Pirate, "ship-pirate-small"),
        (KenneyKit.Pirate, "ship-small"),
        (KenneyKit.Pirate, "ship-wreck"),
        (KenneyKit.Pirate, "ship-pirate-large"),
        (KenneyKit.Pirate, "ship-large"),
        (KenneyKit.Pirate, "ship-medium"),
        (KenneyKit.Watercraft, "ship-cargo-a"),
        (KenneyKit.Watercraft, "boat-tug-a"),
        (KenneyKit.Watercraft, "ship-small"),
        (KenneyKit.Watercraft, "ship-cargo-b"),
        (KenneyKit.Watercraft, "ship-ocean-liner-small"),
    };

    public static void DumpBatch()
    {
        try
        {
            var report = new StringBuilder();
            foreach ((KenneyKit kit, string model) in Models)
            {
                string path = $"{ModelLibrary.KitFolder(kit)}/{model}.fbx";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    report.AppendLine($"MODEL {kit}/{model}: MISSING");
                    continue;
                }

                report.AppendLine($"MODEL {kit}/{model}");
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                {
                    if (child == prefab.transform) continue;

                    string size = child.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null
                        ? filter.sharedMesh.bounds.size.ToString("0.00")
                        : "-";
                    report.AppendLine($"  NODE {child.name} local={child.localPosition.ToString("0.00")} meshSize={size}");
                }
            }

            Debug.Log(report.ToString());
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
}
