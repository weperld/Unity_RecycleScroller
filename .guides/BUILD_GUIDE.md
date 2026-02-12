# 빌드 및 개발 가이드

## 사전 요구사항

- **Unity**: 2022.3.62f2 (LTS)
- **IDE**: Visual Studio 2022 / JetBrains Rider 2024+
- **.NET**: Unity 내장 .NET Standard 2.1
- **Git**: 2.30 이상

## 빌드 방법

### Unity 에디터 빌드
1. Unity Hub에서 프로젝트 열기
2. `File > Build Settings` 메뉴 진입
3. 대상 플랫폼 선택 후 `Build` 클릭

### 어셈블리 단위 빌드 확인
프로젝트는 다음 어셈블리로 분리되어 있습니다:
- `RecycleScroller.Runtime` - 런타임 핵심 코드
- `RecycleScroller.Editor` - 에디터 전용 도구
- `RecycleScroller.Attributes` - 커스텀 속성
- `CustomSerialization` - 직렬화 유틸리티
- `MathUtils` - 수학 유틸리티

### 컴파일 에러 확인
- Unity 에디터 콘솔 (`Window > General > Console`) 에서 빨간색 에러 확인
- `Ctrl+Shift+C`로 콘솔 빠른 접근

## 실행 방법

### Play Mode
1. `Assets/Scenes/SampleScene.unity` 열기
2. `Ctrl+P`로 Play Mode 진입
3. 스크롤러 동작 확인

### 에디터 도구
- `GameObject > UI > RecycleScrollView` - 씬에 스크롤뷰 자동 생성

## 의존성 관리

### Unity Package Manager
`Packages/manifest.json`에서 관리:
- **UniTask**: `https://github.com/Cysharp/UniTask.git`
- **TextMeshPro**: 3.0.9
- **Unity UI (UGUI)**: 1.0.0
- **Newtonsoft JSON**: 3.2.2

### 패키지 복원
1. Unity 에디터 열기 시 자동 복원
2. 문제 발생 시: `Window > Package Manager` 에서 수동 확인

## 배포

### Unity Package 내보내기
1. `Assets > Export Package` 메뉴
2. `RecycleScroll/`, `Editor/`, `Attributes/`, `LoadDataExtension/`, `SerializableDictionary/` 폴더 선택
3. `.unitypackage` 파일로 내보내기

### 주의사항
- `Library/`, `Temp/`, `Logs/` 폴더는 배포에 포함하지 않음
- `.meta` 파일은 반드시 함께 배포

## 문제 해결

### 컴파일 에러
1. `dotnet restore` 또는 Unity 재시작
2. Assembly Definition (.asmdef) 참조 경로 확인
3. 누락된 `using` 문 확인
4. Package Manager에서 의존성 확인

### 런타임 에러
1. `NullReferenceException` - Inspector에서 참조 연결 확인
2. `MissingReferenceException` - 파괴된 오브젝트 참조 확인
3. `IndexOutOfRangeException` - 셀 인덱스 범위 확인

### UniTask 관련
- UniTask 패키지 미설치 시: Package Manager > Add package from git URL
- URL: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
