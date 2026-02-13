// Unity Scrollbar 소스 코드 (참조용)
// 출처: UnityEngine.UI (com.unity.ugui)
//
// [참고 포인트 - 예정 작업 관련]
//
// 1. UpdateVisuals() - 핸들 위치/사이즈 배치 핵심 로직
//    - movement = Clamp01(value) * (1 - size)
//    - anchor 기반으로 핸들 위치와 사이즈를 동시에 제어
//    - RecycleScrollbar의 UpdateVisuals와 비교 참고
//
// 2. Set() - value 설정 시 Clamp01 하지 않음 (주석 참고)
//    - "clamp01 input in callee before calling this function"
//    - "this allows inertia from dragging content to go past extremities without being clamped"
//    - → ScrollRect의 Elastic 오버슈트가 value를 통해 전달됨
//
// 3. size 프로퍼티 - ScrollRect에서 elastic offset에 따라 동적으로 조정
//    - size setter: Clamp01 적용
//    - ScrollRect.UpdateScrollbars()에서: size = (ViewSize - Abs(offset)) / ContentSize
//
// 4. UpdateDrag() / DoUpdateDrag() - 드래그 처리
//    - 절대 위치 기반 (delta 아닌 position)
//    - Clamp01 적용
//    - RecycleScrollbar의 UpdateDragForLoop()와 비교 참고
//
// 5. ClickRepeat() - 핸들 외부 클릭 시 페이지 이동
//    - value += size (한 페이지씩 이동)
//    - Clamp01 + Round 4자리

using System;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Scrollbar", 36)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class Scrollbar : Selectable, IBeginDragHandler, IDragHandler, IInitializePotentialDragHandler, ICanvasElement
    {
        public enum Direction
        {
            LeftToRight,
            RightToLeft,
            BottomToTop,
            TopToBottom,
        }

        [Serializable]
        public class ScrollEvent : UnityEvent<float> {}

        [SerializeField]
        private RectTransform m_HandleRect;
        public RectTransform handleRect { get { return m_HandleRect; } set { if (SetPropertyUtility.SetClass(ref m_HandleRect, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField]
        private Direction m_Direction = Direction.LeftToRight;
        public Direction direction { get { return m_Direction; } set { if (SetPropertyUtility.SetStruct(ref m_Direction, value)) UpdateVisuals(); } }

        protected Scrollbar()
        {}

        [Range(0f, 1f)]
        [SerializeField]
        private float m_Value;

        public float value
        {
            get
            {
                float val = m_Value;
                if (m_NumberOfSteps > 1)
                    val = Mathf.Round(val * (m_NumberOfSteps - 1)) / (m_NumberOfSteps - 1);
                return val;
            }
            set
            {
                Set(value);
            }
        }

        public virtual void SetValueWithoutNotify(float input)
        {
            Set(input, false);
        }

        [Range(0f, 1f)]
        [SerializeField]
        private float m_Size = 0.2f;

        // ★ size setter: Clamp01 적용
        public float size { get { return m_Size; } set { if (SetPropertyUtility.SetStruct(ref m_Size, Mathf.Clamp01(value))) UpdateVisuals(); } }

        [Range(0, 11)]
        [SerializeField]
        private int m_NumberOfSteps = 0;
        public int numberOfSteps { get { return m_NumberOfSteps; } set { if (SetPropertyUtility.SetStruct(ref m_NumberOfSteps, value)) { Set(m_Value); UpdateVisuals(); } } }

        [Space(6)]

        [SerializeField]
        private ScrollEvent m_OnValueChanged = new ScrollEvent();
        public ScrollEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

        private RectTransform m_ContainerRect;
        private Vector2 m_Offset = Vector2.zero;

        float stepSize { get { return (m_NumberOfSteps > 1) ? 1f / (m_NumberOfSteps - 1) : 0.1f; } }

        #pragma warning disable 649
        private DrivenRectTransformTracker m_Tracker;
        #pragma warning restore 649
        private Coroutine m_PointerDownRepeat;
        private bool isPointerDownAndNotDragging = false;
        private bool m_DelayedUpdateVisuals = false;

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

        public virtual void Rebuild(CanvasUpdate executing)
        {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
                onValueChanged.Invoke(value);
#endif
        }

        public virtual void LayoutComplete()
        {}

        public virtual void GraphicUpdateComplete()
        {}

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

        void UpdateCachedReferences()
        {
            if (m_HandleRect && m_HandleRect.parent != null)
                m_ContainerRect = m_HandleRect.parent.GetComponent<RectTransform>();
            else
                m_ContainerRect = null;
        }

        // ★ 핵심: value를 Clamp01하지 않음 (ScrollRect Elastic 오버슈트 허용)
        void Set(float input, bool sendCallback = true)
        {
            float currentValue = m_Value;

            // bugfix (case 802330) clamp01 input in callee before calling this function,
            // this allows inertia from dragging content to go past extremities without being clamped
            m_Value = input;

            if (currentValue == value)
                return;

            UpdateVisuals();
            if (sendCallback)
            {
                UISystemProfilerApi.AddMarker("Scrollbar.value", this);
                m_OnValueChanged.Invoke(value);
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            if (!IsActive())
                return;

            UpdateVisuals();
        }

        enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }

        Axis axis { get { return (m_Direction == Direction.LeftToRight || m_Direction == Direction.RightToLeft) ? Axis.Horizontal : Axis.Vertical; } }
        bool reverseValue { get { return m_Direction == Direction.RightToLeft || m_Direction == Direction.TopToBottom; } }

        // ★ 핵심: 핸들 위치/사이즈 배치
        // movement = Clamp01(value) * (1 - size) → anchor 기반 배치
        // value가 [0,1] 범위를 넘어도 Clamp01(value)로 시각적으로는 [0,1]에 제한
        // 하지만 size가 줄어들면 핸들이 작아져서 Elastic 피드백 제공
        private void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif
            m_Tracker.Clear();

            if (m_ContainerRect != null)
            {
                m_Tracker.Add(this, m_HandleRect, DrivenTransformProperties.Anchors);
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
            }
        }

        void UpdateDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (m_ContainerRect == null)
                return;

            Vector2 position = Vector2.zero;
            if (!MultipleDisplayUtilities.GetRelativeMousePositionForDrag(eventData, ref position))
                return;

            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ContainerRect, position, eventData.pressEventCamera, out localCursor))
                return;

            Vector2 handleCenterRelativeToContainerCorner = localCursor - m_Offset - m_ContainerRect.rect.position;
            Vector2 handleCorner = handleCenterRelativeToContainerCorner - (m_HandleRect.rect.size - m_HandleRect.sizeDelta) * 0.5f;

            float parentSize = axis == 0 ? m_ContainerRect.rect.width : m_ContainerRect.rect.height;
            float remainingSize = parentSize * (1 - size);
            if (remainingSize <= 0)
                return;

            DoUpdateDrag(handleCorner, remainingSize);
        }

        private void DoUpdateDrag(Vector2 handleCorner, float remainingSize)
        {
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

        private bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            isPointerDownAndNotDragging = false;

            if (!MayDrag(eventData))
                return;

            if (m_ContainerRect == null)
                return;

            m_Offset = Vector2.zero;
            if (RectTransformUtility.RectangleContainsScreenPoint(m_HandleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HandleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out localMousePos))
                    m_Offset = localMousePos - m_HandleRect.rect.center;
            }
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            if (m_ContainerRect != null)
                UpdateDrag(eventData);
        }

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

        // ★ 핸들 외부 클릭 시 한 페이지씩 이동
        protected IEnumerator ClickRepeat(Vector2 screenPosition, Camera camera)
        {
            while (isPointerDownAndNotDragging)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(m_HandleRect, screenPosition, camera))
                {
                    Vector2 localMousePos;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HandleRect, screenPosition, camera, out localMousePos))
                    {
                        var axisCoordinate = axis == 0 ? localMousePos.x : localMousePos.y;

                        float change = axisCoordinate < 0 ? size : -size;
                        value += reverseValue ? change : -change;
                        value = Mathf.Clamp01(value);
                        value = Mathf.Round(value * 10000f) / 10000f;
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

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

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
    }
}
