namespace RecycleScroll
{
    /// <summary>
    /// RecycleScroller의 루프/비루프 동작 차이를 추상화하는 전략 인터페이스.
    /// 모드 차이는 경계 정책 하나로 수렴한다:
    /// 비루프 = [0, scrollSize] 클램프(러버밴드/클램프가 이탈량 소비), 루프 = contentSize 주기 wrap
    /// </summary>
    internal interface IScrollerMode
    {
        bool IsLoop { get; }

        /// <summary>가상 스크롤 위치 정규화. 비루프: identity, 루프: [0, contentSize) wrap</summary>
        float Normalize(float pos, float contentSize);

        /// <summary>주축 범위 이탈량. 비루프: Clamp(pos, 0, scrollSize) - pos, 루프: 항상 0</summary>
        float CalculateOffset(float pos, float scrollSize);

        /// <summary>current에서 target까지의 이동량. 비루프: 클램프된 target까지, 루프: 최단거리</summary>
        float GetMoveDelta(float current, float target, float contentSize, float scrollSize);

        /// <summary>주축 시작 패딩. 비루프: 실제 패딩 값, 루프: spacing/2 (이음새 간격 균일화)</summary>
        float GetTopPadding(float spacing, float paddingValue);

        /// <summary>주축 끝 패딩. 비루프: 실제 패딩 값, 루프: spacing/2 (이음새 간격 균일화)</summary>
        float GetBottomPadding(float spacing, float paddingValue);
    }
}
