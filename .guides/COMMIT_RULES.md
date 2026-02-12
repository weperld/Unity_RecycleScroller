# 커밋 규칙 (Commit Rules)

> 프로젝트의 Git 커밋 메시지 표준과 워크플로우를 정의합니다.

---

## 📌 기본 형식

```
[태그] 요약

본문 (선택사항)

WorkID: WIP-YYYYMMDD-NNN
```

### 필수 요소
- **태그**: 작업 유형을 나타내는 괄호 안의 키워드
- **요약**: 변경 내용을 한글로 간결하게 설명 (띄어쓰기 주의)

---

## 🏷️ 커밋 태그

| 태그 | 용도 | 예시 |
|------|------|------|
| `[feat]` | 새로운 기능 추가 | `[feat] CSV 데이터 추출 기능 추가` |
| `[fix]` | 버그 수정 | `[fix] ExportService null 참조 에러 수정` |
| `[refactor]` | 코드 리팩터링 (기능 동일) | `[refactor] ExportService 비동기 처리 개선` |
| `[docs]` | 문서 추가/수정 (코드 X) | `[docs] COMMIT_RULES.md 작성` |
| `[style]` | 코드 포맷팅, 세미콜론 누락 등 | `[style] XML 주석 정렬` |
| `[test]` | 테스트 코드 추가/수정 | `[test] CSVProcessor 단위 테스트 추가` |
| `[chore]` | 빌드 시스템, 의존성, 도구 관련 | `[chore] NPOI 라이브러리 버전 업데이트` |
| `[perf]` | 성능 개선 | `[perf] 대용량 엑셀 처리 속도 최적화` |
| `[ci]` | CI/CD 설정 변경 | `[ci] GitHub Actions 워크플로우 추가` |
| `[revert]` | 이전 커밋 되돌리기 | `[revert] feat: CSV 데이터 추출 기능 추가` |

---

## ✏️ 작성 규칙

### 제목 (Subject)
- **한국어로 작성**: `[feat] 새 기능 추가` (O), `[feat] Add new feature` (X)
- **간결하게**: 50자 이내 권장
- **문장 완결형**: 마침표 사용 ❌
- **대문자로 시작**: `[fix] NullReferenceException 수정` (O), `[fix] nullReferenceException 수정` (X)
- **마침표 금지**: `[feat] 기능 추가` (O), `[feat] 기능 추가.` (X)

### 본문 (Body)
- **한 줄당 72자 이내로 줄바꿈**
- **무엇을 왜 변경했는지 설명**
- **빈 줄로 제목과 분리**

**예시:**
```
[refactor] ExportService 비동기 처리 개선

try-finally 패턴을 적용하여 예외 발생 시에도
IsBusy 상태가 올바르게 해제되도록 수정

WorkID: WIP-20250208-001
```

---

## 🔗 WorkID 참조

작업 추적 시스템과 연동하기 위해 WorkID를 포함:

```
[feat] CSV 데이터 추출 기능 추가

WorkID: WIP-20250208-001
```

### WorkID 형식
- `WIP-YYYYMMDD-NNN`
- 예: `WIP-20250208-001`

---

## 🔄 기본 작업 절차

### 변경 사항 확인
```bash
git status
```

### 변경 내용 검토
```bash
git diff
git diff --staged
```

### 변경 파일 스테이징
```bash
git add <파일경로>
```

---

## 📦 커밋 분류 및 그룹핑 규칙 (중요)

### 🎯 원칙
하나의 커밋에는 **하나의 논리적인 변경**만 포함합니다.

### 🔄 커밋 전 변경 사항 분석 절차

#### 1단계: 변경 사항 확인
```bash
git status
```

#### 2단계: 변경 파일 검토
```bash
git diff
git diff --staged
```

#### 3단계: 파일별 분류

| 변경 유형 | 대상 파일 | 커밋 태그 | 그룹핑 기준 |
|----------|----------|-----------|-------------|
| **기능 구현** | 새로운 기능 관련 파일 | `[feat]` | 하나의 기능 단위로 그룹핑 |
| **버그 수정** | 버그 수정 관련 파일 | `[fix]` | 하나의 버그 단위로 그룹핑 |
| **리팩터링** | 기능 동일, 코드 개선 | `[refactor]` | 하나의 리팩터링 단위로 그룹핑 |
| **문서** | `.md` 파일 (코드 X) | `[docs]` | 문서 단위로 그룹핑 |
| **테스트** | `*Test.cs`, `*Tests.cs` | `[test]` | 테스트 단위로 그룹핑 |
| **스타일/포맷** | 코드 스타일, 포맷팅 | `[style]` | 파일 단위 또는 관련 파일 그룹핑 |
| **빌드/설정** | `.csproj`, `package.json` | `[chore]` | 관련 파일 그룹핑 |

### 📋 그룹핑 시나리오

#### ✅ 올바른 그룹핑 예시

**시나리오 1: 새 기능 추가**
```
[feat] CSV 데이터 추출 기능 추가

변경 파일:
- Services/Processors/CSVProcessor.cs (새로운 기능)
- Models/CSVDataItem.cs (새로운 모델)

→ 하나의 커밋으로 그룹핑
```

**시나리오 2: 버그 수정**
```
[fix] ExportService null 참조 에러 수정

변경 파일:
- Services/ExportService.cs (null 체크 추가)

→ 하나의 커밋으로 그룹핑
```

**시나리오 3: 기능 + 버그 수정**
```
[feat] CSV 데이터 추출 기능 추가
변경: CSVProcessor.cs, CSVDataItem.cs

[fix] CSVProcessor null 참조 에러 수정
변경: CSVProcessor.cs (같은 파일이지만 다른 논리)

→ 두 개의 커밋으로 분리
```

**시나리오 4: 문서 업데이트**
```
[docs] CSV 기능 사용법 추가

변경 파일:
- README.md (CSV 사용법 추가)
- docs/csv-usage.md (새 문서)

→ 하나의 커밋으로 그룹핑
```

**시나리오 5: 리팩터링**
```
[refactor] 비동기 처리 패턴 일관성 개선

변경 파일:
- Services/ExportService.cs (try-finally 추가)
- Services/CSVProcessor.cs (try-finally 추가)

→ 하나의 커밋으로 그룹핑 (동일한 리팩터링 적용)
```

#### ❌ 잘못된 그룹핑 예시

**안 됨 1: 여러 기능을 하나의 커밋에**
```
[feat] CSV와 JSON 기능 추가

변경:
- CSVProcessor.cs (CSV 기능)
- JSONProcessor.cs (JSON 기능)

→ 분리해야 함:
  [feat] CSV 데이터 추출 기능 추가
  [feat] JSON 데이터 추출 기능 추가
```

**안 됨 2: 코드와 문서를 섞음**
```
[feat] CSV 기능 추가 및 문서 작성

변경:
- CSVProcessor.cs (기능)
- README.md (문서)

→ 분리해야 함:
  [feat] CSV 데이터 추출 기능 추가
  [docs] CSV 기능 사용법 추가
```

**안 됨 3: 버그 수정과 스타일 변경을 섞음**
```
[fix] 버그 수정 및 포맷팅

변경:
- ExportService.cs (버그 수정 + 포맷팅)

→ 분리해야 함:
  [fix] ExportService null 참조 에러 수정
  [style] ExportService 포맷팅
```

---

## 🔧 커밋 분류 실전 가이드

### 1단계: 변경 파일 목록 확인
```bash
git status
```

### 2단계: 파일별 변경 내용 검토
```bash
# 각 파일의 변경 내용 확인
git diff Services/ExportService.cs
git diff Models/CSVDataItem.cs
git diff README.md
```

### 3단계: 변경 유형 분류

| 파일 | 변경 내용 | 유형 | 그룹핑 |
|------|----------|------|--------|
| CSVProcessor.cs | 새로운 기능 구현 | `[feat]` | 그룹 A |
| CSVDataItem.cs | 새로운 모델 추가 | `[feat]` | 그룹 A |
| README.md | 사용법 추가 | `[docs]` | 그룹 B |
| ExportService.cs | 버그 수정 | `[fix]` | 그룹 C |
| ExportService.cs | 포맷팅 | `[style]` | 그룹 D |

### 4단계: 그룹별 스테이징 및 커밋

```bash
# 그룹 A: [feat] CSV 데이터 추출 기능 추가
git add Services/CSVProcessor.cs Models/CSVDataItem.cs
git commit -m "[feat] CSV 데이터 추출 기능 추가

WorkID: WIP-20250208-001"

# 그룹 B: [docs] CSV 기능 사용법 추가
git add README.md
git commit -m "[docs] CSV 기능 사용법 추가

WorkID: WIP-20250208-001"

# 그룹 C: [fix] ExportService null 참조 에러 수정
git add Services/ExportService.cs
git commit -m "[fix] ExportService null 참조 에러 수정

WorkID: WIP-20250208-001"

# 그룹 D: [style] ExportService 포맷팅
git add Services/ExportService.cs
git commit -m "[style] ExportService 포맷팅"
```

---

## 🎯 특수 상황 처리

### 이전 커밋 수정
```bash
# 마지막 커밋 수정 (아직 푸시하지 않은 경우)
git commit --amend

# 메시지만 수정
git commit --amend -m "[feat] 수정된 메시지"
```

### 복수의 커밋 합치기 (Squash)
```bash
# 최근 3개 커밋을 하나로 합치기
git rebase -i HEAD~3
```

---

## 🔐 커밋 및 푸시 안전 규칙

### 커밋 전 체크리스트
- **절대 규칙 위반 코드는 커밋하지 않음**: AGENTS.md 절대 규칙 (Line 8-33) 위반 코드는 커밋하지 않음
- **불필요한 파일 제외**: 보안에 민감한 파일이나 불필요한 파일은 커밋하지 않음
  - 예: `.env`, `credentials.json`, `.env.local`, `*.log`, `.DS_Store` 등

### 푸시 전 체크리스트
- **force push 금지**: main/master 브랜치에 force push 금지
  - 로컬 커밋이 원격보다 뒤처진 경우 충돌 해결 후 정상 푸시
- **커밋 완료 확인**: 로컬 커밋이 완료된 후에만 푸시
- **충돌 처리**: 원격 저장소와 충돌 발생 시 사용자에게 알리고 해결 후 다시 푸시

---

## 📚 참고 자료

- [Conventional Commits](https://www.conventionalcommits.org/)
- [Git Commit Message Best Practices](https://chris.beams.io/posts/git-commit/)

---

## 📌 기억해야 할 핵심 규칙

1. **한 커밋, 하나의 변경**
2. **로컬 변경 사항을 확인하고 적절히 분류**
3. **관련 없는 변경은 분리하여 커밋**
4. **태그로 변경 유형 명확히 구분**
5. **한국어로 간결하게 작성**
