using UnityEngine;

namespace RecycleScroll
{
    /// <summary>
    /// 가상 스크롤 위치(주축 스칼라) 관리.
    /// LoadData 이후 위치의 진실은 m_scrollPos이며, 현재는 Content.anchoredPosition을
    /// 동일 값으로 미러링한다 (윈도우 렌더링 도입 시 미러가 렌더 수식으로 대체될 예정).
    /// </summary>
    public partial class RecycleScroller
    {
        /// <summary>
        /// 주축 가상 스크롤 위치.
        /// 부호 관습은 Dot(anchoredPosition, m_cachedContentPosVec)와 동일 (0 = 콘텐트 시작, 진행 방향 양수)
        /// </summary>
        private float m_scrollPos;

        private bool UseVirtualScroll => m_isInitialized;

        /// <summary>
        /// 물리 코드가 읽는 논리 위치.
        /// LoadData 전에는 anchoredPosition 그대로(순정 ScrollRect 동작 보존),
        /// 이후에는 주축 성분만 m_scrollPos로 교체하고 보조축은 실측 유지
        /// </summary>
        private Vector2 LogicalPosition
        {
            get
            {
                var anchored = m_content.anchoredPosition;
                if (UseVirtualScroll == false) return anchored;

                var main = Vector2.Dot(anchored, m_cachedContentPosVec);
                return anchored + (m_scrollPos - main) * m_cachedContentPosVec;
            }
        }

        /// <summary>순정 → 가상 전환(첫 LoadData) 시 시각 위치 승계용 스칼라 동기화</summary>
        private void SyncScrollPosFromAnchored()
        {
            if (UseVirtualScroll == false) return;
            m_scrollPos = Vector2.Dot(m_content.anchoredPosition, m_cachedContentPosVec);
        }

        /// <summary>주축 정규화 스크롤 위치 (wrap 적용). 루프에서는 최대값이 1을 넘을 수 있음</summary>
        private float MainNormalizedScrollPos
        {
            get
            {
                var scrollSize = ScrollSize;
                if (scrollSize <= 0f) return 0f;
                return m_scrollerMode.Normalize(m_scrollPos, ContentSize) / scrollSize;
            }
        }

        /// <summary>
        /// 루프 wrap 정규화. 좌표계만 이동하고 시각/물리 상태는 보존한다.
        /// m_prevPosition·m_contentStartPosition·m_previousScrollPosition을 동일 shift로
        /// 함께 이동하는 것이 불변식 — 누락 시 위치 점프나 velocity 왜곡 발생.
        /// </summary>
        private void NormalizeVirtualScrollPos()
        {
            if (UseVirtualScroll == false) return;

            var wrapped = m_scrollerMode.Normalize(m_scrollPos, ContentSize);
            var shift = wrapped - m_scrollPos;
            if (Mathf.Approximately(shift, 0f)) return;

            m_scrollPos = wrapped;
            m_previousScrollPosition += shift;
            var shiftVec = shift * m_cachedContentPosVec;
            m_prevPosition += shiftVec;
            m_contentStartPosition += shiftVec;
        }

        /// <summary>데이터 변경(Insert/Remove/Reload) 후 위치를 유효 범위로 보정</summary>
        private void ClampScrollPosAfterDataChange()
        {
            if (UseVirtualScroll == false) return;

            m_scrollPos = m_scrollerMode.IsLoop
                ? m_scrollerMode.Normalize(m_scrollPos, ContentSize)
                : Mathf.Clamp(m_scrollPos, 0f, ScrollSize);
        }
    }
}
