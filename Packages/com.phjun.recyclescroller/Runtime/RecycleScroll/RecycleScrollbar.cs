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

        /// <summary>
        /// 실제 화면에 표시되는 핸들 크기 비율.
        /// 핸들 최소 사이즈 보장 기능이 켜져 있고, 고정 크기 비율 >= 자연 크기 비율(size)일 때만 고정 크기를 반환합니다.
        /// 그 외에는 기존 size를 그대로 반환합니다.
        /// </summary>
        private float DisplaySize
        {
            get
            {
                if (!m_useFixedHandleSize) return size;

                float fixedRatio = m_fixedHandleSizeMode switch
                {
                    FixedHandleSizeMode.Ratio => m_fixedHandleRatio,
                    FixedHandleSizeMode.PixelSize => ScrollbarRectSize > 0f
                        ? Mathf.Clamp01(m_fixedHandlePixelSize / ScrollbarRectSize)
                        : size,
                    _ => size,
                };

                // 고정 비율 >= 자연 비율일 때만 고정 크기 적용
                // (자연 핸들이 고정 최소 크기보다 작을 때만 최소 크기 보장)
                return fixedRatio >= size ? fixedRatio : size;
            }
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

                CreateHandles();
            }
        }

        private RectTransform HandleContainerRect => handleRect?.parent as RectTransform;

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

            if (del == null || !del.IsLoopScrollable)
            {
                OnLoopValueChanged.Invoke(val, val);
                return;
            }

            // 루프 모드: val은 showing-normalized position
            // OnLoopValueChanged(realNormalized, showingNormalized)
            float showingScrollSize = del.ShowingSize - del.ViewportSize;
            if (showingScrollSize <= 0f)
            {
                OnLoopValueChanged.Invoke(val, val);
                return;
            }

            float showingPos = val * showingScrollSize;
            float realPos = del.ConvertShowToReal(showingPos);
            float realScrollSize = del.RealSize - del.ViewportSize;
            float realNormalized = realScrollSize > 0f ? realPos / realScrollSize : 0f;
            OnLoopValueChanged.Invoke(realNormalized, val);
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

                // 루프 모드: 서브 핸들 트래커 등록
                if (IsLoopMode)
                {
                    if (m_leftHandle)
                        m_Tracker.Add(this, m_leftHandle,
                            DrivenTransformProperties.Anchors
                            | DrivenTransformProperties.AnchoredPosition
                            | DrivenTransformProperties.SizeDelta);
                    if (m_rightHandle)
                        m_Tracker.Add(this, m_rightHandle,
                            DrivenTransformProperties.Anchors
                            | DrivenTransformProperties.AnchoredPosition
                            | DrivenTransformProperties.SizeDelta);
                }

                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                float displaySize = DisplaySize;

                // === 비루프 모드: Elastic 핸들 사이즈 축소 ===
                // value 오버슈트를 감지하여 핸들의 시각적 사이즈를 동적으로 축소.
                // Unity ScrollRect 공식 변환: elasticSize = Clamp01(displaySize - overValue * (1 - size))
                // DisplaySize 프로퍼티 자체는 변경하지 않아 드래그 로직(remainingSize)에 무영향.
                float elasticDisplaySize = displaySize;
                if (!IsLoopMode)
                {
                    float overValue = m_Value < 0f ? -m_Value : (m_Value > 1f ? m_Value - 1f : 0f);
                    if (overValue > 0f)
                        elasticDisplaySize = Mathf.Clamp01(displaySize - overValue * (1f - size));
                }

                // 시각적 사이즈 결정: 비루프는 Elastic 축소 적용, 루프는 기존 displaySize 유지
                float visualSize = IsLoopMode ? displaySize : elasticDisplaySize;

                // 루프 모드: 핸들 이동 범위 계산에 자연 비율(viewport/content)을 사용.
                // displaySize(고정 최소 크기 적용)로 계산하면 서브 핸들과
                // 이동 범위가 불일치하여 wrap 경계에서 핸들이 점프함.
                // naturalSize를 사용하면 value 범위 [0, content/scrollSize]가
                // movement 범위 [0, 1.0]에 정확히 매핑되어 심리스 루프 달성.
                float movementScale;
                if (IsLoopMode && del != null)
                {
                    float naturalSize = del.ShowingSize > 0f ? del.ViewportSize / del.ShowingSize : displaySize;
                    movementScale = 1f - naturalSize;
                }
                else
                {
                    movementScale = 1f - visualSize;
                }
                float movement = (IsLoopMode ? value : Mathf.Clamp01(value)) * movementScale;
                if (reverseValue)
                {
                    anchorMin[(int)axis] = 1 - movement - visualSize;
                    anchorMax[(int)axis] = 1 - movement;
                }
                else
                {
                    anchorMin[(int)axis] = movement;
                    anchorMax[(int)axis] = movement + visualSize;
                }

                // === 루프 모드: 메인 핸들 클램프 + 서브 핸들 사이즈 전환 ===
                // 메인 핸들 anchor가 [0,1] 범위를 넘는 양을 서브 핸들 사이즈로 설정하고,
                // 메인 핸들 자체는 [0,1]로 클램프하여 경계에서 시각적으로 축소.
                // reverseValue가 이미 anchor 계산에 반영되어 Direction 독립적.
                if (IsLoopMode)
                {
                    float startWrap = Mathf.Max(0f, anchorMax[(int)axis] - 1f);
                    float endWrap = Mathf.Max(0f, -anchorMin[(int)axis]);

                    // 메인 핸들을 [0,1] 범위로 클램프 → wrap 부분만큼 실제 사이즈 축소
                    anchorMin[(int)axis] = Mathf.Max(0f, anchorMin[(int)axis]);
                    anchorMax[(int)axis] = Mathf.Min(1f, anchorMax[(int)axis]);

                    UpdateLoopHandles(startWrap, endWrap);
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
            // UpdateLoopHandles는 Set() → UpdateVisuals()에서 자동 호출
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
            float remainingSize = parentSize * (1 - DisplaySize);
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
            float remainingSize = parentSize * (1 - DisplaySize);
            if (remainingSize <= 0)
                return;

            float valueDelta = axisDelta / remainingSize;
            float newValue = m_Value + valueDelta;

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

                        if (!IsLoopMode)
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
                case MovDirection.Left:
                    if (axis == Axis.Horizontal && FindSelectableOnLeft() == null)
                        Set(Mathf.Clamp01(reverseValue ? value + stepSize : value - stepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MovDirection.Right:
                    if (axis == Axis.Horizontal && FindSelectableOnRight() == null)
                        Set(Mathf.Clamp01(reverseValue ? value - stepSize : value + stepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MovDirection.Up:
                    if (axis == Axis.Vertical && FindSelectableOnUp() == null)
                        Set(Mathf.Clamp01(reverseValue ? value - stepSize : value + stepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MovDirection.Down:
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

        public void Refresh()
        {
            ResetSubHandlesPosition();
            UpdateVisuals();
        }

        public void SetSize(float size)
        {
            this.size = size;
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
                aMin[(int)axis] = 0f;
                aMax[(int)axis] = startWrap;
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
                aMin[(int)axis] = 1f - endWrap;
                aMax[(int)axis] = 1f;
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

            bool loopActive = Del != null && Del.IsLoopScrollable;
            subHandle.gameObject.SetActive(loopActive);
            if (!loopActive) return;

            Vector2 aMin = Vector2.zero;
            Vector2 aMax = Vector2.one;

            if (isStartEdge)
            {
                aMin[(int)axis] = 0f;
                aMax[(int)axis] = 0f;
            }
            else
            {
                aMin[(int)axis] = 1f;
                aMax[(int)axis] = 1f;
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
            if (handleRect == null) return;

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
                if (subHandle == null) subHandle = Instantiate(handleRect, HandleContainerRect);
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
            if (m_graphics == null) m_graphics = Array.Empty<Graphic>();

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
