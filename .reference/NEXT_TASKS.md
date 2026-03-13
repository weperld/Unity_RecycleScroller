# 다음 예정 작업 목록

> 최종 업데이트: 2026-03-14
> 상세 문서: [.reference/tasks/](./tasks/)

---

## A. 버그 수정

| Task | 제목 | 공수 | 위험도 | 상세 |
|------|------|------|--------|------|
| 1 | 페이지 순환 분기 — `LoopScrollIsOn` → `IsLoopScrollable` 교체 | 낮음 | 낮음 | [page-loop-branch](./tasks/page-loop-branch.md) |
| 2 | `Insert` / `AddToEnd` 인덱스 오프바이원 | 낮음 | 중간 | [insert-off-by-one](./tasks/insert-off-by-one.md) |
| 3 | `RealScrollPosition` setter 0 나누기 위험 | 낮음 | 중간 | [scroll-position-div-zero](./tasks/scroll-position-div-zero.md) |
| 4 | `ScrollerMode` Convert 메서드 0 나누기 위험 | 낮음 | 중간 | [scroller-mode-div-zero](./tasks/scroller-mode-div-zero.md) |

## B. 성능 개선

| Task | 제목 | 공수 | 위험도 | 우선순위 | 상세 |
|------|------|------|--------|----------|------|
| **5** | **`UpdateCellView` 매 프레임 분기 제거 — `m_reverse`/`ScrollAxis` 캐싱** | **중간** | **낮음** | **높음** | [updatecellview-branch-caching](./tasks/updatecellview-branch-caching.md) |
| 6 | `UpdateCellView` 내 매 프레임 `List<int>` 할당 제거 | 낮음 | 낮음 | - | [updatecellview-list-alloc](./tasks/updatecellview-list-alloc.md) |
| 7 | `UpdateCellView` 내 LINQ `Where().Sum()` 제거 | 낮음 | 낮음 | - | [updatecellview-linq-removal](./tasks/updatecellview-linq-removal.md) |
| 8 | 셀/그룹 이름 `string.Format` — `#if UNITY_EDITOR` 감싸기 | 낮음 | 낮음 | - | [string-format-editor-only](./tasks/string-format-editor-only.md) |
| 9 | `FindClosestIndex` LINQ → O(n) for 루프 | 낮음 | 낮음 | - | [find-closest-index-linq](./tasks/find-closest-index-linq.md) |
| 10 | `Enumerable.Range` / `Repeat` → for 루프 | 낮음 | 낮음 | - | [enumerable-to-for-loop](./tasks/enumerable-to-for-loop.md) |

## C. 리팩토링 / 코드 품질

| Task | 제목 | 공수 | 위험도 | 상세 |
|------|------|------|--------|------|
| 11 | `RecalculateForInsert` — `CanDoPartialRecalc()` 사문화 코드 연결 | 낮음 | 낮음 | [partial-recalc-dead-code](./tasks/partial-recalc-dead-code.md) |
| 12 | `(Type)cast` 규칙 위반 수정 | 낮음 | 낮음 | [type-cast-rule-violation](./tasks/type-cast-rule-violation.md) |
| 13 | `GetSafeRange` 예외 기반 흐름 → 범위 검증 | 낮음 | 낮음 | [safe-range-exception-flow](./tasks/safe-range-exception-flow.md) |
| 14 | `del` public 필드 → 프로퍼티 변환 | 중간 | 낮음 | [del-field-to-property](./tasks/del-field-to-property.md) |
