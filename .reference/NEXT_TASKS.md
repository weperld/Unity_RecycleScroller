# 다음 예정 작업 목록

> 최종 업데이트: 2026-02-14

---

## Task 3. 전체 모드 관련 로직 객체화 검토

- Task 2(루프 모드 객체화) 완료 후 수행
- 루프 모드 외에도 **모드에 의존하는 로직**이 있는지 전체 점검
- 가능한 부분은 동일하게 객체화하여 일관된 구조로 통일

### 분석 결과: 객체화 후보 패턴

코드베이스에서 발견된 모드 의존 분기 패턴을 2가지 카테고리로 분류합니다.

---

### A. ScrollAxis 분기 (VERTICAL/HORIZONTAL)

가장 많이 반복되는 패턴. `switch (ScrollAxis)` 또는 `ScrollAxis == eScrollAxis.VERTICAL` 형태로 약 **30곳 이상** 산재.

#### A-1. 좌표 매핑 (주축/보조축 → x/y 변환)

| 위치 | 패턴 |
|------|------|
| `RecycleScroller.cs:45-49` | `ViewportSize` — VERTICAL→height, HORIZONTAL→width |
| `RecycleScroller.cs:163-167` | `RealNormalizedScrollPosition` getter — VERTICAL→verticalNormalized, HORIZONTAL→horizontalNormalized |
| `RecycleScroller.cs:178-185` | `RealNormalizedScrollPosition` setter — 동일 역매핑 |
| `RecycleScroller_Functions.cs:162-173` | `ResetSpaceCellsWidth()` — sizeDelta x/y, Vector2 방향 |
| `RecycleScroller_Functions.cs:324-326` | `ResetContent_Size()` — sizeDelta 구성 |
| `RecycleScroller_Functions.cs:525-533` | `ResetGroupSize()` — sizeDelta.y/x 할당 |
| `RecycleScroller_Functions.cs:1093-1095` | `SetScrollbarValueWithoutNotify()` — VERTICAL→1-val 반전 |
| `RecycleScroller_Functions.cs:1105-1107` | `OnScrollbarValueChanged()` — 동일 반전 |
| `RecycleScroller_LoadData.cs:380-385` | `GetMaxGroupWidth()` — VERTICAL→width, HORIZONTAL→height |
| `RecycleScroller_LoadData.cs:582` | `topBoundaryPos` — contentPos.y/-contentPos.x |
| `RecycleScroller_LoadData.cs:603-613` | `axisVec`/`widthVec` 설정 — Vector2.up/right |
| `RecycleScroller_OnValidate.cs:17-19` | `OnValidate` — Content.sizeDelta 구성 |
| `RecycleScroller_OnValidate.cs:47-55` | 예제 그룹 sizeDelta.y/x |
| `RecycleScroller_OnValidate.cs:102-104` | `GetSizeOfRectTransform` — rect.height/width, scale.y/x |
| `IRecycleScrollerDelegate.cs:118-134` | `Size()`/`Width()` 확장 메서드 — 축 매핑 |

**객체화 방안**: `IAxisMapper` 인터페이스 도입
```
interface IAxisMapper {
    float GetSize(Vector2 v);        // 주축 크기
    float GetWidth(Vector2 v);       // 보조축 크기
    Vector2 MakeSizeDelta(float axis, float cross);
    Vector2 AxisVector { get; }      // Vector2.up or Vector2.right
    float GetNormalizedPosition(ScrollRect sr);
    void SetNormalizedPosition(ScrollRect sr, float value);
}
```
`VerticalAxisMapper`, `HorizontalAxisMapper` 구현체로 **모든 좌표 매핑 분기를 제거** 가능.

#### A-2. 레이아웃 그룹 타입 결정

| 위치 | 패턴 |
|------|------|
| `RecycleScroller_Functions.cs:197-202` | `GetNeedLayoutGroupType()` — VERTICAL→VerticalLayoutGroup |
| `RecycleScroller_Functions.cs:331-336` | `GetNeedLayoutGroupTypeOfGroupCell()` — VERTICAL→HorizontalLayoutGroup (보조축) |

**객체화 방안**: `IAxisMapper`에 `ContentLayoutGroupType`, `GroupLayoutGroupType` 프로퍼티 추가.

#### A-3. 레이아웃 속성 설정

| 위치 | 패턴 |
|------|------|
| `RecycleScroller_Functions.cs:238-248` | 루프 패딩 — top/bottom vs left/right |
| `RecycleScroller_Functions.cs:256-273` | childControl/childForceExpand — height/width 교차 설정 |
| `RecycleScroller_Functions.cs:280-301` | `GetAlignmentPoint()` — 축별 alignment 매핑 |

**객체화 방안**: `IAxisMapper`에 패딩/정렬 헬퍼 추가 또는 별도 `ILayoutAxisHelper`.

#### A-4. ScrollRect 바인딩

| 위치 | 패턴 |
|------|------|
| `RecycleScroller_Functions.cs:154-157` | `UpdateScrollAxisToScrollRect()` — horizontal/vertical 토글 |
| `RecycleScroller_Functions.cs:1120-1130` | `NullifyScrollRectScrollbar()` — verticalScrollbar/horizontalScrollbar null 처리 |

**객체화 방안**: `IAxisMapper`에 `BindScrollRect()`, `NullifyScrollbar()` 포함.

---

### B. 루프 모드 잔여 분기

Task 2에서 `IScrollerMode`/`IScrollbarMode`로 주요 로직을 객체화했으나, 아직 Mode 객체로 이관되지 않은 **잔여 루프 분기**가 존재.

| 위치 | 패턴 | 설명 |
|------|------|------|
| `RecycleScroller.cs:172-176` | `if (m_loopScrollable)` | `RealNormalizedScrollPosition` setter에서 값 wrapping |
| `RecycleScroller_Functions.cs:236` | `if (m_loopScrollable)` | `SetAlignmentValuesToContentLayout`에서 루프 패딩 분기 |
| `RecycleScroller_Functions.cs:789,795` | `isLoop ? 0 : pageCount-1` | `GetNextPageIndex`/`GetPrevPageIndex` 페이지 순환 |
| `RecycleScroller_Functions.cs:1018` | `if (m_loopScroll == false)` | `RecalculateForInsert`에서 루프 시 재계산 스킵 |
| `RecycleScroller_LoadData.cs:457` | `if (m_loopScroll == false) return` | `CheckLoop`에서 루프 비활성 시 조기 반환 |
| `RecycleScroller_LoadParam.cs:143` | `if (scroller.LoopScrollIsOn) return` | `ScrollOptimization`이 루프 시 비활성 |

**객체화 방안**: 이미 존재하는 `IScrollerMode`에 메서드 추가
- `WrapScrollPosition(float value)` → 값 wrapping 로직
- `GetLoopPadding(...)` → 루프 패딩 분기
- `WrapPageIndex(int index, int count)` → 페이지 순환
- `ShouldRecalculateOnInsert()` → 재계산 여부

---

### 우선순위 권장

| 순위 | 대상 | 이유 |
|------|------|------|
| **1** | A-1. 좌표 매핑 (`IAxisMapper`) | 30곳+ 중복, 가장 높은 ROI |
| **2** | A-4. ScrollRect 바인딩 | A-1과 함께 자연스럽게 통합 |
| **3** | A-2/A-3. 레이아웃 관련 | A-1 완료 후 `IAxisMapper` 확장으로 처리 |
| **4** | B. 루프 잔여 분기 | 기존 `IScrollerMode` 확장으로 간단히 처리 가능 |

---

## 완료된 작업

| Task | 제목 | 완료 커밋/브랜치 |
|------|------|-----------------|
| Task 1 | Elastic 핸들 사이즈 조정 동작 구현 | WIP-20260214-001 |
| Task 2 | 루프 모드 로직 객체화 (Strategy Pattern) | `dc76cd7` (mode-objectification) |
| Task 4 | 에디터 버전 체크 팝업 | `9b2e3c9` (feat/editor-version-check) |

