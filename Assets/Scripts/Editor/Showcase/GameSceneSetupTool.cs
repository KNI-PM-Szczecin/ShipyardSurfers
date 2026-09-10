using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class GameSceneSetupTool
{
    public const string GAME_SCENE_PATH = "Assets/Scenes/GameScene.unity";
    private const string MENU_PATH = "Shipyard Surfers/World/Setup Game Scene (sea + skybox)";
    private const string SEA_NAME = "Sea";
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

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"{nameof(GameSceneSetupTool)}: {GAME_SCENE_PATH} updated (skybox: {(skybox != null ? skybox.name : "unchanged")}, " +
                  $"fog: {FOG_START}-{FOG_END})");
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
