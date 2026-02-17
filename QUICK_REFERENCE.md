# Quick Reference

> 자주 사용하는 명령어, 코드 패턴, 지시법을 요약한 빠른 참조용 문서입니다.

---

## 🚀 빌드 및 실행

### 빌드
```bash
Unity Editor 빌드 (메뉴: Build Settings > Build) 또는 dotnet build (csproj 단위)
```

### 실행
```bash
Unity Editor Play Mode (Ctrl+P)
```

### CLI 옵션
Unity Editor 메뉴:
- GameObject > UI > RecycleScrollView (스크롤뷰 생성)
- Window > General > Test Runner (테스트 실행)

---

## 💻 자주 쓰는 코드 패턴

- **Delegate 패턴**: IRecycleScrollerDelegate 인터페이스 구현
- **Partial Class**: 기능별 파일 분리 (RecycleScroller_*.cs)
- **Object Pooling**: Dictionary<Type, Dictionary<string, Stack<Cell>>> 구조
- **비동기 패턴**: UniTask + CancellationTokenSource
- **커스텀 에디터**: PropertyDrawer + CustomEditor 패턴

---

## 📝 명명 규칙

- **클래스/메서드/프로퍼티**: PascalCase (예: RecycleScroller, LoadData)
- **private 필드**: m_ 접두사 + camelCase (예: m_scrollRect)
- **인터페이스**: I 접두사 + PascalCase (예: IRecycleScrollerDelegate)
- **부분 클래스 파일**: ClassName_Responsibility.cs (예: RecycleScroller_Functions.cs)
- **열거형**: e 접두사 + PascalCase (예: eScrollAxis)
- **상수**: UPPER_SNAKE_CASE (예: DEFAULT_POOL_SUBKEY)
- **네임스페이스**: PascalCase (예: RecycleScroll, CustomSerialization)

---

## 🎯 단축 지시법

### 프로젝트 이해 필요
```
요약: PROJ_SUMMARY 읽고 3줄로 설명
```
→ PROJECT_SUMMARY.md 읽고 핵심 3줄로 요약

---

### 기획서 처리
```
기획: [파일경로]
또는
기획: "기획서 내용"
```
→ WORKFLOW_PLANNING/INDEX.md 참고 → 분석 → 계획 → 확인

**예시:**
```
기획: ./docs/planning/feature_001.md
기획: "[기능 설명]"
```

---

### 기능 수정
```
수정: [파일:라인] [문제]
또는
수정: [문제 설명]
```
→ 유형A 분석 → 계획 → 확인 → 구현

**예시:**
```
수정: [파일명]:[라인] [문제 설명]
수정: [서비스명]에서 [문제] 버그 수정
```

---

### 새로운 기능
```
신규: [기능 설명]
```
→ 유형B 분석 → 계획 → 확인 → 구현

**예시:**
```
신규: [기능명] 데이터 추출 기능 추가
신규: [프로세서명] 기능 추가
```

---

### 작업 재개
```
재개: WIP-YYYYMMDD-NNN
또는
CONTINUE: WIP-YYYYMMDD-NNN
```
→ WORK_IN_PROGRESS.md에서 상태 확인 → 재개

**예시:**
```
재개: WIP-20250202-001
CONTINUE: WIP-20250202-001
```

---

### 작업 완료
```
완료: WIP-YYYYMMDD-NNN
```
→ 완료 단계 모두 체크 → 완료 작업으로 이동

---

### 작업 취소
```
취소: WIP-YYYYMMDD-NNN [사유]
```
→ 활성 작업에서 제거 → 취소 작업으로 이동

**예시:**
```
취소: WIP-20250202-001 우선순위 조정으로 인해
```

---

### 상태 확인
```
상태: WIP-YYYYMMDD-NNN
또는
상태: 전체
```

---

### 내보내기
```
내보내기: json
또는
내보내기: markdown
```
→ WORK_IN_PROGRESS.md의 완료/취소 작업을 WORK_HISTORY.json 또는 마크다운으로 내보내기

**예시:**
```
내보내기: json
내보내기: markdown
```

---

### 긴급 버그
```
🚨 [파일:라인] [오류 메시지]
```
→ 즉시 LOG 확인 → 문제 분석 → 수정

**예시:**
```
🚨 [파일명]:[라인] [예외타입] 발생
🚨 [뷰모델명]에서 빌드 오류
```

---

### 커밋 및 푸시
```
커밋: [메시지]
```
→ 로컬 변경 사항을 확인하고 적절히 분류 후 커밋

```
푸시: [메시지]
```
→ 커밋 후 원격 저장소로 푸시

**예시:**
```
커밋: [기능명] 데이터 추출 기능 추가
푸시: [기능명] 기능 추가 및 버그 수정
```

---

## 🔍 에러 해결 체크리스트

### 빌드 오류
- [ ] Unity 에디터 콘솔에 컴파일 에러 없음
- [ ] Assembly Definition (.asmdef) 참조 경로 확인
- [ ] 누락된 using 문 확인
- [ ] Unity 패키지 의존성 확인 (Package Manager)

### 런타임 오류
- [ ] NullReferenceException 발생 위치 확인
- [ ] MissingReferenceException (파괴된 오브젝트 참조) 확인
- [ ] IndexOutOfRangeException (셀 인덱스) 확인
- [ ] ScrollRect 이벤트 핸들러 무한 호출 확인

---

## 📌 WorkID 형식

### 추천 형식: **WIP-YYYYMMDD-NNN**

```
WIP-20250202-001  # 2025년 2월 2일 첫 번째 작업
WIP-20250202-002  # 같은 날 두 번째 작업
WIP-20250203-001  # 다음 날 첫 번째 작업
```

---

## 🔗 빠른 링크

### 메인 문서
- [전체 가이드 목차](AGENTS.md)
- [프로젝트 요약](PROJECT_SUMMARY.md)
- [빠른 참조 (현재 문서)](QUICK_REFERENCE.md)

### 워크플로우
- [기획서 워크플로우](WORKFLOW_PLANNING/INDEX.md)
- [작업 추적](WORK_IN_PROGRESS.md)
- [작업 히스토리](WORK_HISTORY.json)

### 상세 가이드
- [빌드 및 실행](.guides/BUILD_GUIDE.md)
- [작업 워크플로우](.guides/WORKFLOW_GUIDE.md)
- [코드 스타일](.guides/CODE_STYLE.md)
- [기술 규칙](.guides/TECHNICAL_RULES.md)
- [커밋 규칙](.guides/COMMIT_RULES.md)

---

## 🚨 긴급 상황 대응

### 빌드 오류 발생 시
```
1. 에러 메시지 복사
2. "🚨 [파일:라인] [에러]" 지시
3. 에이전트가 자동으로 분석 및 수정 제안
```

### 작업 중단 시
```
1. "상태: 전체"로 현재 작업 확인
2. 다음 대화에서 "재개: WIP-XXXXXXXX-NNN"로 작업 재개
```

---

## 📊 파일 구조 요약

Assets/
├── RecycleScroll/           # 핵심 스크롤러 구현 (partial class)
│   ├── RecycleScroller.cs            # 메인 컨트롤러
│   ├── RecycleScroller_Functions.cs  # 기능 메서드
│   ├── RecycleScroller_LoadData.cs   # 데이터 로드 로직
│   ├── RecycleScroller_Inspector.cs  # 에디터 인스펙터
│   ├── RecycleScrollerCell.cs        # 셀 기본 클래스
│   ├── RecycleScrollerDatas.cs       # 데이터 구조
│   ├── RecycleScrollerEnums.cs       # 열거형 정의
│   ├── IRecycleScrollerDelegate.cs   # 델리게이트 인터페이스
│   ├── EasingFunctions.cs            # 이징 함수
│   └── RecycleScrollbar.cs           # 재활용 스크롤바
├── Editor/                  # 에디터 도구 및 커스텀 드로어
│   ├── Attributes/          # 속성 드로어
│   ├── Drawers/             # 커스텀 프로퍼티 드로어
│   ├── RecycleScroll/       # 스크롤러 에디터
│   └── Creator/             # 스크롤뷰 생성 도구
├── Attributes/              # 커스텀 속성 정의
├── LoadDataExtension/       # 데이터 로드 확장
├── SerializableDictionary/  # 직렬화 가능 딕셔너리
└── Scenes/                  # 샘플 씬

---

## 💡 기술 원칙 (즉시 확인 필요)

- **Object Pooling 필수**: 셀 재활용으로 메모리 효율화
- **타입 안전성**: 무조건 캐스팅 남용 금지
- **GC 최소화**: Update 루프에서 할당 최소화
- **에러 처리**: 빈 catch 블록 사용 금지
- **에디터/런타임 분리**: #if UNITY_EDITOR 엄격 적용

---

## 🎯 새 대화에서의 빠른 시작

### 문서 참조 순서
```
1. CLAUDE.md (자동 로드 — 절대 규칙, 작업 방식)
2. PROJECT_SUMMARY.md 읽기 (프로젝트 이해)
3. WORK_IN_PROGRESS.md 읽기 (현재 진행 중 작업 확인)
4. 필요에 따라 상세 가이드 참조 (CODE_STYLE.md, TECHNICAL_RULES.md 등)
5. AGENTS.md 참조 (Self-Validation, Cross-Stage Review 등 필요 시)
```

### 작업 시작
```
1. PROJECT_SUMMARY.md에서 프로젝트 컨텍스트 파악
2. "요약: PROJ_SUMMARY"로 빠른 컨텍스트 전달
3. 커스텀 명령어로 작업 시작 (/project:신규, /project:수정, /project:간편)
4. 모든 단계에서 WORK_IN_PROGRESS.md 자동 업데이트
```

---

## 📖 상세 참조

**더 상세한 내용이 필요하면:**
- [코드 작성법](.guides/CODE_STYLE.md)
- [기술 규칙](.guides/TECHNICAL_RULES.md)
- [기획서 처리](WORKFLOW_PLANNING/INDEX.md)
- [작업 추적](WORK_IN_PROGRESS.md)

---

## 📊 보고서 생성

### 보고서 명령어
```
보고서: WIP-YYYYMMDD-NNN
```
→ WORK_IN_PROGRESS.md의 완료 작업에서 보고서 생성

### 보고서 형식

#### JSON (WORK_HISTORY.json)
```json
{
  "workId": "WIP-20250202-001",
  "type": "수정",
  "title": "[작업명]",
  "startDate": "2025-02-02T10:00:00",
  "endDate": "2025-02-02T16:30:00",
  "duration": "6.5h",
  "files": ["[파일경로]"],
  "commit": "abc123"
}
```

#### 마크다운 (reports/WORK_REPORT_WIP-YYYYMMDD-NNN.md)
```markdown
# 작업 보고서

## 작업 정보
- **WorkID**: WIP-20250202-001
- **유형**: 기능 수정
- **제목**: [작업명]

## 기간
- **시작**: 2025-02-02 10:00
- **완료**: 2025-02-02 16:30
- **소요 시간**: 6.5시간

## 구현 내용
- [파일경로]
  - [라인번호]: [변경 내용]

## 테스트 결과
- [x] 빌드 성공
- [x] 기능 테스트 통과
```
