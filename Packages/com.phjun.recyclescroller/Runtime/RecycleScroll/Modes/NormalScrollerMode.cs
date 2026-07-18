using UnityEngine;

namespace RecycleScroll
{
    /// <summary>
    /// 비루프 스크롤러 모드. [0, scrollSize] 경계 클램프 정책.
    /// 상태를 가지지 않으므로 싱글톤 사용.
    /// </summary>
    internal sealed class NormalScrollerMode : IScrollerMode
    {
        public static readonly NormalScrollerMode Instance = new();

        public bool IsLoop => false;

        public float Normalize(float pos, float contentSize) => pos;

        public float CalculateOffset(float pos, float scrollSize)
            => Mathf.Clamp(pos, 0f, scrollSize) - pos;

        public float GetMoveDelta(float current, float target, float contentSize, float scrollSize)
            => Mathf.Clamp(target, 0f, scrollSize) - current;

        public float GetTopPadding(float spacing, float paddingValue) => paddingValue;

        public float GetBottomPadding(float spacing, float paddingValue) => paddingValue;
    }
}
