# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
