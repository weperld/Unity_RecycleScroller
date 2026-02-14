using System;
using UnityEngine;

namespace RecycleScroll
{
    /// <summary>
    /// 루프 스크롤러 모드.
    /// 표시/실제 좌표 변환, spacing/2 패딩, 전체 재계산.
    /// CheckLoop()에서 생성되며 루프 전용 상태를 보유합니다.
    /// </summary>
    internal sealed class LoopScrollerMode : IScrollerMode
    {
        public float AddingFrontContentSize { get; }
        public float AddingBackContentSize { get; }
        public int FrontAdditionalPageCount { get; }
        public int BackAdditionalPageCount { get; }

        public LoopScrollerMode(
            float addingFrontContentSize,
            float addingBackContentSize,
            int frontAdditionalPageCount,
            int backAdditionalPageCount)
        {
            AddingFrontContentSize = addingFrontContentSize;
            AddingBackContentSize = addingBackContentSize;
            FrontAdditionalPageCount = frontAdditionalPageCount;
            BackAdditionalPageCount = backAdditionalPageCount;
        }

        public float GetTopPadding(float spacing, RectOffset padding, eScrollAxis axis)
        {
            return spacing / 2f;
        }

        public float GetBottomPadding(float spacing, RectOffset padding, eScrollAxis axis)
        {
            return spacing / 2f;
        }

        public float ConvertRealToShow(float realValue, float showingContentSize)
        {
            var pos = realValue - AddingFrontContentSize;
            pos %= showingContentSize;
            if (pos < 0f) pos += showingContentSize;
            return pos;
        }

        public float ConvertShowToReal(float showValue, float showingContentSize)
        {
            var val = showValue % showingContentSize;
            if (val < 0f) val += showingContentSize;
            return val + AddingFrontContentSize;
        }

        public bool NeedReposition(float currentRealPos, float frontThreshold,
            float backThreshold, float realScrollSize)
        {
            return currentRealPos < frontThreshold
                || currentRealPos > realScrollSize - backThreshold;
        }

        public float GetScrollbarNormalizedPosition(
            float showingNormalized, float realNormalized, float showingScrollSize)
        {
            // ShowingScrollSize가 0이면 (content <= viewport) NaN 방지
            return showingScrollSize > 0f ? showingNormalized : 0f;
        }

        public void ApplyScrollbarValue(float normalizedValue,
            Action<float> setShowingNormalized, Action<float> setRealNormalized)
        {
            setShowingNormalized(normalizedValue);
        }

        public bool CanDoPartialRecalc(int cellCount, int prevCellCount)
        {
            // 루프 모드: 삽입/삭제 시 항상 전체 재계산 필요
            return false;
        }

        public int ConvertToShowPageIndex(int realPageIndex, int realPageCount)
        {
            var adjustIndex = realPageIndex - FrontAdditionalPageCount;
            adjustIndex %= realPageCount;
            if (adjustIndex < 0) adjustIndex += realPageCount;
            return adjustIndex;
        }
    }
}
