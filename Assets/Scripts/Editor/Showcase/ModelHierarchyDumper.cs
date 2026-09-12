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

    private const string MODELS_ARGUMENT = "-dumpModels";

    public static void DumpBatch()
    {
        try
        {
            var report = new StringBuilder();
            foreach (string path in ModelPaths())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    report.AppendLine($"MODEL {path}: MISSING");
                    continue;
                }

                report.AppendLine($"MODEL {path}");
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                {
                    if (child == prefab.transform) continue;

                    string mesh = child.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null
                        ? $"meshCenter={filter.sharedMesh.bounds.center.ToString("0.00")} meshSize={filter.sharedMesh.bounds.size.ToString("0.00")}"
                        : "no mesh";
                    report.AppendLine($"  NODE {Path(child, prefab.transform)} local={child.localPosition.ToString("0.00")} euler={child.localEulerAngles.ToString("0")} scale={child.localScale.ToString("0.00")} {mesh}");
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

    private static string[] ModelPaths()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i] == MODELS_ARGUMENT) return arguments[i + 1].Split(',');
        }

        var paths = new string[Models.Length];
        for (int i = 0; i < Models.Length; i++) paths[i] = $"{ModelLibrary.KitFolder(Models[i].kit)}/{Models[i].model}.fbx";
        return paths;
    }

    private static string Path(Transform node, Transform root)
    {
        string path = node.name;
        for (Transform parent = node.parent; parent != null && parent != root; parent = parent.parent) path = $"{parent.name}/{path}";
        return path;
    }
}
