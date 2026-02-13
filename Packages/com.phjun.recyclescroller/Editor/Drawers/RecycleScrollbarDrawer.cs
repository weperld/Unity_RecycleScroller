using RecycleScroll;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(RecycleScrollbar))]
public class RecycleScrollbarEditor : SelectableEditor
{
    // Core Scrollbar fields
    private SerializedProperty m_HandleRect;
    private SerializedProperty m_Direction;
    private SerializedProperty m_Value;
    private SerializedProperty m_Size;
    private SerializedProperty m_NumberOfSteps;
    private SerializedProperty m_OnValueChanged;

    // Fixed Handle Size fields
    private SerializedProperty m_useFixedHandleSize;
    private SerializedProperty m_fixedHandleSizeMode;
    private SerializedProperty m_fixedHandleRatio;
    private SerializedProperty m_fixedHandlePixelSize;

    // Recycle Scrollbar fields
    private SerializedProperty m_leftHandle;
    private SerializedProperty m_rightHandle;
    private SerializedProperty m_graphics;
    private SerializedProperty m_onLoopValueChanged;
    private SerializedProperty m_onBeginDragged;
    private SerializedProperty m_onEndDragged;

    private SerializedProperty m_loopScrollSettingFoldout;
    private SerializedProperty m_eventFoldout;

    private GUIStyle m_boldFoldoutStyle;
    private GUIStyle m_boxStyle;

    private GUIStyle boldFoldoutStyle
    {
        get
        {
            if (m_boldFoldoutStyle == null)
            {
                m_boldFoldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold
                };
            }

            return m_boldFoldoutStyle;
        }
    }
    private GUIStyle boxStyle
    {
        get
        {
            if (m_boxStyle == null)
            {
                m_boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(20, 10, 10, 10)
                };
            }

            return m_boxStyle;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // Core Scrollbar fields
        m_HandleRect = serializedObject.FindProperty("m_HandleRect");
        m_Direction = serializedObject.FindProperty("m_Direction");
        m_Value = serializedObject.FindProperty("m_Value");
        m_Size = serializedObject.FindProperty("m_Size");
        m_NumberOfSteps = serializedObject.FindProperty("m_NumberOfSteps");
        m_OnValueChanged = serializedObject.FindProperty("m_OnValueChanged");

        // Fixed Handle Size fields
        m_useFixedHandleSize = serializedObject.FindProperty("m_useFixedHandleSize");
        m_fixedHandleSizeMode = serializedObject.FindProperty("m_fixedHandleSizeMode");
        m_fixedHandleRatio = serializedObject.FindProperty("m_fixedHandleRatio");
        m_fixedHandlePixelSize = serializedObject.FindProperty("m_fixedHandlePixelSize");

        // Recycle Scrollbar fields
        m_leftHandle = serializedObject.FindProperty("m_leftHandle");
        m_rightHandle = serializedObject.FindProperty("m_rightHandle");
        m_graphics = serializedObject.FindProperty("m_graphics");
        m_onLoopValueChanged = serializedObject.FindProperty("m_onLoopValueChanged");
        m_onBeginDragged = serializedObject.FindProperty("m_onBeginDragged");
        m_onEndDragged = serializedObject.FindProperty("m_onEndDragged");
        m_loopScrollSettingFoldout = serializedObject.FindProperty("m_loopScrollSettingFoldout");
        m_eventFoldout = serializedObject.FindProperty("m_eventFoldout");
    }

    public override void OnInspectorGUI()
    {
        // 헬프 박스
        Rect helpBoxRect = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
        string helpMessage = "Sliding Area/Handle 오프셋과 루프 상태 업데이트가 자동으로 관리됩니다.";
        MessageType messageType = MessageType.Info;
        Color textColor = Color.white;
        EditorDrawerHelper.DrawCustomHelpBox(helpBoxRect, helpMessage, messageType, textColor);

        // Selectable 기본 인스펙터 (Interactable, Transition, Colors 등)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("[Selectable Settings]", EditorStyles.boldLabel);
        base.OnInspectorGUI();

        serializedObject.Update();

        // Core Scrollbar 필드
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("[Scrollbar Settings]", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(m_HandleRect, new GUIContent("Handle Rect"));
        EditorGUILayout.PropertyField(m_Direction, new GUIContent("Direction"));
        EditorGUILayout.PropertyField(m_Value, new GUIContent("Value"));
        EditorGUILayout.PropertyField(m_Size, new GUIContent("Size"));
        EditorGUILayout.PropertyField(m_NumberOfSteps, new GUIContent("Number Of Steps"));

        // Fixed Handle Size
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("[Fixed Handle Size]", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_useFixedHandleSize, new GUIContent("Use Fixed Handle Size"));
        if (m_useFixedHandleSize.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorDrawerHelper.DrawCustomHelpBox(
                "핸들의 최소 사이즈를 보장합니다.\n"
                + "자연 크기(viewport/content 비율)가 고정 크기보다 작을 때만 적용됩니다.",
                MessageType.Info, Color.white);
            EditorGUILayout.Space(2f);

            EditorGUILayout.PropertyField(m_fixedHandleSizeMode, new GUIContent("Size Mode"));
            if (m_fixedHandleSizeMode.enumValueIndex == (int)RecycleScrollbar.FixedHandleSizeMode.Ratio)
                EditorGUILayout.PropertyField(m_fixedHandleRatio, new GUIContent("Min Ratio", "스크롤바 영역 대비 핸들 최소 비율 (0.01 ~ 1)"));
            else
                EditorGUILayout.PropertyField(m_fixedHandlePixelSize, new GUIContent("Min Pixel Size", "핸들 최소 픽셀 크기"));

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(m_OnValueChanged, new GUIContent("On Value Changed (float)"));

        // Recycle Scrollbar 필드
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(boxStyle);
        m_loopScrollSettingFoldout.boolValue = EditorGUILayout.Foldout(m_loopScrollSettingFoldout.boolValue, "[Recycle Scrollbar Settings]", true, boldFoldoutStyle);
        if (m_loopScrollSettingFoldout.boolValue)
        {
            EditorGUI.indentLevel++;

            EditorDrawerHelper.DrawCustomHelpBox(
                "Target Graphics: Selectable 상태 전환(Color Tint/Sprite Swap) 대상 그래픽\n"
                + "Sub Handle 0/1: 루프 스크롤 시 핸들 양쪽에 표시되는 보조 핸들 (자동 생성)",
                MessageType.Info, Color.white);
            EditorGUILayout.Space(2f);

            EditorGUILayout.PropertyField(m_graphics, new GUIContent("Target Graphics"));

            EditorGUILayout.PropertyField(m_leftHandle, new GUIContent("Sub Handle 0"));
            EditorGUILayout.PropertyField(m_rightHandle, new GUIContent("Sub Handle 1"));

            EditorGUILayout.Space();
            m_eventFoldout.boolValue = EditorGUILayout.Foldout(m_eventFoldout.boolValue, "[Recycle Scrollbar Events]", true, boldFoldoutStyle);
            if (m_eventFoldout.boolValue)
            {
                EditorGUILayout.PropertyField(m_onLoopValueChanged, new GUIContent("On Loop Value Changed"));
                EditorGUILayout.PropertyField(m_onBeginDragged, new GUIContent("On Begin Dragged"));
                EditorGUILayout.PropertyField(m_onEndDragged, new GUIContent("On End Dragged"));
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}
