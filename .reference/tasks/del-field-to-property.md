# `del` public 필드 → 프로퍼티 변환

- **유형**: 리팩토링 (API 개선)
- **공수**: 중간 (외부 API 변경)
- **위험도**: 낮음

## 문제

`public IRecycleScrollerDelegate del;`이 프로퍼티가 아닌 public 필드로 노출.
외부에서 아무 시점에나 변경 가능하고, null 설정 시 알림이 없음.

## 수정 대상

`RecycleScroller.cs:320`

```csharp
// 현재 코드
public IRecycleScrollerDelegate del;
```

## 수정 가이드

프로퍼티로 변환. 메이저 버전 업데이트 시 수행 권장 (API 변경).

```csharp
private IRecycleScrollerDelegate m_del;
public IRecycleScrollerDelegate del
{
    get => m_del;
    set => m_del = value;
}
```

## 검증 방법

1. 기존에 `del`을 직접 할당하던 코드가 정상 컴파일되는지 확인
2. Samples~ 내 사용 코드에서도 정상 동작하는지 확인
