using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class UiWiring
{
    public static void Set(Object target, params (string property, Object value)[] references)
    {
        var so = new SerializedObject(target);
        foreach ((string property, Object value) in references)
        {
            Property(so, property).objectReferenceValue = value;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetString(Object target, string property, string value)
    {
        var so = new SerializedObject(target);
        Property(so, property).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetInt(Object target, string property, int value)
    {
        var so = new SerializedObject(target);
        Property(so, property).intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetColors(Object target, string property, params Color[] colors)
    {
        var so = new SerializedObject(target);
        SerializedProperty array = Property(so, property);
        array.arraySize = colors.Length;
        for (int i = 0; i < colors.Length; i++)
        {
            array.GetArrayElementAtIndex(i).colorValue = colors[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetColor(Object target, string property, Color color)
    {
        var so = new SerializedObject(target);
        Property(so, property).colorValue = color;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static SerializedProperty Property(SerializedObject so, string property)
    {
        SerializedProperty found = so.FindProperty(property);
        if (found == null)
        {
            throw new ArgumentException($"{so.targetObject.GetType().Name} has no serialized property '{property}'");
        }

        return found;
    }

    public static void OnClick(Button button, UnityAction action)
    {
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }
}
