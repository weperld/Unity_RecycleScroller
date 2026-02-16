# 에이전트 작업 가이드

> 슬래시 명령어가 필요 시 참조하는 레퍼런스 문서입니다.
> 상시 로드 대상이 아니며, 명령어 파일에서 특정 섹션을 참조합니다.

---

## 🔒 절대 규칙 (모든 에이전트 공통)

> **중요**: 이 절대 규칙은 모든 에이전트(coordinator, analyst, architect, developer, reviewer, doc-manager, tester)가 반드시 99% 이상 준수해야 합니다.
>
> **위반 시**: 작업 중단, 사용자 알림, 재시도 요청

### Hard Blocks (절대 위반 금지)

| 규칙 | 위반 시 조치 |
|------|------------|
| 무조건 캐스팅 (Type)cast 남용 금지, as 연산자 + null 체크 사용 권장, Generic 타입 제약 활용 |
| **빈 catch 블록 금지** | `catch(e) {}` 빈 블록 금지 |
| **테스트 삭제 금지** | 실패한 테스트를 삭제하여 "통과"로 만드는 행위 금지 |
| **커밋 없이 파일 수정 금지** | 사용자의 명시적 요청 없이 커밋 금지 |
| **추측 금지** | 코드를 읽지 않고 추측해서 작업 금지 |
| **코드 파괴 금지** | 개발 파이프라인의 지침을 파괴하는 행위 금지 |

> **참고**: 현재 프로젝트는 C#, Unity 2022.3.62f2, UGUI, UniTask 기반입니다

### Anti-Patterns (BLOCKING violations)

| 범주 | 금지 행동 | 예시 |
|------|----------|------|
| (Type)cast 남용, GetComponent 반복 호출 (캐싱 필수), string 비교 대신 enum 사용, Update()에서 GC 유발 코드, Find/FindObjectOfType 런타임 사용 |
| **Error Handling** | 빈 catch 블록 | `catch(e) {}` |
| **Testing** | 테스트 삭제 | 실패한 테스트 삭제 |
| **Git** | 무단 커밋 | 커밋 명령 실행 |

---

### 에이전트별 금지 행동

> **기본 원칙**: 모든 에이전트는 절대 규칙(Hard Blocks)을 위반하는 행동을 금지함

#### coordinator (코디네이터)

**금지 행동**:
1. **모호한 지시 전달 금지**: "architect Design 진행해줘" 처럼 구체적 정보 없이 위임
2. **WorkID 누락 금지**: 작업 시작 시 WorkID 생성 누락
3. **WORK_IN_PROGRESS.md 업데이트 누락 금지**: 단계 완료 시 업데이트 누락
4. **절대 규칙 위반 통과 금지**: 하위 에이전트의 절대 규칙 위반을 통과시키는 행위

---

#### analyst (분석가)

**금지 행동**:
1. **추측해서 작업 금지**: 기획서 내용을 추측해서 분석
2. **문서 읽기 생략 금지**: 필수 문서(PROJECT_SUMMARY.md, CODE_STYLE.md 등) 읽기 생략
3. **모호한 요청 그대로 진행 금지**: 사용자의 모호한 요청을 그대로 작업에 반영

---

#### architect (아키텍트)

**금지 행동**:
1. **데이터 무결성 위반 설계 금지**: 기본값 대신 예외 발생 설계 누락
2. - MonoBehaviour 생명주기 준수 (Awake -> OnEnable -> Start)
- ScrollRect 이벤트 핸들링 시 무한 루프 방지
- Object Pool 해제 시 참조 정리 필수
- 에디터 코드와 런타임 코드 엄격 분리 (#if UNITY_EDITOR)
3. **기술 규칙 위반 설계 금지**: TECHNICAL_RULES.md 위반 설계

---

#### developer (개발자)

**금지 행동**:
1. **데이터 무결성 위반 금지**: 기본값 대신 예외 발생
2. - GetComponent 결과 캐싱 필수
- 불필요한 GC Allocation 최소화 (string 연결, LINQ 등)
- null 체크 후 안전한 접근
- SerializeField로 인스펙터 노출, public 필드 최소화
- partial class 패턴으로 관심사 분리

---

#### tester (테스터)

**금지 행동**:
1. **버그 은폐 금지**: 발견한 버그를 보고하지 않고 은폐
2. **테스트 결과 왜곡 금지**: 실패를 성공으로 보고

---

#### reviewer (리뷰어)

**금지 행동**:
1. **절대 규칙(Hard Blocks) 위반 코드 통과 금지**: 모든 절대 규칙을 위반한 코드 리뷰 통과
   - 타입 에러 억제
   - 빈 catch 블록
   - 테스트 삭제
   - 커밋 없이 파일 수정
   - 추측 작업
   - 코드 파괴

---

#### doc-manager (문서 관리자)

**금지 행동**:
1. **WORK_IN_PROGRESS.md 업데이트 누락 금지**: 단계 완료 시 업데이트 누락
2. **버그 은폐 문서화 금지**: 발견한 버그를 문서에서 은폐
3. **테스트 결과 왜곡 문서화 금지**: 실패를 성공으로 보고하는 문서 작성

---

### Self-Validation Checklist (작업 완료 전 필수)

모든 에이전트는 작업 완료 전 다음 체크리스트를 반드시 수행해야 합니다:

> **강제 사항**: Self-Validation Checklist 수행 결과를 WORK_IN_PROGRESS.md에 기록하고,
> 기록되지 않은 경우 Cross-Stage Review Chain이 수행되지 않습니다.

```markdown
## 🔒 작업 완료 전 필수 체크리스트

- [ ] 무조건 캐스팅 (Type)cast 남용하지 않음
- [ ] GetComponent 호출 결과를 캐싱함
- [ ] Update/LateUpdate에서 GC 유발 코드 없음
- [ ] 빈 catch 블록이 없음
- [ ] 에디터 코드가 런타임에 포함되지 않음 (#if UNITY_EDITOR)
- [ ] Object Pool 사용 시 참조 정리 완료

### Error Handling
- [ ] 빈 catch 블록 없음

### Testing
- [ ] 테스트 삭제하지 않음

### Git
- [ ] 커밋 없이 파일 수정하지 않음 (사용자 요청 전)

### Integrity
- [ ] 단언 없이 추측하지 않음

### Pipeline
- [ ] 모든 위반이 없음을 확인함:
  - [ ] Type Safety 위반 없음
  - [ ] Error Handling 위반 없음
  - [ ] Testing 위반 없음
  - [ ] Git 위반 없음
- [ ] 개발 파이프라인 지침을 파괴하지 않음:
  - [ ] WORK_IN_PROGRESS.md 업데이트 완료
  - [ ] WorkID 생성 누락 없음
  - [ ] 크로스체크 통과 완료
```

**위반 시 조치**:
1. 즉시 작업 중단
2. 사용자에게 위반 내용 보고
3. 수정 후 재시도

---

## 🔒 Cross-Stage Review Chain (강제 검증)

각 단계에서 다음 단계로 넘어갈 때 **Gate 크로스체크 에이전트**가 강제 검증을 수행합니다:

| 단계 전이 | 크로스체크 담당 | 검증 내용 |
|---------|--------------|----------|
| Plan → Design | architect | Gate-1 크로스체크 검증 |
| Design → Code | developer | Gate-2 크로스체크 검증 |
| Code → Test | tester | Gate-3 크로스체크 검증 |
| Test → Docs | developer | Gate-4 크로스체크 검증 |
| Docs → QA | reviewer | Gate-5 크로스체크 검증 |
| QA → Review | architect | Gate-6 크로스체크 검증 |

> **중요**: 크로스체크 담당 에이전트는 Gate 시스템에서 정의되며, 각 단계의 전문성에 맞는 에이전트가 배정됩니다.
> 상세 게이트 통과 조건은 `WORKFLOW_PLANNING/GATES.md`를 참조하세요.

**절대 규칙 검증 포함**:
- 크로스체크 에이전트는 각 단계에서 **절대 규칙 준수 여부**를 반드시 검증해야 합니다.
- 절대 규칙 위반 시 **즉시 해당 단계로 롤백**하고 **수정 요청**해야 합니다.

> **강제 사항**: 각 단계 전이 시 크로스체크 에이전트가 검증을 완료하고 WORK_IN_PROGRESS.md에
> "크로스체크 통과 완료"를 기록하지 않은 경우, 다음 단계로 넘어갈 수 없습니다.

---

## 📢 사용자 피드백 시스템

에이전트가 작업 완료 후 사용자에게 피드백을 요청합니다:

### 피드백 요청 프로세스

#### 1단계: 에이전트 작업 완료
- 에이전트가 자신의 작업 완료
- 작업 결과 요약 생성
- 생성/수정 파일 목록 작성

#### 2단계: Self-Validation Checklist 수행
- Type Safety 체크
- Error Handling 체크
- Testing 체크
- Git 체크
- Integrity 체크
- Pipeline 체크
- 체크리스트 결과를 WORK_IN_PROGRESS.md에 기록

#### 3단계: 사용자 피드백 요청 메시지 생성
- 에이전트: @agent_name
- 작업: task_description
- 작업 결과: result_summary
- Self-Validation Checklist 수행 결과 포함

#### 4단계: 사용자 피드백 제공
- 사용자가 피드백 항목 확인
- "결과 평가" 선택:
  - 규칙 위반함
  - 규칙 위반하지 않음, 결과 문제 있음
  - 규칙 위반하지 않음, 결과 만족
- "결과 문제 있는 경우" 선택 시 상세 기재

#### 5단계: 피드백 처리
- "규칙 위반함" 선택: 즉시 작업 중단, 수정 후 재시도
- "규칙 위반하지 않음, 결과 문제 있음" 선택: 작업 중단, 수정 후 재시도
  - 사용자가 "검증 항목 추가"를 선택한 경우: `.guides/VERIFICATION_ITEMS.md`에 해당 항목 추가 후 수정 진행
- "규칙 위반하지 않음, 결과 만족" 선택: 다음 단계 진행

#### 6단계: Cross-Stage Review Chain 검증
- reviewer가 검증 수행
- 검증 결과를 WORK_IN_PROGRESS.md에 기록
- 다음 단계로 진행

---

## 📌 지시 형식 필수 원칙

### 에이전트 지시 규칙 (코디네이터 & 사용자 공통)

**필수 원칙**:
작업 지시 시 **반드시 구체적인 지시 문서** 또는 **구체적인 설명**을 포함해야 합니다.

### 금지 사항
- "Design 진행해줘"
- "이거 구현해줘"
- 아무 내용 없이 모호한 지시를 하는 경우

### 지시 필수 구성 요소

**형식 1: 문서 기반 지시** (권장)
```
작업에 대한 구체적 지시 사항이 담긴 문서를 작성했습니다.
문서: ./docs/instructions/design-001.md
해당 문서를 참고하여 작업을 진행해주세요.
```

**형식 2: 구체적 설명 포함 지시**
```
[기능명] 데이터 추출 기능에 대한 아키텍처 설계를 진행해주세요.

[작업 대상]
- 새로운 서비스 클래스 설계
- 파싱 로직 구조

[작업 범위]
- 파일 로드, 파싱, 데이터 매핑 처리
- 기존 추출 로직과의 통합 고려

[요구사항]
- 비동기 처리 필수
- 에러 처리 완벽히 구현
- 기존 패턴 준수

[참고 자료]
- 기존 서비스 코드 참조
- 현재 추출 로직 참조

[기대 결과]
- 아키텍처 다이어그램
- 클래스 구조 정의서
- 인터페이스 명세
```

### 사용자 지시 부족 시 대응
사용자가 "Design 진행해줘"처럼 애매하게 지시한 경우:
- **즉시 사용자에게 구체적 정보를 요구**해야 함
- "어떤 기능에 대한 디자인인지, 작업 범위, 요구사항 등 구체적인 정보를 알려주세요"
- 추측해서 작업하지 말고 반드시 정보 요구

---

## 📁 WIP 템플릿 및 폴더 구조

### 전체 폴더 구조
```
.wips/
├── templates/           # 템플릿 파일 (읽기 전용)
│   ├── WIP-Plan-YYYYMMDD-NNN.md
│   ├── WIP-Design-YYYYMMDD-NNN.md
│   ├── WIP-Code-YYYYMMDD-NNN.md
│   ├── WIP-Test-YYYYMMDD-NNN.md
│   ├── WIP-Docs-YYYYMMDD-NNN.md
│   └── WIP-QA-YYYYMMDD-NNN.md
└── active/              # 독립 WIP 작성 폴더 (쓰기 전용)
    ├── Plan/
    ├── Design/
    ├── Code/
    ├── Test/
    ├── Docs/
    └── QA/
```

### 에이전트별 매핑 테이블

| 에이전트 | 담당 스테이지 | 템플릿 파일 | 작성 폴더 | 템플릿 경로 | 작성 경로 |
|---------|-------------|-----------|-----------|-----------|----------|
| **analyst** | Plan | `WIP-Plan-YYYYMMDD-NNN.md` | `.wips/active/Plan/` | `.wips/templates/WIP-Plan-YYYYMMDD-NNN.md` | `.wips/active/Plan/WIP-Plan-YYYYMMDD-NNN.md` |
| **architect** | Design | `WIP-Design-YYYYMMDD-NNN.md` | `.wips/active/Design/` | `.wips/templates/WIP-Design-YYYYMMDD-NNN.md` | `.wips/active/Design/WIP-Design-YYYYMMDD-NNN.md` |
| **developer** | Code | `WIP-Code-YYYYMMDD-NNN.md` | `.wips/active/Code/` | `.wips/templates/WIP-Code-YYYYMMDD-NNN.md` | `.wips/active/Code/WIP-Code-YYYYMMDD-NNN.md` |
| **tester** | Test | `WIP-Test-YYYYMMDD-NNN.md` | `.wips/active/Test/` | `.wips/templates/WIP-Test-YYYYMMDD-NNN.md` | `.wips/active/Test/WIP-Test-YYYYMMDD-NNN.md` |
| **doc-manager** | Docs | `WIP-Docs-YYYYMMDD-NNN.md` | `.wips/active/Docs/` | `.wips/templates/WIP-Docs-YYYYMMDD-NNN.md` | `.wips/active/Docs/WIP-Docs-YYYYMMDD-NNN.md` |
| **reviewer** | QA | `WIP-QA-YYYYMMDD-NNN.md` | `.wips/active/QA/` | `.wips/templates/WIP-QA-YYYYMMDD-NNN.md` | `.wips/active/QA/WIP-QA-YYYYMMDD-NNN.md` |
| **coordinator** | Review | (전체 관리) | (해당 없음) | - | - |

### 절대 규칙

1. **템플릿은 읽기 전용**: `.wips/templates/` 폴더의 파일은 절대 수정하지 마세요
2. **독립 WIP는 복사 후 작성**: 템플릿 내용을 복사한 후, 실제 정보로 수정하여 작성
3. **폴더 분리**: 각 스테이지의 독립 WIP는 `.wips/active/{Stage}/` 폴더에만 작성하세요
4. **보존**: 작업 완료/취소 후 독립 WIP 파일은 `.wips/archive/{Stage}/` 폴더로 이동하여 보존하세요 (히스토리용)

---

## 📝 WIP 필수 변경 항목

### 파일명 규칙

**템플릿 파일 형식**: `WIP-{Stage}-YYYYMMDD-NNN.md`
- `{Stage}`: 스테이지 이름 (Plan, Design, Code, Test, Docs, QA)
- `YYYYMMDD`: 날짜 플레이스홀더
- `NNN`: 순번 플레이스홀더 (3자리 0패딩)

**독립 WIP 파일 형식**: `WIP-{Stage}-{YYYYMMDD}-{NNN}.md`
- 예: `WIP-Plan-20250208-001.md`

### 1. 기본 정보 섹션

| 항목 | 템플릿 내용 | 변경 대상 | 예시 |
|------|-----------|----------|------|
| WorkID | `WIP-YYYYMMDD-NNN` | 실제 WorkID | `WIP-20250208-001` |
| 생성일 | `YYYY-MM-DD` | 실제 날짜 | `2025-02-08` |
| 상태 | `준비 / 진행 중 / 완료 / 취소` | 작업 상태 | `진행 중` |

> **참고**: WorkID 생성은 [WORKFLOW_PLANNING/INDEX.md](./WORKFLOW_PLANNING/INDEX.md)에 상세히 설명되어 있습니다.

### 2. 지시 정보 섹션

| 섹션 | 템플릿 내용 | 변경 대상 |
|------|-----------|----------|
| 작업 대상 (Target) | `(에이전트가 코디네이터/사용자로부터 받은 지시 내용 기반)` | 실제 작업 대상 내용 |
| 작업 범위 (Scope) | `(에이전트가 코디네이터/사용자로부터 받은 지시 내용 기반)` | 실제 작업 범위 내용 |
| 요구사항 (Requirements) | `(에이전트가 코디네이터/사용자로부터 받은 지시 내용 기반)` | 실제 요구사항 내용 |
| 참고 자료/링크 (References) | `(에이전트가 코디네이터/사용자로부터 받은 지시 내용 기반)` | 실제 참고 자료/링크 |
| 기대 결과 (Expected Outcome) | `(에이전트가 코디네이터/사용자로부터 받은 지시 내용 기반)` | 실제 기대 결과 내용 |

### 3. 크로스체크 결과 섹션

| 항목 | 템플릿 내용 | 변경 대상 | 예시 |
|------|-----------|----------|------|
| 요청일 | `YYYY-MM-DD` | 실제 요청일 | `2025-02-08` |
| 응답일 | `YYYY-MM-DD` | 실제 응답일 | `2025-02-08` |
| 결과 | `통과 / 수정 요청` | 실제 결과 | `통과` |
| 피드백 | `(내용)` | 실제 피드백 내용 | `설계 적절함` |
| 조치 | `(수정 사항 및 재요청 여부)` | 실제 조치 내용 | `없음` |

### 4. 진척도 섹션

| 항목 | 템플릿 내용 | 변경 대상 | 예시 |
|------|-----------|----------|------|
| 진척도 | `0% / 25% / 50% / 75% / 100%` | 실제 진척도 | `50%` |
