using MathUtils;
using UnityEngine;
using UnityEngine.Serialization;

namespace RecycleScroll
{
    public partial class RecycleScroller
    {
        #region Alignment Values

        [SerializeField] private RectOffset m_padding;
        [SerializeField] private float m_spacing;
        [SerializeField] private TextAnchor m_childAlignment;
        [SerializeField] private bool m_reverse;
        [SerializeField] private BoolVector2 m_controlChildSize;
        [SerializeField] private BoolVector2 m_useChildScale;
        [SerializeField] private BoolVector2 m_childForceExpand;

        #endregion

        #region Scroll Axis

        [SerializeField] private eScrollAxis m_scrollAxis = eScrollAxis.VERTICAL;

        #endregion

        #region Scrollbar

        [SerializeField] private RecycleScrollbar m_scrollbarRef;

        #endregion

        #region Fit Content Size To Viewport Size When Smaller

        [HelpBoxAuto("계산된 콘텐트의 사이즈가 뷰포트 사이즈보다 작은 경우 이를 뷰포트에 맞춥니다", HelpBoxMessageType.Info)]
        [SerializeField] private bool m_fitContentToViewport = false;

        #endregion

        #region Cell Group Config

        [SerializeField] private bool m_fixedCellCountInGroup = true;
        [SerializeField, Min(1)] private int m_fixedCellCount = 1;
        [SerializeField] private bool m_useMinMaxFlexibleCellCount = false;
        [SerializeField] private MinMaxInt m_flexibleCellCountLimit = new(1, 1);
        [SerializeField] private float m_spacingInGroup;

        public bool FixCellCountInGroup => m_fixedCellCountInGroup;
        public bool ShowMinMaxFlexibleCellCount => m_useMinMaxFlexibleCellCount && FixCellCountInGroup == false;
        public bool ShowSpacingInGroup
            => m_fixedCellCountInGroup ? m_fixedCellCount > 1 : !(m_useMinMaxFlexibleCellCount && m_flexibleCellCountLimit.max <= 1);

        #endregion

        #region Page

        [SerializeField] private ScrollPagingConfig m_pagingData;

        #endregion

        #region Loop Scroll

        [HelpBoxAuto("이 옵션이 켜져 있는 경우, 스크롤 방향에 해당하는 패딩 값을 사용하지 않고, "
            + "Scroll Rect의 MovementType이 Unrestricted로 변경됩니다",
            hexColor: ColorHexTemplate.CT_HEX_FF3333)]
        [SerializeField] private bool m_loopScroll = false;

        #endregion

        #region Pool Management

        [SerializeField, Min(0)] private int m_maxPoolSizePerType = 100;

        #endregion
    }
}