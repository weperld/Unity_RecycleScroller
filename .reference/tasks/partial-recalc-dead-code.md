# `RecalculateForInsert` — `CanDoPartialRecalc()` 사문화 코드 연결

- **유형**: 리팩토링 (구조 정합성)
- **공수**: 낮음 (조건식 1줄 교체)
- **위험도**: 낮음

## 문제

`IScrollerMode.CanDoPartialRecalc()`가 인터페이스에 정의되고 `NormalScrollerMode` / `LoopScrollerMode`에
모두 구현되어 있지만, 실제 호출 경로에서 사용되지 않음.
대신 `m_loopScroll == false`를 직접 체크하고 있어 루프 모드 객체화 의도와 불일치.

## 수정 대상

`RecycleScroller_Functions.cs:1008`

```csharp
// 현재 코드
if (m_loopScroll == false && m_cellCount > 0 && prevCellCount > 0)
```

## 수정 가이드

```csharp
// m_scrollerMode.CanDoPartialRecalc()로 교체
if (m_scrollerMode.CanDoPartialRecalc(m_cellCount, prevCellCount))
```

- `NormalScrollerMode.CanDoPartialRecalc()`: `cellCount > 0 && prevCellCount > 0` 일 때 `true`
- `LoopScrollerMode.CanDoPartialRecalc()`: 항상 `false`
- 현재 코드와 동작이 동일하면서 `IScrollerMode` 계약에 맞게 정렬됨

## 가이드

### 1) 필수 수정

| 위치 | 내용 |
|------|------|
| `RecycleScroller_Functions.cs:1008` | `m_loopScroll == false && m_cellCount > 0 && prevCellCount > 0` → `m_scrollerMode.CanDoPartialRecalc(m_cellCount, prevCellCount)` 교체 |

### 2) 권장 수정

없음. 조건식 1줄 교체로 완료되는 작업.

### 3) 수정할 필요 없는 것

| 위치 | 이유 |
|------|------|
| `NormalScrollerMode.cs:72` | `CanDoPartialRecalc` 구현체 — `cellCount > 0 && prevCellCount > 0` 반환. 이미 올바름 |
| `LoopScrollerMode.cs:75` | `CanDoPartialRecalc` 구현체 — 항상 `false` 반환. 이미 올바름 |
| `IScrollerMode.cs:73` | `CanDoPartialRecalc` 인터페이스 정의 — 시그니처 변경 불필요 |
| `RecycleScroller_Functions.cs:1009-1034` | `RecalculateForInsert` 내부의 부분 재계산 알고리즘 — 조건 진입부만 바꾸면 되고 내부 로직은 무관 |

### 4) 수정해서는 안 되는 것

| 위치 | 이유 |
|------|------|
| `RecycleScroller_LoadData.cs:469` | `CheckLoop()`의 `m_loopScroll == false` — 루프 활성화 여부를 판단하는 진입 조건. `CanDoPartialRecalc`와 역할이 완전히 다름 |
| `RecycleScroller_LoadParam.cs:143` | `ScrollOptimization`의 `LoopScrollIsOn` — 미완성 기능의 가드 조건. 이 Task와 무관 |
| `RecycleScroller.cs:164-168` | `RealNormalizedScrollPosition` setter의 `m_loopScrollable` 분기 — normalized 값 wrapping 로직으로 Insert/Remove와 무관한 경로 |

## 검증 방법

1. 비루프 모드에서 `Insert()` / `Remove()` 호출 후 셀 배치가 정상인지 확인
2. 루프 모드에서 `Insert()` / `Remove()` 호출 후 전체 재계산이 동작하는지 확인
