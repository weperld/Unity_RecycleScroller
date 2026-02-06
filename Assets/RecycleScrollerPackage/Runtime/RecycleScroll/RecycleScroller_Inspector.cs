using MathUtils;
using UnityEngine;

namespace RecycleScroll
{
    public partial class RecycleScroller
    {
        #region Alignment Values

        [ColoredHeader("[Cell Alignment Values]", ColorHexTemplate.CT_HEX_FFFF99)]
        [SerializeField] private RectOffset m_Padding;
        [SerializeField] private float m_Spacing;
        [SerializeField] private TextAnchor m_ChildAlignment;
        [SerializeField] private bool m_Reverse;
        [SerializeField] private BoolVector2 m_ControlChildSize;
        [SerializeField] private BoolVector2 m_UseChildScale;
        [SerializeField] private BoolVector2 m_ChildForceExpand;

        #endregion

        #region Scroll Axis

        [Space(10f), ColoredHeader("[Scroll Direction Axis]", ColorHexTemplate.CT_HEX_FFFF99)]
        [SerializeField] private eScrollAxis m_ScrollAxis = eScrollAxis.VERTICAL;

        #endregion

        #region Fit Content Size To Viewport Size When Smaller

        [Space(10f), ColoredHeader("[Content Size Fit To Viewport Size When Smaller]", ColorHexTemplate.CT_HEX_FFFF99)]
        [HelpBoxAuto("계산된 콘텐트의 사이즈가 뷰포트 사이즈보다 작은 경우 이를 뷰포트에 맞춥니다", HelpBoxMessageType.Info)]
        [SerializeField] private bool m_FitContentToViewport = false;

        #endregion

        #region Cell Group Config

        [Space(10f), ColoredHeader("[Cell Group Configs]", ColorHexTemplate.CT_HEX_FFFF99)]
        [SerializeField] private bool m_fixedCellCountInGroup = true;
        [SerializeField, Min(1)] private int m_fixedCellCount = 1;
        [SerializeField] private bool m_useMinMaxFlexibleCellCount = false;
        [SerializeField] private MinMaxInt m_flexibleCellCountLimit = new(1, 1);
        [SerializeField] private float m_SpacingInGroup;

        public bool FixCellCountInGroup => m_fixedCellCountInGroup;
        public bool ShowMinMaxFlexibleCellCount => m_useMinMaxFlexibleCellCount && FixCellCountInGroup == false;
        public bool ShowSpacingInGroup
            => m_fixedCellCountInGroup ? m_fixedCellCount > 1 : !(m_useMinMaxFlexibleCellCount && m_flexibleCellCountLimit.max <= 1);

        #endregion

        #region Page

        [Space(10f), ColoredHeader("[Paging Configs]", ColorHexTemplate.CT_HEX_FFFF99)]
        [SerializeField] private ScrollPagingConfig m_PagingData;

        #endregion

        #region Loop Scroll

        [Space(10f), ColoredHeader("[Loop Scroll]", ColorHexTemplate.CT_HEX_FFFF99)]
        [HelpBoxAuto("이 옵션이 켜져 있는 경우, 스크롤 방향에 해당하는 패딩 값을 사용하지 않고, "
            + "Scroll Rect의 MovementType이 Unrestricted로 변경됩니다",
            hexColor: ColorHexTemplate.CT_HEX_FF3333)]
        [SerializeField] private bool m_loopScroll = false;

        #endregion
    }
}