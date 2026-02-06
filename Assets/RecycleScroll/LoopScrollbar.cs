using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecycleScroll
{
    [AddComponentMenu("UI/Loop Scrollbar", 36)]
    [RequireComponent(typeof(RectTransform))]
    public class LoopScrollbar : Scrollbar, IEndDragHandler
    {
        #region Scroll Event
        /// <summary>
        /// normalized real value, normalized showing value
        /// </summary>
        [Serializable]
        public class LoopScrollEvent : UnityEvent<float, float>
        {
        }
        
        [SerializeField] private LoopScrollEvent m_onLoopValueChanged = new();
        /// <summary>
        /// normalized real value, normalized showing value
        /// </summary>
        public LoopScrollEvent OnLoopValueChanged => m_onLoopValueChanged;
        
        [Serializable]
        public class BeginDragEvent : UnityEvent<PointerEventData>
        {
        }
        
        [SerializeField] private BeginDragEvent m_onBeginDragged = new();
        public BeginDragEvent OnBeginDragged => m_onBeginDragged;
        
        [Serializable]
        public class EndDragEvent : UnityEvent<PointerEventData>
        {
        }
        
        [SerializeField] private EndDragEvent m_onEndDragged = new();
        public EndDragEvent OnEndDragged => m_onEndDragged;
        #endregion
        
        #region Vars
        #region SerializeField
        [SerializeField] private RectTransform m_leftHandle;
        [SerializeField] private RectTransform m_rightHandle;
        [SerializeField] private Graphic[] m_graphics;
        #endregion
        
        public RectTransform rectTransform => transform as RectTransform;
        
        private ILoopScrollDelegate del;
        public ILoopScrollDelegate Del
        {
            get => del;
            set
            {
                del = value;
                if (del is null) return;
                
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
        
        #region Editor
        #if UNITY_EDITOR
        [SerializeField] private bool m_loopScrollSettingFoldout = false;
        [SerializeField] private bool m_eventFoldout = false;
        #endif
        #endregion
        #endregion
        
        #region Methods
        #region Event Handler
        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);
            if (Application.isPlaying == false) return;
            
            OnBeginDragged.Invoke(eventData);
        }
        
        // 기본 드래그 로직을 유지하면서도 루프 업데이트 반영
        public override void OnDrag(PointerEventData eventData)
        {
            base.OnDrag(eventData);
            if (Application.isPlaying == false) return;
            
            UpdateSlideArea();
            UpdateLoopHandles();
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (Application.isPlaying == false) return;
            
            OnEndDragged.Invoke(eventData);
        }
        
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
        }
        #endregion
        
        #region Set Value
        public void SetValueForLoop(float input)
        {
            SetValueWithoutNotify(input);
        }
        
        public override void SetValueWithoutNotify(float input)
        {
            base.SetValueWithoutNotify(input);
            UpdateLoopHandles();
        }
        
        /// <summary>
        /// Call from onValueChange of base scrollbar
        /// </summary>
        /// <param name="value"></param>
        public void OnValueChangeSelf(float value)
        {
            if (Application.isPlaying == false) return;
            
            if (Del is null)
            {
                OnLoopValueChanged.Invoke(value, value);
                return;
            }
            
            UpdateSlideArea();
            UpdateLoopHandles();
            
            var valueToRealSize = value;
            valueToRealSize *= del.RealSize;
            
            var normalizedShowingValue = del.ConvertRealToShow(valueToRealSize) / del.ShowingSize;
            OnLoopValueChanged.Invoke(value, normalizedShowingValue);
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
            if (Del is null) return;
            
            var realSize = Del.RealSize;
            var showingSize = Del.ShowingSize;
            var normalized = realSize / showingSize;
            if (normalized is float.NaN) return;
            
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
        #endregion
        
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
        
        #region Graphic Transition
        // 밑의 함수들은 Selectable.cs에서 복붙한 것들임
        
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
        #endregion
    }
}