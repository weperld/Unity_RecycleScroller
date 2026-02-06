using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RecycleScroll
{
    public partial class RecycleScroller
    {
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
            Init(_params);
            m_cellCount = 0;
            m_prevPageIndexByScrollPos = 0;
            ResetRealContentSize();
            ResetAllCellsAndGroups();
        }

        private void UpdateObjectsOnLoadData(LoadParam[] _params)
        {
            ExecuteLoadParam<LoadParam_ScrollPosSetter>(_params, () => ShowingNormalizedScrollPosition = 0f);
            UpdateCellView();
            SetScrollbarSize();
        }

        private bool CheckActiveInHierarchy()
        {
            if (gameObject.activeInHierarchy is false)
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
            if (CheckActiveInHierarchy_WithNotify(callbacks) is false) return;

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
            if (CheckActiveInHierarchy_WithNotify(loadDataCallbacks) is false) return;

            StopAllPreviousLoadDataTask();

            m_loadDataCancellationTokenSource = new CancellationTokenSource();
            var token = m_loadDataCancellationTokenSource.Token;

            m_loadDataTaskCompletionSource = new TaskCompletionSource<bool>();

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
                await m_loadDataTask;
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

        private async Task CalculateAndUpdateObjectsAsync(LoadParam[] _params,
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
        private async Task CalculateScrollValuesOnLoadDataAsync(LoadParam[] _params, CancellationToken token, LoadDataCallbacks loadDataCallbacks,
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

        private async Task CalculateTotalScrollSizeForAsync(int cellCount, CancellationToken token, float viewportSize)
        {
            await Task.Run(() => CalculateTotalScrollSize(cellCount, m_maxGroupWidth, token, 0), token);

            AddEmptySpaceToLastGroupIfNeed();
            await Task.Run(() => CheckLoop(viewportSize), token);
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
        private void ResetRealContentSize()
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
            m_list_groupDatas.Clear();
            m_dict_groupIndexOfCell.Clear();
        }
        private void ResetCollectionsForInsert()
        {
            m_dp_pagePos.Clear();
            m_dp_groupPos.Clear();
            m_list_groupDatas.Clear();
            m_dict_groupIndexOfCell.Clear();
        }

        private void SetCellSizeList(int cellCount)
        {
            if (m_useOneCellRect == false)
                foreach (var index in Enumerable.Range(0, cellCount))
                    m_list_cellSizeVec.Add(GetCellRect(index));
            else
            {
                var cellRect = GetCellRect(0);
                m_list_cellSizeVec.AddRange(Enumerable.Repeat(cellRect, cellCount));
            }

            return;

            CellSizeVector GetCellRect(int dataIndex) => del.GetCellRect(this, dataIndex).ToScaledValues;
        }

        private void CalculateTotalScrollSize(int cellCount, CancellationToken? token = null, int startIndex = 0)
        {
            CalculateTotalScrollSize(cellCount, m_maxGroupWidth, token, startIndex);

            AddEmptySpaceToLastGroupIfNeed();
            CheckLoop(ViewportSize);
        }

        private void ResetMaxGroupWidthValue()
        {
            m_maxGroupWidth = GetMaxGroupWidth();
        }
        private float GetMaxGroupWidth() => ScrollAxis switch
        {
            eScrollAxis.VERTICAL => Viewport.rect.width - m_Padding.left - m_Padding.right,
            eScrollAxis.HORIZONTAL => Viewport.rect.height - m_Padding.top - m_Padding.bottom,
            _ => 0f,
        };

        private void CalculateTotalScrollSize(int cellCount, float maxGroupWidth, CancellationToken? token, int startIndex)
        {
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
                #endregion

                #region 그룹 데이터 세팅
                m_list_groupDatas.Add(newGroup);
                m_dp_groupPos.Add(startingPoint);
                #endregion

                #region 페이지 위치 세팅
                if (m_PagingData.countPerPage > 0 && ((m_list_groupDatas.Count - 1) % m_PagingData.countPerPage) == 0)
                    m_dp_pagePos.Add(startingPoint);
                #endregion

                #region 셀이 속한 그룹 인덱스 세팅
                var cellStartIndex = i;
                var cellEndIndex = newGroup.endDataIndex;
                for (int j = cellStartIndex; j <= cellEndIndex; j++)
                    m_dict_groupIndexOfCell.Add(j, m_list_groupDatas.Count - 1);
                #endregion

                i = cellEndIndex + 1;
            }

            m_realContentSize -= Spacing;
        }

        private void AddEmptySpaceToLastGroupIfNeed()
        {
            // 페이지 기능 사용 중 마지막 페이지가 뷰포트 사이즈보다 작은 경우
            // 부족한 사이즈만큼의 빈 그룹 데이터 추가
            if (m_PagingData.usePaging == false || m_PagingData.addEmptySpaceToLastPage == false) return;

            var lastPageIndex = RealPageCount - 1;
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
            m_list_groupDatas.Add(newGroup);
            m_realContentSize += addedEmptySpaceSize + Spacing;
        }

        private void CheckLoop(float viewportSize)
        {
            m_addingFrontContentSizeInLoop = 0f;
            m_addingBackContentSizeInLoop = 0f;
            m_frontAdditionalPageCount = 0;
            m_backAdditionalPageCount = 0;
            m_loopScrollable = false;

            if (m_loopScroll == false) return;

            // 현재까지 계산된 총 콘텐츠 사이즈가 루프를 필요로 할 만큼 크지 않다면 계산하지 않음
            if (m_realContentSize <= viewportSize || Mathf.Approximately(m_realContentSize, viewportSize)) return;

            m_loopScrollable = true;

            // 그룹데이터 리스트의 양 끝에 뷰포트 사이즈만큼씩 순환을 위한 그룹 데이터 추가
            // 각각의 추가할 그룹데이터 리스트는 뷰포트 사이즈만큼 찾아 추가

            #region 1. 맨 앞에 추가될 그룹 데이터 리스트 검사
            var originalGroupCount = m_list_groupDatas.Count;
            var startIndex = originalGroupCount - 1;
            var checkIndex = startIndex;
            var addingGroupsSize = 0f;
            for (; checkIndex >= 0 && addingGroupsSize - Spacing + TopPadding < viewportSize; checkIndex--)
                addingGroupsSize += m_list_groupDatas[checkIndex].size + Spacing;
            var endIndex = checkIndex;
            var frontAddGroupList = m_list_groupDatas.GetSafeRange(endIndex, startIndex - endIndex + 1);
            #endregion

            #region 2. 맨 뒤에 추가될 그룹 데이터 리스트 검사
            startIndex = 0;
            checkIndex = startIndex;
            addingGroupsSize = 0f;
            for (; checkIndex < originalGroupCount && addingGroupsSize - Spacing + BottomPadding < viewportSize; checkIndex++)
                addingGroupsSize += m_list_groupDatas[checkIndex].size + Spacing;
            endIndex = checkIndex;
            var backAddGroupList = m_list_groupDatas.GetSafeRange(startIndex, endIndex - startIndex + 1);
            #endregion

            #region 3. 양 끝에 그룹 데이터 리스트 추가에 따른 콜렉션 재계산
            var frontAddCount = frontAddGroupList.Count;
            var backAddCount = backAddGroupList.Count;
            var groupIndexKeys = m_dict_groupIndexOfCell.Keys.ToList();
            foreach (var cellIndex in groupIndexKeys)
                m_dict_groupIndexOfCell[cellIndex] += frontAddCount;

            var frontAddGroupPosList = m_dp_groupPos.GetSafeRange(m_dp_groupPos.Count - frontAddCount, frontAddCount);
            var tempPos = frontAddGroupPosList.FirstOrDefault();
            for (int i = 0; i < frontAddGroupPosList.Count; i++)
                frontAddGroupPosList[i] -= tempPos;
            var frontAddingGroupsSize = frontAddGroupPosList.LastOrDefault()
                + frontAddGroupList.LastOrDefault().size + Spacing;
            frontAddingGroupsSize = Mathf.Max(frontAddingGroupsSize, 0f);

            var backAddGroupPosList = m_dp_groupPos.GetSafeRange(0, backAddCount);
            tempPos = m_realContentSize + frontAddingGroupsSize;
            for (int i = 0; i < backAddGroupPosList.Count; i++)
                backAddGroupPosList[i] += tempPos;
            var backAddingGroupsSize = backAddGroupPosList.LastOrDefault() - backAddGroupPosList.FirstOrDefault()
                + backAddGroupList.LastOrDefault().size + Spacing;
            backAddingGroupsSize = Mathf.Max(backAddingGroupsSize, 0f);

            m_addingFrontContentSizeInLoop = frontAddingGroupsSize;
            m_addingBackContentSizeInLoop = backAddingGroupsSize;
            m_realContentSize += frontAddingGroupsSize + backAddingGroupsSize;

            #region 그룹 위치 리스트 재계산
            for (int i = 0; i < m_dp_groupPos.Count; i++)
                m_dp_groupPos[i] += frontAddingGroupsSize;
            m_dp_groupPos.InsertRange(0, frontAddGroupPosList);
            m_dp_groupPos.AddRange(backAddGroupPosList);
            #endregion

            #region 페이지 위치 리스트 재계산
            if (m_PagingData.usePaging)
            {
                // 앞에 추가된 그룹 데이터들이 속한 페이지 체크
                var frontPageCount = frontAddCount / m_PagingData.countPerPage;
                if (frontAddCount % m_PagingData.countPerPage > 0) frontPageCount++;
                frontPageCount++;
                var frontAddPageStartIndex = m_dp_pagePos.Count - frontPageCount;
                var frontPagePosList = m_dp_pagePos.GetSafeRange(frontAddPageStartIndex, frontPageCount)
                    .Select(pos => pos - ShowingContentSize + frontAddingGroupsSize);
                m_frontAdditionalPageCount = frontPageCount;

                // 뒤에 추가된 그룹 데이터들이 속한 페이지 체크
                var backPageCount = backAddCount / m_PagingData.countPerPage;
                if (backAddCount % m_PagingData.countPerPage > 0) backPageCount++;
                backPageCount++;
                var backPagePosList = m_dp_pagePos.GetSafeRange(0, backPageCount)
                    .Select(pos => pos + RealContentSize - m_addingBackContentSizeInLoop);
                m_backAdditionalPageCount = backPageCount;

                // 각 페이지 위치 리스트 재계산
                for (int i = 0; i < m_dp_pagePos.Count; i++)
                    m_dp_pagePos[i] += frontAddingGroupsSize;
                m_dp_pagePos.InsertRange(0, frontPagePosList);
                m_dp_pagePos.AddRange(backPagePosList);
            }
            #endregion
            #endregion

            #region 4. 추가된 그룹 데이터 리스트를 각각의 양 끝에 추가
            m_list_groupDatas.InsertRange(0, frontAddGroupList);
            m_list_groupDatas.AddRange(backAddGroupList);
            #endregion
        }
        #endregion
        #endregion

        #region Update Cell Views
        private void UpdateCellView()
        {
            if (del == null) return;

            // 스크롤 영역 사이즈, 현재 스크롤 위치, 뷰포트 사이즈, 각 그룹의 크기를 이용해 현재 영역에 보여야 할 그룹 세팅
            // 1. 스크롤 영역 기준 뷰포트의 가장 자리 위치 계산
            // 2. 각 그룹을 돌며 뷰포트에 어떤 그룹이 걸쳐져 있는지 확인
            // 3. 양 끝에 걸친 그룹의 인덱스를 이용해 보여야 하는 그룹들을 세팅

            #region 1. Calculate Boundary Position
            int groupCount = m_list_groupDatas.Count;
            if (groupCount == 0) return;

            var contentPos = Content.anchoredPosition;
            var topBoundaryPos = Mathf.Clamp(ScrollAxis == eScrollAxis.VERTICAL ? contentPos.y : -contentPos.x, 0f, RealScrollSize);
            var bottomBoundaryPos = Mathf.Clamp(topBoundaryPos + ViewportSize, ViewportSize, RealContentSize);
            #endregion

            #region 2. Check Group Is Showing
            var findVisibleGroupIndices = FindVisibleGroupIndices(topBoundaryPos, bottomBoundaryPos);
            var firstGroupViewIndex = findVisibleGroupIndices.firstIndex;
            var lastGroupViewIndex = findVisibleGroupIndices.lastIndex;
            if (firstGroupViewIndex == -1 || lastGroupViewIndex == -1) return;

            // Calculate space sizes
            var topSpaceGroupSize = firstGroupViewIndex == 0
                ? 0f
                : Mathf.Max(m_dp_groupPos[firstGroupViewIndex] - Spacing - TopPadding, 0f);
            var bottomSpaceGroupSize = lastGroupViewIndex == groupCount - 1
                ? 0f
                : Mathf.Max(RealContentSize - (m_dp_groupPos[lastGroupViewIndex] + m_list_groupDatas[lastGroupViewIndex].size + Spacing) - BottomPadding, 0f);
            #endregion

            #region 3. Set Showing Groups
            #region Set Space Cells
            var axisVec = ScrollAxis == eScrollAxis.VERTICAL ? Vector2.up : Vector2.right;
            var widthVec = Viewport.rect.size;
            switch (ScrollAxis)
            {
                case eScrollAxis.VERTICAL:
                    widthVec.y = 0;
                    break;
                case eScrollAxis.HORIZONTAL:
                    widthVec.x = 0;
                    break;
            }

            Rt_TopSpaceCell.gameObject.SetActive(firstGroupViewIndex != (m_Reverse ? groupCount - 1 : 0));
            Rt_TopSpaceCell.sizeDelta = axisVec * topSpaceGroupSize + widthVec;
            Rt_BottomSpaceCell.gameObject.SetActive(lastGroupViewIndex != (m_Reverse ? 0 : groupCount - 1));
            Rt_BottomSpaceCell.sizeDelta = axisVec * bottomSpaceGroupSize + widthVec;
            #endregion

            #region Push Cells and Groups
            var pushCellIndexList = new List<int>();
            var pushGroupIndexList = new List<int>();
            foreach (var groupIndex in m_dic_ActivatedGroups.Keys)
            {
                if (m_Reverse
                    ? (lastGroupViewIndex <= groupIndex && groupIndex <= firstGroupViewIndex)
                    : (firstGroupViewIndex <= groupIndex && groupIndex <= lastGroupViewIndex))
                    continue;

                pushGroupIndexList.Add(groupIndex);
                PushIntoGroupStack(m_dic_ActivatedGroups[groupIndex]);

                var groupData = m_list_groupDatas[groupIndex];
                for (int cellIndex = groupData.startDataIndex; cellIndex <= groupData.endDataIndex; cellIndex++)
                {
                    if (m_dic_ActivatedCells.ContainsKey(cellIndex) == false) continue;

                    pushCellIndexList.Add(cellIndex);
                    PushIntoCellStack(m_dic_ActivatedCells[cellIndex]);
                }
            }

            foreach (var pushIndex in pushCellIndexList)
                m_dic_ActivatedCells.Remove(pushIndex);
            foreach (var pushIndex in pushGroupIndexList)
                m_dic_ActivatedGroups.Remove(pushIndex);
            #endregion

            #region Set Cells
            int setStartIndex = m_Reverse ? firstGroupViewIndex : lastGroupViewIndex;
            int setLastIndex = m_Reverse ? lastGroupViewIndex : firstGroupViewIndex;
            int totalCellViewCount = m_list_groupDatas
                .Where((w, index) => m_Reverse ? (setStartIndex <= index && index <= setLastIndex) : (setLastIndex <= index && index <= setStartIndex))
                .Sum(s => s.cellCount);
            int lastCellViewIndex = totalCellViewCount - 1;

            for (int i = setStartIndex; i >= setLastIndex; i--)
            {
                // 이미 활성화된 그룹이 존재하는 경우, 해당 그룹을 재활용
                var isAlreadyActivated = m_dic_ActivatedGroups.ContainsKey(i) && m_dic_ActivatedGroups[i];
                if (isAlreadyActivated == false) m_dic_ActivatedGroups.Remove(i);

                var getGroup = isAlreadyActivated ? m_dic_ActivatedGroups[i] : PopFromGroupStack();
                if (getGroup == false) continue;

                var groupData = m_list_groupDatas[i];
                var cellStartIndex = groupData.startDataIndex;
                var cellLastIndex = groupData.endDataIndex;
                var sortedStartIndex = m_Reverse ? cellLastIndex : cellStartIndex;
                var sortedLastIndex = m_Reverse ? cellStartIndex : cellLastIndex;
                var cellIsNothing = groupData.cellCount == 0;

                // 존재했던 그룹이 아니라면 그룹 오브젝트 설정
                if (isAlreadyActivated == false)
                {
                    if (cellIsNothing == false) ResetGroupWithCellRange(getGroup, i, sortedStartIndex, sortedLastIndex);
                    else ResetGroupNoCells(getGroup, i);
                }

                // 그룹 내 셀 설정
                for (int j = sortedStartIndex; j <= sortedLastIndex && cellIsNothing == false; j++)
                {
                    var isAlreadyActivatedCell = m_dic_ActivatedCells.ContainsKey(j) && m_dic_ActivatedCells[j];
                    if (isAlreadyActivatedCell == false) m_dic_ActivatedCells.Remove(j);

                    var getCell = isAlreadyActivatedCell ? m_dic_ActivatedCells[j] : del.GetCell(this, j, lastCellViewIndex);
                    if (getCell == false) continue;

                    if (isAlreadyActivatedCell == false)
                    {
                        getCell.gameObject.name = string.Format("{0}_Index({1})", getCell.GetType().ToString(), j);

                        // 셀 오브젝트의 하이어라키 위치 설정
                        getCell.transform.SetParent(getGroup.transform);
                        getCell.transform.SetAsLastSibling();
                    }

                    getCell.SetCellViewIndex(lastCellViewIndex);
                    m_dic_ActivatedCells.TryAdd(j, getCell);

                    lastCellViewIndex--;
                }

                m_dic_ActivatedGroups.TryAdd(i, getGroup);

                // 그룹 오브젝트의 하이어라키 위치 설정
                getGroup.transform.SetSiblingIndex(1);
            }
            #endregion
            #endregion
        }

        public void ReloadCellView()
        {
            if (TryAddWaitBuffer_AndCheckCurrentState(ReloadCellView, out _)) return;

            ResetAllCellsAndGroups();
            if (del is null || del.GetCellCount(this) <= 0) return;
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
            var groupCount = m_list_groupDatas.Count;
            if (groupCount == 0) return (-1, -1);

            // 현재 범위에서 보여지는 셀 탐색
            // 첫번째 셀은 셀의 크기를 이용해 셀의 끝 부분이 탐색 시작 위치에 걸쳐져 있는 지 확인이 필요함
            // 마지막 셀은 셀의 위치를 이용해 셀의 시작 부분이 탐색 종료 위치에 걸쳐져 있는 지 확인이 필요함
            // 즉, 셀의 끝 부분이 탐색 시작 위치보다 크거나 같고, 셀의 시작 부분이 탐색 종료 위치보다 작거나 같은 셀들을 찾아야 함
            // 이를 이용해 보여지는 셀의 범위를 찾을 수 있음
            var visibleGroups = m_dp_groupPos
                .Select(
                    (pos, index) => new
                    {
                        index,
                        pos,
                        endPos = pos + m_list_groupDatas[index].size
                    })
                .Where(x => x.endPos >= topBoundaryPos && x.pos <= bottomBoundaryPos)
                .ToArray();
            if (visibleGroups.Any() == false) return (-1, -1);

            int firstIndex = visibleGroups.First().index;
            int lastIndex = visibleGroups.Last().index;
            return (firstIndex, lastIndex);
        }

        private CellGroupData CreateCellGroupData(int startIndex, int totalCellCount, float maxGroupWidth)
        {
            // 시작 인덱스가 총 셀 수를 넘지 않도록 제한
            startIndex = Mathf.Clamp(startIndex, 0, totalCellCount);

            // 그룹의 초기 너비와 크기를 설정
            var sizeVec = m_list_cellSizeVec[startIndex];
            var cellsWidthInGroup = sizeVec.Width;
            var groupSize = sizeVec.Size;

            // 새로운 셀 그룹 데이터를 생성
            var newGroup = new CellGroupData(startIndex);

            // 그룹에 셀을 넣을 수 있는 지 체크
            // 그룹에 셀을 최소 하나는 포함하도록 설정
            int endIndex = totalCellCount;
            if (FixCellCountInGroup) endIndex = Mathf.Min(startIndex + m_fixedCellCount, totalCellCount);
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
                var nextCellWidth = l_sizeVec.Width;

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