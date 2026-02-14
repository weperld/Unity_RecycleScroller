using System;

namespace RecycleScroll
{
    /// <summary>
    /// RecycleScroller의 루프/비루프 동작 차이를 추상화하는 전략 인터페이스.
    /// 비루프: 직접 좌표계, 실제 패딩 사용, 부분 재계산
    /// 루프: 표시/실제 좌표 변환, spacing/2 패딩, 전체 재계산
    /// </summary>
    internal interface IScrollerMode
    {
        /// <summary>루프 모드에서 앞쪽에 추가된 콘텐츠 크기. 비루프: 0.</summary>
        float AddingFrontContentSize { get; }

        /// <summary>루프 모드에서 뒤쪽에 추가된 콘텐츠 크기. 비루프: 0.</summary>
        float AddingBackContentSize { get; }

        /// <summary>루프 모드에서 앞쪽 추가 페이지 수. 비루프: 0.</summary>
        int FrontAdditionalPageCount { get; }

        /// <summary>루프 모드에서 뒤쪽 추가 페이지 수. 비루프: 0.</summary>
        int BackAdditionalPageCount { get; }

        /// <summary>
        /// 주축 방향 상단 패딩을 반환합니다.
        /// 루프: spacing/2, 비루프: 실제 패딩 값.
        /// </summary>
        float GetTopPadding(float spacing, UnityEngine.RectOffset padding, eScrollAxis axis);

        /// <summary>
        /// 주축 방향 하단 패딩을 반환합니다.
        /// 루프: spacing/2, 비루프: 실제 패딩 값.
        /// </summary>
        float GetBottomPadding(float spacing, UnityEngine.RectOffset padding, eScrollAxis axis);

        /// <summary>
        /// 실좌표(Real)를 표시좌표(Showing)로 변환합니다.
        /// 비루프: identity. 루프: front offset 차감 + modulo.
        /// </summary>
        float ConvertRealToShow(float realValue, float showingContentSize);

        /// <summary>
        /// 표시좌표(Showing)를 실좌표(Real)로 변환합니다.
        /// 비루프: modulo만 적용. 루프: modulo + front offset 가산.
        /// </summary>
        float ConvertShowToReal(float showValue, float showingContentSize);

        /// <summary>
        /// 스크롤 이벤트 시 경계 리포지션이 필요한지 판단합니다.
        /// 비루프: 항상 false. 루프: 임계값 도달 시 true.
        /// </summary>
        bool NeedReposition(float currentRealPos, float frontThreshold,
            float backThreshold, float realScrollSize);

        /// <summary>
        /// 스크롤바에 설정할 normalized position을 반환합니다.
        /// 비루프: realNormalized. 루프: showingNormalized (showingScrollSize > 0 시).
        /// </summary>
        float GetScrollbarNormalizedPosition(
            float showingNormalized, float realNormalized, float showingScrollSize);

        /// <summary>
        /// 스크롤바 값 변경을 스크롤 위치에 적용합니다.
        /// 비루프: setRealNormalized. 루프: setShowingNormalized.
        /// </summary>
        void ApplyScrollbarValue(float normalizedValue,
            Action<float> setShowingNormalized, Action<float> setRealNormalized);

        /// <summary>
        /// Insert/Remove 시 부분 재계산이 가능한지 판단합니다.
        /// 비루프: cellCount > 0 && prevCellCount > 0이면 가능. 루프: 항상 불가.
        /// </summary>
        bool CanDoPartialRecalc(int cellCount, int prevCellCount);

        /// <summary>
        /// 실제 페이지 인덱스를 표시용 인덱스로 변환합니다.
        /// 비루프: identity. 루프: front 페이지 수 보정 + modulo.
        /// </summary>
        int ConvertToShowPageIndex(int realPageIndex, int realPageCount);
    }
}
