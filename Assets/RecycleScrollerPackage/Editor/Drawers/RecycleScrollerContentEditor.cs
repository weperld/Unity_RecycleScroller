using UnityEditor;
using RecycleScroll;
using UnityEngine;
using UnityEngine.UI;


[CustomEditor(typeof(RectTransform), true)]
[CanEditMultipleObjects]
public class RecycleScrollerContentEditor : Editor
{
    Editor defaultEditor;
    RectTransform rt;
    RecycleScroller rs;
    bool is_RS_Content = false;

    readonly float propTitleLabelWidth = 120f;
    readonly float fieldLabelWidth = 40f;
    readonly float fieldMinWidth = 40f;

    private void OnEnable()
    {
        defaultEditor = Editor.CreateEditor(targets, System.Type.GetType("UnityEditor.RectTransformEditor, UnityEditor"));
        rt = target as RectTransform;
        if (rt == null) return;

        rs = rt.GetComponentInParent<RecycleScroller>(true);
        is_RS_Content = rs != null && rs.Content == rt;
        if (is_RS_Content)
        {
            rs.ResetContent_Pivot();
            rs.ResetContent_Anchor();
        }
    }

    private void OnDisable()
    {
        DestroyImmediate(defaultEditor);
    }

    public override void OnInspectorGUI()
    {
        if (is_RS_Content == false)
        {
            defaultEditor.OnInspectorGUI();
            return;
        }

        var isVertical = rs.ScrollAxis == eScrollAxis.VERTICAL;
        var parentHaveLayoutGroup = rt.parent.TryGetComponent<LayoutGroup>(out var _);

        // Draw RecycleScrollContent RectTransform Inspector
        GUILayout.Space(2f);
        EditorGUILayout.LabelField("Recycle Scroller Content RectTransform", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField($"Current Scroll Axis: {(isVertical ? "Vertical" : "Horizontal")}");
        if (parentHaveLayoutGroup) EditorGUILayout.LabelField("**Detected Layout Component Of Parent**");
        EditorGUI.indentLevel--;
        GUILayout.Space(15f);

        #region Draw Anchored Position

        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Anchored Position", GUILayout.Width(propTitleLabelWidth));

            EditorGUIUtility.labelWidth = fieldLabelWidth;

            var anchorPos = rt.anchoredPosition;

            // x 필드
            EditorGUI.BeginChangeCheck();
            anchorPos.x = DrawFloatField(parentHaveLayoutGroup, "Pos X", anchorPos.x, GUILayout.MinWidth(fieldMinWidth));
            if (EditorGUI.EndChangeCheck())
            {
                ApplyGUIChanged(
                    "Content Anchored Position(X) Change",
                    () =>
                    {
                        rt.anchoredPosition = anchorPos;
                    });
            }

            // y 필드
            EditorGUI.BeginChangeCheck();
            anchorPos.y = DrawFloatField(parentHaveLayoutGroup, "Pos Y", anchorPos.y, GUILayout.MinWidth(fieldMinWidth));
            if (EditorGUI.EndChangeCheck())
            {
                ApplyGUIChanged(
                    "Recycle Scroller Content Anchored Position(Y) Change",
                    () =>
                    {
                        rt.anchoredPosition = anchorPos;
                    });
            }

            GUI.enabled = !parentHaveLayoutGroup;
            GUILayout.Space(5f);
            string btnStr = parentHaveLayoutGroup ? "Layout Detected" : "Set To Origin";
            if (GUILayout.Button(btnStr))
            {
                ApplyGUIChanged(
                    "Recycle Scroller Content Anchored Position(X, Y) Change",
                    () =>
                    {
                        rt.anchoredPosition = Vector2.zero;
                        rs._ScrollRect.velocity = Vector2.zero;
                    });
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0f;
        }

        #endregion

        GUILayout.Space(3f);

        #region Draw Size

        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Size", GUILayout.Width(propTitleLabelWidth));

            // LabelWidth를 조절하여 Width와 Height 필드의 너비를 동일하게 맞춥니다.
            EditorGUIUtility.labelWidth = fieldLabelWidth; // 레이블 너비 조정

            var sizeDelta = rt.sizeDelta;

            // Width 필드
            DrawFloatField(true, "Width", sizeDelta.x, GUILayout.MinWidth(fieldMinWidth));

            // Height 필드
            DrawFloatField(true, "Height", sizeDelta.y, GUILayout.MinWidth(fieldMinWidth));

            GUILayout.Space(5f);
            string btnStr = isVertical ? "Width" : "Height";
            if (GUILayout.Button($"Fit To Viewport {btnStr}"))
            {
                var viewSize = isVertical ? rs.Viewport.rect.width : rs.Viewport.rect.height;
                if (isVertical) sizeDelta.x = viewSize;
                else sizeDelta.y = viewSize;

                ApplyGUIChanged(
                    "Recycle Scroller Content Size Change",
                    () =>
                    {
                        rt.sizeDelta = sizeDelta;
                    });
            }

            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0;
        }

        #endregion
    }

    private float DrawFloatField(bool disable, string label, float value, params GUILayoutOption[] options)
    {
        EditorGUI.BeginDisabledGroup(disable);
        float ret = EditorGUILayout.FloatField(label, value, options);
        EditorGUI.EndDisabledGroup();

        return ret;
    }

    private void ApplyGUIChanged(string recordName, System.Action changeAction)
    {
        if (GUI.changed == false) return;

        Undo.RecordObject(rt, recordName);
        changeAction?.Invoke();
        EditorUtility.SetDirty(target);
    }
}