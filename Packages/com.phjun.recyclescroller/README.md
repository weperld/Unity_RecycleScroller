# Recycle Scroller

Unity용 고성능 재활용 스크롤 시스템. Object Pooling 기반의 효율적인 UI 스크롤러입니다.

## 기능

- Object Pooling 기반 셀 재활용
- 무한 루프 스크롤
- 페이지네이션
- 셀 그룹 배치
- 비동기 데이터 로드 (UniTask)
- Addressables 비동기 셀 프리팹 로더
- 이징 애니메이션
- 커스텀 에디터 인스펙터

## 요구사항

- Unity 2022.3 이상
- **UniTask** (필수 - 사전 설치 필요)

## 설치

### Git URL (권장)

1. UniTask를 먼저 설치합니다:
   - Package Manager > + > Add package from git URL:
   ```
   https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
   ```

2. RecycleScroller를 설치합니다:
   - Package Manager > + > Add package from git URL:
   ```
   https://github.com/weperld/Unity_RecycleScroller.git?path=Packages/com.phjun.recyclescroller
   ```

   또는 `Packages/manifest.json`에 직접 추가:
   ```json
   {
     "dependencies": {
       "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
       "com.phjun.recyclescroller": "https://github.com/weperld/Unity_RecycleScroller.git?path=Packages/com.phjun.recyclescroller"
     }
   }
   ```

### Addressables 지원 (선택)

Addressables 패키지가 설치되어 있으면 `AddressableCellProvider`가 자동으로 활성화됩니다.

## 빠른 시작

1. `GameObject > UI > RecycleScrollView`로 스크롤뷰 생성
2. `IRecycleScrollerDelegate` 인터페이스를 구현하는 스크립트 작성
3. `RecycleScrollerCell`을 상속하는 셀 프리팹 생성
4. 스크롤러에 델리게이트와 셀 프리팹 연결

## 라이선스

MIT License - 자세한 내용은 [LICENSE.md](LICENSE.md) 참조
