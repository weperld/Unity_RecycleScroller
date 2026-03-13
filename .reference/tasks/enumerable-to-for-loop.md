# `Enumerable.Range` / `Enumerable.Repeat` — for 루프로 교체

- **유형**: 성능 (코드 스타일)
- **공수**: 낮음
- **위험도**: 낮음

## 문제

`SetCellSizeList`에서 `Enumerable.Range`, `Enumerable.Repeat` 사용. LoadData 시 1회 호출이라 영향은 작지만
프로젝트의 LINQ 지양 스타일과 불일치.

## 수정 대상

`RecycleScroller_LoadData.cs:367,372`

```csharp
// 현재 코드
foreach (var index in Enumerable.Range(0, cellCount))   // :367
m_list_cellSizeVec.AddRange(Enumerable.Repeat(cellRect, cellCount));  // :372
```

## 수정 가이드

```csharp
// :367 교체
for (int i = 0; i < cellCount; i++)
    m_list_cellSizeVec.Add(GetCellRect(i));

// :372 교체
for (int i = 0; i < cellCount; i++)
    m_list_cellSizeVec.Add(cellRect);
```

## 검증 방법

1. LoadData 후 셀 크기 리스트가 기존과 동일한지 확인
