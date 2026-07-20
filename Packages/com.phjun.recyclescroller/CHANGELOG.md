# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.0] - 2026-07-20

### Added
- **커스텀 템플릿 생성 메뉴** — 임포트한 프로젝트에서 자체 프리팹을 `GameObject/UI/RecycleScroll` 메뉴에 생성 항목으로 등록 가능. 최대 20개
  - 설정 창: `Tools > RecycleScroller > 템플릿 설정`
  - 설정 저장: `ProjectSettings/RecycleScrollerTemplates.asset` (프로젝트별로 유효하며 git 으로 팀 공유됨)
  - 템플릿마다 프리팹 / 메뉴 이름 / 생성될 오브젝트 이름 / 언팩 방식(완전 Unpack · 최상위만 Unpack · Unpack 안 함)을 지정
  - 이름을 비우면 기본 명칭으로 폴백 — 메뉴는 `RecycleScroller Template01 Create` 형식, 오브젝트는 프리팹 이름
  - 드래그로 순서 변경 가능하며 그 순서가 메뉴 표시 순서에 반영됨
  - 적용 전 변경 사항은 좌측 컬러 바로 표시(추가=초록, 변경=노랑)되고 `되돌리기`로 복구 가능
  - RecycleScroller 계열이 아닌 임의의 UI 프리팹도 등록 가능

### Changed
- **오브젝트 생성 시 형제 중 이름이 겹치면 `(1)` 이 붙는다** — Unity 표준 UI 생성 메뉴와 동일한 동작. 기존 `Recycle Scroll View` / `Recycle Scrollbar` 메뉴에도 적용되므로 2.0.0 까지와 동작이 다름
- 생성 시 캔버스 자동 생성 · 오브젝트 생성 · 언팩이 하나의 Undo 그룹으로 묶여 `Ctrl+Z` 한 번에 정리됨

### 구현 참고
`MenuItem` 이름은 컴파일 타임 상수이고 런타임 등록 API인 `UnityEditor.Menu.AddMenuItem` 은 internal 이라, 설정 적용 시점에 `Assets/RecycleScroller.Generated/` 에 메뉴 코드를 생성하는 방식을 사용한다. 이 폴더는 설정에서 언제든 재생성되므로 직접 수정하지 않는다.

## [2.0.0] - 2026-07-18

### 아키텍처 변경 (Breaking)
- **루프/비루프 통일: 윈도우 콘텐트 + 가상 스크롤 위치** — 위치의 진실이 `Content.anchoredPosition`에서 내부 가상 스칼라로 이동. Content 렉트는 뷰포트+여유분 크기의 윈도우로 축소되고, 루프의 복제 그룹 생성·경계 점프 리포지션이 제거됨
- 루프 모드도 Insert/Remove 부분 재계산 지원 (복제 데이터가 사라져 가능해짐)
- 루프 MoveTo/MoveToIndex가 **최단거리 경로**로 이동 (wrap 경계 통과 허용)
- 루프 스크롤 가능 최소 조건 추가: `ContentSize >= ViewportSize + 최대 그룹 크기 + Spacing` 미만이면 경고 후 비루프로 폴백
- LoadData 호출 전에는 완전한 순정 ScrollRect로 동작 — 루프/페이징 설정이 로드 전 동작에 간섭하지 않음 (MovementType 강제·페이지 스냅·관성 억제 모두 로드 후에만 적용)
- 페이징이 `m_inertia` 직렬화 값을 영구 변경하던 동작 제거 — 설정 훼손 없이 파생 값(UseInertia)으로 억제

### 제거된 API (마이그레이션 표)
| 구 API | 신 API |
|---|---|
| `RealContentSize` / `ShowingContentSize` / `RealSize` / `ShowingSize` | `ContentSize` |
| `RealScrollSize` / `ShowingScrollSize` | `ScrollSize` |
| `RealScrollPosition` / `ShowingScrollPosition` | `ScrollPosition` (루프: [0, ContentSize) wrap) |
| `RealNormalizedScrollPosition` / `ShowingNormalizedScrollPosition` | `NormalizedScrollPosition` (루프: 1 초과 가능) |
| `ShowingPageCount` | `PageCount` |
| `ConvertRealToShow` / `ConvertShowToReal` | 삭제 (좌표계가 하나) |
| `LoadParam_NormalScrollPos_Showing` / `LoadParam_DenormalScrollPos_Showing` | 무접미 버전으로 통합 |
| `RecycleScrollbar.OnLoopValueChanged(real, showing)` | `OnValueChanged(float)`로 통합 |
| `IRecycleScrollbarDelegate`의 `LoopScrollIsOn`/`RealSize`/`ShowingSize`/`Convert*` | `IsLoopScrollable`/`ContentSize`/`ViewportSize`만 유지 |

### 마이그레이션 주의
- LoadData 이후 `Content.anchoredPosition` 직접 조작은 더 이상 지원되지 않음 (다음 렌더에서 덮어씀). `ScrollPosition`/`NormalizedScrollPosition`을 사용할 것
- 루프 모드의 주축 패딩은 `spacing/2` 정책이 실제로 적용됨 (이음새 간격 = spacing)

### Changed
- **스크롤바 RectTransform 점유 완화 (순정 parity)** — 핸들은 Anchors만 점유(마진 sizeDelta/anchoredPosition 조정 가능), Sliding Area는 점유하지 않음, 스크롤러의 스크롤바 루트 주축 점유는 `AutoHideAndExpandViewport`일 때만. 루프 서브 핸들은 메인 핸들의 마진을 자동 복사
  - 마이그레이션: 커스텀 스크롤바는 Sliding Area anchors를 full-stretch (0,0)-(1,1)로 설정 필요 (주축 크기 0이면 1회 경고 로그로 안내). 비-Expand 모드에서 스크롤바 루트가 직렬화 값으로 복원되므로 배치 확인 권장. 패키지 프리팹은 수정 완료
- `LoadData`에 위치 파라미터가 없으면 **항상 기존 스크롤 위치 유지**로 통일 — 기존에는 "파라미터 0개면 유지, 다른 파라미터만 있으면 맨 앞 리셋"으로 비일관적이었음. 맨 앞 이동이 필요하면 `LoadParam_NormalScrollPos(0)`을 명시
- 스크롤바 트랙 클릭 반복 속도를 프레임 기준 → 시간 기준으로 변경 — `Click Repeat Interval`(기본 0.01초, unscaled)로 조절 가능, 0이면 1.x 동작(매 프레임)
- 루프 wrap 상태의 트랙 클릭 방향을 원형 최단 경로 기준으로 개선, 서브 핸들 클릭은 핸들 클릭으로 취급
- `UpdateCellView()` 매 프레임 `m_reverse`/`ScrollAxis` 반복 분기 제거 — LoadData 시점 캐싱 및 진입부 로컬 변수 1회 정리
- 페이지 이동(`NextRealPageIndex`, `PrevRealPageIndex`)의 루프 판정을 `LoopScrollIsOn`(Inspector 설정값)에서 `IsLoopScrollable`(런타임 루프 가능 여부)로 변경 — 콘텐츠가 뷰포트보다 작으면 루프 설정이 켜져 있어도 페이지가 순환하지 않음
- 가시 그룹 탐색을 이진 탐색 기반으로 개선 (`FindVisibleGroupIndices`)
- MoveToIndex/JumpToIndex 계열은 데이터 인덱스 기준으로 통일 — reverse에서도 해당 데이터 셀의 시각 위치로 정확히 이동. 시각(배치) 순서 기준 이동은 신규 `VisualIndexToDataIndex(int)` 변환 후 호출
- reverse 모드의 그룹/셀 활성화 순회가 실제로 동작하도록 수정 (기존 코드는 역방향 조건이 성립하지 않아 활성화가 누락됨)

### Fixed
- `JumpToPage`/`MoveToPage` 계열이 실좌표를 normalized로 해석하고 뷰포트 피벗 보정을 누락해 엉뚱한 페이지로 이동하던 버그 — 페이지 스냅과 동일 공식으로 통일
- 스크롤바 트랙 클릭(드래그 없이 눌렀다 뗌) 후 페이지 스냅이 동작하지 않던 문제 — 클릭 해제도 드래그 종료와 동일하게 처리
- 셀 활성화 시 `GetCellRect` 선언 크기를 셀 RectTransform에 반영 (`UpdateCellSize`) — 가변 크기 셀이 시각적으로도 올바른 크기로 표시됨 (기존에는 배치 계산에만 사용되고 렉트는 프리팹 크기 유지)
- reverse에서 페이지/그룹 파티션이 데이터 순서 기준이라 시각 첫 페이지·그룹이 나머지 조각이 되던 문제 — 시각 순서 기준으로 재구성 (나머지는 스크롤 끝 배치)
- Elastic + 페이징에서 경계 밖 드래그 해제 시 스프링 복원과 페이지 스냅이 충돌해 버벅이던 문제 — 이징 중 경계 물리 양보로 스냅 우선
- 마지막 페이지 셀 부족 시 마그넷이 끝 페이지에 도달하지 못하던 문제 — 최근접 페이지 판정을 '착지 지점' 기준으로 변경 (비루프는 꼬리 페이지가 스크롤 끝 정렬로 수렴, 루프는 원형 최근접 유지)
- `RecalculateForInsert` 부분 재계산 버그 4건 (AddToEnd 예외, 중복 키, 페이지 잘림, 콘텐트 사이즈 재구성 오류)
- 빈 데이터에서 `MoveToIndex` 호출 시 예외 가드
- `MoveTo` 계열의 ScrollSize 0 나누기 제거 (구조적으로 소멸)

## [1.5.0] - 2026-03-18

### Changed
- Viewport RectTransform 점유 조건을 Unity ScrollRect와 동일하게 변경 — `AutoHideAndExpandViewport`일 때만 점유
- Expand 조건 `!= AutoHide` → `== AutoHideAndExpandViewport`로 변경
- 보조축 스크롤바 expand 판정 명시적 분리 (시스템 비활성화 고려)
- Permanent 모드에서 Viewport 자동 축소 제거 (Unity ScrollRect 동일 동작)

### Documentation
- `.reference/NEXT_TASKS.md` 작업 목록 재구성 및 상세 문서 분리

## [1.4.0] - 2026-03-14

### Fixed
- `RecycleScroller_OnValidate.cs`의 `Start()` → `protected override Start()` 수정 — UIBehaviour 상속 후 override 누락

### Changed
- `RSCellRect`, `CellSizeVector`의 `Width` 관련 멤버를 `CrossAxisSize`로 리네이밍 — 보조축 크기 의미 명확화
- 확장 메서드 `Vector2.Width(eScrollAxis)` → `Vector2.CrossAxisSize(eScrollAxis)` 리네이밍

### Documentation
- `.guides/VERIFICATION_ITEMS.md`에 asmdef 어셈블리 참조 검증 항목 추가

## [1.3.1] - 2026-03-13

### Fixed
- `RecycleScroller.Runtime.asmdef`에 `Unity.ugui` 어셈블리 참조 추가 — 외부 프로젝트에서 MonoScript 타입 매핑 실패로 컴포넌트가 missing script로 표시되는 문제 수정

## [1.3.0] - 2026-03-13

### Breaking Changes
- `[RequireComponent(typeof(ScrollRect))]` 제거 — ScrollRect 컴포넌트 의존성 완전 제거
- `RecycleScrollerContentEditor.cs` 삭제 — Content RectTransform 커스텀 에디터를 `DrivenRectTransformTracker`로 대체
- `_ScrollRect` 프로퍼티 삭제 — ScrollRect 직접 접근 불가
- Content 앵커 방식 변경: 단일 점 앵커 → 스트레치 앵커 (Vertical: 상단 가로 스트레치, Horizontal: 좌측 세로 스트레치)
- 프리팹의 Viewport/Content 참조 재할당 필요 (ScrollRect에서 RecycleScroller로 이전)

### Added
- ScrollRect 소스 코드 포크 (`RecycleScroller_ScrollRect.cs`) — 드래그/관성/탄성/Bounds/normalizedPosition/Canvas/Layout 시스템 내장
- `UIBehaviour` 상속 — `OnRectTransformDimensionsChange`, `IsActive()` 등 UI 라이프사이클 지원
- `ICanvasElement`, `ILayoutElement`, `ILayoutGroup` 인터페이스 구현
- `IDragHandler`, `IInitializePotentialDragHandler`, `IScrollHandler` 인터페이스 추가
- `[ExecuteAlways]` 어트리뷰트 — 에디트 모드 레이아웃 리빌드 지원
- `onValueChanged` 이벤트 (`ScrollerEvent : UnityEvent<Vector2>`) — normalizedPosition 콜백
- `m_useScrollbar` 토글 — 스크롤바 사용/미사용 전환 (플레이 중 변경 가능)
- 듀얼 스크롤바 필드 (`m_verticalScrollbar`, `m_horizontalScrollbar`) 및 `MainAxisScrollbar`/`CrossAxisScrollbar` 프로퍼티
- `ScrollbarVisibility` 3모드 지원 (Permanent / AutoHide / AutoHideAndExpandViewport)
- `DrivenRectTransformTracker`로 Viewport, Scrollbar, Content의 RectTransform 점유 ("Some values driven by RecycleScroller." 표시)
- `OverwriteValue<MovementType>` — 루프 스크롤 시 MovementType 강제 Unrestricted
- ScrollRect Settings 필드 플레이 중 편집 가능 (MovementType, Elasticity, Inertia, DecelerationRate, ScrollSensitivity)
- 에디트 모드 스크롤바 크기/가시성 실시간 갱신

### Fixed
- `CollectionUtils.FindClosestIndex` 빈 컬렉션 접근 시 `InvalidOperationException` 방지 (`First()` → `FirstOrDefault()`)

### Changed
- `MonoBehaviour` → `UIBehaviour` 상속 변경
- `m_scrollbarRef` → `m_verticalScrollbar` (`[FormerlySerializedAs]` 적용)
- `OnScrollRectScrolling` → `OnScrollPositionChanged` 리네이밍 (`[Obsolete]` 포워딩 유지)
- `ResetContent_Pivot()`, `ResetContent_Anchor()` 접근 제한자 `public` → `private`
- `ResetContent_Size()` 보조축 sizeDelta: Viewport.rect 값 → 0 (스트레치 앵커가 자동 처리)
- `ResetSpaceCellsWidth()`: `Content.sizeDelta` → `Content.rect` (스트레치 모드 대응)
- `ScrollPagingConfigDrawer` HelpBox 텍스트에서 "ScrollRect의" 참조 제거

## [1.2.3] - 2026-03-11

### Fixed
- Vertical 스크롤바 프리팹 방향값 및 직렬화 필드명 수정

### Changed
- Android, iPhone 플랫폼 applicationIdentifier 추가

### Documentation
- NEXT_TASKS 업데이트 — Task 5, 6 추가 및 Task 3 보류 처리

## [1.2.2] - 2026-02-19

### Fixed
- Addressables 어셈블리 참조 누락으로 인한 컴파일 에러 수정

### Changed
- Addressables 패키지 의존성 추가

## [1.2.1] - 2026-02-18

### Changed
- Editor/VersionCheck 누락 메타 파일 추가
- 슬래시 명령어의 AGENTS.md 참조를 CLAUDE.md로 변경
- CLAUDE.md, AGENTS.md 경량화 및 슬래시 명령어 강화

### Documentation
- 문서의 AGENTS.md 참조를 CLAUDE.md로 변경 및 참조 순서 업데이트
- Task 3 사전 분석 결과 문서화 (객체화 후보 패턴 정리)

## [1.2.0] - 2026-02-14

### Added
- Elastic 핸들 사이즈 조정 동작 구현 (비루프: 오버슈트 시 핸들 축소, 루프: anchor 기반 서브 핸들 사이즈 전환)
- ExtraTransitionEntry 독립 트랜지션 시스템 추가 (스크롤바 핸들 개별 ColorTint/SpriteSwap 설정)
- 에디터 시작 시 패키지 버전 체크 팝업 기능 추가 (GitHub API 기반)

### Fixed
- Set 메서드 그룹의 Action<float> 변환 오류 수정
- 테스트 스크립트 eEase enum 참조 누락 수정

### Changed
- 루프/비루프 분기를 Strategy Pattern(IScrollerMode, IScrollbarMode)으로 객체화
- RecycleScroller/Scrollbar 에디터를 계층적 폴드아웃 UI로 재구성
- 에디터 드로어 공통 코드를 EditorDrawerHelper로 추출
- private 필드 명명 규칙(m_ 접두사) 및 enum 명명 규칙(e 접두사) 정리

### Documentation
- 예정 작업 목록 정리 및 완료된 참조 코드 파일 삭제

## [1.1.0] - 2026-02-14

### Added
- RecycleScrollbar 핸들 최소 사이즈 보장 기능 추가
- RecycleScroller/RecycleScrollbar 통합 테스트 추가

### Fixed
- 루프 스크롤바 핸들 위치 계산 방식 재설계 (wrap 경계 점프 해결)

### Changed
- LoopScrollbar → RecycleScrollbar 리네임 및 프리팹 설정
- Recycle Scroll View 프리팹 설정 보완
- 테스트 스크립트 불필요 로깅 제거 및 씬 설정 업데이트

### Documentation
- CLAUDE.md 프로젝트 구조를 UPM 패키지 기준으로 업데이트
- README 설치 섹션에 UPM Git URL 추가
- PROJECT_SUMMARY 파일 구조를 UPM 패키지 경로로 업데이트
- Unity ScrollRect/Scrollbar 소스 코드 참조 파일 추가

## [1.0.0] - 2026-02-12

### Added
- RecycleScroller 핵심 스크롤러 (Object Pooling 기반)
- 무한 루프 스크롤 지원 (RecycleScrollbar)
- 페이지네이션 기능
- 셀 그룹 배치 (LoadDataExtension)
- 비동기 데이터 로드 (UniTask 기반)
- Addressables 기반 비동기 셀 프리팹 로더 (조건부 컴파일)
- 이징 애니메이션 함수
- 커스텀 에디터 인스펙터
- 스크롤뷰 생성 도구 (GameObject > UI > RecycleScrollView)
- SerializableDictionary 유틸리티
- 커스텀 속성 드로어 (ColoredHeader, HelpBox, HorizontalLine 등)
