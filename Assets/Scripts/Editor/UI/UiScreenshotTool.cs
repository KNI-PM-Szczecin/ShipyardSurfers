using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class UiScreenshotTool
{
    private const string OUTPUT_ARGUMENT = "-uiShotsOut";
    private const int UI_LAYER = 5;
    private const int CAPTURE_LAYER = 31;
    private const string GAME_SCENE_PATH = "Assets/Scenes/GameScene.unity";
    private const float SAMPLE_SCORE = 12480f;
    private const float SAMPLE_BEST_SCORE = 9870f;

    private static readonly Vector2Int Landscape = new Vector2Int(1920, 1080);
    private static readonly Vector2Int Portrait = new Vector2Int(1080, 1920);

    [MenuItem("Shipyard Surfers/UI/Capture UI Screenshots")]
    public static void CaptureAllMenu()
    {
        string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "UiShots");
        CaptureAll(output);
        EditorUtility.RevealInFinder(output);
    }

    public static void CaptureAllBatch()
    {
        try
        {
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
        CaptureMainMenu(outputDirectory);
        CaptureGame(outputDirectory);
        EditorSceneManager.OpenScene(MainMenuUiBuilder.SCENE_PATH, OpenSceneMode.Single);
    }

    private static void CaptureMainMenu(string outputDirectory)
    {
        EditorSceneManager.OpenScene(MainMenuUiBuilder.SCENE_PATH, OpenSceneMode.Single);

        MenuController menu = Object.FindFirstObjectByType<MenuController>(FindObjectsInactive.Include);
        var menuSo = new SerializedObject(menu);
        var menuPanel = menuSo.FindProperty("_menuPanel").objectReferenceValue as GameObject;
        var settingsPanel = menuSo.FindProperty("_settingsPanel").objectReferenceValue as GameObject;
        Canvas canvas = menu.GetComponentInParent<Canvas>(true);
        Camera camera = SceneCamera();

        PopulateScoreboardPreview();

        Capture(camera, canvas, Landscape, Path.Combine(outputDirectory, "menu_main.png"));
        Capture(camera, canvas, Portrait, Path.Combine(outputDirectory, "menu_main_portrait.png"));

        menuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        Capture(camera, canvas, Landscape, Path.Combine(outputDirectory, "menu_settings.png"));
    }

    private static void PopulateScoreboardPreview()
    {
        HighScoreUIManager highScores = Object.FindFirstObjectByType<HighScoreUIManager>(FindObjectsInactive.Include);
        if (highScores == null) return;

        highScores.Populate(new List<HighScoreEntry>
        {
            new HighScoreEntry { Name = "Bosman", Score = 18420f },
            new HighScoreEntry { Name = "sztorm_91", Score = 15980f },
            new HighScoreEntry { Name = "Mira", Score = 14210f },
            new HighScoreEntry { Name = "dzwigowy", Score = 11875f },
            new HighScoreEntry { Name = "Anka", Score = 9870f },
            new HighScoreEntry { Name = "spawacz", Score = 7340f },
            new HighScoreEntry { Name = "Majtek", Score = 5120f },
        });
    }

    private static void CaptureGame(string outputDirectory)
    {
        EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);

        GameUIManager gameUi = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);
        var uiSo = new SerializedObject(gameUi);
        var gameplay = uiSo.FindProperty("GameplayPanel").objectReferenceValue as GameObject;
        var endScreen = uiSo.FindProperty("EndScreen").objectReferenceValue as GameObject;
        var scoreText = uiSo.FindProperty("ScoreText").objectReferenceValue as TMP_Text;
        Canvas canvas = gameplay.GetComponentInParent<Canvas>(true);
        Camera camera = SceneCamera();

        if (scoreText != null) scoreText.text = ScoreFormatter.Format(SAMPLE_SCORE);
        PopulatePowerUpPreview(gameplay.GetComponentInChildren<PowerUpUIManager>(true));

        Capture(camera, canvas, Landscape, Path.Combine(outputDirectory, "hud.png"));
        Capture(camera, canvas, Portrait, Path.Combine(outputDirectory, "hud_portrait.png"));

        gameplay.SetActive(false);
        endScreen.SetActive(true);
        DeathUIManager death = endScreen.GetComponent<DeathUIManager>();
        death.PresentScore(SAMPLE_SCORE, SAMPLE_BEST_SCORE);
        Capture(camera, canvas, Landscape, Path.Combine(outputDirectory, "death.png"));
        Capture(camera, canvas, Portrait, Path.Combine(outputDirectory, "death_portrait.png"));

        try
        {
            death.EnableSaveScoreInput();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"{nameof(UiScreenshotTool)}: save input preview fallback ({exception.Message})");
        }

        Capture(camera, canvas, Landscape, Path.Combine(outputDirectory, "death_save.png"));
    }

    private static void PopulatePowerUpPreview(PowerUpUIManager powerUps)
    {
        if (powerUps == null) return;

        var so = new SerializedObject(powerUps);
        var prefab = so.FindProperty("_sliderPrefab").objectReferenceValue as PowerUpTimerSlider;
        var container = so.FindProperty("_container").objectReferenceValue as Transform;
        SerializedProperty visuals = so.FindProperty("_visuals");
        if (prefab == null || container == null || visuals == null) return;

        for (int i = 0; i < visuals.arraySize; i++)
        {
            SerializedProperty element = visuals.GetArrayElementAtIndex(i);
            var visual = new PowerUpVisual
            {
                Type = (PowerUpType)element.FindPropertyRelative("Type").intValue,
                Label = element.FindPropertyRelative("Label").stringValue,
                Color = element.FindPropertyRelative("Color").colorValue,
                Icon = element.FindPropertyRelative("Icon").objectReferenceValue as Sprite
            };

            PowerUpTimerSlider slider = Object.Instantiate(prefab, container);
            slider.Present(visual, 8f, 8f - i * 2.6f);
        }
    }

    private static void Capture(Camera camera, Canvas canvas, Vector2Int size, string path)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        RenderMode previousMode = canvas.renderMode;
        Camera previousCanvasCamera = canvas.worldCamera;
        float previousPlaneDistance = canvas.planeDistance;
        float previousScale = canvas.scaleFactor;
        bool scalerEnabled = scaler != null && scaler.enabled;
        RenderTexture previousTarget = camera.targetTexture;
        int previousCullingMask = camera.cullingMask;

        var sceneTexture = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32);
        var uiTexture = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32);
        GameObject uiCameraObject = null;
        try
        {
            camera.targetTexture = sceneTexture;
            bool overlay = canvas.renderMode != RenderMode.WorldSpace;
            if (overlay)
            {
                Camera uiCamera = CreateUiCamera(camera, uiTexture, out uiCameraObject);
                camera.cullingMask &= ~(1 << CAPTURE_LAYER);
                UiFactory.SetLayerRecursive(canvas.transform, CAPTURE_LAYER);

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCamera;
                canvas.planeDistance = 1f;
            }

            if (scaler != null) scaler.enabled = false;
            Vector2 reference = size.x >= size.y ? UiTheme.REFERENCE_RESOLUTION : new Vector2(UiTheme.REFERENCE_RESOLUTION.y, UiTheme.REFERENCE_RESOLUTION.x);
            canvas.scaleFactor = Mathf.Min(size.x / reference.x, size.y / reference.y);

            RebuildLayout(canvas);
            Render(camera, sceneTexture);
            Texture2D scene = ReadPixels(sceneTexture, size);

            if (overlay)
            {
                Camera uiCamera = uiCameraObject.GetComponent<Camera>();
                uiCamera.backgroundColor = Color.black;
                Render(uiCamera, uiTexture);
                Texture2D overBlack = ReadPixels(uiTexture, size);

                uiCamera.backgroundColor = Color.white;
                Render(uiCamera, uiTexture);
                Texture2D overWhite = ReadPixels(uiTexture, size);

                CompositeOver(scene, overBlack, overWhite);
                Object.DestroyImmediate(overBlack);
                Object.DestroyImmediate(overWhite);
            }

            File.WriteAllBytes(path, scene.EncodeToPNG());
            Object.DestroyImmediate(scene);
            Debug.Log($"{nameof(UiScreenshotTool)}: wrote {path}");
        }
        finally
        {
            RenderTexture.active = null;
            camera.targetTexture = previousTarget;
            camera.cullingMask = previousCullingMask;
            if (uiCameraObject != null)
            {
                Object.DestroyImmediate(uiCameraObject);
                UiFactory.SetLayerRecursive(canvas.transform, UI_LAYER);
            }

            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCanvasCamera;
            canvas.planeDistance = previousPlaneDistance;
            canvas.scaleFactor = previousScale;
            if (scaler != null) scaler.enabled = scalerEnabled;
            sceneTexture.Release();
            uiTexture.Release();
            Object.DestroyImmediate(sceneTexture);
            Object.DestroyImmediate(uiTexture);
        }
    }

    private static Camera CreateUiCamera(Camera baseCamera, RenderTexture target, out GameObject cameraObject)
    {
        cameraObject = new GameObject("UiCaptureCamera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        cameraObject.transform.SetPositionAndRotation(baseCamera.transform.position, baseCamera.transform.rotation);

        var uiCamera = cameraObject.AddComponent<Camera>();
        uiCamera.clearFlags = CameraClearFlags.SolidColor;
        uiCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        uiCamera.cullingMask = 1 << CAPTURE_LAYER;
        uiCamera.fieldOfView = baseCamera.fieldOfView;
        uiCamera.nearClipPlane = 0.1f;
        uiCamera.farClipPlane = 100f;
        uiCamera.targetTexture = target;

        UniversalAdditionalCameraData data = uiCamera.GetUniversalAdditionalCameraData();
        data.renderType = CameraRenderType.Base;
        data.renderPostProcessing = false;
        data.renderShadows = false;
        data.antialiasing = AntialiasingMode.None;
        return uiCamera;
    }

    private static Texture2D ReadPixels(RenderTexture source, Vector2Int size)
    {
        RenderTexture.active = source;
        var texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
        texture.Apply();
        RenderTexture.active = null;
        return texture;
    }

    private static void CompositeOver(Texture2D background, Texture2D overlayOnBlack, Texture2D overlayOnWhite)
    {
        Color32[] under = background.GetPixels32();
        Color32[] black = overlayOnBlack.GetPixels32();
        Color32[] white = overlayOnWhite.GetPixels32();
        for (int i = 0; i < under.Length; i++)
        {
            under[i] = new Color32(
                Blend(under[i].r, black[i].r, white[i].r),
                Blend(under[i].g, black[i].g, white[i].g),
                Blend(under[i].b, black[i].b, white[i].b),
                255);
        }

        background.SetPixels32(under);
        background.Apply();
    }

    private static byte Blend(byte under, byte overBlack, byte overWhite)
    {
        float transmission = Mathf.Clamp01((overWhite - overBlack) / 255f);
        return (byte)Mathf.Clamp(overBlack + under * transmission, 0f, 255f);
    }

    private static void RebuildLayout(Canvas canvas)
    {
        for (int pass = 0; pass < 3; pass++)
        {
            Canvas.ForceUpdateCanvases();
            foreach (RectTransform child in canvas.transform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(child);
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    private static void Render(Camera camera, RenderTexture target)
    {
        var request = new RenderPipeline.StandardRequest { destination = target };
        if (RenderPipeline.SupportsRenderRequest(camera, request))
        {
            RenderPipeline.SubmitRenderRequest(camera, request);
            return;
        }

        camera.Render();
    }

    private static Camera SceneCamera()
    {
        Camera camera = Camera.main;
        if (camera == null) camera = Object.FindFirstObjectByType<Camera>();
        if (camera == null) throw new InvalidOperationException("Scene has no camera to render UI screenshots with");
        return camera;
    }

    private static string OutputDirectoryFromArguments()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i] == OUTPUT_ARGUMENT) return arguments[i + 1];
        }

        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "UiShots");
    }
}
