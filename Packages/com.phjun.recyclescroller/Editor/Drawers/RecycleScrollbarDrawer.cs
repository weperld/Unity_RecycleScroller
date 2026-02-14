using System;
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

    // Extra Transitions
    private SerializedProperty m_extraTransitions;

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

    #region Big Title Colors

    private const string BIG_TITLE_COLOR_SELECTABLE = "#FF99CC";
    private const string BIG_TITLE_COLOR_SCROLLBAR = "#FFFF99";
    private const string BIG_TITLE_COLOR_LOOP = "#99FFBB";
    private const string BIG_TITLE_COLOR_EVENTS = "#CC99FF";

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

        // Extra Transitions
        m_extraTransitions = serializedObject.FindProperty("m_extraTransitions");

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
        EditorGUILayout.LabelField("[Recycle Scrollbar Settings]", EditorDrawerHelper.InspectorTitleStyle);
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

    // base.OnInspectorGUI()는 람다에서 호출 불가하므로 래퍼 사용
    private void DrawBaseSelectableGUI() => base.OnInspectorGUI();

    private void DrawSelectableSection()
    {
        EditorDrawerHelper.DrawBigCategory(ref m_foldoutSelectable, "[Selectable]", BIG_TITLE_COLOR_SELECTABLE,
            EditorDrawerHelper.BigBoxColorA, "Interactable, Transition, Navigation, Extra Transitions 설정", () =>
        {
            DrawBaseSelectableGUI();
            EditorGUILayout.Space(2f);
            int prevSize = m_extraTransitions.arraySize;
            EditorGUILayout.PropertyField(m_extraTransitions, new GUIContent("Extra Transitions",
                "각 항목이 독립적인 Transition/ColorBlock/SpriteState를 가집니다.\n"
                + "메인 핸들(또는 자식)의 그래픽을 지정하면 서브 핸들에도 동일 트랜지션이 자동 적용됩니다."));

            // 새 엔트리 추가 시 기본 Selectable 트랜지션 설정 자동 복사
            if (m_extraTransitions.arraySize > prevSize)
            {
                for (int i = prevSize; i < m_extraTransitions.arraySize; i++)
                    ExtraTransitionEntryDrawer.CopyBaseTransitionToEntry(
                        m_extraTransitions.GetArrayElementAtIndex(i));
            }
        });
    }

    #endregion

    #region Draw - Scrollbar

    private void DrawScrollbarSection()
    {
        EditorDrawerHelper.DrawBigCategory(ref m_foldoutScrollbar, "[Scrollbar]", BIG_TITLE_COLOR_SCROLLBAR,
            EditorDrawerHelper.BigBoxColorB, "핸들, 방향 및 값 설정", () =>
        {
            EditorDrawerHelper.DrawSmallCategory(ref m_foldoutHandle, "[Handle]",
                EditorDrawerHelper.SMALL_TITLE_COLOR_A, EditorDrawerHelper.SmallBoxColorA, () =>
            {
                EditorGUILayout.PropertyField(m_handleRect, new GUIContent("Handle Rect"));
                EditorGUILayout.PropertyField(m_direction, new GUIContent("Direction"));
            });

            EditorDrawerHelper.DrawSmallCategory(ref m_foldoutValue, "[Value]",
                EditorDrawerHelper.SMALL_TITLE_COLOR_B, EditorDrawerHelper.SmallBoxColorB, () =>
            {
                EditorGUILayout.PropertyField(m_value, new GUIContent("Value"));
                EditorGUILayout.PropertyField(m_size, new GUIContent("Size"));
                EditorGUILayout.PropertyField(m_numberOfSteps, new GUIContent("Number Of Steps"));
            });

            EditorDrawerHelper.DrawSmallCategory(ref m_foldoutFixedHandleSize, "[Fixed Handle Size]",
                EditorDrawerHelper.SMALL_TITLE_COLOR_A, EditorDrawerHelper.SmallBoxColorA, () =>
            {
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
            });
        });
    }

    #endregion

    #region Draw - Loop Scrollbar

    private void DrawLoopScrollbarSection()
    {
        EditorDrawerHelper.DrawBigCategory(ref m_foldoutLoopScrollbar, "[Loop Scrollbar]", BIG_TITLE_COLOR_LOOP,
            EditorDrawerHelper.BigBoxColorA, "루프 스크롤 전용 레퍼런스 (자동 관리, 읽기 전용)", () =>
        {
            EditorDrawerHelper.DrawSmallCategory(ref m_foldoutReferences, "[References]",
                EditorDrawerHelper.SMALL_TITLE_COLOR_A, EditorDrawerHelper.SmallBoxColorA, () =>
            {
                EditorDrawerHelper.DrawCustomHelpBox(
                    "Sub Handle 0/1: 루프 스크롤 시 핸들 양쪽에 표시되는 보조 핸들 (자동 생성)\n"
                    + "서브 핸들의 트랜지션 그래픽은 Selectable > Extra Transitions 설정에 따라 자동 감지됩니다.",
                    MessageType.Info, Color.white);
                EditorGUILayout.Space(2f);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(m_leftHandle, new GUIContent("Sub Handle 0"));
                EditorGUILayout.PropertyField(m_rightHandle, new GUIContent("Sub Handle 1"));
                EditorGUI.EndDisabledGroup();
            });
        });
    }

    #endregion

    #region Draw - Events

    private void DrawEventsSection()
    {
        EditorDrawerHelper.DrawBigCategory(ref m_foldoutEvents, "[Events]", BIG_TITLE_COLOR_EVENTS,
            EditorDrawerHelper.BigBoxColorB, "스크롤바 이벤트 콜백 설정", () =>
        {
            EditorDrawerHelper.DrawSmallCategory(ref m_foldoutScrollbarEvents, "[Scrollbar Events]",
                EditorDrawerHelper.SMALL_TITLE_COLOR_A, EditorDrawerHelper.SmallBoxColorA, () =>
            {
                EditorGUILayout.PropertyField(m_onValueChanged, new GUIContent("On Value Changed (float)"));
            });

            EditorDrawerHelper.DrawSmallCategory(ref m_foldoutLoopEvents, "[Loop Events]",
                EditorDrawerHelper.SMALL_TITLE_COLOR_B, EditorDrawerHelper.SmallBoxColorB, () =>
            {
                EditorGUILayout.PropertyField(m_onLoopValueChanged, new GUIContent("On Loop Value Changed"));
                EditorGUILayout.PropertyField(m_onBeginDragged, new GUIContent("On Begin Dragged"));
                EditorGUILayout.PropertyField(m_onEndDragged, new GUIContent("On End Dragged"));
            });
        });
    }

    #endregion
}
