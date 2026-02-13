# 기획서 템플릿

> Unity_RecycleScroller 기능 개발을 위한 표준 기획서 포맷

---

## 1. 기획 유형

- [ ] 기능 수정 (Feature Fix/Enhancement)
- [ ] 새로운 기능 (New Feature)

---

## 2. 배경

### 현재 문제
...

### 사용자 니즈
...

### 목표
...

---

## 3. 상세 요구사항

### 기능적 요구사항 (FR)
- **FR-001**: ...
- **FR-002**: ...

### 비기능적 요구사항 (NFR)
- **NFR-001**: 성능 (처리 시간 X초 이내)
- **NFR-002**: 사용성 (3단계 내 완료)
- **NFR-003**: 안정성 (에러 처리)

---

## 4. 구현 범위

### 포함
...

### 제외
...

---

## 5. UI/UX 변경사항

### 화면 추가/수정
- [ ] 새로운 화면: ...
- [ ] 기존 화면 수정: ...

### 사용자 시나리오
1. 사용자가 ...
2. ...
3. ...

---

## 6. 데이터 모델 변경

### 새로운 모델
```
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
```

### 기존 모델 수정
- [ ] 모델 파일 수정
- [ ] 스키마 정의 수정

---

## 7. API 변경

### 새로운 API
```
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
```

### 기존 API 수정
- [ ] Service 파일 수정
- [ ] ...

---

## 8. 설정 파일

### 필요한 경로 설정
```json
{
  "paths": {
    "dataPath": "...",
    "exportPath": "...",
    "configPath": "..."
  }
}
```

### 필요한 템플릿
- [ ] 템플릿 파일 경로
- [ ] 템플릿 변수: ...

---

## 9. 구현 순서

1. [ ] Model 정의
2. [ ] Processor 구현
3. [ ] ViewModel 생성
4. [ ] View 생성
5. [ ] 통합
6. [ ] 테스트

---

## 10. 테스트 계획

### 단위 테스트
- [ ] 기능 X 테스트
- [ ] 기능 Y 테스트

### 통합 테스트
- [ ] 전체 흐름 테스트
- [ ] UI 연동 테스트

### 사용자 테스트
- [ ] 사용자 승인
- [ ] 버그 리포트

---

## 11. 릴리즈 계획

### 우선순위
- [ ] High (즉시 필요)
- [ ] Medium (다음 릴리즈)
- [ ] Low (향후 고려)

### 일정
- **시작 예정**: ...
- **완료 예정**: ...
- **릴리즈 예정**: ...

---

## 12. 위험 요소

| 위험 | 영향 | 확률 | 대응책 |
|------|------|------|--------|
| ... | ... | ... | ... |
| ... | ... | ... | ... |

---

## 13. 메모

추가로 고려할 사항...
