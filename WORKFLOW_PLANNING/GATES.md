# Gate 검증 시스템

> 각 개발 단계 간 전환 시 품질을 보장하기 위한 검증 게이트 시스템입니다.

---

## 1단계: 기획서 유형 분석

### 유형 A: 기능 수정 (Feature Fix/Enhancement)

**판단 기준:**
- ✅ 기존 기능의 버그 수정
- ✅ 기존 기능의 성능 개선
- ✅ 기존 기능의 사용성 개선
- ✅ 리팩토링 (기능 변화 없음)
- ✅ 호환성 이슈 수정

**분석 체크리스트:**
- [ ] 영향받는 Feature ID 확인
- [ ] 영향받는 파일 목록
- [ ] 영향받는 UI 요소
- [ ] 호환성 고려사항
- [ ] 기존 데이터 호환성 여부
- [ ] 테스트 방법

**자동 판단 키워드:**
- "버그", "오류", "수정", "개선", "최적화", "리팩토링"

---

### 유형 B: 새로운 기능 추가 (New Feature)

**판단 기준:**
- ✅ 완전히 새로운 카테고리
- ✅ 기존 카테고리의 새로운 컴포넌트
- ✅ 새로운 UI 요소 추가
- ✅ 새로운 데이터 모델 필요
- ✅ 새로운 API 추가

**분석 체크리스트:**
- [ ] 필요한 카테고리 결정 ScrollCore, CellPooling, LoopScroll, Pagination, AsyncLoad, CellGrouping, EasingAnimation, EditorTools
- [ ] 새로운 컴포넌트 필요 여부
- [ ] 새로운 ViewModel 필요 여부
- [ ] 새로운 View 필요 여부
- [ ] 설정 요구사항
- [ ] 템플릿 요구사항
- [ ] UI 흐름

**자동 판단 키워드:**
- "신규", "추가", "생성", "새로운", "기능"

---

## 2단계: 계획 수립

### 🚪 검증 게이트 시스템

```
Plan → [Gate-1] → Design → [Gate-2] → Code → [Gate-3] → Test → [Gate-4] → Docs → [Gate-5] → QA → [Gate-6] → Review → [Gate-7] → 완료
```

#### 검증 게이트 개요

| 게이트 | 검증 대상 | 자체 더블체크 | 크로스체크 에이전트 | 실패 시 롤백 |
|--------|----------|---------------|---------------------|--------------|
| Gate-1 | Plan → Design | analyst 2회 | architect 1회 | Plan 재계획 |
| Gate-2 | Design → Code | architect 2회 | developer 1회 | Plan 재계획 |
| Gate-3 | Code → Test | developer 2회 | tester 1회 | Design 재설계 |
| Gate-4 | Test → Docs | tester 2회 | developer 1회 | Code 재코딩 |
| Gate-5 | Docs → QA | doc-manager 2회 | reviewer 1회 | Test 재테스트 |
| Gate-6 | QA → Review | reviewer 2회 | architect 1회 | Code 재코딩 |
| Gate-7 | Review → 완료 | coordinator 2회 | - | Review 재최종검토 |

#### 게이트 통과 조건

**Gate-1 (Plan → Design)**
- ✅ 계획 명확성 검증
- ✅ 영향 파일 완전성 검증
- ✅ 위험 요소 식별 완료
- ✅ 수렴 완료: 필수 보완 사항 0건 (선택 보완 사항만 잔존)
- ✅ 사용자 승인 완료
- ✅ analyst 1차 검증
- ✅ analyst 2차 검증
- ✅ architect 크로스체크

**Gate-2 (Design → Code)**
- ✅ 순환 참조 없음
- ✅ 스택 오버플로우 안전
- ✅ 성능 O(n) 이하
- ✅ 스레드 안전성 보장
- ✅ 메모리 누수 없음
- ✅ 데이터 무결성 준수
- ✅ UI 프리징 방지
- ✅ 아키텍처 준수
- ✅ 수렴 완료: 필수 보완 사항 0건 (선택 보완 사항만 잔존)
- ✅ architect 1차 검증
- ✅ architect 2차 검증
- ✅ developer 크로스체크

**Gate-3 (Code → Test)**
- ✅ 빌드 성공 (Exit Code 0)
- ✅ 컴파일 에러 0개
- ✅ 컴파일 경고 < 5개 (심각 경고 0개)
- ✅ 참조 에러 0개
- ✅ 코드 스타일 준수
- ✅ 기술 규칙 준수 - [ ] 무조건 캐스팅 (Type)cast 남용하지 않음
- [ ] GetComponent 호출 결과를 캐싱함
- [ ] Update/LateUpdate에서 GC 유발 코드 없음
- [ ] 빈 catch 블록이 없음
- [ ] 에디터 코드가 런타임에 포함되지 않음 (#if UNITY_EDITOR)
- [ ] Object Pool 사용 시 참조 정리 완료
- ✅ developer 1차 검증
- ✅ developer 2차 검증
- ✅ tester 크로스체크

**Gate-4 (Test → Docs)**
- ✅ 빌드 테스트 통과
- ✅ 단위 테스트 통과율 100%
- ✅ 기능 테스트 통과
- ✅ 버그 0개 (또는 문서화된 버그만 존재)
- ✅ tester 1차 검증
- ✅ tester 2차 검증
- ✅ developer 크로스체크

**Gate-5 (Docs → QA)**
- ✅ API 문서 완비
- ✅ 문서 생성 완료
- ✅ 사용자 가이드 완성
- ✅ 변경 로그 작성
- ✅ doc-manager 1차 검증
- ✅ doc-manager 2차 검증
- ✅ reviewer 크로스체크

**Gate-6 (QA → Review)**
- ✅ 코드 스타일 완벽 준수
- ✅ 아키텍처 완벽 준수
- ✅ 잠재적 버그 0개
- ✅ 성능 기준 충족
- ✅ 보안 취약점 0개
- ✅ reviewer 1차 검증
- ✅ reviewer 2차 검증
- ✅ architect 크로스체크

**Gate-7 (Review → 완료)**
- ✅ 모든 게이트 통과
- ✅ 모든 체크박스 완료
- ✅ 사용자 승인
- ✅ coordinator 1차 검증
- ✅ coordinator 2차 검증
- ✅ coordinator 최종 검증

---

### 🔄 수렴 검증 프로토콜

계획/설계 단계에서는 **반복적 수렴 점검**을 수행합니다.
결과물에 누락·모호·위험 요소(필수 보완 사항)가 0건이 될 때까지 점검→반영을 반복하며,
선택적 보완 사항만 남으면 Gate로 진행합니다.

> 이 프로토콜은 "개선" 작업에만 해당하는 것이 아니라, **수렴 대상인 모든 작업**에서
> 결과물의 완전성과 견고성을 확보하기 위한 표준 프로세스입니다.

#### 수렴 프로세스

```
┌─────────────────────────────────────────────┐
│  1. 초기 작업 수행 (수렴 대상 단계)         │
└──────────────────┬──────────────────────────┘
                   ▼
┌─────────────────────────────────────────────┐
│  2. 결과물 점검: 보완 사항 도출             │◄──┐
│     - 누락된 항목, 모호한 정의, 위험 요소   │   │
│     - 구조적 결함, 성능 우려, 확장성 이슈   │   │
│     - 미식별 의존성, 불완전한 분석          │   │
└──────────────────┬──────────────────────────┘   │
                   ▼                               │
┌─────────────────────────────────────────────┐   │
│  3. 보완 사항 분류                          │   │
│     - 필수(MUST): 미해결 시 후속 단계에서   │   │
│       문제 발생 또는 재작업 필요            │   │
│     - 선택(OPTIONAL): 품질 향상에 기여하나  │   │
│       현재 진행에 지장 없음                 │   │
└──────────────────┬──────────────────────────┘   │
                   ▼                               │
           ┌──────────────┐                        │
           │ 필수 > 0건?  │── YES ── 사용자 확인   │
           └──────┬───────┘         후 반영 ────────┘
                  │ NO
                  ▼
┌─────────────────────────────────────────────┐
│  4. 수렴 완료                               │
│     - 잔존 선택 보완 사항 목록 기록         │
│     - Gate 검증으로 진행                    │
└─────────────────────────────────────────────┘
```

#### 분류 기준

| 구분 | 기준 | 예시 |
|------|------|------|
| **필수** | 미해결 시 후속 단계에서 문제/재작업 발생 | 누락된 영향 파일, 모호한 인터페이스 정의, 미식별 의존성, 보안 취약 설계, 불완전한 위험 분석 |
| **선택** | 품질 향상에 기여하나 현재 진행에 지장 없음 | 코드 스타일 선호도, 추가 문서화, 미래 확장성 고려, 대안 아키텍처 검토 |

#### 수렴 기록 형식

각 수렴 반복 시 다음을 기록합니다:

```
### 수렴 반복 #N
- **발견된 보완 사항**: X건 (필수 Y건 / 선택 Z건)
- **필수 보완 사항**:
  1. [내용] → [반영 방법]
  2. ...
- **선택 보완 사항**:
  1. [내용] → [선택으로 분류한 근거]
  2. ...
- **결과**: 필수 보완 사항 반영 완료 / 수렴 달성
```

#### 적용 대상 단계

수렴 검증은 다음 단계에 적용됩니다 (stages.json의 `"convergence": true`):
- **Plan (계획)**: 분석 결과와 작업 계획의 완전성·견고성 수렴
- **Design (설계)**: 아키텍처 설계의 기술적 완전성·견고성 수렴

> 수렴 검증이 적용되지 않는 단계는 Gate 검증과 크로스체크로 품질을 보장합니다.

---

### 기능 수정 계획 템플릿

```markdown
## 📋 기능 수정 계획

### 문제 정의
- **현재 동작**: ...
- **예상 동작**: ...
- **발생 상황**: ...

### 수정 범위
- **영향 파일**:
  - Assets/
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
- **수정 라인**: ...
- **테스트 방법**: ...

### 위험 요소
- [ ] 호환성 문제
- [ ] 데이터 무결성 영향
- [ ] 성능 영향

### 검증 계획
- **더블체크 계획**: 1차 검증 후 2차 검증
- **크로스체크 에이전트**: architect

### 예상 소요 시간
...

### 우선순위
- [ ] High
- [ ] Medium
- [ ] Low
```

---

### 새로운 기능 계획 템플릿

```markdown
## 📋 새로운 기능 추가 계획

### 기능 개요
- **기능 이름**: ...
- **카테고리**: ScrollCore, CellPooling, LoopScroll, Pagination, AsyncLoad, CellGrouping, EasingAnimation, EditorTools
- **설명**: ...

### 구현 파일
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

### 설정
- **필요한 경로 설정**: ...
- **필요한 템플릿**: ...

### UI 변경사항
- **새로운 화면**: ...
- **기존 화면 수정**: ...

### 구현 순서
1. Model 정의
2. 핵심 로직 구현
3. ViewModel 생성
4. View 생성
5. 통합
6. 테스트

### 위험 요소
...

### 검증 계획
- **더블체크 계획**: 각 단계별 1차, 2차 검증
- **크로스체크 에이전트**: architect (Gate-2), reviewer (Gate-3), developer (Gate-4)

### 예상 소요 시간
...
```

---

## 3단계: 사용자 확인

### 확인 형식

```
✅ 계획이 수립되었습니다.

[WorkID]: WIP-YYYYMMDD-NNN
[유형]: (기능 수정 / 새로운 기능)

[계획 요약]
...

[영향 파일]
...

[위험 요소]
...

진행하시겠습니까? (y/n 또는 수정 요청)
```

### 확인 대기 시 에이전트 동작
- 사용자 "y" → 즉시 구현 시작
- 사용자 "n" → 취소 사유 문의
- 사용자 "수정 요청" → 계획 수정 후 재확인

---

## 📚 관련 모듈

- [PIPELINE.md](PIPELINE.md) - 개발 파이프라인
- [AUTO_UPDATE.md](AUTO_UPDATE.md) - WorkID 및 자동 업데이트 시스템
- [ERROR_HANDLING.md](ERROR_HANDLING.md) - 에러 처리 및 롤백 프로토콜
