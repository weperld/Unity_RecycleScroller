using System;
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

    #region Big Title Colors

    private const string BIG_TITLE_COLOR_SCROLL = "#FFFF99";
    private const string BIG_TITLE_COLOR_LAYOUT = "#FF99CC";
    private const string BIG_TITLE_COLOR_FEATURES = "#99FFBB";
    private const string BIG_TITLE_COLOR_ADVANCED = "#CC99FF";

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

        EditorGUILayout.LabelField("[Recycle Scroller Settings]", EditorDrawerHelper.InspectorTitleStyle);
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
        EditorDrawerHelper.DrawBigCategory(ref m_foldoutScroll, "[Scroll]", BIG_TITLE_COLOR_SCROLL,
            EditorDrawerHelper.BigBoxColorA, "스크롤 축 및 스크롤바 설정", () =>
        {
            EditorDrawerHelper.DrawSmallCategory(ref m_foldoutScrollAxis, "[Scroll Axis]",
                EditorDrawerHelper.SMALL_TITLE_COLOR_A, EditorDrawerHelper.SmallBoxColorA, () =>
            {
                EditorGUI.BeginDisabledGroup(IsAppPlaying);
                EditorGUILayout.PropertyField(m_scrollAxis);
                EditorGUI.EndDisabledGroup();
            });

            EditorDrawerHelper.DrawSmallCategory(ref m_foldoutScrollbar, "[Scrollbar]",
                EditorDrawerHelper.SMALL_TITLE_COLOR_B, EditorDrawerHelper.SmallBoxColorB, () =>
            {
                EditorGUI.BeginDisabledGroup(IsAppPlaying);
                EditorGUILayout.PropertyField(m_scrollbarRef, new GUIContent("Recycle Scrollbar"));
                EditorDrawerHelper.DrawCustomHelpBox(
                    "Recycle Scrollbar: 주축(셀 재활용 방향) 전용 스크롤바\n"
                    + "보조축(그룹 내 셀 배치 방향) 스크롤이 필요한 경우 ScrollRect의 기본 Scrollbar를 사용하세요",
                    MessageType.Info, Color.white);
                EditorGUI.EndDisabledGroup();
            });
        });
    }

    #endregion

    #region Draw - Layout

    private void DrawLayoutSection(RecycleScroller scroller)
    {
        EditorDrawerHelper.DrawBigCategory(ref m_foldoutLayout, "[Layout]", BIG_TITLE_COLOR_LAYOUT,
            EditorDrawerHelper.BigBoxColorB, "셀 정렬, 그룹 배치 및 콘텐트 크기 설정", () =>
        {
            DrawCellAlignmentSubSection();
            DrawCellGroupSubSection(scroller);
            DrawContentFitSubSection();
        });
    }

    private void DrawCellAlignmentSubSection()
    {
        EditorDrawerHelper.DrawSmallCategory(ref m_foldoutCellAlignment, "[Cell Alignment]",
            EditorDrawerHelper.SMALL_TITLE_COLOR_A, EditorDrawerHelper.SmallBoxColorA, () =>
        {
            EditorGUI.BeginDisabledGroup(IsAppPlaying);
            EditorGUILayout.PropertyField(m_padding, new GUIContent("Layout Padding"));
            EditorGUILayout.PropertyField(m_spacing);
            EditorGUILayout.PropertyField(m_childAlignment);
            EditorGUILayout.PropertyField(m_reverse);
            EditorGUILayout.PropertyField(m_controlChildSize);
            EditorGUILayout.PropertyField(m_useChildScale);
            EditorGUILayout.PropertyField(m_childForceExpand);
            EditorGUI.EndDisabledGroup();
        });
    }

    private void DrawCellGroupSubSection(RecycleScroller scroller)
    {
        EditorDrawerHelper.DrawSmallCategory(ref m_foldoutCellGroup, "[Cell Group]",
            EditorDrawerHelper.SMALL_TITLE_COLOR_B, EditorDrawerHelper.SmallBoxColorB, () =>
        {
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
        });
    }

    private void DrawContentFitSubSection()
    {
        EditorDrawerHelper.DrawSmallCategory(ref m_foldoutContentFit, "[Content Fit]",
            EditorDrawerHelper.SMALL_TITLE_COLOR_A, EditorDrawerHelper.SmallBoxColorA, () =>
        {
            EditorGUI.BeginDisabledGroup(IsAppPlaying);
            EditorGUILayout.PropertyField(m_fitContentToViewport);
            EditorGUI.EndDisabledGroup();
        });
    }

    #endregion

    #region Draw - Features

    private void DrawFeaturesSection()
    {
        EditorDrawerHelper.DrawBigCategory(ref m_foldoutFeatures, "[Features]", BIG_TITLE_COLOR_FEATURES,
            EditorDrawerHelper.BigBoxColorA, "루프 스크롤 및 페이징 기능 설정", () =>
        {
            EditorDrawerHelper.DrawSmallCategory(ref m_foldoutLoopScroll, "[Loop Scroll]",
                EditorDrawerHelper.SMALL_TITLE_COLOR_A, EditorDrawerHelper.SmallBoxColorA, () =>
            {
                EditorGUI.BeginDisabledGroup(IsAppPlaying);
                EditorGUILayout.PropertyField(m_loopScroll);
                EditorGUI.EndDisabledGroup();
            });

            EditorDrawerHelper.DrawSmallCategory(ref m_foldoutPaging, "[Paging]",
                EditorDrawerHelper.SMALL_TITLE_COLOR_B, EditorDrawerHelper.SmallBoxColorB, () =>
            {
                EditorGUILayout.PropertyField(m_pagingData, new GUIContent("Use Page Configs"));
            });
        });
    }

    #endregion

    #region Draw - Advanced

    private void DrawAdvancedSection()
    {
        EditorDrawerHelper.DrawBigCategory(ref m_foldoutAdvanced, "[Advanced]", BIG_TITLE_COLOR_ADVANCED,
            EditorDrawerHelper.BigBoxColorB, "오브젝트 풀 및 에디터 도구 설정", () =>
        {
            EditorDrawerHelper.DrawSmallCategory(ref m_foldoutPoolManagement, "[Pool Management]",
                EditorDrawerHelper.SMALL_TITLE_COLOR_A, EditorDrawerHelper.SmallBoxColorA, () =>
            {
                EditorGUI.BeginDisabledGroup(IsAppPlaying);
                EditorGUILayout.PropertyField(m_maxPoolSizePerType, new GUIContent("Max Pool Size Per Type"));
                EditorGUI.EndDisabledGroup();
            });

            EditorDrawerHelper.DrawSmallCategory(ref m_foldoutExampleLayout, "[Example Layout]",
                EditorDrawerHelper.SMALL_TITLE_COLOR_B, EditorDrawerHelper.SmallBoxColorB, () =>
            {
                EditorGUILayout.PropertyField(m_exampleLayoutGroups);
            });
        });
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
