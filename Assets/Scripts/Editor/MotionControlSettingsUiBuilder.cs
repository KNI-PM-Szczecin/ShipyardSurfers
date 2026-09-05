using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MotionControlSettingsUiBuilder
{
    private const string MENU_PATH = "Shipyard Surfers/Motion Control/Build Settings UI";
    private const string SCENE_PATH = "Assets/Scenes/MainMenu.unity";
    private const string CONTAINER_NAME = "MotionControlSettings";
    private const string SETTINGS_PANEL_PROPERTY = "_settingsPanel";

    private const float ROW_HEIGHT = 64f;
    private const float ROW_SPACING = 18f;
    private const float TOGGLE_SIZE = 48f;
    private const float DROPDOWN_WIDTH = 620f;
    private const float DROPDOWN_ITEM_HEIGHT = 52f;
    private const float DROPDOWN_TEMPLATE_HEIGHT = 280f;
    private const int FONT_SIZE = 30;
    private static readonly Color TextColor = Color.white;

    [MenuItem(MENU_PATH)]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != SCENE_PATH)
        {
            scene = EditorSceneManager.OpenScene(SCENE_PATH);
        }

        MenuController menu = Object.FindFirstObjectByType<MenuController>(FindObjectsInactive.Include);
        if (menu == null)
        {
            Debug.LogError($"{nameof(MotionControlSettingsUiBuilder)}: no {nameof(MenuController)} in {SCENE_PATH}");
            return;
        }

        var panel = new SerializedObject(menu).FindProperty(SETTINGS_PANEL_PROPERTY).objectReferenceValue as GameObject;
        if (panel == null)
        {
            Debug.LogError($"{nameof(MotionControlSettingsUiBuilder)}: {nameof(MenuController)} has no settings panel assigned");
            return;
        }

        Transform existing = panel.transform.Find(CONTAINER_NAME);
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        RectTransform container = CreateContainer(panel.transform);

        Toggle enabledToggle = CreateToggleRow(container, "Sterowanie kamera", font);
        TMP_Dropdown cameraDropdown = CreateDropdownRow(container, "Kamera", font);
        Toggle mirrorToggle = CreateToggleRow(container, "Odbicie lustrzane", font);
        TMP_Text status = CreateLabel(container, "Status", string.Empty, font);
        status.alignment = TextAlignmentOptions.Center;
        status.fontStyle = FontStyles.Italic;

        MotionControlSettingsPanel view = panel.GetComponent<MotionControlSettingsPanel>();
        if (view == null) view = Undo.AddComponent<MotionControlSettingsPanel>(panel);

        var viewSo = new SerializedObject(view);
        viewSo.FindProperty("_enabledToggle").objectReferenceValue = enabledToggle;
        viewSo.FindProperty("_cameraDropdown").objectReferenceValue = cameraDropdown;
        viewSo.FindProperty("_mirrorToggle").objectReferenceValue = mirrorToggle;
        viewSo.FindProperty("_statusLabel").objectReferenceValue = status;
        viewSo.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"{nameof(MotionControlSettingsUiBuilder)}: settings UI built in {SCENE_PATH}");
    }

    private static RectTransform CreateContainer(Transform parent)
    {
        var go = new GameObject(CONTAINER_NAME, typeof(RectTransform), typeof(VerticalLayoutGroup));
        Undo.RegisterCreatedObjectUndo(go, "Create motion control settings");
        go.layer = parent.gameObject.layer;

        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.12f, 0.3f);
        rect.anchorMax = new Vector2(0.88f, 0.9f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.spacing = ROW_SPACING;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return rect;
    }

    private static RectTransform CreateRow(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        go.layer = parent.gameObject.layer;

        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        var layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        go.GetComponent<LayoutElement>().preferredHeight = ROW_HEIGHT;
        return rect;
    }

    private static TMP_Text CreateLabel(RectTransform parent, string name, string text, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
        go.layer = parent.gameObject.layer;
        go.GetComponent<RectTransform>().SetParent(parent, false);
        go.GetComponent<LayoutElement>().flexibleWidth = 1f;
        go.GetComponent<LayoutElement>().preferredHeight = ROW_HEIGHT;

        var label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = font;
        label.fontSize = FONT_SIZE;
        label.color = TextColor;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        return label;
    }

    private static Toggle CreateToggleRow(RectTransform container, string labelText, TMP_FontAsset font)
    {
        RectTransform row = CreateRow(container, labelText);
        CreateLabel(row, "Label", labelText, font);

        GameObject toggleGo = DefaultControls.CreateToggle(UguiResources());
        toggleGo.name = "Toggle";
        toggleGo.layer = row.gameObject.layer;
        toggleGo.GetComponent<RectTransform>().SetParent(row, false);
        SetLayer(toggleGo.transform, row.gameObject.layer);

        Transform legacyLabel = toggleGo.transform.Find("Label");
        if (legacyLabel != null) Object.DestroyImmediate(legacyLabel.gameObject);

        var element = toggleGo.AddComponent<LayoutElement>();
        element.preferredWidth = TOGGLE_SIZE;
        element.preferredHeight = TOGGLE_SIZE;

        var background = toggleGo.transform.Find("Background") as RectTransform;
        if (background != null)
        {
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;

            var checkmark = background.Find("Checkmark") as RectTransform;
            if (checkmark != null)
            {
                checkmark.anchorMin = Vector2.zero;
                checkmark.anchorMax = Vector2.one;
                checkmark.offsetMin = new Vector2(6f, 6f);
                checkmark.offsetMax = new Vector2(-6f, -6f);
            }
        }

        return toggleGo.GetComponent<Toggle>();
    }

    private static TMP_Dropdown CreateDropdownRow(RectTransform container, string labelText, TMP_FontAsset font)
    {
        RectTransform row = CreateRow(container, labelText);
        CreateLabel(row, "Label", labelText, font);

        GameObject dropdownGo = TMP_DefaultControls.CreateDropdown(TmpResources());
        dropdownGo.name = "Dropdown";
        dropdownGo.GetComponent<RectTransform>().SetParent(row, false);
        SetLayer(dropdownGo.transform, row.gameObject.layer);

        var element = dropdownGo.AddComponent<LayoutElement>();
        element.preferredWidth = DROPDOWN_WIDTH;
        element.preferredHeight = ROW_HEIGHT;

        var dropdown = dropdownGo.GetComponent<TMP_Dropdown>();
        StyleText(dropdown.captionText, font);
        StyleText(dropdown.itemText, font);

        var template = dropdown.template;
        template.sizeDelta = new Vector2(template.sizeDelta.x, DROPDOWN_TEMPLATE_HEIGHT);

        var item = dropdown.itemText.transform.parent as RectTransform;
        if (item != null) item.sizeDelta = new Vector2(item.sizeDelta.x, DROPDOWN_ITEM_HEIGHT);

        var itemToggle = item != null ? item.GetComponent<Toggle>() : null;
        if (itemToggle != null && itemToggle.targetGraphic is Graphic itemBackground)
        {
            var backgroundRect = itemBackground.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
        }

        var content = item != null ? item.parent as RectTransform : null;
        if (content != null) content.sizeDelta = new Vector2(content.sizeDelta.x, DROPDOWN_ITEM_HEIGHT);

        return dropdown;
    }

    private static void StyleText(TMP_Text text, TMP_FontAsset font)
    {
        if (text == null) return;
        text.font = font;
        text.fontSize = FONT_SIZE - 4;
    }

    private static void SetLayer(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root) SetLayer(child, layer);
    }

    private static DefaultControls.Resources UguiResources()
    {
        return new DefaultControls.Resources
        {
            standard = BuiltinSprite("UISprite.psd"),
            background = BuiltinSprite("Background.psd"),
            inputField = BuiltinSprite("InputFieldBackground.psd"),
            knob = BuiltinSprite("Knob.psd"),
            checkmark = BuiltinSprite("Checkmark.psd"),
            dropdown = BuiltinSprite("DropdownArrow.psd"),
            mask = BuiltinSprite("UIMask.psd")
        };
    }

    private static TMP_DefaultControls.Resources TmpResources()
    {
        return new TMP_DefaultControls.Resources
        {
            standard = BuiltinSprite("UISprite.psd"),
            background = BuiltinSprite("Background.psd"),
            inputField = BuiltinSprite("InputFieldBackground.psd"),
            knob = BuiltinSprite("Knob.psd"),
            checkmark = BuiltinSprite("Checkmark.psd"),
            dropdown = BuiltinSprite("DropdownArrow.psd"),
            mask = BuiltinSprite("UIMask.psd")
        };
    }

    private static Sprite BuiltinSprite(string fileName)
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/" + fileName);
    }
}
