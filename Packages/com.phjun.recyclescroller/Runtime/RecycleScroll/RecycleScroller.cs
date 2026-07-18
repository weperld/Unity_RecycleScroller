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
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Recycle Scroll/Recycle Scroller")]
    public partial class RecycleScroller : UIBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler,
        IInitializePotentialDragHandler, IScrollHandler, ICanvasElement, ILayoutElement, ILayoutGroup,
        IRecycleScrollbarDelegate
    {
        #region Fields

        #region Scroll Rect

        public RectTransform Viewport => m_viewport;
        public RectTransform Content => m_content;

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

        /// <summary>
        /// 루프 모드는 경계 클램프가 없어야 하므로 Unrestricted로 파생.
        /// LoadData 전에는 루프 설정과 무관하게 인스펙터 값 그대로 (순정 ScrollRect 동작 보존)
        /// </summary>
        private ScrollRect.MovementType CurrentMovementType =>
            m_isInitialized && m_scrollerMode.IsLoop
                ? ScrollRect.MovementType.Unrestricted
                : m_movementType;

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

        public int PageCount
        {
            get
            {
                if (del == null) return 0;

                return m_dp_pagePos.Count;
            }
        }

        private float PagePivotPosInViewport => m_pagingData.ScrollViewPivot * ViewportSize;

        /// <summary>
        /// 현재 스크롤 위치(페이지 피벗 포함)를 기준으로 가장 가까운 페이지 인덱스 반환, 페이지 기능 미사용 시 -1 반환
        /// </summary>
        public int NearestPageIndexByScrollPos => m_pagingData.usePaging ? FindShowingClosestPageIndexFrom(ScrollPosition) : -1;

        private int m_prevPageIndexByScrollPos = 0;

        #endregion

        #region Loop Scroll

        private IScrollerMode m_scrollerMode = NormalScrollerMode.Instance;

        /// <summary>루프 스크롤 설정값. 변경은 다음 LoadData부터 적용됨</summary>
        public bool LoopScrollIsOn
        {
            get => m_loopScroll;
            set => m_loopScroll = value;
        }

        public bool IsLoopScrollable => m_isInitialized && m_scrollerMode.IsLoop;

        #endregion

        #region Optimization

        private bool m_useOneCellRect = false;

        private readonly ScrollOptimizationValues m_scrollOptimizationValues = new();
        private bool UseScrollOptimization => m_scrollerMode.IsLoop == false && m_scrollOptimizationValues.use;

        #endregion

        #region Scroll Size

        /// <summary>
        /// Setter Only Use In LoadData Method
        /// </summary>
        private float m_realContentSize = 0f;
        /// <summary>실 데이터 기준 콘텐트 총 크기 (패딩 포함). Content 렉트는 윈도우 크기라 이 값과 다름</summary>
        public float ContentSize => m_realContentSize;

        /// <summary>스크롤 가능 거리 = Max(ContentSize - ViewportSize, 0)</summary>
        public float ScrollSize => Mathf.Max(ContentSize - ViewportSize, 0f);

        #endregion

        #region Scroll Position

        /// <summary>
        /// 정규화 스크롤 위치 [0, 1].
        /// 루프 모드에서는 wrap 주기가 ContentSize라 최대값이 1을 넘을 수 있음 (ContentSize/ScrollSize)
        /// </summary>
        public float NormalizedScrollPosition
        {
            get => ScrollAxis switch
            {
                eScrollAxis.VERTICAL => 1f - verticalNormalizedPosition,
                eScrollAxis.HORIZONTAL => horizontalNormalizedPosition,
                _ => 0f,
            };
            set
            {
                // 루프 wrap은 setter가 아니라 LateUpdate의 NormalizeVirtualScrollPos와 getter에서 처리
                switch (ScrollAxis)
                {
                    case eScrollAxis.VERTICAL:
                        verticalNormalizedPosition = 1f - value;
                        break;
                    case eScrollAxis.HORIZONTAL:
                        horizontalNormalizedPosition = value;
                        break;
                    default:
                        Debug.LogError("Not Support Scroll Axis Value");
                        break;
                }
            }
        }

        /// <summary>스크롤 위치. 루프 모드에서는 [0, ContentSize) 범위로 wrap된 값</summary>
        public float ScrollPosition
        {
            get => NormalizedScrollPosition * ScrollSize;
            set => NormalizedScrollPosition = ScrollSize > 0f ? value / ScrollSize : 0f;
        }

        public bool IsEasing => m_corMoveContent != null;

        private float m_previousScrollPosition = 0f;

        #endregion

        #region Scroll Bar

        private RecycleScrollbar m_scrollbar = null;
        public RecycleScrollbar Scrollbar
        {
            get
            {
                if (m_scrollbar)
                {
                    if (m_scrollbar == MainAxisScrollbar) return m_scrollbar;
                    RemoveDragEventAtScrollbar(m_scrollbar);
                    m_scrollbar.OnValueChanged.RemoveListener(OnScrollbarValueChanged);
                }

                m_scrollbar = MainAxisScrollbar;
                if (m_scrollbar)
                {
                    m_scrollbar.Del = this;
                    AddDragEventToScrollbar(m_scrollbar);
                    m_scrollbar.OnValueChanged.AddListener(OnScrollbarValueChanged);
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

        // UpdateCellView 분기 캐싱 (LoadData 시 설정)
        private Vector2 m_cachedAxisVec;
        private Vector2 m_cachedWidthMaskVec;
        private Vector2 m_cachedContentPosVec;
        private RectTransform m_cachedTopSpaceCell;
        private RectTransform m_cachedBottomSpaceCell;

        public int GroupCount => m_list_groupData.Count;
        public int CellCount => m_cellCount;

        #endregion

        #region Interface

        // 스크롤러 델리게이트
        private IRecycleScrollerDelegate m_del;
        public IRecycleScrollerDelegate del
        {
            get => m_del;
            set => m_del = value;
        }

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

        public UnityEngine.UI.ScrollRect.MovementType Debug_CurrentMovementType => CurrentMovementType;
        public bool Debug_CurrentUseInertia => UseInertia;
        public bool Debug_IsChildAlignmentOverwritten => m_overwriteChildAlignment.IsOverwritten;
        public TextAnchor Debug_OverwrittenChildAlignment => m_overwriteChildAlignment.OverwrittenValue;
#endif

    }
}