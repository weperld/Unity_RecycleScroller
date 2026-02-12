# 기술 규칙

## 필수 규칙

### 타입 안전성
- 무조건 캐스팅 `(Type)cast` 남용 금지
- `as` 연산자 + null 체크 사용 권장
- Generic 타입 제약(`where T :`)으로 컴파일 타임 안전성 확보

### MonoBehaviour 생명주기
- 초기화 순서 준수: `Awake` → `OnEnable` → `Start`
- `OnDestroy`에서 이벤트 구독 해제 및 리소스 정리
- `OnDisable`에서 실행 중인 비동기 작업 취소

### Object Pooling
- 셀은 항상 풀에서 가져오고 반환
- 풀 반환 시 이벤트 핸들러, 콜백 참조 정리 필수
- 풀이 비었을 때만 새 인스턴스 생성 (`CellCreateFuncWhenPoolEmpty`)

### ScrollRect 확장
- `onValueChanged` 이벤트 핸들러에서 `scrollPosition` 변경 시 무한 루프 주의
- 스크롤 위치 정규화(0~1) 범위 준수
- `ContentSizeFitter`와의 충돌 방지

### 에디터/런타임 분리
- `#if UNITY_EDITOR` 프리프로세서로 에디터 전용 코드 감싸기
- `Editor/` 폴더 내 코드는 런타임 어셈블리에서 참조 불가
- `CustomEditor`, `PropertyDrawer`는 반드시 `Editor/` 폴더에 배치

## 라이브러리 사용 규칙

### UniTask
- 모든 비동기 작업은 `UniTask` 사용 (코루틴 대신)
- `CancellationTokenSource`로 작업 취소 지원 필수
- `UniTask.Yield()` / `UniTask.NextFrame()`으로 프레임 대기
- `async void` 사용 금지 → `async UniTaskVoid` 또는 `async UniTask` 사용

### TextMeshPro
- 기존 `UnityEngine.UI.Text` 대신 `TMP_Text` 사용
- 폰트 에셋 누락 시 런타임 에러 발생하므로 참조 확인 필수

### Newtonsoft.Json
- Unity 내장 `JsonUtility` 대신 복잡한 직렬화에 사용
- `[JsonProperty]` 속성으로 직렬화 이름 명시

## 성능 규칙

### GC Allocation 최소화
- `Update()`, `LateUpdate()`에서 `new` 키워드 사용 최소화
- string 연결 시 `StringBuilder` 활용
- LINQ 사용 주의 (박싱, 할당 발생)
- 람다 캡처 최소화 (클로저에 의한 GC 발생)

### 스크롤 성능
- Viewport 밖 셀은 즉시 비활성화 및 풀 반환
- 셀 개수가 많아도 활성 셀은 화면에 보이는 것 + 여유분만 유지
- `RectTransform.SetSizeWithCurrentAnchors()` 호출 최소화

### 메모리
- 대량 데이터 로드 시 페이지 단위 처리
- 미사용 셀 프리팹 참조 정리
- `Resources.UnloadUnusedAssets()` 적절히 활용

## 보안 규칙

### 데이터 무결성
- 원본 데이터 직접 수정 금지 (복사본 사용)
- 인덱스 범위 검증 후 접근
- 외부 입력(JSON 등) 파싱 시 예외 처리 필수

### 참조 안전성
- `null` 체크 후 멤버 접근
- Unity 오브젝트의 `== null` 연산자 특수성 인지 (Destroyed 상태)
- `WeakReference` 고려 (순환 참조 방지)
