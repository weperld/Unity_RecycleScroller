using UnityEditor;
using UnityEngine;
using MathUtils;

[CustomPropertyDrawer(typeof(MinMaxFloat))]
public class MinMaxFloatDrawer : PropertyDrawer
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
        var minVal = minProp.floatValue;
        var maxVal = maxProp.floatValue;

        // 필드 렌더링
        // 전체 설정 입력 칸
        EditorGUI.BeginChangeCheck();
        EditorGUIUtility.labelWidth = 45f;
        var prevUnifiedValue = Mathf.Approximately(minVal, maxVal) ? minVal : float.NaN;
        var unifiedValue = EditorGUI.FloatField(setAllRect, "Set All", prevUnifiedValue);
        if (EditorGUI.EndChangeCheck())
        {
            if (float.IsNaN(prevUnifiedValue))
                minProp.floatValue = maxProp.floatValue;
            else
                minProp.floatValue = maxProp.floatValue = unifiedValue;
        }

        // Min, Max 개별 필드
        EditorGUI.BeginChangeCheck();
        var newMinVal = EditorGUI.FloatField(minRect, "Min", minVal);
        if (EditorGUI.EndChangeCheck()) minProp.floatValue = Mathf.Min(newMinVal, maxVal);

        EditorGUI.BeginChangeCheck();
        var newMaxVal = EditorGUI.FloatField(maxRect, "Max", maxVal);
        if (EditorGUI.EndChangeCheck()) maxProp.floatValue = Mathf.Max(newMaxVal, minVal);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
    }
}
