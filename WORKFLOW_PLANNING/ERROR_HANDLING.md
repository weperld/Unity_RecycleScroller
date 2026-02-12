# 에러 처리 및 롤백 프로토콜

> 검증 게이트 실패, 빌드 에러, 테스트 실패 등의 상황을 처리하는 프로토콜입니다.

---

## 검증 게이트 실패 시 처리 프로토콜

### Gate 실패 시 기본 흐름
```
[Gate 실패]
    ↓
[1차 수정 시도] 같은 단계에서 수정
    ↓
[성공?] 예 → 다시 Gate 통과 시도
    ↓
[실패?]
    ↓
[2차 수정 시도] 다른 방법으로 수정
    ↓
[성공?] 예 → 다시 Gate 통과 시도
    ↓
[실패?]
    ↓
[3차 수정 시도] 마지막 시도
    ↓
[성공?] 예 → 다시 Gate 통과 시도
    ↓
[실패?] → [이전 단계로 롤백]
```

### 게이트별 롤백 매핑

> **참고**: 아래 테이블은 full 프리셋(7단계) 기준입니다. lite/standard 프리셋에서는 게이트 번호와 롤백 대상이 다르며, `init.ps1`/`init.sh` 실행 시 프리셋에 맞게 동적 생성됩니다.

| 실패한 Gate | 롤백 단계 | 이유 |
|------------|-----------|------|
| Gate-1 | Plan → 재계획 | 설계 불가능한 계획 |
| Gate-2 | Design → Plan | 설계 해결 불가 |
| Gate-3 | Code → Design | 구현 해결 불가 |
| Gate-4 | Test → Code | 버그 수정 불가 |
| Gate-5 | Docs → Test | 문서화 해결 불가 |
| Gate-6 | QA → Code | 품질 문제 해결 불가 |

## 더블체크 실패 시 처리
```
[에이전트] 작업 완료
    ↓
[1차 더블체크] 자체 검증 1차
    ↓
[실패?] 예 → 수정 후 2차 더블체크
    ↓
[성공?] 예 → 크로스체크로 전달
    ↓
[크로스체크 에이전트] 독립적 검증
    ↓
[실패?] 예 → [원래 에이전트]에게 수정 요청
    ↓
[원래 에이전트] 수정 후 재제출
    ↓
[크로스체크 에이전트] 재검증 (최대 3번)
    ↓
[통과?] 예 → 다음 단계로 전달
```

---

## 단계별 에러 처리

### 1단계: Plan (계획) 에러

**에러 예시:**
- 기획서 파일을 찾을 수 없음
- 기획서 내용을 이해할 수 없음
- 유형 판단 실패 (애매한 내용)

**처리 프로세스:**
```markdown
analyst:
❌ Plan 단계 실패

[에러]
- 기획서 파일을 찾을 수 없음: ./docs/planning/feature_001.md

[대응]
1. WORK_IN_PROGRESS.md 업데이트: 에러 기록
2. 사용자에게 결정 요청
   옵션1: 기획서 파일 경로 수정
   옵션2: 기획서 내용을 직접 입력
   옵션3: 작업 취소
```

**WORK_IN_PROGRESS.md 업데이트:**
```markdown
### WIP-20250205-001: CSV 기능 추가

#### ❌ 에러 기록
[2025-02-05 10:15] Plan 단계 실패
에러: 기획서 파일을 찾을 수 없음
위치: ./docs/planning/feature_001.md
```

---

### 2단계: Code (코딩) 에러

**에러 예시:**
- 빌드 실패
- 파일 생성 실패
- 파일 쓰기 권한 없음

**처리 프로세스:**
```markdown
developer:
❌ Code 단계 실패

[에러]
- 빌드 실패: CS8602 NullReferenceException
위치: Services/Processors/CSVProcessor.cs:45

[대응]
1. WORK_IN_PROGRESS.md 업데이트: 에러 기록
2. 에러 원인 분석
3. 수정 시도
4. 3번 시도 후 실패 시 coordinator에게 보고
```

**WORK_IN_PROGRESS.md 업데이트:**
```markdown
#### ❌ 에러 기록
[2025-02-05 11:30] Code 단계 실패 (1차 시도)
에러: CS8602 NullReferenceException
위치: Services/Processors/CSVProcessor.cs:45
메시지: NullReferenceException: Object reference not set to an instance of an object.

[2025-02-05 11:35] Code 단계 실패 (2차 시도)
에러: CS8602 NullReferenceException
위치: Services/Processors/CSVProcessor.cs:45
메시지: 여전히 null 참조 발생

[2025-02-05 11:40] Code 단계 실패 (3차 시도)
에러: CS8602 NullReferenceException
위치: Services/Processors/CSVProcessor.cs:45
메시지: 3번 시도 후 실패 → coordinator에 보고
```

---

### 3단계: Test (테스트) 에러

**에러 예시:**
- 단위 테스트 실패
- 기능 테스트 실패
- 빌드 테스트 실패

**처리 프로세스:**
```markdown
tester:
❌ Test 단계 실패

[에러]
- 단위 테스트 실패: Test_ExportAsync_ValidFile_Success
예상: 성공
실제: NullReferenceException

[대응]
1. WORK_IN_PROGRESS.md 업데이트: 에러 기록
2. 버그 재현 단계 기록
3. developer에게 버그 수정 요청
4. developer 수정 후 reviewer에게 리뷰 요청
5. reviewer 리뷰 후 재테스트
```

**WORK_IN_PROGRESS.md 업데이트:**
```markdown
#### ❌ 에러 기록
[2025-02-05 12:00] Test 단계 실패
에러: 단위 테스트 실패
테스트: Test_ExportAsync_ValidFile_Success
예상: 성공
실제: NullReferenceException

[버그 재현 단계]
1. CSVProcessor.cs 생성 완료
2. 단위 테스트 실행: Test_ExportAsync_ValidFile_Success
3. NullReferenceException 발생

[다음 단계]
→ developer에게 버그 수정 요청
→ developer 수정 후 reviewer 리뷰
→ reviewer 리뷰 후 재테스트
```

---

### 4단계: Docs (문서화) 에러

**에러 예시:**
- WORK_IN_PROGRESS.md 쓰기 실패
- WORK_HISTORY.json 업데이트 실패
- 보고서 생성 실패

**처리 프로세스:**
```markdown
doc-manager:
❌ Docs 단계 실패

[에러]
- WORK_IN_PROGRESS.md 쓰기 실패: 파일 잠김

[대응]
1. WORK_IN_PROGRESS.md 쓰기 재시도 (최대 3번)
2. 3번 시도 후 실패 시 coordinator에게 보고
3. 사용자에게 수동 업데이트 요청
```

---

### 5단계: QA (품질검사) 에러

**에러 예시:**
- 코드 스타일 위반
- 아키텍처 위반
- 잠재적 버그 발견

**처리 프로세스:**
```markdown
reviewer:
❌ QA 단계 실패

[에러]
- 코드 스타일 위반: _camelCase 대신 camelCase 사용
위치: ViewModels/CSVExecutionViewModel.cs:20

[대응]
1. WORK_IN_PROGRESS.md 업데이트: 에러 기록
2. developer에게 수정 요청
3. developer 수정 후 재리뷰
4. 재리뷰 통과 시 다음 단계
```

---

## 롤백 프로세스

### 롤백 시 상태 복구 메커니즘

#### 상태 스냅샷 저장
```markdown
## 상태 스냅샷 저장 규칙

### Gate 통과 시 스냅샷 저장
각 Gate 통과 시 WORK_IN_PROGRESS.md에 상태 스냅샷 자동 저장:

```markdown
#### 📸 상태 스냅샷
[Gate-1 통과 시점: 2025-02-07 15:30]
- 완료 단계: Plan ✅
- Gate 상태: Gate-1 통과 ✅
- 다음 단계: Design
- 진척도: 10%
```

### Gate 진입 전 상태 백업
Gate 진입 전 현재 상태를 백업 섹션에 저장:

```markdown
#### 💾 Gate 진입 전 백업
[Gate-1 진입 전: 2025-02-07 15:00]
- 완료 단계: (이전 상태)
- 계획 요약: (이전 계획)
- 진척도: (이전 진척도)
```
```

#### 롤백 시 상태 복구
```markdown
## 롤백 시 상태 복구 절차

### 1. 백업 확인
```
1. WORK_IN_PROGRESS.md에서 Gate 진입 전 백업 섹션 확인
2. 스냅샷 섹션 확인
3. 롤백 대상 단계 확인
```

### 2. 상태 복구 수행
```
1. Gate 실패 원인 분석
2. 백업 상태 확인
3. 스냅샷 상태로 복구
   - 완료 단계 롤백
   - Gate 상태 초기화
   - 진척도 조정
4. WORK_IN_PROGRESS.md 업데이트
```

### 3. 복구 완료 확인
```
1. WORK_IN_PROGRESS.md 상태 확인
2. 다음 단계 에이전트에게 알림
3. 진행 계속
```

### 상태 복구 예시
```
Gate-2 실패 (3번 시도 후) → Design 단계로 롤백

1. 백업 확인:
   - [Gate-1 진입 전 백업: 2025-02-07 15:00]
     - 완료 단계: (없음)
     - 계획 요약: CSV 기능 추가
     - 진척도: 0%

2. 상태 복구:
   - 완료 단계: Plan ✅
   - Gate 상태: Gate-1 통과 ✅, Gate-2 실패 ❌
   - 진척도: 10% → 10% (유지)

3. WORK_IN_PROGRESS.md 업데이트:
   - Gate-2 상태: 실패
   - 완료 단계: Plan만 체크
   - 다음 작업: analyst → 계획 재검토
```
```

### 시나리오 1: 단계 실패 후 재시도

```
tester: Test 단계 실패 (버그 발견)
  ↓
WORK_IN_PROGRESS.md 업데이트 (에러 기록)
  ↓
developer: 버그 수정
  ↓
WORK_IN_PROGRESS.md 업데이트 (수정 완료)
  ↓
reviewer: 수정 리뷰
  ↓
WORK_IN_PROGRESS.md 업데이트 (리뷰 통과)
  ↓
tester: 재테스트
  ↓
WORK_IN_PROGRESS.md 업데이트 (테스트 통과)
  ↓
다음 단계 (Docs)
```

### 시나리오 2: 다중 단계 실패 후 롤백

```
tester: Test 단계 실패 (버그 발견)
  ↓
developer: 버그 수정 시도
  ↓
developer: 수정 실패 (더 깊은 문제 발견)
  ↓
coordinator: 이전 단계로 롤백 결정
  ↓
WORK_IN_PROGRESS.md 업데이트 (Code 단계로 롤백)
  ↓
analyst: 계획 재검토
  ↓
developer: 재구현
  ↓
reviewer: 재리뷰
  ↓
tester: 재테스트
  ↓
성공 ✅
```

### 시나리오 3: 긴급 버그 수정 후 원래 작업 재개

```
[상황: WIP-20250205-001 진행 중]
developer: Code 단계 진행 중...
  ↓
사용자: "🚨 ExportService.cs:45 NullReferenceException"
  ↓
coordinator: 긴급 대응 모드 시작
  ↓
WIP-20250205-001 일시 정지 (상태: ⏸️ 일시 정지)
  ↓
[새 WorkID 생성: WIP-20250205-999 (긴급)]
  ↓
analyst: 오류 분석
  ↓
developer: 즉시 수정
  ↓
tester: 수정 검증
  ↓
reviewer: 수정 검토
  ↓
WIP-20250205-999 완료 ✅
  ↓
WIP-20250205-001 재개 (상태: ⏸️ 진행 중)
  ↓
developer: 원래 작업 계속
```

---

## 에러 상황 사용자 결정 요청

### 결정 요청 형식
```markdown
coordinator:
⚠️ 단계 실패: 사용자 결정 요청

[WorkID]: WIP-20250205-001
[실패 단계]: Code
[에러]: 빌드 실패: CS8602 NullReferenceException
[위치]: Services/Processors/CSVProcessor.cs:45

[옵션]
1. 재시도: developer가 에러 수정을 다시 시도합니다.
2. 수정: developer에게 구체적인 수정 방법을 지정합니다.
3. 롤백: 이전 단계로 롤백하여 다시 시작합니다.
4. 취소: 작업을 취소합니다.

[추천]
옵션2: null 체크 추가 권장
```

### 사용자 응답
```markdown
사용자: "옵션2: CSVProcessor.cs:45에 if (data == null) throw new Exception(...) 추가해줘"

developer:
✅ 사용자 지시대로 수정 완료

[수정 내용]
- CSVProcessor.cs:45에 null 체크 추가
- 빌드 성공 ✅

reviewer에게 리뷰를 요청합니다...
```

---

## 사용자 역할

**사용자는 다음 지시만 하면 됩니다:**
```
"신규: 기능 설명"           # 새 작업 시작
"수정: 파일:라인 문제"      # 기능 수정
"기획: 파일경로"            # 기획서 처리
"완료: WIP-XXXXXXXX-NNN"            # 작업 완료
"취소: WIP-XXXXXXXX-NNN 사유"        # 작업 취소
```

**에이전트가 자동으로:**
- WORK_IN_PROGRESS.md 업데이트
- 단계 추적
- 완료/취소 처리

---

## 📚 관련 모듈

- [PIPELINE.md](PIPELINE.md) - 개발 파이프라인
- [GATES.md](GATES.md) - Gate 검증 시스템
- [AUTO_UPDATE.md](AUTO_UPDATE.md) - WorkID 및 자동 업데이트 시스템
- [REPORTS.md](REPORTS.md) - 작업 완료 보고서 생성
