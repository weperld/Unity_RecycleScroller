# RecycleScroll - 핵심 스크롤러

RecycleScroller의 핵심 런타임 코드가 위치한 디렉토리입니다.

## 파일 구조

RecycleScroller는 **partial class** 패턴으로 책임별로 파일이 분리되어 있습니다.

### 메인 컨트롤러 (RecycleScroller partial class)

| 파일 | 책임 |
|------|------|
| `RecycleScroller.cs` | 필드 선언, 프로퍼티, 이벤트, Unity 기본 이벤트 |
| `RecycleScroller_Functions.cs` | 초기화, 풀링, MoveTo/JumpTo, 페이징, 드래그 핸들러, Insert/Remove |
| `RecycleScroller_LoadData.cs` | 동기/비동기 데이터 로드, UpdateCellView, 셀 가시성 관리 |
| `RecycleScroller_LoadParam.cs` | LoadParam 파라미터 체계 (스크롤 위치 설정, 포커스 등) |
| `RecycleScroller_Inspector.cs` | SerializeField 선언 (Inspector에 노출되는 설정값) |
| `RecycleScroller_OnValidate.cs` | 에디터 OnValidate 로직 (설정값 변경 시 자동 검증) |

### 셀 및 인터페이스

| 파일 | 설명 |
|------|------|
| `RecycleScrollerCell.cs` | 셀 기본 클래스. 상속하여 커스텀 셀 구현. `OnBecameVisible`/`OnBecameInvisible` 가상 메서드 제공 |
| `IRecycleScrollerDelegate.cs` | 델리게이트 인터페이스 (`GetCell`, `GetCellCount`, `GetCellRect`). `RecycleScrollDelegate` 클래스형 구현도 포함 |
| `ILoopScrollDelegate.cs` | 루프 스크롤 내부 델리게이트 |

### 데이터 및 열거형

| 파일 | 설명 |
|------|------|
| `RecycleScrollerDatas.cs` | 데이터 구조 (`CellGroupData`, `ScrollPagingConfig`, `LoadDataCallbacks`, `LoadDataProceedState` 등) |
| `RecycleScrollerEnums.cs` | 열거형 (`eScrollAxis`, `eMagnetPivotType`, `eScrollDirection`) |

### 유틸리티

| 파일 | 설명 |
|------|------|
| `EasingFunctions.cs` | 30종 이상 이징 함수 (Linear, Quad, Cubic, Elastic, Bounce 등) |
| `LoopScrollbar.cs` | 루프 스크롤 대응 커스텀 스크롤바 |
| `MathUtils.cs` | 수학 유틸리티 (MinMax 구조체, 리스트 확장 메서드 등) |
| `RecycleScrollerHelper.cs` | 스크롤러 확장 메서드 |

### 확장

| 파일 | 설명 |
|------|------|
| `AddressableCellProvider.cs` | Addressable 기반 비동기 셀 프리팹 로더. `ENABLE_ADDRESSABLES` define 시 활성화 |

## 핵심 흐름

```
1. scroller.del = delegate 등록
2. scroller.LoadData() 호출
   ├── Init() - ScrollRect, Content, SpaceCell 초기화
   ├── CalculateTotalScrollSize() - 셀 크기 합산, 그룹/페이지 데이터 생성
   ├── CheckLoop() - 루프 스크롤용 양 끝 그룹 복제
   └── UpdateCellView() - 뷰포트 내 셀 배치
3. 스크롤 시 OnScrollRectScrolling()
   ├── UpdateCellView() - 뷰포트 밖 셀 → Pool, 새 셀 → 뷰포트
   ├── onScrollDirectionChanged - 방향 감지 이벤트
   └── onChangePage - 페이지 변경 감지
```

## 셀 생명주기

```
[Pool] ──Pop──> [Viewport] ──Push──> [Pool]
                    │                    │
          OnBecameVisible()    OnBecameInvisible()
          onCellBecameVisible  onCellBecameInvisible
```

- 풀에서 꺼내져 뷰포트에 배치될 때: `OnBecameVisible` + `onCellBecameVisible` 호출
- 뷰포트에서 벗어나 풀로 반환될 때: `OnBecameInvisible` + `onCellBecameInvisible` 호출
- 풀 크기가 `maxPoolSizePerType`을 초과하면 반환 대신 `Destroy` 처리

## 이벤트 목록

| 이벤트 | 타입 | 발생 시점 |
|--------|------|-----------|
| `onScroll` | `Action<Vector2>` | 매 스크롤 프레임 |
| `onBeginDrag` | `Action` | 드래그 시작 |
| `onEndDrag` | `Action` | 드래그 종료 |
| `onChangePage` | `Action<int, int>` | 페이지 변경 (prev, next) |
| `onEndEasing` | `Action` | 이징 완료 |
| `onScrollDirectionChanged` | `Action<eScrollDirection>` | 방향 변경 (Forward/Backward) |
| `onCellBecameVisible` | `Action<Cell, int>` | 셀 뷰포트 진입 |
| `onCellBecameInvisible` | `Action<Cell, int>` | 셀 뷰포트 이탈 |
