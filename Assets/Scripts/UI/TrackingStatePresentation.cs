using UnityEngine;

public static class TrackingStatePresentation
{
    private static readonly Color Disabled = new Color(0.45f, 0.53f, 0.64f);
    private static readonly Color Error = new Color(1f, 0.36f, 0.42f);
    private static readonly Color Searching = new Color(0.97f, 0.72f, 0.2f);
    private static readonly Color Tracking = new Color(0.24f, 0.86f, 0.52f);

    public static string Text(TrackingState state)
    {
        switch (state)
        {
            case TrackingState.Disabled: return "Sterowanie kamerą wyłączone";
            case TrackingState.NoCamera: return "Brak obrazu z kamery";
            case TrackingState.Searching: return "Szukam gracza...";
            case TrackingState.Tracking: return "Śledzenie aktywne";
            case TrackingState.Lost: return "Zgubiono gracza";
            default: return string.Empty;
        }
    }

    public static Color Color(TrackingState state)
    {
        switch (state)
        {
            case TrackingState.Tracking: return Tracking;
            case TrackingState.Searching: return Searching;
            case TrackingState.NoCamera:
            case TrackingState.Lost: return Error;
            default: return Disabled;
        }
    }

    public static Color UnavailableColor => Disabled;
}
