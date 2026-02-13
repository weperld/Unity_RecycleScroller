using RecycleScroll;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RecycleScroller & RecycleScrollbar 통합 테스트용 MonoBehaviour.
/// 씬에 배치 후 Inspector에서 scroller/scrollbar를 연결하고 Play하면 동작 확인 가능.
/// </summary>
public class RecycleScrollerTest : MonoBehaviour, IRecycleScrollerDelegate
{
    [Header("Required References")]
    [SerializeField] private RecycleScroller m_scroller;
    [SerializeField] private RecycleScrollbar m_scrollbar;

    [Header("Cell Settings")]
    [SerializeField] private int m_cellCount = 50;
    [SerializeField] private float m_cellSize = 100f;
    [SerializeField] private float m_cellWidth = 300f;

    [Header("Runtime Controls")]
    [SerializeField] private int m_jumpToIndex = 0;
    [SerializeField] private int m_insertIndex = 0;
    [SerializeField] private int m_removeIndex = 0;

    private TestCell m_cellPrefab;

    #region Unity Lifecycle

    private void Start()
    {
        if (m_scroller == null)
        {
            Debug.LogError("[RecycleScrollerTest] RecycleScroller 참조가 없습니다. Inspector에서 연결해주세요.");
            return;
        }

        CreateCellPrefab();

        m_scroller.del = this;
        var callbacks = m_scroller.LoadData();
        callbacks.Complete += result =>
        {
            Debug.Log($"[RecycleScrollerTest] LoadData 완료: {result}"
                + $"\n  - CellCount: {m_scroller.CellCount}"
                + $"\n  - GroupCount: {m_scroller.GroupCount}"
                + $"\n  - RealContentSize: {m_scroller.RealContentSize}"
                + $"\n  - ViewportSize: {m_scroller.ViewportSize}");
            LogScrollbarState();
        };

        // 스크롤 이벤트 로깅
        m_scroller.onCellBecameVisible += (cell, index) =>
            Debug.Log($"[Cell Visible] dataIndex={index}, type={cell.GetType().Name}");
        m_scroller.onCellBecameInvisible += (cell, index) =>
            Debug.Log($"[Cell Invisible] dataIndex={index}");

        // 스크롤바 이벤트 로깅
        if (m_scrollbar != null)
        {
            m_scrollbar.onValueChanged.AddListener(val =>
                Debug.Log($"[Scrollbar] value={val:F3}"));
            m_scrollbar.OnLoopValueChanged.AddListener((real, showing) =>
                Debug.Log($"[Scrollbar Loop] real={real:F3}, showing={showing:F3}"));
        }
    }

    #endregion

    #region IRecycleScrollerDelegate

    public RecycleScrollerCell GetCell(RecycleScroller scroller, int dataIndex, int cellViewIndex)
    {
        var cell = scroller.GetCellInstance(m_cellPrefab, dataIndex);
        if (cell is TestCell testCell)
        {
            // TODO: TestCell에 대한 추가 초기화 작업 수행
        }

        return cell;
    }

    public int GetCellCount(RecycleScroller scroller)
    {
        return m_cellCount;
    }

    public RSCellRect GetCellRect(RecycleScroller scroller, int dataIndex)
    {
        return new(m_cellSize, m_cellWidth);
    }

    #endregion

    #region Cell Prefab

    private void CreateCellPrefab()
    {
        var go = new GameObject("CellPrefab");
        go.SetActive(false);
        go.transform.SetParent(transform);

        var rtf = go.AddComponent<RectTransform>();
        rtf.sizeDelta = new Vector2(m_cellWidth, m_cellSize);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.9f, 0.9f, 0.9f);

        m_cellPrefab = go.AddComponent<TestCell>();

        // Text 자식 오브젝트
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform);
        var textRtf = textGo.AddComponent<RectTransform>();
        textRtf.anchorMin = Vector2.zero;
        textRtf.anchorMax = Vector2.one;
        textRtf.sizeDelta = Vector2.zero;
        textRtf.anchoredPosition = Vector2.zero;

        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 24;
        text.color = Color.black;
        text.text = "Cell";
    }

    #endregion

    #region Runtime Test Methods (Context Menu)

    [ContextMenu("Reload Data")]
    private void ReloadData()
    {
        if (m_scroller == null) return;

        var callbacks = m_scroller.LoadData();
        callbacks.Complete += result =>
        {
            Debug.Log($"[Test] ReloadData 완료: {result}, CellCount={m_scroller.CellCount}");
            LogScrollbarState();
        };
    }

    [ContextMenu("Jump To Index")]
    private void JumpToIndex()
    {
        if (m_scroller == null) return;

        Debug.Log($"[Test] JumpToIndex({m_jumpToIndex})");
        m_scroller.JumpToIndex(m_jumpToIndex);
    }

    [ContextMenu("Jump To Index (Center)")]
    private void JumpToIndexCenter()
    {
        if (m_scroller == null) return;

        Debug.Log($"[Test] JumpToIndex_ViewportCenter({m_jumpToIndex})");
        m_scroller.JumpToIndex_ViewportCenter(m_jumpToIndex);
    }

    [ContextMenu("Insert Cell")]
    private void InsertCell()
    {
        if (m_scroller == null) return;

        m_cellCount++;
        m_scroller.Insert(m_insertIndex);
        Debug.Log($"[Test] Insert at {m_insertIndex}, new count={m_cellCount}");
    }

    [ContextMenu("Remove Cell")]
    private void RemoveCell()
    {
        if (m_scroller == null || m_cellCount <= 0) return;

        m_cellCount--;
        m_scroller.Remove(m_removeIndex);
        Debug.Log($"[Test] Remove at {m_removeIndex}, new count={m_cellCount}");
    }

    [ContextMenu("Add 10 Cells To End")]
    private void Add10CellsToEnd()
    {
        if (m_scroller == null) return;

        m_cellCount += 10;
        m_scroller.AddToEnd(10);
        Debug.Log($"[Test] Added 10 cells, new count={m_cellCount}");
    }

    [ContextMenu("Log Scroll Info")]
    private void LogScrollInfo()
    {
        if (m_scroller == null) return;

        var log = "=== RecycleScroller Info ==="
            + $"\n  CellCount: {m_scroller.CellCount}"
            + $"\n  GroupCount: {m_scroller.GroupCount}"
            + $"\n  ScrollAxis: {m_scroller.ScrollAxis}"
            + $"\n  RealContentSize: {m_scroller.RealContentSize}"
            + $"\n  ShowingContentSize: {m_scroller.ShowingContentSize}"
            + $"\n  ViewportSize: {m_scroller.ViewportSize}"
            + $"\n  RealScrollPosition: {m_scroller.RealScrollPosition}"
            + $"\n  ShowingScrollPosition: {m_scroller.ShowingScrollPosition}"
            + $"\n  LoopScrollIsOn: {m_scroller.LoopScrollIsOn}"
            + $"\n  IsLoopScrollable: {m_scroller.IsLoopScrollable}";
#if UNITY_EDITOR
        log += $"\n  ActiveCells: {m_scroller.Debug_ActiveCellCount}"
            + $"\n  ActiveGroups: {m_scroller.Debug_ActiveGroupCount}"
            + $"\n  PooledCells: {m_scroller.Debug_PooledCellCount}"
            + $"\n  LoadDataState: {m_scroller.Debug_LoadDataState}";
#endif
        Debug.Log(log);
        LogScrollbarState();
    }

    [ContextMenu("Log Scrollbar State")]
    private void LogScrollbarState()
    {
        if (m_scrollbar == null)
        {
            Debug.Log("[Scrollbar] 참조 없음");
            return;
        }

        Debug.Log("=== RecycleScrollbar Info ==="
            + $"\n  value: {m_scrollbar.value:F3}"
            + $"\n  size: {m_scrollbar.size:F3}"
            + $"\n  direction: {m_scrollbar.direction}"
            + $"\n  Del assigned: {m_scrollbar.Del != null}");
    }

    [ContextMenu("Scroll To Start")]
    private void ScrollToStart()
    {
        if (m_scroller == null) return;
        m_scroller.JumpTo(0f);
        Debug.Log("[Test] Scroll to start");
    }

    [ContextMenu("Scroll To End")]
    private void ScrollToEnd()
    {
        if (m_scroller == null) return;
        m_scroller.JumpTo(1f);
        Debug.Log("[Test] Scroll to end");
    }

    [ContextMenu("Scroll To Middle (Animated)")]
    private void ScrollToMiddleAnimated()
    {
        if (m_scroller == null) return;
        m_scroller.MoveTo(0.5f, EaseUtil.Ease.EaseOutCubic, 0.5f);
        Debug.Log("[Test] Animated scroll to middle");
    }

    #endregion
}
