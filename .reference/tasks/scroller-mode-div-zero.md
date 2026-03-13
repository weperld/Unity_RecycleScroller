# `ScrollerMode.ConvertShowToReal` / `ConvertRealToShow` 0 나누기 위험

- **유형**: 버그 수정
- **공수**: 낮음
- **위험도**: 중간

## 문제

`NormalScrollerMode.ConvertShowToReal`(:49)과 `LoopScrollerMode.ConvertRealToShow`(:43), `ConvertShowToReal`(:50)에서
`% showingContentSize`를 수행하는데, `showingContentSize`가 0이면 `DivideByZeroException` 발생.

## 수정 대상

| 위치 | 코드 |
|------|------|
| `NormalScrollerMode.cs:49` | `var val = showValue % showingContentSize;` |
| `LoopScrollerMode.cs:43` | `pos %= showingContentSize;` |
| `LoopScrollerMode.cs:50` | `var val = showValue % showingContentSize;` |

## 수정 가이드

각 위치에 `showingContentSize <= 0f` 가드 추가. 0 이하인 경우 0f 반환.

## 검증 방법

1. 셀 0개 상태에서 스크롤 위치 변환 메서드 호출 시 예외 없이 동작하는지 확인
