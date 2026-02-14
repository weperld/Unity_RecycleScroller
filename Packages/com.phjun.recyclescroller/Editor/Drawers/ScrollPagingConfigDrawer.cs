using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RecycleScroll.ScrollPagingConfig))]
public class ScrollPagingConfigDrawer : PropertyDrawer
{
    private Color onColorBack => ColorHexTemplate.CT_80FF80;
    private Color offColorBack => ColorHexTemplate.CT_FF801A;
    private Color warningColor => ColorHexTemplate.CT_FF3333;
    
    private readonly float m_toggleSize = 80f;
    private readonly float m_toggleLabelPosX = 90f;
    private readonly float m_warningLabelHeight = EditorDrawerHelper_ConstValues.DEFAULT_HELPBOX_HEIGHT;
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var useProp = property.FindPropertyRelative("usePaging");
        var useVal = useProp.boolValue;
        
        float height = EditorGUIUtility.singleLineHeight;
        if (useVal == false) return height;
        
        // Warning Label On UsePaging
        height += m_warningLabelHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // Check Foldout
        if (property.isExpanded == false) return height;
        
        // add empty space to last page
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // count, duration
        height += EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing * 2f;
        
        // scrollview pivotType, pivot(only if custom)
        var svPivotTypeProp = property.FindPropertyRelative("scrollViewPivotType");
        PivotTypeHeight(svPivotTypeProp);
        
        // cell pivotType, pivot(only if custom)
        var cellPivotTypeProp = property.FindPropertyRelative("pagePivotType");
        PivotTypeHeight(cellPivotTypeProp);
        
        // useCustomEase, easeFunction or customEase
        height += EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing * 2f;
        
        return height;
        
        void PivotTypeHeight(SerializedProperty pivotTypeProp)
        {
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if ((RecycleScroll.eMagnetPivotType)pivotTypeProp.enumValueIndex == RecycleScroll.eMagnetPivotType.Custom)
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        
        var useProp = property.FindPropertyRelative("usePaging");
        var useVal = useProp.boolValue;
        
        #region Draw UsePaging toggle
        // Toggle
        if (Application.isPlaying) EditorGUI.BeginDisabledGroup(true);
        var useGUIContent = new GUIContent(useVal ? "ON" : "OFF");
        var style = new GUIStyle(GUI.skin.button);
        style.fontStyle = FontStyle.Bold;
        rect.width = m_toggleSize;
        GUI.backgroundColor = useVal ? onColorBack : offColorBack;
        if (GUI.Button(rect, useGUIContent, style))
            useVal = useProp.boolValue = !useVal;
        GUI.backgroundColor = Color.white;
        
        // Label
        rect.x += m_toggleLabelPosX;
        rect.width = position.width - m_toggleLabelPosX;
        if (useVal is false)
        {
            var labelRect = rect;
            labelRect.x += 13f;
            EditorGUI.LabelField(labelRect, label);
        }
        else property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
        if (Application.isPlaying) EditorGUI.EndDisabledGroup();
        
        rect.x = position.x;
        rect.width = position.width;
        #endregion
        
        if (useVal == false) return;
        
        #region Draw Warning Label
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        rect.height = m_warningLabelHeight;
        EditorDrawerHelper.DrawCustomHelpBox(rect, "주의! 실행 시 ScrollRect의 Inertia가 꺼집니다. 이전 상태를 기억하지 않습니다!",
            MessageType.Warning, warningColor);
        rect.height = EditorGUIUtility.singleLineHeight;
        rect.y += m_warningLabelHeight - EditorGUIUtility.singleLineHeight;
        #endregion
        
        if (property.isExpanded == false) return;
        
        rect.x += 20f;
        rect.width -= 20f;
        EditorGUIUtility.labelWidth -= 20f;
        
        #region Draw Add Empty Space To Last Page
        var addEmptySpaceProp = property.FindPropertyRelative("addEmptySpaceToLastPage");
        var addEmptySpaceVal = addEmptySpaceProp.boolValue;
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        if (Application.isPlaying) EditorGUI.BeginDisabledGroup(true);
        addEmptySpaceVal = EditorGUI.Toggle(rect, "Add Empty Space To Last Page", addEmptySpaceVal);
        addEmptySpaceProp.boolValue = addEmptySpaceVal;
        if (Application.isPlaying) EditorGUI.EndDisabledGroup();
        #endregion
        
        #region Draw Item Count Per Page
        var countProp = property.FindPropertyRelative("countPerPage");
        var countVal = countProp.intValue;
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        if (Application.isPlaying) EditorGUI.BeginDisabledGroup(true);
        var newCountVal = EditorGUI.IntField(rect, "Item Count Per Page", countVal);
        countProp.intValue = Mathf.Max(1, newCountVal);
        if (Application.isPlaying) EditorGUI.EndDisabledGroup();
        #endregion
        
        #region Draw Ease Duration
        var durationProp = property.FindPropertyRelative("duration");
        var durationVal = durationProp.floatValue;
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        var newDurationVal = EditorGUI.FloatField(rect, "Ease Duration", durationVal);
        durationProp.floatValue = Mathf.Max(0f, newDurationVal);
        #endregion
        
        #region Draw Magnet Pivot Position
        void DrawPivotType(SerializedProperty pivotTypeProp, SerializedProperty customPivotProp, string label,
            string customPivotName)
        {
            var pivotTypeVal = (RecycleScroll.eMagnetPivotType)pivotTypeProp.enumValueIndex;
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(rect, pivotTypeProp, new GUIContent(label));
            if (pivotTypeVal == RecycleScroll.eMagnetPivotType.Custom)
            {
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.Slider(rect, customPivotProp, 0f, 1f, customPivotName);
            }
        }
        
        var pivotTypeProp = property.FindPropertyRelative("scrollViewPivotType");
        var customPivotProp = property.FindPropertyRelative("scrollViewCustomPivot");
        DrawPivotType(pivotTypeProp, customPivotProp, "ScrollView Magnet Pivot", "ScrollView Custom Pivot Value");
        var cellPivotTypeProp = property.FindPropertyRelative("pagePivotType");
        var cellCustomPivotProp = property.FindPropertyRelative("pageCustomPivot");
        DrawPivotType(cellPivotTypeProp, cellCustomPivotProp, "Page Magnet Pivot", "Page Custom Pivot Value");
        #endregion
        
        #region Draw Ease Config
        var useCustomEaseProp = property.FindPropertyRelative("useCustomEase");
        var useCustomEaseVal = useCustomEaseProp.boolValue;
        // Toggle
        useGUIContent = new GUIContent("Select Ease Curve: " + (useCustomEaseVal ? "Custom" : "Const"));
        style = new GUIStyle(GUI.skin.button);
        style.fontStyle = FontStyle.Bold;
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        GUI.backgroundColor = useCustomEaseVal ? offColorBack : onColorBack;
        if (GUI.Button(rect, useGUIContent, style))
            useCustomEaseVal = useCustomEaseProp.boolValue = !useCustomEaseVal;
        GUI.backgroundColor = Color.white;
        
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        if (useCustomEaseVal == false)
        {
            var easeFunctionProp = property.FindPropertyRelative("easeFunction");
            EditorGUI.PropertyField(rect, easeFunctionProp, new GUIContent("Ease Function"));
        }
        else
        {
            var customEaseProp = property.FindPropertyRelative("customEase");
            EditorGUI.PropertyField(rect, customEaseProp, new GUIContent("Custom Ease Curve"));
        }
        #endregion
        
        EditorGUIUtility.labelWidth += 20f;
    }
}