# `GetSafeRange` 예외 기반 흐름 제어 → 범위 검증으로 교체

- **유형**: 리팩토링
- **공수**: 낮음
- **위험도**: 낮음

## 문제

`GetSafeRange`가 정상 흐름에서 `ArgumentOutOfRangeException`을 catch하여 빈 리스트를 반환.
예외 기반 흐름 제어는 성능/가독성 모두에서 비효율적.

## 수정 대상

`CollectionUtils.cs:34-46`

```csharp
// 현재 코드
try { return list.GetRange(index, count); }
catch (ArgumentOutOfRangeException) { return new List<T>(); }
```

## 수정 가이드

```csharp
public static List<T> GetSafeRange<T>(this List<T> list, int index, int count)
{
    if (list == null || index < 0 || index >= list.Count || count <= 0)
        return new List<T>();

    count = Math.Min(count, list.Count - index);
    return list.GetRange(index, count);
}
```

## 검증 방법

1. 정상 범위, 범위 초과, 빈 리스트 등 다양한 입력에서 기존과 동일한 결과 반환 확인
