using EaseUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecycleScroll
{
    public partial class RecycleScroller
    {
        #region ETC

        private void Init(LoadParam[] _params)
        {
            if (m_isInitialized) return;
            m_isInitialized = true;

            ExecuteInitializer(_params);

            TopPadding = m_scrollerMode.GetTopPadding(m_spacing, m_padding, ScrollAxis);
            BottomPadding = m_scrollerMode.GetBottomPadding(m_spacing, m_padding, ScrollAxis);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_RectTransform);

            var inst = new GameObject(TRASH_OBJECT_NAME);
            inst.transform.SetParent(transform);
            var trash = inst.AddComponent<RectTransform>();
            trash.localScale = Vector3.one;
            inst.SetActive(false);
            var trashList = new List<Transform>();
            foreach (Transform child in Content)
            {
                if (child == false || child.gameObject == false) continue;

                child.gameObject.SetActive(false);
                trashList.Add(child);
            }

            foreach (var child in trashList) child.SetParent(trash);

            inst = new GameObject("SpaceCell_Top");
            inst.transform.SetParent(Content);
            inst.transform.localScale = Vector3.one;
            m_rt_spcCell_top = inst.AddComponent<RectTransform>();

            inst = new GameObject("SpaceCell_Bottom");
            inst.transform.SetParent(Content);
            inst.transform.localScale = Vector3.one;
            m_rt_spcCell_bottom = inst.AddComponent<RectTransform>();

            ResetContentSizeFitter();
            ResetSpaceCellsHierarchy();
            ResetSpaceCellsWidth();

            SetAlignmentValuesToContentLayout();

            ResetMaxGroupWidthValue();
        }

        public Dictionary<int, RecycleScrollerCell> GetAllActivatedCells() => m_dict_activatedCells;

        /// <returns>Content의 가장 처음으로부터 얼만큼 떨어져 있는 지 계산하여 반환</returns>
        private float CalculateDistanceTo(int cellIndex)
        {
            cellIndex = Mathf.Clamp(cellIndex, 0, m_dict_groupIndexOfCell.Count - 1);
            var groupIndex = m_dict_groupIndexOfCell[cellIndex];
            return m_dp_groupPos[groupIndex];
        }

        private void StopAllMoveCor()
        {
            StopMoveContentCor();
        }

        private int FindRealClosestGroupIndexFrom(float pivot_real) => m_dp_groupPos.FindClosestIndex(pivot_real);

        public void ClearCellCollections()
        {
            m_dict_activatedCells.Clear();
            PushAllActivatedGroups();
        }
        private void PushAllActivatedGroups()
        {
            foreach (var groupIndex in m_dict_activatedGroups.Keys)
            {
                var curGroup = m_dict_activatedGroups[groupIndex];
                if (!curGroup) continue;

                PushIntoGroupStack(curGroup);
            }

            m_dict_activatedGroups.Clear();
        }
        private void PushAllActivatedCells()
        {
            foreach (var cellIndex in m_dict_activatedCells.Keys)
            {
                var curCell = m_dict_activatedCells[cellIndex];
                if (!curCell) continue;

                PushIntoCellStack(curCell);
            }

            m_dict_activatedCells.Clear();
        }

        [return: NotNull] private GameObject CreateEmptyGameObject(string name, [NotNull] Transform parent)
        {
            var inst = new GameObject(name);
            inst.transform.SetParent(parent);
            inst.AddComponent<RectTransform>();
            inst.transform.localScale = Vector3.one;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            return inst;
        }

        public float ConvertRealToShow(float realValue)
        {
            return m_scrollerMode.ConvertRealToShow(realValue, ShowingContentSize);
        }

        public float ConvertShowToReal(float showValue)
        {
            return m_scrollerMode.ConvertShowToReal(showValue, ShowingContentSize);
        }

        private void SetDirty()
        {
            if (isActiveAndEnabled == false || Content == false) return;

            LayoutRebuilder.MarkLayoutForRebuild(_RectTransform);
        }

        #endregion

        #region Set Component Values

        public void ResetContentSizeFitter()
        {
            if (Content == false) return;

            if (Content.TryGetComponent<ContentSizeFitter>(out var fitter) == false || fitter == false)
                return;

            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        private void UpdateScrollAxisToScrollRect()
        {
            _ScrollRect.horizontal = ScrollAxis == eScrollAxis.HORIZONTAL;
            _ScrollRect.vertical = ScrollAxis == eScrollAxis.VERTICAL;
        }

        private void ResetSpaceCellsWidth()
        {
            float width = ScrollAxis switch
            {
                eScrollAxis.VERTICAL => Content.sizeDelta.x,
                eScrollAxis.HORIZONTAL => Content.sizeDelta.y,
                _ => 0f
            };
            var size = ScrollAxis switch
            {
                eScrollAxis.VERTICAL => new Vector2(width, 0f),
                eScrollAxis.HORIZONTAL => new Vector2(0f, width),
                _ => Vector2.zero
            };

            var pivot = Vector2.up;
            if (m_rt_spcCell_top)
            {
                m_rt_spcCell_top.sizeDelta = size;
                m_rt_spcCell_top.pivot = pivot;
            }

            if (m_rt_spcCell_bottom)
            {
                m_rt_spcCell_bottom.sizeDelta = size;
                m_rt_spcCell_bottom.pivot = pivot;
            }
        }

        private void ResetSpaceCellsHierarchy()
        {
            m_rt_spcCell_top?.SetAsFirstSibling();
            m_rt_spcCell_bottom?.SetAsLastSibling();
        }

        private Type GetNeedLayoutGroupType()
        {
            return ScrollAxis switch
            {
                eScrollAxis.VERTICAL => typeof(VerticalLayoutGroup),
                eScrollAxis.HORIZONTAL => typeof(HorizontalLayoutGroup),
                _ => throw new InvalidOperationException("Unsupported scroll axis type."),
            };
        }

        private void CheckLayoutGroupToContent()
        {
            var needType = GetNeedLayoutGroupType();
            var curLayoutGroup = Content.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (curLayoutGroup && curLayoutGroup.GetType() == needType) return;

            if (curLayoutGroup)
            {
#if UNITY_EDITOR
                if (Application.isPlaying) DestroyImmediate(curLayoutGroup);
                else UnityEditor.Undo.DestroyObjectImmediate(curLayoutGroup);
#endif
            }

#if UNITY_EDITOR
            AddLayoutGroup(needType);
#endif
        }

        private void AddLayoutGroup(Type needType)
        {
            Content.gameObject.AddComponent(needType);
        }

        private void SetAlignmentValuesToContentLayout()
        {
            if (Content.TryGetComponent<HorizontalOrVerticalLayoutGroup>(out var layoutGroup) == false) return;

            m_padding ??= new();

            var copyPadding = new RectOffset(m_padding.left, m_padding.right, m_padding.top, m_padding.bottom);
            if (m_loopScrollable)
            {
                switch (ScrollAxis)
                {
                    case eScrollAxis.VERTICAL:
                        copyPadding.top = (int)TopPadding;
                        copyPadding.bottom = (int)BottomPadding;
                        break;
                    case eScrollAxis.HORIZONTAL:
                        copyPadding.left = (int)TopPadding;
                        copyPadding.right = (int)BottomPadding;
                        break;
                }
            }

            layoutGroup.padding = copyPadding;
            layoutGroup.spacing = Spacing;
            layoutGroup.childAlignment = m_childAlignment;
            layoutGroup.reverseArrangement = m_reverse;

            switch (ScrollAxis)
            {
                case eScrollAxis.VERTICAL:
                    m_controlChildSize.height = false;
                    m_childForceExpand.height = false;
                    break;
                case eScrollAxis.HORIZONTAL:
                    m_controlChildSize.width = false;
                    m_childForceExpand.width = false;
                    break;
            }

            layoutGroup.childControlHeight = ScrollAxis == eScrollAxis.HORIZONTAL || m_controlChildSize.height;
            layoutGroup.childControlWidth = ScrollAxis == eScrollAxis.VERTICAL || m_controlChildSize.width;
            layoutGroup.childScaleHeight = m_useChildScale.height;
            layoutGroup.childScaleWidth = m_useChildScale.width;
            layoutGroup.childForceExpandHeight = ScrollAxis == eScrollAxis.HORIZONTAL || m_childForceExpand.height;
            layoutGroup.childForceExpandWidth = ScrollAxis == eScrollAxis.VERTICAL || m_childForceExpand.width;
        }

        private Vector2 GetAlignmentPoint()
        {
            // 수직 스크롤일 경우 상단, 그리고 layoutGroup의 child alignment값에 따라 왼쪽, 중앙, 오른쪽으로 설정
            // 수평 스크롤일 경우 왼쪽, 그리고 layoutGroup의 child alignment값에 따라 상단, 중앙, 하단으로 설정
            var x = ScrollAxis switch
            {
                eScrollAxis.VERTICAL => m_childAlignment switch
                {
                    TextAnchor.UpperLeft or TextAnchor.LowerLeft or TextAnchor.MiddleLeft => 0f,
                    TextAnchor.UpperCenter or TextAnchor.LowerCenter or TextAnchor.MiddleCenter => 0.5f,
                    TextAnchor.UpperRight or TextAnchor.LowerRight or TextAnchor.MiddleRight => 1f,
                    _ => 0f,
                },
                _ => 0f,
            };
            var y = ScrollAxis switch
            {
                eScrollAxis.HORIZONTAL => m_childAlignment switch
                {
                    TextAnchor.LowerLeft or TextAnchor.LowerCenter or TextAnchor.LowerRight => 0f,
                    TextAnchor.MiddleLeft or TextAnchor.MiddleCenter or TextAnchor.MiddleRight => 0.5f,
                    TextAnchor.UpperLeft or TextAnchor.UpperCenter or TextAnchor.UpperRight => 1f,
                    _ => 1f,
                },
                _ => 1f,
            };

            return new Vector2(x, y);
        }

        public void ResetContent_Pivot()
        {
            Content.pivot = GetAlignmentPoint();
        }

        public void ResetContent_Anchor()
        {
            Content.anchorMin = Content.anchorMax = GetAlignmentPoint();
        }

        private void ResetContent_Size()
        {
            var totalSize = RealContentSize;
            var viewportSize = ViewportSize;
            float axisSize = m_fitContentToViewport && totalSize < viewportSize
                ? viewportSize
                : totalSize;

            Content.sizeDelta = ScrollAxis == eScrollAxis.VERTICAL
                ? new Vector2(Viewport.rect.width, axisSize)
                : new Vector2(axisSize, Viewport.rect.height);
        }

        private Type GetNeedLayoutGroupTypeOfGroupCell()
        {
            return ScrollAxis switch
            {
                eScrollAxis.VERTICAL => typeof(HorizontalLayoutGroup),
                eScrollAxis.HORIZONTAL => typeof(VerticalLayoutGroup),
                _ => throw new InvalidOperationException("Unsupported scroll axis type."),
            };
        }

        private void SetLayoutGroupFieldOfCellGroup(HorizontalOrVerticalLayoutGroup layout)
        {
            if (layout == false) return;

            layout.padding = new RectOffset();
            layout.spacing = m_spacingInGroup;
            layout.childAlignment = m_childAlignment;
            layout.reverseArrangement = m_reverse;
            layout.childControlWidth = m_controlChildSize.width;
            layout.childControlHeight = m_controlChildSize.height;
            layout.childScaleWidth = m_useChildScale.width;
            layout.childScaleHeight = m_useChildScale.height;
            layout.childForceExpandWidth = m_childForceExpand.width;
            layout.childForceExpandHeight = m_childForceExpand.height;

            if (layout.TryGetComponent<RectTransform>(out var rtf) == false)
                rtf = layout.gameObject.AddComponent<RectTransform>();
            rtf.anchorMax = rtf.anchorMin = rtf.pivot = Vector2.up;

            var sizeDelta = rtf.sizeDelta;
            switch (ScrollAxis)
            {
                case eScrollAxis.VERTICAL:
                    sizeDelta.x = Content.rect.width - m_padding.left - m_padding.right;
                    break;
                case eScrollAxis.HORIZONTAL:
                    sizeDelta.y = Content.rect.height - m_padding.top - m_padding.bottom;
                    break;
            }

            rtf.sizeDelta = sizeDelta;
        }

        #endregion

        #region Scroll Rect Listener

        /// <summary>
        /// Scroll Rect OnValueChanged Listener;
        /// </summary>
        /// <param name="val"></param>
        public void OnScrollRectScrolling(Vector2 val)
        {
            if (m_scrollerMode.NeedReposition(RealScrollPosition, FrontThreshold, BackThreshold, RealScrollSize))
            {
                var velocity = _ScrollRect.velocity;
                ShowingScrollPosition = ShowingScrollPosition;
                _ScrollRect.velocity = velocity;
            }

            SetScrollbarValueWithoutNotify();

            UpdateCellView();
            onScroll?.Invoke(val);

            var currentPos = RealScrollPosition;
            if (Mathf.Abs(currentPos - m_previousScrollPosition) > 0.01f)
            {
                var direction = currentPos > m_previousScrollPosition
                    ? eScrollDirection.Forward
                    : eScrollDirection.Backward;
                onScrollDirectionChanged?.Invoke(direction);
                m_previousScrollPosition = currentPos;
            }

            var nearestPageIndex = NearestPageIndexByScrollPos;
            if (m_prevPageIndexByScrollPos != nearestPageIndex) ChangeCurrentPageIndex(nearestPageIndex);
        }

        #endregion

        #region Pooling

        [return: NotNull] private string GetMergedSubKey(params string[] subKeys)
        {
            // subKeys배열이 비어있는 경우 디폴트 키 반환
            if (subKeys == null || subKeys.Length == 0) return DEFAULT_POOL_SUBKEY;

            // 빈 문자열 제거 후 '//'로 합침
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < subKeys.Length; i++)
            {
                if (string.IsNullOrEmpty(subKeys[i])) continue;
                if (sb.Length > 0) sb.Append("//");
                sb.Append(subKeys[i]);
            }
            return sb.Length == 0 ? DEFAULT_POOL_SUBKEY : sb.ToString();
        }

        /// <summary>
        /// 풀에서 셀 인스턴스를 하나 빼내옴<para/>
        /// 풀에 인스턴스가 존재하지 않을 경우 전달 받은 프리팹을 이용해 인스턴스 생성 후 반환<para/>
        /// 이미 활성화된 셀 중 해당 인덱스의 셀이 존재한다면, 필요한 셀인지 확인 후 반환
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="dataIndex"></param>
        /// <param name="subKey">빈 문자열인 경우 디폴트 키 사용</param>
        /// <returns></returns>
        [return: NotNull] public RecycleScrollerCell GetCellInstance([NotNull] RecycleScrollerCell prefab, int dataIndex, params string[] subKeys)
        {
            var type = prefab.GetType();
            // 이미 활성화되어 있는 셀이라면 해당 셀 반환
            if (m_dict_activatedCells.TryGetValue(dataIndex, out var cell) && cell?.GetType() == type) return cell;
            // 입력 프리팹과 다른 타입의 셀이라면 해당 셀을 풀에 반환
            if (cell) PushIntoCellStack(cell);

            m_dict_activatedCells.Remove(dataIndex);

            var mergedSubKey = GetMergedSubKey(subKeys);
            var ret = PopFromCellStack(prefab, dataIndex, type, mergedSubKey);

            ret.PoolSubKey = mergedSubKey;
            ret.gameObject.SetActive(true);

            ResetSpaceCellsHierarchy();

            return ret;
        }

        #region Pop & Push

        #region Cell

        [return: NotNull] private RecycleScrollerCell CreateCell([NotNull] RecycleScrollerCell prefab, int dataIndex)
        {
            if (CellCreateFuncWhenPoolEmpty == null) return Instantiate(prefab, Tf_CellPool);

            var newInst = CellCreateFuncWhenPoolEmpty.Invoke(prefab, Tf_CellPool, dataIndex);
            if (newInst == false) newInst = Instantiate(prefab, Tf_CellPool);

            return newInst;
        }

        [return: NotNull] private Stack<RecycleScrollerCell> GetCellStack(Type type, string subKey = DEFAULT_POOL_SUBKEY)
        {
            if (m_pool_cells.ContainsKey(type) == false) m_pool_cells.Add(type, new());

            var mainPool = m_pool_cells[type];
            if (string.IsNullOrEmpty(subKey)) subKey = DEFAULT_POOL_SUBKEY;
            if (mainPool.ContainsKey(subKey) == false) mainPool.Add(subKey, new());

            return mainPool[subKey];
        }

        [return: NotNull]
        private RecycleScrollerCell PopFromCellStack([NotNull] RecycleScrollerCell prefab, int dataIndex, Type type, string subKey = DEFAULT_POOL_SUBKEY)
        {
            var stack = GetCellStack(type, subKey);
            var obj = stack.Count > 0 ? stack.Pop() : null;
            if (obj == false) obj = CreateCell(prefab, dataIndex);
            return obj;
        }

        private void PushIntoCellStack(RecycleScrollerCell cell)
        {
            if (cell == false) return;
            cell.SetCellViewIndex(-1);

            cell.gameObject.SetActive(false);
            cell.transform.SetParent(Tf_CellPool);

            var stack = GetCellStack(cell.GetType(), cell.PoolSubKey);
            if (m_maxPoolSizePerType > 0 && stack.Count >= m_maxPoolSizePerType)
            {
                Destroy(cell.gameObject);
                return;
            }
            stack.Push(cell);
        }

        #endregion

        #region Group

        private void ResetGroupTransform(HorizontalOrVerticalLayoutGroup groupObject)
        {
            SetLayoutGroupFieldOfCellGroup(groupObject);
            groupObject.transform.SetParent(Content);
            groupObject.transform.localScale = Vector3.one;
        }

        private void ResetGroupSize(HorizontalOrVerticalLayoutGroup groupObject, int groupIndex)
        {
            var groupRect = groupObject.GetComponent<RectTransform>();
            var sizeDelta = groupRect.sizeDelta;
            var groupSize = m_list_groupData[groupIndex].size;
            switch (ScrollAxis)
            {
                case eScrollAxis.VERTICAL:
                    sizeDelta.y = groupSize;
                    break;
                case eScrollAxis.HORIZONTAL:
                    sizeDelta.x = groupSize;
                    break;
            }

            groupRect.sizeDelta = sizeDelta;
        }
        private void ResetGroup(HorizontalOrVerticalLayoutGroup groupObject, int groupIndex)
        {
            ResetGroupTransform(groupObject);
            ResetGroupSize(groupObject, groupIndex);
        }
        private void ResetGroupWithCellRange(HorizontalOrVerticalLayoutGroup groupObject, int groupIndex, int sortedStartIndex, int sortedLastIndex)
        {
            ResetGroup(groupObject, groupIndex);
            groupObject.gameObject.name = string.Format("Group({0}), Cell Index({1} ~ {2})", groupIndex, sortedStartIndex, sortedLastIndex);
        }
        private void ResetGroupNoCells(HorizontalOrVerticalLayoutGroup groupObject, int groupIndex)
        {
            ResetGroup(groupObject, groupIndex);
            groupObject.gameObject.name = string.Format("Group({0}), No Cells", groupIndex);
        }

        [return: NotNull] private HorizontalOrVerticalLayoutGroup PopFromGroupStack()
        {
            var needType = GetNeedLayoutGroupTypeOfGroupCell();

            var pop = m_pool_groups.Count > 0 ? m_pool_groups.Pop() : null;
            if (pop && pop.GetType() != needType)
            {
                Destroy(pop.gameObject);
                pop = null;
            }

            if (pop) goto L_RETURN;

            var inst = CreateEmptyGameObject("Group", Tf_GroupPool);
            pop = (HorizontalOrVerticalLayoutGroup)inst.AddComponent(needType);

            L_RETURN:
            pop.gameObject.SetActive(true);
            return pop;
        }

        private void PushIntoGroupStack(HorizontalOrVerticalLayoutGroup group)
        {
            if (group == false) return;

            group.gameObject.SetActive(false);
            group.transform.SetParent(Tf_GroupPool);

            m_pool_groups.Push(group);
        }

        #endregion

        #endregion

        #endregion

        #region Move Content

        #region MoveTo

        private void MoveTo_Base(float pos, float duration, bool normalizedPos, float offset, Action<float, float> moveCorWhenDurationOverZero)
        {
            duration = Mathf.Max(duration, 0f);

            if (normalizedPos) pos = Mathf.Clamp01(pos + offset);
            else pos = Mathf.Clamp(pos + offset, 0f, RealScrollSize) / RealScrollSize;

            if (duration <= 0f)
            {
                RealNormalizedScrollPosition = pos;
                return;
            }

            moveCorWhenDurationOverZero(pos, duration);
        }

        /// <summary>
        /// 지정된 위치로 콘텐츠를 이동합니다.
        /// normalizedPos가 true일 경우 offset 값도 노멀라이즈 값으로 사용됩니다.
        /// </summary>
        /// <param name="pos">이동할 목표 위치</param>
        /// <param name="curve">애니메이션 커브</param>
        /// <param name="duration">이동에 소요되는 시간</param>
        /// <param name="normalizedPos">위치 값이 노멀라이즈되었는지 여부</param>
        /// <param name="offset">위치 오프셋 값</param>
        public void MoveTo(float pos, AnimationCurve curve, float duration = 0f, bool normalizedPos = true, float offset = 0f)
            => MoveTo_Base(pos, duration, normalizedPos, offset, (p, d) => StartMoveContentCor(p, d, curve));

        /// <summary>
        /// 지정된 위치로 콘텐츠를 이동합니다.
        /// normalizedPos가 true일 경우 offset 값도 노멀라이즈 값으로 사용됩니다.
        /// </summary>
        /// <param name="pos">이동할 목표 위치</param>
        /// <param name="ease">이동 이징 커브 함수</param>
        /// <param name="duration">이동에 소요되는 시간</param>
        /// <param name="normalizedPos">위치 값이 노멀라이즈되었는지 여부</param>
        /// <param name="offset">위치 오프셋 값</param>
        public void MoveTo(float pos, Ease ease, float duration = 0f, bool normalizedPos = true, float offset = 0f)
            => MoveTo_Base(pos, duration, normalizedPos, offset, (p, d) => StartMoveContentCor(p, d, ease));

        /// <summary>
        /// 지정된 위치로 콘텐츠를 이동합니다.
        /// normalizedPos가 true일 경우 offset 값도 노멀라이즈 값으로 사용됩니다.
        /// </summary>
        /// <param name="pos">이동할 목표 위치</param>
        /// <param name="duration">이동에 소요되는 시간</param>
        /// <param name="normalizedPos">위치 값이 노멀라이즈되었는지 여부</param>
        /// <param name="offset">위치 오프셋 값</param>
        public void MoveTo(float pos, float duration = 0f, bool normalizedPos = true, float offset = 0f)
            => MoveTo(pos, Ease.Linear, duration, normalizedPos, offset);

        /// <summary>
        /// 지정된 위치로 콘텐츠를 이동합니다.
        /// normalizedPos가 true일 경우 offset 값도 노멀라이즈 값으로 사용됩니다.
        /// </summary>
        /// <param name="pos">이동할 목표 위치</param>
        /// <param name="duration">이동에 소요되는 시간</param>
        /// <param name="normalizedPos">위치 값이 노멀라이즈되었는지 여부</param>
        /// <param name="offset">위치 오프셋 값</param>
        public void MoveTo_UsePagingEaseConfig(float pos, float duration = 0f, bool normalizedPos = true, float offset = 0f)
        {
            var useCustomEase = m_pagingData.useCustomEase;
            if (useCustomEase) MoveTo(pos, m_pagingData.customEase, duration, normalizedPos, offset);
            else MoveTo(pos, m_pagingData.easeFunction, duration, normalizedPos, offset);
        }

        #endregion

        #region MoveToIndex

        #region MoveToIndex_ViaViewportSizeAligned(Base)

        private void MoveToIndex_ViaViewportSizeAligned_Base(int index, float viewportNormalSize, Action<float> moveAction)
        {
            if (TryAddWaitBuffer_AndCheckCurrentState(() => MoveToIndex_ViaViewportSizeAligned_Base(index, viewportNormalSize, moveAction), out _)) return;

            var pos = CalculateDistanceTo(index) - ViewportSize * viewportNormalSize;
            pos = Mathf.Min(pos, RealScrollSize);

            moveAction(pos);
        }

        public void MoveToIndex_ViaViewportSizeAligned(int index, Ease ease, float viewportNormalSize, float duration = 0f, float offset = 0f)
            => MoveToIndex_ViaViewportSizeAligned_Base(index, viewportNormalSize, p => MoveTo(p, ease, duration, false, offset));

        public void MoveToIndex_ViaViewportSizeAligned(int index, AnimationCurve curve, float viewportNormalSize, float duration = 0f, float offset = 0f)
            => MoveToIndex_ViaViewportSizeAligned_Base(index, viewportNormalSize, p => MoveTo(p, curve, duration, false, offset));

        public void MoveToIndex_ViaViewportSizeAligned_UsePageEase(int index, float viewportNormalSize, float duration = 0f, float offset = 0f)
            => MoveToIndex_ViaViewportSizeAligned_Base(index, viewportNormalSize, p => MoveTo_UsePagingEaseConfig(p, duration, false, offset));

        public void MoveToIndex_ViaViewportSizeAligned(int index, float viewportNormalSize, float duration = 0f, float offset = 0f)
            => MoveToIndex_ViaViewportSizeAligned(index, Ease.Linear, viewportNormalSize, duration, offset);

        #endregion

        #region MoveToIndex

        public void MoveToIndex(int index, Ease ease, float duration = 0f, float offset = 0f)
            => MoveToIndex_ViaViewportSizeAligned(index, ease, 0f, duration, offset);

        public void MoveToIndex(int index, AnimationCurve curve, float duration = 0f, float offset = 0f)
            => MoveToIndex_ViaViewportSizeAligned(index, curve, 0f, duration, offset);

        public void MoveToIndex_UsePageEase(int index, float duration = 0f, float offset = 0f)
            => MoveToIndex_ViaViewportSizeAligned_UsePageEase(index, 0f, duration, offset);

        public void MoveToIndex(int index, float duration = 0f, float offset = 0f) => MoveToIndex(index, Ease.Linear, duration, offset);

        #endregion

        #region MoveToIndex_Center

        public void MoveToIndex_ViewportCenter(int index, Ease ease, float duration = 0f, float offset = 0f)
            => MoveToIndex_ViaViewportSizeAligned(index, ease, 0.5f, duration, offset);

        public void MoveToIndex_ViewportCenter(int index, AnimationCurve curve, float duration = 0f, float offset = 0f)
            => MoveToIndex_ViaViewportSizeAligned(index, curve, 0.5f, duration, offset);

        public void MoveToIndex_ViewportCenter_UseCellSizeVec(int index, Func<CellSizeVector, float> offsetFunc, Ease ease, float duration = 0f)
        {
            var cellSize = m_list_cellSizeVec[index];
            var offset = offsetFunc(cellSize);
            MoveToIndex_ViewportCenter(index, ease, duration, offset);
        }

        public void MoveToIndex_ViewportCenter_UseCellSizeVec(int index, Func<CellSizeVector, float> offsetFunc, AnimationCurve curve, float duration = 0f)
        {
            var cellSize = m_list_cellSizeVec[index];
            var offset = offsetFunc(cellSize);
            MoveToIndex_ViewportCenter(index, curve, duration, offset);
        }

        public void MoveToIndex_ViewportCenter_UseCellSize(int index, Func<float, float> offsetFunc, float duration = 0f)
        {
            MoveToIndex_ViewportCenter_UseCellSizeVec(index,
                cellSize =>
                {
                    var size = cellSize.Size;
                    return offsetFunc(size);
                },
                Ease.Linear,
                duration);
        }

        public void MoveToIndex_ViewportCenter_UsePageEase(int index, float duration = 0f, float offset = 0f)
            => MoveToIndex_ViaViewportSizeAligned_UsePageEase(index, 0.5f, duration, offset);

        public void MoveToIndex_ViewportCenter_UseCellSizePageEase(int index, Func<CellSizeVector, float> offsetFunc, float duration = 0f)
        {
            var cellSize = m_list_cellSizeVec[index];
            var offset = offsetFunc(cellSize);
            MoveToIndex_ViewportCenter_UsePageEase(index, duration, offset);
        }

        public void MoveToIndex_ViewportCenter(int index, float duration = 0f, float offset = 0f)
            => MoveToIndex_ViewportCenter(index, Ease.Linear, duration, offset);

        public void MoveToIndex_ViewportCenter_BasedCellCenter(int index, float duration = 0f)
            => MoveToIndex_ViewportCenter_UseCellSize(index, size => size / 2f, duration);

        #endregion

        #endregion

        #region JumpTo & JumpToIndex

        public void JumpTo(float pos, bool normalizedPos = true, float offset = 0f) => MoveTo(pos, normalizedPos: normalizedPos, offset: offset);

        public void JumpToIndex_ViaViewportSizeAligned(int index, float viewportNormalSize, float offset = 0f)
            => MoveToIndex_ViaViewportSizeAligned(index, viewportNormalSize, offset: offset);

        public void JumpToIndex(int index, float offset = 0f) => MoveToIndex(index, offset: offset);

        public void JumpToIndex_ViewportCenter(int index, float offset = 0f) => MoveToIndex_ViewportCenter(index, offset: offset);

        public void JumpToIndex_ViewportCenter_UseCellSizeVec(int index, Func<CellSizeVector, float> offsetFunc)
        {
            var cellSize = m_list_cellSizeVec[index];
            var offset = offsetFunc(cellSize);
            JumpToIndex_ViewportCenter(index, offset);
        }

        public void JumpToIndex_ViewportCenter_UseCellSize(int index, Func<float, float> offsetFunc)
            => JumpToIndex_ViewportCenter_UseCellSizeVec(index, sizeVec => offsetFunc(sizeVec.Size));

        public void JumpToIndex_ViewportCenter_BasedCellCenter(int index) => JumpToIndex_ViewportCenter_UseCellSize(index, size => size / 2f);

        #endregion

        #region MoveToPage

        private static int GetNextPageIndex(int pageIndex, bool isLoop, int pageCount)
        {
            var nextIndex = pageIndex + 1;
            if (nextIndex >= pageCount) nextIndex = isLoop ? 0 : pageCount - 1;
            return nextIndex;
        }
        private static int GetPrevPageIndex(int pageIndex, bool isLoop, int pageCount)
        {
            var prevIndex = pageIndex - 1;
            if (prevIndex < 0) prevIndex = isLoop ? pageCount - 1 : 0;
            return prevIndex;
        }

        private int NextRealPageIndex
            => GetNextPageIndex(FindRealClosestPageIndexFrom(PagePivotPosInScrollRect), LoopScrollIsOn, RealPageCount);
        private int PrevRealPageIndex
            => GetPrevPageIndex(FindRealClosestPageIndexFrom(PagePivotPosInScrollRect), LoopScrollIsOn, RealPageCount);

        public void MoveToPage(int pageIndex)
        {
            var pagePosition = GetPagePivotPos(pageIndex);
            MoveTo(pagePosition, m_pagingData.duration, false, m_pagingData.PagePivot);
        }
        public void MoveToNextPage() => MoveToPage(NextRealPageIndex);
        public void MoveToPrevPage() => MoveToPage(PrevRealPageIndex);

        public void JumpToPage(int pageIndex)
        {
            var pagePosition = GetPagePivotPos(pageIndex);
            JumpTo(pagePosition);
        }
        public void JumpToNextPage() => JumpToPage(NextRealPageIndex);
        public void JumpToPrevPage() => JumpToPage(PrevRealPageIndex);

        #endregion

        #region Coroutines

        private IEnumerator Cor_MoveContent(float endPos, float duration, Func<float, float, float, float> evaluate)
        {
            var progress = 0f;
            var startPos = RealNormalizedScrollPosition;
            while (progress < duration && Mathf.Approximately(progress, duration) == false)
            {
                yield return null;
                progress += Time.deltaTime;
                var normalizeTime = progress / duration;
                RealNormalizedScrollPosition = evaluate(startPos, endPos, normalizeTime);
            }

            RealNormalizedScrollPosition = endPos;
            onEndEasing?.Invoke();
            m_corMoveContent = null;
        }

        private IEnumerator Cor_MoveContent(float endPos, float duration, Ease ease) =>
            Cor_MoveContent(endPos, duration, (s, e, d) => ease.Evaluate(s, e, d));

        private IEnumerator Cor_MoveContent(float endPos, float duration, AnimationCurve ease) =>
            Cor_MoveContent(endPos, duration, (s, e, d) => s + ease.Evaluate(d) * (e - s));

        private void StopMoveContentCor()
        {
            if (m_corMoveContent != null)
            {
                StopCoroutine(m_corMoveContent);
                m_corMoveContent = null;
            }
        }

        private void StartMoveContentCor(Func<IEnumerator> cor)
        {
            if (gameObject.activeInHierarchy == false) return;
            if (TryAddWaitBuffer_AndCheckCurrentState(() => StartMoveContentCor(cor), out _)) return;

            StopAllMoveCor();
            m_corMoveContent = cor();
            StartCoroutine(m_corMoveContent);
        }

        private void StartMoveContentCor(float endPos, float duration, Ease ease)
            => StartMoveContentCor(() => Cor_MoveContent(endPos, duration, ease));

        private void StartMoveContentCor(float endPos, float duration, AnimationCurve curve)
            => StartMoveContentCor(() => Cor_MoveContent(endPos, duration, curve));

        #endregion

        #endregion

        #region Drag Handlers

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            StopAllMoveCor();
            onBeginDrag?.Invoke();
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (m_pagingData.usePaging) StartPagingCor();
            onEndDrag?.Invoke();
        }

        #endregion

        #region Paging

        private void ChangeCurrentPageIndex(int current)
        {
            if (m_pagingData.usePaging == false) return;

            onChangePage?.Invoke(m_prevPageIndexByScrollPos, current);
            m_prevPageIndexByScrollPos = current;
        }

        private void StartPagingCor()
        {
            if (_ScrollRect.inertia) _ScrollRect.inertia = false;

            var nearestPageIndex = FindRealClosestPageIndexFrom(PagePivotPosInScrollRect);
            if (nearestPageIndex == -1) return;

            var endPos = Mathf.Clamp01((GetPagePivotPos(nearestPageIndex) - PagePivotPosInViewport) / RealScrollSize);
            var duration = m_pagingData.duration;
            StartMoveContentCor(() => Cor_MoveContent(endPos, duration, (s, e, d) => m_pagingData.EvaluateEase(s, e, d)));
        }

        private int ConvertToShowPageIndex(int realPageIndex)
        {
            return m_scrollerMode.ConvertToShowPageIndex(realPageIndex, RealPageCount);
        }

        /// <summary>
        /// 특정 실수형 좌표를 기준으로 가장 가까운 페이지 인덱스를 검색하여 반환
        /// </summary>
        /// <param name="pivot_real">검색 기준이 되는 실수형 좌표</param>
        /// <returns>페이지 포지션 리스트가 유효하지 않을 경우 -1을 반환하고, 유효한 경우 가장 가까운 페이지의 인덱스를 반환</returns>
        private int FindRealClosestPageIndexFrom(float pivot_real) => m_dp_pagePos.FindClosestIndex(pivot_real);
        private int FindShowingClosestPageIndexFrom(float pivot_real) => ConvertToShowPageIndex(FindRealClosestPageIndexFrom(pivot_real));

        private float GetPagePivotPos(int pageIndex)
        {
            if (m_dp_pagePos == null || m_dp_pagePos.Count == 0) return 0f;
            if (pageIndex < 0 || pageIndex >= m_dp_pagePos.Count) return 0f;
            return m_dp_pagePos[pageIndex] + GetPageSize(pageIndex) * m_pagingData.PagePivot;
        }

        private float GetPageSize(int pageIndex)
        {
            if (m_dp_pagePos == null || m_dp_pagePos.Count == 0) return 0f;
            if (pageIndex < 0 || pageIndex >= m_dp_pagePos.Count) return 0f;
            return pageIndex == m_dp_pagePos.Count - 1
                ? RealContentSize - BottomPadding - m_dp_pagePos[pageIndex]
                : m_dp_pagePos[pageIndex + 1] - m_dp_pagePos[pageIndex] - Spacing;
        }

        private int FindPageIndex_FromGroupIndex(int groupIndex)
        {
            if (m_pagingData.usePaging == false) return -1;

            var pageIndex = groupIndex / m_pagingData.countPerPage;
            return pageIndex;
        }
        private int FindPageIndex_FromCellIndex(int cellIndex)
        {
            if (m_pagingData.usePaging == false) return -1;
            if (cellIndex < 0 || cellIndex >= m_cellCount) return -1;

            var groupIndex = m_dict_groupIndexOfCell[cellIndex];
            return FindPageIndex_FromGroupIndex(groupIndex);
        }

        #endregion

        #region Insert

        public void Insert(int insertIndex, int insertCount = 1)
        {
            if (del == null) return;

            var copyIndex = insertIndex;
            if (TryAddWaitBuffer_AndCheckCurrentState(() => Insert(copyIndex, insertCount), out _)) return;

            // 삽입할 셀 수가 0 이하라면 삽입하지 않음
            if (insertCount <= 0) return;

            // 삽입할 인덱스가 총 셀 수를 넘지 않도록 제한
            insertIndex = Mathf.Clamp(insertIndex, 0, m_cellCount - 1);
            var prevCellCount = m_cellCount;
            m_cellCount += insertCount;

            // 삽입할 셀의 크기를 리스트에 추가
            for (int i = 0; i < insertCount; i++)
            {
                var index = insertIndex + i;
                m_list_cellSizeVec.Insert(index, del.GetCellRect(this, index).ToScaledValues);
            }

            RecalculateForInsert(insertIndex, prevCellCount);
        }
        public void AddToStart(int insertCount = 1) => Insert(0, insertCount);
        public void AddToEnd(int addCount = 1) => Insert(m_cellCount - 1, addCount);

        public void Remove(int removeIndex, int removeCount = 1)
        {
            if (del == null) return;

            var copyIndex = removeIndex;
            var copyCount = removeCount;
            if (TryAddWaitBuffer_AndCheckCurrentState(() => Remove(copyIndex, copyCount), out _)) return;

            // 삭제할 셀 수가 0 이하라면 삭제하지 않음
            if (removeCount <= 0) return;

            // 삭제할 인덱스가 총 셀 수를 넘지 않도록 제한
            removeIndex = Mathf.Clamp(removeIndex, 0, m_cellCount - 1);
            removeCount = Mathf.Clamp(removeCount, 1, m_cellCount);
            var prevCellCount = m_cellCount;
            m_cellCount -= removeCount;

            // 삭제할 셀의 크기를 리스트에서 제거
            for (int i = 0; i < removeCount; i++)
                m_list_cellSizeVec.RemoveAt(removeIndex);

            RecalculateForInsert(removeIndex, prevCellCount);
        }
        public void RemoveFromStart(int removeCount = 1) => Remove(0, removeCount);
        public void RemoveFromEnd(int removeCount = 1) => Remove(m_cellCount - 1, removeCount);

        private void RecalculateForInsert(int targetCellIndex, int prevCellCount)
        {
            if (m_loopScroll == false && m_cellCount > 0 && prevCellCount > 0)
            {
                // 삭제될 인덱스가 속한 그룹 인덱스 확인
                var groupIndex = m_dict_groupIndexOfCell[targetCellIndex];

                // 속한 그룹의 가장 첫 셀 인덱스 확인
                var groupStartIndex = m_list_groupData[groupIndex].startDataIndex;

                // 그룹 관련 리스트들의 요소들 중 groupIndex 이상의 값들을 초기화 및 제거
                ResetRealContentSize();
                if (groupIndex > 0)
                {
                    var frontGroupIndex = groupIndex - 1;
                    m_realContentSize += m_dp_groupPos[frontGroupIndex] + m_list_groupData[frontGroupIndex].size;
                }

                m_dp_groupPos.RemoveRange(groupIndex, m_dp_groupPos.Count - groupIndex);
                m_list_groupData.RemoveRange(groupIndex, m_list_groupData.Count - groupIndex);
                m_dict_groupIndexOfCell = m_dict_groupIndexOfCell.Where(x => x.Key < targetCellIndex).ToDictionary(x => x.Key, x => x.Value);

                // 페이지 관련 리스트들의 요소들 중 pageIndex 이상의 값들을 초기화 및 제거
                if (m_pagingData.usePaging)
                {
                    var pageIndex = FindPageIndex_FromCellIndex(targetCellIndex);
                    m_dp_pagePos.RemoveRange(pageIndex, m_dp_pagePos.Count - pageIndex);
                }

                // groupIndex부터 재계산
                CalculateTotalScrollSize(m_cellCount, startIndex: groupStartIndex);
            }
            else
            {
                ResetRealContentSize();
                ResetCollectionsForInsert();
                CalculateTotalScrollSize(m_cellCount);
            }

            ResetContent_Size();
            ReloadCellView();
        }

        #endregion

        #region Scrollbar

        private void AddDragEventToScrollbar(RecycleScrollbar scrollbar)
        {
            if (scrollbar == false) return;

            scrollbar.OnBeginDragged.AddListener(OnBeginDragForScrollbar);
            scrollbar.OnEndDragged.AddListener(OnEndDragForScrollbar);
        }
        private void RemoveDragEventAtScrollbar(RecycleScrollbar scrollbar)
        {
            if (scrollbar == false) return;

            scrollbar.OnBeginDragged.RemoveListener(OnBeginDragForScrollbar);
            scrollbar.OnEndDragged.RemoveListener(OnEndDragForScrollbar);
        }

        private void SetScrollbarSize(float size)
        {
            if (Scrollbar == null) return;

            Scrollbar.SetSize(size);
        }
        private void SetScrollbarSize() => SetScrollbarSize(ShowingContentSize > 0f ? ViewportSize / ShowingContentSize : 1f);

        private void SetScrollbarValueWithoutNotify()
        {
            if (Scrollbar == null) return;

            float normalizedPos = m_scrollerMode.GetScrollbarNormalizedPosition(
                ShowingNormalizedScrollPosition, RealNormalizedScrollPosition, ShowingScrollSize);

            Scrollbar.SetValueWithoutNotify(ScrollAxis is eScrollAxis.VERTICAL
                ? 1f - normalizedPos
                : normalizedPos);
        }

        /// <summary>
        /// RecycleScrollbar의 onValueChanged 리스너.
        /// 사용자가 스크롤바를 드래그할 때 value 변경을 RecycleScroller의 스크롤 위치에 반영합니다.
        /// SetScrollbarValueWithoutNotify는 sendCallback=false로 호출하므로 이 리스너가 재진입하지 않습니다.
        /// </summary>
        private void OnScrollbarValueChanged(float scrollbarValue)
        {
            float normalizedValue = ScrollAxis is eScrollAxis.VERTICAL
                ? 1f - scrollbarValue
                : scrollbarValue;

            m_scrollerMode.ApplyScrollbarValue(normalizedValue,
                v => ShowingNormalizedScrollPosition = v,
                v => RealNormalizedScrollPosition = v);
        }

        /// <summary>
        /// RecycleScrollbar가 더 이상 Scrollbar를 상속하지 않으므로,
        /// ScrollRect의 scrollbar 바인딩을 해제하여 간섭을 방지합니다.
        /// </summary>
        private void NullifyScrollRectScrollbar()
        {
            switch (ScrollAxis)
            {
                case eScrollAxis.VERTICAL:
                    if (_ScrollRect.verticalScrollbar != null)
                        _ScrollRect.verticalScrollbar = null;
                    break;
                case eScrollAxis.HORIZONTAL:
                    if (_ScrollRect.horizontalScrollbar != null)
                        _ScrollRect.horizontalScrollbar = null;
                    break;
            }
        }

        private void OnBeginDragForScrollbar(PointerEventData _)
        {
            StopAllMoveCor();
        }
        private void OnEndDragForScrollbar(PointerEventData _)
        {
            if (m_pagingData.usePaging) StartPagingCor();
        }

        #endregion

        #region Load Data Wait Buffer

        private bool TryAddWaitBuffer(Action action, out LoadDataProceedState current)
        {
            current = m_loadDataProceedState;

            if (action is null) return false;
            if (m_loadDataProceedState is not LoadDataProceedState.Loading) return false;

            m_loadDataWaitingActionBuffer.Enqueue(action);
            StartCorWaitLoadDataStateToCompleteForBuffer();
            return true;
        }
        private bool TryAddWaitBuffer_AndCheckCurrentState(Action action, out LoadDataProceedState current)
        {
            var result = TryAddWaitBuffer(action, out current);
            return result || current is LoadDataProceedState.NotLoaded;
        }

        private void ExecuteWaitBuffer()
        {
            while (m_loadDataWaitingActionBuffer.Count > 0)
            {
                var action = m_loadDataWaitingActionBuffer.Dequeue();
                action?.Invoke();
            }

            m_loadDataWaitingActionBuffer.Clear();
        }

        private void StartCorWaitLoadDataStateToCompleteForBuffer()
        {
            if (m_loadDataWaitingActionBuffer.Count <= 0 || IsWaitingLoadDataForActionBuffer) return;

            m_cor_loadDataWaitBuffer = StartCoroutine(Cor_WaitLoadDataStateToCompleteForBuffer());
        }
        private void StopCorWaitLoadDataStateToCompleteForBuffer()
        {
            m_loadDataWaitingActionBuffer.Clear();
            if (IsWaitingLoadDataForActionBuffer == false) return;

            StopCoroutine(m_cor_loadDataWaitBuffer);
            m_cor_loadDataWaitBuffer = null;
        }
        private IEnumerator Cor_WaitLoadDataStateToCompleteForBuffer()
        {
            // 로딩 상태가 끝나길 기다림
            // 하지만, del 등록이 안 되어 있는 등의 상황으로 로딩이 취소되면 대기를 중단하고 버퍼를 비움
            yield return new WaitUntil(() => m_loadDataProceedState is not LoadDataProceedState.Loading);
            if (m_loadDataProceedState is LoadDataProceedState.NotLoaded)
            {
                m_loadDataWaitingActionBuffer.Clear();
                yield break;
            }

            ExecuteWaitBuffer();
            m_cor_loadDataWaitBuffer = null;
        }

        #endregion
    }
}