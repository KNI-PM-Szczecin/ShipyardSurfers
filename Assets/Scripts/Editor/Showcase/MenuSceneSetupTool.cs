using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class MenuSceneSetupTool
{
    private const string MENU_PATH = "Shipyard Surfers/World/Setup Main Menu Scene (crane + container)";
    private const string ROOT_NAME = "MenuDiorama";
    private const string PLACEHOLDER_NAME = "Container";
    private const string MATERIAL_FOLDER = MastTrimmer.GENERATED_FOLDER;
    private const string CONTAINER_MODEL = "cargo-container-b";
    private const string SHIP_MODEL = "ship-cargo-a";
    private static readonly string[] QuayContainerModels = { "cargo-container-a", "cargo-container-c", "cargo-container-b" };

    private static readonly Vector3 ContainerCenter = new Vector3(2f, 0.85f, -5.7f);
    private static readonly Vector2 ScoreboardSize = new Vector2(919f, 517f);
    private static readonly Vector3 ContainerSize = new Vector3(3.2f, 1.8f, 1.25f);
    private const float CONTAINER_YAW = 20f;
    private const float CONTAINER_MASS = 400f;
    private const float CANVAS_FACE_OFFSET = 0.06f;
    private const float CANVAS_FILL = 0.94f;

    private const float ROPE_LENGTH = 5f;
    private const float ROPE_BREAK_FRACTION = 0.75f;
    private const float ROPE_RADIUS = 0.035f;
    private const float SLING_RADIUS = 0.018f;
    private const float SPREADER_HEIGHT = 0.9f;
    private const float SLING_CORNER_INSET = 0.9f;
    private static readonly Vector3 SpreaderSize = new Vector3(0.36f, 0.12f, 0.36f);
    private static readonly Vector3 HookSize = new Vector3(0.3f, 0.3f, 0.3f);

    private const float JIB_MIN_HEIGHT_FRACTION = 0.55f;
    private static readonly Vector2 JibDirection = new Vector2(-0.7f, -0.71f).normalized;

    private const string ANIMATION_FOLDER = "Assets/Animations/Menu";
    private const string SWAY_CLIP_PATH = ANIMATION_FOLDER + "/HookSway.anim";
    private const string SWAY_CONTROLLER_PATH = ANIMATION_FOLDER + "/HookSway.controller";
    private static readonly Vector2 SwayAmplitudeDegrees = new Vector2(1.6f, 2.4f);
    private static readonly Vector2 SwayPeriodSeconds = new Vector2(4f, 6f);
    private const float SWAY_PHASE_OFFSET = 1.3f;
    private const float SWAY_SAMPLE_STEP = 0.1f;

    private const float FOG_START = 25f;
    private const float FOG_END = 240f;
    private static readonly Color FogColor = new Color(0.78f, 0.9f, 0.96f);

    private const string SMOKE_TEXTURE_PATH = MATERIAL_FOLDER + "/Menu_SmokePuff.png";
    private const string SMOKE_MATERIAL_PATH = MATERIAL_FOLDER + "/Menu_Smoke.mat";
    private const float FUNNEL_MIN_SEPARATION = 0.6f;
    private const float FUNNEL_MAX_DROP = 0.6f;
    private const int MAX_FUNNELS = 2;
    private const float SMOKE_RATE = 14f;
    private static readonly Vector2 SmokeLifetime = new Vector2(6f, 9f);
    private static readonly Vector2 SmokeSpeed = new Vector2(0.5f, 0.9f);
    private static readonly Vector2 SmokeStartSize = new Vector2(1.3f, 1.9f);
    private static readonly Vector3 SmokeDrift = new Vector3(-1.4f, 0.45f, 0.5f);
    private const float SMOKE_GROWTH = 3f;
    private const float SMOKE_SPIN = 0.35f;
    private const float SMOKE_NOISE_STRENGTH = 0.3f;
    private const float SMOKE_NOISE_FREQUENCY = 0.15f;
    private const float SMOKE_PEAK_ALPHA = 0.32f;
    private const float SMOKE_CONE_RADIUS = 0.25f;
    private const float SMOKE_CONE_ANGLE = 10f;
    private static readonly Color SmokeBright = new Color(0.92f, 0.92f, 0.92f, 1f);
    private static readonly Color SmokeDark = new Color(0.66f, 0.67f, 0.7f, 1f);

    private const string FLYING_GULL_PATH = "Assets/ThirdParty/PolyPizza/FlyingGull_PolyByGoogle.glb";
    private const string SEAGULL_PATH = "Assets/ThirdParty/PolyPizza/Seagull_PolyByGoogle.glb";
    private const string GULL_PREVIEW_ARGUMENT = "-gullPreviewOut";
    private const float GULL_WINGSPAN = 4.2f;
    private const float GULL_SPEED = 12f;
    private const float GULL_NOSE_YAW = 0f;
    private const float GULL_BOB_AMPLITUDE = 0.6f;
    private const float GULL_BOB_PERIOD = 2.7f;
    private const float GULL_BANK_DEGREES = 12f;
    private const float PERCHED_GULL_HEIGHT = 0.9f;

    private const string BUOY_NAME_PREFIX = "buoy";
    private const string BUOY_WRAPPER_PREFIX = "BuoyBob_";
    private const float BUOY_LOOP_SECONDS = 24f;
    private const float BUOY_BOB_AMPLITUDE = 0.18f;
    private const float BUOY_ROLL_DEGREES = 5f;
    private const float BUOY_PITCH_DEGREES = 3f;
    private static readonly (float bob, float roll, float pitch)[] BuoyPeriods =
    {
        (3f, 4f, 2.4f),
        (24f / 7f, 4.8f, 24f / 9f),
        (24f / 9f, 24f / 7f, 24f / 11f),
    };
    private static readonly float[] BuoyPhases = { 0f, 1.9f, 3.7f };
    private static readonly (int container, Vector2 offset, float yaw)[] PerchedGulls =
    {
        (2, new Vector2(-0.3f, 0.1f), -160f),
        (2, new Vector2(0.35f, -0.2f), -205f),
        (1, new Vector2(0.2f, 0.25f), -175f),
    };

    private struct GullOrbit
    {
        public Vector3 Center;
        public float Radius;
        public bool Clockwise;
        public float[] AnglesDegrees;
        public float[] Heights;
    }

    private static readonly GullOrbit[] GullOrbits =
    {
        new GullOrbit { Center = new Vector3(-28f, 5f, 150f), Radius = 30f, Clockwise = false, AnglesDegrees = new[] { 0f, 40f, 75f }, Heights = new[] { 0f, 1.5f, -1f } },
        new GullOrbit { Center = new Vector3(14f, 9f, 195f), Radius = 42f, Clockwise = true, AnglesDegrees = new[] { 200f, 235f }, Heights = new[] { 0f, 2f } },
        new GullOrbit { Center = new Vector3(-66f, 3f, 125f), Radius = 22f, Clockwise = true, AnglesDegrees = new[] { 90f, 150f, 175f }, Heights = new[] { 0f, -1.2f, 1.8f } },
    };

    private const float SEA_Y = -12f;
    private const float SEA_SIZE = 900f;
    private const float SEA_CENTER_Z = 60f;
    private const float QUAY_TOP_Y = -9f;
    private static readonly Vector3 QuaySize = new Vector3(34f, 6f, 30f);
    private static readonly Vector3 QuayOffset = new Vector3(8f, 0f, 8f);
    private const float QUAY_CONTAINER_WIDTH = 6.1f;
    private static readonly Vector3[] QuayContainerOffsets =
    {
        new Vector3(-1f, 0f, 13f),
        new Vector3(6f, 0f, 13.5f),
        new Vector3(-1f, 1f, 13f),
    };
    private static readonly float[] QuayContainerYaws = { 4f, -8f, 4f };
    private const float SHIP_LENGTH = 22f;
    private static readonly Vector3 ShipPosition = new Vector3(-9f, SEA_Y, 80f);
    private const float SHIP_YAW = 15f;
    private const float SHIP_DRAFT = 0.8f;

    private static readonly Color QuayColor = new Color(0.33f, 0.34f, 0.36f);
    private static readonly Color RopeColor = new Color(0.17f, 0.16f, 0.15f);

    [MenuItem(MENU_PATH)]
    public static void Apply()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuUiBuilder.SCENE_PATH, OpenSceneMode.Single);

        Canvas scoreboard = FindScoreboardCanvas();
        Transform placeholder = scoreboard.transform.parent;
        scoreboard.transform.SetParent(null, true);

        Transform root = EnsureEmptyRoot();
        BuildSea(root);
        ApplyFog();

        HangingContainer hanging = BuildHangingContainer(root);
        Vector3 craneBase = BuildCrane(root, hanging.Hook.position);
        List<GameObject> quayContainers = BuildQuay(root, craneBase);
        GameObject ship = BuildShip(root);
        BuildShipSmoke(ship);
        BuildGulls(root, quayContainers);
        MountScoreboard(scoreboard, hanging);
        RemovePlaceholder(placeholder, hanging.Body.transform);
        WireMenu(hanging.Drop);
        AnimateBuoys(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"{nameof(MenuSceneSetupTool)}: {MainMenuUiBuilder.SCENE_PATH} updated — container {hanging.Size:0.00} at {ContainerCenter}, " +
                  $"hook at {hanging.Hook.position:0.0}, crane base at {craneBase:0.0}");
    }

    public static void ApplyBatch()
    {
        try
        {
            Apply();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private struct HangingContainer
    {
        public Transform Hook;
        public Rigidbody Body;
        public ContainerDrop Drop;
        public Vector3 Size;
    }

    private static Canvas FindScoreboardCanvas()
    {
        HighScoreUIManager highScores = Object.FindFirstObjectByType<HighScoreUIManager>(FindObjectsInactive.Include);
        if (highScores == null) throw new InvalidOperationException($"{MainMenuUiBuilder.SCENE_PATH} has no {nameof(HighScoreUIManager)}");

        Canvas canvas = highScores.GetComponentInParent<Canvas>(true);
        if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
        {
            throw new InvalidOperationException("The scoreboard must sit under a World Space canvas so it can be mounted on the container");
        }

        return canvas;
    }

    private static Transform EnsureEmptyRoot()
    {
        GameObject root = GameObject.Find(ROOT_NAME);
        if (root == null) root = new GameObject(ROOT_NAME);

        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        }

        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        return root.transform;
    }

    private static void BuildSea(Transform root)
    {
        GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
        sea.name = "Sea";
        Object.DestroyImmediate(sea.GetComponent<Collider>());
        sea.transform.SetParent(root, false);
        sea.transform.localPosition = new Vector3(0f, SEA_Y, SEA_CENTER_Z);
        sea.transform.localScale = new Vector3(SEA_SIZE / 10f, 1f, SEA_SIZE / 10f);
        sea.GetComponent<Renderer>().sharedMaterial = GameSceneSetupTool.EnsureSeaMaterial();
    }

    private static void ApplyFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = FogColor;
        RenderSettings.fogStartDistance = FOG_START;
        RenderSettings.fogEndDistance = FOG_END;
    }

    private static HangingContainer BuildHangingContainer(Transform root)
    {
        var body = new GameObject("Container");
        body.transform.SetParent(root, false);
        body.transform.position = ContainerCenter;

        GameObject model = ModelLibrary.Spawn(KenneyKit.Watercraft, CONTAINER_MODEL, body.transform);
        ModelLibrary.OrientLongAxisAlongX(model);
        ScaleToWorldSize(model, ContainerSize);
        if (!ModelLibrary.TryGetBounds(model, out Bounds bounds)) throw new InvalidOperationException($"'{CONTAINER_MODEL}' has no renderers");
        model.transform.position += ContainerCenter - bounds.center;

        Vector3 size = bounds.size;
        Vector3 half = size * 0.5f;

        var collider = body.AddComponent<BoxCollider>();
        collider.size = size;

        var rigidbody = body.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
        rigidbody.mass = CONTAINER_MASS;

        Material rope = EnsureMaterial("Menu_Rope", RopeColor, 0.15f);
        Vector3 spreader = new Vector3(0f, half.y + SPREADER_HEIGHT, 0f);
        Box(body.transform, "Spreader", spreader, SpreaderSize, rope);
        foreach (Vector3 corner in new[]
                 {
                     new Vector3(half.x * SLING_CORNER_INSET, half.y, half.z * SLING_CORNER_INSET),
                     new Vector3(-half.x * SLING_CORNER_INSET, half.y, half.z * SLING_CORNER_INSET),
                     new Vector3(half.x * SLING_CORNER_INSET, half.y, -half.z * SLING_CORNER_INSET),
                     new Vector3(-half.x * SLING_CORNER_INSET, half.y, -half.z * SLING_CORNER_INSET),
                 })
        {
            Cylinder(body.transform, "Sling", spreader, corner, SLING_RADIUS, rope);
        }

        float lowerRope = ROPE_LENGTH * (1f - ROPE_BREAK_FRACTION);
        Cylinder(body.transform, "RopeLower", spreader, spreader + Vector3.up * lowerRope, ROPE_RADIUS, rope);

        var hook = new GameObject("HookPivot");
        hook.transform.SetParent(root, false);
        hook.transform.position = ContainerCenter + Vector3.up * (half.y + SPREADER_HEIGHT + ROPE_LENGTH);
        AttachSway(hook);
        Box(hook.transform, "Hook", Vector3.zero, HookSize, rope);
        Cylinder(hook.transform, "RopeUpper", Vector3.zero, Vector3.down * (ROPE_LENGTH * ROPE_BREAK_FRACTION), ROPE_RADIUS, rope);

        body.transform.SetParent(hook.transform, true);
        body.transform.rotation = Quaternion.Euler(0f, CONTAINER_YAW, 0f);

        var drop = hook.AddComponent<ContainerDrop>();
        UiWiring.Set(drop, ("_container", rigidbody));

        return new HangingContainer { Hook = hook.transform, Body = rigidbody, Drop = drop, Size = size };
    }

    private static void AttachSway(GameObject hook)
    {
        AttachLoopingClip(hook, BuildSwayClip(), SWAY_CLIP_PATH, SWAY_CONTROLLER_PATH);
    }

    private static void AttachLoopingClip(GameObject target, AnimationClip clip, string clipPath, string controllerPath)
    {
        EnsureFolder(ANIMATION_FOLDER);
        AssetDatabase.DeleteAsset(controllerPath);
        AssetDatabase.DeleteAsset(clipPath);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, clipPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, clip);

        Animator animator = UiFactory.Ensure<Animator>(target);
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private static void AnimateBuoys(Scene scene)
    {
        List<Transform> buoys = FindRootBuoys(scene);

        for (int i = 0; i < buoys.Count; i++)
        {
            Transform buoy = buoys[i];
            Transform wrapper = buoy.parent != null && buoy.parent.name.StartsWith(BUOY_WRAPPER_PREFIX, StringComparison.Ordinal)
                ? buoy.parent
                : Wrap(buoy, $"{BUOY_WRAPPER_PREFIX}{i}");
            StripNestedAnimators(buoy);

            AttachLoopingClip(wrapper.gameObject, BuildBuoyClip(buoy.name, i), $"{ANIMATION_FOLDER}/{wrapper.name}.anim", $"{ANIMATION_FOLDER}/{wrapper.name}.controller");
        }

        if (buoys.Count > 0) Debug.Log($"{nameof(MenuSceneSetupTool)}: {buoys.Count} buoy(s) bob with offset phases (loop {BUOY_LOOP_SECONDS:0} s)");
    }

    private static List<Transform> FindRootBuoys(Scene scene)
    {
        var buoys = new List<Transform>();

        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            if (IsBuoyWrapper(rootObject.name))
            {
                Transform buoy = InnermostBuoy(rootObject.transform);
                if (buoy == null) continue;

                Flatten(rootObject.transform, buoy);
                buoys.Add(buoy);
            }
            else if (rootObject.name.StartsWith(BUOY_NAME_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                buoys.Add(rootObject.transform);
            }
        }

        return buoys;
    }

    private static bool IsBuoyWrapper(string name) => name.StartsWith(BUOY_WRAPPER_PREFIX, StringComparison.Ordinal);

    private static Transform InnermostBuoy(Transform wrapper)
    {
        Transform current = wrapper;
        while (current.childCount > 0)
        {
            current = current.GetChild(0);
            if (!IsBuoyWrapper(current.name)) return current;
        }

        return null;
    }

    private static void Flatten(Transform wrapper, Transform buoy)
    {
        if (buoy.parent == wrapper) return;

        buoy.SetParent(wrapper, true);
        buoy.localPosition = Vector3.zero;
        buoy.localRotation = Quaternion.identity;

        for (int i = wrapper.childCount - 1; i >= 0; i--)
        {
            Transform child = wrapper.GetChild(i);
            if (child != buoy && IsBuoyWrapper(child.name)) Object.DestroyImmediate(child.gameObject);
        }
    }

    private static Transform Wrap(Transform target, string name)
    {
        Transform wrapper = new GameObject(name).transform;
        wrapper.SetPositionAndRotation(target.position, target.rotation);
        wrapper.SetSiblingIndex(target.GetSiblingIndex());
        target.SetParent(wrapper, true);
        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;
        StripNestedAnimators(target);
        return wrapper;
    }

    private static void StripNestedAnimators(Transform root)
    {
        foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
        {
            Object.DestroyImmediate(animator, true);
        }

        foreach (Animation animation in root.GetComponentsInChildren<Animation>(true))
        {
            Object.DestroyImmediate(animation, true);
        }
    }

    private static AnimationClip BuildBuoyClip(string childName, int index)
    {
        (float bob, float roll, float pitch) periods = BuoyPeriods[index % BuoyPeriods.Length];
        float phase = BuoyPhases[index % BuoyPhases.Length];

        var clip = new AnimationClip { frameRate = 60f, wrapMode = WrapMode.Loop };
        clip.SetCurve(childName, typeof(Transform), "localPosition.x", AnimationCurve.Constant(0f, BUOY_LOOP_SECONDS, 0f));
        clip.SetCurve(childName, typeof(Transform), "localPosition.y", SineCurve(BUOY_BOB_AMPLITUDE, periods.bob, phase, BUOY_LOOP_SECONDS));
        clip.SetCurve(childName, typeof(Transform), "localPosition.z", AnimationCurve.Constant(0f, BUOY_LOOP_SECONDS, 0f));
        clip.SetCurve(childName, typeof(Transform), "localEulerAnglesRaw.x", SineCurve(BUOY_PITCH_DEGREES, periods.pitch, phase + 0.8f, BUOY_LOOP_SECONDS));
        clip.SetCurve(childName, typeof(Transform), "localEulerAnglesRaw.y", AnimationCurve.Constant(0f, BUOY_LOOP_SECONDS, 0f));
        clip.SetCurve(childName, typeof(Transform), "localEulerAnglesRaw.z", SineCurve(BUOY_ROLL_DEGREES, periods.roll, phase, BUOY_LOOP_SECONDS));
        return clip;
    }

    private static AnimationClip BuildSwayClip()
    {
        float loopSeconds = LoopSeconds(SwayPeriodSeconds.x, SwayPeriodSeconds.y);
        var clip = new AnimationClip { frameRate = 60f, wrapMode = WrapMode.Loop };
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.x", SineCurve(SwayAmplitudeDegrees.x, SwayPeriodSeconds.x, 0f, loopSeconds));
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.y", AnimationCurve.Constant(0f, loopSeconds, 0f));
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.z", SineCurve(SwayAmplitudeDegrees.y, SwayPeriodSeconds.y, SWAY_PHASE_OFFSET, loopSeconds));
        return clip;
    }

    private static void BuildGulls(Transform root, List<GameObject> perches)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(FLYING_GULL_PATH) == null || AssetDatabase.LoadAssetAtPath<GameObject>(SEAGULL_PATH) == null)
        {
            Debug.LogWarning($"{nameof(MenuSceneSetupTool)}: gull models missing under Assets/ThirdParty/PolyPizza, skipping the gulls");
            return;
        }

        var previewSamples = new Dictionary<string, GameObject>();
        PerchGulls(root, perches, previewSamples);

        string[] models = { FLYING_GULL_PATH };

        for (int i = 0; i < GullOrbits.Length; i++)
        {
            GullOrbit orbit = GullOrbits[i];
            var pivot = new GameObject($"GullOrbit_{i}");
            pivot.transform.SetParent(root, false);
            pivot.transform.position = orbit.Center;

            var clip = new AnimationClip { frameRate = 60f, wrapMode = WrapMode.Loop };
            float period = 2f * Mathf.PI * orbit.Radius / GULL_SPEED;
            float turn = orbit.Clockwise ? 360f : -360f;
            clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.y", new AnimationCurve(new Keyframe(0f, 0f, turn / period, turn / period), new Keyframe(period, turn, turn / period, turn / period)));

            for (int j = 0; j < orbit.AnglesDegrees.Length; j++)
            {
                float angle = orbit.AnglesDegrees[j] * Mathf.Deg2Rad;
                var local = new Vector3(Mathf.Cos(angle) * orbit.Radius, orbit.Heights[j], Mathf.Sin(angle) * orbit.Radius);
                Vector3 tangent = orbit.Clockwise ? new Vector3(Mathf.Sin(angle), 0f, -Mathf.Cos(angle)) : new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                float bank = orbit.Clockwise ? -GULL_BANK_DEGREES : GULL_BANK_DEGREES;

                string model = models[(i + j) % models.Length];
                GameObject gull = SpawnGull(model, pivot.transform, $"Gull_{j}");
                gull.transform.localPosition = local;
                gull.transform.localRotation = Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(0f, GULL_NOSE_YAW, bank);
                if (!previewSamples.ContainsKey(model)) previewSamples[model] = gull;

                float loop = LoopSeconds(GULL_BOB_PERIOD, period);
                clip.SetCurve(gull.name, typeof(Transform), "localPosition.x", AnimationCurve.Constant(0f, loop, local.x));
                clip.SetCurve(gull.name, typeof(Transform), "localPosition.y", SineCurve(GULL_BOB_AMPLITUDE, GULL_BOB_PERIOD, j * 1.3f, loop, local.y));
                clip.SetCurve(gull.name, typeof(Transform), "localPosition.z", AnimationCurve.Constant(0f, loop, local.z));
            }

            AttachLoopingClip(pivot, clip, $"{ANIMATION_FOLDER}/GullOrbit_{i}.anim", $"{ANIMATION_FOLDER}/GullOrbit_{i}.controller");
        }

        foreach (KeyValuePair<string, GameObject> sample in previewSamples)
        {
            CaptureGullPreview(sample.Value, System.IO.Path.GetFileNameWithoutExtension(sample.Key));
        }
    }

    private static void PerchGulls(Transform root, List<GameObject> perches, Dictionary<string, GameObject> previewSamples)
    {
        var flock = new GameObject("PerchedGulls");
        flock.transform.SetParent(root, false);

        for (int i = 0; i < PerchedGulls.Length; i++)
        {
            (int container, Vector2 offset, float yaw) perch = PerchedGulls[i];
            if (perch.container >= perches.Count || !ModelLibrary.TryGetBounds(perches[perch.container], out Bounds top)) continue;

            var wrapper = new GameObject($"PerchedGull_{i}");
            wrapper.transform.SetParent(flock.transform, false);
            wrapper.transform.position = new Vector3(top.center.x + top.extents.x * perch.offset.x, top.max.y, top.center.z + top.extents.z * perch.offset.y);
            wrapper.transform.rotation = Quaternion.Euler(0f, perch.yaw, 0f);

            GameObject model = ModelLibrary.SpawnAsset(SEAGULL_PATH, wrapper.transform);
            ModelLibrary.FitHeight(model, PERCHED_GULL_HEIGHT);
            if (ModelLibrary.TryGetBounds(model, out Bounds bounds))
            {
                model.transform.position += wrapper.transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }

            if (!previewSamples.ContainsKey(SEAGULL_PATH)) previewSamples[SEAGULL_PATH] = wrapper;
        }
    }

    private static GameObject SpawnGull(string assetPath, Transform parent, string name)
    {
        var wrapper = new GameObject(name);
        wrapper.transform.SetParent(parent, false);

        GameObject model = ModelLibrary.SpawnAsset(assetPath, wrapper.transform);
        StripNestedAnimators(model.transform);
        ModelLibrary.OrientLongAxisAlongX(model);
        ModelLibrary.FitWidth(model, GULL_WINGSPAN);
        if (ModelLibrary.TryGetBounds(model, out Bounds bounds)) model.transform.position += wrapper.transform.position - bounds.center;

        return wrapper;
    }

    private static void CaptureGullPreview(GameObject gull, string suffix)
    {
        string directory = ArgumentValue(GULL_PREVIEW_ARGUMENT);
        if (string.IsNullOrEmpty(directory) || gull == null) return;

        System.IO.Directory.CreateDirectory(directory);
        string path = System.IO.Path.Combine(directory, $"gull_{suffix}.png");
        var cameraObject = new GameObject("GullPreviewCamera");
        try
        {
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 40f;
            Vector3 forward = gull.transform.forward;
            Vector3 right = gull.transform.right;
            camera.transform.position = gull.transform.position - forward * 4f + right * 6f + Vector3.up * 2.5f;
            camera.transform.LookAt(gull.transform.position);
            SceneCapture.RenderToPng(camera, new Vector2Int(960, 540), path);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
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

    private static float LoopSeconds(float periodA, float periodB)
    {
        float loop = periodA;
        while (Mathf.Abs(loop / periodB - Mathf.Round(loop / periodB)) > 0.001f) loop += periodA;
        return loop;
    }

    private static AnimationCurve SineCurve(float amplitude, float period, float phase, float loopSeconds, float offset = 0f)
    {
        var keys = new List<Keyframe>();
        float angularSpeed = 2f * Mathf.PI / period;

        for (float time = 0f; time <= loopSeconds + SWAY_SAMPLE_STEP * 0.5f; time += SWAY_SAMPLE_STEP)
        {
            float clamped = Mathf.Min(time, loopSeconds);
            float value = offset + amplitude * Mathf.Sin(angularSpeed * clamped + phase);
            float slope = amplitude * angularSpeed * Mathf.Cos(angularSpeed * clamped + phase);
            keys.Add(new Keyframe(clamped, value, slope, slope));
        }

        return new AnimationCurve(keys.ToArray());
    }

    private static void BuildShipSmoke(GameObject ship)
    {
        Material material = EnsureSmokeMaterial();
        List<Vector3> funnels = TopPeaks(ship);

        for (int i = 0; i < funnels.Count; i++)
        {
            EmitSmoke(ship.transform, funnels.Count > 1 ? $"Smoke_{i}" : "Smoke", funnels[i], material);
        }
    }

    private static void EmitSmoke(Transform ship, string name, Vector3 worldPosition, Material material)
    {
        var smoke = new GameObject(name);
        smoke.transform.SetParent(ship, false);
        smoke.transform.position = worldPosition;
        smoke.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        var particles = smoke.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(SmokeLifetime.x, SmokeLifetime.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(SmokeSpeed.x, SmokeSpeed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(SmokeStartSize.x, SmokeStartSize.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(SmokeBright, SmokeDark);
        main.gravityModifier = -0.02f;
        main.maxParticles = 300;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = SMOKE_RATE;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = SMOKE_CONE_ANGLE;
        shape.radius = SMOKE_CONE_RADIUS;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = SmokeDrift.x;
        velocity.y = SmokeDrift.y;
        velocity.z = SmokeDrift.z;

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = SMOKE_NOISE_STRENGTH;
        noise.frequency = SMOKE_NOISE_FREQUENCY;
        noise.scrollSpeed = 0.05f;
        noise.damping = true;

        ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-SMOKE_SPIN, SMOKE_SPIN);

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, SMOKE_GROWTH));

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.85f, 0.86f, 0.88f), 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f), new GradientAlphaKey(SMOKE_PEAK_ALPHA, 0.12f),
                new GradientAlphaKey(SMOKE_PEAK_ALPHA * 0.7f, 0.55f), new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        var renderer = smoke.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.sharedMaterial = material;
    }

    private static Material EnsureSmokeMaterial()
    {
        var puff = AssetDatabase.LoadAssetAtPath<Texture2D>(SMOKE_TEXTURE_PATH);
        if (puff == null) Debug.LogWarning($"{nameof(MenuSceneSetupTool)}: {SMOKE_TEXTURE_PATH} missing (run Tools smoke_puff generator); smoke falls back to the default particle texture");

        Material template = GraphicsSettings.currentRenderPipeline != null
            ? GraphicsSettings.currentRenderPipeline.defaultParticleMaterial
            : AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");

        var material = AssetDatabase.LoadAssetAtPath<Material>(SMOKE_MATERIAL_PATH);
        if (material == null)
        {
            EnsureFolder(MATERIAL_FOLDER);
            material = new Material(template);
            AssetDatabase.CreateAsset(material, SMOKE_MATERIAL_PATH);
        }
        else
        {
            material.shader = template.shader;
            material.CopyPropertiesFromMaterial(template);
        }

        if (puff != null)
        {
            material.SetTexture("_BaseMap", puff);
            material.SetTexture("_MainTex", puff);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static List<Vector3> TopPeaks(GameObject root)
    {
        var vertices = new List<Vector3>();
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;

            foreach (Vector3 vertex in filter.sharedMesh.vertices) vertices.Add(filter.transform.TransformPoint(vertex));
        }

        vertices.Sort((a, b) => b.y.CompareTo(a.y));
        var peaks = new List<Vector3>();

        foreach (Vector3 candidate in vertices)
        {
            if (peaks.Count >= MAX_FUNNELS) break;
            if (peaks.Count > 0 && candidate.y < peaks[0].y - FUNNEL_MAX_DROP) break;

            bool separated = true;
            foreach (Vector3 peak in peaks)
            {
                if (Vector2.Distance(new Vector2(peak.x, peak.z), new Vector2(candidate.x, candidate.z)) < FUNNEL_MIN_SEPARATION) separated = false;
            }

            if (separated) peaks.Add(candidate);
        }

        if (peaks.Count == 0) peaks.Add(root.transform.position);
        return peaks;
    }

    private static void EnsureFolder(string folder) => AssetFolders.Ensure(folder);

    private static void ScaleToWorldSize(GameObject model, Vector3 targetSize)
    {
        if (!ModelLibrary.TryGetBounds(model, out Bounds bounds)) return;

        Vector3 scale = model.transform.localScale;
        for (int axis = 0; axis < 3; axis++)
        {
            Vector3 localAxis = Vector3.zero;
            localAxis[axis] = 1f;
            Vector3 worldAxis = model.transform.rotation * localAxis;

            int dominant = Mathf.Abs(worldAxis.x) >= Mathf.Abs(worldAxis.z) ? (Mathf.Abs(worldAxis.x) >= Mathf.Abs(worldAxis.y) ? 0 : 1)
                : (Mathf.Abs(worldAxis.z) >= Mathf.Abs(worldAxis.y) ? 2 : 1);
            float current = bounds.size[dominant];
            if (current > 0.0001f) scale[axis] *= targetSize[dominant] / current;
        }

        model.transform.localScale = scale;
    }

    private static Vector3 BuildCrane(Transform root, Vector3 hook)
    {
        GameObject crane = ModelLibrary.SpawnAsset(ModelLibrary.CRANE_GLB_PATH, root);
        if (!ModelLibrary.TryGetBounds(crane, out Bounds bounds)) throw new InvalidOperationException("Crane model has no renderers");

        Vector3 tipLocal = JibTip(crane, bounds);
        Vector3 baseLocal = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        float scale = (hook.y - QUAY_TOP_Y) / Mathf.Max(0.01f, tipLocal.y - baseLocal.y);
        var jib = new Vector2(tipLocal.x - baseLocal.x, tipLocal.z - baseLocal.z);
        float yaw = -Vector2.SignedAngle(jib, JibDirection);

        crane.transform.localScale = Vector3.one * scale;
        crane.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        crane.transform.position = hook - crane.transform.rotation * (tipLocal * scale);

        Vector3 baseWorld = crane.transform.TransformPoint(baseLocal);
        Debug.Log($"{nameof(MenuSceneSetupTool)}: crane tip local {tipLocal:0.00}, jib {jib.magnitude:0.00} long, scale {scale:0.00}, yaw {yaw:0}, base {baseWorld:0.0}");
        return baseWorld;
    }

    private static Vector3 JibTip(GameObject crane, Bounds bounds)
    {
        float minY = bounds.min.y + bounds.size.y * JIB_MIN_HEIGHT_FRACTION;
        var center = new Vector2(bounds.center.x, bounds.center.z);
        Vector3 tip = bounds.center;
        float farthest = -1f;

        foreach (MeshFilter filter in crane.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;

            foreach (Vector3 vertex in filter.sharedMesh.vertices)
            {
                Vector3 world = filter.transform.TransformPoint(vertex);
                if (world.y < minY) continue;

                float distance = Vector2.Distance(new Vector2(world.x, world.z), center);
                if (distance <= farthest) continue;

                farthest = distance;
                tip = world;
            }
        }

        return tip;
    }

    private static List<GameObject> BuildQuay(Transform root, Vector3 craneBase)
    {
        Material concrete = EnsureMaterial("Menu_Quay", QuayColor, 0.2f);
        Vector3 quayCenter = new Vector3(craneBase.x, QUAY_TOP_Y - QuaySize.y * 0.5f, craneBase.z) + QuayOffset;
        Box(root, "Quay", quayCenter, QuaySize, concrete);

        var containers = new List<GameObject>();
        for (int i = 0; i < QuayContainerModels.Length; i++)
        {
            GameObject container = ModelLibrary.Spawn(KenneyKit.Watercraft, QuayContainerModels[i], root);
            container.name = $"QuayContainer_{i}";
            ModelLibrary.OrientLongAxisAlongX(container);
            ModelLibrary.FitWidth(container, QUAY_CONTAINER_WIDTH);

            Vector3 offset = QuayContainerOffsets[i];
            float stackHeight = offset.y * ModelLibrary.Size(container).y;
            container.transform.rotation = Quaternion.Euler(0f, QuayContainerYaws[i], 0f);
            ModelLibrary.PlaceBottomCenter(container, new Vector3(craneBase.x + offset.x, QUAY_TOP_Y + stackHeight, craneBase.z + offset.z));
            containers.Add(container);
        }

        return containers;
    }

    private static GameObject BuildShip(Transform root)
    {
        GameObject ship = ModelLibrary.Spawn(KenneyKit.Watercraft, SHIP_MODEL, root);
        ModelLibrary.OrientLongAxisAlongX(ship);
        ModelLibrary.FitWidth(ship, SHIP_LENGTH);
        ship.transform.rotation = Quaternion.Euler(0f, SHIP_YAW, 0f);
        ModelLibrary.PlaceBottomCenter(ship, ShipPosition + Vector3.down * SHIP_DRAFT);
        return ship;
    }

    private static void MountScoreboard(Canvas canvas, HangingContainer hanging)
    {
        var rect = canvas.GetComponent<RectTransform>();
        rect.SetParent(hanging.Body.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = ScoreboardSize;

        float scale = Mathf.Min(hanging.Size.x * CANVAS_FILL / ScoreboardSize.x, hanging.Size.y * CANVAS_FILL / ScoreboardSize.y);
        rect.localScale = Vector3.one * scale;
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition3D = new Vector3(0f, 0f, -(hanging.Size.z * 0.5f + CANVAS_FACE_OFFSET));
    }

    private static void RemovePlaceholder(Transform placeholder, Transform replacement)
    {
        if (placeholder == null || placeholder == replacement || placeholder.name != PLACEHOLDER_NAME) return;
        if (placeholder.parent != null || placeholder.childCount > 0) return;

        Object.DestroyImmediate(placeholder.gameObject);
        Debug.Log($"{nameof(MenuSceneSetupTool)}: removed the placeholder '{PLACEHOLDER_NAME}' cube; the scoreboard now hangs on the real container");
    }

    private static void WireMenu(ContainerDrop drop)
    {
        MenuController menu = Object.FindFirstObjectByType<MenuController>(FindObjectsInactive.Include);
        if (menu == null) throw new InvalidOperationException($"{MainMenuUiBuilder.SCENE_PATH} has no {nameof(MenuController)}");

        UiWiring.Set(menu, ("_containerDrop", drop));
    }

    private static GameObject Box(Transform parent, string name, Vector3 localCenter, Vector3 size, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        Object.DestroyImmediate(box.GetComponent<Collider>());
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localCenter;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private static GameObject Cylinder(Transform parent, string name, Vector3 localFrom, Vector3 localTo, float radius, Material material)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        cylinder.transform.SetParent(parent, false);

        Vector3 axis = localTo - localFrom;
        cylinder.transform.localPosition = (localFrom + localTo) * 0.5f;
        cylinder.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis.normalized);
        cylinder.transform.localScale = new Vector3(radius * 2f, axis.magnitude * 0.5f, radius * 2f);
        cylinder.GetComponent<Renderer>().sharedMaterial = material;
        return cylinder;
    }

    private static Material EnsureMaterial(string name, Color color, float smoothness)
    {
        string path = $"{MATERIAL_FOLDER}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        EnsureFolder(MATERIAL_FOLDER);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader);
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
