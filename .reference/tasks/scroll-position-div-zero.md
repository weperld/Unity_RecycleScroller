# `RealScrollPosition` setter 0 나누기 위험

- **유형**: 버그 수정
- **공수**: 낮음
- **위험도**: 중간

## 문제

`RealScrollPosition` setter에서 `value / RealScrollSize`를 수행하는데,
콘텐츠가 뷰포트보다 작거나 같으면 `RealScrollSize`가 0이 되어 NaN/Infinity 발생.

## 수정 대상

`RecycleScroller.cs:190`

```csharp
// 현재 코드
set => RealNormalizedScrollPosition = value / RealScrollSize;
```

## 수정 가이드

```csharp
// RealScrollSize가 0 이하인 경우 0f 설정
set => RealNormalizedScrollPosition = RealScrollSize > 0f ? value / RealScrollSize : 0f;
```

## 검증 방법

1. 셀 0개 또는 셀 총 크기 < 뷰포트 상태에서 `RealScrollPosition` setter 호출 시 예외 없이 동작하는지 확인
