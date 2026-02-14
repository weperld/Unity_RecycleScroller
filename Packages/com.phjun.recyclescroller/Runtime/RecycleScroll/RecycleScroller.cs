using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace RecycleScroll
{
    [RequireComponent(typeof(ScrollRect))]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Recycle Scroll/Recycle Scroller")]
    public partial class RecycleScroller : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IRecycleScrollbarDelegate
    {
        #region Fields

        #region Scroll Rect

        private ScrollRect m_scrollRect;

        public ScrollRect _ScrollRect
        {
            get
            {
                if (m_scrollRect) return m_scrollRect;

                if (!gameObject.TryGetComponent(out m_scrollRect))
                    m_scrollRect = gameObject.AddComponent<ScrollRect>();
                return m_scrollRect;
            }
        }

        public RectTransform Viewport => _ScrollRect.viewport;
        public RectTransform Content => _ScrollRect.content;

        public float ViewportSize
        {
            get
            {
                bool viewportIsEmpty = Viewport == false;
                if (viewportIsEmpty) return 0f;

                return ScrollAxis switch
                {
                    eScrollAxis.VERTICAL => Viewport.rect.height,
                    eScrollAxis.HORIZONTAL => Viewport.rect.width,
                    _ => 0f,
                };
            }
        }

        [NonSerialized] private RectTransform m_rectSelf;
        private RectTransform _RectTransform
        {
            get
            {
                if (m_rectSelf == null)
                    m_rectSelf = GetComponent<RectTransform>();
                return m_rectSelf;
            }
        }

        #endregion

        #region Alignment Values

        private readonly OverwriteValue<TextAnchor> m_overwriteChildAlignment = new();
        private TextAnchor CurrentTextAnchor => m_overwriteChildAlignment.GetValue(m_childAlignment);

        public ReadOnlyBoolVector2 UseChildScale => m_useChildScale.AsReadOnly;

        public float TopPadding { get; private set; }

        public float BottomPadding { get; private set; }

        public float Spacing => m_spacing;

        #endregion

        #region Scroll Axis

        public eScrollAxis ScrollAxis => m_scrollAxis;

        #endregion

        #region Page

        private int RealPageCount
        {
            get
            {
                if (del == null) return 0;

                return m_dp_pagePos.Count;
            }
        }
        public int ShowingPageCount => RealPageCount - m_scrollerMode.FrontAdditionalPageCount - m_scrollerMode.BackAdditionalPageCount;

        private float PagePivotPosInViewport => m_pagingData.ScrollViewPivot * ViewportSize;
        private float PagePivotPosInScrollRect => PagePivotPosInViewport + RealScrollPosition;

        /// <summary>
        /// 현재 스크롤 위치(페이지 피벗 포함)를 기준으로 가장 가까운 페이지 인덱스 반환, 페이지 기능 미사용 시 -1 반환
        /// </summary>
        public int NearestPageIndexByScrollPos => m_pagingData.usePaging ? FindShowingClosestPageIndexFrom(PagePivotPosInScrollRect) : -1;

        private int m_prevPageIndexByScrollPos = 0;

        #endregion

        #region Loop Scroll

        private readonly float m_normalizedLoopThreshold = 0.5f;
        private bool m_loopScrollable = false;

        private IScrollerMode m_scrollerMode = NormalScrollerMode.Instance;

        private float FrontThreshold => m_normalizedLoopThreshold * m_scrollerMode.AddingFrontContentSize;
        private float BackThreshold => m_normalizedLoopThreshold * m_scrollerMode.AddingBackContentSize;

        public bool LoopScrollIsOn => m_loopScroll;
        public bool IsLoopScrollable => m_loopScrollable;

        #endregion

        #region Optimization

        private bool m_useOneCellRect = false;

        private readonly ScrollOptimizationValues m_scrollOptimizationValues = new();
        private bool UseScrollOptimization => LoopScrollIsOn == false && m_scrollOptimizationValues.use;

        #endregion

        #region Scroll Size

        /// <summary>
        /// Setter Only Use In LoadData Method
        /// </summary>
        private float m_realContentSize = 0f;
        public float RealContentSize => m_realContentSize;
        public float ShowingContentSize => RealContentSize - AddingContentSize;

        public float RealScrollSize => Mathf.Max(RealContentSize - ViewportSize, 0f);
        public float ShowingScrollSize => Mathf.Max(ShowingContentSize - ViewportSize, 0f);

        private float AddingContentSize => m_scrollerMode.AddingFrontContentSize + m_scrollerMode.AddingBackContentSize;

        public float RealSize => RealContentSize;
        public float ShowingSize => ShowingContentSize;

        #endregion

        #region Scroll Position

        /// <summary>
        /// 스크롤 순환 기능 사용 여부에 관계 없이 실제 정규화 스크롤 위치 반환
        /// </summary>
        public float RealNormalizedScrollPosition
        {
            get => ScrollAxis switch
            {
                eScrollAxis.VERTICAL => 1f - _ScrollRect.verticalNormalizedPosition,
                eScrollAxis.HORIZONTAL => _ScrollRect.horizontalNormalizedPosition,
                _ => 0f,
            };
            set
            {
                var setValue = value;
                if (m_loopScrollable)
                {
                    if (setValue is < 0f or > 1f) setValue %= 1f;
                    if (setValue < 0f) setValue += 1f;
                }

                switch (ScrollAxis)
                {
                    case eScrollAxis.VERTICAL:
                        _ScrollRect.verticalNormalizedPosition = 1f - setValue;
                        break;
                    case eScrollAxis.HORIZONTAL:
                        _ScrollRect.horizontalNormalizedPosition = setValue;
                        break;
                    default:
                        Debug.LogError("Not Support Scroll Axis Value");
                        break;
                }
            }
        }
        /// <summary>
        /// 스크롤 순환 기능 사용 여부에 관계 없이 실제 스크롤 위치 반환
        /// </summary>
        public float RealScrollPosition
        {
            get => RealNormalizedScrollPosition * RealScrollSize;
            set => RealNormalizedScrollPosition = value / RealScrollSize;
        }

        /// <summary>
        /// 스크롤 순환 기능을 사용 중인 경우 실제 스크롤 위치가 아닌 추가된 콘텐트 사이즈를 고려해 정규화된 스크롤 위치 반환
        /// </summary>
        public float ShowingNormalizedScrollPosition
        {
            get => ShowingScrollSize > 0f ? ShowingScrollPosition / ShowingScrollSize : 0f;
            set => ShowingScrollPosition = value * ShowingScrollSize;
        }
        public float ShowingScrollPosition
        {
            get => ConvertRealToShow(RealScrollPosition);
            set => RealScrollPosition = ConvertShowToReal(value);
        }

        public bool IsEasing => m_corMoveContent != null;

        private float m_previousScrollPosition = 0f;

        #endregion

        #region Scroll Bar

        private RecycleScrollbar m_scrollbar = null;
        private RecycleScrollbar Scrollbar
        {
            get
            {
                if (m_scrollbar)
                {
                    if (m_scrollbar == m_scrollbarRef) return m_scrollbar;
                    RemoveDragEventAtScrollbar(m_scrollbar);
                    m_scrollbar.OnValueChanged.RemoveListener(OnScrollbarValueChanged);
                }

                m_scrollbar = m_scrollbarRef;
                if (m_scrollbar)
                {
                    m_scrollbar.Del = this;
                    AddDragEventToScrollbar(m_scrollbar);
                    m_scrollbar.OnValueChanged.AddListener(OnScrollbarValueChanged);
                    NullifyScrollRectScrollbar();
                }

                return m_scrollbar;
            }
        }

        #endregion

        #region Item Objects

        private RectTransform m_rt_spcCell_top;    // vertical: top, horizontal: left
        private RectTransform m_rt_spcCell_bottom; // vertical: bottom, horizontal: right
        private readonly Dictionary<int, RecycleScrollerCell> m_dict_activatedCells = new();
        private readonly Dictionary<int, HorizontalOrVerticalLayoutGroup> m_dict_activatedGroups = new();

        public RectTransform Rt_TopSpaceCell => m_reverse ? m_rt_spcCell_bottom : m_rt_spcCell_top;
        public RectTransform Rt_BottomSpaceCell => m_reverse ? m_rt_spcCell_top : m_rt_spcCell_bottom;

        // 아이템 오브젝트 풀
        private readonly Dictionary<Type, Dictionary<string, Stack<RecycleScrollerCell>>> m_pool_cells = new();
        public const string DEFAULT_POOL_SUBKEY = "DEFAULT_POOL_SUBKEY";
        private const string TRASH_OBJECT_NAME = "[Trash]";
        private readonly Stack<HorizontalOrVerticalLayoutGroup> m_pool_groups = new();

        private Transform m_tf_cellPool;
        private Transform m_tf_groupPool;
        private Transform Tf_CellPool
        {
            get
            {
                if (m_tf_cellPool == false)
                {
                    m_tf_cellPool = CreateEmptyGameObject("CellPool", transform).transform;
                    var canvasGroup = m_tf_cellPool.gameObject.AddComponent<CanvasGroup>();
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                    canvasGroup.ignoreParentGroups = true;
                }

                return m_tf_cellPool;
            }
        }
        private Transform Tf_GroupPool
        {
            get
            {
                if (m_tf_groupPool == false)
                {
                    m_tf_groupPool = CreateEmptyGameObject("GroupPool", transform).transform;
                    var canvasGroup = m_tf_groupPool.gameObject.AddComponent<CanvasGroup>();
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                    canvasGroup.ignoreParentGroups = true;
                }

                return m_tf_groupPool;
            }
        }

        #endregion

        #region Coroutine Container

        private IEnumerator m_corMoveContent = null;

        #endregion

        #region Data Cache

        private readonly List<CellSizeVector> m_list_cellSizeVec = new();
        private readonly List<float> m_dp_pagePos = new();
        private readonly List<float> m_dp_groupPos = new();
        private readonly List<CellGroupData> m_list_groupData = new();
        private Dictionary<int, int> m_dict_groupIndexOfCell = new();
        private float m_maxGroupWidth = 0f;
        private int m_cellCount = 0;

        public int GroupCount => m_list_groupData.Count;
        public int CellCount => m_cellCount;

        #endregion

        #region Interface

        // 스크롤러 델리게이트
        public IRecycleScrollerDelegate del;

        #endregion

        #region Load Data Async Manage

        private Coroutine m_corWaitForEndOfFrameAndLoadData;
        private CancellationTokenSource m_loadDataCancellationTokenSource;
        private UniTask? m_loadDataTask;
        private UniTaskCompletionSource<bool> m_loadDataTaskCompletionSource;
        private ulong m_loadDataAsyncCallID = ulong.MinValue;

        #endregion

        #region Load Data State

        private LoadDataProceedState m_loadDataProceedState = LoadDataProceedState.NotLoaded;

        private Coroutine m_cor_loadDataWaitBuffer = null;
        private readonly Queue<Action> m_loadDataWaitingActionBuffer = new();

        private bool IsWaitingLoadDataForActionBuffer => m_cor_loadDataWaitBuffer != null;

        #endregion

        // 초기화
        private bool m_isInitialized = false;

        #endregion

        #region Delegates

        /// <summary>
        /// Call when scroll position changed(ScrollRect.normalizedPosition)
        /// </summary>
        public Action<Vector2> onScroll;

        public Action onBeginDrag;
        public Action onEndDrag;

        /// <summary>
        /// Call when the page changes(prev, next)
        /// </summary>
        public Action<int, int> onChangePage;

        /// <summary>
        /// Call when easing end
        /// </summary>
        public Action onEndEasing;

        /// <summary>
        /// Call when a scroll direction changed
        /// </summary>
        public Action<eScrollDirection> onScrollDirectionChanged;

        /// <summary>
        /// Call when a cell becomes visible (cell, dataIndex)
        /// </summary>
        public Action<RecycleScrollerCell, int> onCellBecameVisible;

        /// <summary>
        /// Call when a cell becomes invisible (cell, dataIndex)
        /// </summary>
        public Action<RecycleScrollerCell, int> onCellBecameInvisible;

        /// <summary>
        /// 등록된 메소드가 없을 경우 기본 Instantiate 사용<para/>
        /// 입력 파라미터 RecycleScrollerCell은 prefab, DataIndex, Transform은 Parent가 될 RecycleScroller.transform
        /// </summary>
        public Func<RecycleScrollerCell, Transform, int, RecycleScrollerCell> CellCreateFuncWhenPoolEmpty;

        #endregion

#if UNITY_EDITOR
        public int Debug_ActiveCellCount => m_dict_activatedCells?.Count ?? 0;
        public int Debug_ActiveGroupCount => m_dict_activatedGroups?.Count ?? 0;
        public int Debug_PooledCellCount
        {
            get
            {
                if (m_pool_cells == null) return 0;
                int total = 0;
                foreach (var typeDict in m_pool_cells.Values)
                    foreach (var stack in typeDict.Values)
                        total += stack.Count;
                return total;
            }
        }
        public LoadDataProceedState Debug_LoadDataState => m_loadDataProceedState;
#endif

        #region Unity Default Events

        private void OnDisable()
        {
            StopAllMoveCor();
            StopAllPreviousLoadDataTask();
            StopCorWaitLoadDataStateToCompleteForBuffer();
        }

        #endregion
    }
}