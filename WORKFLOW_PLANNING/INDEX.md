# 워크플로우 계획 시스템

> 개발 파이프라인, Gate 검증, 작업 추적, 에러 처리를 위한 종합 시스템

## 모듈 구성

| 모듈 | 파일 | 설명 |
|------|------|------|
| 파이프라인 | [PIPELINE.md](PIPELINE.md) | 개발 파이프라인 및 실행 모드 |
| Gate 검증 | [GATES.md](GATES.md) | 단계별 검증 게이트 및 통과 조건 |
| 자동 업데이트 | [AUTO_UPDATE.md](AUTO_UPDATE.md) | WorkID, 상태 전이, 충돌 방지 |
| 에러 처리 | [ERROR_HANDLING.md](ERROR_HANDLING.md) | 에러 프로토콜, 롤백, 복구 |
| 보고서 | [REPORTS.md](REPORTS.md) | 작업 완료 보고서 생성 |

## 관련 문서

- [WORK_IN_PROGRESS.md](../WORK_IN_PROGRESS.md) - 작업 추적
- [AGENTS.md](../AGENTS.md) - 에이전트 규칙
