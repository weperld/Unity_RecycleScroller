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

    // Events
    private SerializedProperty m_onValueChanged;

    // Scroll
    private SerializedProperty m_scrollAxis;
    private SerializedProperty m_viewport;
    private SerializedProperty m_content;
    private SerializedProperty m_movementType;
    private SerializedProperty m_elasticity;
    private SerializedProperty m_inertia;
    private SerializedProperty m_decelerationRate;
    private SerializedProperty m_scrollSensitivity;
    private SerializedProperty m_useScrollbar;
    private SerializedProperty m_verticalScrollbar;
    private SerializedProperty m_horizontalScrollbar;
    private SerializedProperty m_scrollbarVisibility;

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
    private static bool m_foldoutScrollRectSettings = DEFAULT_FOLD_OUT_STATE;
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
        // Events
        m_onValueChanged = serializedObject.FindProperty("m_onValueChanged");

        // Scroll
        m_scrollAxis = serializedObject.FindProperty("m_scrollAxis");
        m_viewport = serializedObject.FindProperty("m_viewport");
        m_content = serializedObject.FindProperty("m_content");
        m_movementType = serializedObject.FindProperty("m_movementType");
        m_elasticity = serializedObject.FindProperty("m_elasticity");
        m_inertia = serializedObject.FindProperty("m_inertia");
        m_decelerationRate = serializedObject.FindProperty("m_decelerationRate");
        m_scrollSensitivity = serializedObject.FindProperty("m_scrollSensitivity");
        m_useScrollbar = serializedObject.FindProperty("m_useScrollbar");
        m_verticalScrollbar = serializedObject.FindProperty("m_verticalScrollbar");
        m_horizontalScrollbar = serializedObject.FindProperty("m_horizontalScrollbar");
        m_scrollbarVisibility = serializedObject.FindProperty("m_scrollbarVisibility");

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

        DrawScrollSection(scroller);
        EditorGUILayout.Space(4f);

        DrawLayoutSection(scroller);
        EditorGUILayout.Space(4f);

        DrawFeaturesSection();
        EditorGUILayout.Space(4f);

        DrawAdvancedSection();
        EditorGUILayout.Space(4f);

        EditorGUILayout.PropertyField(m_onValueChanged);

        serializedObject.ApplyModifiedProperties();

        DrawDebugSection(scroller);

        EditorGUI.indentLevel--;
    }

    #endregion

    #region Draw - Scroll

    private void DrawScrollSection(RecycleScroller scroller)
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

            EditorDrawerHelper.DrawSmallCategory(ref m_foldoutScrollRectSettings, "[ScrollRect Settings]",
                EditorDrawerHelper.SMALL_TITLE_COLOR_B, EditorDrawerHelper.SmallBoxColorB, () =>
            {
                EditorGUI.BeginDisabledGroup(IsAppPlaying);
                EditorGUILayout.PropertyField(m_viewport, new GUIContent("Viewport"));
                EditorGUILayout.PropertyField(m_content, new GUIContent("Content"));
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.PropertyField(m_movementType, new GUIContent("Movement Type"));
                DrawOverwriteLabel(scroller.Debug_IsMovementTypeOverwritten, scroller.Debug_OverwrittenMovementType);
                if (m_movementType.enumValueIndex == (int)UnityEngine.UI.ScrollRect.MovementType.Elastic)
                    EditorGUILayout.PropertyField(m_elasticity);
                EditorGUILayout.PropertyField(m_inertia);
                if (m_inertia.boolValue)
                    EditorGUILayout.PropertyField(m_decelerationRate, new GUIContent("Deceleration Rate"));
                EditorGUILayout.PropertyField(m_scrollSensitivity, new GUIContent("Scroll Sensitivity"));
            });

            EditorDrawerHelper.DrawSmallCategory(ref m_foldoutScrollbar, "[Scrollbar]",
                EditorDrawerHelper.SMALL_TITLE_COLOR_A, EditorDrawerHelper.SmallBoxColorA, () =>
            {
                EditorGUILayout.PropertyField(m_useScrollbar, new GUIContent("Use Scrollbar"));

                if (m_useScrollbar.boolValue)
                {
                    EditorGUI.BeginDisabledGroup(IsAppPlaying);
                    EditorGUILayout.PropertyField(m_verticalScrollbar, new GUIContent("Vertical Scrollbar"));
                    EditorGUILayout.PropertyField(m_horizontalScrollbar, new GUIContent("Horizontal Scrollbar"));
                    EditorGUILayout.PropertyField(m_scrollbarVisibility, new GUIContent("Visibility"));
                    EditorDrawerHelper.DrawCustomHelpBox(
                        "주축(셀 재활용 방향) RecycleScrollbar만 활성화됩니다.\n"
                        + "반대축 스크롤바는 자동으로 비활성화됩니다.",
                        MessageType.Info, Color.white);
                    EditorGUI.EndDisabledGroup();
                }
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
            DrawCellAlignmentSubSection(scroller);
            DrawCellGroupSubSection(scroller);
            DrawContentFitSubSection();
        });
    }

    private void DrawCellAlignmentSubSection(RecycleScroller scroller)
    {
        EditorDrawerHelper.DrawSmallCategory(ref m_foldoutCellAlignment, "[Cell Alignment]",
            EditorDrawerHelper.SMALL_TITLE_COLOR_A, EditorDrawerHelper.SmallBoxColorA, () =>
        {
            EditorGUI.BeginDisabledGroup(IsAppPlaying);
            EditorGUILayout.PropertyField(m_padding, new GUIContent("Layout Padding"));
            EditorGUILayout.PropertyField(m_spacing);
            EditorGUILayout.PropertyField(m_childAlignment);
            DrawOverwriteLabel(scroller.Debug_IsChildAlignmentOverwritten, scroller.Debug_OverwrittenChildAlignment);
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

    #region Draw - Overwrite Label

    private static GUIStyle s_overwriteLabelStyle;

    private static GUIStyle OverwriteLabelStyle
    {
        get
        {
            if (s_overwriteLabelStyle == null)
            {
                s_overwriteLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                    fontSize = 10,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, -2, 2),
                };
            }
            return s_overwriteLabelStyle;
        }
    }

    private static void DrawOverwriteLabel<T>(bool isOverwritten, T overwrittenValue)
    {
        if (isOverwritten == false || Application.isPlaying == false) return;

        var indent = EditorGUI.indentLevel * 15f;
        var rect = EditorGUILayout.GetControlRect(false, 14f);
        rect.x += indent + 16f;
        rect.width -= indent + 16f;
        EditorGUI.LabelField(rect, $"\u21b3 Overwritten: {overwrittenValue}", OverwriteLabelStyle);
    }

    #endregion
}
