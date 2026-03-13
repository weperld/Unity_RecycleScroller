# `FindClosestIndex` LINQ 제거 — O(n) for 루프로 교체

- **유형**: 성능 (GC / 알고리즘)
- **공수**: 낮음
- **위험도**: 낮음

## 문제

`Select().OrderBy().FirstOrDefault()`로 최솟값을 찾아 O(n log n) + 익명 객체 할당.
O(n) 순회로 충분하며, 페이징 시마다 호출됨.

## 수정 대상

`CollectionUtils.cs:9-18`

```csharp
// 현재 코드
var ordered = positions
    .Select((pos, index) => new { index, dist = distanceFunc(pos, pivot) })
    .OrderBy(o => o.dist)
    .FirstOrDefault();
return ordered?.index ?? -1;
```

## 수정 가이드

```csharp
int closestIndex = -1;
float minDist = float.MaxValue;
int i = 0;
foreach (var pos in positions)
{
    var dist = distanceFunc(pos, pivot);
    if (dist < minDist)
    {
        minDist = dist;
        closestIndex = i;
    }
    i++;
}
return closestIndex;
```

## 검증 방법

1. 페이지 이동 시 가장 가까운 페이지 인덱스가 정확히 반환되는지 확인
