using UnityEngine;
using UnityEngine.UI;

namespace RecycleScroll
{
    public partial class RecycleScroller
    {
#if UNITY_EDITOR
        [Space(20f), ColoredHeader("[Recycle Scroller - OnValidate]", ColorHexTemplate.CT_HEX_ADD8E6)]
        [SerializeField] private GameObject[] m_exampleLayoutGroups;

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            UpdateScrollAxisToScrollRect();
            if (m_FitContentToViewport)
                Content.sizeDelta = ScrollAxis == eScrollAxis.VERTICAL
                    ? new Vector2(Viewport.rect.width, ViewportSize)
                    : new Vector2(ViewportSize, Viewport.rect.height);
            UnityEditor.EditorApplication.delayCall += DelayedCall_ForOnValidate;
        }

        private void DelayedCall_ForOnValidate()
        {
            UnityEditor.EditorApplication.delayCall -= DelayedCall_ForOnValidate;
            // 에디터 편집 중 발생하는 RecycleScroller 타입에 대한 MissingReferenceException 에러 예외 처리
            if (this == null || this.gameObject == null) return;

            CheckLayoutGroupToContent();
            SetAlignmentValuesToContentLayout();
            CheckLayoutGroupOfExampleLayoutGroup();
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

                layoutGroup = (HorizontalOrVerticalLayoutGroup)layoutGroupObj.AddComponent(needType);
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
    }
}