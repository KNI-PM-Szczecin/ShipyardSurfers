using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class CalibrationSceneBuilder
{
    public const string SCENE_PATH = "Assets/Scenes/Calibration.unity";
    public const string SCENE_NAME = "Calibration";

    private const string CANVAS_NAME = "Canvas";
    private const float MARGIN = 120f;
    private const float HEADER_HEIGHT = 64f;
    private const float ACTION_SIZE = 72f;
    private const float CONTENT_TOP = -180f;
    private const float CONTENT_HEIGHT = 620f;
    private const float PREVIEW_X = 232f;
    private const float PREVIEW_WIDTH = 1064f;
    private const float SIDE_X = 1328f;
    private const float SIDE_WIDTH = 472f;
    private const float GESTURE_SIZE = 236f;
    private const float GUIDE_SIZE = 132f;
    private const float COLUMN_WIDTH = 1680f;
    private const float GUIDE_CARD_WIDTH = 830f;
    private const float GUIDE_CARD_HEIGHT = 188f;
    private const float POPUP_WIDTH = 760f;

    private static readonly (GestureType Gesture, string Sprite, string Hint)[] Guides =
    {
        (GestureType.LaneLeft, "Pose_Left", "Wyrzuć lewą rękę w lewo, na wysokości ramion."),
        (GestureType.LaneRight, "Pose_Right", "Wyrzuć prawą rękę w prawo, na wysokości ramion."),
        (GestureType.Jump, "Pose_Jump", "Podnieś obie ręce nad głowę."),
        (GestureType.Roll, "Pose_Roll", "Opuść obie ręce wyraźnie poniżej łokci.")
    };

    public static void Build(UiAssetLibrary assets)
    {
        Scene scene = OpenOrCreate();
        var f = new UiFactory(assets);

        EnsureCamera();
        EnsureEventSystem();

        Canvas canvas = EnsureCanvas();
        UiFactory.ConfigureOverlayCanvas(canvas);
        var canvasRect = (RectTransform)canvas.transform;
        UiFactory.ClearChildren(canvasRect);

        Button back = BuildHeader(f, canvasRect);
        BuildActions(f, canvasRect, out Button info, out Button start);
        PoseCameraView view = BuildPreview(f, canvasRect, out Toggle mirror);
        BuildSideColumn(f, canvasRect, out SidePanel side);
        RectTransform infoPanel = BuildInfoPanel(f, canvasRect, out Button closeInfo);
        RectTransform donePanel = BuildDonePanel(f, canvasRect, out DonePanel done);

        infoPanel.gameObject.SetActive(false);
        donePanel.gameObject.SetActive(false);

        CalibrationController controller = UiFactory.Ensure<CalibrationController>(canvas.gameObject);
        UiWiring.Set(controller,
            ("_backButton", back),
            ("_infoButton", info),
            ("_startButton", start),
            ("_closeInfoButton", closeInfo),
            ("_openFolderButton", done.OpenFolder),
            ("_doneBackButton", done.Back),
            ("_infoPanel", infoPanel.gameObject),
            ("_donePanel", donePanel.gameObject),
            ("_promptCard", side.PromptCard.gameObject),
            ("_idleHint", side.IdleHint.gameObject),
            ("_phaseLabel", side.Phase),
            ("_gestureLabel", side.Gesture),
            ("_timerLabel", side.Timer),
            ("_progressLabel", side.Progress),
            ("_doneLabel", done.Message),
            ("_pathLabel", done.Path),
            ("_gestureImage", side.GestureImage),
            ("_mirrorToggle", mirror),
            ("_laneLeftSprite", f.Assets.Sprite("Pose_Left")),
            ("_laneRightSprite", f.Assets.Sprite("Pose_Right")),
            ("_jumpSprite", f.Assets.Sprite("Pose_Jump")),
            ("_rollSprite", f.Assets.Sprite("Pose_Roll")),
            ("MenuScene", AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuUiBuilder.SCENE_PATH)));

        UiWiring.SetColor(controller, "_gestureActiveColor", UiTheme.White);
        UiWiring.SetColor(controller, "_gestureIdleColor", UiTheme.Alpha(UiTheme.White, 0.3f));
        UiWiring.SetString(controller, "MenuSceneName", Path.GetFileNameWithoutExtension(MainMenuUiBuilder.SCENE_PATH));

        UiWiring.Set(view, ("_jointSprite", f.Assets.Sprite("Circle")), ("_boneSprite", f.Assets.Sprite("Rect_R6")));
        UiWiring.SetColor(view, "_skeletonColor", UiTheme.Alpha(UiTheme.Cyan, 0.9f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        EnsureInBuildSettings();
        Debug.Log($"{nameof(CalibrationSceneBuilder)}: rebuilt {SCENE_PATH}");
    }

    private struct SidePanel
    {
        public RectTransform PromptCard;
        public RectTransform IdleHint;
        public TextMeshProUGUI Phase;
        public TextMeshProUGUI Gesture;
        public TextMeshProUGUI Timer;
        public TextMeshProUGUI Progress;
        public Image GestureImage;
    }

    private struct DonePanel
    {
        public TextMeshProUGUI Message;
        public TextMeshProUGUI Path;
        public Button OpenFolder;
        public Button Back;
    }

    private static Scene OpenOrCreate()
    {
        if (File.Exists(SCENE_PATH)) return EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static void EnsureCamera()
    {
        Camera camera = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
        if (camera == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            camera = go.AddComponent<Camera>();
        }

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = UiTheme.Main1Dark;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private static Canvas EnsureCanvas()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas != null) return canvas;

        var go = new GameObject(CANVAS_NAME, typeof(RectTransform), typeof(Canvas));
        return go.GetComponent<Canvas>();
    }

    private static Button BuildHeader(UiFactory f, RectTransform canvas)
    {
        RectTransform header = f.Rect(canvas, "Header");
        UiFactory.Place(header, new Vector2(0f, 1f), new Vector2(MARGIN, -80f), new Vector2(COLUMN_WIDTH, HEADER_HEIGHT));
        f.HorizontalLayout(header, new RectOffset(0, 0, 0, 0), 18f, TextAnchor.MiddleLeft);

        Button back = f.IconButton(header, "Back", f.Assets.Sprite("Icon_Back"), UiButtonStyle.Secondary, 56f);
        TextMeshProUGUI title = f.Heading(header, "Title", "Kalibracja", 44f, UiTheme.White, TextAlignmentOptions.MidlineLeft);
        UiFactory.Layout(title, -1f, -1f, 1f);

        return back;
    }

    private static void BuildActions(UiFactory f, RectTransform canvas, out Button info, out Button start)
    {
        RectTransform column = f.Rect(canvas, "Actions");
        UiFactory.Place(column, new Vector2(0f, 1f), new Vector2(MARGIN, CONTENT_TOP), new Vector2(ACTION_SIZE, 168f));
        f.VerticalLayout(column, new RectOffset(0, 0, 0, 0), 16f, TextAnchor.UpperLeft);

        info = f.IconButton(column, "Info", f.Assets.Sprite("Icon_Info"), UiButtonStyle.Secondary, ACTION_SIZE);
        start = f.IconButton(column, "Start", f.Assets.Sprite("Icon_Play"), UiButtonStyle.Primary, ACTION_SIZE);
    }

    private static PoseCameraView BuildPreview(UiFactory f, RectTransform canvas, out Toggle mirror)
    {
        RectTransform card = f.Card(canvas, "Preview", UiTheme.Alpha(UiTheme.Main1, 0.92f), UiTheme.Line);
        UiFactory.Place(card, new Vector2(0f, 1f), new Vector2(PREVIEW_X, CONTENT_TOP), new Vector2(PREVIEW_WIDTH, CONTENT_HEIGHT));

        RectTransform frame = f.Rect(card, "Frame");
        UiFactory.Stretch(frame, 16f, 16f, 16f, 56f);

        RectTransform cameraRect = f.Rect(frame, "Camera");
        UiFactory.Stretch(cameraRect);
        var raw = cameraRect.gameObject.AddComponent<RawImage>();
        raw.color = UiTheme.White;
        raw.raycastTarget = false;
        raw.enabled = false;
        AspectRatioFitter fitter = UiFactory.Ensure<AspectRatioFitter>(cameraRect.gameObject);
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;

        RectTransform overlay = f.Rect(cameraRect, "Overlay");
        UiFactory.Stretch(overlay);

        TextMeshProUGUI offline = f.Text(frame, "OfflineHint", "Brak obrazu z kamery — sprawdź kamerę w ustawieniach.",
            f.Assets.BodyMedium, 22f, UiTheme.TextMuted, TextAlignmentOptions.Center);
        UiFactory.Stretch(offline.rectTransform);

        RectTransform statusRow = f.Rect(card, "StatusRow");
        statusRow.anchorMin = new Vector2(0f, 0f);
        statusRow.anchorMax = new Vector2(1f, 0f);
        statusRow.pivot = new Vector2(0.5f, 0f);
        statusRow.offsetMin = new Vector2(24f, 14f);
        statusRow.offsetMax = new Vector2(-24f, 44f);
        f.HorizontalLayout(statusRow, new RectOffset(0, 0, 0, 0), 12f, TextAnchor.MiddleLeft);

        Image dot = f.Image(statusRow, "Dot", f.Assets.Sprite("Circle"), UiTheme.TextDim, Image.Type.Simple);
        UiFactory.Layout(dot, 12f, 12f);
        TextMeshProUGUI status = f.Text(statusRow, "StatusLabel", "Sterowanie kamerą wyłączone", f.Assets.BodyMedium, 20f,
            UiTheme.TextMuted, TextAlignmentOptions.MidlineLeft);
        UiFactory.Layout(status, -1f, -1f, 1f);

        TextMeshProUGUI mirrorLabel = f.Text(statusRow, "MirrorLabel", "Odbicie lustrzane", f.Assets.BodyMedium, 20f,
            UiTheme.TextMuted, TextAlignmentOptions.MidlineRight);
        UiFactory.Layout(mirrorLabel, 190f, -1f);
        mirror = f.Switch(statusRow, "MirrorToggle");

        var view = card.gameObject.AddComponent<PoseCameraView>();
        UiWiring.Set(view,
            ("_image", raw),
            ("_fitter", fitter),
            ("_overlay", overlay),
            ("_offlineHint", offline.gameObject),
            ("_statusLabel", status));

        return view;
    }

    private static void BuildSideColumn(UiFactory f, RectTransform canvas, out SidePanel side)
    {
        RectTransform column = f.Rect(canvas, "Side");
        UiFactory.Place(column, new Vector2(0f, 1f), new Vector2(SIDE_X, CONTENT_TOP), new Vector2(SIDE_WIDTH, CONTENT_HEIGHT));
        f.VerticalLayout(column, new RectOffset(0, 0, 0, 0), 18f, TextAnchor.UpperLeft);

        RectTransform prompt = f.Card(column, "PromptCard", UiTheme.Alpha(UiTheme.Main1, 0.92f), UiTheme.Line);
        f.VerticalLayout(prompt, new RectOffset(28, 28, 24, 24), 10f, TextAnchor.UpperCenter);

        TextMeshProUGUI phase = f.Caption(prompt, "Phase", "Przygotuj się", UiTheme.Main2);
        phase.alignment = TextAlignmentOptions.Center;
        UiFactory.Layout(phase, -1f, 24f);

        Image gesture = f.Icon(prompt, "GestureImage", f.Assets.Sprite("Pose_Jump"), UiTheme.White, GESTURE_SIZE);
        UiFactory.Layout(gesture, GESTURE_SIZE, GESTURE_SIZE);

        TextMeshProUGUI gestureLabel = f.Heading(prompt, "Gesture", "Skok", 40f, UiTheme.White, TextAlignmentOptions.Center);
        UiFactory.Layout(gestureLabel, -1f, 48f);

        TextMeshProUGUI timer = f.Heading(prompt, "Timer", "10", 64f, UiTheme.Cyan, TextAlignmentOptions.Center);
        UiFactory.Layout(timer, -1f, 74f);

        TextMeshProUGUI progress = f.Text(prompt, "Progress", "1 / 12", f.Assets.BodyMedium, 20f, UiTheme.TextMuted,
            TextAlignmentOptions.Center);
        UiFactory.Layout(progress, -1f, 26f);

        RectTransform idle = f.Card(column, "IdleHint", UiTheme.Alpha(UiTheme.Main1, 0.92f), UiTheme.Line);
        f.VerticalLayout(idle, new RectOffset(28, 28, 24, 24), 10f, TextAnchor.UpperLeft);

        TextMeshProUGUI idleTitle = f.Text(idle, "Title", "Zacznij od instrukcji", f.Assets.Body, 24f, UiTheme.White,
            TextAlignmentOptions.TopLeft);
        UiFactory.Layout(idleTitle, -1f, 32f);

        TextMeshProUGUI idleBody = f.Text(idle, "Body",
            "Ikona info pokazuje wszystkie cztery ruchy. Przycisk play uruchamia kalibrację: 10 sekund na przygotowanie, " +
            "potem 3 serie po 4 ruchy w losowej kolejności.", f.Assets.BodyMedium, 20f, UiTheme.TextMuted, TextAlignmentOptions.TopLeft);
        idleBody.textWrappingMode = TextWrappingModes.Normal;

        side = new SidePanel
        {
            PromptCard = prompt,
            IdleHint = idle,
            Phase = phase,
            Gesture = gestureLabel,
            Timer = timer,
            Progress = progress,
            GestureImage = gesture
        };
    }

    private static RectTransform BuildInfoPanel(UiFactory f, RectTransform canvas, out Button close)
    {
        RectTransform panel = f.Rect(canvas, "InfoPanel");
        UiFactory.Stretch(panel);

        Image scrim = f.Panel(panel, "Scrim", UiTheme.Main1Dark, true);
        UiFactory.Stretch(scrim.rectTransform);

        RectTransform column = f.Rect(panel, "Column");
        UiFactory.Place(column, new Vector2(0f, 1f), new Vector2(MARGIN, -80f), new Vector2(COLUMN_WIDTH, 880f));
        f.VerticalLayout(column, new RectOffset(0, 0, 0, 0), 20f, TextAnchor.UpperLeft);

        RectTransform header = f.Rect(column, "Header");
        UiFactory.Layout(header, -1f, HEADER_HEIGHT);
        f.HorizontalLayout(header, new RectOffset(0, 0, 0, 0), 18f, TextAnchor.MiddleLeft);
        close = f.IconButton(header, "Back", f.Assets.Sprite("Icon_Back"), UiButtonStyle.Secondary, 56f);
        TextMeshProUGUI title = f.Heading(header, "Title", "Instrukcje", 44f, UiTheme.White, TextAlignmentOptions.MidlineLeft);
        UiFactory.Layout(title, -1f, -1f, 1f);

        BuildGuideCard(f, column, "Neutral", "Poza gotowa", "Pose_Neutral",
            "Wracaj do niej po każdym ruchu: dłonie na wysokości klatki piersiowej, łokcie przy ciele. Bez niej kolejny " +
            "gest nie zostanie rozpoznany. Stój około 2 m od kamery, tak aby cała sylwetka była widoczna.", COLUMN_WIDTH);

        for (int row = 0; row < 2; row++)
        {
            RectTransform grid = f.Rect(column, $"Row{row}");
            UiFactory.Layout(grid, -1f, GUIDE_CARD_HEIGHT);
            f.HorizontalLayout(grid, new RectOffset(0, 0, 0, 0), 20f, TextAnchor.UpperLeft);

            for (int slot = 0; slot < 2; slot++)
            {
                (GestureType Gesture, string Sprite, string Hint) guide = Guides[row * 2 + slot];
                BuildGuideCard(f, grid, guide.Gesture.ToString(), CalibrationController.GestureName(guide.Gesture),
                    guide.Sprite, guide.Hint, GUIDE_CARD_WIDTH);
            }
        }

        return panel;
    }

    private static void BuildGuideCard(UiFactory f, RectTransform parent, string name, string title, string sprite, string hint,
        float width)
    {
        RectTransform card = f.Card(parent, name, UiTheme.Alpha(UiTheme.Main1, 0.92f), UiTheme.Line);
        UiFactory.Layout(card, width, GUIDE_CARD_HEIGHT);
        f.HorizontalLayout(card, new RectOffset(24, 24, 24, 24), 22f, TextAnchor.MiddleLeft);

        Image figure = f.Icon(card, "Figure", f.Assets.Sprite(sprite), UiTheme.Alpha(UiTheme.White, 0.75f), GUIDE_SIZE);
        UiFactory.Layout(figure, GUIDE_SIZE, GUIDE_SIZE);

        RectTransform text = f.Rect(card, "Text");
        UiFactory.Layout(text, -1f, GUIDE_SIZE, 1f);
        f.VerticalLayout(text, new RectOffset(0, 0, 0, 0), 8f, TextAnchor.MiddleLeft);

        TextMeshProUGUI caption = f.Heading(text, "Name", title, 30f, UiTheme.White, TextAlignmentOptions.MidlineLeft);
        UiFactory.Layout(caption, -1f, 36f);

        TextMeshProUGUI body = f.Text(text, "Hint", hint, f.Assets.BodyMedium, 20f, UiTheme.TextMuted,
            TextAlignmentOptions.TopLeft);
        body.textWrappingMode = TextWrappingModes.Normal;
    }

    private static RectTransform BuildDonePanel(UiFactory f, RectTransform canvas, out DonePanel done)
    {
        RectTransform panel = f.Rect(canvas, "DonePanel");
        UiFactory.Stretch(panel);

        Image scrim = f.Panel(panel, "Scrim", UiTheme.Alpha(UiTheme.Main1Dark, 0.82f), true);
        UiFactory.Stretch(scrim.rectTransform);

        RectTransform card = f.Card(panel, "Card", UiTheme.Main1, UiTheme.Line);
        UiFactory.PlaceCentered(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(POPUP_WIDTH, 360f));
        f.VerticalLayout(card, new RectOffset(36, 36, 32, 32), 12f, TextAnchor.UpperLeft);

        TextMeshProUGUI title = f.Heading(card, "Title", "Kalibracja zakończona", 34f, UiTheme.White, TextAlignmentOptions.TopLeft);
        UiFactory.Layout(title, -1f, 42f);

        TextMeshProUGUI message = f.Text(card, "Message", "Zapisano plik kalibracji.", f.Assets.Body, 22f, UiTheme.White,
            TextAlignmentOptions.TopLeft);
        message.textWrappingMode = TextWrappingModes.Normal;
        UiFactory.Layout(message, -1f, 32f);

        TextMeshProUGUI path = f.Text(card, "Path", "-", f.Assets.BodyMedium, 17f, UiTheme.TextDim, TextAlignmentOptions.TopLeft);
        path.textWrappingMode = TextWrappingModes.Normal;
        UiFactory.Layout(path, -1f, 70f);

        TextMeshProUGUI hint = f.Text(card, "Hint", "Możesz wrócić do menu — plik zostaje na dysku.", f.Assets.BodyMedium, 19f,
            UiTheme.TextMuted, TextAlignmentOptions.TopLeft);
        UiFactory.Layout(hint, -1f, 26f);

        RectTransform buttons = f.Rect(card, "Buttons");
        UiFactory.Layout(buttons, -1f, UiTheme.BUTTON_HEIGHT);
        f.HorizontalLayout(buttons, new RectOffset(0, 0, 0, 0), 14f, TextAnchor.MiddleLeft, true);

        Button openFolder = f.Button(buttons, "OpenFolder", "Otwórz folder", null, UiButtonStyle.Secondary);
        UiFactory.Layout(openFolder, -1f, UiTheme.BUTTON_HEIGHT, 1f);
        Button backToMenu = f.Button(buttons, "BackToMenu", "Wróć do menu", null, UiButtonStyle.Primary);
        UiFactory.Layout(backToMenu, -1f, UiTheme.BUTTON_HEIGHT, 1f);

        done = new DonePanel
        {
            Message = message,
            Path = path,
            OpenFolder = openFolder,
            Back = backToMenu
        };

        return panel;
    }

    private static void EnsureInBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(entry => entry.path == SCENE_PATH)) return;

        scenes.Add(new EditorBuildSettingsScene(SCENE_PATH, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
