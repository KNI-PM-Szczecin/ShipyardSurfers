using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MainMenuUiBuilder
{
    public const string SCENE_PATH = "Assets/Scenes/MainMenu.unity";

    private const float COLUMN_X = 120f;
    private const float COLUMN_WIDTH = 720f;
    private const float BUTTON_WIDTH = 520f;
    private const float ROW_HEIGHT = 64f;
    private const float LOGO_SIZE = 176f;
    private const float LOGO_GAP = 28f;
    private const float TITLE_SIZE = 76f;
    private const float CLUB_LOGO_SIZE = 88f;
    private const float HANDLE_WIDTH = 200f;
    private const float LICENSE_WIDTH = 220f;
    private const float HINT_MARGIN = 24f;
    private const float HINT_WIDTH = 160f;
    private const float HINT_HEIGHT = 36f;
    private const float HINT_LABEL_SIZE = 18f;
    private const float HINT_REST_ALPHA = 0.22f;
    private const float HINT_HOVER_ALPHA = 0.9f;

    private struct MenuButtons
    {
        public Button Start;
        public Button Settings;
        public Button Credits;
        public Button Quit;
        public Button Calibration;
    }

    public static void Build(UiAssetLibrary assets)
    {
        Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        var f = new UiFactory(assets);

        MenuController menu = Object.FindFirstObjectByType<MenuController>(FindObjectsInactive.Include);
        if (menu == null) throw new InvalidOperationException($"{SCENE_PATH} has no {nameof(MenuController)}");

        var canvasRect = menu.transform.parent as RectTransform;
        if (canvasRect == null) throw new InvalidOperationException($"{nameof(MenuController)} must live under the menu Canvas");

        UiFactory.ConfigureOverlayCanvas(canvasRect.GetComponent<Canvas>());
        canvasRect.localScale = Vector3.one;
        UiFactory.ClearChildren(canvasRect, menu.transform);

        BuildBackdrop(f, canvasRect);
        RectTransform menuPanel = BuildMenuPanel(f, canvasRect, out MenuButtons buttons);
        RectTransform settingsPanel = BuildSettingsPanel(f, canvasRect, out Button closeSettings);
        RectTransform creditsPanel = BuildCreditsPanel(f, canvasRect, out Button closeCredits);
        settingsPanel.gameObject.SetActive(false);
        creditsPanel.gameObject.SetActive(false);
        menu.transform.SetAsLastSibling();

        UiWiring.Set(menu,
            ("_startGameButton", buttons.Start),
            ("_settingsButton", buttons.Settings),
            ("_creditsButton", buttons.Credits),
            ("_calibrationButton", buttons.Calibration),
            ("_quitButton", buttons.Quit),
            ("_closeSettingsButton", closeSettings),
            ("_closeCreditsButton", closeCredits),
            ("_settingsPanel", settingsPanel.gameObject),
            ("_creditsPanel", creditsPanel.gameObject),
            ("_menuPanel", menuPanel.gameObject));

        WireCalibrationScene(menu);
        BuildScoreboard(f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"{nameof(MainMenuUiBuilder)}: rebuilt {SCENE_PATH}");
    }

    private static void BuildBackdrop(UiFactory f, RectTransform canvas)
    {
        RectTransform backdrop = f.Rect(canvas, "Backdrop");
        UiFactory.Stretch(backdrop);

        Image scrim = f.Image(backdrop, "Scrim", f.Assets.Sprite("Gradient_H"), UiTheme.Alpha(UiTheme.Main1Dark, 0.92f), Image.Type.Simple);
        UiFactory.StretchHorizontal(scrim.rectTransform, 0f, 0.62f);

        Image vignette = f.Image(backdrop, "Vignette", f.Assets.Sprite("Vignette"), UiTheme.Alpha(UiTheme.Main1Dark, 0.55f), Image.Type.Simple);
        UiFactory.Stretch(vignette.rectTransform);

        Image wave = f.Image(backdrop, "Wave", f.Assets.Sprite("Wave_Band"), UiTheme.Alpha(UiTheme.Main2, 0.1f), Image.Type.Tiled);
        wave.rectTransform.anchorMin = new Vector2(0f, 0f);
        wave.rectTransform.anchorMax = new Vector2(1f, 0f);
        wave.rectTransform.pivot = new Vector2(0.5f, 0f);
        wave.rectTransform.offsetMin = new Vector2(-300f, -20f);
        wave.rectTransform.offsetMax = new Vector2(300f, 76f);
    }

    private static RectTransform BuildMenuPanel(UiFactory f, RectTransform canvas, out MenuButtons buttons)
    {
        RectTransform panel = f.Rect(canvas, "MainMenu");
        UiFactory.Stretch(panel);

        BuildLogo(f, panel);

        RectTransform column = f.Rect(panel, "Buttons");
        UiFactory.Place(column, new Vector2(0f, 1f), new Vector2(COLUMN_X, -440f), new Vector2(BUTTON_WIDTH, 388f));
        f.VerticalLayout(column, new RectOffset(0, 0, 0, 0), 14f, TextAnchor.UpperLeft);

        buttons = new MenuButtons
        {
            Start = f.Button(column, "Start", "Graj", null, UiButtonStyle.Primary, UiTheme.BUTTON_HEIGHT_LARGE, 28f),
            Settings = f.Button(column, "Settings", "Ustawienia", null, UiButtonStyle.Secondary),
            Credits = f.Button(column, "Credits", "Autorzy", null, UiButtonStyle.Secondary),
            Quit = f.Button(column, "Quit", "Wyjdź", null, UiButtonStyle.Ghost),
            Calibration = BuildCalibrationHint(f, panel)
        };

        BuildMotionControlHint(f, panel);
        return panel;
    }

    private static Button BuildCalibrationHint(UiFactory f, RectTransform panel)
    {
        RectTransform root = f.Rect(panel, "Calibration");
        UiFactory.Place(root, new Vector2(1f, 0f), new Vector2(-HINT_MARGIN, HINT_MARGIN), new Vector2(HINT_WIDTH, HINT_HEIGHT));

        TextMeshProUGUI label = f.Text(root, "Label", "Kalibracja", f.Assets.BodyMedium, HINT_LABEL_SIZE, UiTheme.White,
            TextAlignmentOptions.MidlineRight);
        UiFactory.Stretch(label.rectTransform);
        label.raycastTarget = true;

        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = label;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = HintColors();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        return button;
    }

    private static ColorBlock HintColors()
    {
        return new ColorBlock
        {
            normalColor = UiTheme.Alpha(UiTheme.White, HINT_REST_ALPHA),
            highlightedColor = UiTheme.Alpha(UiTheme.White, HINT_HOVER_ALPHA),
            pressedColor = UiTheme.Alpha(UiTheme.Main2, HINT_HOVER_ALPHA),
            selectedColor = UiTheme.Alpha(UiTheme.White, HINT_REST_ALPHA),
            disabledColor = UiTheme.Alpha(UiTheme.White, HINT_REST_ALPHA * 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.12f
        };
    }

    private static void WireCalibrationScene(MenuController menu)
    {
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(CalibrationSceneBuilder.SCENE_PATH);
        if (scene != null) UiWiring.Set(menu, ("CalibrationScene", scene));

        UiWiring.SetString(menu, "CalibrationSceneName", CalibrationSceneBuilder.SCENE_NAME);
    }

    private static void BuildLogo(UiFactory f, RectTransform panel)
    {
        RectTransform logo = f.Rect(panel, "Logo");
        UiFactory.Place(logo, new Vector2(0f, 1f), new Vector2(COLUMN_X, -96f), new Vector2(COLUMN_WIDTH, 260f));

        Sprite clubLogo = f.Assets.OptionalSprite(UiAssetLibrary.LOGO_SPRITE_NAME);
        float textOffset = 0f;
        if (clubLogo != null)
        {
            Image emblem = f.Icon(logo, "ClubLogo", clubLogo, UiTheme.White, LOGO_SIZE);
            UiFactory.Place(emblem.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -6f), new Vector2(LOGO_SIZE, LOGO_SIZE));
            textOffset = LOGO_SIZE + LOGO_GAP;
        }

        float textWidth = COLUMN_WIDTH - textOffset;
        TextMeshProUGUI titleTop = f.Heading(logo, "TitleTop", "Shipyard", TITLE_SIZE, UiTheme.White, TextAlignmentOptions.TopLeft, 8f);
        UiFactory.Place(titleTop.rectTransform, new Vector2(0f, 1f), new Vector2(textOffset - 4f, 0f), new Vector2(textWidth, TITLE_SIZE + 12f));

        TextMeshProUGUI titleBottom = f.Heading(logo, "TitleBottom", "Surfers", TITLE_SIZE, UiTheme.White, TextAlignmentOptions.TopLeft, 8f);
        UiFactory.Place(titleBottom.rectTransform, new Vector2(0f, 1f), new Vector2(textOffset - 4f, -(TITLE_SIZE + 4f)), new Vector2(textWidth, TITLE_SIZE + 12f));

        Image accent = f.Flat(logo, "Accent", UiTheme.Red);
        UiFactory.Place(accent.rectTransform, new Vector2(0f, 1f), new Vector2(textOffset, -(TITLE_SIZE * 2f + 24f)), new Vector2(120f, 4f));

        TextMeshProUGUI tagline = f.Text(logo, "Tagline", "Biegnij. Skacz. Surfuj po stoczni.", f.Assets.BodyMedium, 22f, UiTheme.TextMuted, TextAlignmentOptions.TopLeft);
        UiFactory.Place(tagline.rectTransform, new Vector2(0f, 1f), new Vector2(textOffset, -(TITLE_SIZE * 2f + 44f)), new Vector2(textWidth, 30f));
    }

    private static void BuildMotionControlHint(UiFactory f, RectTransform panel)
    {
        Image body = f.Panel(panel, "MotionControlHint", UiTheme.Alpha(UiTheme.Main1, 0.75f));
        RectTransform chip = body.rectTransform;
        UiFactory.Place(chip, new Vector2(0f, 0f), new Vector2(COLUMN_X, 56f), new Vector2(BUTTON_WIDTH, 52f));
        HorizontalLayoutGroup layout = f.HorizontalLayout(chip, new RectOffset(14, 20, 12, 12), 12f, TextAnchor.MiddleLeft);
        layout.childForceExpandHeight = false;
        chip.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image icon = f.Icon(chip, "Icon", f.Assets.Sprite("Icon_Camera"), UiTheme.Main2, 22f);
        UiFactory.Layout(icon, 22f, 22f);

        TextMeshProUGUI text = f.Text(chip, "Text", "Steruj ciałem przed kamerą · włącz w ustawieniach", f.Assets.BodyMedium, 19f, UiTheme.TextMuted, TextAlignmentOptions.MidlineLeft);
        text.textWrappingMode = TextWrappingModes.Normal;
        UiFactory.Layout(text, -1f, -1f, 1f);
    }

    private static RectTransform BuildSettingsPanel(UiFactory f, RectTransform canvas, out Button close)
    {
        RectTransform panel = f.Rect(canvas, "SettingsPanel");
        UiFactory.Stretch(panel);

        RectTransform column = f.Rect(panel, "Column");
        UiFactory.Place(column, new Vector2(0f, 1f), new Vector2(COLUMN_X, -80f), new Vector2(COLUMN_WIDTH, 900f));
        f.VerticalLayout(column, new RectOffset(0, 0, 0, 0), 20f, TextAnchor.UpperLeft);

        RectTransform header = f.Rect(column, "Header");
        UiFactory.Layout(header, -1f, 64f);
        f.HorizontalLayout(header, new RectOffset(0, 0, 0, 0), 18f, TextAnchor.MiddleLeft);
        close = f.IconButton(header, "Back", f.Assets.Sprite("Icon_Back"), UiButtonStyle.Secondary, 56f);
        TextMeshProUGUI title = f.Heading(header, "Title", "Ustawienia", 44f, UiTheme.White, TextAlignmentOptions.MidlineLeft);
        UiFactory.Layout(title, -1f, -1f, 1f);

        RectTransform card = f.Card(column, "MotionControlCard", UiTheme.Alpha(UiTheme.Main1, 0.92f), UiTheme.Line);
        f.VerticalLayout(card, new RectOffset(36, 36, 32, 32), 12f, TextAnchor.UpperLeft);

        RectTransform titleRow = f.Rect(card, "TitleRow");
        UiFactory.Layout(titleRow, -1f, 40f);
        f.HorizontalLayout(titleRow, new RectOffset(0, 0, 0, 0), 14f, TextAnchor.MiddleLeft);
        Image iconBackground = f.Panel(titleRow, "IconBackground", UiTheme.Main2);
        UiFactory.Layout(iconBackground, 40f, 40f);
        Image icon = f.Icon(iconBackground.transform, "Icon", f.Assets.Sprite("Icon_Camera"), UiTheme.White, 22f);
        UiFactory.PlaceCentered(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
        TextMeshProUGUI cardTitle = f.Text(titleRow, "Title", "Sterowanie kamerą", f.Assets.Body, 26f, UiTheme.White, TextAlignmentOptions.MidlineLeft);
        UiFactory.Layout(cardTitle, -1f, -1f, 1f);

        TextMeshProUGUI description = f.Text(card, "Description", "Graj całym ciałem: skacz, rób przewroty i zmieniaj tory ruchem przed kamerą.", f.Assets.BodyMedium, 20f, UiTheme.TextMuted, TextAlignmentOptions.TopLeft);
        description.textWrappingMode = TextWrappingModes.Normal;

        f.Spacer(card, 4f);
        f.Divider(card, UiTheme.Alpha(UiTheme.Main2, 0.4f));

        Toggle enabledToggle = f.Switch(SettingRow(f, card, "EnabledRow", "Sterowanie kamerą"), "EnabledToggle");
        TMP_Dropdown cameraDropdown = f.Dropdown(SettingRow(f, card, "CameraRow", "Kamera"), "CameraDropdown", new Vector2(320f, 48f));
        Toggle mirrorToggle = f.Switch(SettingRow(f, card, "MirrorRow", "Odbicie lustrzane"), "MirrorToggle");

        f.Divider(card, UiTheme.Alpha(UiTheme.Main2, 0.4f));

        RectTransform statusRow = f.Rect(card, "StatusRow");
        UiFactory.Layout(statusRow, -1f, 36f);
        f.HorizontalLayout(statusRow, new RectOffset(0, 0, 0, 0), 12f, TextAnchor.MiddleLeft);
        Image statusIndicator = f.Image(statusRow, "StatusIndicator", f.Assets.Sprite("Circle"), UiTheme.TextDim, Image.Type.Simple);
        UiFactory.Layout(statusIndicator, 12f, 12f);
        TextMeshProUGUI statusLabel = f.Text(statusRow, "StatusLabel", "Sterowanie kamerą wyłączone", f.Assets.BodyMedium, 20f, UiTheme.TextMuted, TextAlignmentOptions.MidlineLeft);
        UiFactory.Layout(statusLabel, -1f, -1f, 1f);

        TextMeshProUGUI hint = f.Text(column, "Hint", "Stań około 2 m od kamery tak, aby cała sylwetka była widoczna.", f.Assets.BodyMedium, 18f, UiTheme.TextDim, TextAlignmentOptions.TopLeft);
        hint.textWrappingMode = TextWrappingModes.Normal;

        var view = panel.gameObject.AddComponent<MotionControlSettingsPanel>();
        UiWiring.Set(view,
            ("_enabledToggle", enabledToggle),
            ("_cameraDropdown", cameraDropdown),
            ("_mirrorToggle", mirrorToggle),
            ("_statusLabel", statusLabel),
            ("_statusIndicator", statusIndicator));

        return panel;
    }

    private static RectTransform BuildCreditsPanel(UiFactory f, RectTransform canvas, out Button close)
    {
        RectTransform panel = f.Rect(canvas, "CreditsPanel");
        UiFactory.Stretch(panel);

        RectTransform column = f.Rect(panel, "Column");
        UiFactory.Place(column, new Vector2(0f, 1f), new Vector2(COLUMN_X, -80f), new Vector2(COLUMN_WIDTH, 900f));
        f.VerticalLayout(column, new RectOffset(0, 0, 0, 0), 20f, TextAnchor.UpperLeft);

        RectTransform header = f.Rect(column, "Header");
        UiFactory.Layout(header, -1f, 64f);
        f.HorizontalLayout(header, new RectOffset(0, 0, 0, 0), 18f, TextAnchor.MiddleLeft);
        close = f.IconButton(header, "Back", f.Assets.Sprite("Icon_Back"), UiButtonStyle.Secondary, 56f);
        TextMeshProUGUI title = f.Heading(header, "Title", "Autorzy", 44f, UiTheme.White, TextAlignmentOptions.MidlineLeft);
        UiFactory.Layout(title, -1f, -1f, 1f);

        BuildClubCard(f, column);
        BuildTeamCard(f, column);
        BuildAssetsCard(f, column);

        return panel;
    }

    private static void BuildClubCard(UiFactory f, RectTransform column)
    {
        RectTransform card = f.Card(column, "ClubCard", UiTheme.Alpha(UiTheme.Main1, 0.92f), UiTheme.Line);
        f.HorizontalLayout(card, new RectOffset(28, 28, 22, 22), 22f, TextAnchor.MiddleLeft);

        Sprite logo = f.Assets.OptionalSprite(UiAssetLibrary.LOGO_SPRITE_NAME);
        if (logo != null)
        {
            Image emblem = f.Icon(card, "Logo", logo, UiTheme.White, CLUB_LOGO_SIZE);
            UiFactory.Layout(emblem, CLUB_LOGO_SIZE, CLUB_LOGO_SIZE);
        }

        RectTransform text = f.Rect(card, "Text");
        UiFactory.Layout(text, -1f, CLUB_LOGO_SIZE, 1f);
        f.VerticalLayout(text, new RectOffset(0, 0, 0, 0), 6f, TextAnchor.MiddleLeft);

        TextMeshProUGUI name = f.Text(text, "Name", "KNI-PM-Szczecin", f.Assets.Body, 28f, UiTheme.White, TextAlignmentOptions.MidlineLeft);
        UiFactory.Layout(name, -1f, 34f);

        TextMeshProUGUI subtitle = f.Text(text, "Subtitle", "Koło Naukowe Informatyki · Politechnika Morska w Szczecinie",
            f.Assets.BodyMedium, 20f, UiTheme.TextMuted, TextAlignmentOptions.MidlineLeft);
        subtitle.textWrappingMode = TextWrappingModes.Normal;
        UiFactory.Layout(subtitle, -1f, 30f);
    }

    private static void BuildTeamCard(UiFactory f, RectTransform column)
    {
        RectTransform card = f.Card(column, "TeamCard", UiTheme.Alpha(UiTheme.Main1, 0.92f), UiTheme.Line);
        f.VerticalLayout(card, new RectOffset(28, 28, 22, 22), 10f, TextAnchor.UpperLeft);

        TextMeshProUGUI caption = f.Caption(card, "Caption", "Programiści", UiTheme.Main2);
        UiFactory.Layout(caption, -1f, 24f);

        PersonRow(f, card, "Dominik", "Dominik Mech", "@invisibleF0x");
        PersonRow(f, card, "Scarlet", "Scarlet Dorożalska", "@scarletsun");
    }

    private static void PersonRow(UiFactory f, RectTransform card, string name, string person, string handle)
    {
        RectTransform row = f.Rect(card, name);
        UiFactory.Layout(row, -1f, 34f);
        f.HorizontalLayout(row, new RectOffset(0, 0, 0, 0), 12f, TextAnchor.MiddleLeft);

        TextMeshProUGUI label = f.Text(row, "Name", person, f.Assets.Body, 23f, UiTheme.White, TextAlignmentOptions.MidlineLeft);
        UiFactory.Layout(label, -1f, -1f, 1f);

        TextMeshProUGUI tag = f.Text(row, "Handle", handle, f.Assets.BodyMedium, 20f, UiTheme.TextMuted, TextAlignmentOptions.MidlineRight);
        UiFactory.Layout(tag, HANDLE_WIDTH, 30f);
    }

    private static void BuildAssetsCard(UiFactory f, RectTransform column)
    {
        RectTransform card = f.Card(column, "AssetsCard", UiTheme.Alpha(UiTheme.Main1, 0.92f), UiTheme.Line);
        f.VerticalLayout(card, new RectOffset(28, 28, 22, 22), 8f, TextAnchor.UpperLeft);

        TextMeshProUGUI caption = f.Caption(card, "Caption", "Modele i zasoby", UiTheme.Main2);
        UiFactory.Layout(caption, -1f, 24f);

        AssetRow(f, card, "PirateKit", "Pirate Kit · Kenney", "CC0 1.0");
        AssetRow(f, card, "WatercraftKit", "Watercraft Kit · Kenney", "CC0 1.0");
        AssetRow(f, card, "FactoryKit", "Factory Kit · Kenney", "CC0 1.0");
        AssetRow(f, card, "Crane", "Dźwig · J-Toastie", "CC BY 3.0");
        AssetRow(f, card, "Skybox", "Fantasy Skybox FREE · Render Knight", "Asset Store EULA");
    }

    private static void AssetRow(UiFactory f, RectTransform card, string name, string asset, string license)
    {
        RectTransform row = f.Rect(card, name);
        UiFactory.Layout(row, -1f, 30f);
        f.HorizontalLayout(row, new RectOffset(0, 0, 0, 0), 12f, TextAnchor.MiddleLeft);

        TextMeshProUGUI label = f.Text(row, "Asset", asset, f.Assets.BodyMedium, 20f, UiTheme.TextMuted, TextAlignmentOptions.MidlineLeft);
        UiFactory.Layout(label, -1f, -1f, 1f);

        TextMeshProUGUI tag = f.Text(row, "License", license, f.Assets.BodyMedium, 18f, UiTheme.TextDim, TextAlignmentOptions.MidlineRight);
        UiFactory.Layout(tag, LICENSE_WIDTH, 26f);
    }

    private static RectTransform SettingRow(UiFactory f, RectTransform parent, string name, string label)
    {
        RectTransform row = f.Rect(parent, name);
        UiFactory.Layout(row, -1f, ROW_HEIGHT);
        f.HorizontalLayout(row, new RectOffset(0, 0, 0, 0), 20f, TextAnchor.MiddleLeft);

        TextMeshProUGUI text = f.Text(row, "Label", label, f.Assets.Body, 22f, UiTheme.White, TextAlignmentOptions.MidlineLeft);
        UiFactory.Layout(text, -1f, -1f, 1f);
        return row;
    }

    private static void BuildScoreboard(UiFactory f)
    {
        HighScoreUIManager highScores = Object.FindFirstObjectByType<HighScoreUIManager>(FindObjectsInactive.Include);
        if (highScores == null) throw new InvalidOperationException($"{SCENE_PATH} has no {nameof(HighScoreUIManager)}");

        var board = highScores.transform as RectTransform;
        UiFactory.ClearChildren(board);

        Image background = UiFactory.Ensure<Image>(board.gameObject);
        background.sprite = f.Assets.Sprite("Rect_R10");
        background.type = Image.Type.Sliced;
        background.color = UiTheme.Alpha(UiTheme.Main1Dark, 0.94f);
        background.raycastTarget = false;

        f.Outline(board, UiTheme.Alpha(UiTheme.Main2, 0.7f), true);

        RectTransform header = f.Rect(board, "Header");
        StretchTop(header, 28f, 0f, 78f);
        f.HorizontalLayout(header, new RectOffset(0, 0, 0, 0), 14f, TextAnchor.MiddleLeft);
        Image trophy = f.Icon(header, "Icon", f.Assets.Sprite("Icon_Trophy"), UiTheme.Main2, 32f);
        UiFactory.Layout(trophy, 32f, 32f);
        TextMeshProUGUI title = f.Heading(header, "Label", "Tablica wyników", 32f, UiTheme.White, TextAlignmentOptions.MidlineLeft, 4f);
        UiFactory.Layout(title, -1f, -1f, 1f);

        Image divider = f.Flat(board, "Divider", UiTheme.Alpha(UiTheme.Main2, 0.6f));
        StretchTop(divider.rectTransform, 28f, 80f, 2f);

        RectTransform columns = f.Rect(board, "TableLabels");
        StretchTop(columns, 28f, 92f, 28f);
        TextMeshProUGUI numberCaption = f.Caption(columns, "Nr", "#", UiTheme.TextDim, 15f);
        numberCaption.alignment = TextAlignmentOptions.Center;
        UiFactory.StretchHorizontal(numberCaption.rectTransform, 0f, 0.12f);
        TextMeshProUGUI nameCaption = f.Caption(columns, "Name", "Gracz", UiTheme.TextDim, 15f);
        UiFactory.StretchHorizontal(nameCaption.rectTransform, 0.12f, 0.6f);
        TextMeshProUGUI scoreCaption = f.Caption(columns, "Score", "Wynik", UiTheme.TextDim, 15f);
        scoreCaption.alignment = TextAlignmentOptions.MidlineRight;
        UiFactory.StretchHorizontal(scoreCaption.rectTransform, 0.6f, 0.98f);

        RectTransform scores = f.Rect(board, "Scores");
        UiFactory.Stretch(scores, 28f, 28f, 128f, 18f);
        f.VerticalLayout(scores, new RectOffset(0, 0, 0, 0), 2f, TextAnchor.UpperCenter);

        var entryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameHudUiBuilder.SCOREBOARD_ENTRY_PREFAB_PATH).GetComponent<ScoreboardEntryView>();
        UiWiring.Set(highScores, ("_entryPrefab", entryPrefab), ("_container", scores));
        UiWiring.SetInt(highScores, "_rowCount", 10);
    }

    private static void StretchTop(RectTransform rect, float sideInset, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(sideInset, -(top + height));
        rect.offsetMax = new Vector2(-sideInset, -top);
    }
}
