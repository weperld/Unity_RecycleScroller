# 전체 코드 리뷰 결과 (2026-07-17)

4개 영역(코어/스크롤바·유틸/에디터/패키지) 전수 리뷰. 완료 시 체크.

## 수정/유지 결정 (2026-07-17)

각 항목 앞 마커: `[O]` 수정 확정 / `[X]` 유지(수정 안 함) 결정.

- 1순위 A (품질—사용자 측면): **전부 수정**
- 1순위 B (품질—개발 측면): 미사용 코드 대량 삭제만 **유지(X)**, MinMax 드로어 통합 / FindVisibleGroupIndices 성능 / 조건식 파서 정리는 **수정(O)**
- 2순위 (버그): **전부 수정**
- 기타: **전부 수정**

## 🔴 Critical — RecalculateForInsert (RecycleScroller_Functions.cs:1015-1056)

- [ ] `[O]` **[critical] AddToEnd → 항상 KeyNotFoundException** — `Insert(m_cellCount, n)`의 `targetCellIndex = 이전 셀 개수`는 `m_dict_groupIndexOfCell`에 없는 키(키는 0~cellCount-1). 비루프 모드에서 기존 데이터 1개 이상이면 "맨 뒤 추가"가 무조건 예외. → 끝 추가 시 전체 재계산 폴백 또는 마지막 그룹부터 재계산
- [ ] `[O]` **[major] 그룹당 셀 2개 이상이면 중복 키 ArgumentException** (1035-1045행) — 딕셔너리를 `targetCellIndex` 미만으로 자르고 `groupStartIndex`부터 재추가 → 범위 겹침. `m_fixedCellCount==1`일 때만 우연히 통과. → 자르는 기준을 `groupStartIndex` 미만으로 통일
- [ ] `[O]` **[major] 페이징 사용 시 잘린 딕셔너리 조회** (1038-1042행) — 자른 뒤 `FindPageIndex_FromCellIndex(targetCellIndex)` 호출 → 사라진 키 조회. 삭제 시 -1 → `RemoveRange(-1, …)`. → 페이지 인덱스를 자르기 전에 계산 또는 `FindPageIndex_FromGroupIndex` 전환

## 🟠 품질 — 패키지 사용자 측면 (1순위 A — 전부 수정)

- [ ] `[O]` **[major] Newtonsoft.Json 의존성 미선언** (package.json) — `JsonHelper.cs:10`이 무조건 사용, dependencies엔 `com.unity.ugui`뿐 → 미설치 프로젝트 컴파일 실패. 권장: `MinMax*`는 단순 구조체라 `JsonUtility.FromJson`으로 대체 → 의존성+빈 catch+JsonHelper 파일 일괄 제거
- [ ] `[O]` **[major] Samples~/BasicUsage가 빈 껍데기** — 빈 씬 하나뿐(스크립트·셀 프리팹·README 없음). 실동작 예제는 Assets/Test/에 있어 미포함. → 예제 이관 또는 samples 선언 제거, README 빠른시작 보강
- [ ] `[O]` **[major] Addressables 하드 참조 vs README "선택" 모순** (Runtime asmdef) — `Unity.Addressables`/`UniTask.Addressables`/`Unity.ResourceManager` 무조건 참조 → 미설치 시 에러. → 별도 asmdef(defineConstraints: com.unity.addressables) 분리
- [ ] `[O]` **[major] MinMax*Drawer 4종 "Set All" 입력값 폐기** — min≠max에서 Set All 입력 시 입력값 버려지고 min=max 붕괴 (Float:39-45, Double:38-46, Int:36-44, Long:37-45)
- [ ] `[O]` **[major] MinMaxInt/Long sentinel 노출** — min≠max일 때 "Set All" 칸에 `-2147483648` 그대로 표시. → `EditorGUI.showMixedValue` 사용
- [ ] `[O]` **[major] RecycleScrollViewCreator.cs:54-55 프리팹 연결 끊김 + null 미검사** — `Instantiate` → `PrefabUtility.InstantiatePrefab` 교체, prefab null 가드 추가
- [ ] `[O]` **[major] PushAllActivatedCells 수명주기 비대칭** (_Functions.cs:98-109) — 리로드/클리어 회수 경로에서 `OnCellBecameInvisible` 콜백 누락 → 사용자 구독/리소스 해제 패턴에서 상태 누수
- [ ] `[O]` **[minor] RS_LDE_* 네이밍** — 약어 설명 부재, `2` 접미사 의미 불명. 문서화 또는 리네이밍(`GroupCountAlignmentOverride` 류)
- [ ] `[O]` **[minor] 공개 API 소문자** — `scroller.del`, `scrollbar._Direction`, `scrollbar.Del` — PascalCase 위반. 변경 시 참조 전수 조사 필수

## 🟠 품질 — 개발 측면 (1순위 B — 미사용 코드만 유지)

- [ ] `[X]` ~~**[major] EasingFunctions.cs 파생(Derivative) 리전 전량 미사용** (433-775행 등, ~350줄)~~ — **유지 결정** (삭제 안 함)
- [ ] `[X]` ~~**[major] MathUtils.cs Derivative/Integrate/GetGradient 전량 미사용** (12-157행, ~145줄)~~ — **유지 결정** (삭제 안 함)
- [ ] `[X]` ~~**[minor] EditorDrawerHelper.cs 죽은 코드 4개** (124-166, 206-225행)~~ — **유지 결정** (삭제 안 함)
- [ ] `[O]` **[major] MinMax*Drawer 4종 90% 중복** — 제네릭 베이스로 통합(ReadValue/WriteValue/DrawField만 추상화) → Set All 버그 수정이 1곳으로 수렴
- [ ] `[O]` **[major] FindVisibleGroupIndices O(groupCount) 선형 스캔** (_Functions.cs:760-785) — 매 스크롤 호출. 정렬 리스트라 이진 탐색 가능. GC는 없음, 순수 시간복잡도
- [ ] `[O]` **[minor] RS_LDE_...GroupCount2.cs 조건식 파서 과설계** (66-350행) — 정수 하나 비교에 재귀 하향 파서 ~300줄. `"10 >"`→`groupCount < 10` 역직관 매핑. 미사용 `ExpressionParser.Evaluate(int)` 제거

## 🟡 버그 — 비동기/경계값 (2순위 — 전부 수정)

- [ ] `[O]` **[major] AddressableCellProvider.PreloadCellPrefabsAsync CancellationToken 없음 + 해제 후 사용** (76-89행) — await 중 파괴 시 해제된 애셋 유령 참조. → 토큰 파라미터 + await 후 `this == null` 가드 (destroyCancellationToken 활용)
- [ ] `[O]` **[major] RecycleScrollerHelper 코루틴 브리지 hang** (8-34행) — 소유자 파괴 시 tcs 미완료 → await 영구 대기 (_LoadData.cs:224 경로). → UniTask 내장 `WaitForEndOfFrame(mb, token)`으로 대체하면 파일 삭제 가능
- [ ] `[O]` **[minor] MoveTo_Base 0 나누기 NaN** (_Functions.cs:614) — 콘텐츠<뷰포트면 RealScrollSize==0 → NaN으로 Content 오염. RealScrollPosition 세터처럼 `>0f` 가드
- [ ] `[O]` **[minor] 빈 데이터에서 MoveToIndex 예외** (_Functions.cs:67-72) — Count==0이면 Clamp(0,0,-1)=-1 → 딕셔너리 [-1] 조회. `m_list_cellSizeVec` 범위 미검사(730,737,759,787행)도 동일 계열. → cellCount==0 조기 반환
- [ ] `[O]` **[minor] SerializableDictionary.cs:136 중복 키 역직렬화 크래시** — ToDictionary가 ArgumentException. 인스펙터에서 기존 키를 같은 값으로 직접 수정하면 발생 가능. → 방어적 구성 + LogWarning
- [ ] `[O]` **[minor] LoopScrollbarMode.cs:44-47 콘텐츠<뷰포트 시 movementScale 음수** — `Mathf.Max(0f, 1f - naturalSize)` 방어

## ⚪ 기타 (전부 수정)

- [ ] `[O]` [minor] JsonHelper.cs:13-16 빈 catch (절대규칙 위반) — JsonUtility 전환 시 자연 제거
- [ ] `[O]` [minor] 직접 캐스트 (규칙 위반) — RecycleScrollerEditor.cs:162 `(RecycleScroller)target`, SerializableDictionaryDrawer.cs:463,465 등 → as+null 체크
- [ ] `[O]` [minor] RecycleScrollbar.cs:492 OnDrag마다 메서드 그룹→델리게이트 할당(GC) — `Action<float>` 필드 캐싱
- [ ] `[O]` [minor] MinMax*Drawer + BoolVector2Drawer labelWidth 복원 안 함 — 전역 상태 오염
- [ ] `[O]` [minor] SerializableDictionaryDrawer.cs:415,425 콜백마다 드로어/Dictionary new — 인스턴스 캐싱
- [ ] `[O]` [minor] EditorDrawerHelper.cs:32-37,62-64,84-88 매 호출 new GUIStyle — static 캐싱
- [ ] `[O]` [minor] SerializableDictionaryDrawer.cs:343,367 정상 조작에 Debug.Log 스팸 — 제거
- [ ] `[O]` [minor] HelpBoxAutoDrawer.cs:18 높이 계산 폭 불일치 — 긴 메시지 잘림 가능
- [ ] `[O]` [minor] RecycleScrollViewCreator MonoBehaviour 상속 불필요 + Canvas 신규 생성 시 EventSystem 미생성
- [ ] `[O]` [minor] RecycleScroller_LoadData.cs:220,285,340-346 `m_loadDataTaskCompletionSource` 데드 필드 — 제거
- [ ] `[O]` [minor] RecycleScrollbar.cs:518-521 미호출 `ClickRepeat(PointerEventData)` 오버로드 — 삭제
- [ ] `[O]` [minor] RecycleScrollbar.cs:551 자기 자신 StopCoroutine no-op — 제거
- [ ] `[O]` [minor] RecycleScrollbar.cs:314-318 OnDisable에서 `m_isPointerDownAndNotDragging` 미초기화
- [ ] `[O]` [minor] EasingFunctions.cs:838-857 도달 불가 null 가드 + 낡은 주석 (807행) — 파일은 유지하되 해당 라인만 정리
- [ ] `[O]` [minor] MathUtils.cs:86-88 지역 함수 XML doc 주석 → CS1587 경고 — 파일은 유지하되 주석만 수정
- [ ] `[O]` [minor] EditorDrawerHelper.cs:340-341 DrawSmallCategory DrawRect Repaint 가드 누락
- [ ] `[O]` [minor] PackageVersionChecker User-Agent 미설정 — rate-limit 시 알림만 조용히 안 뜸 (저우선)

## 잘 되어 있는 부분 (참고)

- 비동기 로드 스레드 안전: Unity API를 메인스레드에서 캐싱 후 스레드풀 진입 (_LoadData.cs:301-331)
- callID 기반 취소/최신성 관리 정확 (_LoadData.cs:244-260,334-346)
- UpdateCellView 프레임당 GC 할당 0
- 스크롤바 0-나누기 가드, 이벤트 누수 없음, VersionCheck 예외 처리 graceful

## 권장 착수 순서 (O 항목 기준)

1. RecalculateForInsert 재작성 (critical+major 3건 일괄)
2. 패키지 신뢰성 3건 (Newtonsoft→JsonUtility, 샘플, asmdef)
3. MinMax 드로어 통합 + Set All 버그
4. 버그(비동기/경계값) 6건 + 기타 minor 일괄
