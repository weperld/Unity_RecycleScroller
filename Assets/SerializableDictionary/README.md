# SerializableDictionary - 직렬화 가능 딕셔너리

Unity의 Inspector에서 Dictionary를 직접 편집할 수 있게 해주는 직렬화 가능한 Dictionary 구현입니다.
Unity의 기본 `Dictionary<TKey, TValue>`는 SerializeField로 노출할 수 없는 제한을 해결합니다.

## 파일 구조

| 파일 | 설명 |
|------|------|
| `SerializableDictionary.cs` | 런타임 직렬화 Dictionary 구현 |
| `Editor/SerializableDictionaryDrawer.cs` | Inspector 드로어 |
| `Editor/SerializableKeyValuePairDrawer.cs` | Key-Value 쌍 드로어 |
| `Editor/ISerializableKVPDrawerStrategy.cs` | 드로어 전략 인터페이스 |
| `Editor/SerializableKVPDrawerStrategies.cs` | 기본 드로어 전략 구현 |

## 사용 예시

```csharp
using UnityEngine;
using CustomSerialization;

public class MyComponent : MonoBehaviour
{
    // Inspector에서 직접 편집 가능한 Dictionary
    [SerializeField]
    private SerializableDictionary<string, int> scores = new();

    [SerializeField]
    private SerializableDictionary<int, GameObject> prefabMap = new();

    private void Start()
    {
        // 일반 Dictionary처럼 사용
        if (scores.TryGetValue("player1", out var score))
            Debug.Log($"Score: {score}");
    }
}
```

## 특징

- Unity Inspector에서 Key-Value 쌍을 시각적으로 편집
- 제네릭 타입 지원 (기본 타입, UnityEngine.Object, Enum 등)
- 런타임에서 표준 `Dictionary<TKey, TValue>`처럼 사용 가능
- 커스텀 드로어 전략으로 확장 가능
