# 다음 예정 작업 목록

> 작성일: 2026-02-14
> 상태: 미착수

---

## Task 1. Elastic 핸들 사이즈 조정 동작 구현

### 비루프 모드
- Unity Scrollbar처럼 Elastic 모드에서 끝을 넘어 드래그 시, 핸들이 멈추는 것이 아닌 **핸들 RectSize가 줄어드는 형태**로 피드백 제공
- 참고: `UnityEngine_ScrollRect.cs` → `UpdateScrollbars()` 메서드
  - `size = Clamp01((ViewSize - Abs(offset)) / ContentSize)`

### 루프 모드
- 현재: 서브 핸들이 메인 핸들에서 일정 간격 떨어져 **위치만 이동**하는 방식
- 변경: 드래그 방향에 따라 핸들 **사이즈가 전환**되는 방식
  - 케이스 1: 좌측 핸들 0→최대 증가, 우측 핸들 최대→0 감소
  - 케이스 2: 좌측 핸들 최대→0 감소, 우측 핸들 0→최대 증가

---

## Task 2. 루프 모드 로직 객체화

- RecycleScroller, RecycleScrollbar 모두에서 루프 모드 관련 **분기 처리(`if (loopMode)`)를 별도 객체로 분리**
- 전략 패턴 등을 활용하여 모드별 객체가 동작을 담당하도록 구조 변경
- 참고: `UnityEngine_ScrollRect.cs` → `MovementType` 분기 패턴 (Unrestricted/Elastic/Clamped)

---

## Task 3. 전체 모드 관련 로직 객체화 검토

- Task 2 완료 후 수행
- 루프 모드 외에도 **모드에 의존하는 로직**이 있는지 전체 점검
- 가능한 부분은 동일하게 객체화하여 일관된 구조로 통일

---

## Task 4. 에디터 오픈 시 패키지 버전 체크 및 업데이트 팝업

- RecycleScroller 패키지가 설치된 프로젝트에서 Unity 에디터 오픈 시 **GitHub 최신 릴리즈 버전을 확인**
- 현재 설치된 버전과 최신 버전이 불일치할 경우 **업데이트 안내 팝업** 표시
- Editor 전용 코드 (빌드에 포함되지 않음)
- 구현 고려 사항:
  - `[InitializeOnLoad]` 또는 `SessionState` 기반으로 에디터 시작 시 1회 체크
  - GitHub API (`releases/latest`) 또는 Git 태그로 최신 버전 조회
  - 팝업에서 바로 UPM 업데이트 가능하도록 안내 (Git URL 복사 등)

---

## 참조 파일

| 파일 | 용도 |
|------|------|
| `.reference/UnityEngine_ScrollRect.cs` | Unity ScrollRect 소스 (Elastic offset, UpdateScrollbars, RubberDelta) |
| `.reference/UnityEngine_Scrollbar.cs` | Unity Scrollbar 소스 (Set, UpdateVisuals, UpdateDrag) |
