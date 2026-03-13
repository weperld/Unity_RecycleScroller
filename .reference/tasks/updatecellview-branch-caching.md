# `UpdateCellView` 매 프레임 반복 분기 제거 — `m_reverse` / `ScrollAxis` 캐싱

- **유형**: 성능 (분기 최소화)
- **공수**: 중간
- **위험도**: 낮음
- **우선순위**: 높음

## 문제

`UpdateCellView()`는 스크롤 중 매 프레임 호출되는 핵심 핫 경로인데,
런타임 중 변하지 않는 `m_reverse`와 `ScrollAxis` 값으로 매 호출마다 다수의 분기를 수행.

**`m_reverse` 분기 — 매 프레임 8회 이상 (+ 루프 내 그룹 수만큼 추가)**

| 위치 | 코드 |
|------|------|
| `RecycleScroller_LoadData.cs:627` | `m_reverse ? groupCount - 1 : 0` |
| `RecycleScroller_LoadData.cs:629` | `m_reverse ? 0 : groupCount - 1` |
| `RecycleScroller_LoadData.cs:638-640` | `m_reverse ? (last <= idx <= first) : (first <= idx <= last)` |
| `RecycleScroller_LoadData.cs:666` | `m_reverse ? firstGroupViewIndex : lastGroupViewIndex` |
| `RecycleScroller_LoadData.cs:667` | `m_reverse ? lastGroupViewIndex : firstGroupViewIndex` |
| `RecycleScroller_LoadData.cs:669` | LINQ 내 `m_reverse` 조건 |
| `RecycleScroller_LoadData.cs:685` | `m_reverse ? cellLastIndex : cellStartIndex` (루프 내) |
| `RecycleScroller_LoadData.cs:686` | `m_reverse ? cellStartIndex : cellLastIndex` (루프 내) |
| `RecycleScroller.cs:248` | `Rt_TopSpaceCell` 프로퍼티 접근 시 `m_reverse` 분기 |
| `RecycleScroller.cs:249` | `Rt_BottomSpaceCell` 프로퍼티 접근 시 `m_reverse` 분기 |

**`ScrollAxis` 분기 — 매 프레임 3~4회**

| 위치 | 코드 |
|------|------|
| `RecycleScroller_LoadData.cs:594` | `ScrollAxis == eScrollAxis.VERTICAL ? contentPos.y : -contentPos.x` |
| `RecycleScroller_LoadData.cs:615` | `ScrollAxis == eScrollAxis.VERTICAL ? Vector2.up : Vector2.right` |
| `RecycleScroller_LoadData.cs:617-624` | `switch (ScrollAxis)` widthVec 설정 |

## 수정 가이드

**1) `UpdateCellView` 진입부에서 `m_reverse` 기반 값을 1회 swap 후 일관 사용:**

```csharp
// 메서드 최상단에서 1회 정리
var viewFirst = m_reverse ? lastGroupViewIndex : firstGroupViewIndex;
var viewLast = m_reverse ? firstGroupViewIndex : lastGroupViewIndex;
// 이후 m_reverse 분기 없이 viewFirst/viewLast만 사용
```

**2) `ScrollAxis` 관련 값은 LoadData 시점에 필드 캐싱:**

```csharp
// LoadData 시 1회 캐싱
private Vector2 m_cachedAxisVec;      // Vector2.up or Vector2.right
private Vector2 m_cachedWidthMask;    // widthVec 마스크
```

**3) `Rt_TopSpaceCell` / `Rt_BottomSpaceCell` 프로퍼티도 LoadData 시 캐싱:**

```csharp
// LoadData 시 1회 캐싱
private RectTransform m_cachedTopSpaceCell;
private RectTransform m_cachedBottomSpaceCell;
```

## 검증 방법

1. reverse ON/OFF, Vertical/Horizontal 각 조합에서 스크롤 동작이 기존과 동일한지 확인
2. Profiler에서 `UpdateCellView` 내 분기 감소 및 CPU 시간 변화 확인
