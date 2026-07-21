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

`Window > Package Manager`에서 **Basic Usage** 샘플을 임포트하면 델리게이트 구현 예제
(`BasicUsageSample.cs`)를 확인할 수 있습니다.

### 셀 크기 소유권

스크롤러는 셀의 `RectTransform`을 수정하지 않습니다. `GetCellRect`가 선언한 크기는
배치 공간 계산에만 쓰입니다.

- 셀마다 크기가 다르면 `GetCell`에서 `cell.UpdateCellSize(...)`로 직접 세팅해야 합니다
- 셀 프리팹에 `localScale`을 줬다면 Inspector의 `Use Child Scale`을 켜고,
  `GetCellRect`에도 스케일을 함께 선언합니다
- `UpdateCellSize`에는 스케일 적용 전 값(`ToUnScaledValues`)을 넣습니다 —
  `ToScaledValues`를 넣으면 `localScale`과 이중 적용됩니다

자세한 내용은 [저장소 README](https://github.com/weperld/Unity_RecycleScroller#cell-size--use-child-scale) 참조

## 라이선스

MIT License - 자세한 내용은 [LICENSE.md](LICENSE.md) 참조
