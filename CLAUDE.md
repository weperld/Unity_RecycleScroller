# Unity_RecycleScroller - Claude Code 프로젝트 설정

## 프로젝트 개요

Unity용 고성능 재활용 스크롤 시스템. Object Pooling 기반의 효율적인 UI 스크롤러로, 무한 스크롤, 페이지네이션, 비동기 데이터 로드, 셀 그룹 배치 등을 지원

- **기술 스택**: C#, Unity 2022.3.62f2, UGUI, UniTask
- **라이브러리**: UniTask (비동기 작업), TextMeshPro 3.0.9, Newtonsoft.Json 3.2.2, Unity UI (UGUI) 1.0.0
- **출력 포맷**: Unity Package (.unitypackage), DLL Assembly
- **기능 카테고리**: ScrollCore, CellPooling, LoopScroll, Pagination, AsyncLoad, CellGrouping, EasingAnimation, EditorTools
- **상세 정보**: PROJECT_SUMMARY.md 참조

### 프로젝트 구조

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
│   └── LoopScrollbar.cs              # 루프 스크롤바
├── Editor/                  # 에디터 도구 및 커스텀 드로어
│   ├── Attributes/          # 속성 드로어
│   ├── Drawers/             # 커스텀 프로퍼티 드로어
│   ├── RecycleScroll/       # 스크롤러 에디터
│   └── Creator/             # 스크롤뷰 생성 도구
├── Attributes/              # 커스텀 속성 정의
├── LoadDataExtension/       # 데이터 로드 확장
├── SerializableDictionary/  # 직렬화 가능 딕셔너리
└── Scenes/                  # 샘플 씬

### Unity 스크롤러 규칙
- **Object Pooling 필수**: 셀은 항상 재활용, 매번 새로 생성하지 않음
- **Viewport 기반 관리**: 보이는 셀만 활성화, 나머지는 풀에 보관
- **ScrollRect 래핑**: UnityEngine.UI.ScrollRect를 기반으로 확장
- **MonoBehaviour 상속**: 모든 컴포넌트는 MonoBehaviour 기반
- **에디터/런타임 분리**: Editor 폴더 내 코드는 빌드에 포함되지 않음
- **비동기 처리**: UniTask 기반, CancellationToken으로 취소 지원

---

## 필수 참조 문서

작업 전 반드시 해당 문서를 확인하세요:

| 문서 | 경로 | 용도 |
|------|------|------|
| **프로젝트 요약** | `PROJECT_SUMMARY.md` | 30초 프로젝트 이해 |
| **에이전트 규칙** | `AGENTS.md` | 절대 규칙, Self-Validation, Cross-Stage Review |
| **에이전트 역할** | `AGENT_ROLES.md` | 각 에이전트 역할 정의 |
| **워크플로우** | `WORKFLOW_PLANNING/INDEX.md` | 자동 업데이트 시스템, WIP 관리 |
| **작업 현황** | `WORK_IN_PROGRESS.md` | 현재 진행 중인 작업 |
| **빠른 참조** | `QUICK_REFERENCE.md` | 자주 사용하는 명령어/패턴 |

### 개발 가이드 (.guides/)

| 문서 | 용도 |
|------|------|
| `.guides/BUILD_GUIDE.md` | 빌드 및 개발 절차 |
| `.guides/CODE_STYLE.md` | 코드 스타일 가이드 |
| `.guides/TECHNICAL_RULES.md` | 기술 요구사항 및 표준 |
| `.guides/WORKFLOW_GUIDE.md` | 워크플로우 절차 |
| `.guides/TEST_GUIDE.md` | 테스트 표준 |
| `.guides/COMMIT_RULES.md` | Git 커밋 규칙 |
| `.guides/PLANNING_TEMPLATE.md` | 기획 문서 템플릿 |

---

## 절대 규칙 (Hard Blocks)

> AGENTS.md의 절대 규칙 섹션을 반드시 준수하세요.

핵심 규칙 요약:
- **타입 안전성**: 무조건 캐스팅 (Type)cast 남용 금지, as 연산자 + null 체크 사용 권장, Generic 타입 제약 활용
- **빈 catch 블록 금지**: catch(e) {} 사용 금지
- **추측 금지**: 모호한 요청은 반드시 사용자에게 확인

---

## 커스텀 명령어

`.claude/commands/` 디렉토리에 14개의 명령어가 정의되어 있습니다.
`/project:명령어`로 전체 목록을 확인하세요.

주요 명령어:
- `/project:신규 [기능 설명]` - 새로운 기능 추가
- `/project:수정 [문제 설명]` - 버그 수정 또는 기능 개선
- `/project:커밋` - 변경 사항 커밋 (메시지 자동 생성)
- `/project:전송` - 스테이징 → 커밋 → 푸시 한번에
- `/project:상태 전체` - 전체 작업 상태 확인

---

## 워크플로우 파이프라인

```
Plan → Design → Code → Test → Docs → QA → Review
```

각 단계마다 Gate 검증이 수행되며, 3번 실패 시 이전 단계로 롤백됩니다.
계획/설계 단계에서는 **수렴 검증**이 적용됩니다: 결과물의 누락·모호·위험 요소(필수 보완 사항)가 0건이 될 때까지 사용자 확인을 거쳐 반복 점검 후 Gate로 진행합니다.
상세 프로세스는 `WORKFLOW_PLANNING/INDEX.md`를 참조하세요.

---

## WIP 추적 시스템

- **WorkID 형식**: `WIP-YYYYMMDD-NNN`
- **활성 WIP**: `.wips/active/{Stage}/WIP-{Stage}-YYYYMMDD-NNN.md`
- **완료 WIP**: `.wips/archive/{Stage}/WIP-{Stage}-YYYYMMDD-NNN.md`
- **전체 현황**: `WORK_IN_PROGRESS.md`

---

## 작업 중 문서화 규칙 (필수)

다른 PC 또는 다른 사용자가 작업을 이어받을 수 있도록, 모든 개발 작업 시 다음을 준수합니다:

1. **작업 시작** → `WORK_IN_PROGRESS.md`에 WorkID 및 계획 기록
2. **각 단계 완료** → 체크박스 업데이트 + 진행 상황 타임스탬프
3. **중단 시** → 현재 상태, 다음 할 일, 미해결 이슈를 명시적으로 기록
4. **재개 시** → `/project:작업이어하기 WIP-YYYYMMDD-NNN`으로 이전 작업 확인 후 이어서 진행

---

## 빌드 및 실행

```bash
Unity Editor 빌드 (메뉴: Build Settings > Build) 또는 dotnet build (csproj 단위)

Unity Test Runner (Window > General > Test Runner) 또는 dotnet test

Unity Editor Play Mode (Ctrl+P)

Unity Editor 메뉴:
- GameObject > UI > RecycleScrollView (스크롤뷰 생성)
- Window > General > Test Runner (테스트 실행)
```

## 명명 규칙

- **클래스/메서드/프로퍼티**: PascalCase (예: RecycleScroller, LoadData)
- **private 필드**: m_ 접두사 + camelCase (예: m_scrollRect)
- **인터페이스**: I 접두사 + PascalCase (예: IRecycleScrollerDelegate)
- **부분 클래스 파일**: ClassName_Responsibility.cs (예: RecycleScroller_Functions.cs)
- **열거형**: e 접두사 + PascalCase (예: eScrollAxis)
- **상수**: UPPER_SNAKE_CASE (예: DEFAULT_POOL_SUBKEY)
- **네임스페이스**: PascalCase (예: RecycleScroll, CustomSerialization)
