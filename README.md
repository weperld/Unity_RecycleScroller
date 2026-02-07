# Unity RecycleScroller

Unity용 고성능 재활용 스크롤 시스템입니다. 대량의 아이템을 효율적으로 표시하고 관리할 수 있는 UI 스크롤러를 제공합니다.

## 특징

- 🚀 **Object Pooling**: 셀 재활용을 통해 대량의 아이템도 원활하게 처리
- 🔄 **Loop Scroll**: 무한 스크롤 지원
- 📄 **Pagination**: 페이지네이션 기능 내장
- 🎯 **Bidirectional**: 수직/수평 스크롤 지원
- ⚡ **Async Load**: UniTask를 활용한 비동기 데이터 로드
- 🎨 **Easing Functions**: 다양한 이징 함수 제공
- 📦 **Cell Grouping**: Grid 형태의 아이템 배치 지원

## 요구사항

- Unity 2022.3.62f2 이상
- [UniTask](https://github.com/Cysharp/UniTask)

## 설치

1. 이 저장소를 클론하거나 다운로드합니다
2. Unity 프로젝트에 `Assets/RecycleScroll` 폴더를 복사합니다
3. UniTask 패키지가 설치되어 있는지 확인합니다 (manifest.json에 포함)

## 빠른 시작

### 1. RecycleScroller 설정

```csharp
using RecycleScroll;
using UnityEngine;

public class ExampleScroller : MonoBehaviour, IRecycleScrollerDelegate
{
    [SerializeField] private RecycleScroller recycleScroller;

    private void Start()
    {
        recycleScroller.del = this;
        recycleScroller.LoadData();
    }

    public RecycleScrollerCell GetCell(RecycleScroller scroller, int dataIndex, int cellViewIndex)
    {
        // 셀을 가져오거나 생성합니다 (Object Pool에서 재사용)
        var prefab = GetPrefab();
        var cell = scroller.GetCellFromPoolOrInstantiate(prefab, scroller.Content, dataIndex);

        // 셀 데이터 업데이트
        cell.UpdateCell(dataIndex);

        return cell;
    }

    public int GetCellCount(RecycleScroller scroller)
    {
        return YourDataCount; // 총 아이템 수
    }

    public RSCellRect GetCellRect(RecycleScroller scroller, int dataIndex)
    {
        // 각 셀의 크기 반환
        return new RSCellRect(cellSize, cellWidth);
    }
}
```

### 2. 커스텀 셀 생성

```csharp
using RecycleScroll;
using UnityEngine;
using UnityEngine.UI;

public class CustomCell : RecycleScrollerCell
{
    [SerializeField] private Text titleText;
    [SerializeField] private Image iconImage;

    public void UpdateCell(int dataIndex)
    {
        // 데이터 로드 및 UI 업데이트
        titleText.text = $"Item {dataIndex}";
    }
}
```

### 3. Hierarchy 설정

```
RecycleScroller (GameObject)
├── ScrollRect
├── Viewport
└── Content
```

## 주요 기능

### Object Pooling

Viewport 내에 보이는 셀만 생성하고 나머지는 Object Pool에 저장하여 메모리와 성능을 최적화합니다.

```csharp
// 수동으로 셀 풀에서 가져오기
var cell = scroller.GetCellFromPool(typeof(CustomCell), "PoolSubKey");

// 셀을 풀로 반환
scroller.ReturnCellToPool(cell);
```

### Loop Scroll

스크롤이 끝에 도달하면 처음으로 돌아가는 무한 스크롤 기능입니다.

```csharp
recycleScroller.loopScroll = true;
```

### Pagination

페이지 단위로 스크롤을 스냅하는 기능입니다.

```csharp
recycleScroller.PagingData.usePaging = true;
recycleScroller.PagingData.countPerPage = 10;
recycleScroller.PagingData.duration = 0.3f;

// 특정 페이지로 이동
recycleScroller.JumpToPage(2);
```

### Async Load

대량의 데이터를 비동기로 로드하여 프레임 드롭을 방지합니다.

```csharp
// 비동기 로드
var callbacks = recycleScroller.LoadDataAsync();
callbacks.Complete += (result) =>
{
    if (result == LoadDataResultState.Complete)
    {
        Debug.Log("Load complete!");
    }
};
```

### Cell Grouping

Grid 형태로 아이템을 배치합니다.

```csharp
recycleScroller.FixCellCountInGroup = true;
recycleScroller.FixedCellCount = 3; // 한 행에 3개의 셀
```

## API 참조

### RecycleScroller

| 속성/메서드 | 설명 |
|-----------|------|
| `LoadData()` | 데이터 동기 로드 |
| `LoadDataAsync()` | 데이터 비동기 로드 |
| `Refresh()` | 셀 새로고침 |
| `JumpToDataIndex(int index)` | 특정 데이터 인덱스로 이동 |
| `JumpToPage(int pageIndex)` | 특정 페이지로 이동 |
| `ScrollToCell(int dataIndex, float duration)` | 셀로 부드럽게 스크롤 |
| `CellCount` | 총 셀 수 |
| `ShowingScrollPosition` | 현재 스크롤 위치 |

### RecycleScrollerCell

| 속성/메서드 | 설명 |
|-----------|------|
| `CellViewIndex` | 활성화된 셀의 인덱스 (-1: 비활성화) |
| `PoolSubKey` | 풀 서브 키 |
| `UpdateCellSize(Vector2 size)` | 셀 크기 업데이트 |

### 이벤트

| 이벤트 | 설명 |
|--------|------|
| `onScroll` | 스크롤 위치 변경 시 |
| `onBeginDrag` | 드래그 시작 시 |
| `onEndDrag` | 드래그 종료 시 |
| `onChangePage` | 페이지 변경 시 |
| `onEndEasing` | 이징 종료 시 |

## 에디터 기능

RecycleScroller는 Unity Editor에서 직관적인 인스펙터를 제공합니다:

- **Scroll Axis**: 수직/수평 스크롤 선택
- **Layout**: 패딩, 간격, 정렬 설정
- **Cell Group**: 그룹당 셀 수, 간격 설정
- **Paging**: 페이지 수, 이징 함수 설정
- **Loop Scroll**: 무한 스크롤 활성화

## 예제

프로젝트 내 `Assets/Scenes/SampleScene.unity`에서 사용 예제를 확인할 수 있습니다.

## 라이선스

이 프로젝트의 라이선스를 확인하세요.

## 기여

버그 리포트나 기능 요청은 이슈를 통해 제출해 주세요.
