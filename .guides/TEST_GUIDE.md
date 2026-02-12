# 테스트 가이드

## 테스트 프레임워크

### Unity Test Framework
- **기반**: NUnit 3.x (`com.unity.ext.nunit`)
- **실행**: `Window > General > Test Runner`
- **위치**: 테스트 코드는 `Assets/Tests/` 또는 `Assets/Editor/Tests/` 에 배치

### 테스트 모드
| 모드 | 용도 | 실행 환경 |
|------|------|-----------|
| **Edit Mode** | 에디터 전용 로직, 순수 C# 로직 | Unity 에디터 |
| **Play Mode** | MonoBehaviour, 씬 로직, UI 상호작용 | Play Mode 진입 |

## 테스트 명명 규칙

```
[테스트대상메서드]_[시나리오]_[기대결과]
```

예시:
```csharp
[Test]
public void GetCellCount_WithEmptyDelegate_ReturnsZero() { }

[Test]
public void LoadData_WithValidData_SetsLoadedState() { }

[Test]
public void JumpToPage_WithInvalidIndex_ClampsToRange() { }
```

## 테스트 패턴

### Edit Mode 테스트 (순수 로직)
```csharp
[TestFixture]
public class RecycleScrollerDataTests
{
    [Test]
    public void CellGroupData_Creation_HasCorrectDefaults()
    {
        var data = new CellGroupData();
        Assert.AreEqual(0, data.cellCount);
    }

    [TestCase(0, 5)]
    [TestCase(10, 15)]
    public void CellGroupData_IndexRange_IsValid(int start, int end)
    {
        var data = new CellGroupData { startDataIndex = start, endDataIndex = end };
        Assert.That(data.endDataIndex, Is.GreaterThanOrEqualTo(data.startDataIndex));
    }
}
```

### Play Mode 테스트 (컴포넌트)
```csharp
[UnityTest]
public IEnumerator RecycleScroller_LoadData_CreatesVisibleCells()
{
    var go = new GameObject();
    var scroller = go.AddComponent<RecycleScroller>();
    // 설정...

    yield return null; // 한 프레임 대기

    Assert.IsTrue(scroller.LoadDataProceedState == LoadDataProceedState.Loaded);

    Object.Destroy(go);
}
```

### EasingFunctions 테스트
```csharp
[TestFixture]
public class EasingFunctionTests
{
    [TestCase(0f, 0f)]
    [TestCase(1f, 1f)]
    public void Linear_BoundaryValues_AreCorrect(float input, float expected)
    {
        Assert.AreEqual(expected, EasingFunctions.Linear(input), 0.001f);
    }
}
```

## 커버리지 기준

| 영역 | 최소 커버리지 | 우선순위 |
|------|-------------|----------|
| 데이터 구조 (Datas, Enums) | 90% | 높음 |
| 핵심 로직 (LoadData, Pooling) | 80% | 높음 |
| 이징 함수 | 70% | 중간 |
| 에디터 도구 | 50% | 낮음 |
| UI 상호작용 | 40% | 낮음 |

## Mock/Stub 사용 규칙

### IRecycleScrollerDelegate Mock
```csharp
public class MockScrollerDelegate : IRecycleScrollerDelegate
{
    public int CellCount { get; set; } = 10;
    public RSCellRect CellRect { get; set; }

    public int GetCellCount(RecycleScroller scroller) => CellCount;
    public RSCellRect GetCellRect(RecycleScroller scroller, int dataIndex) => CellRect;
    public RecycleScrollerCell GetCell(RecycleScroller scroller, int dataIndex, int cellViewIndex)
    {
        // 테스트용 셀 반환
        return null;
    }
}
```

### 주의사항
- Unity 컴포넌트는 `new`로 생성 불가 → `AddComponent<T>()` 사용
- 테스트 후 생성한 GameObject 반드시 `Destroy()` 호출
- `async` 테스트는 `[UnityTest]` + `IEnumerator` 또는 UniTask 지원 패키지 활용
