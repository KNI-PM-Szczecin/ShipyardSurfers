#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObsticleSetSO))]
public class ObsticleSetSOEditor : Editor
{
    private const float LABEL_WIDTH = 58f;
    private const float CELL_GAP = 3f;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var set = (ObsticleSetSO)target;
        SerializedProperty gridProp = serializedObject.FindProperty("Grid");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Obsticle set editor ({ObsticleSetSO.COLUMNS} columns x {ObsticleSetSO.ROWS} rows)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Higher objects = further on screen", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        float lineH = EditorGUIUtility.singleLineHeight;
        float cellH = lineH * 3f + 10f;

        for (int y = ObsticleSetSO.ROWS - 1; y >= 0; y--)
        {
            Rect row = EditorGUILayout.GetControlRect(false, cellH);

            EditorGUI.LabelField(new Rect(row.x, row.y + (cellH - lineH) * 0.5f, LABEL_WIDTH, lineH), $"Row {y:00}");

            float colW = (row.width - LABEL_WIDTH) / ObsticleSetSO.COLUMNS;

            for (int x = 0; x < ObsticleSetSO.COLUMNS; x++)
            {
                Rect cell = new Rect(row.x + LABEL_WIDTH + x * colW, row.y, colW - CELL_GAP, cellH);
                DrawCell(cell, gridProp.GetArrayElementAtIndex(y * ObsticleSetSO.COLUMNS + x), ObsticleSetAnalyzer.IsCoveredByBlockade(set, x, y), lineH);
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Clear whole track", GUILayout.Height(30)))
        {
            ClearGrid(gridProp);
        }

        serializedObject.ApplyModifiedProperties();

        DrawIssues(ObsticleSetValidator.Validate(set));
    }

    private void DrawCell(Rect cell, SerializedProperty cellProp, bool covered, float lineH)
    {
        var typeProp = cellProp.FindPropertyRelative("Type");
        var lengthProp = cellProp.FindPropertyRelative("BlockadeLength");
        var coinProp = cellProp.FindPropertyRelative("HasCoin");
        var currentType = (ObsticleType)typeProp.enumValueIndex;

        EditorGUI.DrawRect(cell, GetObstacleColor(covered ? ObsticleType.Blockade : currentType));
        EditorGUI.DrawRect(new Rect(cell.x, cell.y, cell.width, 1), new Color(0, 0, 0, 0.25f));

        Rect coinRect = new Rect(cell.x + 3, cell.y + 7 + lineH * 2, cell.width - 6, lineH);

        if (covered)
        {
            if (typeProp.enumValueIndex != (int)ObsticleType.Empty) typeProp.enumValueIndex = (int)ObsticleType.Empty;
            if (lengthProp.intValue != 1) lengthProp.intValue = 1;

            coinProp.boolValue = EditorGUI.ToggleLeft(coinRect, "Coin", coinProp.boolValue);
            return;
        }

        EditorGUI.PropertyField(new Rect(cell.x + 3, cell.y + 3, cell.width - 6, lineH), typeProp, GUIContent.none);

        if (currentType == ObsticleType.Blockade)
        {
            Rect lengthRect = new Rect(cell.x + 3, cell.y + 5 + lineH, cell.width - 6, lineH);
            lengthProp.intValue = EditorGUI.IntPopup(lengthRect, lengthProp.intValue, BlockadeLengthLabels, BlockadeLengthValues);
        }
        else if (lengthProp.intValue != 1)
        {
            lengthProp.intValue = 1;
        }

        coinProp.boolValue = EditorGUI.ToggleLeft(coinRect, "Coin", coinProp.boolValue);
    }

    private static void ClearGrid(SerializedProperty gridProp)
    {
        for (int i = 0; i < gridProp.arraySize; i++)
        {
            SerializedProperty cellProp = gridProp.GetArrayElementAtIndex(i);
            cellProp.FindPropertyRelative("Type").enumValueIndex = (int)ObsticleType.Empty;
            cellProp.FindPropertyRelative("BlockadeLength").intValue = 1;
            cellProp.FindPropertyRelative("HasCoin").boolValue = false;
        }
    }

    private static void DrawIssues(IReadOnlyList<string> issues)
    {
        if (issues.Count == 0)
        {
            EditorGUILayout.HelpBox("Set is valid.", MessageType.Info);
            return;
        }

        foreach (string issue in issues)
        {
            EditorGUILayout.HelpBox(issue, MessageType.Warning);
        }
    }

    private static readonly int[] BlockadeLengthValues = CreateLengthValues();
    private static readonly string[] BlockadeLengthLabels = CreateLengthLabels();

    private static int[] CreateLengthValues()
    {
        var values = new int[ObsticleSetSO.MAX_BLOCKADE_LENGTH];
        for (int i = 0; i < values.Length; i++) values[i] = i + 1;
        return values;
    }

    private static string[] CreateLengthLabels()
    {
        var labels = new string[ObsticleSetSO.MAX_BLOCKADE_LENGTH];
        for (int i = 0; i < labels.Length; i++) labels[i] = $"Len: {i + 1}";
        return labels;
    }

    private static Color GetObstacleColor(ObsticleType type)
    {
        switch (type)
        {
            case ObsticleType.Empty: return Color.white;
            case ObsticleType.Jump: return new Color(0.6f, 0.8f, 1.0f);
            case ObsticleType.Slide: return new Color(1.0f, 0.8f, 0.5f);
            case ObsticleType.Blockade: return new Color(1.0f, 0.5f, 0.5f);
            case ObsticleType.Ramp: return new Color(0.6f, 1.0f, 0.6f);
            default: return Color.white;
        }
    }
}
#endif
