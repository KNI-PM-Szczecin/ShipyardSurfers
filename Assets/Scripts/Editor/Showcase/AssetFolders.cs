using System.IO;
using UnityEditor;

public static class AssetFolders
{
    public static void Ensure(string folder)
    {
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;

        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(parent)) return;

        Ensure(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
