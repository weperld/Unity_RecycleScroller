# 셀/그룹 이름 설정 `string.Format` — `#if UNITY_EDITOR` 감싸기

- **유형**: 성능 (GC)
- **공수**: 낮음
- **위험도**: 낮음

## 문제

스크롤 중 셀이 보일 때마다 `string.Format` + `GetType().ToString()`으로 이름을 설정.
디버깅 전용이므로 런타임 빌드에서는 불필요한 GC 유발.

## 수정 대상

| 위치 | 코드 |
|------|------|
| `RecycleScroller_LoadData.cs:707` | `getCell.gameObject.name = string.Format("{0}_Index({1})", getCell.GetType().ToString(), j);` |
| `RecycleScroller_Functions.cs:551` | `groupObject.gameObject.name = string.Format("Group({0}), Cell Index({1} ~ {2})", ...);` |
| `RecycleScroller_Functions.cs:556` | `groupObject.gameObject.name = string.Format("Group({0}), No Cells", groupIndex);` |

## 수정 가이드

각 위치를 `#if UNITY_EDITOR` ~ `#endif`로 감싸기.

## 검증 방법

1. 에디터에서 셀/그룹 이름이 기존과 동일하게 표시되는지 확인
2. 빌드 후 해당 코드가 제외되는지 확인 (IL2CPP 빌드 시)
