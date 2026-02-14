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

        private ScrollRect m_ScrollRect;

        public ScrollRect _ScrollRect
        {
            get
            {
                if (m_ScrollRect) return m_ScrollRect;

                if (!gameObject.TryGetComponent(out m_ScrollRect))
                    m_ScrollRect = gameObject.AddComponent<ScrollRect>();
                return m_ScrollRect;
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

        [System.NonSerialized] private RectTransform m_rectSelf;
        private RectTransform rectTransform
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
        private TextAnchor CurrentTextAnchor => m_overwriteChildAlignment.GetValue(m_ChildAlignment);

        public ReadOnlyBoolVector2 UseChildScale => m_UseChildScale.AsReadOnly;

        public float TopPadding { get; private set; }

        public float BottomPadding { get; private set; }

        public float Spacing => m_Spacing;

        #endregion

        #region Scroll Axis

        public eScrollAxis ScrollAxis => m_ScrollAxis;

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
        public int ShowingPageCount => RealPageCount - m_frontAdditionalPageCount - m_backAdditionalPageCount;

        private float PagePivotPosInViewport => m_PagingData.ScrollViewPivot * ViewportSize;
        private float PagePivotPosInScrollRect => PagePivotPosInViewport + RealScrollPosition;

        /// <summary>
        /// 현재 스크롤 위치(페이지 피벗 포함)를 기준으로 가장 가까운 페이지 인덱스 반환, 페이지 기능 미사용 시 -1 반환
        /// </summary>
        public int NearestPageIndexByScrollPos => m_PagingData.usePaging ? FindShowingClosestPageIndexFrom(PagePivotPosInScrollRect) : -1;

        private int m_prevPageIndexByScrollPos = 0;

        private int m_frontAdditionalPageCount = 0;
        private int m_backAdditionalPageCount = 0;

        #endregion

        #region Loop Scroll

        private readonly float m_normalizedLoopThreshold = 0.5f;
        private bool m_loopScrollable = false;

        private float m_addingFrontContentSizeInLoop = 0f;
        private float m_addingBackContentSizeInLoop = 0f;

        private float NormalizedAddingFrontContentSizeInLoop => m_addingFrontContentSizeInLoop / RealContentSize;
        private float NormalizedAddingBackContentSizeInLoop => m_addingBackContentSizeInLoop / RealContentSize;

        private float FrontThreshold => m_normalizedLoopThreshold * m_addingFrontContentSizeInLoop;
        private float BackThreshold => m_normalizedLoopThreshold * m_addingBackContentSizeInLoop;

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

        private float AddingContentSize => m_loopScrollable ? m_addingFrontContentSizeInLoop + m_addingBackContentSizeInLoop : 0f;

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
        private RecycleScrollbar _Scrollbar
        {
            get
            {
                if (m_scrollbar)
                {
                    if (m_scrollbar == m_ScrollbarRef) return m_scrollbar;
                    RemoveDragEventAtScrollbar(m_scrollbar);
                    m_scrollbar.onValueChanged.RemoveListener(OnScrollbarValueChanged);
                }

                m_scrollbar = m_ScrollbarRef;
                if (m_scrollbar)
                {
                    m_scrollbar.Del = this;
                    AddDragEventToScrollbar(m_scrollbar);
                    m_scrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);
                    NullifyScrollRectScrollbar();
                }

                return m_scrollbar;
            }
        }

        #endregion

        #region Item Objects

        private RectTransform rt_SpcCell_Top;    // vertical: top, horizontal: left
        private RectTransform rt_SpcCell_Bottom; // vertical: bottom, horizontal: right
        private Dictionary<int, RecycleScrollerCell> m_dic_ActivatedCells = new();
        private Dictionary<int, HorizontalOrVerticalLayoutGroup> m_dic_ActivatedGroups = new();

        public RectTransform Rt_TopSpaceCell => m_Reverse ? rt_SpcCell_Bottom : rt_SpcCell_Top;
        public RectTransform Rt_BottomSpaceCell => m_Reverse ? rt_SpcCell_Top : rt_SpcCell_Bottom;

        // 아이템 오브젝트 풀
        private Dictionary<System.Type, Dictionary<string, Stack<RecycleScrollerCell>>> m_pool_Cells = new();
        public const string DEFAULT_POOL_SUBKEY = "DEFAULT_POOL_SUBKEY";
        private const string TRASH_OBJECT_NAME = "[Trash]";
        private Stack<HorizontalOrVerticalLayoutGroup> m_pool_Groups = new();

        private Transform m_tf_CellPool;
        private Transform m_tf_GroupPool;
        private Transform Tf_CellPool
        {
            get
            {
                if (m_tf_CellPool == false)
                {
                    m_tf_CellPool = CreateEmptyGameObject("CellPool", transform).transform;
                    var canvasGroup = m_tf_CellPool.gameObject.AddComponent<CanvasGroup>();
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                    canvasGroup.ignoreParentGroups = true;
                }

                return m_tf_CellPool;
            }
        }
        private Transform Tf_GroupPool
        {
            get
            {
                if (m_tf_GroupPool == false)
                {
                    m_tf_GroupPool = CreateEmptyGameObject("GroupPool", transform).transform;
                    var canvasGroup = m_tf_GroupPool.gameObject.AddComponent<CanvasGroup>();
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                    canvasGroup.ignoreParentGroups = true;
                }

                return m_tf_GroupPool;
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
        private readonly List<CellGroupData> m_list_groupDatas = new();
        private Dictionary<int, int> m_dict_groupIndexOfCell = new();
        private float m_maxGroupWidth = 0f;
        private int m_cellCount = 0;

        public int GroupCount => m_list_groupDatas.Count;
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
        /// Call when scroll direction changed
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
        public int Debug_ActiveCellCount => m_dic_ActivatedCells?.Count ?? 0;
        public int Debug_ActiveGroupCount => m_dic_ActivatedGroups?.Count ?? 0;
        public int Debug_PooledCellCount
        {
            get
            {
                if (m_pool_Cells == null) return 0;
                int total = 0;
                foreach (var typeDict in m_pool_Cells.Values)
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