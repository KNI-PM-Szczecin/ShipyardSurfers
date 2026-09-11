using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WorldBuildMenu
{
    private const string MENU_PATH = "Shipyard Surfers/World/Rebuild World (sets + showcase + game scene)";
    private const string PENDING_REBUILD_MARKER = "Library/WorldRebuildRequested";

    [InitializeOnLoadMethod]
    private static void RunPendingRebuild()
    {
        if (!File.Exists(PENDING_REBUILD_MARKER)) return;

        EditorApplication.delayCall += () =>
        {
            File.Delete(PENDING_REBUILD_MARKER);
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            try
            {
                RebuildWorld();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        };
    }

    [MenuItem(MENU_PATH)]
    public static void RebuildWorld()
    {
        ObstacleSetBuilder.BuildAll();
        ObstacleShowcaseSceneBuilder.Build();
        GameSceneSetupTool.Apply();
        MenuSceneSetupTool.Apply();
        AssetDatabase.SaveAssets();
        Debug.Log($"{nameof(WorldBuildMenu)}: world rebuilt");
    }

    public static void RebuildWorldBatch()
    {
        try
        {
            ObstacleSetBuilder.BuildAll();
            ObstacleShowcaseSceneBuilder.Build();
            ObstacleShowcaseSceneBuilder.CaptureAll(ObstacleShowcaseSceneBuilder.OutputDirectoryFromArguments());
            GameSceneSetupTool.Apply();
            MenuSceneSetupTool.Apply();
            AssetDatabase.SaveAssets();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
}
