using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RecycleScroll
{
    /// <summary>
    /// 루프 스크롤바 모드.
    /// naturalSize 기반 이동 범위, delta 기반 드래그, wrap-around 서브 핸들.
    /// </summary>
    internal sealed class LoopScrollbarMode : IScrollbarMode
    {
        /// <summary>루프 모드 delta 기반 드래그를 위한 이전 프레임 로컬 커서 위치</summary>
        private Vector2? m_prevDragLocalCursor;

        public void RegisterAdditionalTrackers(
            ref DrivenRectTransformTracker tracker, UnityEngine.Object driver,
            RectTransform leftHandle, RectTransform rightHandle, int axisIndex)
        {
            if (leftHandle)
                tracker.Add(driver, leftHandle,
                    DrivenTransformProperties.Anchors
                    | DrivenTransformProperties.AnchoredPosition
                    | DrivenTransformProperties.SizeDelta);
            if (rightHandle)
                tracker.Add(driver, rightHandle,
                    DrivenTransformProperties.Anchors
                    | DrivenTransformProperties.AnchoredPosition
                    | DrivenTransformProperties.SizeDelta);
        }

        public HandleVisualResult CalculateHandleAnchors(
            float displaySize, float value, float size,
            IRecycleScrollbarDelegate del, int axisIndex, bool reverseValue)
        {
            float visualSize = displaySize;

            // 루프 모드: 핸들 이동 범위 계산에 자연 비율(viewport/content)을 사용.
            // displaySize(고정 최소 크기 적용)로 계산하면 서브 핸들과
            // 이동 범위가 불일치하여 wrap 경계에서 핸들이 점프함.
            float movementScale;
            if (del != null)
            {
                float naturalSize = del.ShowingSize > 0f
                    ? del.ViewportSize / del.ShowingSize
                    : displaySize;
                movementScale = 1f - naturalSize;
            }
            else
            {
                movementScale = 1f - visualSize;
            }

            // 루프 모드: value를 클램프하지 않음
            float movement = value * movementScale;

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

            // 메인 핸들이 [0,1] 경계를 넘는 양을 서브 핸들 wrap으로 분리
            float startWrap = Mathf.Max(0f, anchorMax[axisIndex] - 1f);
            float endWrap = Mathf.Max(0f, -anchorMin[axisIndex]);

            // 메인 핸들을 [0,1]로 클램프 → wrap 부분만큼 실제 사이즈 축소
            anchorMin[axisIndex] = Mathf.Max(0f, anchorMin[axisIndex]);
            anchorMax[axisIndex] = Mathf.Min(1f, anchorMax[axisIndex]);

            return new HandleVisualResult(anchorMin, anchorMax, startWrap, endWrap);
        }

        public void InitDrag(PointerEventData eventData,
            RectTransform containerRect, RectTransform handleRect)
        {
            // 루프 모드: delta 추적용 초기 커서 위치 기록
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                containerRect, eventData.position, eventData.pressEventCamera, out var localCursor))
                m_prevDragLocalCursor = localCursor;
            else
                m_prevDragLocalCursor = null;
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

            if (!m_prevDragLocalCursor.HasValue)
            {
                m_prevDragLocalCursor = localCursor;
                return;
            }

            Vector2 delta = localCursor - m_prevDragLocalCursor.Value;
            m_prevDragLocalCursor = localCursor;

            float axisDelta = axisIndex == 0 ? delta.x : delta.y;
            if (reverseValue) axisDelta = -axisDelta;

            float parentSize = axisIndex == 0
                ? containerRect.rect.width
                : containerRect.rect.height;
            float remainingSize = parentSize * (1 - displaySize);
            if (remainingSize <= 0)
                return;

            float valueDelta = axisDelta / remainingSize;
            setValue(currentValue + valueDelta);
        }

        public void EndDrag()
        {
            m_prevDragLocalCursor = null;
        }

        public float ClampClickValue(float newValue)
        {
            // 루프 모드: 클램프하지 않음 (값이 [0,1] 범위를 넘을 수 있음)
            return newValue;
        }

        public (float real, float showing) ConvertValueForEvent(
            float val, IRecycleScrollbarDelegate del)
        {
            if (del == null || !del.IsLoopScrollable)
                return (val, val);

            // val은 showing-normalized position
            float showingScrollSize = del.ShowingSize - del.ViewportSize;
            if (showingScrollSize <= 0f)
                return (val, val);

            float showingPos = val * showingScrollSize;
            float realPos = del.ConvertShowToReal(showingPos);
            float realScrollSize = del.RealSize - del.ViewportSize;
            float realNormalized = realScrollSize > 0f ? realPos / realScrollSize : 0f;
            return (realNormalized, val);
        }
    }
}
