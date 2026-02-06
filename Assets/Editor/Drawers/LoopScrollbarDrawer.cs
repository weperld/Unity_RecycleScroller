using RecycleScroll;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(LoopScrollbar))]
public class LoopScrollbarEditor : ScrollbarEditor
{
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
        // LoopScrollbar의 추가 필드가 있다면 여기서 프로퍼티를 가져옴
        m_leftHandle = serializedObject.FindProperty("m_leftHandle");
        m_rightHandle = serializedObject.FindProperty("m_rightHandle");
        m_graphics = serializedObject.FindProperty("m_graphics");
        m_onLoopValueChanged = serializedObject.FindProperty("m_onLoopValueChanged");
        m_onBeginDragged = serializedObject.FindProperty("m_onBeginDragged");
        m_onEndDragged = serializedObject.FindProperty("m_onEndDragged");
        m_loopScrollSettingFoldout = serializedObject.FindProperty("m_loopScrollSettingFoldout");
        m_eventFoldout = serializedObject.FindProperty("m_eventFoldout");

        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        // 커스텀 헬프 박스를 그릴 위치와 메시지 설정
        Rect helpBoxRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        string helpMessage = "온전한 작동을 위해 Sliding Area와 Handle의 Left,Right,Top,Bottom을 0으로 설정하고, LoopScrollbar의 OnValueChangeSelf가 등록되어 있어야 합니다.";
        MessageType messageType = MessageType.Info;
        Color textColor = Color.white;
        EditorDrawerHelper.DrawCustomHelpBox(helpBoxRect, helpMessage, messageType, textColor);

        // 기본 Scrollbar의 인스펙터 UI를 그리기EditorGUILayout.Space();
        EditorGUILayout.LabelField("[Base Scrollbar Settings]", EditorStyles.boldLabel);
        base.OnInspectorGUI();
        serializedObject.Update();

        // LoopScrollbar의 추가 필드를 인스펙터 UI에 추가
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(boxStyle);
        m_loopScrollSettingFoldout.boolValue = EditorGUILayout.Foldout(m_loopScrollSettingFoldout.boolValue, "[Loop Scrollbar Settings]", true, boldFoldoutStyle);
        if (m_loopScrollSettingFoldout.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_graphics, new GUIContent("Target Graphics"));

            EditorGUILayout.PropertyField(m_leftHandle, new GUIContent("Sub Handle 0"));
            EditorGUILayout.PropertyField(m_rightHandle, new GUIContent("Sub Handle 1"));

            EditorGUILayout.Space();
            m_eventFoldout.boolValue = EditorGUILayout.Foldout(m_eventFoldout.boolValue, "[Loop Scrollbar Events]", true, boldFoldoutStyle);
            if (m_eventFoldout.boolValue)
            {
                EditorGUILayout.PropertyField(m_onLoopValueChanged, new GUIContent("On Loop Value Changed"));
                EditorGUILayout.PropertyField(m_onBeginDragged, new GUIContent("On Begin Dragged"));
                EditorGUILayout.PropertyField(m_onEndDragged, new GUIContent("On End Dragged"));
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();

        // 기타 커스텀 설정이 필요하면 추가
        serializedObject.ApplyModifiedProperties();
    }
}