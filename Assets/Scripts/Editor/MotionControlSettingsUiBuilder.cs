using UnityEditor;

public static class MotionControlSettingsUiBuilder
{
    private const string MENU_PATH = "Shipyard Surfers/Motion Control/Build Settings UI";

    [MenuItem(MENU_PATH)]
    public static void Build()
    {
        UiFaceliftMenu.RebuildMainMenu();
    }
}
