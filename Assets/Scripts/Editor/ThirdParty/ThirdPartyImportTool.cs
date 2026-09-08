using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ThirdPartyImportTool
{
    private const string MENU_ROOT = "Shipyard Surfers/Third Party/";
    private const string SKYBOX_PACKAGE_RELATIVE_PATH = @"Unity\Asset Store-5.x\Render Knight\Textures MaterialsSkies\Fantasy Skybox FREE.unitypackage";
    public const string SKYBOX_FOLDER = "Assets/Fantasy Skybox FREE";

    [MenuItem(MENU_ROOT + "Import Fantasy Skybox FREE (from Asset Store cache)")]
    public static void ImportSkybox()
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), SKYBOX_PACKAGE_RELATIVE_PATH);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"{nameof(ThirdPartyImportTool)}: package not found at {path}. Download it once through Package Manager > My Assets.");
            return;
        }

        if (AssetDatabase.IsValidFolder(SKYBOX_FOLDER))
        {
            Debug.Log($"{nameof(ThirdPartyImportTool)}: {SKYBOX_FOLDER} already present, skipping import");
            return;
        }

        AssetDatabase.ImportPackage(path, false);
        AssetDatabase.Refresh();
        Debug.Log($"{nameof(ThirdPartyImportTool)}: imported {path}");
    }

    public static void ImportSkyboxBatch()
    {
        try
        {
            ImportSkybox();
            AssetDatabase.SaveAssets();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static Material FindSkyboxMaterial(string preferredNameFragment)
    {
        if (!AssetDatabase.IsValidFolder(SKYBOX_FOLDER)) return null;

        Material fallback = null;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { SKYBOX_FOLDER }))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material == null || material.shader == null || !material.shader.name.StartsWith("Skybox")) continue;

            if (fallback == null) fallback = material;
            if (material.name.IndexOf(preferredNameFragment, StringComparison.OrdinalIgnoreCase) >= 0) return material;
        }

        return fallback;
    }
}
