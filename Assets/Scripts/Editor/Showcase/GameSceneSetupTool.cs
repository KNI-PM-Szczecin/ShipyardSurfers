using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class GameSceneSetupTool
{
    public const string GAME_SCENE_PATH = "Assets/Scenes/GameScene.unity";
    public const string START_LAYOUT_PATH = "Assets/Scripts/ScriptableObjects/StartLayout/ObsticleSetStart.asset";
    private const string MENU_PATH = "Shipyard Surfers/World/Setup Game Scene (sea + skybox)";
    private const string SEA_NAME = "Sea";
    private const string DEBUGGER_NAME = "ObstacleSetDebugger";
    private const string FIRST_SEGMENT_NAME = "First track";
    private const int START_COIN_LANE = 1;
    private const int START_COIN_FIRST_ROW = 14;
    private const int START_COIN_LAST_ROW = 23;
    private const string SEA_MATERIAL_PATH = "Assets/DemoMaterials/Water/Water.mat";
    private const int WATER_LAYER = 4;
    private const float SEA_Y = -0.8f;
    private const float SEA_SIZE = 600f;
    private const float SEA_CENTER_Z = 60f;
    private const float FOG_START = 40f;
    private const float FOG_END = 100f;
    private static readonly Color SeaColor = new Color(0.086f, 0.407f, 1f);
    private static readonly Color FogColor = new Color(0.34f, 0.66f, 0.78f);

    [MenuItem(MENU_PATH)]
    public static void Apply()
    {
        Scene scene = EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);

        Material skybox = ThirdPartyImportTool.FindSkyboxMaterial("Day");
        if (skybox != null)
        {
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = AmbientMode.Skybox;
        }

        ApplyDistanceFog();
        EnsureSea();
        EnsureObstacleSetDebugger();
        ConfigureFirstSegment();
        ConfigureBoatRig();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"{nameof(GameSceneSetupTool)}: {GAME_SCENE_PATH} updated (skybox: {(skybox != null ? skybox.name : "unchanged")}, " +
                  $"fog: {FOG_START}-{FOG_END}, '{DEBUGGER_NAME}' present and disabled, '{FIRST_SEGMENT_NAME}' builds with {START_LAYOUT_PATH})");
    }

    private static void ConfigureFirstSegment()
    {
        TrackSpawner spawner = Object.FindFirstObjectByType<TrackSpawner>(FindObjectsInactive.Include);
        if (spawner == null) throw new InvalidOperationException($"{GAME_SCENE_PATH} has no {nameof(TrackSpawner)}");

        var first = new SerializedObject(spawner).FindProperty("_firstSegment").objectReferenceValue as TrackInitiator;
        if (first == null) first = Object.FindFirstObjectByType<TrackInitiator>(FindObjectsInactive.Include);
        if (first == null) throw new InvalidOperationException($"{GAME_SCENE_PATH} has no pre-placed {nameof(TrackInitiator)} segment");

        GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(first.gameObject);
        if (root != null) PrefabUtility.RevertPrefabInstance(root, InteractionMode.AutomatedAction);
        else root = first.gameObject;

        root.name = FIRST_SEGMENT_NAME;
        first.enabled = true;
        first.LaneCenters = new List<Transform>(spawner.LaneCenters);
        if (first.TrackRenderer == null) first.TrackRenderer = first.GetComponentInChildren<Renderer>();
        UiWiring.Set(first, ("_layoutOverride", EnsureStartLayout()));
        UiWiring.Set(spawner, ("_firstSegment", first));
        EditorUtility.SetDirty(first);
    }

    private static void ConfigureBoatRig()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) throw new InvalidOperationException($"{GAME_SCENE_PATH} has no object tagged Player");

        var controller = new SerializedObject(player.GetComponent<MovmentController>());
        float rise = controller.FindProperty("_jumpDuration").floatValue;
        float height = controller.FindProperty("_jumpHeight").floatValue;
        float fallSpeed = Mathf.Max(0.01f, controller.FindProperty("_fallSpeed").floatValue);
        var roll = new RollProfile(
            controller.FindProperty("_rollDuration").floatValue,
            controller.FindProperty("_rollHoldDuration").floatValue,
            controller.FindProperty("_rollHeight").floatValue);
        BoatRigBuilder.Build(player, rise + height / fallSpeed, roll, controller.FindProperty("_rollDuration").floatValue, controller.FindProperty("_rollHoldDuration").floatValue);
    }

    private static ObsticleSetSO EnsureStartLayout()
    {
        var layout = AssetDatabase.LoadAssetAtPath<ObsticleSetSO>(START_LAYOUT_PATH);
        if (layout != null) return layout;

        layout = ScriptableObject.CreateInstance<ObsticleSetSO>();
        for (int y = 0; y < ObsticleSetSO.ROWS; y++)
            for (int x = 0; x < ObsticleSetSO.COLUMNS; x++)
            {
                layout.Grid[y * ObsticleSetSO.COLUMNS + x] = new ObstacleCell
                {
                    Type = ObsticleType.Empty,
                    BlockadeLength = 1,
                    HasCoin = x == START_COIN_LANE && y >= START_COIN_FIRST_ROW && y <= START_COIN_LAST_ROW
                };
            }

        AssetFolders.Ensure(System.IO.Path.GetDirectoryName(START_LAYOUT_PATH)?.Replace('\\', '/'));
        AssetDatabase.CreateAsset(layout, START_LAYOUT_PATH);
        return layout;
    }

    private static void EnsureObstacleSetDebugger()
    {
        GameObject holder = GameObject.Find(DEBUGGER_NAME);
        if (holder == null) holder = new GameObject(DEBUGGER_NAME);
        if (holder.TryGetComponent(out ObstacleSetDebugger _)) return;

        holder.AddComponent<ObstacleSetDebugger>().enabled = false;
    }

    private static void ApplyDistanceFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = FogColor;
        RenderSettings.fogStartDistance = FOG_START;
        RenderSettings.fogEndDistance = FOG_END;
    }

    private static void EnsureSea()
    {
        GameObject sea = GameObject.Find(SEA_NAME);
        if (sea == null)
        {
            sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sea.name = SEA_NAME;
            Object.DestroyImmediate(sea.GetComponent<Collider>());
        }

        sea.layer = WATER_LAYER;
        sea.transform.position = new Vector3(0f, SEA_Y, SEA_CENTER_Z);
        sea.transform.localScale = new Vector3(SEA_SIZE / 10f, 1f, SEA_SIZE / 10f);
        sea.GetComponent<Renderer>().sharedMaterial = EnsureSeaMaterial();
    }

    public static Material EnsureSeaMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(SEA_MATERIAL_PATH);
        if (material != null) return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader);
        material.SetColor("_BaseColor", SeaColor);
        material.SetFloat("_Smoothness", 0.6f);
        AssetDatabase.CreateAsset(material, SEA_MATERIAL_PATH);
        return material;
    }
}
