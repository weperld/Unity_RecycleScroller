using System.Collections.Generic;
using NUnit.Framework;
using RecycleScroll.Editor;
using UnityEngine;

namespace RecycleScroll.Editor.Tests
{
    /// <summary>미저장 변경 하이라이트 판정 — 저장본 대비 추가/변경/삭제/순서.</summary>
    public class TemplateDiffTests
    {
        private readonly List<GameObject> m_created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in m_created)
            {
                if (go == null) continue;

                Object.DestroyImmediate(go);
            }

            m_created.Clear();
        }

        private TemplateEntry NewEntry(string id, string menuName = "name", bool withPrefab = true)
        {
            var entry = TemplateEntry.CreateNew();
            entry.m_id = id;
            entry.m_menuName = menuName;

            if (withPrefab is false) return entry;

            var go = new GameObject("TemplatePrefabStub");
            m_created.Add(go);
            entry.m_prefab = go;

            return entry;
        }

        private static List<TemplateEntry> Clone(IReadOnlyList<TemplateEntry> source)
        {
            var copy = new List<TemplateEntry>();
            foreach (var entry in source) copy.Add(entry.Clone());

            return copy;
        }

        // --- 상태 판정 ---

        [Test]
        public void 저장본에_없는_항목은_추가로_본다()
        {
            var saved = new List<TemplateEntry> { NewEntry("a") };
            var added = NewEntry("b");

            Assert.AreEqual(TemplateDiffState.Added, TemplateDiff.GetState(added, saved));
        }

        [Test]
        public void 값이_같으면_변경으로_보지_않는다()
        {
            var saved = new List<TemplateEntry> { NewEntry("a") };
            var working = Clone(saved);

            Assert.AreEqual(TemplateDiffState.Unchanged, TemplateDiff.GetState(working[0], saved));
        }

        [Test]
        public void 값이_바뀌면_변경으로_본다()
        {
            var saved = new List<TemplateEntry> { NewEntry("a", "before") };
            var working = Clone(saved);
            working[0].m_menuName = "after";

            Assert.AreEqual(TemplateDiffState.Modified, TemplateDiff.GetState(working[0], saved));
        }

        [Test]
        public void 순서만_바뀐_항목은_변경으로_표시하지_않는다()
        {
            var saved = new List<TemplateEntry> { NewEntry("a"), NewEntry("b"), NewEntry("c") };
            var working = Clone(saved);

            var first = working[0];
            working.RemoveAt(0);
            working.Add(first);

            foreach (var entry in working)
            {
                Assert.AreEqual(TemplateDiffState.Unchanged, TemplateDiff.GetState(entry, saved),
                    "값이 그대로인데 노란 바가 뜨면 무엇이 바뀐 건지 알 수 없다.");
            }
        }

        [Test]
        public void 중간_항목을_지워도_뒤_항목이_변경으로_오탐되지_않는다()
        {
            var saved = new List<TemplateEntry> { NewEntry("a"), NewEntry("b"), NewEntry("c") };
            var working = Clone(saved);
            working.RemoveAt(1);

            foreach (var entry in working)
            {
                Assert.AreEqual(TemplateDiffState.Unchanged, TemplateDiff.GetState(entry, saved),
                    "인덱스가 아니라 id 로 매칭해야 오탐이 없다.");
            }
        }

        // --- 집계 ---

        [Test]
        public void 삭제_예정_개수를_센다()
        {
            var saved = new List<TemplateEntry> { NewEntry("a"), NewEntry("b"), NewEntry("c") };
            var working = Clone(saved);
            working.RemoveAt(0);
            working.RemoveAt(0);

            Assert.AreEqual(2, TemplateDiff.CountDeleted(saved, working));
        }

        [Test]
        public void 프리팹_미지정_개수를_센다()
        {
            var working = new List<TemplateEntry> { NewEntry("a"), NewEntry("b", withPrefab: false) };

            Assert.AreEqual(1, TemplateDiff.CountMissingPrefab(working));
        }

        // --- 순서 변경 ---

        [Test]
        public void 순서를_바꾸면_순서_변경으로_감지한다()
        {
            var saved = new List<TemplateEntry> { NewEntry("a"), NewEntry("b"), NewEntry("c") };
            var working = Clone(saved);

            var first = working[0];
            working.RemoveAt(0);
            working.Add(first);

            Assert.IsTrue(TemplateDiff.IsOrderChanged(saved, working));
        }

        [Test]
        public void 항목_추가나_삭제만으로는_순서_변경이_아니다()
        {
            var saved = new List<TemplateEntry> { NewEntry("a"), NewEntry("b") };

            var appended = Clone(saved);
            appended.Add(NewEntry("c"));
            Assert.IsFalse(TemplateDiff.IsOrderChanged(saved, appended));

            var removed = Clone(saved);
            removed.RemoveAt(0);
            Assert.IsFalse(TemplateDiff.IsOrderChanged(saved, removed));
        }

        // --- 저장 필요 여부 ---

        [Test]
        public void 변경이_없으면_저장이_필요하지_않다()
        {
            var saved = new List<TemplateEntry> { NewEntry("a"), NewEntry("b") };

            Assert.IsFalse(TemplateDiff.IsDirty(saved, Clone(saved)));
        }

        [Test]
        public void 값_추가_삭제_순서_변경은_모두_저장이_필요하다()
        {
            var saved = new List<TemplateEntry> { NewEntry("a", "one"), NewEntry("b", "two") };

            var valueChanged = Clone(saved);
            valueChanged[0].m_menuName = "changed";
            Assert.IsTrue(TemplateDiff.IsDirty(saved, valueChanged), "값 변경");

            var appended = Clone(saved);
            appended.Add(NewEntry("c"));
            Assert.IsTrue(TemplateDiff.IsDirty(saved, appended), "추가");

            var removed = Clone(saved);
            removed.RemoveAt(0);
            Assert.IsTrue(TemplateDiff.IsDirty(saved, removed), "삭제");

            var reordered = Clone(saved);
            reordered.Reverse();
            Assert.IsTrue(TemplateDiff.IsDirty(saved, reordered), "순서 변경");
        }

        // --- 이름 중복 ---

        [Test]
        public void 같은_메뉴_이름을_찾아낸다()
        {
            var working = new List<TemplateEntry> { NewEntry("a", "같은이름"), NewEntry("b", "다른이름"), NewEntry("c", "같은이름") };

            Assert.AreEqual("같은이름", TemplateDiff.FindDuplicatedName(working));
        }

        [Test]
        public void 이름을_비워두면_번호가_달라_중복되지_않는다()
        {
            var working = new List<TemplateEntry> { NewEntry("a", ""), NewEntry("b", ""), NewEntry("c", "") };

            Assert.IsNull(TemplateDiff.FindDuplicatedName(working));
        }

        [Test]
        public void 입력한_이름이_다른_항목의_기본_명칭과_겹치면_중복이다()
        {
            var working = new List<TemplateEntry>
            {
                NewEntry("a", ""),                                    // -> RecycleScroller Template01 Create
                NewEntry("b", "RecycleScroller Template01 Create"),
            };

            Assert.AreEqual("RecycleScroller Template01 Create", TemplateDiff.FindDuplicatedName(working));
        }
    }
}
