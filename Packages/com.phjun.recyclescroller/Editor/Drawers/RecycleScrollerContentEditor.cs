using UnityEditor;
using RecycleScroll;
using UnityEngine;
using UnityEngine.UI;


[CustomEditor(typeof(RectTransform), true)]
[CanEditMultipleObjects]
public class RecycleScrollerContentEditor : Editor
{
    Editor m_defaultEditor;
    RectTransform m_rt;
    RecycleScroller m_rs;
    bool m_isRSContent = false;

    readonly float m_propTitleLabelWidth = 120f;
    readonly float m_fieldLabelWidth = 40f;
    readonly float m_fieldMinWidth = 40f;

    private void OnEnable()
    {
        m_defaultEditor = Editor.CreateEditor(targets, System.Type.GetType("UnityEditor.RectTransformEditor, UnityEditor"));
        m_rt = target as RectTransform;
        if (m_rt == null) return;

        m_rs = m_rt.GetComponentInParent<RecycleScroller>(true);
        m_isRSContent = m_rs != null && m_rs.Content == m_rt;
        if (m_isRSContent)
        {
            m_rs.ResetContent_Pivot();
            m_rs.ResetContent_Anchor();
        }
    }

    private void OnDisable()
    {
        DestroyImmediate(m_defaultEditor);
    }

    public override void OnInspectorGUI()
    {
        if (m_isRSContent == false)
        {
            m_defaultEditor.OnInspectorGUI();
            return;
        }

        var isVertical = m_rs.ScrollAxis == eScrollAxis.VERTICAL;
        var parentHaveLayoutGroup = m_rt.parent.TryGetComponent<LayoutGroup>(out var _);

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
            EditorGUILayout.LabelField("Anchored Position", GUILayout.Width(m_propTitleLabelWidth));

            EditorGUIUtility.labelWidth = m_fieldLabelWidth;

            var anchorPos = m_rt.anchoredPosition;

            // x 필드
            EditorGUI.BeginChangeCheck();
            anchorPos.x = DrawFloatField(parentHaveLayoutGroup, "Pos X", anchorPos.x, GUILayout.MinWidth(m_fieldMinWidth));
            if (EditorGUI.EndChangeCheck())
            {
                ApplyGUIChanged(
                    "Content Anchored Position(X) Change",
                    () =>
                    {
                        m_rt.anchoredPosition = anchorPos;
                    });
            }

            // y 필드
            EditorGUI.BeginChangeCheck();
            anchorPos.y = DrawFloatField(parentHaveLayoutGroup, "Pos Y", anchorPos.y, GUILayout.MinWidth(m_fieldMinWidth));
            if (EditorGUI.EndChangeCheck())
            {
                ApplyGUIChanged(
                    "Recycle Scroller Content Anchored Position(Y) Change",
                    () =>
                    {
                        m_rt.anchoredPosition = anchorPos;
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
                        m_rt.anchoredPosition = Vector2.zero;
                        m_rs._ScrollRect.velocity = Vector2.zero;
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
            EditorGUILayout.LabelField("Size", GUILayout.Width(m_propTitleLabelWidth));

            // LabelWidth를 조절하여 Width와 Height 필드의 너비를 동일하게 맞춥니다.
            EditorGUIUtility.labelWidth = m_fieldLabelWidth; // 레이블 너비 조정

            var sizeDelta = m_rt.sizeDelta;

            // Width 필드
            DrawFloatField(true, "Width", sizeDelta.x, GUILayout.MinWidth(m_fieldMinWidth));

            // Height 필드
            DrawFloatField(true, "Height", sizeDelta.y, GUILayout.MinWidth(m_fieldMinWidth));

            GUILayout.Space(5f);
            string btnStr = isVertical ? "Width" : "Height";
            if (GUILayout.Button($"Fit To Viewport {btnStr}"))
            {
                var viewSize = isVertical ? m_rs.Viewport.rect.width : m_rs.Viewport.rect.height;
                if (isVertical) sizeDelta.x = viewSize;
                else sizeDelta.y = viewSize;

                ApplyGUIChanged(
                    "Recycle Scroller Content Size Change",
                    () =>
                    {
                        m_rt.sizeDelta = sizeDelta;
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

        Undo.RecordObject(m_rt, recordName);
        changeAction?.Invoke();
        EditorUtility.SetDirty(target);
    }
}