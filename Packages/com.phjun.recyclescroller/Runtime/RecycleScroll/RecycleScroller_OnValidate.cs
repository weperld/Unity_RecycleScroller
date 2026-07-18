using UnityEngine;
using UnityEngine.UI;

namespace RecycleScroll
{
    public partial class RecycleScroller
    {
#if UNITY_EDITOR
        [SerializeField] private GameObject[] m_exampleLayoutGroups;

        protected override void OnValidate()
        {
            SetDirtyCaching();
            if (Application.isPlaying) return;

            if (Content == null || Viewport == null) return;

            UpdateScrollAxisToScrollRect();
            UnityEditor.EditorApplication.delayCall += DelayedCall_ForOnValidate;
        }

        private void DelayedCall_ForOnValidate()
        {
            UnityEditor.EditorApplication.delayCall -= DelayedCall_ForOnValidate;
            // 에디터 편집 중 발생하는 RecycleScroller 타입에 대한 MissingReferenceException 에러 예외 처리
            if (this == null || this.gameObject == null) return;

            // RectTransform 크기 변경은 OnValidate 안에서 금지(SendMessage 경고) — 지연 호출로 수행
            if (m_fitContentToViewport && Content != null)
                Content.sizeDelta = ScrollAxis == eScrollAxis.VERTICAL
                    ? new Vector2(0f, ViewportSize)
                    : new Vector2(ViewportSize, 0f);

            CheckLayoutGroupToContent();
            SetAlignmentValuesToContentLayout();
            CheckLayoutGroupOfExampleLayoutGroup();
            UpdateScrollbarSizeFromRect();
            HideCrossAxisScrollbar();
        }

        private void CheckLayoutGroupOfExampleLayoutGroup()
        {
            if (m_exampleLayoutGroups == null || m_exampleLayoutGroups.Length == 0) return;

            var needType = GetNeedLayoutGroupTypeOfGroupCell();
            foreach (var layoutGroupObj in m_exampleLayoutGroups)
            {
                if (layoutGroupObj == null) continue;

                #region Reset Group Cell Size

                var maxSize = GetMaxSizeOfChildObj(layoutGroupObj.transform);
                var sizeDelta = layoutGroupObj.GetComponent<RectTransform>().sizeDelta;
                switch (ScrollAxis)
                {
                    case eScrollAxis.VERTICAL:
                        sizeDelta.y = maxSize;
                        break;
                    case eScrollAxis.HORIZONTAL:
                        sizeDelta.x = maxSize;
                        break;
                }

                layoutGroupObj.GetComponent<RectTransform>().sizeDelta = sizeDelta;

                #endregion

                #region Set Layout Group

                var layoutGroup = layoutGroupObj.GetComponent<HorizontalOrVerticalLayoutGroup>();
                if (layoutGroup && layoutGroup.GetType() == needType)
                {
                    SetLayoutGroupFieldOfCellGroup(layoutGroup);
                    continue;
                }

                if (layoutGroup)
                {
                    if (Application.isPlaying) DestroyImmediate(layoutGroup);
                    else UnityEditor.Undo.DestroyObjectImmediate(layoutGroup);
                }

                layoutGroup = layoutGroupObj.AddComponent(needType) as HorizontalOrVerticalLayoutGroup;
                if (layoutGroup == null) return;
                SetLayoutGroupFieldOfCellGroup(layoutGroup);

                #endregion
            }

            #region Local Functions

            float GetMaxSizeOfChildObj(Transform parent)
            {
                if (parent == null || parent.childCount == 0) return 0f;
                var maxSize = 0f;
                foreach (Transform child in parent)
                {
                    var rectTransform = child.GetComponent<RectTransform>();
                    if (rectTransform == null) continue;
                    var size = GetSizeOfRectTransform(rectTransform);
                    if (size > maxSize) maxSize = size;
                }

                return maxSize;
            }

            float GetSizeOfRectTransform(RectTransform rectTransform)
            {
                if (rectTransform == null) return 0f;
                var size = ScrollAxis == eScrollAxis.VERTICAL ? rectTransform.rect.height : rectTransform.rect.width;
                var localScale = rectTransform.localScale;
                var scale = ScrollAxis == eScrollAxis.VERTICAL ? localScale.y : localScale.x;
                return size * scale;
            }

            #endregion
        }

#endif

        protected override void Start()
        {
            base.Start();

            UpdateScrollbarSizeFromRect();
        }

        /// <summary>
        /// LoadData 호출 전 ScrollRect의 Content/Viewport 비율로 스크롤바 크기를 설정합니다.
        /// 에디트 모드(OnValidate)와 런타임(Start) 모두에서 호출됩니다.
        /// </summary>
        private void UpdateScrollbarSizeFromRect()
        {
            var scrollbar = MainAxisScrollbar;
            if (scrollbar == null) return;

            // LoadData 후에는 Content 렉트가 윈도우 크기라 실측이 무의미하고,
            // 이 함수는 레이아웃 리빌드 루프(SetLayoutVertical) 안에서도 호출되므로
            // SetSize→Refresh(서브 핸들 SetActive) 경로를 타면 안 됨.
            // 초기화 후 크기 갱신은 LoadData/Insert/LateUpdate(리빌드 밖)에서 수행
            if (Application.isPlaying && m_isInitialized) return;

            float viewportSize = ViewportSize;
            float contentSize = ScrollAxis == eScrollAxis.VERTICAL
                ? Content.rect.height
                : Content.rect.width;

            float size = contentSize > 0f ? Mathf.Clamp01(viewportSize / contentSize) : 1f;
            scrollbar.Size = size;
        }
    }
}