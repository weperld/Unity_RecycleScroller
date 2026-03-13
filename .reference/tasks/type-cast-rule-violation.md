# `(Type)cast` 규칙 위반 수정

- **유형**: 코드 품질 (규칙 위반)
- **공수**: 낮음
- **위험도**: 낮음

## 문제

프로젝트 절대 규칙 "as 연산자 + null 체크 사용. (Type)cast 금지"를 위반하는 코드 존재.

## 수정 대상

| 위치 | 코드 |
|------|------|
| `RecycleScroller_Functions.cs:573` | `pop = (HorizontalOrVerticalLayoutGroup)inst.AddComponent(needType);` |

## 수정 가이드

```csharp
// as + null 체크로 교체
pop = inst.AddComponent(needType) as HorizontalOrVerticalLayoutGroup;
```

## 검증 방법

1. 그룹 셀 생성 시 정상 동작하는지 확인
