# Attributes - 커스텀 속성 정의

Unity Inspector를 꾸미기 위한 커스텀 속성(Attribute) 정의 파일입니다.
이 속성들은 `[SerializeField]`와 함께 사용하여 Inspector UI를 향상시킵니다.
대응하는 드로어(Drawer)는 `Editor/Attributes/`에 있습니다.

## 속성 목록

| 파일 | 속성 | 용도 | 사용 예시 |
|------|------|------|-----------|
| `ColoredHeaderAttribute.cs` | `[ColoredHeader]` | 색상이 있는 섹션 헤더 표시 | `[ColoredHeader("[Pool Management]", "#FFFF99")]` |
| `HelpBoxAttribute.cs` | `[HelpBox]` | 고정 도움말 박스 표시 | `[HelpBox("주의사항", MessageType.Warning)]` |
| `HelpBoxAutoAttribute.cs` | `[HelpBoxAuto]` | 필드 위에 자동 도움말 표시 | `[HelpBoxAuto("설명 텍스트", HelpBoxMessageType.Info)]` |
| `HorizontalLineAttribute.cs` | `[HorizontalLine]` | 수평 구분선 삽입 | `[HorizontalLine]` |
| `OnlyLastAttributes.cs` | `[OnlyLast]` | 마지막 요소만 표시 | 배열/리스트 용 |
| `ColorHexTemplate.cs` | - | 자주 사용하는 색상 HEX 상수 모음 | `ColorHexTemplate.CT_HEX_FFFF99` |
| `AttributeUtils.cs` | - | 속성 유틸리티 (HelpBoxMessageType 등) | - |

## 사용 예시

```csharp
using UnityEngine;

public class MyComponent : MonoBehaviour
{
    [ColoredHeader("[Settings]", ColorHexTemplate.CT_HEX_FFFF99)]
    [SerializeField] private float speed = 1f;

    [HelpBoxAuto("이 값은 런타임에 변경할 수 없습니다", HelpBoxMessageType.Warning)]
    [SerializeField] private int maxCount = 10;

    [HorizontalLine]
    [SerializeField] private bool useFeature = false;
}
```

## 구조

```
Attributes/          (런타임 - 빌드 포함)
  └── 속성 클래스 정의

Editor/Attributes/   (에디터 전용 - 빌드 미포함)
  └── 드로어(Drawer) 구현
```

속성 정의는 런타임에도 사용 가능하도록 `Attributes/`에, 실제 Inspector 렌더링 로직은 `Editor/Attributes/`에 분리되어 있습니다.
