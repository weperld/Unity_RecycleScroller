# 보고서 생성

> 작업 완료 후 보고서를 생성하는 시스템입니다.

---

## 구현 및 보고

### 완료 보고 형식

```
✅ 작업 완료!

[WorkID]: WIP-YYYYMMDD-NNN
[완료일]: 2025-02-02 16:30
[소요 시간]: 6.5시간

[수정/생성 파일]
- Services/ExportService.cs (수정: 20번 라인 null 체크 추가)
- ...

[테스트 결과]
- [x] 빌드 성공
- [x] 기능 테스트 통과

[커밋]
[fix] ExportService null 체크 추가

[다음 단계]
- WORK_IN_PROGRESS.md 업데이트 (완료 작업으로 이동)
- 필요 시 보고서 생성
```

---

> **참고**: 에이전트 지시 단축, 유형 판단 로직, 분석 예시는 [GATES.md](GATES.md) 및 [AGENTS.md](../AGENTS.md)를 참조하세요.

---

## 보고서 생성 (선택 사항)

### 보고서 명령어
```
보고서: WIP-YYYYMMDD-NNN
```

### 보고서 형식

#### JSON 형식 (WORK_HISTORY.json)
```json
{
  "completed_works": [
    {
      "workId": "WIP-20250202-001",
      "type": "수정",
      "title": "ExportService null 체크 추가",
      "startDate": "2025-02-02T10:00:00",
      "endDate": "2025-02-02T16:30:00",
      "duration": "6.5h",
      "files": [
        "Services/ExportService.cs"
      ],
      "commit": "abc123",
      "tags": ["fix", "data-integrity"]
    }
  ],
  "cancelled_works": [
    {
      "workId": "WIP-20250130-002",
      "type": "수정",
      "title": "ExportService 최적화",
      "startDate": "2025-01-30T14:00:00",
      "cancelledDate": "2025-01-30T15:30:00",
      "reason": "우선순위 조정",
      "duration": "1.5h"
    }
  ]
}
```

#### 마크다운 형식 (reports/WORK_REPORT_WIP-YYYYMMDD-NNN.md)
```markdown
# 작업 보고서

## 작업 정보
- **WorkID**: WIP-20250202-001
- **유형**: 기능 수정
- **제목**: ExportService null 체크 추가

## 기간
- **시작**: 2025-02-02 10:00
- **완료**: 2025-02-02 16:30
- **소요 시간**: 6.5시간

## 계획 요약
- [x] 기획서 분석
- [x] 계획 수립
- [x] 사용자 확인
- [x] 구현
- [x] 테스트
- [x] 커밋

## 구현 내용
- Services/ExportService.cs
  - 20번 라인: null 체크 추가
  - 45번 라인: 예외 메시지 개선

## 변경 파일
- Services/ExportService.cs

## 커밋
- [fix] ExportService null 체크 추가
- Hash: abc123def456

## 테스트 결과
- [x] 단위 테스트 통과
- [x] 빌드 성공
- [x] 기능 테스트 통과

## 노트
- 데이터 무결성 원칙 준수
- 예외 발생 시 명확한 메시지 제공
```

### 내보내기 명령어
```
내보내기: json
→ WORK_HISTORY.json 업데이트

내보내기: markdown
→ reports/WORK_REPORT_WIP-YYYYMMDD-NNN.md 생성
```

---

## 📚 관련 모듈

- [PIPELINE.md](PIPELINE.md) - 개발 파이프라인
- [GATES.md](GATES.md) - Gate 검증 시스템
- [AUTO_UPDATE.md](AUTO_UPDATE.md) - WorkID 및 자동 업데이트 시스템
- [ERROR_HANDLING.md](ERROR_HANDLING.md) - 에러 처리 및 롤백 프로토콜
