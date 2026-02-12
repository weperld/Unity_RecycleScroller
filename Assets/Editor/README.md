# Editor - 에디터 전용 도구

Unity Editor에서만 동작하는 커스텀 인스펙터, 프로퍼티 드로어, 메뉴 도구가 위치한 디렉토리입니다.
이 폴더의 코드는 빌드에 포함되지 않습니다.

## 디렉토리 구조

```
Editor/
├── RecycleScroll/       # 스크롤러 커스텀 인스펙터
├── Attributes/          # 커스텀 속성 드로어
├── Drawers/             # 프로퍼티 드로어
├── Creator/             # 스크롤뷰 생성 메뉴
└── LoadDataExtension/   # 확장 컴포넌트 에디터
```

## RecycleScroll/

### RecycleScrollerEditor.cs

`RecycleScroller` 컴포넌트의 커스텀 인스펙터입니다.

**기능:**
- 설정값의 조건부 표시/숨김/비활성화 (예: `FixCellCountInGroup` 해제 시 `fixedCellCount` 숨김)
- Play Mode에서 설정 변경 방지 (`BeginDisabledGroup`)
- Pool Management 필드 (타입별 최대 풀 사이즈)
- **Debug Info 섹션** (Play Mode 전용):
  - Active Cells / Active Groups / Pooled Cells 수
  - 현재 스크롤 위치 (정규화)
  - 현재 페이지 인덱스 / 총 페이지 수
  - LoadData 상태 (NotLoaded / Loading / Loaded)
  - `Repaint()`로 실시간 갱신

## Attributes/

Inspector에 사용되는 커스텀 속성의 드로어 구현입니다.

| 파일 | 대응 속성 | 기능 |
|------|-----------|------|
| `ColoredHeaderDrawer.cs` | `[ColoredHeader]` | 색상 헤더 표시 |
| `HelpBoxDrawer.cs` | `[HelpBox]` | 도움말 박스 표시 |
| `HelpBoxAutoDrawer.cs` | `[HelpBoxAuto]` | 자동 도움말 박스 |
| `HorizontalLineDrawer.cs` | `[HorizontalLine]` | 수평 구분선 |
| `EditorDrawerHelper.cs` | - | 드로어 공통 유틸리티 |

## Drawers/

데이터 타입별 커스텀 프로퍼티 드로어입니다.

| 파일 | 대상 타입 | 기능 |
|------|-----------|------|
| `BoolVector2Drawer.cs` | `BoolVector2` | width/height 토글 한 줄 표시 |
| `ScrollPagingConfigDrawer.cs` | `ScrollPagingConfig` | 페이징 설정 펼침 UI |
| `MinMaxIntDrawer.cs` | `MinMaxInt` | min/max 슬라이더 |
| `MinMaxFloatDrawer.cs` | `MinMaxFloat` | min/max 슬라이더 |
| `MinMaxDoubleDrawer.cs` | `MinMaxDouble` | min/max 슬라이더 |
| `MinMaxLongDrawer.cs` | `MinMaxLong` | min/max 슬라이더 |
| `LoopScrollbarDrawer.cs` | `LoopScrollbar` | 루프 스크롤바 인스펙터 |
| `RecycleScrollerContentEditor.cs` | Content RectTransform | 콘텐트 영역 에디터 |

## Creator/

### RecycleScrollViewCreator.cs

`GameObject > UI > RecycleScrollView` 메뉴를 통해 RecycleScroller가 구성된 스크롤뷰를 자동 생성하는 도구입니다.

자동 생성되는 구조:
```
RecycleScrollView
├── Viewport
│   └── Content (VerticalLayoutGroup)
└── RecycleScroller (Component)
```

## LoadDataExtension/

LoadDataExtension 컴포넌트의 커스텀 에디터입니다.

| 파일 | 기능 |
|------|------|
| `RS_LDE_ChangeAlignment_UsingByGroupCount_BaseEditor.cs` | 기본 정렬 변경 에디터 |
| `RS_LDE_ChangeAlignment_UsingByGroupCount2Editor.cs` | 2단계 정렬 변경 에디터 |
