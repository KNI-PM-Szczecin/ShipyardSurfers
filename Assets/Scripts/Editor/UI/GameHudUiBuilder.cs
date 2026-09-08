using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GameHudUiBuilder
{
    public const string GAME_UI_PREFAB_PATH = "Assets/Prefabs/UI/GM&UI.prefab";
    public const string POWER_UP_SLIDER_PREFAB_PATH = "Assets/Prefabs/UI/PowerUpTimerSlider.prefab";
    public const string SCOREBOARD_ENTRY_PREFAB_PATH = "Assets/Prefabs/UI/ScoreboardEntry.prefab";
    public const string MENU_SCENE_PATH = "Assets/Scenes/MainMenu.unity";
    public const float SCOREBOARD_ROW_HEIGHT = 35f;

    private const float POWER_UP_WIDTH = 340f;
    private const float POWER_UP_HEIGHT = 64f;
    private const float CARD_WIDTH = 860f;

    public static void BuildAll(UiAssetLibrary assets)
    {
        var factory = new UiFactory(assets);
        BuildScoreboardEntry(factory);
        BuildPowerUpSlider(factory);
        BuildGameUi(factory);
    }

    private static void BuildScoreboardEntry(UiFactory f)
    {
        EditPrefab(SCOREBOARD_ENTRY_PREFAB_PATH, root =>
        {
            RectTransform rect = ResetRoot(root);
            rect.sizeDelta = new Vector2(860f, SCOREBOARD_ROW_HEIGHT);
            UiFactory.Layout(rect, -1f, SCOREBOARD_ROW_HEIGHT);

            Image rowBackground = f.Panel(rect, "RowBackground", UiTheme.Alpha(UiTheme.White, 0.04f));
            UiFactory.Stretch(rowBackground.rectTransform);
            rowBackground.pixelsPerUnitMultiplier = 1.5f;

            Image badge = f.Panel(rect, "RankBadge", UiTheme.Alpha(UiTheme.White, 0.12f));
            UiFactory.PlaceCentered(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(26f, 26f));
            badge.pixelsPerUnitMultiplier = 1.5f;

            TextMeshProUGUI position = f.Text(badge.transform, "Position", "1", f.Assets.BodyBold, 14f, UiTheme.White, TextAlignmentOptions.Center);
            UiFactory.Stretch(position.rectTransform);

            TextMeshProUGUI name = f.Text(rect, "Name", "Bosman", f.Assets.Body, 20f, UiTheme.White, TextAlignmentOptions.MidlineLeft);
            UiFactory.StretchHorizontal(name.rectTransform, 0.12f, 0.6f);
            name.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI score = f.Text(rect, "Score", "10 020", f.Assets.BodyBold, 20f, UiTheme.White, TextAlignmentOptions.MidlineRight);
            UiFactory.StretchHorizontal(score.rectTransform, 0.6f, 0.98f);

            var view = root.AddComponent<ScoreboardEntryView>();
            UiWiring.Set(view,
                ("_positionText", position),
                ("_nameText", name),
                ("_scoreText", score),
                ("_rankBadge", badge),
                ("_rowBackground", rowBackground));
            UiWiring.SetColors(view, "_medalColors", UiTheme.Main2, UiTheme.Alpha(UiTheme.Main2, 0.7f), UiTheme.Alpha(UiTheme.Main2, 0.45f));
            UiWiring.SetColor(view, "_defaultBadgeColor", UiTheme.Alpha(UiTheme.White, 0.12f));
            UiWiring.SetColor(view, "_medalTextColor", UiTheme.White);
            UiWiring.SetColor(view, "_defaultTextColor", UiTheme.White);
            UiWiring.SetColor(view, "_emptyTextColor", UiTheme.TextDim);
            UiWiring.SetColor(view, "_nameColor", UiTheme.White);
            UiWiring.SetColor(view, "_scoreColor", UiTheme.White);
            UiWiring.SetColor(view, "_evenRowColor", UiTheme.Alpha(UiTheme.White, 0f));
            UiWiring.SetColor(view, "_oddRowColor", UiTheme.Alpha(UiTheme.White, 0.05f));
        });
    }

    private static void BuildPowerUpSlider(UiFactory f)
    {
        EditPrefab(POWER_UP_SLIDER_PREFAB_PATH, root =>
        {
            RectTransform rect = ResetRoot(root);
            rect.sizeDelta = new Vector2(POWER_UP_WIDTH, POWER_UP_HEIGHT);
            UiFactory.Layout(rect, POWER_UP_WIDTH, POWER_UP_HEIGHT);

            Image body = f.Panel(rect, "Body", UiTheme.Alpha(UiTheme.Main1, 0.85f));
            UiFactory.Stretch(body.rectTransform);

            Image iconBackground = f.Panel(rect, "IconBackground", UiTheme.Main2);
            UiFactory.PlaceCentered(iconBackground.rectTransform, new Vector2(0f, 0.5f), new Vector2(32f, 0f), new Vector2(40f, 40f));

            Image icon = f.Icon(iconBackground.transform, "Icon", null, UiTheme.White, 24f);
            UiFactory.PlaceCentered(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));

            TextMeshProUGUI label = f.Text(rect, "Label", "Power up", f.Assets.Body, 20f, UiTheme.White, TextAlignmentOptions.TopLeft);
            UiFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(66f, -10f), new Vector2(190f, 26f));

            TextMeshProUGUI time = f.Text(rect, "Time", "0.0 s", f.Assets.BodyMedium, 18f, UiTheme.TextMuted, TextAlignmentOptions.TopRight);
            UiFactory.Place(time.rectTransform, new Vector2(1f, 1f), new Vector2(-16f, -10f), new Vector2(90f, 26f));

            Image barBackground = f.Flat(rect, "Background", UiTheme.Alpha(UiTheme.White, 0.15f));
            PlaceBar(barBackground.rectTransform);

            RectTransform fillArea = f.Rect(rect, "Fill Area");
            PlaceBar(fillArea);

            Image fill = f.Flat(fillArea, "Fill", UiTheme.Cyan);
            UiFactory.Stretch(fill.rectTransform);

            var slider = root.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = null;
            slider.direction = Slider.Direction.LeftToRight;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            var view = root.AddComponent<PowerUpTimerSlider>();
            UiWiring.Set(view,
                ("_slider", slider),
                ("_fill", fill),
                ("_icon", icon),
                ("_label", label),
                ("_timeText", time));
        });
    }

    private static void PlaceBar(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(66f, 12f);
        rect.offsetMax = new Vector2(-16f, 18f);
    }

    private static void BuildGameUi(UiFactory f)
    {
        EditPrefab(GAME_UI_PREFAB_PATH, root =>
        {
            Transform canvasTransform = root.transform.Find("Canvas");
            if (canvasTransform == null) throw new InvalidOperationException($"{GAME_UI_PREFAB_PATH} has no Canvas child");

            UiFactory.ConfigureOverlayCanvas(canvasTransform.GetComponent<Canvas>());
            UiFactory.ClearChildren(canvasTransform);

            RectTransform gameplay = BuildGameplayUi(f, canvasTransform, out TextMeshProUGUI scoreText);
            RectTransform death = BuildDeathUi(f, canvasTransform);
            death.gameObject.SetActive(false);

            GameUIManager gameUi = root.GetComponentInChildren<GameUIManager>(true);
            if (gameUi == null) throw new InvalidOperationException($"{GAME_UI_PREFAB_PATH} has no {nameof(GameUIManager)}");
            UiWiring.Set(gameUi,
                ("ScoreText", scoreText),
                ("EndScreen", death.gameObject),
                ("GameplayPanel", gameplay.gameObject));
        });
    }

    private static RectTransform BuildGameplayUi(UiFactory f, Transform canvas, out TextMeshProUGUI scoreText)
    {
        RectTransform gameplay = f.Rect(canvas, "GameplayUI");
        UiFactory.Stretch(gameplay);

        RectTransform scorePanel = f.Card(gameplay, "ScorePanel", UiTheme.Alpha(UiTheme.Main1, 0.8f), UiTheme.Alpha(UiTheme.Main2, 0.5f));
        UiFactory.Place(scorePanel, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(360f, 104f));

        TextMeshProUGUI caption = f.Caption(scorePanel, "Caption", "Wynik", UiTheme.TextMuted, 18f);
        caption.alignment = TextAlignmentOptions.Center;
        UiFactory.Place(caption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(300f, 24f));

        scoreText = f.Text(scorePanel, "ScoreText", "0", f.Assets.Display, 56f, UiTheme.White, TextAlignmentOptions.Center);
        UiFactory.Place(scoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(340f, 60f));
        UiFactory.AutoSize(scoreText, 32f, 56f);

        RectTransform powerUpPanel = f.Rect(gameplay, "PowerUpPanel");
        UiFactory.Place(powerUpPanel, new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(POWER_UP_WIDTH, 0f));
        f.VerticalLayout(powerUpPanel, new RectOffset(0, 0, 0, 0), 10f, TextAnchor.LowerRight);
        powerUpPanel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var powerUps = powerUpPanel.gameObject.AddComponent<PowerUpUIManager>();
        var sliderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(POWER_UP_SLIDER_PREFAB_PATH).GetComponent<PowerUpTimerSlider>();
        UiWiring.Set(powerUps, ("_sliderPrefab", sliderPrefab), ("_container", powerUpPanel));
        WritePowerUpVisuals(f, powerUps);

        return gameplay;
    }

    private static void WritePowerUpVisuals(UiFactory f, PowerUpUIManager powerUps)
    {
        (PowerUpType type, string label, string icon)[] visuals =
        {
            (PowerUpType.CoinMagnet, "Magnes", "Icon_Magnet"),
            (PowerUpType.SuperJump, "Super skok", "Icon_Jump"),
            (PowerUpType.DoublePoints, "2x punkty", "Icon_X2"),
        };

        var so = new SerializedObject(powerUps);
        SerializedProperty array = UiWiring.Property(so, "_visuals");
        array.arraySize = visuals.Length;
        for (int i = 0; i < visuals.Length; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("Type").intValue = (int)visuals[i].type;
            element.FindPropertyRelative("Label").stringValue = visuals[i].label;
            element.FindPropertyRelative("Color").colorValue = UiTheme.Cyan;
            element.FindPropertyRelative("Icon").objectReferenceValue = f.Assets.Sprite(visuals[i].icon);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform BuildDeathUi(UiFactory f, Transform canvas)
    {
        RectTransform death = f.Rect(canvas, "DeathUI");
        UiFactory.Stretch(death);

        var overlay = death.gameObject.AddComponent<Image>();
        overlay.color = UiTheme.Alpha(UiTheme.Main1Dark, 0.88f);
        overlay.raycastTarget = true;

        Image vignette = f.Image(death, "Vignette", f.Assets.Sprite("Vignette"), UiTheme.Alpha(Color.black, 0.5f), Image.Type.Simple);
        UiFactory.Stretch(vignette.rectTransform);

        RectTransform card = f.Card(death, "Card", UiTheme.Alpha(UiTheme.Main1, 0.97f), UiTheme.Line);
        UiFactory.PlaceCentered(card, new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(CARD_WIDTH, 600f));
        card.gameObject.AddComponent<CanvasGroup>();
        card.gameObject.AddComponent<PanelAppearAnimation>();
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        f.VerticalLayout(card, new RectOffset(56, 56, 44, 44), 12f, TextAnchor.UpperCenter);

        TextMeshProUGUI title = f.Heading(card, "Title", "Koniec gry", 64f, UiTheme.Red, TextAlignmentOptions.Center);
        UiFactory.Layout(title, -1f, 76f);

        TextMeshProUGUI subtitle = f.Text(card, "Subtitle", "Świetny bieg! Oto Twój wynik", f.Assets.BodyMedium, 22f, UiTheme.TextMuted, TextAlignmentOptions.Center);
        UiFactory.Layout(subtitle, -1f, 30f);

        TextMeshProUGUI score = f.Text(card, "ScoreText", "0", f.Assets.Display, 112f, UiTheme.White, TextAlignmentOptions.Center);
        UiFactory.AutoSize(score, 64f, 112f);
        UiFactory.Layout(score, -1f, 124f);

        TextMeshProUGUI bestScore = BuildBestRow(f, card);
        RectTransform newRecordBadge = BuildNewRecordBadge(f, card);
        f.Spacer(card, 8f);

        RectTransform actions = f.Rect(card, "Actions");
        UiFactory.Layout(actions, -1f, UiTheme.BUTTON_HEIGHT);
        f.HorizontalLayout(actions, new RectOffset(0, 0, 0, 0), 14f, TextAnchor.MiddleCenter, true);
        Button save = f.Button(actions, "SaveScoreOptionButton", "Zapisz wynik", null, UiButtonStyle.Primary, UiTheme.BUTTON_HEIGHT, UiTheme.BUTTON_LABEL_SIZE_SMALL);
        Button restart = f.Button(actions, "RestartButton", "Jeszcze raz", null, UiButtonStyle.Secondary, UiTheme.BUTTON_HEIGHT, UiTheme.BUTTON_LABEL_SIZE_SMALL);
        Button menu = f.Button(actions, "EndButton", "Menu", null, UiButtonStyle.Ghost, UiTheme.BUTTON_HEIGHT, UiTheme.BUTTON_LABEL_SIZE_SMALL);
        foreach (Button button in new[] { save, restart, menu })
        {
            UiFactory.Layout(ButtonRoot(button), -1f, UiTheme.BUTTON_HEIGHT, 1f);
        }

        RectTransform savePanel = f.Rect(card, "SaveScorePanel");
        f.VerticalLayout(savePanel, new RectOffset(0, 0, 0, 0), 12f, TextAnchor.UpperCenter);

        TextMeshProUGUI saveLabel = f.Text(savePanel, "Username", "Podpisz swój wynik", f.Assets.Body, 22f, UiTheme.White, TextAlignmentOptions.Center);
        UiFactory.Layout(saveLabel, -1f, 28f);

        RectTransform inputRow = f.Rect(savePanel, "InputRow");
        UiFactory.Layout(inputRow, -1f, UiTheme.BUTTON_HEIGHT);
        f.HorizontalLayout(inputRow, new RectOffset(0, 0, 0, 0), 14f, TextAnchor.MiddleCenter);
        TMP_InputField input = f.InputField(inputRow, "UsernameInput", "Wpisz nazwę gracza...");
        UiFactory.Layout(input, -1f, UiTheme.BUTTON_HEIGHT, 1f);
        Button confirm = f.Button(inputRow, "SaveScoreButton", "Zapisz", null, UiButtonStyle.Primary, UiTheme.BUTTON_HEIGHT, UiTheme.BUTTON_LABEL_SIZE_SMALL);
        UiFactory.Layout(ButtonRoot(confirm), 200f, UiTheme.BUTTON_HEIGHT);

        Button cancel = f.Button(savePanel, "CancelButton", "Wróć", null, UiButtonStyle.Ghost, UiTheme.BUTTON_HEIGHT_SMALL, 20f);
        UiFactory.Layout(ButtonRoot(cancel), -1f, UiTheme.BUTTON_HEIGHT_SMALL);
        savePanel.gameObject.SetActive(false);

        var manager = death.gameObject.AddComponent<DeathUIManager>();
        UiWiring.Set(manager,
            ("_scoreText", score),
            ("_bestScoreText", bestScore),
            ("_newRecordBadge", newRecordBadge.gameObject),
            ("_actionsRow", actions.gameObject),
            ("_scoreSavePanel", savePanel.gameObject),
            ("_scoreSaveInput", input),
            ("_saveConfirmButton", confirm),
            ("MenuScene", AssetDatabase.LoadAssetAtPath<SceneAsset>(MENU_SCENE_PATH)));
        UiWiring.SetString(manager, "MenuSceneName", System.IO.Path.GetFileNameWithoutExtension(MENU_SCENE_PATH));

        UiWiring.OnClick(save, manager.EnableSaveScoreInput);
        UiWiring.OnClick(restart, manager.Restart);
        UiWiring.OnClick(menu, manager.MainMenu);
        UiWiring.OnClick(confirm, manager.SaveScore);
        UiWiring.OnClick(cancel, manager.CancelSaveScoreInput);

        return death;
    }

    private static TextMeshProUGUI BuildBestRow(UiFactory f, RectTransform parent)
    {
        RectTransform row = f.Rect(parent, "BestRow");
        UiFactory.Layout(row, -1f, 30f);
        f.HorizontalLayout(row, new RectOffset(0, 0, 0, 0), 10f, TextAnchor.MiddleCenter);

        Image trophy = f.Icon(row, "Icon", f.Assets.Sprite("Icon_Trophy"), UiTheme.Main2, 20f);
        UiFactory.Layout(trophy, 20f, 20f);

        TextMeshProUGUI caption = f.Caption(row, "Caption", "Rekord", UiTheme.TextDim, 16f);
        caption.alignment = TextAlignmentOptions.Center;

        return f.Text(row, "BestScoreText", "0", f.Assets.BodyBold, 22f, UiTheme.White, TextAlignmentOptions.Center);
    }

    private static RectTransform BuildNewRecordBadge(UiFactory f, RectTransform parent)
    {
        RectTransform holder = f.Rect(parent, "NewRecordBadge");
        UiFactory.Layout(holder, -1f, 42f);

        Image badge = f.Image(holder, "Badge", f.Assets.Sprite("Outline_R6"), UiTheme.Red);
        UiFactory.PlaceCentered(badge.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 40f));
        HorizontalLayoutGroup layout = f.HorizontalLayout(badge.rectTransform, new RectOffset(20, 20, 0, 0), 10f, TextAnchor.MiddleCenter);
        layout.childForceExpandHeight = true;

        Image star = f.Icon(badge.transform, "Icon", f.Assets.Sprite("Icon_Star"), UiTheme.Red, 18f);
        UiFactory.Layout(star, 18f, 18f);

        TextMeshProUGUI text = f.Text(badge.transform, "Text", "Nowy rekord!", f.Assets.BodyBold, 20f, UiTheme.Red, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.UpperCase;
        text.characterSpacing = 4f;

        holder.gameObject.SetActive(false);
        return holder;
    }

    public static RectTransform ButtonRoot(Button button)
    {
        return button.transform as RectTransform;
    }

    private static void EditPrefab(string path, Action<GameObject> edit)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"{nameof(GameHudUiBuilder)}: rebuilt {path}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static RectTransform ResetRoot(GameObject root)
    {
        UiFactory.ClearChildren(root.transform);
        root.layer = 5;

        bool removedAny = true;
        while (removedAny)
        {
            removedAny = false;
            foreach (Component component in root.GetComponents<Component>())
            {
                if (component is Transform) continue;
                if (TryDestroy(component)) removedAny = true;
            }
        }

        var rect = root.GetComponent<RectTransform>();
        if (rect == null) rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        return rect;
    }

    private static bool TryDestroy(Component component)
    {
        try
        {
            UnityEngine.Object.DestroyImmediate(component);
            return component == null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
