# LoadDataExtension - 데이터 로드 확장 컴포넌트

RecycleScroller의 `LoadData` 완료 시 자동으로 추가 처리를 실행하는 확장 컴포넌트입니다.
RecycleScroller와 같은 GameObject에 부착하면 LoadData 콜백에 자동 등록됩니다.

## 파일 구조

| 파일 | 설명 |
|------|------|
| `LoadDataExtensionComponent.cs` | 추상 기본 클래스. `LoadDataExtendFunction()` 메서드를 오버라이드하여 확장 로직 구현 |
| `RS_LDE_ChangeAlignment_UsingByGroupCount_Base.cs` | 그룹 수에 따라 Content의 정렬(Alignment)을 동적으로 변경하는 기본 구현 |
| `RS_LDE_ChangeAlignment_UsingByGroupCount.cs` | 단일 조건 정렬 변경 |
| `RS_LDE_ChangeAlignment_UsingByGroupCount2.cs` | 2단계 조건 정렬 변경 |

## 동작 원리

```
LoadData() 호출
  └── LoadData 완료 콜백
        └── LoadDataExtensionComponent.LoadDataExtendFunction() 자동 호출
```

RecycleScroller는 `GetComponents<LoadDataExtensionComponent>()`로 같은 GameObject에 부착된 모든 확장 컴포넌트를 찾아 LoadData 완료 시 순서대로 호출합니다.

## 사용 예시

### 커스텀 확장 만들기

```csharp
using RecycleScroll;

public class MyLoadDataExtension : LoadDataExtensionComponent
{
    public override void LoadDataExtendFunction(RecycleScroller scroller, LoadDataResultState result)
    {
        if (result != LoadDataResultState.Complete) return;

        // LoadData 완료 후 추가 처리
        // 예: 그룹 수에 따라 레이아웃 조정, 스크롤 위치 보정 등
    }
}
```

### 그룹 수 기반 정렬 변경

`RS_LDE_ChangeAlignment_UsingByGroupCount` 컴포넌트를 부착하면, 그룹 수가 특정 조건일 때 Content의 `childAlignment`를 자동으로 변경합니다. 예를 들어 아이템이 1개일 때 중앙 정렬, 여러 개일 때 좌측 정렬 등의 동적 레이아웃에 활용합니다.
