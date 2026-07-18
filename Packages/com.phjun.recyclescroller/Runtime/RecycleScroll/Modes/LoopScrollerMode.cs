using UnityEngine;

namespace RecycleScroll
{
    /// <summary>
    /// 루프 스크롤러 모드. contentSize 주기 wrap 경계 정책.
    /// 복제 그룹 없이 원본 데이터만으로 순환하며, 상태를 가지지 않으므로 싱글톤 사용.
    /// </summary>
    internal sealed class LoopScrollerMode : IScrollerMode
    {
        public static readonly LoopScrollerMode Instance = new();

        public bool IsLoop => true;

        public float Normalize(float pos, float contentSize)
            => contentSize > 0f ? Mathf.Repeat(pos, contentSize) : 0f;

        public float CalculateOffset(float pos, float scrollSize) => 0f;

        public float GetMoveDelta(float current, float target, float contentSize, float scrollSize)
        {
            if (contentSize <= 0f) return 0f;

            // 최단거리: 차이를 [-contentSize/2, contentSize/2) 범위로 wrap
            var half = contentSize * 0.5f;
            return Mathf.Repeat(target - current + half, contentSize) - half;
        }

        public float GetTopPadding(float spacing, float paddingValue) => spacing / 2f;

        public float GetBottomPadding(float spacing, float paddingValue) => spacing / 2f;
    }
}
