# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
