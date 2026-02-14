using RecycleScroll;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(RecycleScrollbar))]
public class RecycleScrollbarEditor : SelectableEditor
{
    #region SerializedProperties

    // Scrollbar - Handle
    private SerializedProperty m_handleRect;
    private SerializedProperty m_direction;

    // Scrollbar - Value
    private SerializedProperty m_value;
    private SerializedProperty m_size;
    private SerializedProperty m_numberOfSteps;

    // Scrollbar - Fixed Handle Size
    private SerializedProperty m_useFixedHandleSize;
    private SerializedProperty m_fixedHandleSizeMode;
    private SerializedProperty m_fixedHandleRatio;
    private SerializedProperty m_fixedHandlePixelSize;

    // Loop Scrollbar - References
    private SerializedProperty m_leftHandle;
    private SerializedProperty m_rightHandle;
    private SerializedProperty m_graphics;

    // Events
    private SerializedProperty m_onValueChanged;
    private SerializedProperty m_onLoopValueChanged;
    private SerializedProperty m_onBeginDragged;
    private SerializedProperty m_onEndDragged;

    #endregion

    #region Foldout States

    private const bool DEFAULT_FOLD_OUT_STATE = false;

    // Big categories
    private static bool m_foldoutSelectable = DEFAULT_FOLD_OUT_STATE;
    private static bool m_foldoutScrollbar = DEFAULT_FOLD_OUT_STATE;
    private static bool m_foldoutLoopScrollbar = DEFAULT_FOLD_OUT_STATE;
    private static bool m_foldoutEvents = DEFAULT_FOLD_OUT_STATE;

    // Scrollbar
    private static bool m_foldoutHandle = DEFAULT_FOLD_OUT_STATE;
    private static bool m_foldoutValue = DEFAULT_FOLD_OUT_STATE;
    private static bool m_foldoutFixedHandleSize = DEFAULT_FOLD_OUT_STATE;

    // Loop Scrollbar
    private static bool m_foldoutReferences = DEFAULT_FOLD_OUT_STATE;

    // Events
    private static bool m_foldoutScrollbarEvents = DEFAULT_FOLD_OUT_STATE;
    private static bool m_foldoutLoopEvents = DEFAULT_FOLD_OUT_STATE;

    #endregion

    #region Foldout Styles & Colors

    // Big category title colors (each unique pastel)
    private const string BIG_TITLE_COLOR_SELECTABLE = "#FF99CC";
    private const string BIG_TITLE_COLOR_SCROLLBAR = "#FFFF99";
    private const string BIG_TITLE_COLOR_LOOP = "#99FFBB";
    private const string BIG_TITLE_COLOR_EVENTS = "#CC99FF";

    // Big category box colors (alternating neutral grey tints over helpBox)
    private static readonly Color m_bigBoxColorA = new(0.06f, 0.06f, 0.06f, 0.5f);
    private static readonly Color m_bigBoxColorB = new(0.14f, 0.14f, 0.14f, 0.5f);

    // Small category title colors (alternating green pastel)
    private const string SMALL_TITLE_COLOR_A = "#A8E6CF";
    private const string SMALL_TITLE_COLOR_B = "#C8E6A0";

    // Small category box colors (alternating green-grey)
    private static readonly Color m_smallBoxColorA = new(0.20f, 0.28f, 0.22f, 0.45f);
    private static readonly Color m_smallBoxColorB = new(0.26f, 0.30f, 0.20f, 0.45f);

    private static GUIStyle m_titleStyle;
    private static GUIStyle TitleStyle
    {
        get
        {
            if (m_titleStyle == null)
            {
                m_titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 15
                };
            }
            return m_titleStyle;
        }
    }

    private static GUIStyle m_bigFoldoutStyle;
    private static GUIStyle BigFoldoutStyle
    {
        get
        {
            if (m_bigFoldoutStyle == null)
            {
                m_bigFoldoutStyle = new GUIStyle(EditorStyles.foldoutHeader)
                {
                    richText = true,
                    fontStyle = FontStyle.Bold,
                    fontSize = 12
                };
            }
            return m_bigFoldoutStyle;
        }
    }

    private static GUIStyle m_smallFoldoutStyle;
    private static GUIStyle SmallFoldoutStyle
    {
        get
        {
            if (m_smallFoldoutStyle == null)
            {
                m_smallFoldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    richText = true
                };
            }
            return m_smallFoldoutStyle;
        }
    }

    private static bool DrawBigCategoryFoldout(ref bool foldout, string title, string hexColor)
    {
        foldout = EditorGUILayout.Foldout(foldout, $"<color={hexColor}>{title}</color>", true, BigFoldoutStyle);
        return foldout;
    }

    private static bool DrawSmallCategoryFoldout(ref bool foldout, string title, string hexColor, Color boxColor)
    {
        var rect = EditorGUILayout.GetControlRect();
        EditorGUI.DrawRect(rect, boxColor);
        foldout = EditorGUI.Foldout(rect, foldout, $"<color={hexColor}>{title}</color>", true, SmallFoldoutStyle);
        return foldout;
    }

    #endregion

    #region OnEnable

    protected override void OnEnable()
    {
        base.OnEnable();

        // Scrollbar - Handle
        m_handleRect = serializedObject.FindProperty("m_handleRect");
        m_direction = serializedObject.FindProperty("m_direction");

        // Scrollbar - Value
        m_value = serializedObject.FindProperty("m_value");
        m_size = serializedObject.FindProperty("m_size");
        m_numberOfSteps = serializedObject.FindProperty("m_numberOfSteps");

        // Scrollbar - Fixed Handle Size
        m_useFixedHandleSize = serializedObject.FindProperty("m_useFixedHandleSize");
        m_fixedHandleSizeMode = serializedObject.FindProperty("m_fixedHandleSizeMode");
        m_fixedHandleRatio = serializedObject.FindProperty("m_fixedHandleRatio");
        m_fixedHandlePixelSize = serializedObject.FindProperty("m_fixedHandlePixelSize");

        // Loop Scrollbar - References
        m_leftHandle = serializedObject.FindProperty("m_leftHandle");
        m_rightHandle = serializedObject.FindProperty("m_rightHandle");
        m_graphics = serializedObject.FindProperty("m_graphics");

        // Events
        m_onValueChanged = serializedObject.FindProperty("m_onValueChanged");
        m_onLoopValueChanged = serializedObject.FindProperty("m_onLoopValueChanged");
        m_onBeginDragged = serializedObject.FindProperty("m_onBeginDragged");
        m_onEndDragged = serializedObject.FindProperty("m_onEndDragged");
    }

    #endregion

    #region OnInspectorGUI

    public override void OnInspectorGUI()
    {
        // 헬프 박스
        Rect helpBoxRect = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
        EditorDrawerHelper.DrawCustomHelpBox(helpBoxRect,
            "Sliding Area/Handle 오프셋과 루프 상태 업데이트가 자동으로 관리됩니다.",
            MessageType.Info, Color.white);

        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("[Recycle Scrollbar Settings]", TitleStyle);
        EditorDrawerHelper.DrawDividerLine();

        EditorGUI.indentLevel++;

        EditorGUILayout.Space(2f);

        DrawSelectableSection();
        EditorGUILayout.Space(4f);

        DrawScrollbarSection();
        EditorGUILayout.Space(4f);

        DrawLoopScrollbarSection();
        EditorGUILayout.Space(4f);

        DrawEventsSection();

        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();
    }

    #endregion

    #region Draw - Selectable

    private void DrawSelectableSection()
    {
        var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rect, m_bigBoxColorA);

        DrawBigCategoryFoldout(ref m_foldoutSelectable, "[Selectable]", BIG_TITLE_COLOR_SELECTABLE);
        if (m_foldoutSelectable)
        {
            EditorGUILayout.LabelField("↳ Interactable, Transition, Navigation 설정", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;
            base.OnInspectorGUI();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Draw - Scrollbar

    private void DrawScrollbarSection()
    {
        var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rect, m_bigBoxColorB);

        DrawBigCategoryFoldout(ref m_foldoutScrollbar, "[Scrollbar]", BIG_TITLE_COLOR_SCROLLBAR);
        if (m_foldoutScrollbar)
        {
            EditorGUILayout.LabelField("↳ 핸들, 방향 및 값 설정", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;

            // Handle
            var subRect1 = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(subRect1, m_smallBoxColorA);
            DrawSmallCategoryFoldout(ref m_foldoutHandle, "[Handle]",
                SMALL_TITLE_COLOR_A, m_smallBoxColorA);
            if (m_foldoutHandle)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_handleRect, new GUIContent("Handle Rect"));
                EditorGUILayout.PropertyField(m_direction, new GUIContent("Direction"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // Value
            var subRect2 = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(subRect2, m_smallBoxColorB);
            DrawSmallCategoryFoldout(ref m_foldoutValue, "[Value]",
                SMALL_TITLE_COLOR_B, m_smallBoxColorB);
            if (m_foldoutValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_value, new GUIContent("Value"));
                EditorGUILayout.PropertyField(m_size, new GUIContent("Size"));
                EditorGUILayout.PropertyField(m_numberOfSteps, new GUIContent("Number Of Steps"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // Fixed Handle Size
            var subRect3 = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(subRect3, m_smallBoxColorA);
            DrawSmallCategoryFoldout(ref m_foldoutFixedHandleSize, "[Fixed Handle Size]",
                SMALL_TITLE_COLOR_A, m_smallBoxColorA);
            if (m_foldoutFixedHandleSize)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_useFixedHandleSize, new GUIContent("Use Fixed Handle Size"));
                if (m_useFixedHandleSize.boolValue)
                {
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
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Draw - Loop Scrollbar

    private void DrawLoopScrollbarSection()
    {
        var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rect, m_bigBoxColorA);

        DrawBigCategoryFoldout(ref m_foldoutLoopScrollbar, "[Loop Scrollbar]", BIG_TITLE_COLOR_LOOP);
        if (m_foldoutLoopScrollbar)
        {
            EditorGUILayout.LabelField("↳ 루프 스크롤 전용 레퍼런스 설정", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;

            // References
            var subRect1 = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(subRect1, m_smallBoxColorA);
            DrawSmallCategoryFoldout(ref m_foldoutReferences, "[References]",
                SMALL_TITLE_COLOR_A, m_smallBoxColorA);
            if (m_foldoutReferences)
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
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Draw - Events

    private void DrawEventsSection()
    {
        var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rect, m_bigBoxColorB);

        DrawBigCategoryFoldout(ref m_foldoutEvents, "[Events]", BIG_TITLE_COLOR_EVENTS);
        if (m_foldoutEvents)
        {
            EditorGUILayout.LabelField("↳ 스크롤바 이벤트 콜백 설정", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;

            // Scrollbar Events
            var subRect1 = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(subRect1, m_smallBoxColorA);
            DrawSmallCategoryFoldout(ref m_foldoutScrollbarEvents, "[Scrollbar Events]",
                SMALL_TITLE_COLOR_A, m_smallBoxColorA);
            if (m_foldoutScrollbarEvents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_onValueChanged, new GUIContent("On Value Changed (float)"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // Loop Events
            var subRect2 = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(subRect2, m_smallBoxColorB);
            DrawSmallCategoryFoldout(ref m_foldoutLoopEvents, "[Loop Events]",
                SMALL_TITLE_COLOR_B, m_smallBoxColorB);
            if (m_foldoutLoopEvents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_onLoopValueChanged, new GUIContent("On Loop Value Changed"));
                EditorGUILayout.PropertyField(m_onBeginDragged, new GUIContent("On Begin Dragged"));
                EditorGUILayout.PropertyField(m_onEndDragged, new GUIContent("On End Dragged"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    #endregion
}
