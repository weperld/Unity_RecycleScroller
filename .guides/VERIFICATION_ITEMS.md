# 프로젝트 검증 항목 (Verification Items)

> 과거 작업에서 발생한 문제를 기반으로 축적된 검증 항목입니다.
> 모든 코드 변경 작업(파이프라인 사용 여부와 관계없이) 완료 전에 해당 항목을 확인해야 합니다.
> 새로운 문제가 발생할 때마다 이 문서에 항목을 추가하세요.

---

## 검증 체크리스트

### C# / Unity 컴파일

- [ ] nullable 타입(`T?`)을 `await`할 때 `.Value` 접근이 필요한지 확인
- [ ] 커스텀 메서드명이 Unity 내장 이벤트(`OnBecameVisible`, `OnBecameInvisible`, `OnEnable`, `OnDisable` 등)와 충돌하지 않는지 확인
- [ ] `MonoBehaviour` 상속 클래스에서 Unity 매직 메서드 시그니처를 의도치 않게 오버라이드하지 않는지 확인

### 파일 이동 / 구조 변경

- [ ] 파일 이동 후 컴파일 에러가 0건인지 확인
- [ ] `.meta` 파일이 함께 이동되었는지 확인 (Unity GUID 보존)
- [ ] `.gitignore`가 필수 추적 파일(`.meta` 등)을 차단하지 않는지 확인

### UPM 패키지

- [ ] `package.json`의 내용이 유효한 JSON이고 필수 필드가 포함되어 있는지 확인
- [ ] `.asmdef` 파일의 참조(`references`)가 올바른지 확인
- [ ] `Samples~/` 폴더가 컴파일에서 제외되는지 확인

### 이름 변경 (Rename / Replace All)

- [ ] `replace_all`로 식별자를 변경할 때 복합어(compound word)가 오염되지 않았는지 확인 (예: `EqualityType`→`eEqualityType` 시 `ConvertStringToEqualityType`→`ConvertStringToeEqualityType` 오염)
- [ ] enum 접두사(`e`) 추가 시 이미 접두사가 있는 식별자에 이중 적용(`eeXxx`)되지 않았는지 확인

### 파일 생성 / 수정

- [ ] 생성한 파일에 플레이스홀더(`<user>`, `TODO`, `FIXME` 등)가 남아있지 않은지 확인
- [ ] URL, 경로 등이 실제 값으로 채워져 있는지 확인

---

## 항목 추가 규칙

새로운 문제가 발생했을 때 다음 형식으로 추가합니다:

```markdown
### [카테고리]

- [ ] [검증 항목 설명]
```

**추가 기준**: 동일한 실수가 반복될 가능성이 있는 경우에만 추가합니다. 일회성 오타나 단순 실수는 추가하지 않습니다.
