using RecycleScroll;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스크롤이 불가능한 뷰에서 Child Alignment가 무시되던 문제를 테스트 씬에서 재현·검증하기 위한 프리셋.
/// 에디터가 열린 상태에서 씬 파일을 외부 편집하면 충돌하므로 메뉴로 적용한다
/// </summary>
public static class RecycleScrollerTestPreset
{
    private const string MENU_ROOT = "Tools/RecycleScroller/";
    private const string PREFS_KEY_SCROLLER = "RecycleScrollerTestPreset.Backup.Scroller";
    private const string PREFS_KEY_TEST = "RecycleScrollerTestPreset.Backup.Test";

    [MenuItem(MENU_ROOT + "재현 프리셋 적용 - 중앙 정렬 (스크롤 없음)", priority = 0)]
    private static void ApplyPreset()
    {
        if (TryFindTargets(out var scroller, out var test) == false) return;

        EditorPrefs.SetString(PREFS_KEY_SCROLLER, EditorJsonUtility.ToJson(scroller));
        EditorPrefs.SetString(PREFS_KEY_TEST, EditorJsonUtility.ToJson(test));

        Undo.RecordObjects(new Object[] { scroller, test }, "Apply RecycleScroller Test Preset");
        ApplyScrollerPreset(scroller);
        ApplyTestPreset(test);
        MarkSceneDirty(scroller, test);

        Debug.Log("[RecycleScrollerTestPreset] 프리셋 적용 완료 — Play 후 셀 1개가 뷰포트 가로 중앙에 오는지 확인", scroller);
    }

    [MenuItem(MENU_ROOT + "재현 프리셋 원복", priority = 1)]
    private static void RestorePreset()
    {
        if (TryFindTargets(out var scroller, out var test) == false) return;

        if (EditorPrefs.HasKey(PREFS_KEY_SCROLLER) == false)
        {
            Debug.LogWarning("[RecycleScrollerTestPreset] 백업이 없습니다 — 프리셋을 먼저 적용해야 원복할 수 있습니다");
            return;
        }

        Undo.RecordObjects(new Object[] { scroller, test }, "Restore RecycleScroller Test Preset");
        EditorJsonUtility.FromJsonOverwrite(EditorPrefs.GetString(PREFS_KEY_SCROLLER), scroller);
        EditorJsonUtility.FromJsonOverwrite(EditorPrefs.GetString(PREFS_KEY_TEST), test);
        MarkSceneDirty(scroller, test);

        Debug.Log("[RecycleScrollerTestPreset] 프리셋 적용 직전 값으로 원복", scroller);
    }

    #region Preset Values

    private static void ApplyScrollerPreset(RecycleScroller scroller)
    {
        var so = new SerializedObject(scroller);

        SetEnum(so, "m_scrollAxis", (int)eScrollAxis.HORIZONTAL);
        SetEnum(so, "m_childAlignment", (int)TextAnchor.MiddleCenter);
        SetBool(so, "m_reverse", false);

        SetPadding(so, "m_padding", left: 0, right: 0, top: 0, bottom: 16);
        SetFloat(so, "m_spacing", 32f);
        SetFloat(so, "m_spacingInGroup", 0f);

        SetBoolVector2(so, "m_controlChildSize", false, false);
        SetBoolVector2(so, "m_useChildScale", false, false);
        SetBoolVector2(so, "m_childForceExpand", false, false);

        SetBool(so, "m_fitContentToViewport", true);
        SetBool(so, "m_fixedCellCountInGroup", true);
        SetInt(so, "m_fixedCellCount", 1);

        SetBool(so, "m_loopScroll", false);
        SetBool(so, "m_pagingData.usePaging", false);
        SetBool(so, "m_useScrollbar", false);

        SetEnum(so, "m_movementType", (int)ScrollRect.MovementType.Clamped);
        SetBool(so, "m_inertia", false);

        so.ApplyModifiedProperties();
    }

    private static void ApplyTestPreset(RecycleScrollerTest test)
    {
        var so = new SerializedObject(test);

        SetInt(so, "m_cellCount", 1);
        SetVector2(so, "m_cellMainSizeRange", new Vector2(130f, 130f));
        SetVector2(so, "m_cellCrossSizeRange", new Vector2(236f, 236f));
        SetVector2(so, "m_cellScale", Vector2.one);
        SetBool(so, "m_loadDataOnStart", true);

        so.ApplyModifiedProperties();
    }

    #endregion

    #region Serialized Property Helpers

    private static SerializedProperty Find(SerializedObject so, string path)
    {
        var prop = so.FindProperty(path);
        if (prop == null) Debug.LogError($"[RecycleScrollerTestPreset] 직렬화 필드를 찾지 못했습니다: {path}");
        return prop;
    }

    private static void SetBool(SerializedObject so, string path, bool value)
    {
        var prop = Find(so, path);
        if (prop == null) return;
        prop.boolValue = value;
    }

    private static void SetInt(SerializedObject so, string path, int value)
    {
        var prop = Find(so, path);
        if (prop == null) return;
        prop.intValue = value;
    }

    private static void SetFloat(SerializedObject so, string path, float value)
    {
        var prop = Find(so, path);
        if (prop == null) return;
        prop.floatValue = value;
    }

    private static void SetEnum(SerializedObject so, string path, int enumValue)
    {
        var prop = Find(so, path);
        if (prop == null) return;
        prop.enumValueIndex = enumValue;
    }

    private static void SetVector2(SerializedObject so, string path, Vector2 value)
    {
        var prop = Find(so, path);
        if (prop == null) return;
        prop.vector2Value = value;
    }

    private static void SetBoolVector2(SerializedObject so, string path, bool width, bool height)
    {
        var prop = Find(so, path);
        if (prop == null) return;
        prop.FindPropertyRelative("width").boolValue = width;
        prop.FindPropertyRelative("height").boolValue = height;
    }

    private static void SetPadding(SerializedObject so, string path, int left, int right, int top, int bottom)
    {
        var prop = Find(so, path);
        if (prop == null) return;
        prop.FindPropertyRelative("m_Left").intValue = left;
        prop.FindPropertyRelative("m_Right").intValue = right;
        prop.FindPropertyRelative("m_Top").intValue = top;
        prop.FindPropertyRelative("m_Bottom").intValue = bottom;
    }

    #endregion

    #region Scene Helpers

    private static bool TryFindTargets(out RecycleScroller scroller, out RecycleScrollerTest test)
    {
        test = Object.FindObjectOfType<RecycleScrollerTest>(true);
        if (test == null)
        {
            scroller = null;
            Debug.LogError("[RecycleScrollerTestPreset] 씬에서 RecycleScrollerTest를 찾지 못했습니다 — TestScene을 먼저 여세요");
            return false;
        }

        scroller = Object.FindObjectOfType<RecycleScroller>(true);
        if (scroller == null)
        {
            Debug.LogError("[RecycleScrollerTestPreset] 씬에서 RecycleScroller를 찾지 못했습니다");
            return false;
        }

        return true;
    }

    private static void MarkSceneDirty(params Object[] targets)
    {
        foreach (var target in targets)
        {
            if (target == null) continue;
            EditorUtility.SetDirty(target);
        }

        var component = targets[0] as Component;
        if (component == null) return;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
    }

    #endregion
}
