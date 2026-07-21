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

    [Header("Cell Settings")]
    [SerializeField] private int m_cellCount = 50;
    // RSCellRect 의미: 첫 인자 = 주축(Size), 둘째 인자 = 보조축(CrossAxisSize)
    [Tooltip("주축(스크롤 방향) 크기 범위 (min == max면 균일)")]
    [SerializeField] private Vector2 m_cellMainSizeRange = new(100f, 100f);
    [Tooltip("보조축 크기 범위 (min == max면 균일)")]
    [SerializeField] private Vector2 m_cellCrossSizeRange = new(300f, 300f);
    [Tooltip("셀 프리팹의 localScale — 스크롤러의 Use Child Scale 검증용")]
    [SerializeField] private Vector2 m_cellScale = Vector2.one;

    private float CellMainSize => m_cellMainSizeRange.x;
    private float CellCrossSize => m_cellCrossSizeRange.x;

    [Header("Runtime Controls")]
    [SerializeField] private int m_jumpToIndex = 0;
    [SerializeField] private int m_insertIndex = 0;
    [SerializeField] private int m_removeIndex = 0;

    [Header("Startup")]
    [Tooltip("끄면 Play 시 LoadData를 호출하지 않음 — LoadData 전 순정 ScrollRect 동작 테스트용")]
    [SerializeField] private bool m_loadDataOnStart = true;

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
        RegisterOnValueChangedTest();

        if (m_loadDataOnStart) StartLoadData();
        else Debug.Log("[RecycleScrollerTest] Load Data On Start가 꺼져 있습니다 — LoadData 전 순정 ScrollRect 상태. GUI 패널의 [초기화] 그룹으로 더미 생성/로드 가능");
    }

    private void StartLoadData()
    {
        m_scroller.del = this;
        var callbacks = m_scroller.LoadData();
        callbacks.Complete += result =>
        {
            Debug.Log($"[RecycleScrollerTest] LoadData 완료: {result}"
                + $"\n  - CellCount: {m_scroller.CellCount}"
                + $"\n  - GroupCount: {m_scroller.GroupCount}"
                + $"\n  - ContentSize: {m_scroller.ContentSize}"
                + $"\n  - ViewportSize: {m_scroller.ViewportSize}");
            LogScrollbarState();
            LogCellRectVerification();
        };
    }

    /// <summary>
    /// 셀 크기 검증 — 델리게이트가 세팅한 rect에 localScale이 곱해진 결과가
    /// 스크롤러가 예약한 공간(GetCellRect의 스케일 적용값)과 일치해야 한다.
    /// 어긋나면 스케일이 이중 적용됐거나 누군가 rect를 덮어쓴 것
    /// </summary>
    private void LogCellRectVerification()
    {
        var cells = m_scroller.GetAllActivatedCells();
        if (cells.Count == 0) return;

        var axis = m_scroller.ScrollAxis;
        foreach (var pair in cells)
        {
            var rtf = pair.Value.rectTransform;
            var cellRect = GetCellRect(m_scroller, pair.Key);
            var declared = ToAxisVector(cellRect.ToUnScaledValues, axis);
            var reserved = ToAxisVector(cellRect.ToScaledValues, axis);

            var scale = rtf.localScale;
            var actual = new Vector2(rtf.rect.width * scale.x, rtf.rect.height * scale.y);
            var matched = (actual - reserved).sqrMagnitude < 0.01f;

            var log = $"[RecycleScrollerTest] 셀 크기 검증 (index {pair.Key})"
                + $"\n  - GetCellRect 선언 크기 : {declared}"
                + $"\n  - 셀 sizeDelta          : {rtf.sizeDelta}"
                + $"\n  - localScale            : ({scale.x}, {scale.y})"
                + $"\n  - 화면상 실제 크기      : {actual}"
                + $"\n  - 스크롤러 예약 공간    : {reserved} → {(matched ? "OK — 일치" : "불일치! 스케일 이중 적용 또는 rect 덮어쓰기")}";

            if (matched) Debug.Log(log);
            else Debug.LogError(log);
            return;
        }
    }

    /// <summary>
    /// LoadData 없이 Content에 더미 셀을 직접 배치 — 순정 ScrollRect 동작 확인용.
    /// LoadData 전에는 스크롤러가 주축 사이즈를 점유하지 않으므로 콘텐트 크기도 수동 설정
    /// </summary>
    private void SpawnDummyCells(int count)
    {
        var content = m_scroller.Content;
        for (int i = 0; i < count; i++)
        {
            var cell = Instantiate(m_cellPrefab, content.transform);
            cell.gameObject.name = $"Dummy_{i}";
            cell.gameObject.SetActive(true);
            var text = cell.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = $"Dummy {i}";
        }

        var isVertical = m_scroller.ScrollAxis == eScrollAxis.VERTICAL;
        var mainSize = count * CellMainSize + (count - 1) * m_scroller.Spacing;
        content.sizeDelta = isVertical
            ? new Vector2(content.sizeDelta.x, mainSize)
            : new Vector2(mainSize, content.sizeDelta.y);
        Debug.Log($"[Test] 더미 셀 {count}개 생성 — LoadData 전 순정 ScrollRect 상태에서 드래그/러버밴드 확인");
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

        // 셀 rect 세팅은 델리게이트 책임 — 스크롤러는 GetCellRect를 배치 계산에만 쓴다.
        // 스케일 적용 전(UnScaled) 값을 넣어야 localScale과 이중 적용되지 않음
        cell.UpdateCellSize(ToAxisVector(GetCellRect(scroller, dataIndex).ToUnScaledValues, scroller.ScrollAxis));

        return cell;
    }

    /// <summary>주축/보조축 값을 실제 x/y로 매핑 (수직: y=주축, 수평: x=주축)</summary>
    private static Vector2 ToAxisVector(CellSizeVector sizeVec, eScrollAxis axis)
        => axis == eScrollAxis.VERTICAL
            ? new Vector2(sizeVec.CrossAxisSize, sizeVec.Size)
            : new Vector2(sizeVec.Size, sizeVec.CrossAxisSize);

    public int GetCellCount(RecycleScroller scroller)
    {
        return m_cellCount;
    }

    public RSCellRect GetCellRect(RecycleScroller scroller, int dataIndex)
    {
        // Use Child Scale이 켜진 축만 스케일을 선언 — RSCellRect(rtf, scroller) 생성자와 동일 규칙
        var scale = new Vector2(
            scroller.UseChildScale.Width ? m_cellScale.x : 1f,
            scroller.UseChildScale.Height ? m_cellScale.y : 1f);

        // RSCellRect(size: 주축, crossAxisSize: 보조축, 주축 스케일, 보조축 스케일)
        return new(DeterministicInRange(dataIndex, 0, m_cellMainSizeRange),
            DeterministicInRange(dataIndex, 1, m_cellCrossSizeRange),
            scale.Size(scroller.ScrollAxis),
            scale.CrossAxisSize(scroller.ScrollAxis));
    }

    /// <summary>인덱스별 고정 의사난수 — 같은 인덱스는 항상 같은 크기 (가변 크기 테스트용)</summary>
    private static float DeterministicInRange(int dataIndex, int salt, Vector2 range)
    {
        if (range.y <= range.x) return range.x;
        var t = Mathf.Abs(Mathf.Sin((dataIndex + 1) * 12.9898f + salt * 78.233f) * 43758.5453f) % 1f;
        return Mathf.Lerp(range.x, range.y, t);
    }

    #endregion

    #region Cell Prefab

    private void CreateCellPrefab()
    {
        var go = new GameObject("CellPrefab");
        go.SetActive(false);
        go.transform.SetParent(transform);

        var rtf = go.AddComponent<RectTransform>();
        // 주축/보조축을 실제 x/y로 매핑 (수평: x=주축, 수직: y=주축)
        rtf.sizeDelta = m_scroller.ScrollAxis == eScrollAxis.VERTICAL
            ? new Vector2(CellCrossSize, CellMainSize)
            : new Vector2(CellMainSize, CellCrossSize);
        rtf.localScale = new Vector3(m_cellScale.x, m_cellScale.y, 1f);

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

    #region 7-x: onValueChanged Event Test

    [Header("Event Test")]
    [SerializeField] private bool m_logOnValueChanged = true;

    private const string EVT_TAG = "[RSTest:Event]";
    private bool m_isDragging = false;
    private int m_eventFireCount = 0;

    private void RegisterOnValueChangedTest()
    {
        m_scroller.onValueChanged.AddListener(OnValueChangedCallback);
        m_scroller.onBeginDrag += () => { m_isDragging = true; };
        m_scroller.onEndDrag += () =>
        {
            Debug.Log($"{EVT_TAG} [7-1] 드래그 종료 — 드래그 중 이벤트 발생 횟수: {m_eventFireCount}");
            m_isDragging = false;
            m_eventFireCount = 0;
        };
        Debug.Log($"{EVT_TAG} [7-3] onValueChanged 리스너 등록 완료 (코드 AddListener)");
    }

    private void OnValueChangedCallback(Vector2 normalizedPos)
    {
        if (!m_logOnValueChanged) return;

        m_eventFireCount++;

        if (m_isDragging)
        {
            if (m_eventFireCount == 1)
                Debug.Log($"{EVT_TAG} [7-1] 드래그 중 첫 이벤트 발생 — pos: {normalizedPos}");
        }
        else
        {
            if (m_eventFireCount <= 3)
                Debug.Log($"{EVT_TAG} [7-2] 관성/물리 이벤트 #{m_eventFireCount} — pos: {normalizedPos}");
        }
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
            + $"\n  ContentSize: {m_scroller.ContentSize}"
            + $"\n  ScrollSize: {m_scroller.ScrollSize}"
            + $"\n  ViewportSize: {m_scroller.ViewportSize}"
            + $"\n  ScrollPosition: {m_scroller.ScrollPosition}"
            + $"\n  NormalizedScrollPosition: {m_scroller.NormalizedScrollPosition}"
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
        var scrollbar = m_scroller.Scrollbar;
        if (scrollbar == null)
        {
            Debug.Log("[Scrollbar] 참조 없음");
            return;
        }

        Debug.Log("=== RecycleScrollbar Info ==="
            + $"\n  value: {scrollbar.Value:F3}"
            + $"\n  size: {scrollbar.Size:F3}"
            + $"\n  direction: {scrollbar._Direction}"
            + $"\n  Del assigned: {scrollbar.Del != null}");
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

    #region Runtime GUI Test Panel

    [Header("GUI Test Panel")]
    [SerializeField] private bool m_showGuiPanel = true;

    private static readonly EaseUtil.Ease[] GUI_EASES =
    {
        EaseUtil.Ease.Linear,
        EaseUtil.Ease.EaseOutCubic,
        EaseUtil.Ease.EaseInOutQuad,
        EaseUtil.Ease.EaseOutBack,
        EaseUtil.Ease.EaseOutElastic,
    };

    private float m_guiDuration = 0.5f;
    private int m_guiEaseIndex = 1;
    private string m_guiIndexText = "50";
    private string m_guiCellCountText = "100";
    private string m_guiDataIndexText = "0";
    private string m_guiDataCountText = "1";
    private bool m_guiUseVisualIndex;
    private string m_guiSizeMinText = "100";
    private string m_guiSizeMaxText = "100";
    private string m_guiWidthMinText = "300";
    private string m_guiWidthMaxText = "300";
    private GUIStyle m_guiGroupTitleStyle;
    private GUIStyle m_guiLabelStyle;
    private bool m_guiPanelCollapsed;
    private Vector2 m_guiPanelScroll;

    private EaseUtil.Ease CurrentEase => GUI_EASES[m_guiEaseIndex];

    private void OnGUI()
    {
        if (m_showGuiPanel == false || m_scroller == null) return;

        m_guiGroupTitleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        m_guiLabelStyle ??= new GUIStyle(GUI.skin.label);

        // 스킨/상태에 따라 어두운 색이 섞이지 않도록 전 상태의 텍스트 색을 매 프레임 재보증
        ForceTextColor(m_guiGroupTitleStyle, Color.white);
        ForceTextColor(m_guiLabelStyle, Color.white);

        static void ForceTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }

        // 숨김 상태: 화면 하단 중앙에 표시 버튼만
        if (m_guiPanelCollapsed)
        {
            if (GUI.Button(new Rect((Screen.width - 130f) * 0.5f, Screen.height - 34f, 130f, 24f), "▲ 테스트 패널"))
                m_guiPanelCollapsed = false;
            return;
        }

        // 고정 콘텐츠 사이즈 — 화면이 좁으면 가로 스크롤
        const float GROUPS_WIDTH = 1740f;
        const float GROUPS_HEIGHT = 150f;
        const float HEADER_HEIGHT = 26f;

        var panelWidth = Mathf.Min(Screen.width - 20f, GROUPS_WIDTH + 24f);
        var needScroll = panelWidth < GROUPS_WIDTH + 24f;
        var viewHeight = GROUPS_HEIGHT + (needScroll ? 18f : 0f);
        var panelHeight = HEADER_HEIGHT + viewHeight + 12f;

        GUILayout.BeginArea(new Rect(10f, Screen.height - panelHeight - 10f, panelWidth, panelHeight), GUI.skin.box);

        // 헤더: 제목(좌) + 숨김 버튼(패널 중앙)
        GUI.Label(new Rect(8f, 4f, 120f, HEADER_HEIGHT - 4f), "테스트 패널", m_guiGroupTitleStyle);
        if (GUI.Button(new Rect((panelWidth - 90f) * 0.5f, 3f, 90f, HEADER_HEIGHT - 5f), "▼ 숨기기"))
        {
            m_guiPanelCollapsed = true;
            GUILayout.EndArea();
            return;
        }
        GUILayout.Space(HEADER_HEIGHT);

        m_guiPanelScroll = GUILayout.BeginScrollView(m_guiPanelScroll, false, false, GUILayout.Height(viewHeight));
        GUILayout.BeginHorizontal(GUILayout.Width(GROUPS_WIDTH));

        var hasIndex = int.TryParse(m_guiIndexText, out var guiIndex);

        // ── 상태 ──
        BeginGroup("상태", 210f);
        GUILayout.Label($"pos  {m_scroller.ScrollPosition:F1} / {m_scroller.ContentSize:F0}", m_guiLabelStyle);
        GUILayout.Label($"norm {m_scroller.NormalizedScrollPosition:F3}", m_guiLabelStyle);
        GUILayout.Label($"loop {m_scroller.IsLoopScrollable}   cells {m_scroller.CellCount}", m_guiLabelStyle);
        var currentPage = m_scroller.NearestPageIndexByScrollPos;
        if (currentPage >= 0)
            GUILayout.Label($"page {currentPage} / {m_scroller.PageCount}", m_guiLabelStyle);
        EndGroup();

        // ── 초기화 (LoadData 전 상태 테스트) ──
        BeginGroup("초기화", 170f);
#if UNITY_EDITOR
        GUILayout.Label($"state: {m_scroller.Debug_LoadDataState}", m_guiLabelStyle);
#endif
        GUI.enabled = m_scroller.del == null;
        if (GUILayout.Button("Dummy 셀 x30")) SpawnDummyCells(30);
        if (GUILayout.Button("LoadData 시작")) StartLoadData();
        GUI.enabled = true;
        EndGroup();

        // ── 이동 옵션 ──
        BeginGroup("이동 옵션", 180f);
        GUILayout.Label($"Duration {m_guiDuration:F2}s", m_guiLabelStyle);
        m_guiDuration = GUILayout.HorizontalSlider(m_guiDuration, 0f, 2f);
        if (GUILayout.Button($"{CurrentEase}"))
            m_guiEaseIndex = (m_guiEaseIndex + 1) % GUI_EASES.Length;
        EndGroup();

        // ── 위치 이동 ──
        BeginGroup("위치 이동", 230f);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Move", m_guiLabelStyle, GUILayout.Width(42f));
        if (GUILayout.Button("0")) m_scroller.MoveTo(0f, CurrentEase, m_guiDuration);
        if (GUILayout.Button("0.5")) m_scroller.MoveTo(0.5f, CurrentEase, m_guiDuration);
        if (GUILayout.Button("1")) m_scroller.MoveTo(1f, CurrentEase, m_guiDuration);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Jump", m_guiLabelStyle, GUILayout.Width(42f));
        if (GUILayout.Button("0")) m_scroller.JumpTo(0f);
        if (GUILayout.Button("0.5")) m_scroller.JumpTo(0.5f);
        if (GUILayout.Button("1")) m_scroller.JumpTo(1f);
        GUILayout.EndHorizontal();
        EndGroup();

        // ── 인덱스 이동 ──
        BeginGroup("인덱스 이동", 230f);
        var targetIndex = m_guiUseVisualIndex && hasIndex ? m_scroller.VisualIndexToDataIndex(guiIndex) : guiIndex;
        GUILayout.BeginHorizontal();
        GUILayout.Label("Index", m_guiLabelStyle, GUILayout.Width(40f));
        m_guiIndexText = GUILayout.TextField(m_guiIndexText);
        GUI.enabled = hasIndex;
        if (GUILayout.Button("Jump", GUILayout.Width(50f))) m_scroller.JumpToIndex(targetIndex);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Move")) m_scroller.MoveToIndex(targetIndex, CurrentEase, m_guiDuration);
        if (GUILayout.Button("Move(Center)")) m_scroller.MoveToIndex_ViewportCenter(targetIndex, CurrentEase, m_guiDuration);
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        m_guiUseVisualIndex = GUILayout.Toggle(m_guiUseVisualIndex,
            m_guiUseVisualIndex ? " 시각(배치) 인덱스 기준" : " 데이터 인덱스 기준(기본)");
        EndGroup();

        // ── 데이터 ──
        BeginGroup("데이터", 190f);

        // 데이터 조작 전용 인덱스/개수 입력 (인덱스 이동 그룹과 별개)
        var hasDataIndex = int.TryParse(m_guiDataIndexText, out var guiDataIndex) && guiDataIndex >= 0;
        var hasDataCount = int.TryParse(m_guiDataCountText, out var guiDataCount) && guiDataCount > 0;
        GUILayout.BeginHorizontal();
        GUILayout.Label("Idx", m_guiLabelStyle, GUILayout.Width(28f));
        m_guiDataIndexText = GUILayout.TextField(m_guiDataIndexText, GUILayout.Width(42f));
        GUILayout.Label("Cnt", m_guiLabelStyle, GUILayout.Width(28f));
        m_guiDataCountText = GUILayout.TextField(m_guiDataCountText, GUILayout.Width(42f));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUI.enabled = hasDataIndex && hasDataCount;
        if (GUILayout.Button("Insert"))
        {
            m_cellCount += guiDataCount;
            m_scroller.Insert(guiDataIndex, guiDataCount);
            Debug.Log($"[Test] Insert {guiDataCount} at {guiDataIndex}, new count={m_cellCount}");
        }
        if (GUILayout.Button("Remove"))
        {
            var removeCount = Mathf.Min(guiDataCount, m_cellCount);
            if (removeCount > 0)
            {
                m_cellCount -= removeCount;
                m_scroller.Remove(guiDataIndex, removeCount);
                Debug.Log($"[Test] Remove {removeCount} at {guiDataIndex}, new count={m_cellCount}");
            }
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        if (GUILayout.Button("AddToEnd x10")) Add10CellsToEnd();

        // 셀 수 직접 설정 + 리로드 (작은 콘텐트 폴백 등 테스트용)
        GUILayout.BeginHorizontal();
        GUILayout.Label("Cells", m_guiLabelStyle, GUILayout.Width(38f));
        m_guiCellCountText = GUILayout.TextField(m_guiCellCountText);
        GUI.enabled = int.TryParse(m_guiCellCountText, out var guiCellCount) && guiCellCount >= 0;
        if (GUILayout.Button("Set+Reload", GUILayout.Width(80f)))
        {
            m_cellCount = guiCellCount;
            ReloadData();
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        EndGroup();

        // ── 페이지 ──
        BeginGroup("페이지", 110f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("◀")) m_scroller.JumpToPrevPage();
        if (GUILayout.Button("▶")) m_scroller.JumpToNextPage();
        GUILayout.EndHorizontal();
        EndGroup();

        // ── 셀 크기 (가변 크기 테스트) ──
        BeginGroup("셀 크기", 210f);
        GUILayout.BeginHorizontal();
        GUILayout.Label("주축", m_guiLabelStyle, GUILayout.Width(38f));
        m_guiSizeMinText = GUILayout.TextField(m_guiSizeMinText, GUILayout.Width(45f));
        GUILayout.Label("~", m_guiLabelStyle, GUILayout.Width(12f));
        m_guiSizeMaxText = GUILayout.TextField(m_guiSizeMaxText, GUILayout.Width(45f));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("보조축", m_guiLabelStyle, GUILayout.Width(38f));
        m_guiWidthMinText = GUILayout.TextField(m_guiWidthMinText, GUILayout.Width(45f));
        GUILayout.Label("~", m_guiLabelStyle, GUILayout.Width(12f));
        m_guiWidthMaxText = GUILayout.TextField(m_guiWidthMaxText, GUILayout.Width(45f));
        GUILayout.EndHorizontal();
        var hasCellRange = float.TryParse(m_guiSizeMinText, out var sizeMin);
        hasCellRange &= float.TryParse(m_guiSizeMaxText, out var sizeMax);
        hasCellRange &= float.TryParse(m_guiWidthMinText, out var widthMin);
        hasCellRange &= float.TryParse(m_guiWidthMaxText, out var widthMax);
        hasCellRange = hasCellRange && sizeMin > 0f && widthMin > 0f;
        GUI.enabled = hasCellRange;
        if (GUILayout.Button("Apply+Reload"))
        {
            m_cellMainSizeRange = new Vector2(sizeMin, sizeMax);
            m_cellCrossSizeRange = new Vector2(widthMin, widthMax);
            ReloadData();
        }
        GUI.enabled = true;
        EndGroup();

        // ── 리로드 ──
        BeginGroup("리로드", 160f);
        if (GUILayout.Button(m_scroller.LoopScrollIsOn ? "Loop ON→OFF" : "Loop OFF→ON"))
        {
            m_scroller.LoopScrollIsOn = !m_scroller.LoopScrollIsOn;
            ReloadData();
        }
        if (GUILayout.Button("Reload")) ReloadData();
        EndGroup();

        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        return;

        void BeginGroup(string title, float width)
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(width), GUILayout.ExpandHeight(true));
            GUILayout.Label(title, m_guiGroupTitleStyle);
        }

        void EndGroup()
        {
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
    }

    #endregion
}
