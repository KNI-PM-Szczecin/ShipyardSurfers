using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class MenuPlayCheck
{
    private const string PENDING_MARKER = "Library/MenuPlayCheckPending";
    private const string ENTRY_METHOD = "MenuPlayCheck.RunBatch";
    private const string SHOTS_ARGUMENT = "-menuShotsOut";
    private const string GAME_SCENE_NAME = "GameScene";
    private const float SWAY_SAMPLE_SECONDS = 0.3f;
    private const float PRESS_START_SECONDS = 1.6f;
    private const float FIRST_FALL_SHOT_SECONDS = 2.2f;
    private const float SECOND_FALL_SHOT_SECONDS = 2.7f;
    private const float VERIFY_SECONDS = 4.5f;
    private const float MIN_SWAY_DEGREES = 0.3f;
    private const float MIN_FALL = 0.5f;
    private static readonly Vector2Int ShotSize = new Vector2Int(960, 540);

    private static readonly List<string> Problems = new List<string>();
    private static double _startedAt;
    private static bool _armed;
    private static int _step;
    private static Quaternion _swaySample;
    private static float _buoyMin = float.MaxValue;
    private static float _buoyMax = float.MinValue;
    private static float _containerYAtRelease;
    private static string _shots;

    static MenuPlayCheck()
    {
        if (!IsBatchRun() || !File.Exists(PENDING_MARKER)) return;

        EditorApplication.update += ArmWhenPlaying;
    }

    public static void RunBatch()
    {
        try
        {
            EditorSceneManager.OpenScene(MainMenuUiBuilder.SCENE_PATH, OpenSceneMode.Single);
            File.WriteAllText(PENDING_MARKER, DateTime.UtcNow.ToString("O"));
            EditorApplication.update += ArmWhenPlaying;
            EditorApplication.isPlaying = true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static bool IsBatchRun()
    {
        if (!Application.isBatchMode) return false;

        foreach (string argument in Environment.GetCommandLineArgs())
        {
            if (argument == ENTRY_METHOD) return true;
        }

        return false;
    }

    private static void ArmWhenPlaying()
    {
        if (!EditorApplication.isPlaying) return;

        EditorApplication.update -= ArmWhenPlaying;
        if (File.Exists(PENDING_MARKER)) File.Delete(PENDING_MARKER);
        if (_armed) return;

        _armed = true;
        _startedAt = EditorApplication.timeSinceStartup;
        _shots = ArgumentValue(SHOTS_ARGUMENT);
        if (!string.IsNullOrEmpty(_shots)) Directory.CreateDirectory(_shots);
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Tick;
        Debug.Log($"{nameof(MenuPlayCheck)}: armed in '{SceneManager.GetActiveScene().name}'");
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Problems.Add($"{type}: {condition}");
    }

    private static void Tick()
    {
        float elapsed = (float)(EditorApplication.timeSinceStartup - _startedAt);
        if (_step <= 1) TrackBuoy();

        switch (_step)
        {
            case 0 when elapsed >= SWAY_SAMPLE_SECONDS:
                _swaySample = HookRotation();
                _step = 1;
                break;

            case 1 when elapsed >= PRESS_START_SECONDS:
                ReportSway();
                Shot("menu_sway.png");
                PressStart();
                _step = 2;
                break;

            case 2 when elapsed >= FIRST_FALL_SHOT_SECONDS:
                Shot("menu_fall_1.png");
                _step = 3;
                break;

            case 3 when elapsed >= SECOND_FALL_SHOT_SECONDS:
                ReportFall();
                Shot("menu_fall_2.png");
                _step = 4;
                break;

            case 4 when elapsed >= VERIFY_SECONDS:
                Finish();
                break;
        }
    }

    private static Quaternion HookRotation()
    {
        ContainerDrop drop = Object.FindFirstObjectByType<ContainerDrop>();
        if (drop != null) return drop.transform.localRotation;

        Problems.Add("no ContainerDrop (hook pivot) in the menu scene");
        return Quaternion.identity;
    }

    private static void ReportSway()
    {
        float delta = Quaternion.Angle(_swaySample, HookRotation());
        ContainerDrop drop = Object.FindFirstObjectByType<ContainerDrop>();
        Animator animator = drop != null ? drop.GetComponent<Animator>() : null;
        string clip = animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "none";
        int smoke = 0;
        foreach (ParticleSystem system in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)) smoke += system.particleCount;

        int buoys = BuoyCount();
        float buoyRange = buoys > 0 && _buoyMax > _buoyMin ? _buoyMax - _buoyMin : 0f;

        Debug.Log($"{nameof(MenuPlayCheck)}: hook swung {delta:0.00} degrees between t={SWAY_SAMPLE_SECONDS}s and t={PRESS_START_SECONDS}s " +
                  $"(animator controller: {clip}), fog={RenderSettings.fog}, smokeParticles={smoke}, buoys={buoys} firstBuoyBobRange={buoyRange:0.000}");
        if (delta < MIN_SWAY_DEGREES) Problems.Add("the container does not sway");
        if (clip == "none") Problems.Add("the hook pivot has no Animator controller driving the sway");
        if (smoke == 0) Problems.Add("the ship emits no smoke particles");
        if (buoys > 0 && buoyRange < 0.05f) Problems.Add("the buoys do not bob");
        DescribeChildAnimators();
    }

    private static void TrackBuoy()
    {
        float height = BuoyHeight();
        if (float.IsNaN(height)) return;

        _buoyMin = Mathf.Min(_buoyMin, height);
        _buoyMax = Mathf.Max(_buoyMax, height);
    }

    private static void DescribeChildAnimators()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (!root.name.StartsWith("BuoyBob_", StringComparison.Ordinal)) continue;

            Describe(root.transform);
        }

        GameObject orbit = GameObject.Find("GullOrbit_0");
        if (orbit != null) Describe(orbit.transform);
    }

    private static void Describe(Transform wrapper)
    {
        Animator animator = wrapper.GetComponent<Animator>();
        Transform child = wrapper.childCount > 0 ? wrapper.GetChild(0) : null;
        string state = "no animator";
        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
            string clip = clips.Length > 0 && clips[0].clip != null ? clips[0].clip.name : "none";
            state = $"enabled={animator.isActiveAndEnabled} controller={(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "none")} " +
                    $"clip={clip} normalizedTime={info.normalizedTime:0.00} length={info.length:0.0} speed={animator.speed} bound={animator.hasBoundPlayables} layers={animator.layerCount}";
        }

        string childInfo = child == null
            ? "no child"
            : $"child='{child.name}' localPos={child.localPosition:0.000} localEuler={child.localEulerAngles:0.0} childAnimators={child.GetComponentsInChildren<Animator>(true).Length} childAnimations={child.GetComponentsInChildren<Animation>(true).Length}";

        Debug.Log($"{nameof(MenuPlayCheck)}: {wrapper.name}: {state}; {childInfo}");
    }

    private static int BuoyCount()
    {
        int count = 0;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.StartsWith("BuoyBob_", StringComparison.Ordinal)) count++;
        }

        return count;
    }

    private static float BuoyHeight()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.StartsWith("BuoyBob_", StringComparison.Ordinal) && root.transform.childCount > 0) return root.transform.GetChild(0).localPosition.y;
        }

        return float.NaN;
    }

    private static void PressStart()
    {
        MenuController menu = Object.FindFirstObjectByType<MenuController>();
        ContainerDrop drop = Object.FindFirstObjectByType<ContainerDrop>();
        if (menu == null || drop == null)
        {
            Problems.Add("menu scene lacks MenuController or ContainerDrop");
            return;
        }

        var button = new SerializedObject(menu).FindProperty("_startGameButton").objectReferenceValue as Button;
        var container = new SerializedObject(drop).FindProperty("_container").objectReferenceValue as Rigidbody;
        if (button == null || container == null)
        {
            Problems.Add("start button or container rigidbody is not wired");
            return;
        }

        _containerYAtRelease = container.position.y;
        button.onClick.Invoke();
        Debug.Log($"{nameof(MenuPlayCheck)}: pressed Start at container y={_containerYAtRelease:0.00}, dropped={drop.HasDropped}");
        if (!drop.HasDropped) Problems.Add("pressing Start did not snap the rope");
    }

    private static void ReportFall()
    {
        ContainerDrop drop = Object.FindFirstObjectByType<ContainerDrop>();
        var container = drop != null ? new SerializedObject(drop).FindProperty("_container").objectReferenceValue as Rigidbody : null;
        if (container == null)
        {
            Problems.Add("container vanished before the fall could be measured");
            return;
        }

        float fall = _containerYAtRelease - container.position.y;
        Debug.Log($"{nameof(MenuPlayCheck)}: container fell {fall:0.00} units, kinematic={container.isKinematic}, parent={(container.transform.parent != null ? container.transform.parent.name : "none")}, scene={SceneManager.GetActiveScene().name}");
        if (fall < MIN_FALL) Problems.Add($"container fell only {fall:0.00} units");
        if (SceneManager.GetActiveScene().name != MainMenuSceneName()) Problems.Add("game scene loaded before the fall finished");
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;

        string scene = SceneManager.GetActiveScene().name;
        Debug.Log($"{nameof(MenuPlayCheck)}: scene after the drop delay = '{scene}', problems={Problems.Count}");
        if (scene != GAME_SCENE_NAME) Problems.Add($"expected '{GAME_SCENE_NAME}' after the drop delay, got '{scene}'");
        foreach (string problem in Problems) Debug.Log($"{nameof(MenuPlayCheck)}: {problem}");

        bool healthy = Problems.Count == 0;
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(healthy ? 0 : 2);
    }

    private static string MainMenuSceneName() => Path.GetFileNameWithoutExtension(MainMenuUiBuilder.SCENE_PATH);

    private static void Shot(string fileName)
    {
        if (string.IsNullOrEmpty(_shots)) return;

        Camera camera = Camera.main;
        if (camera == null)
        {
            Problems.Add($"no main camera for {fileName}");
            return;
        }

        SceneCapture.RenderToPng(camera, ShotSize, Path.Combine(_shots, fileName));
    }

    private static string ArgumentValue(string name)
    {
        string[] arguments = Environment.GetCommandLineArgs();

        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i] == name) return arguments[i + 1];
        }

        return null;
    }
}
