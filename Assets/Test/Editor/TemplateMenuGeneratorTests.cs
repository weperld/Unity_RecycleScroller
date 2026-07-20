using System.Collections.Generic;
using NUnit.Framework;
using RecycleScroll.Editor;
using UnityEngine;

namespace RecycleScroll.Editor.Tests
{
    /// <summary>
    /// 생성되는 메뉴 코드 검증. 여기서 깨지면 생성된 .cs 가 컴파일되지 않아
    /// 프로젝트 전체 컴파일이 멈추므로 가장 방어가 필요한 지점이다.
    /// </summary>
    public class TemplateMenuGeneratorTests
    {
        private const string MENU_PATH = "GameObject/UI/RecycleScroll/";

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

        private TemplateEntry NewEntry(string menuName = null, bool withPrefab = true)
        {
            var entry = TemplateEntry.CreateNew();
            entry.m_menuName = menuName;

            if (withPrefab is false) return entry;

            var go = new GameObject("TemplatePrefabStub");
            m_created.Add(go);
            entry.m_prefab = go;

            return entry;
        }

        // --- 이스케이프: 실패하면 생성 코드가 컴파일되지 않는다 ---

        [Test]
        public void 따옴표와_역슬래시는_제거된다()
        {
            Assert.AreEqual("My Cell", RecycleScrollerTemplateMenuGenerator.EscapeForLiteral("My \"Cell\""));
            Assert.AreEqual("MyCell", RecycleScrollerTemplateMenuGenerator.EscapeForLiteral("My\\Cell"));
        }

        [Test]
        public void 슬래시는_서브메뉴_표기라_남긴다()
        {
            Assert.AreEqual("Shop/List", RecycleScrollerTemplateMenuGenerator.EscapeForLiteral("Shop/List"));
        }

        [Test]
        public void 이름에_따옴표를_넣어도_생성_코드의_리터럴이_깨지지_않는다()
        {
            var code = RecycleScrollerTemplateMenuGenerator.BuildCode(new List<TemplateEntry> { NewEntry("aaa\"); bbb //") });

            Assert.IsNotNull(code);
            // MenuItem 인자로 열고 닫는 따옴표 2개 외의 따옴표가 남아 있으면 컴파일이 깨진다.
            Assert.AreEqual(2, code.Split('"').Length - 1, "생성 코드에 따옴표가 정확히 2개여야 한다.\n" + code);
            Assert.IsFalse(code.Contains("\\"), "생성 코드에 역슬래시가 남으면 안 된다.\n" + code);
        }

        // --- 생성 대상 선별 ---

        [Test]
        public void 유효한_템플릿이_없으면_코드를_만들지_않는다()
        {
            Assert.IsNull(RecycleScrollerTemplateMenuGenerator.BuildCode(new List<TemplateEntry>()));
        }

        [Test]
        public void 프리팹이_없는_항목은_메뉴로_만들지_않는다()
        {
            var templates = new List<TemplateEntry> { NewEntry("있음"), NewEntry("없음", withPrefab: false) };

            var code = RecycleScrollerTemplateMenuGenerator.BuildCode(templates);

            Assert.IsTrue(code.Contains(MENU_PATH + "있음"));
            Assert.IsFalse(code.Contains(MENU_PATH + "없음"));
        }

        [Test]
        public void 프리팹이_하나도_없으면_코드를_만들지_않는다()
        {
            var templates = new List<TemplateEntry> { NewEntry("a", withPrefab: false) };

            Assert.IsNull(RecycleScrollerTemplateMenuGenerator.BuildCode(templates));
        }

        // --- 인덱스와 우선순위 ---

        [Test]
        public void 호출_인덱스는_건너뛴_항목이_있어도_원본_위치를_가리킨다()
        {
            var templates = new List<TemplateEntry>
            {
                NewEntry("첫번째", withPrefab: false),   // index 0 — 건너뜀
                NewEntry("두번째"),                       // index 1
            };

            var code = RecycleScrollerTemplateMenuGenerator.BuildCode(templates);

            Assert.IsTrue(code.Contains("CreateFromTemplate(command, 1)"),
                "건너뛴 항목 때문에 인덱스가 밀리면 엉뚱한 템플릿이 생성된다.\n" + code);
            Assert.IsFalse(code.Contains("CreateFromTemplate(command, 0)"));
        }

        [Test]
        public void 우선순위는_목록_순서대로_1011부터_1씩_증가한다()
        {
            var templates = new List<TemplateEntry> { NewEntry("a"), NewEntry("b"), NewEntry("c") };

            var code = RecycleScrollerTemplateMenuGenerator.BuildCode(templates);

            Assert.IsTrue(code.Contains(MENU_PATH + "a\", false, 1011)"), code);
            Assert.IsTrue(code.Contains(MENU_PATH + "b\", false, 1012)"), code);
            Assert.IsTrue(code.Contains(MENU_PATH + "c\", false, 1013)"), code);
        }

        [Test]
        public void 건너뛴_항목이_있어도_우선순위는_연속이다()
        {
            var templates = new List<TemplateEntry>
            {
                NewEntry("a"),
                NewEntry("skip", withPrefab: false),
                NewEntry("b"),
            };

            var code = RecycleScrollerTemplateMenuGenerator.BuildCode(templates);

            Assert.IsTrue(code.Contains(MENU_PATH + "a\", false, 1011)"), code);
            Assert.IsTrue(code.Contains(MENU_PATH + "b\", false, 1012)"),
                "우선순위가 끊기면 Unity가 메뉴 사이에 구분선을 넣는다.\n" + code);
        }

        [Test]
        public void 이름이_비어있으면_기본_명칭으로_메뉴를_만든다()
        {
            var code = RecycleScrollerTemplateMenuGenerator.BuildCode(new List<TemplateEntry> { NewEntry(null) });

            Assert.IsTrue(code.Contains(MENU_PATH + "RecycleScroller Template01 Create"), code);
        }

        [Test]
        public void 메서드_이름은_항목마다_겹치지_않는다()
        {
            var templates = new List<TemplateEntry> { NewEntry("a"), NewEntry("b") };

            var code = RecycleScrollerTemplateMenuGenerator.BuildCode(templates);

            Assert.IsTrue(code.Contains("CreateTemplate0("), code);
            Assert.IsTrue(code.Contains("CreateTemplate1("), code);
        }

        [Test]
        public void 생성_코드는_수정하지_말라는_표시로_시작한다()
        {
            var code = RecycleScrollerTemplateMenuGenerator.BuildCode(new List<TemplateEntry> { NewEntry("a") });

            Assert.IsTrue(code.StartsWith("// <auto-generated>"), code);
        }
    }
}
