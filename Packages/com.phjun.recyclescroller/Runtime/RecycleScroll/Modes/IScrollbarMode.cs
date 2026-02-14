using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RecycleScroll
{
    /// <summary>
    /// RecycleScrollbar의 루프/비루프 동작 차이를 추상화하는 전략 인터페이스.
    /// 비루프: Elastic 핸들 축소, 절대 좌표 드래그, [0,1] 클램프
    /// 루프: naturalSize 기반 이동, delta 드래그, wrap-around 서브 핸들
    /// </summary>
    internal interface IScrollbarMode
    {
        /// <summary>
        /// UpdateVisuals에서 모드별 추가 트래커를 등록합니다.
        /// 루프 모드: 서브 핸들 트래커 등록. 비루프: no-op.
        /// </summary>
        void RegisterAdditionalTrackers(
            ref DrivenRectTransformTracker tracker, UnityEngine.Object driver,
            RectTransform leftHandle, RectTransform rightHandle, int axisIndex);

        /// <summary>
        /// 핸들의 앵커 위치를 모드별로 계산합니다.
        /// displaySize: Fixed Handle Size 적용된 표시 크기
        /// size: 자연 크기 비율 (viewport/content)
        /// </summary>
        HandleVisualResult CalculateHandleAnchors(
            float displaySize, float value, float size,
            IRecycleScrollbarDelegate del, int axisIndex, bool reverseValue);

        /// <summary>드래그 시작 시 모드별 초기화</summary>
        void InitDrag(PointerEventData eventData,
            RectTransform containerRect, RectTransform handleRect);

        /// <summary>드래그 중 모드별 값 계산</summary>
        void ProcessDrag(PointerEventData eventData,
            RectTransform containerRect, RectTransform handleRect,
            float displaySize, float currentValue,
            int axisIndex, bool reverseValue, Action<float> setValue);

        /// <summary>드래그 종료 시 모드별 정리</summary>
        void EndDrag();

        /// <summary>
        /// ClickRepeat에서 새 값을 모드별로 클램프합니다.
        /// 비루프: Clamp01 + Round. 루프: 클램프 없음.
        /// </summary>
        float ClampClickValue(float newValue);

        /// <summary>
        /// OnLoopValueChanged 이벤트를 위한 값 변환.
        /// 비루프: (val, val). 루프: 좌표 변환 후 (real, showing).
        /// </summary>
        (float real, float showing) ConvertValueForEvent(
            float val, IRecycleScrollbarDelegate del);
    }

    /// <summary>
    /// CalculateHandleAnchors의 결과를 담는 구조체.
    /// </summary>
    internal readonly struct HandleVisualResult
    {
        public readonly Vector2 AnchorMin;
        public readonly Vector2 AnchorMax;

        /// <summary>루프 모드에서 시작 에지(0 side) wrap 양. 비루프: 항상 0.</summary>
        public readonly float StartWrap;

        /// <summary>루프 모드에서 끝 에지(1 side) wrap 양. 비루프: 항상 0.</summary>
        public readonly float EndWrap;

        public HandleVisualResult(Vector2 anchorMin, Vector2 anchorMax,
            float startWrap = 0f, float endWrap = 0f)
        {
            AnchorMin = anchorMin;
            AnchorMax = anchorMax;
            StartWrap = startWrap;
            EndWrap = endWrap;
        }

        public bool HasWrap => StartWrap > 0.001f || EndWrap > 0.001f;
    }
}
