using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace RecycleScroll
{
    public partial class RecycleScroller
    {
        private readonly List<int> m_pushCellIndexList = new();
        private readonly List<int> m_pushGroupIndexList = new();
        private readonly List<int> m_tempKeyBuffer = new();

        // 윈도우 렌더링용 가시 그룹 버퍼 (논리 순서 — 루프 이음새에서 [N-1, 0, 1...] 형태 허용)
        private readonly List<int> m_visibleGroupBuffer = new();
        private readonly HashSet<int> m_visibleGroupSet = new();

        /// <summary>최대 그룹 크기 — 윈도우 여유분(slack)과 루프 가능 판정에 사용</summary>
        private float m_maxGroupSize;

        // 스레드풀 안전 주축 패딩 캐시 (CacheUpdateCellViewValues에서 갱신)
        private float m_cachedPaddingMainStart;
        private float m_cachedPaddingMainEnd;

        private LoadDataExtensionComponent[] m_loadDataExtension;
        private LoadDataExtensionComponent[] LoadDataExtension
        {
            get
            {
                // find all LoadDataExtensionComponent in this gameObject
                if (m_loadDataExtension is null)
                    m_loadDataExtension = GetComponents<LoadDataExtensionComponent>();
                return m_loadDataExtension;
            }
        }

        #region Set Load Data Proceed State
        private void ResetValuesOnStartLoadData()
        {
            RemoveOverwriteChildAlignment();
        }

        private LoadDataCallbacks CreateNewLoadDataCallbacks()
        {
            var callbacks = new LoadDataCallbacks();
            if (LoadDataExtension != null)
                callbacks.Complete += result =>
                {
                    if (LoadDataExtension is null) return;

                    foreach (var loadDataExtension in LoadDataExtension)
                        if (loadDataExtension != null)
                            loadDataExtension.LoadDataExtendFunction(this, result);
                };
            return callbacks;
        }

        private void SetLoadProceedState(LoadDataProceedState state)
        {
            m_loadDataProceedState = state;
        }
        private void SetLoadProceedStateToNotLoaded_IfIsLoading()
        {
            if (m_loadDataProceedState is not LoadDataProceedState.Loading) return;
            SetLoadProceedState(LoadDataProceedState.NotLoaded);
        }
        #endregion

        #region LoadData
        /// <summary>
        /// 호출 전 (IRecycleScrollerDelegate)del 등록 필수!!!
        /// </summary>
        public LoadDataCallbacks LoadData(params LoadParam[] _params)
        {
            SetLoadProceedState(LoadDataProceedState.Loading);

            ResetValuesOnStartLoadData();
            var callbacks = CreateNewLoadDataCallbacks();

            LoadDataSyncInternal(_params, callbacks);

            return callbacks;
        }

        /// <summary>
        /// 호출 전 (IRecycleScrollerDelegate)del 등록 필수!!!
        /// </summary>
        public LoadDataCallbacks LoadDataAsync(params LoadParam[] _params)
        {
            SetLoadProceedState(LoadDataProceedState.Loading);

            ResetValuesOnStartLoadData();
            var loadDataAsyncCallbacks = CreateNewLoadDataCallbacks();

            m_loadDataAsyncCallID++;
            var currentCallID = m_loadDataAsyncCallID;
            LoadDataAsyncInternal(_params, loadDataAsyncCallbacks, currentCallID).Forget();
            return loadDataAsyncCallbacks;
        }

        private void StopAllPreviousLoadDataTask()
        {
            StopCorWaitForEndOfFrameAndLoadData();
            TryCancelPreviousLoadDataAsyncTask();
        }
        #endregion

        #region LoadData 기능 분리용 함수
        private void InitializeOnLoadData(LoadParam[] _params)
        {
            var firstInit = m_isInitialized == false;
            Init(_params);
            m_cellCount = 0;
            m_prevPageIndexByScrollPos = 0;
            m_previousScrollPosition = 0f;
            ResetContentSizeValue();
            ResetAllCellsAndGroups();

            // 가상 위치 변환에 필요한 캐시 벡터를 로드 초기에 준비 (비동기 로드 중 물리 프레임 대비)
            CacheUpdateCellViewValues();

            // 순정 ScrollRect → 가상 위치로 전환되는 첫 로드에만 시각 위치 승계
            // (재로드 시 anchoredPosition은 윈도우 렌더 값이라 동기화하면 안 됨)
            if (firstInit) SyncScrollPosFromAnchored();
        }

        private void CacheUpdateCellViewValues()
        {
            m_cachedAxisVec = ScrollAxis == eScrollAxis.VERTICAL ? Vector2.up : Vector2.right;
            m_cachedWidthMaskVec = ScrollAxis == eScrollAxis.VERTICAL ? new Vector2(1f, 0f) : new Vector2(0f, 1f);
            m_cachedContentPosVec = ScrollAxis == eScrollAxis.VERTICAL ? Vector2.up : Vector2.left;
            m_cachedTopSpaceCell = Rt_TopSpaceCell;
            m_cachedBottomSpaceCell = Rt_BottomSpaceCell;

            // RectOffset은 네이티브 백킹이라 스레드풀에서 접근 불가 — 주축 패딩을 float으로 캐싱
            m_padding ??= new RectOffset();
            m_cachedPaddingMainStart = ScrollAxis == eScrollAxis.VERTICAL ? m_padding.top : m_padding.left;
            m_cachedPaddingMainEnd = ScrollAxis == eScrollAxis.VERTICAL ? m_padding.bottom : m_padding.right;
        }

        private void UpdateObjectsOnLoadData(LoadParam[] _params)
        {
            // 캐시 벡터를 위치 세터보다 먼저 갱신해야 가상 위치 기록이 올바르게 동작
            CacheUpdateCellViewValues();
            ClampScrollPosAfterDataChange();
            // 위치 파라미터가 없으면 항상 기존 위치 유지 (유효 범위 보정은 위 Clamp가 담당)
            ExecuteLoadParam<LoadParam_ScrollPosSetter>(_params);
            UpdateCellView();
            SetScrollbarSize();
        }

        private bool CheckActiveInHierarchy()
        {
            if (gameObject.activeInHierarchy == false)
            {
                Debug.LogError("RecycleScroller.gameObject.activeInHierarchy is FALSE!!\t"
                    + "RecycleScroller의 기능이 온전히 작동하기 위해서는 오브젝트 활성화 여부 확인이 필요합니다!!");
                return false;
            }

            return true;
        }
        private bool CheckActiveInHierarchy_WithNotify(LoadDataCallbacks callbacks)
        {
            if (CheckActiveInHierarchy()) return true;

            SetLoadProceedStateToNotLoaded_IfIsLoading();
            callbacks.Invoke(LoadDataResultState.Fail_NotActive);
            return false;
        }

        #region For Sync
        private void LoadDataSyncInternal(LoadParam[] _params, LoadDataCallbacks callbacks)
        {
            StopAllPreviousLoadDataTask();
            if (CheckActiveInHierarchy_WithNotify(callbacks) == false) return;

            StartCorWaitForEndOfFrameAndLoadData(_params, callbacks);
        }

        private void CalculateScrollValuesOnLoadData(LoadParam[] _params)
        {
            // 콜렉션 초기화
            ResetCollections();

            m_cellCount = del.GetCellCount(this);
            // 셀 사이즈 리스트 세팅
            SetCellSizeList(m_cellCount);

            // 총 스크롤 영역 사이즈 계산
            CalculateTotalScrollSize(m_cellCount);

            ResetContent_Pivot();
            ResetContent_Anchor();
            ResetContent_Size();
        }

        private void StopCorWaitForEndOfFrameAndLoadData()
        {
            if (m_corWaitForEndOfFrameAndLoadData == null) return;

            StopCoroutine(m_corWaitForEndOfFrameAndLoadData);
            m_corWaitForEndOfFrameAndLoadData = null;
        }
        private void StartCorWaitForEndOfFrameAndLoadData(LoadParam[] _params, LoadDataCallbacks callbacks)
        {
            m_corWaitForEndOfFrameAndLoadData = StartCoroutine(Cor_WaitForEndOfFrameAndLoadData(_params, callbacks));
        }
        private IEnumerator Cor_WaitForEndOfFrameAndLoadData(LoadParam[] _params, LoadDataCallbacks callbacks)
        {
            yield return new WaitForEndOfFrame();

            InitializeOnLoadData(_params);
            if (del == null)
            {
                SetLoadProceedStateToNotLoaded_IfIsLoading();
                callbacks.Invoke(LoadDataResultState.Fail_EmptyDel);
                yield break;
            }

            CalculateScrollValuesOnLoadData(_params);
            UpdateObjectsOnLoadData(_params);

            SetLoadProceedState(LoadDataProceedState.Loaded);
            callbacks.Invoke(LoadDataResultState.Complete);
        }
        #endregion

        #region For Async
        private async UniTaskVoid LoadDataAsyncInternal(LoadParam[] _params, LoadDataCallbacks loadDataCallbacks, ulong callID)
        {
            if (CheckActiveInHierarchy_WithNotify(loadDataCallbacks) == false) return;

            StopAllPreviousLoadDataTask();

            m_loadDataCancellationTokenSource = new CancellationTokenSource();
            var token = m_loadDataCancellationTokenSource.Token;

            m_loadDataTaskCompletionSource = new UniTaskCompletionSource<bool>();

            try
            {
                await this.WaitForEndOfFrameTask(token);
            }
            catch (OperationCanceledException)
            {
                loadDataCallbacks.Invoke(LoadDataResultState.Fail_Cancelled);
                return;
            }

            InitializeOnLoadData(_params);
            if (CheckContinuableLoadDataAsync(loadDataCallbacks, callID) == false) return;
            if (del == null)
            {
                SetLoadProceedStateToNotLoaded_IfIsLoading();
                loadDataCallbacks.Invoke(LoadDataResultState.Fail_EmptyDel);
                return;
            }

            m_loadDataTask = CalculateAndUpdateObjectsAsync(_params, token, loadDataCallbacks, callID);
            if (CheckContinuableLoadDataAsync(loadDataCallbacks, callID) == false) return;

            try
            {
                await m_loadDataTask.Value;
            }
            catch (OperationCanceledException)
            {
                loadDataCallbacks.Invoke(LoadDataResultState.Fail_Cancelled);
            }
            finally
            {
                if (callID == m_loadDataAsyncCallID)
                {
                    ResetLoadDataAsyncSources();
                    SetLoadProceedState(LoadDataProceedState.Loaded);
                }
            }
        }

        private async UniTask CalculateAndUpdateObjectsAsync(LoadParam[] _params,
            CancellationToken token,
            LoadDataCallbacks loadDataCallbacks,
            ulong callID)
        {
            try
            {
                if (CheckContinuableLoadDataAsync(token, loadDataCallbacks, callID) == false) return;

                await CalculateScrollValuesOnLoadDataAsync(_params, token, loadDataCallbacks, callID);
                if (CheckContinuableLoadDataAsync(token, loadDataCallbacks, callID) == false) return;

                UpdateObjectsOnLoadData(_params);

                loadDataCallbacks.Invoke(LoadDataResultState.Complete);
            }
            finally
            {
                m_loadDataTaskCompletionSource?.TrySetResult(true);
            }
        }
        private async UniTask CalculateScrollValuesOnLoadDataAsync(LoadParam[] _params, CancellationToken token, LoadDataCallbacks loadDataCallbacks,
            ulong callID)
        {
            // 콜렉션 초기화
            ResetCollections();
            if (CheckContinuableLoadDataAsync(token, loadDataCallbacks, callID) == false) return;

            m_cellCount = del.GetCellCount(this);
            // 셀 사이즈 리스트 세팅
            SetCellSizeList(m_cellCount);
            if (CheckContinuableLoadDataAsync(token, loadDataCallbacks, callID) == false) return;

            // 총 스크롤 영역 사이즈 계산
            var viewportSize = ViewportSize;
            await CalculateTotalScrollSizeForAsync(m_cellCount, token, viewportSize);
            if (CheckContinuableLoadDataAsync(token, loadDataCallbacks, callID) == false) return;

            ResetContent_Pivot();
            ResetContent_Anchor();
            ResetContent_Size();
        }

        private bool CheckContinuableLoadDataAsync(LoadDataCallbacks loadDataCallbacks, ulong callID)
        {
            if (callID != m_loadDataAsyncCallID)
            {
                loadDataCallbacks.Invoke(LoadDataResultState.Fail_NotLatest);
                return false;
            }

            return true;
        }
        private bool CheckContinuableLoadDataAsync(CancellationToken token, LoadDataCallbacks loadDataCallbacks, ulong callID)
        {
            if (token.IsCancellationRequested) return false;
            return CheckContinuableLoadDataAsync(loadDataCallbacks, callID);
        }

        private async UniTask CalculateTotalScrollSizeForAsync(int cellCount, CancellationToken token, float viewportSize)
        {
            await UniTask.RunOnThreadPool(() => CalculateTotalScrollSize(cellCount, m_maxGroupWidth, token, 0), cancellationToken: token);

            AddEmptySpaceToLastGroupIfNeed();
            await UniTask.RunOnThreadPool(() => CheckLoopable(viewportSize), cancellationToken: token);
        }

        private void TryCancelPreviousLoadDataAsyncTask()
        {
            m_loadDataCancellationTokenSource?.Cancel();
            ResetLoadDataAsyncSources();
        }

        private void ResetLoadDataAsyncSources()
        {
            m_loadDataCancellationTokenSource?.Dispose();
            m_loadDataCancellationTokenSource = null;
            m_loadDataTask = null;
            m_loadDataTaskCompletionSource = null;
        }
        #endregion

        #region 세부 분리
        private void ResetContentSizeValue()
        {
            m_realContentSize = TopPadding + BottomPadding;
        }

        private void ResetAllCellsAndGroups()
        {
            PushAllActivatedCells();
            PushAllActivatedGroups();
        }

        private void ResetCollections()
        {
            m_list_cellSizeVec.Clear();
            m_dp_pagePos.Clear();
            m_dp_groupPos.Clear();
            m_list_groupData.Clear();
            m_dict_groupIndexOfCell.Clear();
        }
        private void ResetCollectionsForInsert()
        {
            m_dp_pagePos.Clear();
            m_dp_groupPos.Clear();
            m_list_groupData.Clear();
            m_dict_groupIndexOfCell.Clear();
        }

        private void SetCellSizeList(int cellCount)
        {
            if (m_useOneCellRect == false)
                for (int i = 0; i < cellCount; i++)
                    m_list_cellSizeVec.Add(GetCellRect(i));
            else
            {
                var cellRect = GetCellRect(0);
                for (int i = 0; i < cellCount; i++)
                    m_list_cellSizeVec.Add(cellRect);
            }

            return;

            CellSizeVector GetCellRect(int dataIndex) => del.GetCellRect(this, dataIndex).ToScaledValues;
        }

        private void CalculateTotalScrollSize(int cellCount, CancellationToken? token = null, int startIndex = 0)
        {
            CalculateTotalScrollSize(cellCount, m_maxGroupWidth, token, startIndex);

            AddEmptySpaceToLastGroupIfNeed();
            CheckLoopable(ViewportSize);
        }

        private void ResetMaxGroupWidthValue()
        {
            m_maxGroupWidth = GetMaxGroupWidth();
        }
        private float GetMaxGroupWidth() => ScrollAxis switch
        {
            eScrollAxis.VERTICAL => Viewport.rect.width - m_padding.left - m_padding.right,
            eScrollAxis.HORIZONTAL => Viewport.rect.height - m_padding.top - m_padding.bottom,
            _ => 0f,
        };

        private void CalculateTotalScrollSize(int cellCount, float maxGroupWidth, CancellationToken? token, int startIndex)
        {
            // ponytail: 부분 재계산(startIndex>0) 시 러닝 맥스 유지 — 제거로 실제 최대가 줄어도 윈도우가 약간 커질 뿐
            if (startIndex == 0) m_maxGroupSize = 0f;

            for (int i = startIndex; i < cellCount;)
            {
                if (token is { IsCancellationRequested: true })
                {
                    token.Value.ThrowIfCancellationRequested();
                    return;
                }

                #region 그룹 생성
                var newGroup = CreateCellGroupData(i, cellCount, maxGroupWidth);
                var startingPoint = m_realContentSize - BottomPadding;
                m_realContentSize += newGroup.size + Spacing;
                if (newGroup.size > m_maxGroupSize) m_maxGroupSize = newGroup.size;
                #endregion

                #region 그룹 데이터 세팅
                m_list_groupData.Add(newGroup);
                m_dp_groupPos.Add(startingPoint);
                #endregion

                #region 페이지 위치 세팅
                if (m_pagingData.countPerPage > 0 && ((m_list_groupData.Count - 1) % m_pagingData.countPerPage) == 0)
                    m_dp_pagePos.Add(startingPoint);
                #endregion

                #region 셀이 속한 그룹 인덱스 세팅
                var cellStartIndex = i;
                var cellEndIndex = newGroup.endDataIndex;
                for (int j = cellStartIndex; j <= cellEndIndex; j++)
                    m_dict_groupIndexOfCell.Add(j, m_list_groupData.Count - 1);
                #endregion

                i = cellEndIndex + 1;
            }

            m_realContentSize -= Spacing;
        }

        private void AddEmptySpaceToLastGroupIfNeed()
        {
            // 페이지 기능 사용 중 마지막 페이지가 뷰포트 사이즈보다 작은 경우
            // 부족한 사이즈만큼의 빈 그룹 데이터 추가
            if (m_pagingData.usePaging == false || m_pagingData.addEmptySpaceToLastPage == false) return;

            var lastPageIndex = PageCount - 1;
            var lastPagePos = m_dp_pagePos[lastPageIndex];
            var endContentPos = m_realContentSize;
            var scrollViewSize = ViewportSize;
            var fromLastPageToEndContent = endContentPos - lastPagePos;
            if (fromLastPageToEndContent >= scrollViewSize) return;

            var addedEmptyGroupPos = endContentPos - BottomPadding + Spacing;
            m_dp_groupPos.Add(addedEmptyGroupPos);
            var addedEmptySpaceSize = scrollViewSize - fromLastPageToEndContent - Spacing;
            var newGroup = new CellGroupData()
            {
                cellCount = 0,
                startDataIndex = -1,
                size = addedEmptySpaceSize
            };
            m_list_groupData.Add(newGroup);
            m_realContentSize += addedEmptySpaceSize + Spacing;
        }

        /// <summary>
        /// 루프 가능 여부 판정 및 모드 선택. 복제 그룹 없이 원본 데이터만 유지한다.
        /// 스레드풀에서 호출될 수 있으므로 Unity API 접근 금지 (패딩은 float 캐시 사용).
        /// </summary>
        private void CheckLoopable(float viewportSize)
        {
            var prevTop = TopPadding;
            var prevBottom = BottomPadding;

            var loopable = m_loopScroll
                && m_realContentSize > viewportSize
                && Mathf.Approximately(m_realContentSize, viewportSize) == false;

            // 동일 그룹이 화면에 두 번 노출되는 구성은 미지원 (기존 복제 방식에도 있던 제약)
            if (loopable && m_realContentSize < viewportSize + m_maxGroupSize + Spacing)
            {
                Debug.LogWarning("[RecycleScroller] 콘텐트가 뷰포트 대비 너무 작아 루프 스크롤을 비활성화합니다. "
                    + $"(content: {m_realContentSize}, viewport: {viewportSize}, maxGroup: {m_maxGroupSize})");
                loopable = false;
            }

            m_scrollerMode = loopable ? (IScrollerMode)LoopScrollerMode.Instance : NormalScrollerMode.Instance;

            // 모드별 주축 패딩 정책 적용 (루프: spacing/2 — 이음새 간격 균일화)
            TopPadding = m_scrollerMode.GetTopPadding(m_spacing, m_cachedPaddingMainStart);
            BottomPadding = m_scrollerMode.GetBottomPadding(m_spacing, m_cachedPaddingMainEnd);
            var topDelta = TopPadding - prevTop;
            var bottomDelta = BottomPadding - prevBottom;
            if (Mathf.Approximately(topDelta, 0f) == false || Mathf.Approximately(bottomDelta, 0f) == false)
            {
                // 패딩 차이만큼 이미 계산된 위치들을 시프트 (전체 재계산 불필요)
                for (int i = 0; i < m_dp_groupPos.Count; i++) m_dp_groupPos[i] += topDelta;
                for (int i = 0; i < m_dp_pagePos.Count; i++) m_dp_pagePos[i] += topDelta;
                m_realContentSize += topDelta + bottomDelta;
                // ponytail: 패딩 변화로 루프 가능 여부가 뒤바뀌는 극단 엣지는 무시 (spacing/2 vs 패딩 차이 수준)
            }

            // 최종 위치/크기 확정 후 reverse 페이지 파티션 재구성
            RebuildPagePositionsForReverse();
        }

        /// <summary>
        /// reverse: 페이지 파티션을 시각 순서(데이터 끝 그룹부터 countPerPage개씩) 기준으로 재구성.
        /// 위치도 스크롤(시각) 좌표로 저장하므로 소비처는 좌표계 구분 없이 사용한다.
        /// 데이터 순서 파티션을 그대로 미러하면 나머지 그룹 페이지가 시각 첫 페이지가 되어 경계가 어긋남
        /// </summary>
        private void RebuildPagePositionsForReverse()
        {
            if (m_reverse == false || m_pagingData.countPerPage <= 0) return;

            m_dp_pagePos.Clear();
            var groupCount = m_list_groupData.Count;
            var countPerPage = m_pagingData.countPerPage;
            for (int p = 0; p * countPerPage < groupCount; p++)
            {
                var groupIndex = groupCount - 1 - p * countPerPage;
                m_dp_pagePos.Add(m_realContentSize - m_dp_groupPos[groupIndex] - m_list_groupData[groupIndex].size);
            }
        }
        #endregion
        #endregion

        #region Update Cell Views
        private void UpdateCellView()
        {
            if (del == null) return;

            #region 1. Calculate Visible Groups
            int groupCount = m_list_groupData.Count;
            if (groupCount == 0) return;

            var rawScrollPos = UseVirtualScroll
                ? m_scrollPos
                : Vector2.Dot(Content.anchoredPosition, m_cachedContentPosVec);
            var drawPos = m_scrollerMode.Normalize(rawScrollPos, ContentSize);

            // reverse: 시각 좌표 [t, t+VP] ↔ 데이터 좌표 [C−t−VP, C−t] 미러.
            // 버퍼는 항상 데이터 오름차순(=계층 순서)이며, reverse의 시각 내림차순은
            // LayoutGroup.reverseArrangement가 담당한다.
            m_visibleGroupBuffer.Clear();
            float anchorBasePos;   // anchored 주축 = anchorBasePos − windowOrigin
            float windowOrigin;

            if (m_scrollerMode.IsLoop)
            {
                if (m_reverse)
                {
                    var dataDrawPos = m_scrollerMode.Normalize(ContentSize - drawPos - ViewportSize, ContentSize);
                    CollectVisibleGroups_Loop(dataDrawPos, groupCount, out _, out var lastUnwrappedEnd);
                    anchorBasePos = drawPos;
                    // 시각 최상단(버퍼 마지막)의 시각 좌표 = d + (ws + VP − lastEnd)
                    windowOrigin = drawPos + dataDrawPos + ViewportSize - lastUnwrappedEnd;
                }
                else
                {
                    CollectVisibleGroups_Loop(drawPos, groupCount, out var firstUnwrappedPos, out _);
                    anchorBasePos = drawPos;
                    windowOrigin = firstUnwrappedPos;
                }
            }
            else
            {
                if (m_reverse)
                {
                    var topBoundaryPos = Mathf.Clamp(drawPos, 0f, ScrollSize);
                    var bottomBoundaryPos = Mathf.Clamp(topBoundaryPos + ViewportSize, ViewportSize, ContentSize);
                    var (firstIndex, lastIndex) = FindVisibleGroupIndices(
                        ContentSize - bottomBoundaryPos, ContentSize - topBoundaryPos);
                    windowOrigin = 0f;
                    if (firstIndex != -1)
                    {
                        for (int i = firstIndex; i <= lastIndex; i++)
                            m_visibleGroupBuffer.Add(i);
                        // 시각 최상단 = 데이터 마지막 그룹의 미러 좌표
                        windowOrigin = ContentSize - m_dp_groupPos[lastIndex] - m_list_groupData[lastIndex].size;
                    }
                    anchorBasePos = rawScrollPos;
                }
                else
                {
                    windowOrigin = CollectVisibleGroups_Normal(drawPos, groupCount);
                    anchorBasePos = rawScrollPos;
                }
            }
            if (m_visibleGroupBuffer.Count == 0) return;

            bool reverseCellSort = m_reverse;
            #endregion

            #region 2. Push Cells and Groups
            m_visibleGroupSet.Clear();
            foreach (var visibleIndex in m_visibleGroupBuffer)
                m_visibleGroupSet.Add(visibleIndex);

            m_pushCellIndexList.Clear();
            m_pushGroupIndexList.Clear();
            foreach (var groupIndex in m_dict_activatedGroups.Keys)
            {
                // 루프 이음새에서 가시 범위가 불연속이므로 범위 비교 대신 집합 소속 비교
                if (m_visibleGroupSet.Contains(groupIndex))
                    continue;

                m_pushGroupIndexList.Add(groupIndex);
                PushIntoGroupStack(m_dict_activatedGroups[groupIndex]);

                var groupData = m_list_groupData[groupIndex];
                for (int cellIndex = groupData.startDataIndex; cellIndex <= groupData.endDataIndex; cellIndex++)
                {
                    if (m_dict_activatedCells.ContainsKey(cellIndex) == false) continue;

                    var pushCell = m_dict_activatedCells[cellIndex];
                    pushCell.OnCellBecameInvisible(this);
                    onCellBecameInvisible?.Invoke(pushCell, cellIndex);
                    m_pushCellIndexList.Add(cellIndex);
                    PushIntoCellStack(pushCell);
                }
            }

            foreach (var pushIndex in m_pushCellIndexList)
                m_dict_activatedCells.Remove(pushIndex);
            foreach (var pushIndex in m_pushGroupIndexList)
                m_dict_activatedGroups.Remove(pushIndex);
            #endregion

            #region Set Cells
            int totalCellViewCount = 0;
            foreach (var visibleIndex in m_visibleGroupBuffer)
                totalCellViewCount += m_list_groupData[visibleIndex].cellCount;
            int lastCellViewIndex = totalCellViewCount - 1;

            // 버퍼 역순으로 sibling 1 삽입 → 최종 계층은 버퍼(논리) 순서.
            // reverse 모드의 시각 반전은 LayoutGroup.reverseArrangement가 담당
            for (int b = m_visibleGroupBuffer.Count - 1; b >= 0; b--)
            {
                var i = m_visibleGroupBuffer[b];

                // 이미 활성화된 그룹이 존재하는 경우, 해당 그룹을 재활용
                var isAlreadyActivated = m_dict_activatedGroups.ContainsKey(i) && m_dict_activatedGroups[i];
                if (isAlreadyActivated == false) m_dict_activatedGroups.Remove(i);

                var getGroup = isAlreadyActivated ? m_dict_activatedGroups[i] : PopFromGroupStack();
                if (getGroup == false) continue;

                var groupData = m_list_groupData[i];
                var cellStartIndex = groupData.startDataIndex;
                var cellLastIndex = groupData.endDataIndex;
                var sortedStartIndex = reverseCellSort ? cellLastIndex : cellStartIndex;
                var sortedLastIndex = reverseCellSort ? cellStartIndex : cellLastIndex;
                var cellIsNothing = groupData.cellCount == 0;

                // 존재했던 그룹이 아니라면 그룹 오브젝트 설정
                if (isAlreadyActivated == false)
                {
                    if (cellIsNothing == false) ResetGroupWithCellRange(getGroup, i, sortedStartIndex, sortedLastIndex);
                    else ResetGroupNoCells(getGroup, i);
                }

                // 그룹 내 셀 설정 (reverse면 역방향 순회)
                for (int step = 0; step < groupData.cellCount && cellIsNothing == false; step++)
                {
                    var j = reverseCellSort ? cellLastIndex - step : cellStartIndex + step;

                    var isAlreadyActivatedCell = m_dict_activatedCells.ContainsKey(j) && m_dict_activatedCells[j];
                    if (isAlreadyActivatedCell == false) m_dict_activatedCells.Remove(j);

                    var getCell = isAlreadyActivatedCell ? m_dict_activatedCells[j] : del.GetCell(this, j, lastCellViewIndex);
                    if (getCell == false) continue;

                    // 선언된 셀 크기(GetCellRect)를 렉트에 반영 — 가변 크기 셀 지원
                    var cellSizeVec = m_list_cellSizeVec[j];
                    getCell.UpdateCellSize(ScrollAxis == eScrollAxis.VERTICAL
                        ? new Vector2(cellSizeVec.CrossAxisSize, cellSizeVec.Size)
                        : new Vector2(cellSizeVec.Size, cellSizeVec.CrossAxisSize));

                    if (isAlreadyActivatedCell == false)
                    {
#if UNITY_EDITOR
                        getCell.gameObject.name = string.Format("{0}_Index({1})", getCell.GetType().ToString(), j);
#endif

                        // 셀 오브젝트의 하이어라키 위치 설정
                        getCell.transform.SetParent(getGroup.transform);
                        getCell.transform.SetAsLastSibling();

                        getCell.OnCellBecameVisible(this, j);
                        onCellBecameVisible?.Invoke(getCell, j);
                    }

                    getCell.SetCellViewIndex(lastCellViewIndex);
                    m_dict_activatedCells.TryAdd(j, getCell);

                    lastCellViewIndex--;
                }

                m_dict_activatedGroups.TryAdd(i, getGroup);

                // 그룹 오브젝트의 하이어라키 위치 설정
                getGroup.transform.SetSiblingIndex(1);
            }
            #endregion

            ApplyWindowTransform(anchorBasePos, windowOrigin);
        }

        /// <summary>
        /// 비루프 가시 그룹 수집. 버퍼에 [first..last] 연속 구간을 담고 첫 그룹의 위치를 반환
        /// </summary>
        private float CollectVisibleGroups_Normal(float drawPos, int groupCount)
        {
            var topBoundaryPos = Mathf.Clamp(drawPos, 0f, ScrollSize);
            var bottomBoundaryPos = Mathf.Clamp(topBoundaryPos + ViewportSize, ViewportSize, ContentSize);

            var (firstIndex, lastIndex) = FindVisibleGroupIndices(topBoundaryPos, bottomBoundaryPos);
            if (firstIndex == -1 || lastIndex == -1) return 0f;

            for (int i = firstIndex; i <= lastIndex; i++)
                m_visibleGroupBuffer.Add(i);
            return m_dp_groupPos[firstIndex];
        }

        /// <summary>
        /// 루프 가시 그룹 수집. drawPos([0, contentSize) wrap 값)부터 순환 순회하며
        /// 논리 순서(이음새에서 [N-1, 0, 1...] 형태)로 버퍼에 담는다.
        /// unwrap 위치 = m_dp_groupPos[i] + cycle * contentSize (이음새 간격 = Bottom+TopPadding = spacing)
        /// </summary>
        /// <param name="firstUnwrappedPos">첫 가시 그룹의 unwrap 시작 위치</param>
        /// <param name="lastUnwrappedEnd">마지막 가시 그룹의 unwrap 끝 위치 (reverse 원점 계산용)</param>
        private void CollectVisibleGroups_Loop(float drawPos, int groupCount,
            out float firstUnwrappedPos, out float lastUnwrappedEnd)
        {
            var contentSize = ContentSize;
            var limit = drawPos + ViewportSize;
            firstUnwrappedPos = 0f;
            lastUnwrappedEnd = 0f;

            // 시작 그룹: drawPos 이하에서 시작하는 마지막 그룹. 없으면(상단 패딩 구간) 이전 사이클의 마지막 그룹
            var index = UpperBound(m_dp_groupPos, drawPos) - 1;
            var cycle = 0;
            if (index < 0)
            {
                index = groupCount - 1;
                cycle = -1;
            }

            var unwrappedPos = m_dp_groupPos[index] + cycle * contentSize;

            for (int safety = 0; safety <= groupCount; safety++)
            {
                var groupSize = m_list_groupData[index].size;
                var visible = unwrappedPos + groupSize >= drawPos && unwrappedPos <= limit;
                if (visible)
                {
                    // 같은 그룹 재노출(한 바퀴) 방지 — CheckLoopable에서 차단되지만 방어
                    if (m_visibleGroupBuffer.Count > 0 && m_visibleGroupBuffer[0] == index) break;
                    if (m_visibleGroupBuffer.Count == 0) firstUnwrappedPos = unwrappedPos;
                    m_visibleGroupBuffer.Add(index);
                    lastUnwrappedEnd = unwrappedPos + groupSize;
                }
                else if (m_visibleGroupBuffer.Count > 0) break;

                index++;
                if (index >= groupCount)
                {
                    index = 0;
                    cycle++;
                }

                unwrappedPos = m_dp_groupPos[index] + cycle * contentSize;
                if (unwrappedPos > limit) break;
            }
        }

        /// <summary>
        /// 윈도우 배치: Content.anchoredPosition 주축 = anchorBasePos − windowOrigin.
        /// 비루프 overshoot(러버밴드)는 raw 위치가 그대로 반영되어 순정과 동일한 시각 표현.
        /// SpaceCell은 윈도우 방식에서 사용하지 않는다 (오프셋은 anchoredPosition이 담당)
        /// </summary>
        private void ApplyWindowTransform(float anchorBasePos, float windowOrigin)
        {
            if (UseVirtualScroll == false) return;

            if (m_cachedTopSpaceCell && m_cachedTopSpaceCell.gameObject.activeSelf)
                m_cachedTopSpaceCell.gameObject.SetActive(false);
            if (m_cachedBottomSpaceCell && m_cachedBottomSpaceCell.gameObject.activeSelf)
                m_cachedBottomSpaceCell.gameObject.SetActive(false);

            var mainAnchored = anchorBasePos - windowOrigin;
            var anchored = m_content.anchoredPosition;
            var currentMain = Vector2.Dot(anchored, m_cachedContentPosVec);
            if (Mathf.Approximately(currentMain, mainAnchored)) return;

            // 렌더 미러 기록 — SetContentAnchoredPosition을 경유하면 안 됨 (가상 위치가 오염됨)
            m_content.anchoredPosition = anchored + (mainAnchored - currentMain) * m_cachedContentPosVec;
            UpdateBounds();
        }

        /// <summary>list[idx] > value 를 만족하는 첫 인덱스 (이진 탐색)</summary>
        private static int UpperBound(List<float> list, float value)
        {
            int lo = 0, hi = list.Count;
            while (lo < hi)
            {
                var mid = (lo + hi) >> 1;
                if (list[mid] <= value) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }

        public void ReloadCellView()
        {
            if (TryAddWaitBuffer_AndCheckCurrentState(ReloadCellView, out _)) return;

            ResetAllCellsAndGroups();
            if (del == null || del.GetCellCount(this) <= 0) return;
            UpdateCellView();
        }
        #endregion

        #region Update Cell View Helper
        /// <summary>
        /// </summary>
        /// <param name="topBoundaryPos">탐색 시작 위치</param>
        /// <param name="bottomBoundaryPos">탐색 종료 위치</param>
        /// <returns>입력된 탐색 범위 내에서 보여지는 셀 중 처음과 끝 인덱스</returns>
        private (int firstIndex, int lastIndex) FindVisibleGroupIndices(float topBoundaryPos, float bottomBoundaryPos)
        {
            var groupCount = m_list_groupData.Count;
            if (groupCount == 0) return (-1, -1);

            // 현재 범위에서 보여지는 셀 탐색 (이진 탐색으로 시작 지점 확정 후 가시 구간만 순회)
            // 조건: 그룹 끝 >= 탐색 시작 && 그룹 시작 <= 탐색 종료
            var firstIndex = UpperBound(m_dp_groupPos, topBoundaryPos) - 1;
            if (firstIndex < 0) firstIndex = 0;
            while (firstIndex < groupCount
                && m_dp_groupPos[firstIndex] + m_list_groupData[firstIndex].size < topBoundaryPos)
                firstIndex++;
            if (firstIndex >= groupCount || m_dp_groupPos[firstIndex] > bottomBoundaryPos) return (-1, -1);

            var lastIndex = firstIndex;
            while (lastIndex + 1 < groupCount && m_dp_groupPos[lastIndex + 1] <= bottomBoundaryPos)
                lastIndex++;
            return (firstIndex, lastIndex);
        }

        private CellGroupData CreateCellGroupData(int startIndex, int totalCellCount, float maxGroupWidth)
        {
            // 시작 인덱스가 총 셀 수를 넘지 않도록 제한
            startIndex = Mathf.Clamp(startIndex, 0, totalCellCount);

            // 그룹의 초기 너비와 크기를 설정
            var sizeVec = m_list_cellSizeVec[startIndex];
            var cellsWidthInGroup = sizeVec.CrossAxisSize;
            var groupSize = sizeVec.Size;

            // 새로운 셀 그룹 데이터를 생성
            var newGroup = new CellGroupData(startIndex);

            // 그룹에 셀을 넣을 수 있는 지 체크
            // 그룹에 셀을 최소 하나는 포함하도록 설정
            int endIndex = totalCellCount;
            if (FixCellCountInGroup)
            {
                var countInGroup = m_fixedCellCount;
                // reverse: 나머지 셀은 데이터 시작(시각 끝) 그룹에 배치 — 시각 첫 그룹이 가득 차도록
                if (m_reverse && startIndex == 0)
                {
                    var remainder = totalCellCount % m_fixedCellCount;
                    if (remainder > 0) countInGroup = remainder;
                }
                endIndex = Mathf.Min(startIndex + countInGroup, totalCellCount);
            }
            else if (m_useMinMaxFlexibleCellCount)
            {
                int minLimit = Mathf.Min(startIndex + m_flexibleCellCountLimit.min, totalCellCount);
                int maxLimit = Mathf.Min(startIndex + m_flexibleCellCountLimit.max, totalCellCount);

                // 최소 셀 수만큼 처리
                ProcessCells(startIndex + 1, minLimit, ref groupSize, ref cellsWidthInGroup, ref newGroup, false);
                // 최대 셀 수까지 처리
                ProcessCells(minLimit, maxLimit, ref groupSize, ref cellsWidthInGroup, ref newGroup, true);

                newGroup.size = groupSize;
                return newGroup;
            }

            // 고정 셀 수만큼 처리
            ProcessCells(startIndex + 1, endIndex, ref groupSize, ref cellsWidthInGroup, ref newGroup, FixCellCountInGroup == false);
            newGroup.size = groupSize;
            return newGroup;

            // 셀들을 그룹에 추가하는 내부 함수
            void ProcessCells(int from, int to, ref float refGroupSize, ref float refCellsWidthInGroup, ref CellGroupData refNewGroup, bool checkMaxWidth)
            {
                for (int i = from; i < to; i++)
                {
                    if (CalculateCellGroupData(i, ref refGroupSize, ref refCellsWidthInGroup, ref refNewGroup, checkMaxWidth) == false)
                        break;
                }
            }

            // 개별 셀을 그룹에 추가하고 그룹의 크기를 업데이트하는 내부 함수
            bool CalculateCellGroupData(int index, ref float refGroupSize, ref float refCellsWidthInGroup, ref CellGroupData refNewGroup, bool checkMaxWidth)
            {
                var l_sizeVec = m_list_cellSizeVec[index];
                var nextCellWidth = l_sizeVec.CrossAxisSize;

                // 너비가 최대 그룹 너비를 넘는지 확인
                if (checkMaxWidth && refCellsWidthInGroup + nextCellWidth > maxGroupWidth) return false;

                var nextCellSize = l_sizeVec.Size;
                refGroupSize = Mathf.Max(refGroupSize, nextCellSize);

                refCellsWidthInGroup += nextCellWidth;
                refNewGroup.cellCount++;
                return true;
            }
        }
        #endregion
    }
}