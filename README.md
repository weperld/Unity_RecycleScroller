# Unity RecycleScroller

Unity용 고성능 재활용 스크롤 시스템입니다. Object Pooling 기반으로 대량의 아이템을 효율적으로 표시하고 관리할 수 있는 UI 스크롤러를 제공합니다.

## 특징

- **Object Pooling** - 셀 재활용을 통해 대량의 아이템도 원활하게 처리, 풀 사이즈 제한 지원
- **Loop Scroll** - 무한 순환 스크롤 지원
- **Pagination** - 페이지 스냅 + 30종 이상의 이징 함수 애니메이션
- **Bidirectional** - 수직/수평 스크롤 지원
- **Async Load** - UniTask 기반 비동기 데이터 로드 + CancellationToken 취소 지원
- **Cell Grouping** - Grid 형태의 아이템 배치 (행/열당 셀 수 고정 또는 유동)
- **Cell Lifecycle Callbacks** - 셀 진입/이탈 시 가상 메서드 및 이벤트 콜백
- **Scroll Direction Detection** - Forward/Backward 스크롤 방향 감지 API
- **Debug Overlay** - Play Mode에서 Inspector에 실시간 디버그 정보 표시
- **Addressable Support** - Addressables 기반 비동기 셀 프리팹 로딩 (옵션)
- **Dynamic Insert/Remove** - 런타임 셀 추가/삭제 + Wait Buffer 큐잉

## 요구사항

- Unity 2022.3.62f2 이상
- [UniTask](https://github.com/Cysharp/UniTask) (필수)
- TextMeshPro 3.0.9 (선택)
- Addressables (선택, `ENABLE_ADDRESSABLES` define 필요)

## 설치

### UPM (Unity Package Manager) - 권장

Unity Editor에서 `Window > Package Manager > + > Add package from git URL...`에 아래 URL을 입력하세요:

```
https://github.com/weperld/Unity_RecycleScroller.git?path=Packages/com.phjun.recyclescroller
```

또는 `Packages/manifest.json`에 직접 추가할 수도 있습니다:

```json
{
  "dependencies": {
    "com.phjun.recyclescroller": "https://github.com/weperld/Unity_RecycleScroller.git?path=Packages/com.phjun.recyclescroller"
  }
}
```

### 수동 설치

1. 이 저장소를 클론하거나 다운로드합니다
2. `Packages/com.phjun.recyclescroller/` 폴더를 프로젝트의 `Packages/` 디렉토리에 복사합니다
3. UniTask 패키지가 설치되어 있는지 확인합니다

## 프로젝트 구조

```
Assets/
├── RecycleScroll/           # 핵심 스크롤러 (partial class 구조)
│   ├── RecycleScroller.cs              # 메인 컨트롤러 (필드, 이벤트, 프로퍼티)
│   ├── RecycleScroller_Functions.cs    # 기능 메서드 (Init, Pooling, MoveTo, Paging 등)
│   ├── RecycleScroller_LoadData.cs     # 데이터 로드 (동기/비동기, UpdateCellView)
│   ├── RecycleScroller_LoadParam.cs    # LoadParam 파라미터 체계
│   ├── RecycleScroller_Inspector.cs    # SerializeField 선언 (Inspector 설정값)
│   ├── RecycleScroller_OnValidate.cs   # 에디터 OnValidate 로직
│   ├── RecycleScrollerCell.cs          # 셀 기본 클래스 (상속용)
│   ├── RecycleScrollerDatas.cs         # 데이터 구조 (CellGroupData, LoadDataCallbacks 등)
│   ├── RecycleScrollerEnums.cs         # 열거형 (eScrollAxis, eScrollDirection 등)
│   ├── RecycleScrollerHelper.cs        # 유틸리티 확장 메서드
│   ├── IRecycleScrollerDelegate.cs     # 델리게이트 인터페이스
│   ├── EasingFunctions.cs              # 30종 이상 이징 함수
│   ├── LoopScrollbar.cs                # 루프 대응 스크롤바
│   ├── MathUtils.cs                    # 수학 유틸리티
│   ├── ILoopScrollDelegate.cs          # 루프 스크롤 델리게이트
│   └── AddressableCellProvider.cs      # Addressable 비동기 셀 로더 (옵션)
├── Editor/                  # 에디터 전용 (빌드 미포함)
│   ├── RecycleScroll/       # 스크롤러 커스텀 인스펙터
│   ├── Attributes/          # 속성 드로어
│   ├── Drawers/             # 커스텀 프로퍼티 드로어
│   ├── Creator/             # 스크롤뷰 생성 메뉴 도구
│   └── LoadDataExtension/   # 확장 컴포넌트 에디터
├── Attributes/              # 커스텀 속성 정의 (런타임)
├── LoadDataExtension/       # 데이터 로드 확장 컴포넌트
├── SerializableDictionary/  # 직렬화 가능 딕셔너리
└── Scenes/                  # 샘플 씬
```

## 빠른 시작

### 1. 에디터에서 스크롤뷰 생성

`GameObject > UI > RecycleScrollView` 메뉴로 필요한 컴포넌트가 자동 구성된 스크롤뷰를 생성할 수 있습니다.

### 2. 델리게이트 구현

```csharp
using RecycleScroll;
using UnityEngine;

public class MyScroller : MonoBehaviour, IRecycleScrollerDelegate
{
    [SerializeField] private RecycleScroller scroller;
    [SerializeField] private RecycleScrollerCell cellPrefab;

    private List<MyData> dataList = new();

    private void Start()
    {
        scroller.del = this;
        scroller.LoadData();
    }

    public RecycleScrollerCell GetCell(RecycleScroller scroller, int dataIndex, int cellViewIndex)
    {
        var cell = scroller.GetCellInstance(cellPrefab, dataIndex);
        // 셀 데이터 업데이트
        return cell;
    }

    public int GetCellCount(RecycleScroller scroller) => dataList.Count;

    public RSCellRect GetCellRect(RecycleScroller scroller, int dataIndex)
    {
        return new RSCellRect(100f, 400f); // size, width
    }
}
```

### 3. 커스텀 셀 생성

```csharp
using RecycleScroll;
using UnityEngine;
using TMPro;

public class MyCell : RecycleScrollerCell
{
    [SerializeField] private TMP_Text titleText;

    public void SetData(MyData data)
    {
        titleText.text = data.title;
    }

    // 셀이 뷰포트에 나타날 때 호출 (애니메이션 등에 활용)
    public override void OnBecameVisible(RecycleScroller scroller, int dataIndex)
    {
        // 예: fade-in 애니메이션
    }

    // 셀이 뷰포트에서 벗어날 때 호출
    public override void OnBecameInvisible(RecycleScroller scroller)
    {
        // 예: 리소스 정리
    }
}
```

## 주요 기능

### Object Pooling

Viewport에 보이는 셀만 활성화하고 나머지는 Pool에 보관하여 메모리와 성능을 최적화합니다.

```csharp
// Inspector에서 타입별 최대 풀 사이즈 설정 가능 (기본값: 100)
// 풀 크기 초과 시 셀은 Destroy되어 메모리 회수
```

### Loop Scroll

스크롤이 끝에 도달하면 처음으로 이어지는 무한 순환 스크롤입니다. Inspector에서 `Loop Scroll` 체크로 활성화합니다.

### Pagination

페이지 단위로 스냅하는 기능입니다. Inspector `Paging Configs`에서 설정합니다.

```csharp
// 코드에서 페이지 이동
scroller.MoveToNextPage();
scroller.MoveToPrevPage();
scroller.JumpToPage(2);
```

### Async Load

대량 데이터를 비동기로 로드하여 프레임 드롭을 방지합니다.

```csharp
var callbacks = scroller.LoadDataAsync();
callbacks.Complete += (result) =>
{
    if (result == LoadDataResultState.Complete)
        Debug.Log("Load complete!");
};
```

### Cell Grouping

Grid 형태로 아이템을 배치합니다. Inspector에서 `Cell Group Configs`로 설정합니다.

### Cell Lifecycle Callbacks

셀이 뷰포트에 진입/이탈할 때 콜백을 받을 수 있습니다.

```csharp
// 방법 1: 셀 상속 (셀별 개별 동작)
public class FadeCell : RecycleScrollerCell
{
    public override void OnBecameVisible(RecycleScroller scroller, int dataIndex)
    {
        // 셀 진입 시 fade-in
        GetComponent<CanvasGroup>().alpha = 0f;
        DOTween.To(() => GetComponent<CanvasGroup>().alpha,
            x => GetComponent<CanvasGroup>().alpha = x, 1f, 0.3f);
    }
}

// 방법 2: 이벤트 구독 (전역 관찰)
scroller.onCellBecameVisible += (cell, dataIndex) =>
{
    Debug.Log($"Cell {dataIndex} appeared");
};
scroller.onCellBecameInvisible += (cell, dataIndex) =>
{
    Debug.Log($"Cell {dataIndex} disappeared");
};
```

### Scroll Direction Detection

스크롤 방향(Forward/Backward)을 감지하여 UI 반응에 활용합니다.

```csharp
scroller.onScrollDirectionChanged += (direction) =>
{
    if (direction == eScrollDirection.Forward)
        HideHeader(); // 아래로 스크롤 시 헤더 숨기기
    else
        ShowHeader(); // 위로 스크롤 시 헤더 표시
};
```

### Dynamic Insert/Remove

런타임에 셀을 동적으로 추가/삭제합니다.

```csharp
scroller.Insert(insertIndex, insertCount);
scroller.Remove(removeIndex, removeCount);
scroller.AddToStart();
scroller.AddToEnd();
```

### Addressable Cell Prefab (옵션)

Addressables를 통해 셀 프리팹을 비동기로 로드합니다. `ENABLE_ADDRESSABLES` define 추가 시 활성화됩니다.

```csharp
// 프리팹 미리 로드
var provider = GetComponent<AddressableCellProvider>();
await provider.PreloadCellPrefabsAsync("Cell_Chat", "Cell_Image");

// GetCell에서 캐시된 프리팹 사용
var prefab = provider.GetCachedPrefab("Cell_Chat");
var cell = scroller.GetCellInstance(prefab, dataIndex, "Cell_Chat");
```

자세한 사용 예시는 `AddressableCellProvider.cs`의 XML 문서 주석을 참조하세요.

## API 참조

### RecycleScroller

| 속성/메서드 | 설명 |
|-----------|------|
| `LoadData()` | 데이터 동기 로드 |
| `LoadDataAsync()` | 데이터 비동기 로드 |
| `ReloadCellView()` | 셀 뷰 새로고침 |
| `GetCellInstance(prefab, dataIndex, subKeys)` | 풀에서 셀 획득 |
| `Insert(index, count)` | 런타임 셀 삽입 |
| `Remove(index, count)` | 런타임 셀 삭제 |
| `MoveTo(pos, ease, duration)` | 위치로 이징 이동 |
| `MoveToIndex(index, duration)` | 셀 인덱스로 이동 |
| `MoveToIndex_ViewportCenter(index, duration)` | 셀을 뷰포트 중앙으로 이동 |
| `JumpTo(pos)` | 즉시 이동 |
| `JumpToPage(pageIndex)` | 페이지로 즉시 이동 |
| `MoveToNextPage()` / `MoveToPrevPage()` | 다음/이전 페이지 이동 |
| `CellCount` | 총 셀 수 |
| `GroupCount` | 총 그룹 수 |
| `ShowingScrollPosition` | 현재 스크롤 위치 |
| `ShowingNormalizedScrollPosition` | 정규화된 스크롤 위치 (0~1) |
| `NearestPageIndexByScrollPos` | 현재 가장 가까운 페이지 인덱스 |
| `IsEasing` | 이징 애니메이션 진행 중 여부 |

### RecycleScrollerCell

| 속성/메서드 | 설명 |
|-----------|------|
| `CellViewIndex` | 활성화된 셀의 뷰 인덱스 (-1: 비활성) |
| `PoolSubKey` | 풀 서브 키 |
| `UpdateCellSize(Vector2)` | 셀 크기 업데이트 |
| `OnBecameVisible(scroller, dataIndex)` | 뷰포트 진입 시 호출 (virtual) |
| `OnBecameInvisible(scroller)` | 뷰포트 이탈 시 호출 (virtual) |

### 이벤트

| 이벤트 | 타입 | 설명 |
|--------|------|------|
| `onScroll` | `Action<Vector2>` | 스크롤 위치 변경 시 |
| `onBeginDrag` | `Action` | 드래그 시작 시 |
| `onEndDrag` | `Action` | 드래그 종료 시 |
| `onChangePage` | `Action<int, int>` | 페이지 변경 시 (prev, next) |
| `onEndEasing` | `Action` | 이징 종료 시 |
| `onScrollDirectionChanged` | `Action<eScrollDirection>` | 스크롤 방향 변경 시 |
| `onCellBecameVisible` | `Action<RecycleScrollerCell, int>` | 셀 뷰포트 진입 시 |
| `onCellBecameInvisible` | `Action<RecycleScrollerCell, int>` | 셀 뷰포트 이탈 시 |

### 커스텀 셀 생성 훅

| 속성 | 타입 | 설명 |
|------|------|------|
| `CellCreateFuncWhenPoolEmpty` | `Func<Cell, Transform, int, Cell>` | 풀이 비었을 때 셀 생성 커스터마이징 |

## 에디터 기능

RecycleScroller Inspector는 다음 설정을 제공합니다:

- **Scroll Axis** - 수직/수평 스크롤 선택
- **Cell Alignment Values** - 패딩, 간격, 정렬, 역순 배치
- **Content Size Fit** - 콘텐트가 뷰포트보다 작을 때 맞춤
- **Cell Group Configs** - 그룹당 셀 수 (고정/유동)
- **Paging Configs** - 페이지 수, 이징, 피벗
- **Loop Scroll** - 무한 순환 스크롤
- **Pool Management** - 타입별 최대 풀 사이즈
- **Debug Info** (Play Mode) - 활성 셀/그룹 수, 풀 상태, 스크롤 위치, 페이지, 로드 상태

## 명명 규칙

| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스/메서드/프로퍼티 | PascalCase | `RecycleScroller`, `LoadData` |
| private 필드 | m_ + camelCase | `m_scrollRect` |
| 인터페이스 | I + PascalCase | `IRecycleScrollerDelegate` |
| partial class 파일 | ClassName_Responsibility | `RecycleScroller_Functions.cs` |
| 열거형 | e + PascalCase | `eScrollAxis` |
| 상수 | UPPER_SNAKE_CASE | `DEFAULT_POOL_SUBKEY` |

## 라이선스

이 프로젝트의 라이선스를 확인하세요.
