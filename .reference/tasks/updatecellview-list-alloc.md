# `UpdateCellView` 내 매 프레임 `List<int>` 할당 제거

- **유형**: 성능 (GC)
- **공수**: 낮음
- **위험도**: 낮음

## 문제

`UpdateCellView`에서 `pushCellIndexList`와 `pushGroupIndexList`를 매 호출마다 `new List<int>()`로 생성.
스크롤 중 매 프레임 호출되므로 GC 압박 유발.

## 수정 대상

`RecycleScroller_LoadData.cs:634-635`

```csharp
// 현재 코드
var pushCellIndexList = new List<int>();
var pushGroupIndexList = new List<int>();
```

## 수정 가이드

클래스 레벨 `List<int>` 필드로 변경하고 매 호출 시 `Clear()`.

```csharp
// 클래스 필드
private readonly List<int> m_pushCellIndexList = new();
private readonly List<int> m_pushGroupIndexList = new();

// 메서드 내
m_pushCellIndexList.Clear();
m_pushGroupIndexList.Clear();
```

## 검증 방법

1. 스크롤 시 동작이 기존과 동일한지 확인
2. Profiler에서 GC Alloc 감소 확인
