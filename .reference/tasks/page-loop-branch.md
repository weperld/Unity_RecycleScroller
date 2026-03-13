# 페이지 순환 분기 — `LoopScrollIsOn` → `IsLoopScrollable` 교체

- **유형**: 버그 수정
- **공수**: 낮음 (파라미터 1개 교체)
- **위험도**: 낮음

## 문제

`GetNextPageIndex` / `GetPrevPageIndex` 호출 시 `LoopScrollIsOn`(인스펙터 설정값 `m_loopScroll`)을 전달하고 있음.
콘텐츠가 뷰포트보다 작아 루프가 실제로 불가능한 상태(`m_loopScrollable == false`)에서도
페이지가 순환되는 엣지 케이스가 존재함.

## 수정 대상

`RecycleScroller_Functions.cs:805-808`

```csharp
// 현재 코드
private int NextRealPageIndex
    => GetNextPageIndex(FindRealClosestPageIndexFrom(PagePivotPosInScrollRect), LoopScrollIsOn, RealPageCount);
private int PrevRealPageIndex
    => GetPrevPageIndex(FindRealClosestPageIndexFrom(PagePivotPosInScrollRect), LoopScrollIsOn, RealPageCount);
```

## 수정 가이드

```csharp
// LoopScrollIsOn → IsLoopScrollable 로 교체
private int NextRealPageIndex
    => GetNextPageIndex(FindRealClosestPageIndexFrom(PagePivotPosInScrollRect), IsLoopScrollable, RealPageCount);
private int PrevRealPageIndex
    => GetPrevPageIndex(FindRealClosestPageIndexFrom(PagePivotPosInScrollRect), IsLoopScrollable, RealPageCount);
```

- `LoopScrollIsOn` (`m_loopScroll`): "루프를 원하는가" — 인스펙터 설정값
- `IsLoopScrollable` (`m_loopScrollable`): "루프가 실제로 동작 가능한가" — 런타임 계산 결과
- `RecycleScrollbar` 등 다른 곳에서는 이미 `IsLoopScrollable`을 올바르게 사용 중

## 가이드

### 1) 필수 수정

| 위치 | 내용 |
|------|------|
| `RecycleScroller_Functions.cs:806` | `NextRealPageIndex`에서 `LoopScrollIsOn` → `IsLoopScrollable` 교체 |
| `RecycleScroller_Functions.cs:808` | `PrevRealPageIndex`에서 `LoopScrollIsOn` → `IsLoopScrollable` 교체 |

### 2) 권장 수정

없음. 이 Task의 수정 범위는 위 2곳으로 한정됨.

### 3) 수정할 필요 없는 것

| 위치 | 이유 |
|------|------|
| `RecycleScroller_Functions.cs:792-803` | `GetNextPageIndex` / `GetPrevPageIndex` static 메서드 자체는 `isLoop` 파라미터를 받아서 처리하는 정상 구조. 호출부만 수정하면 됨 |
| `RecycleScroller.cs:125` | `UseScrollOptimization`에서 `LoopScrollIsOn` 사용 — 스크롤 최적화는 "루프 설정 의도" 기준으로 비활성하는 게 맞음 (미완성 기능) |
| `RecycleScroller_LoadParam.cs:143` | `ScrollOptimization.Execute()`에서 `LoopScrollIsOn` 사용 — 위와 같은 맥락, 미완성 기능이므로 현 상태 유지 |
| `IRecycleScrollbarDelegate.cs:5` | `LoopScrollIsOn` 인터페이스 프로퍼티 — 스크롤바 delegate 계약으로 다른 용도에 사용 중 |

### 4) 수정해서는 안 되는 것

| 위치 | 이유 |
|------|------|
| `RecycleScroller.cs:115` | `LoopScrollIsOn` 프로퍼티 자체 삭제/변경 금지 — 에디터, 스크롤바 등 여러 곳에서 "설정값" 참조용으로 올바르게 사용 중 |
| `RecycleScroller_LoadData.cs:469` | `CheckLoop()`의 `m_loopScroll == false` — 루프 계산 진입 조건이므로 설정값(`m_loopScroll`) 기준이 맞음. `IsLoopScrollable`로 바꾸면 순환 참조 (CheckLoop이 `m_loopScrollable`을 세팅하는 메서드) |
| `RecycleScroller_Inspector.cs:102` | `m_loopScroll` 필드 선언 — 직렬화 필드 자체는 변경 불가 |
| `RecycleScrollerEditor.cs:58,147,360` | 에디터 인스펙터에서 `m_loopScroll` SerializedProperty 바인딩 — 에디터 전용, 런타임과 무관 |

## 검증 방법

1. 루프 모드 ON + 셀 개수를 뷰포트보다 적게 설정
2. `MoveToNextPage()` / `MoveToPrevPage()` 호출 시 페이지가 순환하지 않는지 확인
3. 루프 모드 ON + 셀 개수 충분할 때는 기존처럼 정상 순환되는지 확인
