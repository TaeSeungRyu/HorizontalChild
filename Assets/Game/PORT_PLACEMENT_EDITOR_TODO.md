# 항구·발견물 위치 에디터 — 셋업 가이드

런타임 (Play 모드) 에서 항구·발견물 위치를 마우스로 옮길 수 있는 개발 툴. 변경은 SO 에셋에 즉시 저장.

> ⚠ **Unity Editor Play 모드 전용** — 빌드(.exe/.apk) 에서는 저장 부분이 동작 안 함 (AssetDatabase 가 빌드에 없음).

---

## A. PortPlacementEditor GameObject 추가

1. Hierarchy 빈 공간 우클릭 → Create Empty
2. 이름 `PortPlacementEditor` 로 변경
3. Inspector → Add Component → **Port Placement Editor**

## B. 필드 할당

Inspector 의 필드:

| 필드 | 값 |
|---|---|
| **Port Catalog** | `Assets/Game/Data/_Catalogs/PortCatalog.asset` 드래그 |
| **Discovery Catalog** | `Assets/Game/Data/_Catalogs/DiscoveryCatalog.asset` 드래그 |
| **Main Camera** | 메인 카메라 (Hierarchy 의 Main Camera) 드래그. 비우면 자동 검색 |
| **Player Ship** | 플레이어 배 GameObject 드래그 (입력 잠금용) |
| **Label Font** | `Assets/Game/Art/Pretendard-Regular SDF.asset` 드래그 (한글) |
| **Enable On Start** | **true** 로 체크 → Play 시 자동 활성 |

옵션 튜닝 필드 (기본값 OK):
- Pan Speed: 0.5
- Zoom Speed: 50
- Handle Size: 10
- Handle Y: 5
- Label Font Size: 6

## C. 사용법

### Play 후
- 모든 항구는 **파랑 구** 핸들로 표시
- 모든 발견물은 **노랑 구** 핸들로 표시
- 각 핸들 위에 한글 이름 라벨

### 조작
| 동작 | 효과 |
|---|---|
| **핸들 좌클릭** | 선택 (초록색으로 변함) |
| **핸들 좌클릭 + 드래그** | 그 핸들을 새 위치로 이동 |
| **마우스 놓기** | 위치를 SO 에셋에 즉시 저장 (Console 로그 확인) |
| **우클릭 + 드래그** | 카메라 팬 (자유 이동) |
| **마우스 휠** | 카메라 줌 (Y 높이 ↑/↓) |

### 비활성화
- Hierarchy 의 PortPlacementEditor 컴포넌트 우클릭 → **Disable Editor Mode**
- 또는 enableOnStart 끄고 Play 재시작

## D. 저장 확인

- 핸들을 드래그하고 놓으면 Console 에 로그:
  > `[PortPlacementEditor] 세우타 → lat 35.91, lng -5.50 저장.`
- Play 종료해도 변경된 위치 유지 — 다시 Play 하면 같은 위치
- PortData / DiscoveryData 에셋 파일이 dirty 표시됨 (저장 아이콘) → Ctrl+S 또는 자동 저장

## E. 사전 요구

- 씬에 **EventSystem** 존재해야 함 (UI 클릭과 동일)
- 메인 카메라에 **Physics Raycaster** 컴포넌트 추가 필요
  - Hierarchy → Main Camera → Add Component → Physics Raycaster
  - 이미 NpcShip 클릭이 동작한다면 이미 설정돼 있음

## F. 문제 해결

| 증상 | 원인 | 해결 |
|---|---|---|
| 핸들 안 보임 | Catalog 미할당 | B 단계 |
| 핸들 클릭해도 반응 X | Physics Raycaster 없음 | E 단계 |
| 라벨 글자 안 보임 | TMP 폰트 미할당 | B 단계 Label Font |
| 위치가 저장 안 됨 | 빌드 모드라 그럼 | Unity Editor Play 모드에서만 작동 |
| 카메라 팬 너무 빠름/느림 | Pan Speed 조정 | Inspector 의 Pan Speed 값 |
| 핸들이 너무 크/작음 | Handle Size | Inspector 의 Handle Size 값 |

## G. 작업 절차 예시

1. Inspector 에 PortPlacementEditor 셋업 후 Play
2. 우클릭 드래그로 카메라를 지중해 쪽으로 이동
3. 휠로 줌인 — 세우타 항구 보임
4. 세우타 핸들 클릭 (초록색으로) → 좌클릭 드래그로 약간 동쪽 이동 → 마우스 놓기
5. Console "[PortPlacementEditor] 세우타 → lat 35.92, lng -5.30 저장." 확인
6. 다른 항구들도 옮기기
7. Play 중지 → Ctrl+S → 변경 확정
8. 다시 Play → 새 위치 그대로

## H. 주의

- **NPC 들이 멈춤** — SeaSimulation.Pause 가 작동. 편집 끝나면 자동 재개
- **NPC 라벨도 함께 떠 있음** — 시각적으로 복잡할 수 있음. 잠시 NpcSpawner.showLabels = false 로 두면 깔끔
- **카메라 위치도 변함** — 편집 중 카메라가 이동한 위치는 게임 종료 시 Reset 되지 않음. 필요하면 수동으로 원위치
- **Undo 안 됨** — 한 번 옮기면 즉시 저장. 실수하면 git 또는 Unity 의 Version Control 로 복구
