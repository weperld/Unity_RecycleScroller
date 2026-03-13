using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecycleScroll
{
    /// <summary>
    /// ScrollRect 소스 코드 포크.
    /// 드래그/관성/탄성 물리, Bounds 계산, normalizedPosition, Canvas/Layout 시스템을 포함합니다.
    /// </summary>
    public partial class RecycleScroller
    {
        #region ScrollRect Forked Fields

        private bool m_horizontal;
        private bool m_vertical;
        private Vector2 m_pointerStartLocalCursor = Vector2.zero;
        private Vector2 m_contentStartPosition = Vector2.zero;
        private RectTransform m_viewRect;
        private Bounds m_contentBounds;
        private Bounds m_viewBounds;
        private Vector2 m_velocity;
        private bool m_dragging;
        private bool m_scrolling;
        private Vector2 m_prevPosition = Vector2.zero;
        private Bounds m_prevContentBounds;
        private Bounds m_prevViewBounds;
        [NonSerialized] private bool m_hasRebuiltLayout = false;
        private bool m_hSliderExpand;
        private bool m_vSliderExpand;
        private float m_hSliderHeight;
        private float m_vSliderWidth;
        private DrivenRectTransformTracker m_tracker;
        private readonly Vector3[] m_corners = new Vector3[4];
        private RectTransform m_mainAxisScrollbarRect;

        #endregion

        #region ScrollRect Properties

        /// <summary>
        /// Content의 현재 속도 (단위: units/second)
        /// </summary>
        public Vector2 velocity { get => m_velocity; set => m_velocity = value; }

        private RectTransform viewRect
        {
            get
            {
                if (m_viewRect == null)
                    m_viewRect = m_viewport != null ? m_viewport : (RectTransform)transform;
                return m_viewRect;
            }
        }

        private bool hScrollingNeeded
        {
            get
            {
                if (Application.isPlaying)
                    return m_contentBounds.size.x > m_viewBounds.size.x + 0.01f;
                return true;
            }
        }

        private bool vScrollingNeeded
        {
            get
            {
                if (Application.isPlaying)
                    return m_contentBounds.size.y > m_viewBounds.size.y + 0.01f;
                return true;
            }
        }

        private bool MainAxisScrollingNeeded
            => ScrollAxis == eScrollAxis.VERTICAL ? vScrollingNeeded : hScrollingNeeded;

        #endregion

        #region ScrollRect Lifecycle

        public override bool IsActive()
        {
            return base.IsActive() && m_content;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            UpdateScrollAxisToScrollRect();
            HideCrossAxisScrollbar();

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            SetDirty();
        }

        protected override void OnDisable()
        {
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            m_dragging = false;
            m_scrolling = false;
            m_hasRebuiltLayout = false;
            m_velocity = Vector2.zero;

            StopAllMoveCor();
            StopAllPreviousLoadDataTask();
            StopCorWaitLoadDataStateToCompleteForBuffer();

            m_tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(_RectTransform);
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        #endregion

        #region ScrollRect Input Handlers

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_velocity = Vector2.zero;
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (IsActive() == false)
                return;

            UpdateBounds();

            m_pointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewRect, eventData.position, eventData.pressEventCamera, out m_pointerStartLocalCursor);
            m_contentStartPosition = m_content.anchoredPosition;
            m_dragging = true;

            // RecycleScroller 기존 로직
            StopAllMoveCor();
            onBeginDrag?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (m_dragging == false)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (IsActive() == false)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    viewRect, eventData.position, eventData.pressEventCamera, out var localCursor) == false)
                return;

            UpdateBounds();

            var pointerDelta = localCursor - m_pointerStartLocalCursor;
            Vector2 position = m_contentStartPosition + pointerDelta;

            Vector2 offset = CalculateOffset(position - m_content.anchoredPosition);
            position += offset;
            if (CurrentMovementType == ScrollRect.MovementType.Elastic)
            {
                if (offset.x != 0)
                    position.x = position.x - RubberDelta(offset.x, m_viewBounds.size.x);
                if (offset.y != 0)
                    position.y = position.y - RubberDelta(offset.y, m_viewBounds.size.y);
            }

            SetContentAnchoredPosition(position);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_dragging = false;

            // RecycleScroller 기존 로직
            if (m_pagingData.usePaging) StartPagingCor();
            onEndDrag?.Invoke();
        }

        public void OnScroll(PointerEventData data)
        {
            if (IsActive() == false)
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();

            Vector2 delta = data.scrollDelta;
            // UI 시스템에서 위가 양수이므로 y 반전
            delta.y *= -1;
            if (m_vertical && !m_horizontal)
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    delta.y = delta.x;
                delta.x = 0;
            }
            if (m_horizontal && !m_vertical)
            {
                if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                    delta.x = delta.y;
                delta.y = 0;
            }

            if (data.IsScrolling())
                m_scrolling = true;

            Vector2 position = m_content.anchoredPosition;
            position += delta * m_scrollSensitivity;
            if (CurrentMovementType == ScrollRect.MovementType.Clamped)
                position += CalculateOffset(position - m_content.anchoredPosition);

            SetContentAnchoredPosition(position);
            UpdateBounds();
        }

        #endregion

        #region ScrollRect Content Position

        private void SetContentAnchoredPosition(Vector2 position)
        {
            if (m_horizontal == false)
                position.x = m_content.anchoredPosition.x;
            if (m_vertical == false)
                position.y = m_content.anchoredPosition.y;

            if (position != m_content.anchoredPosition)
            {
                m_content.anchoredPosition = position;
                UpdateBounds();
            }
        }

        #endregion

        #region ScrollRect LateUpdate

        private void LateUpdate()
        {
            if (m_content == null)
                return;

#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                UpdateScrollbarVisibility();
                UpdateScrollbarSizeFromRect();
                return;
            }
#endif

            EnsureLayoutHasRebuilt();
            UpdateBounds();
            float deltaTime = Time.unscaledDeltaTime;
            Vector2 offset = CalculateOffset(Vector2.zero);

            if (deltaTime > 0.0f)
            {
                if (m_dragging == false && (offset != Vector2.zero || m_velocity != Vector2.zero))
                {
                    Vector2 position = m_content.anchoredPosition;
                    for (int axis = 0; axis < 2; axis++)
                    {
                        if (CurrentMovementType == ScrollRect.MovementType.Elastic && offset[axis] != 0)
                        {
                            float speed = m_velocity[axis];
                            float smoothTime = m_elasticity;
                            if (m_scrolling)
                                smoothTime *= 3.0f;
                            position[axis] = Mathf.SmoothDamp(
                                m_content.anchoredPosition[axis],
                                m_content.anchoredPosition[axis] + offset[axis],
                                ref speed, smoothTime, Mathf.Infinity, deltaTime);
                            if (Mathf.Abs(speed) < 1)
                                speed = 0;
                            m_velocity[axis] = speed;
                        }
                        else if (m_inertia)
                        {
                            m_velocity[axis] *= Mathf.Pow(m_decelerationRate, deltaTime);
                            if (Mathf.Abs(m_velocity[axis]) < 1)
                                m_velocity[axis] = 0;
                            position[axis] += m_velocity[axis] * deltaTime;
                        }
                        else
                        {
                            m_velocity[axis] = 0;
                        }
                    }

                    if (CurrentMovementType == ScrollRect.MovementType.Clamped)
                    {
                        offset = CalculateOffset(position - m_content.anchoredPosition);
                        position += offset;
                    }

                    SetContentAnchoredPosition(position);
                }

                if (m_dragging && m_inertia)
                {
                    Vector3 newVelocity = (m_content.anchoredPosition - m_prevPosition) / deltaTime;
                    m_velocity = Vector3.Lerp(m_velocity, newVelocity, deltaTime * 10);
                }
            }

            if (m_viewBounds != m_prevViewBounds || m_contentBounds != m_prevContentBounds
                || m_content.anchoredPosition != m_prevPosition)
            {
                OnScrollPositionChanged(normalizedPosition);
                UpdatePrevData();
            }

            UpdateScrollbarVisibility();
            if (m_isInitialized == false)
                UpdateScrollbarSizeFromRect();
            m_scrolling = false;
        }

        #endregion

        #region ScrollRect Normalized Position

        private Vector2 normalizedPosition
        {
            get => new Vector2(horizontalNormalizedPosition, verticalNormalizedPosition);
            set
            {
                SetNormalizedPosition(value.x, 0);
                SetNormalizedPosition(value.y, 1);
            }
        }

        private float horizontalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if ((m_contentBounds.size.x <= m_viewBounds.size.x)
                    || Mathf.Approximately(m_contentBounds.size.x, m_viewBounds.size.x))
                    return (m_viewBounds.min.x > m_contentBounds.min.x) ? 1 : 0;
                return (m_viewBounds.min.x - m_contentBounds.min.x) / (m_contentBounds.size.x - m_viewBounds.size.x);
            }
            set => SetNormalizedPosition(value, 0);
        }

        private float verticalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if ((m_contentBounds.size.y <= m_viewBounds.size.y)
                    || Mathf.Approximately(m_contentBounds.size.y, m_viewBounds.size.y))
                    return (m_viewBounds.min.y > m_contentBounds.min.y) ? 1 : 0;
                return (m_viewBounds.min.y - m_contentBounds.min.y) / (m_contentBounds.size.y - m_viewBounds.size.y);
            }
            set => SetNormalizedPosition(value, 1);
        }

        private void SetNormalizedPosition(float value, int axis)
        {
            EnsureLayoutHasRebuilt();
            UpdateBounds();

            float hiddenLength = m_contentBounds.size[axis] - m_viewBounds.size[axis];
            float contentBoundsMinPosition = m_viewBounds.min[axis] - value * hiddenLength;
            float newAnchoredPosition = m_content.anchoredPosition[axis] + contentBoundsMinPosition - m_contentBounds.min[axis];

            Vector3 anchoredPosition = m_content.anchoredPosition;
            if (Mathf.Abs(anchoredPosition[axis] - newAnchoredPosition) > 0.01f)
            {
                anchoredPosition[axis] = newAnchoredPosition;
                m_content.anchoredPosition = anchoredPosition;
                m_velocity[axis] = 0;
                UpdateBounds();
            }
        }

        #endregion

        #region ScrollRect Bounds

        private void UpdateBounds()
        {
            m_viewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            m_contentBounds = GetBounds();

            if (m_content == null)
                return;

            Vector3 contentSize = m_contentBounds.size;
            Vector3 contentPos = m_contentBounds.center;
            var contentPivot = m_content.pivot;
            AdjustBounds(ref m_viewBounds, ref contentPivot, ref contentSize, ref contentPos);
            m_contentBounds.size = contentSize;
            m_contentBounds.center = contentPos;

            if (CurrentMovementType == ScrollRect.MovementType.Clamped)
            {
                Vector2 delta = Vector2.zero;
                if (m_viewBounds.max.x > m_contentBounds.max.x)
                    delta.x = Math.Min(m_viewBounds.min.x - m_contentBounds.min.x, m_viewBounds.max.x - m_contentBounds.max.x);
                else if (m_viewBounds.min.x < m_contentBounds.min.x)
                    delta.x = Math.Max(m_viewBounds.min.x - m_contentBounds.min.x, m_viewBounds.max.x - m_contentBounds.max.x);

                if (m_viewBounds.min.y < m_contentBounds.min.y)
                    delta.y = Math.Max(m_viewBounds.min.y - m_contentBounds.min.y, m_viewBounds.max.y - m_contentBounds.max.y);
                else if (m_viewBounds.max.y > m_contentBounds.max.y)
                    delta.y = Math.Min(m_viewBounds.min.y - m_contentBounds.min.y, m_viewBounds.max.y - m_contentBounds.max.y);

                if (delta.sqrMagnitude > float.Epsilon)
                {
                    contentPos = m_content.anchoredPosition + delta;
                    if (m_horizontal == false)
                        contentPos.x = m_content.anchoredPosition.x;
                    if (m_vertical == false)
                        contentPos.y = m_content.anchoredPosition.y;
                    AdjustBounds(ref m_viewBounds, ref contentPivot, ref contentSize, ref contentPos);
                }
            }
        }

        private static void AdjustBounds(ref Bounds viewBounds, ref Vector2 contentPivot,
            ref Vector3 contentSize, ref Vector3 contentPos)
        {
            Vector3 excess = viewBounds.size - contentSize;
            if (excess.x > 0)
            {
                contentPos.x -= excess.x * (contentPivot.x - 0.5f);
                contentSize.x = viewBounds.size.x;
            }
            if (excess.y > 0)
            {
                contentPos.y -= excess.y * (contentPivot.y - 0.5f);
                contentSize.y = viewBounds.size.y;
            }
        }

        private Bounds GetBounds()
        {
            if (m_content == null)
                return new Bounds();
            m_content.GetWorldCorners(m_corners);
            var viewWorldToLocalMatrix = viewRect.worldToLocalMatrix;
            return InternalGetBounds(m_corners, ref viewWorldToLocalMatrix);
        }

        private static Bounds InternalGetBounds(Vector3[] corners, ref Matrix4x4 viewWorldToLocalMatrix)
        {
            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = viewWorldToLocalMatrix.MultiplyPoint3x4(corners[j]);
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }

            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);
            return bounds;
        }

        private Vector2 CalculateOffset(Vector2 delta)
        {
            return InternalCalculateOffset(ref m_viewBounds, ref m_contentBounds,
                m_horizontal, m_vertical, CurrentMovementType, ref delta);
        }

        private static Vector2 InternalCalculateOffset(ref Bounds viewBounds, ref Bounds contentBounds,
            bool horizontal, bool vertical, ScrollRect.MovementType movementType, ref Vector2 delta)
        {
            Vector2 offset = Vector2.zero;
            if (movementType == ScrollRect.MovementType.Unrestricted)
                return offset;

            Vector2 min = contentBounds.min;
            Vector2 max = contentBounds.max;

            if (horizontal)
            {
                min.x += delta.x;
                max.x += delta.x;

                float maxOffset = viewBounds.max.x - max.x;
                float minOffset = viewBounds.min.x - min.x;

                if (minOffset < -0.001f)
                    offset.x = minOffset;
                else if (maxOffset > 0.001f)
                    offset.x = maxOffset;
            }

            if (vertical)
            {
                min.y += delta.y;
                max.y += delta.y;

                float maxOffset = viewBounds.max.y - max.y;
                float minOffset = viewBounds.min.y - min.y;

                if (maxOffset > 0.001f)
                    offset.y = maxOffset;
                else if (minOffset < -0.001f)
                    offset.y = minOffset;
            }

            return offset;
        }

        #endregion

        #region ScrollRect Utility

        private void EnsureLayoutHasRebuilt()
        {
            if (m_hasRebuiltLayout == false && CanvasUpdateRegistry.IsRebuildingLayout() == false)
                Canvas.ForceUpdateCanvases();
        }

        public void StopMovement()
        {
            m_velocity = Vector2.zero;
        }

        private void UpdatePrevData()
        {
            if (m_content == null)
                m_prevPosition = Vector2.zero;
            else
                m_prevPosition = m_content.anchoredPosition;
            m_prevViewBounds = m_viewBounds;
            m_prevContentBounds = m_contentBounds;
        }

        private static float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1)))
                   * viewSize * Mathf.Sign(overStretching);
        }

        #endregion

        #region ICanvasElement

        public void Rebuild(CanvasUpdate executing)
        {
            if (executing == CanvasUpdate.Prelayout)
            {
                UpdateCachedData();
            }

            if (executing == CanvasUpdate.PostLayout)
            {
                UpdateBounds();
                UpdatePrevData();
                m_hasRebuiltLayout = true;
            }
        }

        public void LayoutComplete() { }

        public void GraphicUpdateComplete() { }

        #endregion

        #region ILayoutElement

        public float minWidth => -1;
        public float preferredWidth => -1;
        public float flexibleWidth => -1;
        public float minHeight => -1;
        public float preferredHeight => -1;
        public float flexibleHeight => -1;
        public int layoutPriority => -1;

        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }

        #endregion

        #region ILayoutGroup

        public void SetLayoutHorizontal()
        {
            m_tracker.Clear();
            UpdateCachedData();

            // Viewport RectTransform을 항상 점유: Anchors(0,0~1,1), Position(0,0), SizeDelta(0,0)
            m_tracker.Add(this, viewRect,
                DrivenTransformProperties.Anchors |
                DrivenTransformProperties.SizeDelta |
                DrivenTransformProperties.AnchoredPosition);

            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.sizeDelta = Vector2.zero;
            viewRect.anchoredPosition = Vector2.zero;

            // Content RectTransform 점유
            // Pivot, Anchors, 보조축 SizeDelta: 항상 점유
            // 주축 SizeDelta: 플레이 중 LoadData 호출 후에만 점유
            if (m_content)
            {
                var drivenProps = DrivenTransformProperties.Pivot |
                                  DrivenTransformProperties.Anchors;

                if (ScrollAxis == eScrollAxis.VERTICAL)
                {
                    drivenProps |= DrivenTransformProperties.SizeDeltaX;
                    if (Application.isPlaying && m_isInitialized)
                        drivenProps |= DrivenTransformProperties.SizeDeltaY;

                    m_content.anchorMin = new Vector2(0f, 1f);
                    m_content.anchorMax = new Vector2(1f, 1f);
                    m_content.sizeDelta = new Vector2(0f, m_content.sizeDelta.y);
                }
                else
                {
                    drivenProps |= DrivenTransformProperties.SizeDeltaY;
                    if (Application.isPlaying && m_isInitialized)
                        drivenProps |= DrivenTransformProperties.SizeDeltaX;

                    m_content.anchorMin = new Vector2(0f, 0f);
                    m_content.anchorMax = new Vector2(0f, 1f);
                    m_content.sizeDelta = new Vector2(m_content.sizeDelta.x, 0f);
                }

                m_tracker.Add(this, m_content, drivenProps);
                m_content.pivot = GetAlignmentPoint();
            }

            // 주축 스크롤바 RectTransform 점유 (스크롤 방향으로 stretch)
            if (m_mainAxisScrollbarRect)
            {
                if (ScrollAxis == eScrollAxis.VERTICAL)
                {
                    m_tracker.Add(this, m_mainAxisScrollbarRect,
                        DrivenTransformProperties.AnchorMinY |
                        DrivenTransformProperties.AnchorMaxY |
                        DrivenTransformProperties.SizeDeltaY |
                        DrivenTransformProperties.AnchoredPositionY);
                    m_mainAxisScrollbarRect.anchorMin = new Vector2(m_mainAxisScrollbarRect.anchorMin.x, 0);
                    m_mainAxisScrollbarRect.anchorMax = new Vector2(m_mainAxisScrollbarRect.anchorMax.x, 1);
                    m_mainAxisScrollbarRect.anchoredPosition = new Vector2(m_mainAxisScrollbarRect.anchoredPosition.x, 0);
                    m_mainAxisScrollbarRect.sizeDelta = new Vector2(m_mainAxisScrollbarRect.sizeDelta.x, 0);
                }
                else
                {
                    m_tracker.Add(this, m_mainAxisScrollbarRect,
                        DrivenTransformProperties.AnchorMinX |
                        DrivenTransformProperties.AnchorMaxX |
                        DrivenTransformProperties.SizeDeltaX |
                        DrivenTransformProperties.AnchoredPositionX);
                    m_mainAxisScrollbarRect.anchorMin = new Vector2(0, m_mainAxisScrollbarRect.anchorMin.y);
                    m_mainAxisScrollbarRect.anchorMax = new Vector2(1, m_mainAxisScrollbarRect.anchorMax.y);
                    m_mainAxisScrollbarRect.anchoredPosition = new Vector2(0, m_mainAxisScrollbarRect.anchoredPosition.y);
                    m_mainAxisScrollbarRect.sizeDelta = new Vector2(0, m_mainAxisScrollbarRect.sizeDelta.y);
                }
            }

            m_viewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            m_contentBounds = GetBounds();

            // 스크롤바가 존재하면 viewport를 스크롤바 크기만큼 축소
            // Permanent: 항상 축소, AutoHide/AutoHideAndExpand: 스크롤 필요 시 축소
            bool isPermanent = m_scrollbarVisibility == ScrollRect.ScrollbarVisibility.Permanent;

            if (m_vSliderExpand && (isPermanent || vScrollingNeeded))
            {
                viewRect.sizeDelta = new Vector2(-m_vSliderWidth, viewRect.sizeDelta.y);
                m_viewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
                m_contentBounds = GetBounds();
            }

            if (m_hSliderExpand && (isPermanent || hScrollingNeeded))
            {
                viewRect.sizeDelta = new Vector2(viewRect.sizeDelta.x, -m_hSliderHeight);
                m_viewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
                m_contentBounds = GetBounds();
            }

            // 수평 스크롤바가 추가된 후 수직 스크롤바가 다시 필요해졌는지 재확인
            if (m_vSliderExpand && (isPermanent || vScrollingNeeded) && viewRect.sizeDelta.x == 0 && viewRect.sizeDelta.y < 0)
            {
                viewRect.sizeDelta = new Vector2(-m_vSliderWidth, viewRect.sizeDelta.y);
            }
        }

        public void SetLayoutVertical()
        {
            m_viewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            m_contentBounds = GetBounds();
            UpdateScrollbarSizeFromRect();
        }

        #endregion

        #region ScrollRect Scrollbar Visibility & Layout

        private void UpdateCachedData()
        {
            var mainScrollbar = MainAxisScrollbar;
            m_mainAxisScrollbarRect = mainScrollbar != null
                ? mainScrollbar.transform as RectTransform
                : null;

            bool viewIsChild = (viewRect.parent == transform);
            bool scrollbarIsChild = (!m_mainAxisScrollbarRect || m_mainAxisScrollbarRect.parent == transform);
            bool allAreChildren = viewIsChild && scrollbarIsChild;

            bool hasScrollbar = m_useScrollbar && allAreChildren && m_mainAxisScrollbarRect;
            bool shouldExpand = hasScrollbar
                && m_scrollbarVisibility != ScrollRect.ScrollbarVisibility.AutoHide;

            if (ScrollAxis == eScrollAxis.VERTICAL)
            {
                m_vSliderExpand = shouldExpand;
                m_hSliderExpand = false;
                m_vSliderWidth = m_mainAxisScrollbarRect != null ? m_mainAxisScrollbarRect.rect.width : 0;
                m_hSliderHeight = 0;
            }
            else
            {
                m_hSliderExpand = shouldExpand;
                m_vSliderExpand = false;
                m_hSliderHeight = m_mainAxisScrollbarRect != null ? m_mainAxisScrollbarRect.rect.height : 0;
                m_vSliderWidth = 0;
            }
        }

        private void UpdateScrollbarVisibility()
        {
            var mainScrollbar = MainAxisScrollbar;
            if (mainScrollbar == null) return;

            if (m_useScrollbar == false)
            {
                if (mainScrollbar.gameObject.activeSelf)
                    mainScrollbar.gameObject.SetActive(false);
                return;
            }

            if (m_scrollbarVisibility == ScrollRect.ScrollbarVisibility.Permanent)
            {
                if (mainScrollbar.gameObject.activeSelf == false)
                    mainScrollbar.gameObject.SetActive(true);
            }
            else
            {
                bool scrollingNeeded = MainAxisScrollingNeeded;
                if (mainScrollbar.gameObject.activeSelf != scrollingNeeded)
                    mainScrollbar.gameObject.SetActive(scrollingNeeded);
            }
        }

        private void HideCrossAxisScrollbar()
        {
            var crossScrollbar = CrossAxisScrollbar;
            if (crossScrollbar != null)
                crossScrollbar.gameObject.SetActive(false);
        }

        #endregion

        #region ScrollRect Dirty

        private void SetDirty()
        {
            if (IsActive() == false)
                return;

            LayoutRebuilder.MarkLayoutForRebuild(_RectTransform);
        }

        private void SetDirtyCaching()
        {
            if (IsActive() == false)
                return;

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            LayoutRebuilder.MarkLayoutForRebuild(_RectTransform);
            m_viewRect = null;
        }

        #endregion
    }
}
