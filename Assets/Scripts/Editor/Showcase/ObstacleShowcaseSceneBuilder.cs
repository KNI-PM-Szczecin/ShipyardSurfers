using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ObstacleShowcaseSceneBuilder
{
    public const string SCENE_PATH = "Assets/Scenes/ObstacleShowcase.unity";
    private const string MENU_PATH = "Shipyard Surfers/Showcase/Build Obstacle Showcase Scene";
    private const string SHOWCASE_MATERIALS_FOLDER = "Assets/Showcase";
    private const string TRACK_MATERIAL_PATH = "Assets/DemoMaterials/Track.mat";
    private const string PLAYER_MATERIAL_PATH = "Assets/DemoMaterials/Player.mat";
    private const string FONT_PATH = "Assets/UI/Fonts/TMP/Poppins Bold SDF.asset";
    private const string OUTPUT_ARGUMENT = "-uiShotsOut";

    private const float FLOOR_Y = ObstacleSetBuilder.FLOOR_Y;
    private const float PIVOT_Y = 0f;
    private const float TRACK_WIDTH = 10f;
    private const float CELL_DEPTH = ObstacleSetBuilder.CELL_DEPTH;
    private const float RAMP_OVERLAP = 0.15f;
    private const float STATION_SPACING = 70f;
    private const float STATION_LENGTH = 44.8f;
    private const float TITLE_FONT_SIZE = 16f;
    private const float LABEL_FONT_SIZE = 7f;
    private const int DRESSING_SEED = 1234;
    private static readonly float[] LaneX = { -3f, 0f, 3f };
    private static readonly Vector3 GameCameraOffset = new Vector3(0f, 6.4f, -8f);
    private static readonly Vector3 GameCameraEuler = new Vector3(30f, 0f, 0f);

    private static TMP_FontAsset _font;
    private static Material _laneMaterial;

    private readonly struct Placement
    {
        public readonly GameObject Prefab;
        public readonly ObsticleType Type;
        public readonly string Label;
        public readonly int Lane;
        public readonly int Row;
        public readonly int Length;

        public Placement(GameObject prefab, ObsticleType type, string label, int lane, int row, int length)
        {
            Prefab = prefab;
            Type = type;
            Label = label;
            Lane = lane;
            Row = row;
            Length = length;
        }
    }

    [MenuItem(MENU_PATH)]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        _laneMaterial = EnsureLaneMaterial();

        BuildLighting();
        ApplySkybox();
        BuildSea();

        BuildStation("Station_Pirate", "PIRACI · Kenney Pirate Kit", -STATION_SPACING * 0.5f, ObstacleSetBuilder.PIRATE_APPEARANCE_PATH);
        BuildStation("Station_Port", "PORT · Kenney Watercraft Kit", STATION_SPACING * 0.5f, ObstacleSetBuilder.PORT_APPEARANCE_PATH);
        BuildCameras();

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AssetDatabase.SaveAssets();
        Debug.Log($"{nameof(ObstacleShowcaseSceneBuilder)}: built {SCENE_PATH}");
    }

    public static void BuildAndCaptureBatch()
    {
        try
        {
            Build();
            CaptureAll(OutputDirectoryFromArguments());
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static void CaptureAll(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            bool wasEnabled = camera.enabled;
            camera.enabled = true;
            SceneCapture.RenderToPng(camera, new Vector2Int(1920, 1080), Path.Combine(outputDirectory, $"showcase_{camera.name}.png"));
            camera.enabled = wasEnabled;
        }
    }

    public static string OutputDirectoryFromArguments()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i] == OUTPUT_ARGUMENT) return arguments[i + 1];
        }

        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "UiShots");
    }

    private static void BuildLighting()
    {
        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.957f, 0.839f);
        light.intensity = 1.1f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void ApplySkybox()
    {
        Material skybox = ThirdPartyImportTool.FindSkyboxMaterial("Day");
        if (skybox == null) return;

        RenderSettings.skybox = skybox;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1f;
    }

    private static void BuildSea()
    {
        var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
        sea.name = "Sea";
        Object.DestroyImmediate(sea.GetComponent<Collider>());
        sea.transform.position = new Vector3(0f, FLOOR_Y - 0.35f, STATION_LENGTH * 0.5f);
        sea.transform.localScale = new Vector3(40f, 1f, 40f);
        sea.GetComponent<Renderer>().sharedMaterial = GameSceneSetupTool.EnsureSeaMaterial();
    }

    private static void BuildStation(string name, string title, float x, string appearancePath)
    {
        var look = AssetDatabase.LoadAssetAtPath<TrackApperenceSO>(appearancePath);
        if (look == null) throw new FileNotFoundException($"Appearance '{appearancePath}' not found; run {nameof(ObstacleSetBuilder)} first");

        var root = new GameObject(name).transform;
        root.position = new Vector3(x, 0f, 0f);

        BuildFloor(root);
        BuildPlayerProxy(root);
        Label(root, title, new Vector3(0f, 8f, STATION_LENGTH + 4f), TITLE_FONT_SIZE);

        UnityEngine.Random.InitState(DRESSING_SEED);
        new TrackDresser().Dress(root, look, FLOOR_Y, -CELL_DEPTH, STATION_LENGTH + CELL_DEPTH);

        PlaceObstacles(root, look);
    }

    private static void BuildFloor(Transform root)
    {
        Material track = AssetDatabase.LoadAssetAtPath<Material>(TRACK_MATERIAL_PATH);
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(root, false);
        floor.transform.localPosition = new Vector3(0f, FLOOR_Y, STATION_LENGTH * 0.5f);
        floor.transform.localScale = new Vector3(TRACK_WIDTH / 10f, 1f, STATION_LENGTH / 10f);
        if (track != null) floor.GetComponent<Renderer>().sharedMaterial = track;

        foreach (float laneX in LaneX)
        {
            foreach (float edge in new[] { -1.5f, 1.5f })
            {
                CreateLine(root, new Vector3(laneX + edge, FLOOR_Y + 0.02f, STATION_LENGTH * 0.5f), new Vector3(0.06f, 0.02f, STATION_LENGTH));
            }
        }

        for (int row = 1; row * CELL_DEPTH < STATION_LENGTH; row++)
        {
            CreateLine(root, new Vector3(0f, FLOOR_Y + 0.015f, row * CELL_DEPTH), new Vector3(9f, 0.015f, 0.04f));
        }
    }

    private static void CreateLine(Transform parent, Vector3 localPosition, Vector3 localScale)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = "Line";
        line.transform.SetParent(parent, false);
        line.transform.localPosition = localPosition;
        line.transform.localScale = localScale;
        line.GetComponent<Renderer>().sharedMaterial = _laneMaterial;
        Object.DestroyImmediate(line.GetComponent<Collider>());
    }

    private static void BuildPlayerProxy(Transform station)
    {
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "PlayerProxy";
        capsule.transform.SetParent(station, false);
        capsule.transform.localPosition = new Vector3(LaneX[1], PIVOT_Y, 1.5f);
        Object.DestroyImmediate(capsule.GetComponent<Collider>());

        Material player = AssetDatabase.LoadAssetAtPath<Material>(PLAYER_MATERIAL_PATH);
        if (player != null) capsule.GetComponent<Renderer>().sharedMaterial = player;
    }

    private static void PlaceObstacles(Transform station, TrackApperenceSO look)
    {
        var placements = new List<Placement>
        {
            new Placement(look.JumpPrefab, ObsticleType.Jump, "SKOK", 1, 1, 1),
            new Placement(look.SlidePrefab, ObsticleType.Slide, "ŚLIZG", 1, 2, 1),
            new Placement(look.RampPrefab, ObsticleType.Ramp, "RAMPA", 2, 3, 1),
            new Placement(look.BlockadePrefab, ObsticleType.Blockade, "BLOKADA", 2, 4, 1),
        };

        int[] lanes = { 0, 1, 2 };
        int[] rows = { 3, 5, 6 };
        for (int i = 0; i < look.BlockadeVariants.Length && i < lanes.Length; i++)
        {
            BlockadeVariant variant = look.BlockadeVariants[i];
            placements.Add(new Placement(variant.Prefab, ObsticleType.Blockade, $"STATEK {i + 1} (≥{variant.MinLength})", lanes[i], rows[i], Mathf.Max(2, variant.MinLength)));
        }

        foreach (Placement placement in placements)
        {
            if (placement.Prefab == null) continue;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(placement.Prefab, station);
            float cellNearZ = placement.Row * CELL_DEPTH;
            float depth = placement.Length * CELL_DEPTH;
            instance.transform.localPosition = new Vector3(LaneX[placement.Lane], PIVOT_Y, cellNearZ + depth * 0.5f);

            if (placement.Type == ObsticleType.Blockade) FitBlockade(instance, depth);
            if (placement.Type == ObsticleType.Ramp) AlignRampToCellEnd(instance, station, cellNearZ + CELL_DEPTH);

            float labelHeight = 4.8f;
            if (ObstacleBoundsUtility.TryGetGameplayBounds(instance, out Bounds bounds)) labelHeight = Mathf.Max(labelHeight, bounds.max.y + 1f);
            Label(station, placement.Label, new Vector3(LaneX[placement.Lane], labelHeight, cellNearZ + depth * 0.5f), LABEL_FONT_SIZE);
        }
    }

    private static void FitBlockade(GameObject obstacle, float targetDepth)
    {
        if (obstacle.TryGetComponent(out BlockadeFitter fitter))
        {
            fitter.Fit(targetDepth);
            return;
        }

        if (!ObstacleBoundsUtility.TryGetGameplayBounds(obstacle, out Bounds bounds) || bounds.size.z <= 0.0001f) return;

        Vector3 scale = obstacle.transform.localScale;
        obstacle.transform.localScale = new Vector3(scale.x, scale.y, scale.z * (targetDepth / bounds.size.z));
    }

    private static void AlignRampToCellEnd(GameObject ramp, Transform station, float cellFarLocalZ)
    {
        if (!ObstacleBoundsUtility.TryGetGameplayBounds(ramp, out Bounds bounds)) return;

        float farLocalZ = station.InverseTransformPoint(new Vector3(0f, 0f, bounds.max.z)).z;
        ramp.transform.localPosition += new Vector3(0f, 0f, cellFarLocalZ + RAMP_OVERLAP - farLocalZ);
    }

    private static void BuildCameras()
    {
        float pirateX = -STATION_SPACING * 0.5f;
        float portX = STATION_SPACING * 0.5f;

        Camera overview = CreateCamera("Overview", new Vector3(0f, 34f, -40f), Quaternion.Euler(36f, 0f, 0f), true);
        overview.tag = "MainCamera";

        CreateCamera("Pirate", new Vector3(pirateX, 0f, CELL_DEPTH) + GameCameraOffset, Quaternion.Euler(GameCameraEuler), false);
        CreateCamera("PirateBlockades", new Vector3(pirateX, 0f, CELL_DEPTH * 3.2f) + GameCameraOffset, Quaternion.Euler(GameCameraEuler), false);
        CreateCamera("PirateSide", new Vector3(pirateX - 30f, 20f, STATION_LENGTH * 0.45f), Quaternion.Euler(32f, 90f, 0f), false);
        CreateCamera("PirateBlockadeCloseup", new Vector3(pirateX - 13f, 4.5f, CELL_DEPTH * 4.6f), Quaternion.Euler(10f, 68f, 0f), false);
        CreateCamera("Port", new Vector3(portX, 0f, CELL_DEPTH) + GameCameraOffset, Quaternion.Euler(GameCameraEuler), false);
        CreateCamera("PortBlockades", new Vector3(portX, 0f, CELL_DEPTH * 3.2f) + GameCameraOffset, Quaternion.Euler(GameCameraEuler), false);
        CreateCamera("PortSide", new Vector3(portX + 30f, 20f, STATION_LENGTH * 0.45f), Quaternion.Euler(32f, -90f, 0f), false);
        CreateCamera("PortBlockadeCloseup", new Vector3(portX + 13f, 4.5f, CELL_DEPTH * 4.6f), Quaternion.Euler(10f, -68f, 0f), false);
    }

    private static Camera CreateCamera(string name, Vector3 position, Quaternion rotation, bool enabled)
    {
        var go = new GameObject($"Camera_{name}");
        go.transform.SetPositionAndRotation(position, rotation);
        var camera = go.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 400f;
        camera.enabled = enabled;
        return camera;
    }

    private static void Label(Transform parent, string text, Vector3 localPosition, float size)
    {
        var go = new GameObject($"Label_{text}");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);

        var label = go.AddComponent<TextMeshPro>();
        if (_font != null) label.font = _font;
        label.text = text;
        label.fontSize = size;
        label.color = new Color(0.9f, 0.9f, 0.9f);
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.rectTransform.sizeDelta = new Vector2(30f, 3f);
    }

    private static Material EnsureLaneMaterial()
    {
        if (!AssetDatabase.IsValidFolder(SHOWCASE_MATERIALS_FOLDER)) AssetDatabase.CreateFolder("Assets", "Showcase");

        string path = $"{SHOWCASE_MATERIALS_FOLDER}/LaneLine.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            material = new Material(shader) { color = new Color(0f, 0.9f, 1f) };
            AssetDatabase.CreateAsset(material, path);
        }

        return material;
    }
}
