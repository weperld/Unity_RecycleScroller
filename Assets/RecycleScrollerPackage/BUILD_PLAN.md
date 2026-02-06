# RecycleScroller Unity Package - 구축 계획

## 목표
RecycleScroller 컴포넌트를 별도의 유니티 패키지로 분리하여 깃허브 원격 리포지토리로 관리

---

## 구축 단계

### ✅ 1. 패키지 디렉토리 구조 생성
```
com.tikitaka-studio.recyclescroller/
├── Runtime/
│   ├── RecycleScroll/
│   │   ├── (RecycleScroller 소스 파일들)
│   │   ├── MathUtils.cs
│   │   └── EasingFunctions.cs
│   └── RecycleScroller.Runtime.asmdef
├── Editor/
│   ├── Attributes/
│   │   ├── ColoredHeaderAttribute.cs
│   │   ├── ColoredHeaderDrawer.cs
│   │   ├── HelpBoxAutoAttribute.cs
│   │   ├── HelpBoxAutoDrawer.cs
│   │   ├── HorizontalLineAttribute.cs
│   │   ├── HorizontalLineDrawer.cs
│   │   └── EditorDrawerHelper.cs
│   ├── Drawers/
│   │   ├── BoolVector2Drawer.cs
│   │   ├── LoopScrollbarDrawer.cs
│   │   ├── MinMaxIntDrawer.cs
│   │   ├── MinMaxFloatDrawer.cs
│   │   ├── MinMaxLongDrawer.cs
│   │   ├── MinMaxDoubleDrawer.cs
│   │   ├── RecycleScrollerContentEditor.cs
│   │   └── ScrollPagingConfigDrawer.cs
│   ├── RecycleScroll/
│   │   ├── RecycleScroller_OnValidate.cs
│   │   ├── RecycleScroller_Inspector.cs
│   │   └── RecycleScrollerEditor.cs
│   ├── Creator/
│   │   └── RecycleScrollViewCreator.cs
│   └── RecycleScroller.Editor.asmdef
└── Samples~/Examples/
    ├── Basic/
    └── Group/
```

---

### ✅ 2. 파일 복사 및 의존성 정리
- ✅ Runtime/RecycleScroll/에 13개 RecycleScroller 파일 복사
- ✅ Editor/에 22개 파일 복사
- ✅ MathUtils.cs 복사 (MinMaxInt, MinMaxFloat 등 포함)
- ✅ EasingFunctions.cs 복사 (Ease enum, EasingFunction 포함)

**의존성 포함**:
- MathUtils.cs → 패키지 내부 포함 (RecycleScroll namespace)
- EasingFunctions.cs → 패키지 내부 포함 (RecycleScroll namespace)

---

### ⚠️ 3. Unity 프로젝트 생성 (필요 시)
새 유니티 프로젝트에서 패키지 테스트를 위해 필요할 수 있음

---

### ⚠️ 4. Assembly Definitions 생성
**파일 위치**: `Runtime/RecycleScroller.Runtime.asmdef`, `Editor/RecycleScroller.Editor.asmdef`

**Runtime.asmdef**:
```json
{
  "name": "RecycleScroller.Runtime",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "autoReferenced": true
}
```

**Editor.asmdef**:
```json
{
  "name": "RecycleScroller.Editor",
  "references": ["RecycleScroller.Runtime", "UnityEditor.UI"],
  "includePlatforms": ["Editor"],
  "autoReferenced": true
}
```

---

### ⚠️ 5. package.json 생성
**파일 위치**: `package.json` (패키지 루트)

**내용**:
```json
{
  "name": "com.tikitaka-studio.recyclescroller",
  "version": "1.0.0",
  "displayName": "Recycle Scroller",
  "description": "High-performance cell recycling scroll view for Unity UI",
  "unity": "2022.3",
  "keywords": ["ui", "scrollview", "recycling", "performance"],
  "author": {
    "name": "Tikitaka Studio",
    "email": "contact@tikitaka-studio.com"
  },
  "dependencies": {
    "com.cysharp.unitask": "2.5.0"
  },
  "samples": [
    {
      "displayName": "Basic Usage",
      "description": "Basic RecycleScroller example",
      "path": "Samples~/Examples/Basic"
    },
    {
      "displayName": "Group Usage",
      "description": "RecycleScroller with grouping",
      "path": "Samples~/Examples/Group"
    }
  ]
}
```

---

### ⚠️ 6. 샘플 작성
**파일 위치**: `Samples~/Examples/Basic/`, `Samples~/Examples/Group/`

**Basic 샘플**: 기본 RecycleScroller 사용 예제
**Group 샘플**: 그룹화된 셀 사용 예제

---

### ⚠️ 7. README 작성
**파일 위치**: `README.md` (패키지 루트)

**내용**: 패키지 사용법, 설치 방법, API 문서

---

### ⚠️ 8. Git 리포지토리 생성
**작업**:
- 깃허브에 새 리포지토리 생성
- 패키지 파일 푸시
- 태그 생성 (v1.0.0 등)

---

## Git URL 패키지 설치 방법

### Unity 패키지 매니저에서 설치
```
Window → Package Manager → "+" → "Add package from git URL"
```

### Git URL (예시)
```
https://github.com/tikitaka-studio/recyclescroller.git
```

### 버전 지정 (예시)
```
https://github.com/tikitaka-studio/recyclescroller.git#1.0.0
https://github.com/tikitaka-studio/recyclescroller.git#v1.0.0
```

> **참고**: 위 Git URL은 예시입니다. 실제 리포지토리는 사용자가 직접 생성한 후 URL을 입력하십시오.

---

## 현재 진행 상태

| 단계 | 상태 |
|------|------|
| **1. 패키지 디렉토리 구조 생성** | ✅ 완료 |
| **2. 파일 복사 및 의존성 정리** | ✅ 완료 |
| **3. Unity 프로젝트 생성** | ⚠️ 대기 중 |
| **4. Assembly Definitions 생성** | ⚠️ 대기 중 |
| **5. package.json 생성** | ⚠️ 대기 중 |
| **6. 샘플 작성** | ⚠️ 대기 중 |
| **7. README 작성** | ⚠️ 대기 중 |
| **8. Git 리포지토리 생성** | ⚠️ 대기 중 |

---

## 다음 작업
1. 새 유니티 프로젝트 생성
2. 생성된 프로젝트로 패키지 폴더 복사
3. asmdef 파일 생성
4. package.json 생성
5. 샘플 파일 작성
6. README 작성
7. Git 리포지토리 푸시
