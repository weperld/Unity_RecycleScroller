using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RecycleScroll.Editor
{
    /// <summary>
    /// 템플릿 설정 그리기. 설정 창을 Project Settings 탭으로 두든 별도 윈도우로 두든
    /// 이 DrawGUI() 하나만 호출하면 되도록 분리해 두었다.
    /// </summary>
    internal static class RecycleScrollerTemplateSettingsUI
    {
        private const float ROW_LINES = 4f;
        private const float DIFF_BAR_WIDTH = 4f;

        private static List<TemplateEntry> s_working;
        private static ReorderableList s_list;
        private static GUIStyle s_placeholderStyle;

        private static RecycleScrollerTemplateSettings Settings => RecycleScrollerTemplateSettings.instance;

        private static Color AddedColor => EditorGUIUtility.isProSkin
            ? new Color(0.35f, 0.78f, 0.42f)
            : new Color(0.18f, 0.60f, 0.26f);

        private static Color ModifiedColor => EditorGUIUtility.isProSkin
            ? new Color(0.92f, 0.75f, 0.25f)
            : new Color(0.80f, 0.58f, 0.05f);

        private static GUIStyle PlaceholderStyle
        {
            get
            {
                if (s_placeholderStyle == null)
                {
                    s_placeholderStyle = new GUIStyle(EditorStyles.label);
                    s_placeholderStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                    s_placeholderStyle.fontStyle = FontStyle.Italic;
                }

                return s_placeholderStyle;
            }
        }

        internal static void DrawGUI()
        {
            EnsureInitialized();

            EditorGUILayout.HelpBox(
                "여기에 등록한 프리팹이 Hierarchy 우클릭 > UI > RecycleScroll 메뉴에 생성 항목으로 추가됩니다.\n" +
                "'적용'을 눌러야 저장되며, 저장 시 메뉴 코드가 다시 생성되어 컴파일이 한 번 발생합니다.",
                MessageType.Info);

            EditorGUILayout.Space(4f);

            DrawWarnings();
            s_list.DoLayoutList();
            DrawFooter();
        }

        /// <summary>설정 창을 벗어났다가 돌아와도 편집 중이던 내용이 유지된다.</summary>
        private static void EnsureInitialized()
        {
            if (s_working != null && s_list != null) return;

            s_working = Settings.CreateWorkingCopy();
            s_list = CreateList();
        }

        private static ReorderableList CreateList()
        {
            var list = new ReorderableList(s_working, typeof(TemplateEntry), true, true, true, true);

            list.drawHeaderCallback = rect
                => EditorGUI.LabelField(rect, $"템플릿 ({s_working.Count} / {RecycleScrollerTemplateSettings.MAX_TEMPLATES})");

            list.elementHeightCallback = _
                => (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * ROW_LINES + 6f;

            list.drawElementCallback = DrawElement;
            list.drawElementBackgroundCallback = DrawElementBackground;

            list.onCanAddCallback = _ => s_working.Count < RecycleScrollerTemplateSettings.MAX_TEMPLATES;
            list.onAddCallback = _ => s_working.Add(TemplateEntry.CreateNew());

            return list;
        }

        private static void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= s_working.Count) return;

            var entry = s_working[index];
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var step = lineHeight + EditorGUIUtility.standardVerticalSpacing;
            var row = new Rect(rect.x + DIFF_BAR_WIDTH + 4f, rect.y + 3f, rect.width - DIFF_BAR_WIDTH - 8f, lineHeight);

            entry.m_prefab = EditorGUI.ObjectField(row, "프리팹", entry.m_prefab, typeof(GameObject), false) as GameObject;

            row.y += step;
            entry.m_menuName = DrawNameField(row, "메뉴 이름", entry.m_menuName,
                RecycleScrollerTemplateSettings.GetDefaultMenuName(index));

            row.y += step;
            // 프리팹이 없으면 안내할 기본 이름도 없으므로 플레이스홀더를 비운다.
            entry.m_objectName = DrawNameField(row, "오브젝트 이름", entry.m_objectName,
                entry.m_prefab == null ? null : entry.m_prefab.name);

            row.y += step;
            entry.m_unpackMode = (TemplateUnpackMode)EditorGUI.EnumPopup(row, "생성 방식", entry.m_unpackMode);
        }

        /// <summary>비어 있을 때 회색 안내 문구가 겹쳐 보이는 텍스트 필드.</summary>
        private static string DrawNameField(Rect row, string label, string value, string placeholder)
        {
            var fieldRect = EditorGUI.PrefixLabel(row, new GUIContent(label));
            var result = EditorGUI.TextField(fieldRect, value);

            if (string.IsNullOrWhiteSpace(result) is false) return result;
            if (string.IsNullOrEmpty(placeholder)) return result;

            // IMGUI에 플레이스홀더가 없어서 빈 칸 위에 겹쳐 그린다.
            var hintRect = new Rect(fieldRect.x + 2f, fieldRect.y, fieldRect.width - 4f, fieldRect.height);
            EditorGUI.LabelField(hintRect, placeholder, PlaceholderStyle);

            return result;
        }

        private static void DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= s_working.Count) return;

            // defaultBehaviours 는 리스트가 처음 그려질 때 초기화된다.
            if (ReorderableList.defaultBehaviours != null)
                ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, isActive, isFocused, true);

            var diffColor = GetDiffColor(s_working[index]);
            if (diffColor.HasValue is false) return;

            EditorGUI.DrawRect(new Rect(rect.x, rect.y, DIFF_BAR_WIDTH, rect.height), diffColor.Value);
        }

        private static Color? GetDiffColor(TemplateEntry entry)
        {
            switch (TemplateDiff.GetState(entry, Settings.Templates))
            {
                case TemplateDiffState.Added: return AddedColor;
                case TemplateDiffState.Modified: return ModifiedColor;
                default: return null;
            }
        }

        private static void DrawWarnings()
        {
            var saved = Settings.Templates;

            var deletedCount = TemplateDiff.CountDeleted(saved, s_working);
            if (deletedCount > 0)
                EditorGUILayout.HelpBox($"삭제 예정 {deletedCount}개. '되돌리기'를 누르면 복구됩니다.", MessageType.Warning);

            if (TemplateDiff.IsOrderChanged(saved, s_working))
                EditorGUILayout.HelpBox("순서가 변경되었습니다. 적용하면 메뉴 표시 순서가 바뀝니다.", MessageType.Info);

            var missingPrefabCount = TemplateDiff.CountMissingPrefab(s_working);
            if (missingPrefabCount > 0)
                EditorGUILayout.HelpBox($"프리팹이 지정되지 않은 항목 {missingPrefabCount}개는 메뉴로 생성되지 않습니다.", MessageType.Warning);

            var duplicated = TemplateDiff.FindDuplicatedName(s_working);
            if (string.IsNullOrEmpty(duplicated) is false)
                EditorGUILayout.HelpBox($"메뉴 이름 '{duplicated}' 이(가) 중복됩니다. 같은 이름은 메뉴에서 하나로 합쳐집니다.", MessageType.Warning);

            if (s_working.Count >= RecycleScrollerTemplateSettings.MAX_TEMPLATES)
                EditorGUILayout.HelpBox($"템플릿은 최대 {RecycleScrollerTemplateSettings.MAX_TEMPLATES}개까지 등록할 수 있습니다.", MessageType.Info);
        }

        private static void DrawFooter()
        {
            var dirty = TemplateDiff.IsDirty(Settings.Templates, s_working);

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(dirty is false))
                {
                    if (GUILayout.Button("되돌리기", GUILayout.Width(100f))) Revert();
                    if (GUILayout.Button("적용", GUILayout.Width(100f))) Apply();
                }
            }

            if (dirty is false) return;

            EditorGUILayout.HelpBox("저장되지 않은 변경 사항이 있습니다.", MessageType.None);
        }

        private static void Revert()
        {
            GUI.FocusControl(null);

            // ReorderableList가 s_working 인스턴스를 붙들고 있으므로 내용만 교체한다.
            s_working.Clear();
            s_working.AddRange(Settings.CreateWorkingCopy());
        }

        private static void Apply()
        {
            GUI.FocusControl(null);
            Settings.Apply(s_working);
            Revert();
        }
    }

    /// <summary>템플릿 설정 전용 창. 그리기는 전부 RecycleScrollerTemplateSettingsUI 가 한다.</summary>
    internal class RecycleScrollerTemplateSettingsWindow : EditorWindow
    {
        private Vector2 m_scrollPosition;

        [MenuItem(RecycleScrollerTemplateSettings.SETTINGS_MENU_PATH)]
        private static void Open()
        {
            var window = GetWindow<RecycleScrollerTemplateSettingsWindow>();
            window.titleContent = new GUIContent("RecycleScroller 템플릿");
            window.minSize = new Vector2(460f, 240f);
            window.Show();
        }

        private void OnGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(m_scrollPosition))
            {
                m_scrollPosition = scroll.scrollPosition;

                // Project Settings 와 달리 창 자체 여백이 없어서 직접 준다.
                EditorGUILayout.Space(6f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(8f);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        RecycleScrollerTemplateSettingsUI.DrawGUI();
                    }
                    GUILayout.Space(8f);
                }
                EditorGUILayout.Space(6f);
            }
        }
    }
}
