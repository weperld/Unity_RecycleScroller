using System.Collections.Generic;
using NUnit.Framework;
using RecycleScroll.Editor;
using UnityEditor;
using UnityEngine;

namespace RecycleScroll.Editor.Tests
{
    /// <summary>템플릿 설정 자체의 스펙 — 기본 명칭, 개수 제한, 언팩 모드 매핑.</summary>
    public class TemplateSettingsTests
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

        private TemplateEntry NewEntry(string menuName = null, bool withPrefab = true, string prefabName = "TemplatePrefabStub")
        {
            var entry = TemplateEntry.CreateNew();
            entry.m_menuName = menuName;

            if (withPrefab is false) return entry;

            var go = new GameObject(prefabName);
            m_created.Add(go);
            entry.m_prefab = go;

            return entry;
        }

        // --- 스펙 2: 명칭 지정 칸과 디폴트 명칭 ---

        [Test]
        public void 기본_명칭은_1부터_시작하는_두자리_번호를_쓴다()
        {
            Assert.AreEqual("RecycleScroller Template01 Create", RecycleScrollerTemplateSettings.GetDefaultMenuName(0));
            Assert.AreEqual("RecycleScroller Template09 Create", RecycleScrollerTemplateSettings.GetDefaultMenuName(8));
            Assert.AreEqual("RecycleScroller Template20 Create", RecycleScrollerTemplateSettings.GetDefaultMenuName(19));
        }

        [Test]
        public void 이름이_비어있으면_기본_명칭이_노출된다()
        {
            Assert.AreEqual("RecycleScroller Template03 Create", RecycleScrollerTemplateSettings.GetDisplayName(NewEntry(null), 2));
            Assert.AreEqual("RecycleScroller Template03 Create", RecycleScrollerTemplateSettings.GetDisplayName(NewEntry(""), 2));
            Assert.AreEqual("RecycleScroller Template03 Create", RecycleScrollerTemplateSettings.GetDisplayName(NewEntry("   "), 2));
        }

        [Test]
        public void 이름이_입력되면_앞뒤_공백을_떼고_그대로_쓴다()
        {
            Assert.AreEqual("My Cell Scroller", RecycleScrollerTemplateSettings.GetDisplayName(NewEntry("  My Cell Scroller  "), 0));
        }

        // --- 생성될 오브젝트 이름 ---

        [Test]
        public void 오브젝트_이름이_비어있으면_프리팹_이름을_쓴다()
        {
            var entry = NewEntry(prefabName: "My Scroll Prefab");

            Assert.AreEqual("My Scroll Prefab", RecycleScrollerTemplateSettings.GetObjectName(entry));

            entry.m_objectName = "";
            Assert.AreEqual("My Scroll Prefab", RecycleScrollerTemplateSettings.GetObjectName(entry));

            entry.m_objectName = "   ";
            Assert.AreEqual("My Scroll Prefab", RecycleScrollerTemplateSettings.GetObjectName(entry));
        }

        [Test]
        public void 오브젝트_이름이_입력되면_앞뒤_공백을_떼고_쓴다()
        {
            var entry = NewEntry(prefabName: "My Scroll Prefab");
            entry.m_objectName = "  Shop List  ";

            Assert.AreEqual("Shop List", RecycleScrollerTemplateSettings.GetObjectName(entry));
        }

        [Test]
        public void 프리팹이_없으면_오브젝트_이름은_빈_문자열이다()
        {
            Assert.AreEqual(string.Empty, RecycleScrollerTemplateSettings.GetObjectName(NewEntry(withPrefab: false)));
            Assert.AreEqual(string.Empty, RecycleScrollerTemplateSettings.GetObjectName(null));
        }

        [Test]
        public void 오브젝트_이름이_다르면_변경으로_감지된다()
        {
            var a = NewEntry("menu");
            var b = a.Clone();
            b.m_objectName = "Renamed";

            Assert.IsFalse(a.ValueEquals(b), "이 비교가 빠지면 이름만 바꿨을 때 '적용' 버튼이 활성화되지 않는다.");
        }

        [Test]
        public void 오브젝트_이름도_null과_빈_문자열을_같게_본다()
        {
            var a = NewEntry("menu");
            a.m_objectName = null;

            var b = a.Clone();
            b.m_objectName = "";

            Assert.IsTrue(a.ValueEquals(b));
        }

        // --- 스펙: 개수 제한 ---

        [Test]
        public void 템플릿은_최대_20개다()
        {
            Assert.AreEqual(20, RecycleScrollerTemplateSettings.MAX_TEMPLATES);
        }

        // --- 스펙 3: 언팩 설정 값 세 가지 ---

        [Test]
        public void 언팩_모드는_세_가지이며_Unity_모드로_정확히_매핑된다()
        {
            Assert.AreEqual(3, System.Enum.GetValues(typeof(TemplateUnpackMode)).Length);

            Assert.IsNull(RecycleScrollViewCreator.ToPrefabUnpackMode(TemplateUnpackMode.None),
                "None 은 언팩하지 않아야 하므로 null 이어야 한다.");
            Assert.AreEqual(PrefabUnpackMode.OutermostRoot, RecycleScrollViewCreator.ToPrefabUnpackMode(TemplateUnpackMode.OutermostRoot));
            Assert.AreEqual(PrefabUnpackMode.Completely, RecycleScrollViewCreator.ToPrefabUnpackMode(TemplateUnpackMode.Completely));
        }

        // --- 생성 위치 판정 ---

        [Test]
        public void UI_프리팹만_Canvas를_필요로_한다()
        {
            var ui = new GameObject("UIPrefabStub", typeof(RectTransform));
            m_created.Add(ui);

            var nonUi = new GameObject("PlainPrefabStub");
            m_created.Add(nonUi);

            Assert.IsTrue(RecycleScrollViewCreator.NeedsCanvas(ui));
            Assert.IsFalse(RecycleScrollViewCreator.NeedsCanvas(nonUi),
                "RectTransform 이 없는 프리팹까지 Canvas 로 넣으면 3D 오브젝트가 UI 계층에 끌려 들어간다.");
            Assert.IsFalse(RecycleScrollViewCreator.NeedsCanvas(null));
        }

        // --- 엔트리 값 비교 (하이라이트 판정의 토대) ---

        [Test]
        public void 값_비교는_id를_무시하고_설정_값만_본다()
        {
            var a = NewEntry("same");
            var b = a.Clone();
            b.m_id = "완전히-다른-id";

            Assert.IsTrue(a.ValueEquals(b));
        }

        [Test]
        public void null과_빈_문자열_이름은_같은_값으로_본다()
        {
            var a = NewEntry(null);
            var b = a.Clone();
            b.m_menuName = "";

            Assert.IsTrue(a.ValueEquals(b), "저장 후 null 이 빈 문자열로 바뀌어도 변경으로 표시되면 안 된다.");
        }

        [Test]
        public void 언팩_모드가_다르면_다른_값이다()
        {
            var a = NewEntry("x");
            var b = a.Clone();
            b.m_unpackMode = TemplateUnpackMode.Completely;

            Assert.IsFalse(a.ValueEquals(b));
        }

        [Test]
        public void 새_엔트리는_항상_고유한_id를_받는다()
        {
            var ids = new HashSet<string>();
            for (int i = 0; i < 50; i++)
            {
                var id = TemplateEntry.CreateNew().m_id;
                Assert.IsFalse(string.IsNullOrEmpty(id));
                Assert.IsTrue(ids.Add(id), "id 가 중복되면 변경 감지 매칭이 깨진다.");
            }
        }
    }
}
