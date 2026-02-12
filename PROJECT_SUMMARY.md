# Unity_RecycleScroller Project Summary

> 프로젝트를 빠르게 이해하기 위한 핵심 요약입니다.

---

## 30초 요약

**Unity_RecycleScroller**는 Unity용 고성능 재활용 스크롤 시스템입니다. Object Pooling 기반으로 대량의 데이터를 메모리 효율적으로 표시하며, 무한 스크롤, 페이지네이션, 비동기 데이터 로드 등을 지원합니다.

- **입력**: IRecycleScrollerDelegate 인터페이스를 통한 데이터 공급
- **출력**: 재활용 가능한 UI 셀 기반의 스크롤 뷰
- **특징**: Object Pooling, Loop Scroll, Pagination, UniTask 비동기 로드, 셀 그룹 배치

---

## 핵심 아키텍처

### 아키텍처 패턴
- **Delegate 패턴**: IRecycleScrollerDelegate 인터페이스로 데이터 공급과 셀 생성 분리
- **Partial Class**: RecycleScroller를 기능별로 6개 파일로 분리
- **Object Pooling**: Dictionary<Type, Dictionary<string, Stack<Cell>>> 구조로 셀 재활용
- **Component 기반**: MonoBehaviour 상속, ScrollRect 래핑

### 시스템 구성요소
| 구성요소 | 역할 |
|----------|------|
| RecycleScroller | 메인 컨트롤러 - 스크롤, 풀링, 페이징 관리 |
| RecycleScrollerCell | 재활용 셀 기본 클래스 |
| IRecycleScrollerDelegate | 데이터/셀 공급 인터페이스 |
| LoopScrollbar | 무한 스크롤 전용 스크롤바 |
| LoadDataExtensionComponent | 데이터 로드 확장 기본 클래스 |
| EasingFunctions | 30+ 이징 함수 라이브러리 |

### 카테고리/모듈
| 카테고리 | 역할 |
|----------|------|
| ScrollCore | 핵심 스크롤 메커니즘, 셀 배치/재배치 |
| CellPooling | 셀 생성, 재활용, 풀 관리 |
| LoopScroll | 무한 순환 스크롤 기능 |
| Pagination | 페이지 단위 스냅, 이동 |
| AsyncLoad | UniTask 기반 비동기 데이터 로드 |
| CellGrouping | Grid 형태 셀 그룹 배치 |
| EasingAnimation | 부드러운 스크롤 애니메이션 |
| EditorTools | 인스펙터, 커스텀 드로어, 생성 도구 |

---

## 핵심 파일 구조

```
Assets/
├── RecycleScroll/           # 핵심 스크롤러 (partial class)
│   ├── RecycleScroller.cs            # 메인 컨트롤러
│   ├── RecycleScroller_Functions.cs  # 기능 메서드
│   ├── RecycleScroller_LoadData.cs   # 데이터 로드
│   ├── RecycleScrollerCell.cs        # 셀 기본 클래스
│   ├── RecycleScrollerDatas.cs       # 데이터 구조
│   ├── IRecycleScrollerDelegate.cs   # 델리게이트 인터페이스
│   └── EasingFunctions.cs           # 이징 함수
│
├── Editor/                  # 에디터 도구
│   ├── Attributes/          # 속성 드로어
│   ├── Drawers/             # 커스텀 프로퍼티 드로어
│   └── Creator/             # 스크롤뷰 생성 도구
│
├── Attributes/              # 커스텀 속성 정의
├── LoadDataExtension/       # 데이터 로드 확장
├── SerializableDictionary/  # 직렬화 가능 딕셔너리
│
└── .guides/                 # 에이전트 가이드
    ├── BUILD_GUIDE.md
    ├── CODE_STYLE.md
    ├── TECHNICAL_RULES.md
    └── TEST_GUIDE.md
```

---

## 기술 스택

| 항목 | 기술 | 버전 |
|------|------|------|
| **언어** | C# | 9.0 |
| **엔진** | Unity | 2022.3.62f2 (LTS) |
| **UI 프레임워크** | UGUI | 1.0.0 |
| **비동기 라이브러리** | UniTask | latest |
| **텍스트 렌더링** | TextMeshPro | 3.0.9 |
| **JSON 직렬화** | Newtonsoft.Json | 3.2.2 |

---

## 빠른 시작

### 빌드
```
Unity Editor > File > Build Settings > Build
```

### 실행
```
Unity Editor Play Mode (Ctrl+P)
샘플 씬: Assets/Scenes/SampleScene.unity
```

### 스크롤뷰 생성
```
Unity Editor > GameObject > UI > RecycleScrollView
```

---

## 주요 기능

### 1. Object Pooling
Viewport 밖으로 벗어난 셀을 자동 회수하여 풀에 보관하고, 새로 필요한 셀에 재활용합니다. 수천 개의 아이템도 소수의 셀만으로 표시 가능합니다.

### 2. Loop Scroll (무한 스크롤)
스크롤 끝에서 처음으로 자동 순환합니다. LoopScrollbar를 통해 시각적 피드백도 지원합니다.

### 3. Pagination
페이지 단위 스냅 기능으로, 스크롤 종료 시 가장 가까운 페이지에 자동 정렬됩니다. 이징 함수로 부드러운 애니메이션을 제공합니다.

### 4. 비동기 데이터 로드
UniTask 기반으로 대량 데이터를 비동기 로드합니다. CancellationToken으로 작업 취소를 지원합니다.

### 5. 셀 그룹 배치
Grid 형태로 여러 셀을 한 행/열에 배치할 수 있습니다. 그룹당 셀 수, 간격, 정렬을 개별 설정 가능합니다.

---

## 에이전트 필독 순서

새로운 대화에서 작업을 시작할 때:

1. **AGENTS.md** 읽기 (메뉴 및 지시 템플릿)
2. **PROJECT_SUMMARY.md** 읽기 (현재 파일)
3. **QUICK_REFERENCE.md** 참조 (자주 쓰는 패턴)
4. 필요한 가이드 읽기 (CODE_STYLE.md, WORKFLOW_PLANNING/INDEX.md 등)
5. **WORK_IN_PROGRESS.md** 확인 (진행 중인 작업)

---

## 개발 프로세스

```
기획서 전달 → 유형 분석 → 계획 수립 → 설계 → 구현 → 테스트 → 문서화 → QA → 최종검토
```

**상세 절차:** [WORKFLOW_PLANNING/INDEX.md](./WORKFLOW_PLANNING/INDEX.md)

---

## 긴급 상황

빌드 오류나 런타임 오류 발생 시:
```
[Assets/RecycleScroll/RecycleScroller.cs:123] CS0246 The type or namespace name could not be found
```

```
[RecycleScroller:LoadData] NullReferenceException: Object reference not set to an instance
```

---

## 상세 문서

| 항목 | 문서 |
|------|------|
| 전체 가이드 목차 | [AGENTS.md](./AGENTS.md) |
| 빌드 및 실행 | [.guides/BUILD_GUIDE.md](./.guides/BUILD_GUIDE.md) |
| 기획서 처리 | [WORKFLOW_PLANNING/INDEX.md](./WORKFLOW_PLANNING/INDEX.md) |
| 작업 추적 | [WORK_IN_PROGRESS.md](./WORK_IN_PROGRESS.md) |
| 빠른 참조 | [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) |
| 코드 스타일 | [.guides/CODE_STYLE.md](./.guides/CODE_STYLE.md) |
| 기술 규칙 | [.guides/TECHNICAL_RULES.md](./.guides/TECHNICAL_RULES.md) |
