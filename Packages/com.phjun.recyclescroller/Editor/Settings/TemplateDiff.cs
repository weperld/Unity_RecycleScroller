using System;
using System.Collections.Generic;

namespace RecycleScroll.Editor
{
    internal enum TemplateDiffState
    {
        Unchanged,
        Added,
        Modified,
    }

    /// <summary>
    /// 저장본과 편집 중인 작업 복사본을 비교하는 순수 로직.
    /// 드래그 정렬 때문에 인덱스가 아니라 m_id 로 매칭한다.
    /// </summary>
    internal static class TemplateDiff
    {
        public static TemplateEntry FindById(IReadOnlyList<TemplateEntry> list, string id)
        {
            if (list is null || string.IsNullOrEmpty(id)) return null;

            foreach (var entry in list)
            {
                if (entry is null) continue;
                if (string.Equals(entry.m_id, id, StringComparison.Ordinal)) return entry;
            }

            return null;
        }

        public static TemplateDiffState GetState(TemplateEntry working, IReadOnlyList<TemplateEntry> saved)
        {
            if (working is null) return TemplateDiffState.Unchanged;

            var match = FindById(saved, working.m_id);
            if (match is null) return TemplateDiffState.Added;
            if (working.ValueEquals(match) is false) return TemplateDiffState.Modified;

            return TemplateDiffState.Unchanged;
        }

        /// <summary>저장본에는 있는데 작업 복사본에서 빠진 항목 수.</summary>
        public static int CountDeleted(IReadOnlyList<TemplateEntry> saved, IReadOnlyList<TemplateEntry> working)
        {
            var count = 0;
            foreach (var entry in saved)
            {
                if (entry is null) continue;
                if (FindById(working, entry.m_id) != null) continue;

                count++;
            }

            return count;
        }

        public static int CountMissingPrefab(IReadOnlyList<TemplateEntry> working)
        {
            var count = 0;
            foreach (var entry in working)
            {
                if (entry?.m_prefab != null) continue;

                count++;
            }

            return count;
        }

        /// <summary>
        /// 양쪽에 모두 남아 있는 항목들의 상대 순서만 비교한다.
        /// 추가/삭제로 인한 위치 변화는 순서 변경으로 보지 않는다.
        /// </summary>
        public static bool IsOrderChanged(IReadOnlyList<TemplateEntry> saved, IReadOnlyList<TemplateEntry> working)
        {
            var savedOrder = CollectCommonIds(saved, working);
            var workingOrder = CollectCommonIds(working, saved);

            if (savedOrder.Count != workingOrder.Count) return false;

            for (int i = 0; i < savedOrder.Count; i++)
            {
                if (string.Equals(savedOrder[i], workingOrder[i], StringComparison.Ordinal) is false) return true;
            }

            return false;
        }

        private static List<string> CollectCommonIds(IReadOnlyList<TemplateEntry> source, IReadOnlyList<TemplateEntry> other)
        {
            var ids = new List<string>();
            foreach (var entry in source)
            {
                if (entry is null) continue;
                if (FindById(other, entry.m_id) is null) continue;

                ids.Add(entry.m_id);
            }

            return ids;
        }

        /// <summary>저장이 필요한 변경(값/추가/삭제/순서)이 하나라도 있는지.</summary>
        public static bool IsDirty(IReadOnlyList<TemplateEntry> saved, IReadOnlyList<TemplateEntry> working)
        {
            if (saved.Count != working.Count) return true;

            for (int i = 0; i < saved.Count; i++)
            {
                if (string.Equals(saved[i].m_id, working[i].m_id, StringComparison.Ordinal) is false) return true;
                if (saved[i].ValueEquals(working[i]) is false) return true;
            }

            return false;
        }

        /// <summary>메뉴에 실제로 노출될 이름 기준으로 중복을 찾는다. 없으면 null.</summary>
        public static string FindDuplicatedName(IReadOnlyList<TemplateEntry> working)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < working.Count; i++)
            {
                var name = RecycleScrollerTemplateSettings.GetDisplayName(working[i], i);
                if (seen.Add(name) is false) return name;
            }

            return null;
        }
    }
}
