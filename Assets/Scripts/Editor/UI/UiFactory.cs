using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public enum UiButtonStyle
{
    Primary,
    Secondary,
    Ghost,
    Danger
}

public class UiFactory
{
    private const int UI_LAYER = 5;
    private const string PANEL_SPRITE = "Rect_R6";
    private const string CARD_SPRITE = "Rect_R10";
    private const string PANEL_OUTLINE_SPRITE = "Outline_R6";
    private const string CARD_OUTLINE_SPRITE = "Outline_R10";

    private readonly UiAssetLibrary _assets;

    public UiFactory(UiAssetLibrary assets)
    {
        _assets = assets;
    }

    public UiAssetLibrary Assets => _assets;

    public RectTransform Rect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = UI_LAYER;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(100f, 100f);
        return rect;
    }

    public static RectTransform Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        return rect;
    }

    public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public static RectTransform PlaceCentered(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public static RectTransform StretchHorizontal(RectTransform rect, float anchorMinX, float anchorMaxX, float left = 0f, float right = 0f)
    {
        rect.anchorMin = new Vector2(anchorMinX, 0f);
        rect.anchorMax = new Vector2(anchorMaxX, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, 0f);
        rect.offsetMax = new Vector2(-right, 0f);
        return rect;
    }

    public Image Image(Transform parent, string name, Sprite sprite, Color color, Image.Type type = UnityEngine.UI.Image.Type.Sliced, bool raycastTarget = false)
    {
        RectTransform rect = Rect(parent, name);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = sprite != null ? type : UnityEngine.UI.Image.Type.Simple;
        image.raycastTarget = raycastTarget;
        image.maskable = true;
        return image;
    }

    public Image Flat(Transform parent, string name, Color color)
    {
        return Image(parent, name, null, color, UnityEngine.UI.Image.Type.Simple);
    }

    public Image Panel(Transform parent, string name, Color color, bool raycastTarget = false)
    {
        return Image(parent, name, _assets.Sprite(PANEL_SPRITE), color, UnityEngine.UI.Image.Type.Sliced, raycastTarget);
    }

    public Image Icon(Transform parent, string name, Sprite sprite, Color color, float size)
    {
        Image icon = Image(parent, name, sprite, color, UnityEngine.UI.Image.Type.Simple);
        icon.preserveAspect = true;
        icon.rectTransform.sizeDelta = new Vector2(size, size);
        return icon;
    }

    public Image Outline(RectTransform parent, Color color, bool card = false)
    {
        Image outline = Image(parent, "Outline", _assets.Sprite(card ? CARD_OUTLINE_SPRITE : PANEL_OUTLINE_SPRITE), color);
        Stretch(outline.rectTransform);
        outline.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        return outline;
    }

    public TextMeshProUGUI Text(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = Rect(parent, name);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.richText = false;
        return label;
    }

    public TextMeshProUGUI Heading(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions alignment, float spacing = UiTheme.LABEL_SPACING)
    {
        TextMeshProUGUI heading = Text(parent, name, text, _assets.Display, size, color, alignment);
        heading.fontStyle = FontStyles.UpperCase;
        heading.characterSpacing = spacing;
        return heading;
    }

    public TextMeshProUGUI Caption(Transform parent, string name, string text, Color color, float size = 18f)
    {
        TextMeshProUGUI caption = Text(parent, name, text, _assets.Body, size, color, TextAlignmentOptions.MidlineLeft);
        caption.fontStyle = FontStyles.UpperCase;
        caption.characterSpacing = UiTheme.CAPTION_SPACING;
        return caption;
    }

    public static void AutoSize(TMP_Text text, float min, float max)
    {
        text.enableAutoSizing = true;
        text.fontSizeMin = min;
        text.fontSizeMax = max;
    }

    public RectTransform Card(Transform parent, string name, Color fill, Color outline)
    {
        Image body = Image(parent, name, _assets.Sprite(CARD_SPRITE), fill);
        Outline(body.rectTransform, outline, true);
        return body.rectTransform;
    }

    public VerticalLayoutGroup VerticalLayout(RectTransform rect, RectOffset padding, float spacing, TextAnchor alignment, bool expandWidth = true)
    {
        var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = expandWidth;
        layout.childForceExpandHeight = false;
        return layout;
    }

    public HorizontalLayoutGroup HorizontalLayout(RectTransform rect, RectOffset padding, float spacing, TextAnchor alignment, bool expandWidth = false)
    {
        var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = expandWidth;
        layout.childForceExpandHeight = false;
        return layout;
    }

    public static LayoutElement Layout(Component target, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f)
    {
        var element = target.gameObject.GetComponent<LayoutElement>();
        if (element == null) element = target.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = preferredWidth;
        element.preferredHeight = preferredHeight;
        element.flexibleWidth = flexibleWidth;
        return element;
    }

    public RectTransform Spacer(Transform parent, float height)
    {
        RectTransform spacer = Rect(parent, "Spacer");
        Layout(spacer, -1f, height);
        return spacer;
    }

    public Image Divider(Transform parent, Color color, float height = 2f)
    {
        Image divider = Flat(parent, "Divider", color);
        Layout(divider, -1f, height);
        return divider;
    }

    public Button Button(Transform parent, string name, string label, Sprite icon, UiButtonStyle style, float height = UiTheme.BUTTON_HEIGHT, float labelSize = UiTheme.BUTTON_LABEL_SIZE)
    {
        Image body = Panel(parent, name, FillColor(style), true);
        RectTransform root = body.rectTransform;
        root.sizeDelta = new Vector2(320f, height);
        Layout(root, -1f, height);
        AddOutline(root, style);

        Button button = AddButton(body, style);

        HorizontalLayoutGroup content = HorizontalLayout(root, new RectOffset(24, 24, 0, 0), 12f, TextAnchor.MiddleCenter);
        content.childForceExpandHeight = true;

        Color labelColor = LabelColor(style);
        if (icon != null)
        {
            float iconSize = Mathf.Min(UiTheme.BUTTON_ICON_SIZE, height * 0.4f);
            Image iconImage = Icon(root, "Icon", icon, labelColor, iconSize);
            Layout(iconImage, iconSize, iconSize);
        }

        TextMeshProUGUI text = Text(root, "Label", label, _assets.Body, labelSize, labelColor, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.UpperCase;
        text.characterSpacing = UiTheme.LABEL_SPACING;
        Layout(text, -1f, -1f, 0f);

        return button;
    }

    public Button IconButton(Transform parent, string name, Sprite icon, UiButtonStyle style, float size)
    {
        Image body = Panel(parent, name, FillColor(style), true);
        RectTransform root = body.rectTransform;
        root.sizeDelta = new Vector2(size, size);
        Layout(root, size, size);
        AddOutline(root, style);

        Button button = AddButton(body, style);

        float iconSize = Mathf.Round(size * 0.42f);
        Image iconImage = Icon(root, "Icon", icon, LabelColor(style), iconSize);
        PlaceCentered(iconImage.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(iconSize, iconSize));

        return button;
    }

    public static void ConfigureOverlayCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = Ensure<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UiTheme.REFERENCE_RESOLUTION;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.referencePixelsPerUnit = 100f;

        Ensure<GraphicRaycaster>(canvas.gameObject);
        Ensure<DynamicCanvasScaler>(canvas.gameObject);
    }

    public Toggle Switch(Transform parent, string name)
    {
        const float width = 56f;
        const float height = 28f;
        const float knobSize = 20f;
        const float margin = 4f;

        RectTransform root = Rect(parent, name);
        root.sizeDelta = new Vector2(width, height);
        Layout(root, width, height);

        Image track = Panel(root, "Track", UiTheme.Alpha(UiTheme.White, 0.2f), true);
        Stretch(track.rectTransform);

        Image knob = Panel(track.transform, "Knob", UiTheme.White);
        PlaceCentered(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-(width - knobSize) * 0.5f + margin, 0f), new Vector2(knobSize, knobSize));

        var toggle = root.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = track;
        toggle.graphic = null;
        toggle.transition = Selectable.Transition.ColorTint;
        toggle.colors = InteractionColors();
        toggle.navigation = new Navigation { mode = Navigation.Mode.None };
        toggle.isOn = false;

        var view = root.gameObject.AddComponent<ToggleSwitchView>();
        var viewSo = new SerializedObject(view);
        viewSo.FindProperty("_knob").objectReferenceValue = knob.rectTransform;
        viewSo.FindProperty("_track").objectReferenceValue = track;
        viewSo.FindProperty("_onTrackColor").colorValue = UiTheme.Main2;
        viewSo.FindProperty("_offTrackColor").colorValue = UiTheme.Alpha(UiTheme.White, 0.2f);
        viewSo.FindProperty("_knobTravel").floatValue = width - knobSize - margin * 2f;
        viewSo.ApplyModifiedPropertiesWithoutUndo();

        return toggle;
    }

    public TMP_Dropdown Dropdown(Transform parent, string name, Vector2 size)
    {
        GameObject go = TMP_DefaultControls.CreateDropdown(BuiltinResources());
        go.name = name;
        SetLayerRecursive(go.transform, UI_LAYER);
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        Layout(rect, size.x, size.y);

        var dropdown = go.GetComponent<TMP_Dropdown>();
        var background = go.GetComponent<Image>();
        background.sprite = _assets.Sprite(PANEL_SPRITE);
        background.type = UnityEngine.UI.Image.Type.Sliced;
        background.color = UiTheme.Main1;
        Outline(rect, UiTheme.Line);
        dropdown.transition = Selectable.Transition.ColorTint;
        dropdown.colors = InteractionColors();
        dropdown.navigation = new Navigation { mode = Navigation.Mode.None };

        StyleDropdownText(dropdown.captionText, _assets.Body, 20f, UiTheme.White);
        var captionRect = dropdown.captionText.rectTransform;
        captionRect.offsetMin = new Vector2(16f, 4f);
        captionRect.offsetMax = new Vector2(-44f, -4f);

        var arrow = go.transform.Find("Arrow")?.GetComponent<Image>();
        if (arrow != null)
        {
            arrow.sprite = _assets.Sprite("Icon_ChevronDown");
            arrow.color = UiTheme.TextMuted;
            arrow.preserveAspect = true;
            arrow.rectTransform.sizeDelta = new Vector2(18f, 18f);
            arrow.rectTransform.anchoredPosition = new Vector2(-20f, 0f);
        }

        RectTransform template = dropdown.template;
        template.sizeDelta = new Vector2(template.sizeDelta.x, 204f);
        template.anchoredPosition = new Vector2(0f, -4f);
        var templateImage = template.GetComponent<Image>();
        templateImage.sprite = _assets.Sprite(PANEL_SPRITE);
        templateImage.type = UnityEngine.UI.Image.Type.Sliced;
        templateImage.color = UiTheme.Main1Dark;
        Outline(template, UiTheme.Line);

        var scrollRect = template.GetComponent<ScrollRect>();
        if (scrollRect != null) scrollRect.scrollSensitivity = 30f;

        var viewport = template.Find("Viewport") as RectTransform;
        if (viewport != null)
        {
            viewport.offsetMin = new Vector2(4f, 4f);
            viewport.offsetMax = new Vector2(-4f, -4f);
            var viewportImage = viewport.GetComponent<Image>();
            if (viewportImage != null)
            {
                viewportImage.sprite = _assets.Sprite(PANEL_SPRITE);
                viewportImage.type = UnityEngine.UI.Image.Type.Sliced;
            }
        }

        var item = dropdown.itemText.transform.parent as RectTransform;
        const float itemHeight = 44f;
        if (item != null)
        {
            item.sizeDelta = new Vector2(item.sizeDelta.x, itemHeight);
            var itemToggle = item.GetComponent<Toggle>();
            if (itemToggle != null)
            {
                itemToggle.colors = InteractionColors();
                itemToggle.navigation = new Navigation { mode = Navigation.Mode.None };
                if (itemToggle.targetGraphic is Image itemBackground)
                {
                    itemBackground.sprite = _assets.Sprite(PANEL_SPRITE);
                    itemBackground.type = UnityEngine.UI.Image.Type.Sliced;
                    itemBackground.color = UiTheme.Main1;
                    Stretch(itemBackground.rectTransform, 2f, 2f, 1f, 1f);
                }

                if (itemToggle.graphic is Image checkmark)
                {
                    checkmark.sprite = _assets.Sprite("Icon_Check");
                    checkmark.color = UiTheme.Cyan;
                    checkmark.preserveAspect = true;
                    checkmark.rectTransform.sizeDelta = new Vector2(18f, 18f);
                    checkmark.rectTransform.anchoredPosition = new Vector2(18f, 0f);
                }
            }

            var content = item.parent as RectTransform;
            if (content != null) content.sizeDelta = new Vector2(content.sizeDelta.x, itemHeight);
        }

        StyleDropdownText(dropdown.itemText, _assets.BodyMedium, 19f, UiTheme.White);
        dropdown.itemText.rectTransform.offsetMin = new Vector2(38f, 2f);
        dropdown.itemText.rectTransform.offsetMax = new Vector2(-10f, -2f);

        var scrollbar = template.Find("Scrollbar");
        if (scrollbar != null)
        {
            var scrollbarImage = scrollbar.GetComponent<Image>();
            if (scrollbarImage != null)
            {
                scrollbarImage.sprite = null;
                scrollbarImage.color = UiTheme.Alpha(Color.black, 0.3f);
            }

            var scrollbarRect = scrollbar as RectTransform;
            if (scrollbarRect != null) scrollbarRect.sizeDelta = new Vector2(6f, scrollbarRect.sizeDelta.y);

            var handle = scrollbar.GetComponentInChildren<Scrollbar>()?.handleRect?.GetComponent<Image>();
            if (handle != null)
            {
                handle.sprite = null;
                handle.color = UiTheme.Alpha(UiTheme.Main2, 0.9f);
            }
        }

        return dropdown;
    }

    public TMP_InputField InputField(Transform parent, string name, string placeholderText, float fontSize = 26f, int characterLimit = 16)
    {
        Image background = Panel(parent, name, UiTheme.Alpha(UiTheme.Main1Dark, 0.9f), true);
        RectTransform root = background.rectTransform;
        Outline(root, UiTheme.Line);

        RectTransform textArea = Rect(root, "Text Area");
        Stretch(textArea, 18f, 18f, 6f, 6f);
        textArea.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = Text(textArea, "Placeholder", placeholderText, _assets.BodyMedium, fontSize, UiTheme.TextDim, TextAlignmentOptions.MidlineLeft);
        Stretch(placeholder.rectTransform);

        TextMeshProUGUI text = Text(textArea, "Text", string.Empty, _assets.Body, fontSize, UiTheme.White, TextAlignmentOptions.MidlineLeft);
        Stretch(text.rectTransform);

        var field = root.gameObject.AddComponent<TMP_InputField>();
        field.textViewport = textArea;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.fontAsset = _assets.Body;
        field.pointSize = fontSize;
        field.characterLimit = characterLimit;
        field.richText = false;
        field.customCaretColor = true;
        field.caretColor = UiTheme.Cyan;
        field.caretWidth = 2;
        field.selectionColor = UiTheme.Alpha(UiTheme.Main2, 0.5f);
        field.targetGraphic = background;
        field.transition = Selectable.Transition.ColorTint;
        field.colors = InteractionColors();
        field.navigation = new Navigation { mode = Navigation.Mode.None };
        return field;
    }

    public static void SetLayerRecursive(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root) SetLayerRecursive(child, layer);
    }

    public static void ClearChildren(Transform parent, params Transform[] keep)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (System.Array.IndexOf(keep, child) >= 0) continue;
            Object.DestroyImmediate(child.gameObject);
        }
    }

    public static T Ensure<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static Button AddButton(Image body, UiButtonStyle style)
    {
        var button = body.gameObject.AddComponent<Button>();
        button.targetGraphic = body;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = InteractionColors();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        return button;
    }

    private void AddOutline(RectTransform root, UiButtonStyle style)
    {
        switch (style)
        {
            case UiButtonStyle.Secondary:
                Outline(root, UiTheme.Line);
                break;
            case UiButtonStyle.Ghost:
                Outline(root, UiTheme.Alpha(UiTheme.White, 0.45f));
                break;
        }
    }

    private static Color FillColor(UiButtonStyle style)
    {
        switch (style)
        {
            case UiButtonStyle.Primary: return UiTheme.Main2;
            case UiButtonStyle.Secondary: return UiTheme.Main1;
            case UiButtonStyle.Danger: return UiTheme.Red;
            default: return UiTheme.Alpha(UiTheme.Main1Dark, 0.5f);
        }
    }

    private static Color LabelColor(UiButtonStyle style)
    {
        return style == UiButtonStyle.Ghost ? UiTheme.Alpha(UiTheme.White, 0.9f) : UiTheme.White;
    }

    private static ColorBlock InteractionColors()
    {
        return new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f),
            pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f),
            selectedColor = Color.white,
            disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
    }

    private static void StyleDropdownText(TMP_Text text, TMP_FontAsset font, float size, Color color)
    {
        if (text == null) return;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static TMP_DefaultControls.Resources BuiltinResources()
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
