using UnityEngine;

public static class UiTheme
{
    public static readonly Vector2 REFERENCE_RESOLUTION = new Vector2(1920f, 1080f);

    public static readonly Color Main1 = Hex("#0E3B66");
    public static readonly Color Main2 = Hex("#008CD2");
    public static readonly Color White = Hex("#E6E6E6");
    public static readonly Color Red = Hex("#E63946");
    public static readonly Color Cyan = Hex("#00E5FF");

    public static readonly Color Main1Dark = Color.Lerp(Main1, Color.black, 0.55f);
    public static readonly Color TextMuted = Alpha(White, 0.7f);
    public static readonly Color TextDim = Alpha(White, 0.45f);
    public static readonly Color Line = Alpha(Main2, 0.6f);

    public const float BUTTON_HEIGHT_LARGE = 84f;
    public const float BUTTON_HEIGHT = 72f;
    public const float BUTTON_HEIGHT_SMALL = 56f;
    public const float BUTTON_LABEL_SIZE = 26f;
    public const float BUTTON_LABEL_SIZE_SMALL = 22f;
    public const float BUTTON_ICON_SIZE = 28f;
    public const float LABEL_SPACING = 6f;
    public const float CAPTION_SPACING = 10f;
    public const float OUTLINE_PIXELS_PER_UNIT = 1f;

    public static Color Alpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Color Hex(string hex)
    {
        return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.magenta;
    }
}
