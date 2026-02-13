# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
