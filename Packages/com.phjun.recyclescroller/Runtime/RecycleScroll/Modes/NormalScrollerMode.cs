using System;
using UnityEngine;

namespace RecycleScroll
{
    /// <summary>
    /// 비루프 스크롤러 모드.
    /// 직접 좌표계, 실제 패딩, 부분 재계산 가능.
    /// 상태를 가지지 않으므로 싱글톤 사용 가능.
    /// </summary>
    internal sealed class NormalScrollerMode : IScrollerMode
    {
        public static readonly NormalScrollerMode Instance = new();

        public float AddingFrontContentSize => 0f;
        public float AddingBackContentSize => 0f;
        public int FrontAdditionalPageCount => 0;
        public int BackAdditionalPageCount => 0;

        public float GetTopPadding(float spacing, RectOffset padding, eScrollAxis axis)
        {
            return axis switch
            {
                eScrollAxis.VERTICAL => padding.top,
                eScrollAxis.HORIZONTAL => padding.left,
                _ => 0f,
            };
        }

        public float GetBottomPadding(float spacing, RectOffset padding, eScrollAxis axis)
        {
            return axis switch
            {
                eScrollAxis.VERTICAL => padding.bottom,
                eScrollAxis.HORIZONTAL => padding.right,
                _ => 0f,
            };
        }

        public float ConvertRealToShow(float realValue, float showingContentSize)
        {
            return realValue;
        }

        public float ConvertShowToReal(float showValue, float showingContentSize)
        {
            // 원본 코드와 동일: 항상 AdjustShowingPosValue 적용
            // 비루프에서 addingFront=0이므로 modulo만 수행
            var val = showValue % showingContentSize;
            if (val < 0f) val += showingContentSize;
            return val;
        }

        public bool NeedReposition(float currentRealPos, float frontThreshold,
            float backThreshold, float realScrollSize)
        {
            return false;
        }

        public float GetScrollbarNormalizedPosition(
            float showingNormalized, float realNormalized, float showingScrollSize)
        {
            return realNormalized;
        }

        public void ApplyScrollbarValue(float normalizedValue,
            Action<float> setShowingNormalized, Action<float> setRealNormalized)
        {
            setRealNormalized(normalizedValue);
        }

        public bool CanDoPartialRecalc(int cellCount, int prevCellCount)
        {
            return cellCount > 0 && prevCellCount > 0;
        }

        public int ConvertToShowPageIndex(int realPageIndex, int realPageCount)
        {
            return realPageIndex;
        }
    }
}
