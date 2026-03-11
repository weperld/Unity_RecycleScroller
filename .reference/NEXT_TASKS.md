# 다음 예정 작업 목록

> 최종 업데이트: 2026-02-25

---

## Task 5. 페이지 순환 분기 — `LoopScrollIsOn` → `IsLoopScrollable` 교체

- **유형**: 버그 수정
- **공수**: 낮음 (파라미터 1개 교체)
- **위험도**: 낮음

### 문제

`GetNextPageIndex` / `GetPrevPageIndex` 호출 시 `LoopScrollIsOn`(인스펙터 설정값 `m_loopScroll`)을 전달하고 있음.
콘텐츠가 뷰포트보다 작아 루프가 실제로 불가능한 상태(`m_loopScrollable == false`)에서도
페이지가 순환되는 엣지 케이스가 존재함.

### 수정 대상

`RecycleScroller_Functions.cs:799-802`

```csharp
// 현재 코드
private int NextRealPageIndex
    => GetNextPageIndex(FindRealClosestPageIndexFrom(PagePivotPosInScrollRect), LoopScrollIsOn, RealPageCount);
private int PrevRealPageIndex
    => GetPrevPageIndex(FindRealClosestPageIndexFrom(PagePivotPosInScrollRect), LoopScrollIsOn, RealPageCount);
```

### 수정 가이드

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

### 가이드

#### 1) 필수 수정

| 위치 | 내용 |
|------|------|
| `RecycleScroller_Functions.cs:800` | `NextRealPageIndex`에서 `LoopScrollIsOn` → `IsLoopScrollable` 교체 |
| `RecycleScroller_Functions.cs:802` | `PrevRealPageIndex`에서 `LoopScrollIsOn` → `IsLoopScrollable` 교체 |

#### 2) 권장 수정

없음. 이 Task의 수정 범위는 위 2곳으로 한정됨.

#### 3) 수정할 필요 없는 것

| 위치 | 이유 |
|------|------|
| `RecycleScroller_Functions.cs:786-797` | `GetNextPageIndex` / `GetPrevPageIndex` static 메서드 자체는 `isLoop` 파라미터를 받아서 처리하는 정상 구조. 호출부만 수정하면 됨 |
| `RecycleScroller.cs:133` | `UseScrollOptimization`에서 `LoopScrollIsOn` 사용 — 스크롤 최적화는 "루프 설정 의도" 기준으로 비활성하는 게 맞음 (미완성 기능) |
| `RecycleScroller_LoadParam.cs:143` | `ScrollOptimization.Execute()`에서 `LoopScrollIsOn` 사용 — 위와 같은 맥락, 미완성 기능이므로 현 상태 유지 |
| `IRecycleScrollbarDelegate.cs:5` | `LoopScrollIsOn` 인터페이스 프로퍼티 — 스크롤바 delegate 계약으로 다른 용도에 사용 중 |

#### 4) 수정해서는 안 되는 것

| 위치 | 이유 |
|------|------|
| `RecycleScroller.cs:123` | `LoopScrollIsOn` 프로퍼티 자체 삭제/변경 금지 — 에디터, 스크롤바 등 여러 곳에서 "설정값" 참조용으로 올바르게 사용 중 |
| `RecycleScroller_LoadData.cs:457` | `CheckLoop()`의 `m_loopScroll == false` — 루프 계산 진입 조건이므로 설정값(`m_loopScroll`) 기준이 맞음. `IsLoopScrollable`로 바꾸면 순환 참조 (CheckLoop이 `m_loopScrollable`을 세팅하는 메서드) |
| `RecycleScroller_Inspector.cs:66` | `m_loopScroll` 필드 선언 — 직렬화 필드 자체는 변경 불가 |
| `RecycleScrollerEditor.cs:45,120,304` | 에디터 인스펙터에서 `m_loopScroll` SerializedProperty 바인딩 — 에디터 전용, 런타임과 무관 |

### 검증 방법

1. 루프 모드 ON + 셀 개수를 뷰포트보다 적게 설정
2. `MoveToNextPage()` / `MoveToPrevPage()` 호출 시 페이지가 순환하지 않는지 확인
3. 루프 모드 ON + 셀 개수 충분할 때는 기존처럼 정상 순환되는지 확인

---

## Task 6. `RecalculateForInsert` — `CanDoPartialRecalc()` 사문화 코드 연결

- **유형**: 리팩토링 (구조 정합성)
- **공수**: 낮음 (조건식 1줄 교체)
- **위험도**: 낮음

### 문제

`IScrollerMode.CanDoPartialRecalc()`가 인터페이스에 정의되고 `NormalScrollerMode` / `LoopScrollerMode`에
모두 구현되어 있지만, 실제 호출 경로에서 사용되지 않음.
대신 `m_loopScroll == false`를 직접 체크하고 있어 Task 2의 객체화 의도와 불일치.

### 수정 대상

`RecycleScroller_Functions.cs:1018`

```csharp
// 현재 코드
if (m_loopScroll == false && m_cellCount > 0 && prevCellCount > 0)
```

### 수정 가이드

```csharp
// m_scrollerMode.CanDoPartialRecalc()로 교체
if (m_scrollerMode.CanDoPartialRecalc(m_cellCount, prevCellCount))
```

- `NormalScrollerMode.CanDoPartialRecalc()`: `cellCount > 0 && prevCellCount > 0` 일 때 `true`
- `LoopScrollerMode.CanDoPartialRecalc()`: 항상 `false`
- 현재 코드와 동작이 동일하면서 `IScrollerMode` 계약에 맞게 정렬됨

### 가이드

#### 1) 필수 수정

| 위치 | 내용 |
|------|------|
| `RecycleScroller_Functions.cs:1018` | `m_loopScroll == false && m_cellCount > 0 && prevCellCount > 0` → `m_scrollerMode.CanDoPartialRecalc(m_cellCount, prevCellCount)` 교체 |

#### 2) 권장 수정

없음. 조건식 1줄 교체로 완료되는 작업.

#### 3) 수정할 필요 없는 것

| 위치 | 이유 |
|------|------|
| `NormalScrollerMode.cs:72-74` | `CanDoPartialRecalc` 구현체 — `cellCount > 0 && prevCellCount > 0` 반환. 이미 올바름 |
| `LoopScrollerMode.cs:75-78` | `CanDoPartialRecalc` 구현체 — 항상 `false` 반환. 이미 올바름 |
| `IScrollerMode.cs:73` | `CanDoPartialRecalc` 인터페이스 정의 — 시그니처 변경 불필요 |
| `RecycleScroller_Functions.cs:1019-1034` | `RecalculateForInsert` 내부의 부분 재계산 알고리즘 — 조건 진입부만 바꾸면 되고 내부 로직은 무관 |

#### 4) 수정해서는 안 되는 것

| 위치 | 이유 |
|------|------|
| `RecycleScroller_LoadData.cs:457` | `CheckLoop()`의 `m_loopScroll == false` — 루프 활성화 여부를 판단하는 진입 조건. `CanDoPartialRecalc`와 역할이 완전히 다름 |
| `RecycleScroller_LoadParam.cs:143` | `ScrollOptimization`의 `LoopScrollIsOn` — 미완성 기능의 가드 조건. 이 Task와 무관 |
| `RecycleScroller.cs:172-175` | `RealNormalizedScrollPosition` setter의 `m_loopScrollable` 분기 — normalized 값 wrapping 로직으로 Insert/Remove와 무관한 경로 |

### 검증 방법

1. 비루프 모드에서 `Insert()` / `Remove()` 호출 후 셀 배치가 정상인지 확인
2. 루프 모드에서 `Insert()` / `Remove()` 호출 후 전체 재계산이 동작하는지 확인

---

## 완료된 작업

| Task | 제목 | 완료 커밋/브랜치 |
|------|------|-----------------|
| Task 1 | Elastic 핸들 사이즈 조정 동작 구현 | WIP-20260214-001 |
| Task 2 | 루프 모드 로직 객체화 (Strategy Pattern) | `dc76cd7` (mode-objectification) |
| Task 3 | 전체 모드 관련 로직 객체화 검토 | 분석 완료, 보류 (ROI 낮음) |
| Task 4 | 에디터 버전 체크 팝업 | `9b2e3c9` (feat/editor-version-check) |
