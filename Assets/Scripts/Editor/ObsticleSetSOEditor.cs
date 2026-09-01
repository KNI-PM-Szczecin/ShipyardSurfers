#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObsticleSetSO))]
public class ObsticleSetSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty gridProp = serializedObject.FindProperty("Grid");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Obsticle set editor (3 columns x 15 rows)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Higher objects = further on screen", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        const float labelW = 58f;
        const float gap = 3f;
        float lineH = EditorGUIUtility.singleLineHeight;
        float cellH = lineH * 3f + 10f;

        bool[] isCovered = new bool[ObsticleSetSO.Rows * ObsticleSetSO.Columns];
        for (int x = 0; x < ObsticleSetSO.Columns; x++)
            for (int y = 0; y < ObsticleSetSO.Rows; y++)
            {
                int i = y * ObsticleSetSO.Columns + x;
                var cp = gridProp.GetArrayElementAtIndex(i);
                if ((ObsticleType)cp.FindPropertyRelative("Type").enumValueIndex == ObsticleType.Blockade)
                {
                    int len = cp.FindPropertyRelative("BlockadeLength").intValue;
                    for (int l = 1; l < len; l++)
                        if (y + l < ObsticleSetSO.Rows)
                            isCovered[(y + l) * ObsticleSetSO.Columns + x] = true;
                }
            }

        for (int y = ObsticleSetSO.Rows - 1; y >= 0; y--)
        {
            Rect row = EditorGUILayout.GetControlRect(false, cellH);

            EditorGUI.LabelField(new Rect(row.x, row.y + (cellH - lineH) * 0.5f, labelW, lineH), $"Row {y:00}");

            float colW = (row.width - labelW) / ObsticleSetSO.Columns;

            for (int x = 0; x < ObsticleSetSO.Columns; x++)
            {
                int idx = y * ObsticleSetSO.Columns + x;
                var cellProp = gridProp.GetArrayElementAtIndex(idx);
                var typeProp = cellProp.FindPropertyRelative("Type");
                var lengthProp = cellProp.FindPropertyRelative("BlockadeLength");
                var coinProp = cellProp.FindPropertyRelative("HasCoin");

                bool covered = isCovered[idx];
                var currentType = (ObsticleType)typeProp.enumValueIndex;

                Rect cell = new Rect(row.x + labelW + x * colW, row.y, colW - gap, cellH);

                Color bg = GetObstacleColor(covered ? ObsticleType.Blockade : currentType);
                EditorGUI.DrawRect(cell, bg);
                EditorGUI.DrawRect(new Rect(cell.x, cell.y, cell.width, 1), new Color(0, 0, 0, 0.25f));

                if (covered)
                {
                    if (typeProp.enumValueIndex != (int)ObsticleType.Empty) typeProp.enumValueIndex = (int)ObsticleType.Empty;
                    if (lengthProp.intValue != 1) lengthProp.intValue = 1;

                    Rect rc = new Rect(cell.x + 3, cell.y + 7 + lineH * 2, cell.width - 6, lineH);
                    coinProp.boolValue = EditorGUI.ToggleLeft(rc, "Coin", coinProp.boolValue);
                    continue;
                }

                Rect r1 = new Rect(cell.x + 3, cell.y + 3, cell.width - 6, lineH);
                EditorGUI.PropertyField(r1, typeProp, GUIContent.none);

                if (currentType == ObsticleType.Blockade)
                {
                    Rect r2 = new Rect(cell.x + 3, cell.y + 5 + lineH, cell.width - 6, lineH);
                    int[] vals = { 1, 2, 3 };
                    string[] opts = { "Len: 1", "Len: 2", "Len: 3" };
                    lengthProp.intValue = EditorGUI.IntPopup(r2, lengthProp.intValue, opts, vals);
                }
                else if (lengthProp.intValue != 1)
                {
                    lengthProp.intValue = 1;
                }

                Rect r3 = new Rect(cell.x + 3, cell.y + 7 + lineH * 2, cell.width - 6, lineH);
                coinProp.boolValue = EditorGUI.ToggleLeft(r3, "Coin", coinProp.boolValue);
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Clear whole track", GUILayout.Height(30)))
        {
            for (int i = 0; i < gridProp.arraySize; i++)
            {
                gridProp.GetArrayElementAtIndex(i).FindPropertyRelative("Type").enumValueIndex = 0;
                gridProp.GetArrayElementAtIndex(i).FindPropertyRelative("BlockadeLength").intValue = 1;
                gridProp.GetArrayElementAtIndex(i).FindPropertyRelative("HasCoin").boolValue = false;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
    private Color GetObstacleColor(ObsticleType type)
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