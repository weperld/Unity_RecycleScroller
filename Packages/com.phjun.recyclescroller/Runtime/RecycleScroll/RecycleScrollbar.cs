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

        public enum FixedHandleSizeMode
        {
            /// <summary>스크롤바 영역 대비 비율로 핸들 최소 크기 지정</summary>
            Ratio,
            /// <summary>픽셀 단위로 핸들 최소 크기 지정</summary>
            PixelSize,
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

        [SerializeField] private RectTransform m_handleRect;
        [SerializeField] private Direction m_direction = Direction.LeftToRight;

        [Range(0f, 1f)]
        [SerializeField] private float m_value;

        [Range(0f, 1f)]
        [SerializeField] private float m_size = 0.2f;

        [Range(0, 11)]
        [SerializeField] private int m_numberOfSteps = 0;

        [Space(6)]
        [SerializeField] private ScrollEvent m_onValueChanged = new ScrollEvent();

        #endregion

        #region Serialized Fields - Fixed Handle Size

        [SerializeField] private bool m_useFixedHandleSize = false;
        [SerializeField] private FixedHandleSizeMode m_fixedHandleSizeMode = FixedHandleSizeMode.Ratio;

        [Range(0.01f, 1f)]
        [SerializeField] private float m_fixedHandleRatio = 0.1f;

        [Min(1f)]
        [SerializeField] private float m_fixedHandlePixelSize = 50f;

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

        private RectTransform m_containerRect;

#pragma warning disable 649
        private DrivenRectTransformTracker m_tracker;
#pragma warning restore 649

        private Coroutine m_pointerDownRepeat;
        private bool m_isPointerDownAndNotDragging = false;
        private bool m_delayedUpdateVisuals = false;

        private IRecycleScrollbarDelegate m_del;
        private IScrollbarMode m_scrollbarMode;
        private IScrollbarMode ScrollbarMode => m_scrollbarMode ??= new NormalScrollbarMode();

        #endregion

        #region Properties - Core Scrollbar

        public RectTransform HandleRect
        {
            get => m_handleRect;
            set
            {
                if (m_handleRect == value) return;
                m_handleRect = value;
                UpdateCachedReferences();
                UpdateVisuals();
            }
        }

        public Direction _Direction
        {
            get => m_direction;
            set
            {
                if (m_direction == value) return;
                m_direction = value;
                UpdateVisuals();
            }
        }

        public float Value
        {
            get
            {
                float val = m_value;
                if (m_numberOfSteps > 1)
                    val = Mathf.Round(val * (m_numberOfSteps - 1)) / (m_numberOfSteps - 1);
                return val;
            }
            set => Set(value);
        }

        public float Size
        {
            get => m_size;
            set
            {
                var clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(m_size, clamped)) return;
                m_size = clamped;
                UpdateVisuals();
            }
        }

        public int NumberOfSteps
        {
            get => m_numberOfSteps;
            set
            {
                if (m_numberOfSteps == value) return;
                m_numberOfSteps = value;
                Set(m_value);
                UpdateVisuals();
            }
        }

        public ScrollEvent OnValueChanged
        {
            get => m_onValueChanged;
            set => m_onValueChanged = value;
        }

        /// <summary>
        /// 실제 화면에 표시되는 핸들 크기 비율.
        /// 핸들 최소 사이즈 보장 기능이 켜져 있고, 고정 크기 비율 >= 자연 크기 비율(size)일 때만 고정 크기를 반환합니다.
        /// 그 외에는 기존 size를 그대로 반환합니다.
        /// </summary>
        private float DisplaySize
        {
            get
            {
                if (!m_useFixedHandleSize) return Size;

                float fixedRatio = m_fixedHandleSizeMode switch
                {
                    FixedHandleSizeMode.Ratio => m_fixedHandleRatio,
                    FixedHandleSizeMode.PixelSize => ScrollbarRectSize > 0f
                        ? Mathf.Clamp01(m_fixedHandlePixelSize / ScrollbarRectSize)
                        : Size,
                    _ => Size,
                };

                // 고정 비율 >= 자연 비율일 때만 고정 크기 적용
                // (자연 핸들이 고정 최소 크기보다 작을 때만 최소 크기 보장)
                return fixedRatio >= Size ? fixedRatio : Size;
            }
        }

        private float StepSize => (m_numberOfSteps > 1) ? 1f / (m_numberOfSteps - 1) : 0.1f;

        private Axis _Axis => (m_direction == Direction.LeftToRight || m_direction == Direction.RightToLeft)
            ? Axis.Horizontal
            : Axis.Vertical;

        private bool ReverseValue => m_direction == Direction.RightToLeft || m_direction == Direction.TopToBottom;

        #endregion

        #region Properties - Loop Scrollbar

        /// <summary>
        /// normalized real value, normalized showing value
        /// </summary>
        public LoopScrollEvent OnLoopValueChanged => m_onLoopValueChanged;
        public BeginDragEvent OnBeginDragged => m_onBeginDragged;
        public EndDragEvent OnEndDragged => m_onEndDragged;

        public RectTransform _RectTransform => transform as RectTransform;

        public IRecycleScrollbarDelegate Del
        {
            get => m_del;
            set
            {
                m_del = value;
                if (m_del == null) return;

                CreateHandles();
            }
        }

        private RectTransform HandleContainerRect => HandleRect?.parent as RectTransform;

        private float ScrollbarRectSize => _Direction switch
        {
            Direction.LeftToRight or Direction.RightToLeft => _RectTransform.rect.size.x,
            Direction.BottomToTop or Direction.TopToBottom => _RectTransform.rect.size.y,
            _ => 0f,
        };

        private bool IsLoopMode => m_del is { IsLoopScrollable: true };

        #endregion

        #region Constructor

        protected RecycleScrollbar() { }

        #endregion

        #region Unity Lifecycle

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            m_size = Mathf.Clamp01(m_size);

            if (IsActive())
            {
                UpdateCachedReferences();
                Set(m_value, false);
                m_delayedUpdateVisuals = true;
            }

            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateCachedReferences();
            Set(m_value, false);
            UpdateVisuals();
        }

        protected override void OnDisable()
        {
            m_tracker.Clear();
            base.OnDisable();
        }

        protected virtual void Update()
        {
            if (m_delayedUpdateVisuals)
            {
                m_delayedUpdateVisuals = false;
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
                OnValueChanged.Invoke(Value);
#endif
        }

        public virtual void LayoutComplete() { }

        public virtual void GraphicUpdateComplete() { }

        #endregion

        #region Core Logic

        private void UpdateCachedReferences()
        {
            if (m_handleRect && m_handleRect.parent != null)
                m_containerRect = m_handleRect.parent.GetComponent<RectTransform>();
            else
                m_containerRect = null;
        }

        private void Set(float input, bool sendCallback = true)
        {
            float currentValue = m_value;

            // Scrollbar 원본 주석: 관성(inertia)으로 인해 extremities를 벗어날 수 있으므로 여기서는 clamp하지 않음
            m_value = input;

            // stepped value가 이전과 같으면 업데이트하지 않음
            if (Mathf.Approximately(currentValue, Value))
                return;

            UpdateVisuals();
            if (sendCallback)
            {
                m_onValueChanged.Invoke(Value);
                UpdateLoopScrollState(Value);
            }
        }

        /// <summary>
        /// value 변경 시 루프 스크롤바 관련 시각적 업데이트 및 OnLoopValueChanged 이벤트를 발사합니다.
        /// Set()에서 sendCallback=true일 때 자동으로 호출되므로 인스펙터 등록이 필요하지 않습니다.
        /// </summary>
        private void UpdateLoopScrollState(float val)
        {
            if (!Application.isPlaying) return;

            var (real, showing) = ScrollbarMode.ConvertValueForEvent(val, m_del);
            OnLoopValueChanged.Invoke(real, showing);
        }

        public virtual void SetValueWithoutNotify(float input)
        {
            Set(input, false);
        }

        /// <summary>
        /// 핸들의 앵커 위치를 value와 size 기반으로 갱신.
        /// Sliding Area와 Handle의 오프셋을 DrivenRectTransformTracker로 자동 강제하여
        /// 수동으로 Left/Right/Top/Bottom을 0으로 설정할 필요가 없습니다.
        ///
        /// 비루프 모드: Elastic 오버슈트 시 핸들 사이즈를 동적 축소.
        /// 루프 모드: 메인 핸들이 [0,1] 경계를 넘을 때 서브 핸들 사이즈를 동적 설정.
        /// </summary>
        private void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif
            m_tracker.Clear();

            if (m_containerRect != null)
            {
                // Sliding Area(HandleContainerRect) 오프셋 강제: 부모(LoopSlidingArea)를 정확히 채움
                m_tracker.Add(this, m_containerRect,
                    DrivenTransformProperties.Anchors
                    | DrivenTransformProperties.AnchoredPosition
                    | DrivenTransformProperties.SizeDelta);
                m_containerRect.anchorMin = Vector2.zero;
                m_containerRect.anchorMax = Vector2.one;
                m_containerRect.anchoredPosition = Vector2.zero;
                m_containerRect.sizeDelta = Vector2.zero;

                // Handle 오프셋 강제 + 앵커 기반 위치/크기 설정
                m_tracker.Add(this, m_handleRect,
                    DrivenTransformProperties.Anchors
                    | DrivenTransformProperties.AnchoredPosition
                    | DrivenTransformProperties.SizeDelta);

                // 모드별 추가 트래커 등록 (루프 모드: 서브 핸들)
                ScrollbarMode.RegisterAdditionalTrackers(
                    ref m_tracker, this, m_leftHandle, m_rightHandle, (int)_Axis);

                float displaySize = DisplaySize;

                // 모드별 핸들 앵커 계산 (비루프: Elastic 축소, 루프: naturalSize + wrap)
                var result = ScrollbarMode.CalculateHandleAnchors(
                    displaySize, Value, Size, m_del, (int)_Axis, ReverseValue);

                // 서브 핸들 wrap 업데이트 (루프 모드에서 메인 핸들이 경계를 넘을 때)
                if (result.HasWrap)
                    UpdateLoopHandles(result.StartWrap, result.EndWrap);

                m_handleRect.anchorMin = result.AnchorMin;
                m_handleRect.anchorMax = result.AnchorMax;
                m_handleRect.anchoredPosition = Vector2.zero;
                m_handleRect.sizeDelta = Vector2.zero;
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
            m_isPointerDownAndNotDragging = false;

            if (!MayDrag(eventData))
                return;

            if (m_containerRect == null)
                return;

            ScrollbarMode.InitDrag(eventData, m_containerRect, m_handleRect);

            if (Application.isPlaying)
                OnBeginDragged.Invoke(eventData);
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            if (m_containerRect != null)
            {
                ScrollbarMode.ProcessDrag(eventData, m_containerRect, m_handleRect,
                    DisplaySize, m_value, (int)_Axis, ReverseValue, Set);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ScrollbarMode.EndDrag();

            if (Application.isPlaying)
                OnEndDragged.Invoke(eventData);
        }

        #endregion

        #region Pointer Handling

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            base.OnPointerDown(eventData);
            m_isPointerDownAndNotDragging = true;
            m_pointerDownRepeat = StartCoroutine(ClickRepeat(eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera));
        }

        protected IEnumerator ClickRepeat(PointerEventData eventData)
        {
            return ClickRepeat(eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera);
        }

        protected IEnumerator ClickRepeat(Vector2 screenPosition, Camera camera)
        {
            while (m_isPointerDownAndNotDragging)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(m_handleRect, screenPosition, camera))
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        m_handleRect, screenPosition, camera, out var localMousePos))
                    {
                        var axisCoordinate = _Axis == Axis.Horizontal ? localMousePos.x : localMousePos.y;

                        float change = axisCoordinate < 0 ? Size : -Size;
                        float newValue = Value + (ReverseValue ? change : -change);

                        newValue = ScrollbarMode.ClampClickValue(newValue);

                        Value = newValue;
                    }
                }

                yield return new WaitForEndOfFrame();
            }

            StopCoroutine(m_pointerDownRepeat);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            m_isPointerDownAndNotDragging = false;
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
                    if (_Axis == Axis.Horizontal && FindSelectableOnLeft() == null)
                        Set(Mathf.Clamp01(ReverseValue ? Value + StepSize : Value - StepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Right:
                    if (_Axis == Axis.Horizontal && FindSelectableOnRight() == null)
                        Set(Mathf.Clamp01(ReverseValue ? Value - StepSize : Value + StepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Up:
                    if (_Axis == Axis.Vertical && FindSelectableOnUp() == null)
                        Set(Mathf.Clamp01(ReverseValue ? Value - StepSize : Value + StepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Down:
                    if (_Axis == Axis.Vertical && FindSelectableOnDown() == null)
                        Set(Mathf.Clamp01(ReverseValue ? Value + StepSize : Value - StepSize));
                    else
                        base.OnMove(eventData);
                    break;
            }
        }

        public override Selectable FindSelectableOnLeft()
        {
            if (navigation.mode == Navigation.Mode.Automatic && _Axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnLeft();
        }

        public override Selectable FindSelectableOnRight()
        {
            if (navigation.mode == Navigation.Mode.Automatic && _Axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnRight();
        }

        public override Selectable FindSelectableOnUp()
        {
            if (navigation.mode == Navigation.Mode.Automatic && _Axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnUp();
        }

        public override Selectable FindSelectableOnDown()
        {
            if (navigation.mode == Navigation.Mode.Automatic && _Axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnDown();
        }

        #endregion

        #region Direction Utility

        public void SetDirection(Direction direction, bool includeRectLayouts)
        {
            Axis oldAxis = _Axis;
            bool oldReverse = ReverseValue;
            this._Direction = direction;

            if (!includeRectLayouts)
                return;

            if (_Axis != oldAxis)
                RectTransformUtility.FlipLayoutAxes(transform as RectTransform, true, true);

            if (ReverseValue != oldReverse)
                RectTransformUtility.FlipLayoutOnAxis(transform as RectTransform, (int)_Axis, true, true);
        }

        #endregion

        #region Loop Scrollbar - Set Value

        public void Refresh()
        {
            m_scrollbarMode = IsLoopMode
                ? new LoopScrollbarMode()
                : new NormalScrollbarMode();
            ResetSubHandlesPosition();
            UpdateVisuals();
        }

        public void SetSize(float size)
        {
            this.Size = size;
            Refresh();
        }

        #endregion

        #region Loop Scrollbar - Handle Updates

        /// <summary>
        /// 루프 모드 서브 핸들의 사이즈를 wrap 양에 따라 동적 설정.
        /// 시작 에지(0 side)와 끝 에지(1 side)에 고정된 서브 핸들의 anchor를 조정하여
        /// 메인 핸들이 [0,1] 경계를 넘을 때 wrap 부분을 표시합니다.
        /// </summary>
        private void UpdateLoopHandles(float startWrap, float endWrap)
        {
            if (m_leftHandle)
            {
                Vector2 aMin = Vector2.zero;
                Vector2 aMax = Vector2.one;
                aMin[(int)_Axis] = 0f;
                aMax[(int)_Axis] = startWrap;
                m_leftHandle.anchorMin = aMin;
                m_leftHandle.anchorMax = aMax;
                m_leftHandle.anchoredPosition = Vector2.zero;
                m_leftHandle.sizeDelta = Vector2.zero;
                m_leftHandle.gameObject.SetActive(startWrap > 0.001f);
            }

            if (m_rightHandle)
            {
                Vector2 aMin = Vector2.zero;
                Vector2 aMax = Vector2.one;
                aMin[(int)_Axis] = 1f - endWrap;
                aMax[(int)_Axis] = 1f;
                m_rightHandle.anchorMin = aMin;
                m_rightHandle.anchorMax = aMax;
                m_rightHandle.anchoredPosition = Vector2.zero;
                m_rightHandle.sizeDelta = Vector2.zero;
                m_rightHandle.gameObject.SetActive(endWrap > 0.001f);
            }
        }

        /// <summary>
        /// 서브 핸들을 anchor 기반으로 에지에 고정 배치합니다.
        /// 시작 에지(isStartEdge=true): anchorMin[axis]=0, anchorMax[axis]=0 (사이즈 0 초기화)
        /// 끝 에지(isStartEdge=false): anchorMin[axis]=1, anchorMax[axis]=1 (사이즈 0 초기화)
        /// 실제 사이즈는 UpdateLoopHandles()에서 wrap 양에 따라 동적 설정됩니다.
        /// </summary>
        private void ResetSubHandlePosition(RectTransform subHandle, bool isStartEdge)
        {
            if (subHandle == null) return;

            bool loopActive = Del is { IsLoopScrollable: true };
            subHandle.gameObject.SetActive(loopActive);
            if (!loopActive) return;

            Vector2 aMin = Vector2.zero;
            Vector2 aMax = Vector2.one;

            if (isStartEdge)
            {
                aMin[(int)_Axis] = 0f;
                aMax[(int)_Axis] = 0f;
            }
            else
            {
                aMin[(int)_Axis] = 1f;
                aMax[(int)_Axis] = 1f;
            }

            subHandle.anchorMin = aMin;
            subHandle.anchorMax = aMax;
            subHandle.pivot = new Vector2(0.5f, 0.5f);
            subHandle.anchoredPosition = Vector2.zero;
            subHandle.sizeDelta = Vector2.zero;
        }

        private void ResetSubHandlesPosition()
        {
            ResetSubHandlePosition(m_leftHandle, true);
            ResetSubHandlePosition(m_rightHandle, false);
        }

        private void CreateHandles()
        {
            if (HandleRect == null) return;

            CreateSubHandle(ref m_leftHandle, "Sub Handle 0");
            CreateSubHandle(ref m_rightHandle, "Sub Handle 1");

            // 서브 핸들을 HandleContainerRect(Sliding Area) 직속 자식으로 배치.
            // 메인 핸들과 독립적인 anchor 기반 사이즈 제어를 위해 동일 부모 레벨에 배치.
            m_leftHandle.SetParent(HandleContainerRect);
            m_rightHandle.SetParent(HandleContainerRect);

            // 서브 핸들의 Graphic을 m_graphics에 동적 등록 (DoStateTransition 색상 연동)
            RegisterSubHandleGraphics();

            ResetSubHandlesPosition();
            return;

            void CreateSubHandle(ref RectTransform subHandle, string name)
            {
                if (subHandle == null) subHandle = Instantiate(HandleRect, HandleContainerRect);
                subHandle.name = name;
                subHandle.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 서브 핸들의 Graphic 컴포넌트를 m_graphics 배열에 등록합니다.
        /// DoStateTransition()에서 색상/스프라이트 전환이 서브 핸들에도 적용되도록 합니다.
        /// </summary>
        private void RegisterSubHandleGraphics()
        {
            m_graphics ??= Array.Empty<Graphic>();

            var newGraphics = new System.Collections.Generic.List<Graphic>(m_graphics);

            RegisterGraphic(m_leftHandle);
            RegisterGraphic(m_rightHandle);

            m_graphics = newGraphics.ToArray();
            return;

            void RegisterGraphic(RectTransform subHandle)
            {
                if (subHandle == null) return;
                var graphic = subHandle.GetComponent<Graphic>();
                if (graphic != null && !newGraphics.Contains(graphic))
                    newGraphics.Add(graphic);
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
