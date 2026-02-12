# 코드 스타일 가이드

## 명명 규칙

| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스/구조체 | PascalCase | `RecycleScroller`, `CellGroupData` |
| 메서드 | PascalCase | `LoadData()`, `JumpToPage()` |
| 프로퍼티 | PascalCase | `CellCount`, `ScrollAxis` |
| public 필드 | PascalCase (최소화) | `CellCreateFuncWhenPoolEmpty` |
| private 필드 | m_ + camelCase | `m_scrollRect`, `m_cellPool` |
| 인터페이스 | I + PascalCase | `IRecycleScrollerDelegate` |
| 열거형 | e + PascalCase | `eScrollAxis`, `eMagnetPivotType` |
| 열거값 | PascalCase 또는 UPPER_SNAKE | `VERTICAL`, `Constant__0_5` |
| 상수 | UPPER_SNAKE_CASE | `DEFAULT_POOL_SUBKEY` |
| 네임스페이스 | PascalCase | `RecycleScroll`, `CustomSerialization` |
| 파일명 | 클래스명과 동일 | `RecycleScroller.cs` |
| 부분 클래스 파일 | ClassName_Responsibility | `RecycleScroller_Functions.cs` |

## 아키텍처 패턴

### Partial Class 패턴
메인 클래스를 기능별로 분리:
```
RecycleScroller.cs              → 핵심 필드/프로퍼티 선언
RecycleScroller_Functions.cs    → 주요 기능 메서드
RecycleScroller_LoadData.cs     → 데이터 로드 로직
RecycleScroller_Inspector.cs    → 에디터 인스펙터 로직
RecycleScroller_LoadParam.cs    → 로드 파라미터
RecycleScroller_OnValidate.cs   → 유효성 검사
```

### Delegate 패턴
인터페이스 기반 콜백으로 외부 제어:
```csharp
public interface IRecycleScrollerDelegate
{
    RecycleScrollerCell GetCell(RecycleScroller scroller, int dataIndex, int cellViewIndex);
    int GetCellCount(RecycleScroller scroller);
    RSCellRect GetCellRect(RecycleScroller scroller, int dataIndex);
}
```

### Object Pooling 패턴
```csharp
// Pool 구조: Type → SubKey → Stack<Cell>
Dictionary<Type, Dictionary<string, Stack<RecycleScrollerCell>>>
```

### 에디터/런타임 분리
- `Assets/Editor/` 내 코드는 빌드에서 자동 제외
- 런타임 코드에서 에디터 기능 사용 시 `#if UNITY_EDITOR` 필수

## 코딩 패턴

### SerializeField 우선
```csharp
// 권장: private + SerializeField
[SerializeField] private ScrollRect m_scrollRect;

// 지양: public 필드
public ScrollRect scrollRect; // X
```

### GetComponent 캐싱
```csharp
// 권장: Awake에서 캐싱
private RectTransform m_rectTransform;
void Awake() { m_rectTransform = GetComponent<RectTransform>(); }

// 지양: 반복 호출
void Update() { GetComponent<RectTransform>().sizeDelta = ...; } // X
```

### UniTask 비동기 패턴
```csharp
private CancellationTokenSource m_cts;

public async UniTask LoadDataAsync()
{
    m_cts?.Cancel();
    m_cts = new CancellationTokenSource();
    await LoadDataInternal(m_cts.Token);
}
```

## 안티패턴

| 안티패턴 | 이유 | 대안 |
|----------|------|------|
| `(Type)cast` 남용 | 런타임 에러 위험 | `as` + null 체크 |
| `GetComponent` 반복 호출 | GC + 성능 저하 | Awake에서 캐싱 |
| `string` 비교 | 오타 위험, 성능 저하 | `enum` 사용 |
| `Update()`에서 GC 유발 | 프레임 드롭 | 캐싱, StringBuilder |
| `Find`/`FindObjectOfType` 런타임 사용 | O(n) 탐색, 성능 저하 | 참조 주입, 캐싱 |
| `public` 필드 남용 | 캡슐화 위반 | `[SerializeField] private` |
| 빈 `catch` 블록 | 에러 은폐 | 로깅 또는 재throw |

## 에러 처리

### 필수 규칙
- 빈 `catch` 블록 절대 금지
- null 가능 참조는 반드시 체크 후 사용
- `try-finally`로 리소스 정리 보장

### Unity 전용
```csharp
// 오브젝트 파괴 여부 확인
if (gameObject != null && !ReferenceEquals(gameObject, null))
{
    // 안전한 접근
}
```
