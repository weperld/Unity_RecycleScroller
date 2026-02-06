using RecycleScroll;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RecycleScroller))]
[CanEditMultipleObjects]
public class RecycleScrollerEditor : Editor
{
    public enum FieldShowState
    {
        Show,
        Disable,
        Hide
    }

    private static bool IsAppPlaying => Application.isPlaying;

    // SerializedProperty들
    private SerializedProperty m_Script;
    private SerializedProperty m_Padding;
    private SerializedProperty m_Spacing;
    private SerializedProperty m_ChildAlignment;
    private SerializedProperty m_Reverse;
    private SerializedProperty m_ControlChildSize;
    private SerializedProperty m_UseChildScale;
    private SerializedProperty m_ChildForceExpand;
    private SerializedProperty m_ScrollAxis;
    private SerializedProperty m_FitContentToViewport;
    private SerializedProperty m_fixedCellCountInGroup;
    private SerializedProperty m_fixedCellCount;
    private SerializedProperty m_useMinMaxFlexibleCellCount;
    private SerializedProperty m_flexibleCellCountLimit;
    private SerializedProperty m_SpacingInGroup;
    private SerializedProperty m_PagingData;
    private SerializedProperty m_loopScroll;
    private SerializedProperty m_exampleLayoutGroups;

    void OnEnable()
    {
        m_Script = serializedObject.FindProperty("m_Script");
        m_Padding = serializedObject.FindProperty("m_Padding");
        m_Spacing = serializedObject.FindProperty("m_Spacing");
        m_ChildAlignment = serializedObject.FindProperty("m_ChildAlignment");
        m_Reverse = serializedObject.FindProperty("m_Reverse");
        m_ControlChildSize = serializedObject.FindProperty("m_ControlChildSize");
        m_UseChildScale = serializedObject.FindProperty("m_UseChildScale");
        m_ChildForceExpand = serializedObject.FindProperty("m_ChildForceExpand");
        m_ScrollAxis = serializedObject.FindProperty("m_ScrollAxis");
        m_FitContentToViewport = serializedObject.FindProperty("m_FitContentToViewport");
        m_fixedCellCountInGroup = serializedObject.FindProperty("m_fixedCellCountInGroup");
        m_fixedCellCount = serializedObject.FindProperty("m_fixedCellCount");
        m_useMinMaxFlexibleCellCount = serializedObject.FindProperty("m_useMinMaxFlexibleCellCount");
        m_flexibleCellCountLimit = serializedObject.FindProperty("m_flexibleCellCountLimit");
        m_SpacingInGroup = serializedObject.FindProperty("m_SpacingInGroup");
        m_PagingData = serializedObject.FindProperty("m_PagingData");
        m_loopScroll = serializedObject.FindProperty("m_loopScroll");
        m_exampleLayoutGroups = serializedObject.FindProperty("m_exampleLayoutGroups");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var scroller = (RecycleScroller)target;

        // Scroll Axis
        EditorGUI.BeginDisabledGroup(IsAppPlaying);
        EditorGUILayout.PropertyField(m_ScrollAxis);
        EditorGUI.EndDisabledGroup();

        // Alignment Values
        EditorGUI.BeginDisabledGroup(IsAppPlaying);
        EditorGUILayout.PropertyField(m_Padding, new GUIContent("Layout Padding"));
        EditorGUILayout.PropertyField(m_Spacing);
        EditorGUILayout.PropertyField(m_ChildAlignment);
        EditorGUILayout.PropertyField(m_Reverse);
        EditorGUILayout.PropertyField(m_ControlChildSize);
        EditorGUILayout.PropertyField(m_UseChildScale);
        EditorGUILayout.PropertyField(m_ChildForceExpand);
        EditorGUI.EndDisabledGroup();

        // Fit Content To Viewport
        EditorGUI.BeginDisabledGroup(IsAppPlaying);
        EditorGUILayout.PropertyField(m_FitContentToViewport);
        EditorGUI.EndDisabledGroup();

        // Cell Group Config
        EditorGUI.BeginDisabledGroup(IsAppPlaying);
        EditorGUILayout.PropertyField(m_fixedCellCountInGroup);
        EditorGUI.EndDisabledGroup();

        // Fixed Cell Count (ConditionalDisableOrHide)
        var fixedCountState = scroller.FixCellCountInGroup
            ? IsAppPlaying
                ? FieldShowState.Disable
                : FieldShowState.Show
            : FieldShowState.Hide;
        if (fixedCountState is not FieldShowState.Hide)
        {
            EditorGUI.BeginDisabledGroup(fixedCountState is FieldShowState.Disable);
            EditorGUILayout.PropertyField(m_fixedCellCount);
            EditorGUI.EndDisabledGroup();
        }

        // Use MinMax Flexible Cell Count (ConditionalDisableOrHide)
        var flexibleCountState = !scroller.FixCellCountInGroup
            ? IsAppPlaying
                ? FieldShowState.Disable
                : FieldShowState.Show
            : FieldShowState.Hide;
        if (flexibleCountState is not FieldShowState.Hide)
        {
            EditorGUI.BeginDisabledGroup(flexibleCountState is FieldShowState.Disable);
            EditorGUILayout.PropertyField(m_useMinMaxFlexibleCellCount);
            EditorGUI.EndDisabledGroup();
        }

        // Flexible Cell Count Limit (ConditionalDisableOrHide)
        var flexibleLimitState = scroller.ShowMinMaxFlexibleCellCount
            ? IsAppPlaying
                ? FieldShowState.Disable
                : FieldShowState.Show
            : FieldShowState.Hide;
        if (flexibleLimitState is not FieldShowState.Hide)
        {
            EditorGUI.BeginDisabledGroup(flexibleLimitState is FieldShowState.Disable);
            EditorGUILayout.PropertyField(m_flexibleCellCountLimit);
            EditorGUI.EndDisabledGroup();
        }

        // Spacing In Group (ConditionalDisableOrHide)
        var spacingState = scroller.ShowSpacingInGroup
            ? IsAppPlaying
                ? FieldShowState.Disable
                : FieldShowState.Show
            : FieldShowState.Hide;
        if (spacingState is not FieldShowState.Hide)
        {
            EditorGUI.BeginDisabledGroup(spacingState is FieldShowState.Disable);
            EditorGUILayout.PropertyField(m_SpacingInGroup);
            EditorGUI.EndDisabledGroup();
        }

        // Page Configs (CustomLabel)
        EditorGUILayout.PropertyField(m_PagingData, new GUIContent("Use Page Configs"));

        // Loop Scroll
        EditorGUI.BeginDisabledGroup(IsAppPlaying);
        EditorGUILayout.PropertyField(m_loopScroll);
        EditorGUI.EndDisabledGroup();

        // Example Layout Groups (OnValidate)
        EditorGUILayout.PropertyField(m_exampleLayoutGroups);

        serializedObject.ApplyModifiedProperties();
    }
}
