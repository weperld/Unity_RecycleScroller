using UnityEditor;
using UnityEngine;
using MathUtils;
using System;

[CustomPropertyDrawer(typeof(MinMaxDouble))]
public class MinMaxDoubleDrawer : PropertyDrawer
{
    // 첫 줄에 MinMaxFloat의 레이블, 다음 줄에 Min과 Max 필드를 한 줄로, 필드를 총 두 줄로 표기하기 위한 프로퍼티 드로어
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
        var minVal = minProp.doubleValue;
        var maxVal = maxProp.doubleValue;

        // 필드 렌더링
        // 전체 설정 입력 칸
        EditorGUI.BeginChangeCheck();
        EditorGUIUtility.labelWidth = 45f;
        var prevUnifiedValue = MathUtil.Approximately(minVal, maxVal) ? minVal : double.NaN;
        var unifiedValue = EditorGUI.DoubleField(setAllRect, "Set All", prevUnifiedValue);
        if (EditorGUI.EndChangeCheck())
        {
            if (double.IsNaN(prevUnifiedValue))
                minProp.doubleValue = maxProp.doubleValue;
            else
                minProp.doubleValue = maxProp.doubleValue = unifiedValue;
        }

        // Min, Max 개별 필드
        EditorGUI.BeginChangeCheck();
        var newMinVal = EditorGUI.DoubleField(minRect, "Min", minVal);
        if (EditorGUI.EndChangeCheck()) minProp.doubleValue = Math.Min(newMinVal, maxVal);

        EditorGUI.BeginChangeCheck();
        var newMaxVal = EditorGUI.DoubleField(maxRect, "Max", maxVal);
        if (EditorGUI.EndChangeCheck()) maxProp.doubleValue = Math.Max(newMaxVal, minVal);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
    }
}
