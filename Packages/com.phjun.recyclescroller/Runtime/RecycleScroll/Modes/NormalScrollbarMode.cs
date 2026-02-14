using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RecycleScroll
{
    /// <summary>
    /// 비루프 스크롤바 모드.
    /// Elastic 오버슈트 시 핸들 축소, 절대 좌표 기반 드래그, [0,1] 값 클램프.
    /// </summary>
    internal sealed class NormalScrollbarMode : IScrollbarMode
    {
        /// <summary>비루프 드래그용 절대 위치 오프셋</summary>
        private Vector2 m_offset;

        public void RegisterAdditionalTrackers(
            ref DrivenRectTransformTracker tracker, UnityEngine.Object driver,
            RectTransform leftHandle, RectTransform rightHandle, int axisIndex)
        {
            // 비루프 모드: 서브 핸들 트래커 불필요
        }

        public HandleVisualResult CalculateHandleAnchors(
            float displaySize, float value, float size,
            IRecycleScrollbarDelegate del, int axisIndex, bool reverseValue)
        {
            // Elastic 핸들 사이즈 축소
            float elasticDisplaySize = displaySize;
            float overValue = value < 0f ? -value : (value > 1f ? value - 1f : 0f);
            if (overValue > 0f)
                elasticDisplaySize = Mathf.Clamp01(displaySize - overValue * (1f - size));

            float visualSize = elasticDisplaySize;
            float movementScale = 1f - visualSize;
            float movement = Mathf.Clamp01(value) * movementScale;

            Vector2 anchorMin = Vector2.zero;
            Vector2 anchorMax = Vector2.one;

            if (reverseValue)
            {
                anchorMin[axisIndex] = 1 - movement - visualSize;
                anchorMax[axisIndex] = 1 - movement;
            }
            else
            {
                anchorMin[axisIndex] = movement;
                anchorMax[axisIndex] = movement + visualSize;
            }

            return new HandleVisualResult(anchorMin, anchorMax);
        }

        public void InitDrag(PointerEventData eventData,
            RectTransform containerRect, RectTransform handleRect)
        {
            m_offset = Vector2.zero;
            if (RectTransformUtility.RectangleContainsScreenPoint(
                handleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    handleRect, eventData.pointerPressRaycast.screenPosition,
                    eventData.pressEventCamera, out var localMousePos))
                    m_offset = localMousePos - handleRect.rect.center;
            }
        }

        public void ProcessDrag(PointerEventData eventData,
            RectTransform containerRect, RectTransform handleRect,
            float displaySize, float currentValue,
            int axisIndex, bool reverseValue, Action<float> setValue)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (containerRect == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                containerRect, eventData.position, eventData.pressEventCamera, out var localCursor))
                return;

            Vector2 handleCenterRelativeToContainerCorner =
                localCursor - m_offset - containerRect.rect.position;
            Vector2 handleCorner = handleCenterRelativeToContainerCorner
                - (handleRect.rect.size - handleRect.sizeDelta) * 0.5f;

            float parentSize = axisIndex == 0
                ? containerRect.rect.width
                : containerRect.rect.height;
            float remainingSize = parentSize * (1 - displaySize);
            if (remainingSize <= 0)
                return;

            float axisValue = axisIndex == 0 ? handleCorner.x : handleCorner.y;
            float normalizedValue = axisValue / remainingSize;

            if (reverseValue)
                normalizedValue = 1f - normalizedValue;

            setValue(Mathf.Clamp01(normalizedValue));
        }

        public void EndDrag()
        {
            // 비루프 모드: 정리할 상태 없음
        }

        public float ClampClickValue(float newValue)
        {
            newValue = Mathf.Clamp01(newValue);
            return Mathf.Round(newValue * 10000f) / 10000f;
        }

        public (float real, float showing) ConvertValueForEvent(
            float val, IRecycleScrollbarDelegate del)
        {
            return (val, val);
        }
    }
}
