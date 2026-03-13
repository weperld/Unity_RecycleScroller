# `UpdateCellView` 내 LINQ `Where().Sum()` 제거

- **유형**: 성능 (GC / 규칙 위반)
- **공수**: 낮음
- **위험도**: 낮음

## 문제

스크롤 중 매 프레임 호출되는 경로에서 LINQ `Where` + `Sum`이 iterator/delegate 할당을 유발.
프로젝트 규칙 "Update()에서 GC 유발 코드 금지"에 해당.

## 수정 대상

`RecycleScroller_LoadData.cs:668-670`

```csharp
// 현재 코드
int totalCellViewCount = m_list_groupData
    .Where((w, index) => m_reverse ? (setStartIndex <= index && index <= setLastIndex) : (setLastIndex <= index && index <= setStartIndex))
    .Sum(s => s.cellCount);
```

## 수정 가이드

for 루프로 수동 합산.

```csharp
int totalCellViewCount = 0;
int rangeStart = m_reverse ? setStartIndex : setLastIndex;
int rangeEnd = m_reverse ? setLastIndex : setStartIndex;
for (int idx = rangeStart; idx <= rangeEnd; idx++)
    totalCellViewCount += m_list_groupData[idx].cellCount;
```

## 검증 방법

1. 스크롤 시 동작이 기존과 동일한지 확인
2. Profiler에서 GC Alloc 감소 확인
