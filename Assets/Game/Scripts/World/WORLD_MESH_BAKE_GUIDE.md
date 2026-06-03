# 세계 대륙 메쉬 베이크 가이드 (M3)

Natural Earth GeoJSON 을 한 번 굽고 끝. 결과로 `WorldLand.prefab` 한 개가 생성되고 씬에 드래그해서 사용.

런타임에는 파싱·삼각화 안 함 — Prefab 의 베이크된 Mesh 만 렌더.

---

## 사전 조건

`Assets/Game/Art/Map/ne_110m_land.geojson` 존재해야 함 (이미 넣어두셨음).

---

## 단계 1 — 베이크

1. Unity 메뉴: **`Game ▸ Bake World Land Mesh from GeoJSON`** 클릭
2. Progress bar 가 잠깐 도는 동안 대기 (~수 초)
3. Console 에 결과 로그:
   ```
   [M3WorldMeshBaker] 완료.
     • Features 읽음: 127 (스킵 N)
     • 폴리곤 ring: ...
     • 정점: ...
     • Asset: WorldLand.mesh / .mat / .prefab
   ```
4. `Assets/Game/Art/Map/` 폴더에 3개 Asset 생성됨:
   - `WorldLand.mesh` (베이크된 메쉬)
   - `WorldLand.mat` (갈색 URP Lit 머티리얼)
   - `WorldLand.prefab` (즉시 사용 가능)

---

## 단계 2 — 씬에 배치 (한 번만)

1. Project 창에서 **`WorldLand.prefab`** 을 Hierarchy 로 드래그
2. Transform 은 (0, 0, 0) 자동 — 그대로 두세요
3. Scene View 에서 갈색 대륙들이 진짜 모양으로 보이는지 확인
4. 씬 저장 (Ctrl+S)

---

## 단계 3 — 기존 큐브 안 보이게 (이미 됐으면 패스)

`LandmassRoot` 의 **Hide Visuals ☑** 체크 — 새 메쉬가 시각을 담당하고 큐브는 충돌만.

세 가지 시각 요소가 겹쳐 있어야 정상:
1. `SeaPlane` — 푸른 바다 텍스처 (y = -0.05)
2. `WorldLand` — 갈색 대륙 메쉬 (y = 0.05 ~ 1.55)
3. `LandmassRoot/*` — 큐브 (시각 ☐, 충돌만 활성)

---

## 단계 4 — Play 테스트

▶ Play. 배가 진짜 모양의 해안선 옆에 있는지, 멀리서도 대륙 윤곽이 보이는지.

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 메뉴 클릭 시 "GeoJSON 찾을 수 없음" | 경로 `Assets/Game/Art/Map/ne_110m_land.geojson` 확인 |
| 대륙이 어둡게 보임 (검정) | URP 셋업 문제. WorldLand.mat 의 Shader 가 "Universal Render Pipeline/Lit" 인지 확인 |
| 대륙 일부가 뒤집혀 그려짐 (구멍) | 폴리곤 winding 문제. 콘솔에 오류 없으면 무시 가능 (이근거리에선 안 보임). 심각하면 알려주세요 |
| Russia/Alaska 등 일부 대륙이 누락 | Date line 횡단으로 스킵됨. 1:110m 에서는 보통 정상 처리되지만 일부 폴리곤은 빠질 수 있음. 큰 문제 아니면 무시 |
| 정점 65k 초과 경고 | 코드가 자동으로 UInt32 indexFormat 으로 전환. 무시 |
| Prefab 갱신했는데 씬의 인스턴스가 안 바뀜 | Prefab Override 가 있어서 그럼. 씬의 WorldLand 선택 → 우클릭 → "Revert" |

---

## 다시 굽기

GeoJSON 파일을 더 정밀한 1:50m 으로 교체하거나, 색상 / 두께를 코드에서 바꾸고 싶을 때:

1. `M3WorldMeshBaker.cs` 상단 상수 수정:
   ```
   ExtrudeHeight = 1.5f      ← 두께
   BaseY = 0.05f             ← 바닥 높이
   LandColor = (0.62, 0.52, 0.38)  ← 색상
   ```
2. 메뉴 다시 클릭 → 기존 Asset 의 내용만 갱신 (Prefab 참조 유지)

---

## 정밀도 업그레이드 (선택)

`ne_110m_land.geojson` (현재) → `ne_50m_land.geojson` 또는 `ne_10m_land.geojson` 으로 교체 가능.

| 정밀도 | 폴리곤 수 | 정점 수 (대략) | 모바일 |
|---|---|---|---|
| 1:110m (현재) | ~127 | ~3K | 매우 가벼움 |
| 1:50m | ~1300 | ~30K | 가벼움 |
| 1:10m | ~5800 | ~300K | 무거움, 비추 |

교체 시: 파일을 같은 이름 `ne_110m_land.geojson` 으로 덮어쓰거나, 코드의 `GeoJsonPath` 상수 수정.

---

## 다음 폴리시 (선택)

| 작업 | 효과 | 시간 |
|---|---|---|
| 정점 색상 — 위도별 그라데이션 (적도 녹색·극지 흰색) | 어린이 교육 효과 | 30분 |
| MeshCollider 추가 (Prefab 에) | 진짜 해안선 충돌 | 5분 (단 좁은 해협에서 끼임 주의) |
| 폴리곤별 머티리얼 분리 (대륙별 색 다르게) | 시각 풍부 | 1시간 — 베이크 코드 일부 재작성 필요 |
| 노멀맵 / Bump | 빛에 따른 음영 | 30분 |
| 옛 항해도 톤 텍스처 매핑 | 콘셉트 강화 | 1시간 — UV 매핑 필요 |
