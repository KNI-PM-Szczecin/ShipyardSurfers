using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class UiFaceliftMenu
{
    private const string MENU_ROOT = "Shipyard Surfers/UI/";
    private const string PENDING_REBUILD_MARKER = "Library/UiRebuildRequested";

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
                RebuildAll();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        };
    }

    [MenuItem(MENU_ROOT + "Rebuild All UI")]
    public static void RebuildAll()
    {
        UiAssetLibrary assets = UiAssetLibrary.Load();
        GameHudUiBuilder.BuildAll(assets);
        CalibrationSceneBuilder.Build(assets);
        MainMenuUiBuilder.Build(assets);
        AssetDatabase.SaveAssets();
        Debug.Log($"{nameof(UiFaceliftMenu)}: UI rebuilt");
    }

    [MenuItem(MENU_ROOT + "Rebuild Calibration Scene")]
    public static void RebuildCalibrationScene()
    {
        CalibrationSceneBuilder.Build(UiAssetLibrary.Load());
        AssetDatabase.SaveAssets();
    }

    [MenuItem(MENU_ROOT + "Rebuild Main Menu")]
    public static void RebuildMainMenu()
    {
        MainMenuUiBuilder.Build(UiAssetLibrary.Load());
        AssetDatabase.SaveAssets();
    }

    [MenuItem(MENU_ROOT + "Rebuild Game HUD")]
    public static void RebuildGameHud()
    {
        GameHudUiBuilder.BuildAll(UiAssetLibrary.Load());
        AssetDatabase.SaveAssets();
    }

    public static void RebuildAllBatch()
    {
        try
        {
            RebuildAll();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
}
