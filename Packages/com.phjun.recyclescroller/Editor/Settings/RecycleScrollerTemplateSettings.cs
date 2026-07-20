using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RecycleScroll.Editor
{
    /// <summary>오브젝트 생성 시 프리팹 인스턴스를 어떻게 풀어놓을지.</summary>
    internal enum TemplateUnpackMode
    {
        [InspectorName("Unpack 안 함 (Nested 유지)")]
        None = 0,

        [InspectorName("최상위만 Unpack")]
        OutermostRoot = 1,

        [InspectorName("완전히 Unpack")]
        Completely = 2,
    }

    [Serializable]
    internal class TemplateEntry
    {
        /// <summary>변경 감지 매칭 전용. UI에 노출하지 않는다.</summary>
        public string m_id;
        public GameObject m_prefab;
        public string m_menuName;

        /// <summary>생성될 GameObject 이름. 비어 있으면 프리팹 이름을 쓴다.</summary>
        public string m_objectName;

        public TemplateUnpackMode m_unpackMode;

        public static TemplateEntry CreateNew()
            => new TemplateEntry
            {
                m_id = Guid.NewGuid().ToString("N"),
                m_menuName = string.Empty,
                m_objectName = string.Empty,
            };

        public TemplateEntry Clone()
            => new TemplateEntry
            {
                m_id = m_id,
                m_prefab = m_prefab,
                m_menuName = m_menuName,
                m_objectName = m_objectName,
                m_unpackMode = m_unpackMode,
            };

        /// <summary>id를 제외한 실제 설정 값이 같은지.</summary>
        public bool ValueEquals(TemplateEntry other)
        {
            if (other is null) return false;

            return m_prefab == other.m_prefab
                && NameEquals(m_menuName, other.m_menuName)
                && NameEquals(m_objectName, other.m_objectName)
                && m_unpackMode == other.m_unpackMode;
        }

        /// <summary>저장 왕복으로 null 과 "" 가 뒤바뀌어도 변경으로 보지 않는다.</summary>
        private static bool NameEquals(string a, string b)
            => string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.Ordinal);
    }

    [FilePath("ProjectSettings/RecycleScrollerTemplates.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class RecycleScrollerTemplateSettings : ScriptableSingleton<RecycleScrollerTemplateSettings>
    {
        public const int MAX_TEMPLATES = 20;

        /// <summary>설정 창 메뉴 경로. MenuItem 등록과 안내 문구가 함께 쓴다.</summary>
        public const string SETTINGS_MENU_PATH = "Tools/RecycleScroller/템플릿 설정";

        [SerializeField] private List<TemplateEntry> m_templates = new List<TemplateEntry>();

        public IReadOnlyList<TemplateEntry> Templates => m_templates;

        /// <summary>메뉴 이름이 비어 있을 때 사용할 기본 명칭. 메뉴 이름이므로 영문이다.</summary>
        public static string GetDefaultMenuName(int index) => $"RecycleScroller Template{index + 1:00} Create";

        /// <summary>메뉴에 실제로 노출될 이름. UI 플레이스홀더와 코드 생성이 함께 쓴다.</summary>
        public static string GetDisplayName(TemplateEntry entry, int index)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.m_menuName)) return GetDefaultMenuName(index);

            return entry.m_menuName.Trim();
        }

        /// <summary>
        /// 생성될 GameObject 이름. 비어 있으면 프리팹 이름을 쓴다.
        /// (프리팹을 Hierarchy 로 끌어놓았을 때의 Unity 기본 동작과 같다.)
        /// UI 플레이스홀더와 실제 생성이 함께 쓴다.
        /// </summary>
        public static string GetObjectName(TemplateEntry entry)
        {
            if (entry is null) return string.Empty;
            if (string.IsNullOrWhiteSpace(entry.m_objectName) is false) return entry.m_objectName.Trim();
            if (entry.m_prefab == null) return string.Empty;

            return entry.m_prefab.name;
        }

        public List<TemplateEntry> CreateWorkingCopy()
        {
            var copy = new List<TemplateEntry>(m_templates.Count);
            foreach (var entry in m_templates)
            {
                if (entry is null) continue;

                copy.Add(entry.Clone());
            }

            return copy;
        }

        /// <summary>작업 복사본을 확정 저장하고 메뉴 코드를 재생성한다.</summary>
        public void Apply(IReadOnlyList<TemplateEntry> working)
        {
            m_templates = new List<TemplateEntry>(working.Count);
            foreach (var entry in working)
            {
                if (entry is null) continue;
                if (string.IsNullOrEmpty(entry.m_id)) entry.m_id = Guid.NewGuid().ToString("N");

                m_templates.Add(entry.Clone());
            }

            Save(true);
            RecycleScrollerTemplateMenuGenerator.Generate(m_templates);
        }
    }
}
