# Unity_RecycleScroller

## 프로젝트 개요
- **기술 스택**: C#, Unity 2022.3.62f2, UGUI, UniTask
- **라이브러리**: UniTask, TextMeshPro 3.0.9, Newtonsoft.Json 3.2.2
- **출력 포맷**: UPM 패키지 (Git URL), Unity Package (.unitypackage)
- **상세 정보**: PROJECT_SUMMARY.md 참조

### 프로젝트 구조
Packages/com.phjun.recyclescroller/
├── Runtime/          # 런타임 코드 (RecycleScroller.Runtime.asmdef)
│   ├── RecycleScroll/ # 핵심 스크롤러 (partial class)
│   ├── Attributes/
│   ├── LoadDataExtension/
│   └── SerializableDictionary/
├── Editor/           # 에디터 전용 (빌드 미포함)
└── Samples~/         # 샘플 (컴파일 제외)

## 절대 규칙
- as 연산자 + null 체크 사용. 무조건 (Type)cast 금지
- 빈 catch 블록 금지
- 추측 금지 — 모호한 요청은 사용자에게 확인

## Unity/C# 규칙
- Object Pooling 필수, 셀은 항상 재활용
- GetComponent 결과 캐싱 필수
- Update()에서 GC 유발 코드 금지 (string 연결, LINQ 등)
- 에디터/런타임 분리 (#if UNITY_EDITOR)
- ScrollRect 기반 확장, MonoBehaviour 상속
- 비동기: UniTask + CancellationToken

## 명명 규칙
- 클래스/메서드/프로퍼티: PascalCase
- private 필드: m_camelCase
- 인터페이스: IPrefix
- partial class: ClassName_Responsibility.cs
- 상수: UPPER_SNAKE_CASE

## 빌드/실행
- 빌드: Unity Editor Build Settings 또는 dotnet build
- 테스트: Window > General > Test Runner
- 실행: Editor Play Mode (Ctrl+P)

## 작업 방식
모든 코드 변경은 커스텀 명령어로 시작한다.
- 대규모 (새 기능, 아키텍처 변경): `/project:신규` 또는 `/project:수정`
- 중소규모 (버그 수정, 리팩토링): `/project:간편`
- 명령어 없이 직접 수정 요청 시, 규모를 판단하여 적절한 명령어 사용을 안내한다.
- 명령어 목록: `/project:명령어`
