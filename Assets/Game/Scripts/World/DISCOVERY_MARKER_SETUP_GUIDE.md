# 발견 위치 마커 셋업 가이드

발견물을 발견하면 그 좌표에 영구 마커가 표시됩니다. 마커 탭 시 발견 패널 재실행.

기획서: GAME_PREP.md §12.2 M3 "발견 위치 마커 + 재진입"

---

## 동작

1. 플레이어가 발견물 좌표 근처에서 정박·탐색 → 발견 패널 표시
2. **그 좌표에 영구 마커 spawn** (색: 카테고리별)
3. 이후 그 마커를 **탭** 하면 발견 패널이 다시 뜸 (정보 열람 전용, 보상 없음)
4. 저장 시스템 도입 후 — 게임 재시작 시 자동 복원

### 카테고리별 색

| 카테고리 | 색 |
|---|---|
| Landmark (랜드마크) | 파랑 |
| FloraFauna (동식물) | 녹색 |
| Ruin (유적) | 갈색 |
| Event (사건) | 노랑-주황 |

---

## 단계 1 — DiscoveryMarkers GameObject 생성

1. Hierarchy 빈 곳 우클릭 → **Create Empty** → 이름 **`DiscoveryMarkers`**
2. Transform Position (0, 0, 0)
3. Add Component → **`Discovery Marker Spawner`** 검색·추가

## 단계 2 — Spawner 필드 채우기

| 필드 | 값 |
|---|---|
| **Discovery Catalog** | `DiscoveryCatalog` SO 드래그 |
| **Reopen Panel** | 씬의 `DiscoveryFoundPanel` GameObject 드래그 (비워두면 자동 검색) |
| **Markers Parent** | 비워둠 (자동으로 자기 자신) |
| **Marker Height** | 3 (기본값 권장) |
| **Marker Size** | 30 (기본 ~220 km. 줌 인 카메라면 더 작게) |

## 단계 3 — Play 테스트

1. ▶ Play
2. 배를 발견물 좌표 근처로 이동 → 정박·탐색
3. 발견 패널 등장 → 닫기
4. **그 위치에 색깔 박스가 떠 있어야 함** ← 마커
5. **마커 탭 → 발견 패널 다시 등장** (보상 없이 정보만)

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 마커가 안 보임 | DiscoveryMarkers GameObject Active 확인 / Discovery Catalog 비어있나 확인 (`Game ▸ Refresh All Catalogs`) |
| 마커가 너무 작음 / 큼 | DiscoveryMarkerSpawner 의 `Marker Size` 조정 (30 → 50 등) |
| 마커가 바다 아래 잠김 | `Marker Height` 키움 (3 → 5 등). WorldLand 의 BaseY 보다 커야 함 |
| 마커 탭해도 패널 안 뜸 | `Reopen Panel` 필드 채워졌는지 확인 / 씬에 DiscoveryFoundPanel 있는지 |
| 마커 색이 다 같음 | DiscoveryData 의 `category` 필드가 모두 같은 값. 시더(M3DiscoveriesSeeder) 가 4 카테고리 분산 |

---

## 시각 커스터마이즈 (선택)

### 색상 변경
[`DiscoveryMarker.cs`](DiscoveryMarker.cs) 의 `ColorFor` 메서드에서 카테고리별 색 변경.

### 크기 / 위치 변경
DiscoveryMarkerSpawner 의 `Marker Size` / `Marker Height` Inspector 에서 조정 — Play 중에도 변경 가능 (단 이미 spawn 된 마커는 영향 없음, Play 재시작 필요).

### 아이콘 추가 (추후 폴리시)
현재는 단색 사각형. Sprite 를 카테고리별로 만들어 spawner 코드의 `image.color = ...` 부분 옆에 `image.sprite = SpriteFor(category)` 추가하면 됨.

---

## 저장 시스템과의 연동 (추후)

현재는 메모리에만 보존 — 게임 재시작 시 발견물 ID 목록이 비워짐 → 마커 사라짐.

저장 시스템 (GAME_PREP.md §11.4) 도입 시:
1. 로드 시 `MissionService.DiscoveredIds` 를 JSON 에서 복원
2. `DiscoveryMarkerSpawner.Start()` 가 자동으로 복원된 ID 목록에 대해 마커 spawn

코드는 이미 `foreach (var id in _missionService.DiscoveredIds) Spawn(...)` 로직이 들어있어 별도 작업 불필요.
