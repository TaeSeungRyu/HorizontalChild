# 발견 위치 마커 셋업 가이드

발견물을 발견하면 그 좌표에 보물상자(Kenney Pirate Kit) 가 표시됩니다. 탭하면 발견 패널 재실행.

기획서: GAME_PREP.md §12.2 M3 "발견 위치 마커 + 재진입"

---

## 동작

1. 플레이어가 발견물 좌표 근처에서 정박·탐색 → 발견 패널 표시
2. 그 좌표에 **3D 보물상자 마커 spawn** (카테고리별 색)
3. 이후 마커를 **탭** 하면 발견 패널 재실행 (정보 열람, 보상 없음)
4. 저장 시스템 도입 후 — 게임 재시작 시 자동 복원

### 카테고리별 색 (chest 머티리얼 tint)

| 카테고리 | 색 |
|---|---|
| Landmark (랜드마크) | 파랑 |
| FloraFauna (동식물) | 녹색 |
| Ruin (유적) | 갈색 |
| Event (사건) | 노랑-주황 |

원본 chest 색 유지하려면 Spawner 의 `Tint By Category` 해제.

---

## 단계 1 — DiscoveryMarkers GameObject 생성

1. Hierarchy 빈 곳 우클릭 → **Create Empty** → 이름 **`DiscoveryMarkers`**
2. Transform Position (0, 0, 0)
3. Add Component → **`Discovery Marker Spawner`**

## 단계 2 — Spawner 필드 채우기

| 필드 | 값 |
|---|---|
| **Discovery Catalog** | `DiscoveryCatalog` SO 드래그 |
| **Reopen Panel** | 씬의 `DiscoveryFoundPanel` (비워두면 자동 검색) |
| **Markers Parent** | 비워둠 (자동) |
| **Marker Prefab** | **`Assets/kenney_pirate-kit/Models/FBX format/chest.fbx`** 드래그 |
| **Marker Height** | 3 |
| **Marker Scale** | 5 (≈37 km) — 줌 인 카메라면 더 작게, 멀면 더 크게 |
| **Tint By Category** | ☑ 권장 |

## 단계 3 — Main Camera 에 Physics Raycaster 추가 (필수)

마커 클릭 처리에 필요.

1. Hierarchy 의 **`Main Camera`** 클릭
2. Inspector → **Add Component** → 검색 **`Physics Raycaster`** → 추가
3. 기본 설정 그대로 두면 됨

> 이 단계 빼먹으면 마커가 보이지만 클릭이 안 됨. Spawner Start 시 콘솔에 경고 출력됨.

## 단계 4 — Play 테스트

1. ▶ Play
2. 배를 발견물 좌표 근처로 이동 → 정박·탐색
3. 발견 패널 등장 → 닫기
4. **그 위치에 색칠된 보물상자가 떠 있어야 함**
5. **보물상자 탭 → 발견 패널 다시 등장** (보상 없음)

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 마커 자체가 안 보임 | `Marker Prefab` 비어있나 확인 / DiscoveryCatalog 비어있나 (`Game ▸ Refresh All Catalogs`) |
| 마커 너무 큼·작음 | `Marker Scale` 조정 (3~10 사이) |
| 마커가 바다 아래 잠김 | `Marker Height` 키움 (3 → 5 등) |
| 마커는 보이는데 클릭 안 됨 | Main Camera 에 **Physics Raycaster** 추가 (단계 3) |
| 클릭해도 패널 안 뜸 | `Reopen Panel` 필드에 DiscoveryFoundPanel 드래그 |
| 마커 색이 다 같음 | DiscoveryData 의 `category` 필드 확인 (4 카테고리 분산되어 있어야) |
| 마커 색이 너무 진해서 chest 모양 안 보임 | `Tint By Category` ☐ 해제 → 원본 chest 색 유지 |

---

## 시각 커스터마이즈

### 다른 모델 사용
Spawner 의 `Marker Prefab` 에 다른 prefab 드래그.

후보:
- Kenney Pirate Kit: `chest`, `barrel`, `flag-pirate`
- 자체 제작 prefab
- 동물·식물 모델 (카테고리별 다른 prefab 으로도 가능 — 그러려면 코드 확장 필요)

### 색 변경
[`DiscoveryMarker.cs`](DiscoveryMarker.cs) 의 `ColorFor` 메서드 수정.

### Tint 끄기
`Tint By Category` ☐ → 모든 마커 원본 chest 색. 카테고리 구분은 좌표나 발견 시 패널에서 확인.

---

## 저장 시스템 연동 (추후)

현재는 메모리 only. 저장 시스템 도입 시:
1. 로드 시 MissionService.DiscoveredIds 를 JSON 에서 복원
2. Spawner.Start() 의 `foreach (var id in DiscoveredIds) Spawn(...)` 가 자동 처리

코드는 이미 준비되어 있어 추가 작업 불필요.
