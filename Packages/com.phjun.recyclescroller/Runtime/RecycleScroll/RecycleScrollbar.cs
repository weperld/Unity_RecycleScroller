using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecycleScroll
{
    /// <summary>
    /// Scrollbar를 상속하지 않고 Selectable에서 직접 구현한 루프 스크롤바.
    /// Scrollbar.UpdateDrag의 절대 좌표 기반 값 계산이 루프 재배치와 충돌하는 문제를 해결하기 위해,
    /// 루프 모드에서는 delta 기반 드래그 계산을 사용합니다.
    /// </summary>
    [AddComponentMenu("UI/Recycle Scrollbar", 36)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class RecycleScrollbar : Selectable, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler, ICanvasElement
    {
        #region Enums & Event Types

        public enum Direction
        {
            LeftToRight,
            RightToLeft,
            BottomToTop,
            TopToBottom,
        }

        private enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }

        [Serializable]
        public class ScrollEvent : UnityEvent<float> { }

        /// <summary>
        /// normalized real value, normalized showing value
        /// </summary>
        [Serializable]
        public class LoopScrollEvent : UnityEvent<float, float> { }

        [Serializable]
        public class BeginDragEvent : UnityEvent<PointerEventData> { }

        [Serializable]
        public class EndDragEvent : UnityEvent<PointerEventData> { }

        #endregion

        #region Serialized Fields - Core Scrollbar

        [SerializeField] private RectTransform m_HandleRect;
        [SerializeField] private Direction m_Direction = Direction.LeftToRight;

        [Range(0f, 1f)]
        [SerializeField] private float m_Value;

        [Range(0f, 1f)]
        [SerializeField] private float m_Size = 0.2f;

        [Range(0, 11)]
        [SerializeField] private int m_NumberOfSteps = 0;

        [Space(6)]
        [SerializeField] private ScrollEvent m_OnValueChanged = new ScrollEvent();

        #endregion

        #region Serialized Fields - Loop Scrollbar

        [SerializeField] private LoopScrollEvent m_onLoopValueChanged = new();
        [SerializeField] private BeginDragEvent m_onBeginDragged = new();
        [SerializeField] private EndDragEvent m_onEndDragged = new();

        [SerializeField] private RectTransform m_leftHandle;
        [SerializeField] private RectTransform m_rightHandle;
        [SerializeField] private Graphic[] m_graphics;

        #endregion

        #region Editor Fields

#if UNITY_EDITOR
        [SerializeField] private bool m_loopScrollSettingFoldout = false;
        [SerializeField] private bool m_eventFoldout = false;
#endif

        #endregion

        #region Internal Fields

        private RectTransform m_ContainerRect;
        private Vector2 m_Offset = Vector2.zero;

#pragma warning disable 649
        private DrivenRectTransformTracker m_Tracker;
#pragma warning restore 649

        private Coroutine m_PointerDownRepeat;
        private bool isPointerDownAndNotDragging = false;
        private bool m_DelayedUpdateVisuals = false;

        /// <summary>
        /// 루프 모드 delta 기반 드래그를 위한 이전 프레임 로컬 커서 위치
        /// </summary>
        private Vector2? m_PrevDragLocalCursor = null;

        private IRecycleScrollbarDelegate del;

        #endregion

        #region Properties - Core Scrollbar

        public RectTransform handleRect
        {
            get => m_HandleRect;
            set
            {
                if (m_HandleRect == value) return;
                m_HandleRect = value;
                UpdateCachedReferences();
                UpdateVisuals();
            }
        }

        public Direction direction
        {
            get => m_Direction;
            set
            {
                if (m_Direction == value) return;
                m_Direction = value;
                UpdateVisuals();
            }
        }

        public float value
        {
            get
            {
                float val = m_Value;
                if (m_NumberOfSteps > 1)
                    val = Mathf.Round(val * (m_NumberOfSteps - 1)) / (m_NumberOfSteps - 1);
                return val;
            }
            set => Set(value);
        }

        public float size
        {
            get => m_Size;
            set
            {
                var clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(m_Size, clamped)) return;
                m_Size = clamped;
                UpdateVisuals();
            }
        }

        public int numberOfSteps
        {
            get => m_NumberOfSteps;
            set
            {
                if (m_NumberOfSteps == value) return;
                m_NumberOfSteps = value;
                Set(m_Value);
                UpdateVisuals();
            }
        }

        public ScrollEvent onValueChanged
        {
            get => m_OnValueChanged;
            set => m_OnValueChanged = value;
        }

        private float stepSize => (m_NumberOfSteps > 1) ? 1f / (m_NumberOfSteps - 1) : 0.1f;

        private Axis axis => (m_Direction == Direction.LeftToRight || m_Direction == Direction.RightToLeft)
            ? Axis.Horizontal
            : Axis.Vertical;

        private bool reverseValue => m_Direction == Direction.RightToLeft || m_Direction == Direction.TopToBottom;

        #endregion

        #region Properties - Loop Scrollbar

        /// <summary>
        /// normalized real value, normalized showing value
        /// </summary>
        public LoopScrollEvent OnLoopValueChanged => m_onLoopValueChanged;
        public BeginDragEvent OnBeginDragged => m_onBeginDragged;
        public EndDragEvent OnEndDragged => m_onEndDragged;

        public RectTransform rectTransform => transform as RectTransform;

        public IRecycleScrollbarDelegate Del
        {
            get => del;
            set
            {
                del = value;
                if (del == null) return;

                UpdateSlideArea();
                CreateHandles();
            }
        }

        private RectTransform HandleContainerRect => handleRect?.parent as RectTransform;
        private RectTransform LoopSlidingAreaRect => HandleContainerRect?.parent as RectTransform;

        private float ScrollbarRectSize => direction switch
        {
            Direction.LeftToRight or Direction.RightToLeft => rectTransform.rect.size.x,
            Direction.BottomToTop or Direction.TopToBottom => rectTransform.rect.size.y,
            _ => 0f,
        };

        private bool IsLoopMode => del != null && del.IsLoopScrollable;

        #endregion

        #region Constructor

        protected RecycleScrollbar() { }

        #endregion

        #region Unity Lifecycle

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            m_Size = Mathf.Clamp01(m_Size);

            if (IsActive())
            {
                UpdateCachedReferences();
                Set(m_Value, false);
                m_DelayedUpdateVisuals = true;
            }

            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateCachedReferences();
            Set(m_Value, false);
            UpdateVisuals();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            base.OnDisable();
        }

        protected virtual void Update()
        {
            if (m_DelayedUpdateVisuals)
            {
                m_DelayedUpdateVisuals = false;
                UpdateVisuals();
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            if (!IsActive())
                return;

            UpdateVisuals();
        }

        #endregion

        #region ICanvasElement

        public virtual void Rebuild(CanvasUpdate executing)
        {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
                onValueChanged.Invoke(value);
#endif
        }

        public virtual void LayoutComplete() { }

        public virtual void GraphicUpdateComplete() { }

        #endregion

        #region Core Logic

        private void UpdateCachedReferences()
        {
            if (m_HandleRect && m_HandleRect.parent != null)
                m_ContainerRect = m_HandleRect.parent.GetComponent<RectTransform>();
            else
                m_ContainerRect = null;
        }

        private void Set(float input, bool sendCallback = true)
        {
            float currentValue = m_Value;

            // Scrollbar 원본 주석: 관성(inertia)으로 인해 extremities를 벗어날 수 있으므로 여기서는 clamp하지 않음
            m_Value = input;

            // stepped value가 이전과 같으면 업데이트하지 않음
            if (currentValue == value)
                return;

            UpdateVisuals();
            if (sendCallback)
            {
                m_OnValueChanged.Invoke(value);
                UpdateLoopScrollState(value);
            }
        }

        /// <summary>
        /// value 변경 시 루프 스크롤바 관련 시각적 업데이트 및 OnLoopValueChanged 이벤트를 발사합니다.
        /// Set()에서 sendCallback=true일 때 자동으로 호출되므로 인스펙터 등록이 필요하지 않습니다.
        /// </summary>
        private void UpdateLoopScrollState(float val)
        {
            if (!Application.isPlaying) return;

            UpdateSlideArea();
            UpdateLoopHandles();

            if (del == null)
            {
                OnLoopValueChanged.Invoke(val, val);
                return;
            }

            var valueToRealSize = val * del.RealSize;
            var normalizedShowingValue = del.ConvertRealToShow(valueToRealSize) / del.ShowingSize;
            OnLoopValueChanged.Invoke(val, normalizedShowingValue);
        }

        public virtual void SetValueWithoutNotify(float input)
        {
            Set(input, false);
            UpdateLoopHandles();
        }

        /// <summary>
        /// 핸들의 앵커 위치를 value와 size 기반으로 갱신.
        /// Sliding Area와 Handle의 오프셋을 DrivenRectTransformTracker로 자동 강제하여
        /// 수동으로 Left/Right/Top/Bottom을 0으로 설정할 필요가 없습니다.
        /// </summary>
        private void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif
            m_Tracker.Clear();

            if (m_ContainerRect != null)
            {
                // Sliding Area(HandleContainerRect) 오프셋 강제: 부모(LoopSlidingArea)를 정확히 채움
                m_Tracker.Add(this, m_ContainerRect,
                    DrivenTransformProperties.Anchors
                    | DrivenTransformProperties.AnchoredPosition
                    | DrivenTransformProperties.SizeDelta);
                m_ContainerRect.anchorMin = Vector2.zero;
                m_ContainerRect.anchorMax = Vector2.one;
                m_ContainerRect.anchoredPosition = Vector2.zero;
                m_ContainerRect.sizeDelta = Vector2.zero;

                // Handle 오프셋 강제 + 앵커 기반 위치/크기 설정
                m_Tracker.Add(this, m_HandleRect,
                    DrivenTransformProperties.Anchors
                    | DrivenTransformProperties.AnchoredPosition
                    | DrivenTransformProperties.SizeDelta);

                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                float movement = Mathf.Clamp01(value) * (1 - size);
                if (reverseValue)
                {
                    anchorMin[(int)axis] = 1 - movement - size;
                    anchorMax[(int)axis] = 1 - movement;
                }
                else
                {
                    anchorMin[(int)axis] = movement;
                    anchorMax[(int)axis] = movement + size;
                }

                m_HandleRect.anchorMin = anchorMin;
                m_HandleRect.anchorMax = anchorMax;
                m_HandleRect.anchoredPosition = Vector2.zero;
                m_HandleRect.sizeDelta = Vector2.zero;
            }
        }

        private bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        #endregion

        #region Drag Handling

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            isPointerDownAndNotDragging = false;

            if (!MayDrag(eventData))
                return;

            if (m_ContainerRect == null)
                return;

            if (IsLoopMode)
            {
                // 루프 모드: delta 추적용 초기 커서 위치 기록
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_ContainerRect, eventData.position, eventData.pressEventCamera, out var localCursor))
                    m_PrevDragLocalCursor = localCursor;
                else
                    m_PrevDragLocalCursor = null;
            }
            else
            {
                // 비루프 모드: 기존 Scrollbar의 절대 위치 오프셋 계산
                m_Offset = Vector2.zero;
                if (RectTransformUtility.RectangleContainsScreenPoint(
                    m_HandleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        m_HandleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out var localMousePos))
                        m_Offset = localMousePos - m_HandleRect.rect.center;
                }
            }

            if (Application.isPlaying)
                OnBeginDragged.Invoke(eventData);
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            if (m_ContainerRect != null)
            {
                if (IsLoopMode)
                    UpdateDragForLoop(eventData);
                else
                    UpdateDrag(eventData);
            }
            // UpdateSlideArea, UpdateLoopHandles는 Set() → UpdateLoopScrollState()에서 자동 호출
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            m_PrevDragLocalCursor = null;

            if (Application.isPlaying)
                OnEndDragged.Invoke(eventData);
        }

        /// <summary>
        /// 비루프 모드 - 기존 Scrollbar의 절대 좌표 기반 드래그 로직
        /// </summary>
        private void UpdateDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (m_ContainerRect == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_ContainerRect, eventData.position, eventData.pressEventCamera, out var localCursor))
                return;

            Vector2 handleCenterRelativeToContainerCorner = localCursor - m_Offset - m_ContainerRect.rect.position;
            Vector2 handleCorner = handleCenterRelativeToContainerCorner - (m_HandleRect.rect.size - m_HandleRect.sizeDelta) * 0.5f;

            float parentSize = axis == Axis.Horizontal ? m_ContainerRect.rect.width : m_ContainerRect.rect.height;
            float remainingSize = parentSize * (1 - size);
            if (remainingSize <= 0)
                return;

            switch (m_Direction)
            {
                case Direction.LeftToRight:
                    Set(Mathf.Clamp01(handleCorner.x / remainingSize));
                    break;
                case Direction.RightToLeft:
                    Set(Mathf.Clamp01(1f - (handleCorner.x / remainingSize)));
                    break;
                case Direction.BottomToTop:
                    Set(Mathf.Clamp01(handleCorner.y / remainingSize));
                    break;
                case Direction.TopToBottom:
                    Set(Mathf.Clamp01(1f - (handleCorner.y / remainingSize)));
                    break;
            }
        }

        /// <summary>
        /// 루프 모드 - delta 기반 드래그 로직.
        /// 절대 좌표 대신 마우스 이동량(delta)으로 value를 계산하여,
        /// 루프 재배치(ShowingScrollPosition = ShowingScrollPosition)와의 피드백 루프를 방지합니다.
        ///
        /// 기존 Scrollbar.UpdateDrag는 절대 좌표 → 절대 value 매핑 방식이라,
        /// 루프 재배치로 value가 점프해도 다음 프레임에 마우스 위치 기반으로 원래 value를 복원해버려
        /// 스크롤이 멈추는 문제가 있었습니다.
        /// </summary>
        private void UpdateDragForLoop(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (m_ContainerRect == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_ContainerRect, eventData.position, eventData.pressEventCamera, out var localCursor))
                return;

            if (!m_PrevDragLocalCursor.HasValue)
            {
                m_PrevDragLocalCursor = localCursor;
                return;
            }

            Vector2 delta = localCursor - m_PrevDragLocalCursor.Value;
            m_PrevDragLocalCursor = localCursor;

            float axisDelta = axis == Axis.Horizontal ? delta.x : delta.y;
            if (reverseValue) axisDelta = -axisDelta;

            float parentSize = axis == Axis.Horizontal ? m_ContainerRect.rect.width : m_ContainerRect.rect.height;
            float remainingSize = parentSize * (1 - size);
            if (remainingSize <= 0)
                return;

            float valueDelta = axisDelta / remainingSize;
            float newValue = m_Value + valueDelta;

            // 루프 모드에서는 값을 wrapping (0~1 순환)
            newValue %= 1f;
            if (newValue < 0f) newValue += 1f;

            Set(newValue);
        }

        #endregion

        #region Pointer Handling

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            base.OnPointerDown(eventData);
            isPointerDownAndNotDragging = true;
            m_PointerDownRepeat = StartCoroutine(ClickRepeat(eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera));
        }

        protected IEnumerator ClickRepeat(PointerEventData eventData)
        {
            return ClickRepeat(eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera);
        }

        protected IEnumerator ClickRepeat(Vector2 screenPosition, Camera camera)
        {
            while (isPointerDownAndNotDragging)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(m_HandleRect, screenPosition, camera))
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        m_HandleRect, screenPosition, camera, out var localMousePos))
                    {
                        var axisCoordinate = axis == Axis.Horizontal ? localMousePos.x : localMousePos.y;

                        float change = axisCoordinate < 0 ? size : -size;
                        float newValue = value + (reverseValue ? change : -change);

                        if (IsLoopMode)
                        {
                            newValue %= 1f;
                            if (newValue < 0f) newValue += 1f;
                        }
                        else
                        {
                            newValue = Mathf.Clamp01(newValue);
                            newValue = Mathf.Round(newValue * 10000f) / 10000f;
                        }

                        value = newValue;
                    }
                }

                yield return new WaitForEndOfFrame();
            }

            StopCoroutine(m_PointerDownRepeat);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            isPointerDownAndNotDragging = false;
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        #endregion

        #region Navigation

        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if (axis == Axis.Horizontal && FindSelectableOnLeft() == null)
                        Set(Mathf.Clamp01(reverseValue ? value + stepSize : value - stepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Right:
                    if (axis == Axis.Horizontal && FindSelectableOnRight() == null)
                        Set(Mathf.Clamp01(reverseValue ? value - stepSize : value + stepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Up:
                    if (axis == Axis.Vertical && FindSelectableOnUp() == null)
                        Set(Mathf.Clamp01(reverseValue ? value - stepSize : value + stepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Down:
                    if (axis == Axis.Vertical && FindSelectableOnDown() == null)
                        Set(Mathf.Clamp01(reverseValue ? value + stepSize : value - stepSize));
                    else
                        base.OnMove(eventData);
                    break;
            }
        }

        public override Selectable FindSelectableOnLeft()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnLeft();
        }

        public override Selectable FindSelectableOnRight()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnRight();
        }

        public override Selectable FindSelectableOnUp()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnUp();
        }

        public override Selectable FindSelectableOnDown()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnDown();
        }

        #endregion

        #region Direction Utility

        public void SetDirection(Direction direction, bool includeRectLayouts)
        {
            Axis oldAxis = axis;
            bool oldReverse = reverseValue;
            this.direction = direction;

            if (!includeRectLayouts)
                return;

            if (axis != oldAxis)
                RectTransformUtility.FlipLayoutAxes(transform as RectTransform, true, true);

            if (reverseValue != oldReverse)
                RectTransformUtility.FlipLayoutOnAxis(transform as RectTransform, (int)axis, true, true);
        }

        #endregion

        #region Loop Scrollbar - Set Value

        public void SetValueForLoop(float input)
        {
            SetValueWithoutNotify(input);
        }

        public void Refresh()
        {
            UpdateSlideArea();
            UpdateLoopHandles();
            ResetSubHandlesPosition();
        }

        public void SetSize(float size)
        {
            this.size = size;
            Refresh();
        }

        #endregion

        #region Loop Scrollbar - Handle Updates

        private void UpdateLoopHandles(float value, float size)
        {
            var rectSize = handleRect.rect.size;
            if (m_leftHandle) m_leftHandle.sizeDelta = rectSize;
            if (m_rightHandle) m_rightHandle.sizeDelta = rectSize;
        }

        private void UpdateLoopHandles() => UpdateLoopHandles(value, size);

        private void ResetSubHandlePosition(RectTransform subHandle, bool isLeft)
        {
            if (subHandle == null) return;

            subHandle.gameObject.SetActive(Del.IsLoopScrollable);
            if (Del.IsLoopScrollable == false) return;

            var handlePosition = Vector2.zero;
            switch (direction)
            {
                case Direction.BottomToTop or Direction.TopToBottom:
                    handlePosition.y = isLeft ? -ScrollbarRectSize : ScrollbarRectSize;
                    break;
                case Direction.LeftToRight or Direction.RightToLeft:
                    handlePosition.x = isLeft ? -ScrollbarRectSize : ScrollbarRectSize;
                    break;
            }

            var vec2 = Vector2.one * 0.5f;
            subHandle.pivot = vec2;
            subHandle.anchorMin = vec2;
            subHandle.anchorMax = vec2;
            subHandle.anchoredPosition = handlePosition;
        }

        private void ResetSubHandlesPosition()
        {
            ResetSubHandlePosition(m_leftHandle, true);
            ResetSubHandlePosition(m_rightHandle, false);
        }

        private void UpdateSlideArea()
        {
            if (Del == null) return;

            var realSize = Del.RealSize;
            var showingSize = Del.ShowingSize;
            var normalized = realSize / showingSize;
            if (float.IsNaN(normalized)) return;

            var scrollRectSize = ScrollbarRectSize;
            var realRectSize = scrollRectSize * normalized;
            var diff = realRectSize - scrollRectSize;
            var sizeDelta = LoopSlidingAreaRect.sizeDelta;
            switch (direction)
            {
                case Direction.BottomToTop or Direction.TopToBottom:
                    sizeDelta.y = diff;
                    break;
                case Direction.LeftToRight or Direction.RightToLeft:
                    sizeDelta.x = diff;
                    break;
            }

            LoopSlidingAreaRect.sizeDelta = sizeDelta;
        }

        private void CreateHandles()
        {
            if (handleRect == null) return;

            CreateSubHandle(ref m_leftHandle, "Sub Handle 0");
            CreateSubHandle(ref m_rightHandle, "Sub Handle 1");

            m_leftHandle.SetParent(handleRect);
            m_rightHandle.SetParent(handleRect);

            ResetSubHandlesPosition();
            UpdateLoopHandles();
            return;

            void CreateSubHandle(ref RectTransform subHandle, string name)
            {
                if (subHandle == null) subHandle = Instantiate(handleRect, HandleContainerRect);
                subHandle.name = name;
                subHandle.gameObject.SetActive(true);
            }
        }

        #endregion

        #region Graphic Transition

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            if (!gameObject.activeInHierarchy)
                return;

            Color tintColor;
            Sprite transitionSprite;

            switch (state)
            {
                case SelectionState.Normal:
                    tintColor = colors.normalColor;
                    transitionSprite = null;
                    break;
                case SelectionState.Highlighted:
                    tintColor = colors.highlightedColor;
                    transitionSprite = spriteState.highlightedSprite;
                    break;
                case SelectionState.Pressed:
                    tintColor = colors.pressedColor;
                    transitionSprite = spriteState.pressedSprite;
                    break;
                case SelectionState.Selected:
                    tintColor = colors.selectedColor;
                    transitionSprite = spriteState.selectedSprite;
                    break;
                case SelectionState.Disabled:
                    tintColor = colors.disabledColor;
                    transitionSprite = spriteState.disabledSprite;
                    break;
                default:
                    tintColor = Color.black;
                    transitionSprite = null;
                    break;
            }

            switch (transition)
            {
                case Transition.ColorTint:
                    StartColorTween(tintColor * colors.colorMultiplier, instant);
                    break;
                case Transition.SpriteSwap:
                    DoSpriteSwap(transitionSprite);
                    break;
            }
        }

        private void StartColorTween(Color targetColor, bool instant)
        {
            if (m_graphics == null) return;

            foreach (var graphic in m_graphics)
                graphic.CrossFadeColor(targetColor, instant ? 0f : colors.fadeDuration, true, true);
        }

        private void DoSpriteSwap(Sprite newSprite)
        {
            if (m_graphics == null) return;

            foreach (var graphic in m_graphics)
            {
                var graphicImg = graphic as Image;
                if (graphicImg == null) continue;

                graphicImg.overrideSprite = newSprite;
            }
        }

        #endregion
    }
}
