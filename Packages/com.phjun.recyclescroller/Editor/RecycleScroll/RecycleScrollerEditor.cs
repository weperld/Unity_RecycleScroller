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

    #region SerializedProperties

    // Scroll
    private SerializedProperty m_scrollAxis;
    private SerializedProperty m_scrollbarRef;

    // Layout - Cell Alignment
    private SerializedProperty m_padding;
    private SerializedProperty m_spacing;
    private SerializedProperty m_childAlignment;
    private SerializedProperty m_reverse;
    private SerializedProperty m_controlChildSize;
    private SerializedProperty m_useChildScale;
    private SerializedProperty m_childForceExpand;

    // Layout - Cell Group
    private SerializedProperty m_fixedCellCountInGroup;
    private SerializedProperty m_fixedCellCount;
    private SerializedProperty m_useMinMaxFlexibleCellCount;
    private SerializedProperty m_flexibleCellCountLimit;
    private SerializedProperty m_spacingInGroup;

    // Layout - Content Fit
    private SerializedProperty m_fitContentToViewport;

    // Features
    private SerializedProperty m_loopScroll;
    private SerializedProperty m_pagingData;

    // Advanced
    private SerializedProperty m_maxPoolSizePerType;
    private SerializedProperty m_exampleLayoutGroups;

    #endregion

    #region Foldout States

    private const bool DEFAULT_FOLD_OUT_STATE = false;

    // Big categories
    private static bool m_foldoutScroll = DEFAULT_FOLD_OUT_STATE;
    private static bool m_foldoutLayout = DEFAULT_FOLD_OUT_STATE;
    private static bool m_foldoutFeatures = DEFAULT_FOLD_OUT_STATE;
    private static bool m_foldoutAdvanced = DEFAULT_FOLD_OUT_STATE;

    // Scroll
    private static bool m_foldoutScrollAxis = DEFAULT_FOLD_OUT_STATE;
    private static bool m_foldoutScrollbar = DEFAULT_FOLD_OUT_STATE;

    // Layout
    private static bool m_foldoutCellAlignment = DEFAULT_FOLD_OUT_STATE;
    private static bool m_foldoutCellGroup = DEFAULT_FOLD_OUT_STATE;
    private static bool m_foldoutContentFit = DEFAULT_FOLD_OUT_STATE;

    // Features
    private static bool m_foldoutLoopScroll = DEFAULT_FOLD_OUT_STATE;
    private static bool m_foldoutPaging = DEFAULT_FOLD_OUT_STATE;

    // Advanced
    private static bool m_foldoutPoolManagement = DEFAULT_FOLD_OUT_STATE;
    private static bool m_foldoutExampleLayout = DEFAULT_FOLD_OUT_STATE;

    #endregion

    #region Foldout Styles & Colors

    // Big category title colors (each unique pastel)
    private const string BIG_TITLE_COLOR_SCROLL = "#FFFF99";
    private const string BIG_TITLE_COLOR_LAYOUT = "#FF99CC";
    private const string BIG_TITLE_COLOR_FEATURES = "#99FFBB";
    private const string BIG_TITLE_COLOR_ADVANCED = "#CC99FF";

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

    private void OnEnable()
    {
        // Scroll
        m_scrollAxis = serializedObject.FindProperty("m_scrollAxis");
        m_scrollbarRef = serializedObject.FindProperty("m_scrollbarRef");

        // Layout - Cell Alignment
        m_padding = serializedObject.FindProperty("m_padding");
        m_spacing = serializedObject.FindProperty("m_spacing");
        m_childAlignment = serializedObject.FindProperty("m_childAlignment");
        m_reverse = serializedObject.FindProperty("m_reverse");
        m_controlChildSize = serializedObject.FindProperty("m_controlChildSize");
        m_useChildScale = serializedObject.FindProperty("m_useChildScale");
        m_childForceExpand = serializedObject.FindProperty("m_childForceExpand");

        // Layout - Cell Group
        m_fixedCellCountInGroup = serializedObject.FindProperty("m_fixedCellCountInGroup");
        m_fixedCellCount = serializedObject.FindProperty("m_fixedCellCount");
        m_useMinMaxFlexibleCellCount = serializedObject.FindProperty("m_useMinMaxFlexibleCellCount");
        m_flexibleCellCountLimit = serializedObject.FindProperty("m_flexibleCellCountLimit");
        m_spacingInGroup = serializedObject.FindProperty("m_spacingInGroup");

        // Layout - Content Fit
        m_fitContentToViewport = serializedObject.FindProperty("m_fitContentToViewport");

        // Features
        m_loopScroll = serializedObject.FindProperty("m_loopScroll");
        m_pagingData = serializedObject.FindProperty("m_pagingData");

        // Advanced
        m_maxPoolSizePerType = serializedObject.FindProperty("m_maxPoolSizePerType");
        m_exampleLayoutGroups = serializedObject.FindProperty("m_exampleLayoutGroups");
    }

    #endregion

    #region OnInspectorGUI

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var scroller = (RecycleScroller)target;

        EditorGUILayout.LabelField("[Recycle Scroller Settings]", TitleStyle);
        EditorDrawerHelper.DrawDividerLine();

        EditorGUI.indentLevel++;

        EditorGUILayout.Space(2f);

        DrawScrollSection();
        EditorGUILayout.Space(4f);

        DrawLayoutSection(scroller);
        EditorGUILayout.Space(4f);

        DrawFeaturesSection();
        EditorGUILayout.Space(4f);

        DrawAdvancedSection();

        serializedObject.ApplyModifiedProperties();

        DrawDebugSection(scroller);

        EditorGUI.indentLevel--;
    }

    #endregion

    #region Draw - Scroll

    private void DrawScrollSection()
    {
        var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rect, m_bigBoxColorA);

        DrawBigCategoryFoldout(ref m_foldoutScroll, "[Scroll]", BIG_TITLE_COLOR_SCROLL);
        if (m_foldoutScroll)
        {
            EditorGUILayout.LabelField("↳ 스크롤 축 및 스크롤바 설정", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;

            // Scroll Axis
            var subRect1 = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(subRect1, m_smallBoxColorA);
            DrawSmallCategoryFoldout(ref m_foldoutScrollAxis, "[Scroll Axis]",
                SMALL_TITLE_COLOR_A, m_smallBoxColorA);
            if (m_foldoutScrollAxis)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(IsAppPlaying);
                EditorGUILayout.PropertyField(m_scrollAxis);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // Scrollbar
            var subRect2 = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(subRect2, m_smallBoxColorB);
            DrawSmallCategoryFoldout(ref m_foldoutScrollbar, "[Scrollbar]",
                SMALL_TITLE_COLOR_B, m_smallBoxColorB);
            if (m_foldoutScrollbar)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(IsAppPlaying);
                EditorGUILayout.PropertyField(m_scrollbarRef, new GUIContent("Recycle Scrollbar"));
                EditorDrawerHelper.DrawCustomHelpBox(
                    "Recycle Scrollbar: 주축(셀 재활용 방향) 전용 스크롤바\n"
                    + "보조축(그룹 내 셀 배치 방향) 스크롤이 필요한 경우 ScrollRect의 기본 Scrollbar를 사용하세요",
                    MessageType.Info, Color.white);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Draw - Layout

    private void DrawLayoutSection(RecycleScroller scroller)
    {
        var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rect, m_bigBoxColorB);

        DrawBigCategoryFoldout(ref m_foldoutLayout, "[Layout]", BIG_TITLE_COLOR_LAYOUT);
        if (m_foldoutLayout)
        {
            EditorGUILayout.LabelField("↳ 셀 정렬, 그룹 배치 및 콘텐트 크기 설정", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;

            DrawCellAlignmentSubSection();
            DrawCellGroupSubSection(scroller);
            DrawContentFitSubSection();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCellAlignmentSubSection()
    {
        var subRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(subRect, m_smallBoxColorA);
        DrawSmallCategoryFoldout(ref m_foldoutCellAlignment, "[Cell Alignment]",
            SMALL_TITLE_COLOR_A, m_smallBoxColorA);
        if (m_foldoutCellAlignment)
        {
            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(IsAppPlaying);
            EditorGUILayout.PropertyField(m_padding, new GUIContent("Layout Padding"));
            EditorGUILayout.PropertyField(m_spacing);
            EditorGUILayout.PropertyField(m_childAlignment);
            EditorGUILayout.PropertyField(m_reverse);
            EditorGUILayout.PropertyField(m_controlChildSize);
            EditorGUILayout.PropertyField(m_useChildScale);
            EditorGUILayout.PropertyField(m_childForceExpand);
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawCellGroupSubSection(RecycleScroller scroller)
    {
        var subRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(subRect, m_smallBoxColorB);
        DrawSmallCategoryFoldout(ref m_foldoutCellGroup, "[Cell Group]",
            SMALL_TITLE_COLOR_B, m_smallBoxColorB);
        if (m_foldoutCellGroup)
        {
            EditorGUI.indentLevel++;

            EditorGUI.BeginDisabledGroup(IsAppPlaying);
            EditorGUILayout.PropertyField(m_fixedCellCountInGroup);
            EditorGUI.EndDisabledGroup();

            // Fixed Cell Count (ConditionalDisableOrHide)
            var fixedCountState = scroller.FixCellCountInGroup
                ? IsAppPlaying ? FieldShowState.Disable : FieldShowState.Show
                : FieldShowState.Hide;
            if (fixedCountState is not FieldShowState.Hide)
            {
                EditorGUI.BeginDisabledGroup(fixedCountState is FieldShowState.Disable);
                EditorGUILayout.PropertyField(m_fixedCellCount);
                EditorGUI.EndDisabledGroup();
            }

            // Use MinMax Flexible Cell Count (ConditionalDisableOrHide)
            var flexibleCountState = !scroller.FixCellCountInGroup
                ? IsAppPlaying ? FieldShowState.Disable : FieldShowState.Show
                : FieldShowState.Hide;
            if (flexibleCountState is not FieldShowState.Hide)
            {
                EditorGUI.BeginDisabledGroup(flexibleCountState is FieldShowState.Disable);
                EditorGUILayout.PropertyField(m_useMinMaxFlexibleCellCount);
                EditorGUI.EndDisabledGroup();
            }

            // Flexible Cell Count Limit (ConditionalDisableOrHide)
            var flexibleLimitState = scroller.ShowMinMaxFlexibleCellCount
                ? IsAppPlaying ? FieldShowState.Disable : FieldShowState.Show
                : FieldShowState.Hide;
            if (flexibleLimitState is not FieldShowState.Hide)
            {
                EditorGUI.BeginDisabledGroup(flexibleLimitState is FieldShowState.Disable);
                EditorGUILayout.PropertyField(m_flexibleCellCountLimit);
                EditorGUI.EndDisabledGroup();
            }

            // Spacing In Group (ConditionalDisableOrHide)
            var spacingState = scroller.ShowSpacingInGroup
                ? IsAppPlaying ? FieldShowState.Disable : FieldShowState.Show
                : FieldShowState.Hide;
            if (spacingState is not FieldShowState.Hide)
            {
                EditorGUI.BeginDisabledGroup(spacingState is FieldShowState.Disable);
                EditorGUILayout.PropertyField(m_spacingInGroup);
                EditorGUI.EndDisabledGroup();
            }

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawContentFitSubSection()
    {
        var subRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(subRect, m_smallBoxColorA);
        DrawSmallCategoryFoldout(ref m_foldoutContentFit, "[Content Fit]",
            SMALL_TITLE_COLOR_A, m_smallBoxColorA);
        if (m_foldoutContentFit)
        {
            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(IsAppPlaying);
            EditorGUILayout.PropertyField(m_fitContentToViewport);
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Draw - Features

    private void DrawFeaturesSection()
    {
        var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rect, m_bigBoxColorA);

        DrawBigCategoryFoldout(ref m_foldoutFeatures, "[Features]", BIG_TITLE_COLOR_FEATURES);
        if (m_foldoutFeatures)
        {
            EditorGUILayout.LabelField("↳ 루프 스크롤 및 페이징 기능 설정", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;

            // Loop Scroll
            var subRect1 = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(subRect1, m_smallBoxColorA);
            DrawSmallCategoryFoldout(ref m_foldoutLoopScroll, "[Loop Scroll]",
                SMALL_TITLE_COLOR_A, m_smallBoxColorA);
            if (m_foldoutLoopScroll)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(IsAppPlaying);
                EditorGUILayout.PropertyField(m_loopScroll);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // Paging
            var subRect2 = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(subRect2, m_smallBoxColorB);
            DrawSmallCategoryFoldout(ref m_foldoutPaging, "[Paging]",
                SMALL_TITLE_COLOR_B, m_smallBoxColorB);
            if (m_foldoutPaging)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_pagingData, new GUIContent("Use Page Configs"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Draw - Advanced

    private void DrawAdvancedSection()
    {
        var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rect, m_bigBoxColorB);

        DrawBigCategoryFoldout(ref m_foldoutAdvanced, "[Advanced]", BIG_TITLE_COLOR_ADVANCED);
        if (m_foldoutAdvanced)
        {
            EditorGUILayout.LabelField("↳ 오브젝트 풀 및 에디터 도구 설정", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;

            // Pool Management
            var subRect1 = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(subRect1, m_smallBoxColorA);
            DrawSmallCategoryFoldout(ref m_foldoutPoolManagement, "[Pool Management]",
                SMALL_TITLE_COLOR_A, m_smallBoxColorA);
            if (m_foldoutPoolManagement)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(IsAppPlaying);
                EditorGUILayout.PropertyField(m_maxPoolSizePerType, new GUIContent("Max Pool Size Per Type"));
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // Example Layout
            var subRect2 = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(subRect2, m_smallBoxColorB);
            DrawSmallCategoryFoldout(ref m_foldoutExampleLayout, "[Example Layout]",
                SMALL_TITLE_COLOR_B, m_smallBoxColorB);
            if (m_foldoutExampleLayout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_exampleLayoutGroups);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Draw - Debug

    private void DrawDebugSection(RecycleScroller scroller)
    {
        if (!Application.isPlaying) return;

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("[Debug Info]", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("Active Cells", scroller.Debug_ActiveCellCount);
        EditorGUILayout.IntField("Active Groups", scroller.Debug_ActiveGroupCount);
        EditorGUILayout.IntField("Pooled Cells", scroller.Debug_PooledCellCount);
        EditorGUILayout.FloatField("Scroll Position", scroller.ShowingNormalizedScrollPosition);

        var pageIndex = scroller.NearestPageIndexByScrollPos;
        var pageCount = scroller.ShowingPageCount;
        EditorGUILayout.TextField("Current Page", pageIndex >= 0 ? $"{pageIndex} / {pageCount}" : "N/A");

        EditorGUILayout.EnumPopup("Load Data State", scroller.Debug_LoadDataState);
        EditorGUI.EndDisabledGroup();

        Repaint();
    }

    #endregion
}
