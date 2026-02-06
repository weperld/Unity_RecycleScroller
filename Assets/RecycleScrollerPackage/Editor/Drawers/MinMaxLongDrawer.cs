using UnityEditor;
using UnityEngine;
using MathUtils;
using System;

[CustomPropertyDrawer(typeof(MinMaxLong))]
public class MinMaxLongDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var labelRect = new Rect(position.x, position.y, position.width - 150f, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(labelRect, label);

        // 필드 렉트 계산
        EditorGUIUtility.labelWidth = 30f;
        var setAllRect = labelRect;
        setAllRect.x = position.width - 120f;
        setAllRect.width = 120f;

        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        var fieldEnableSpace = (position.width - 15f) / 2f;
        var fieldRect = new Rect(position.x + 15f, position.y, fieldEnableSpace - 20f, EditorGUIUtility.singleLineHeight);
        var minRect = fieldRect; fieldRect.x += fieldEnableSpace;
        var maxRect = fieldRect;

        SerializedProperty minProp = property.FindPropertyRelative("min");
        SerializedProperty maxProp = property.FindPropertyRelative("max");
        var minVal = minProp.longValue;
        var maxVal = maxProp.longValue;

        // 필드 렌더링
        // 전체 설정 입력 칸
        EditorGUI.BeginChangeCheck();
        EditorGUIUtility.labelWidth = 45f;
        var prevUnifiedValue = minVal == maxVal ? minVal : long.MinValue;
        var unifiedValue = EditorGUI.LongField(setAllRect, "Set All", prevUnifiedValue);
        if (EditorGUI.EndChangeCheck())
        {
            if (prevUnifiedValue == long.MinValue)
                minProp.longValue = maxProp.longValue;
            else
                minProp.longValue = maxProp.longValue = unifiedValue;
        }

        // Min, Max 개별 필드
        EditorGUI.BeginChangeCheck();
        var newMinVal = EditorGUI.LongField(minRect, "Min", minVal);
        if (EditorGUI.EndChangeCheck()) minProp.longValue = Math.Min(newMinVal, maxVal);

        EditorGUI.BeginChangeCheck();
        var newMaxVal = EditorGUI.LongField(maxRect, "Max", maxVal);
        if (EditorGUI.EndChangeCheck()) maxProp.longValue = Math.Max(newMaxVal, minVal);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
    }
}
