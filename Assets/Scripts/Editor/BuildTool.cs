using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildTool
{
    public const string BUILD_FOLDER = "builds";

    private const string MENU_PATH = "Shipyard Surfers/Build/Windows Player";
    private const string OUTPUT_ARGUMENT = "-buildOut";
    private const string PLAYER_NAME = "ShipyardSurfers";

    [MenuItem(MENU_PATH)]
    public static void BuildWindows()
    {
        BuildReport report = Build(DefaultOutputDirectory());
        if (report.summary.result == BuildResult.Succeeded) EditorUtility.RevealInFinder(report.summary.outputPath);
    }

    public static void BuildWindowsBatch()
    {
        try
        {
            BuildReport report = Build(OutputDirectoryFromArguments());
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static BuildReport Build(string outputDirectory)
    {
        string[] scenes = EnabledScenes();
        if (scenes.Length == 0) throw new InvalidOperationException("No enabled scenes in the build settings");

        Directory.CreateDirectory(outputDirectory);
        string outputPath = Path.Combine(outputDirectory, $"{PLAYER_NAME}.exe");

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Debug.Log($"{nameof(BuildTool)}: {summary.result} in {summary.totalTime:mm\\:ss}, " +
                  $"{summary.totalSize / (1024 * 1024)} MB, {scenes.Length} scenes, starts in '{scenes[0]}' -> {summary.outputPath}");

        foreach (BuildStep step in report.steps)
        {
            foreach (BuildStepMessage message in step.messages)
            {
                if (message.type == LogType.Error || message.type == LogType.Exception)
                {
                    Debug.LogError($"{nameof(BuildTool)}: {message.content}");
                }
            }
        }

        return report;
    }

    private static string[] EnabledScenes()
    {
        var scenes = new List<string>();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled) scenes.Add(scene.path);
        }

        int startup = scenes.IndexOf(MainMenuUiBuilder.SCENE_PATH);
        if (startup > 0)
        {
            scenes.RemoveAt(startup);
            scenes.Insert(0, MainMenuUiBuilder.SCENE_PATH);
        }

        return scenes.ToArray();
    }

    private static string DefaultOutputDirectory()
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, BUILD_FOLDER, PLAYER_NAME);
    }

    private static string OutputDirectoryFromArguments()
    {
        string[] arguments = Environment.GetCommandLineArgs();

        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i] == OUTPUT_ARGUMENT) return arguments[i + 1];
        }

        return DefaultOutputDirectory();
    }
}
