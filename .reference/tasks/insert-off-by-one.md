# `Insert` / `AddToEnd` 인덱스 오프바이원

- **유형**: 버그 수정
- **공수**: 낮음
- **위험도**: 중간

## 문제

`Insert()`에서 `insertIndex`를 `Mathf.Clamp(insertIndex, 0, m_cellCount - 1)`로 제한하여
리스트 맨 뒤(`m_cellCount` 위치)에 삽입할 수 없음.
`AddToEnd()`도 `m_cellCount - 1`을 전달하므로 마지막 셀 **앞에** 삽입됨.

## 수정 대상

`RecycleScroller_Functions.cs:964,978`

```csharp
// 현재 코드
insertIndex = Mathf.Clamp(insertIndex, 0, m_cellCount - 1);  // :964
public void AddToEnd(int addCount = 1) => Insert(m_cellCount - 1, addCount);  // :978
```

## 수정 가이드

```csharp
// 상한을 m_cellCount로 변경
insertIndex = Mathf.Clamp(insertIndex, 0, m_cellCount);  // :964
public void AddToEnd(int addCount = 1) => Insert(m_cellCount, addCount);  // :978
```

## 검증 방법

1. `AddToEnd()`로 셀 추가 후 마지막 위치에 정상 삽입되는지 확인
2. `Insert(0)`으로 맨 앞 삽입이 여전히 정상 동작하는지 확인
3. 범위를 벗어난 인덱스가 올바르게 클램프되는지 확인
