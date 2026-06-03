# 세계지도(대륙) 셋업 가이드

M1 디버깅을 위한 단순 대륙 큐브 시스템. 12개 위/경도 박스로 대륙 표현 + 배 충돌 차단.

M3 폴리시 단계에서 Natural Earth 같은 정밀 데이터로 교체 예정.

---

## 무엇이 추가되나

| 영역 | 결과 |
|---|---|
| 시각 | 12개 갈색 큐브가 각 대륙 위치에 자동 spawn |
| 충돌 | 배가 육지에 닿으면 자동 정지 (어린이 친화 — 데미지 없음) |
| 항해 참조 | 사용자가 항해 중 대륙·바다 경계를 시각으로 인식 가능 |
| 카탈로그 | LandmassCatalog → 새 대륙 추가 시 메뉴 한 번 |

---

## 단계 1 — 데이터 시드

1. 메뉴 바: **`Game ▸ Seed M1 Landmasses`** 클릭
2. Console 에 12개 LandmassData 생성 로그
3. Project 창에서 `Assets/Game/Data/Landmasses/` 폴더 확인 — 12개 .asset:
   - 이베리아 / 북아프리카 / 유럽 / 영국 섬 / 아나톨리아
   - 중동 / 인도 / 중국 / 한반도 / 일본 / 동남아시아 / 사하라이남

---

## 단계 2 — LandmassCatalog 생성 + 자동 채움

카탈로그 패턴 (이전 `CATALOG_SETUP_GUIDE.md` 와 동일):

1. Project 창의 `Assets/Game/Data/_Catalogs/` 폴더 (없으면 만들기)
2. 우클릭 → **Create → Game/Data → Landmass Catalog** → 이름 `LandmassCatalog`
3. 메뉴 바: **`Game ▸ Refresh All Catalogs`** (또는 `Refresh Landmass Catalog`) 클릭
4. LandmassCatalog 의 Inspector 의 `all` 배열에 12개 채워졌는지 확인

---

## 단계 3 — 씬에 LandmassPlacer 부착

대륙을 자동 spawn 할 GameObject 가 필요.

1. Hierarchy 빈 곳 우클릭 → **Create Empty** → 이름 **`LandmassRoot`**
2. Transform 은 (0, 0, 0) 기본값 그대로
3. **`LandmassRoot`** 선택 → Add Component → **`Landmass Placer`** 검색·추가
4. 필드 채우기:

| 필드 | 값 |
|---|---|
| **Landmass Catalog** | `LandmassCatalog` SO 드래그 |
| Landmasses | 비워둠 (카탈로그 우선) |
| Default Material | 비워둠 (자동으로 URP Lit 머티리얼 생성) |
| Landmasses Parent | 비워둠 (자동으로 자기 자신) |

→ 다음 Play 시 12개 대륙이 자동 spawn.

---

## 단계 4 — ShipController 충돌 설정 확인

`ShipController` 에 새 필드 두 개가 추가됐습니다.

1. Hierarchy 의 **`PlayerShip`** 클릭
2. Inspector 의 `Ship Controller` 컴포넌트에서:

| 필드 | 권장값 | 설명 |
|---|---|---|
| **Collision Check Radius** | **2** | 배 너비. 너무 크면 항구 옆에서도 막힘 |
| **Block Movement On Land** | ☑ 체크 | 끄면 충돌 무시 (디버그용) |

자동으로 기본값이 들어가 있어 별도 작업 X.

---

## 단계 5 — Play 테스트

1. ▶ Play
2. 게임 시작 직후:
   - 12개 갈색 큐브가 대륙 위치에 spawn 확인
   - 9개 빨간 원기둥 (항구) 이 각 대륙 해안 근처에 위치
3. 키보드 또는 가상 조이스틱으로 배 조작:
   - 바다 위에서는 자유롭게 이동
   - 대륙 큐브에 가까이 가면 자동 정지 (튕기지 않음)
   - 키 떼면 천천히 후진 또는 정지

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 큐브 안 보임 | Play 했는지 / LandmassPlacer 의 Catalog 필드 비어있는지 확인 |
| Catalog 가 비어있음 | `Game ▸ Refresh All Catalogs` 메뉴 실행 |
| 배가 육지 위로 올라감 | ShipController 의 Block Movement On Land ☑ 인지, Collision Check Radius 가 너무 작지 않은지 |
| 배가 항구 옆에서도 못 움직임 | Collision Check Radius 가 너무 큼 (5 이상). 2~3 권장. 또는 대륙 큐브의 영역이 너무 넓어서 항구를 덮음 |
| 큐브가 항구 위에 덮어짐 | LandmassData 의 위/경도 영역이 너무 큼. 인스펙터에서 `Center Latitude` / `Size Longitude` 조정 |
| 큐브 색이 분홍색 (Missing Shader) | URP 가 아닌 다른 파이프라인. 단계 3 의 Default Material 에 임시 머티리얼 할당 |

---

## 영역 조정 — Inspector 에서 직접

대륙 모양이 안 맞으면 LandmassData SO 를 클릭해서 직접 조정:

1. Project 창의 `Assets/Game/Data/Landmasses/Landmass_Iberia.asset` 클릭
2. Inspector 의 슬라이더:
   - **Center Latitude / Longitude** — 중심 위치
   - **Size Latitude / Longitude** — 폭
   - **Height** — 바다 위 돌출 높이
   - **Color** — 색깔 변경
3. Play 다시 → 변경 반영

---

## 좌표 참고

각 시작 항구 vs 대륙 영역:

| 항구 | 항구 좌표 | 인근 대륙 | 대륙 중심 |
|---|---|---|---|
| 리스본 | (38.7°N, 9.1°W) | 이베리아 | (40, -3) |
| 세우타 | (35.9°N, 5.3°W) | 북아프리카 | (26, 5) |
| 세비야 | (37.4°N, 5.9°W) | 이베리아 | (40, -3) |
| 베네치아 | (45.4°N, 12.3°E) | 유럽 | (50, 15) |
| 암스테르담 | (52.4°N, 4.9°E) | 유럽 | (50, 15) |
| 런던 | (51.5°N, 0.1°W) | 영국 섬 | (54, -3) |
| 이스탄불 | (41.0°N, 28.9°E) | 아나톨리아 | (39, 35) |
| 부산 | (35.1°N, 129.0°E) | 한반도 | (37.5, 127) |
| 광저우 | (23.1°N, 113.3°E) | 중국 | (32, 105) |

→ 모든 항구가 인근 대륙 가장자리에 위치 (시작 시 항구 옆 작은 만 모양으로 보임).

---

## M3 폴리시 단계 (추후)

| 작업 | 효과 |
|---|---|
| Natural Earth 데이터 → 메쉬 생성 | 정확한 대륙 모양 |
| 텍스처 매핑 (옛 항해지도 톤) | 시각 완성도 |
| 대륙별 라벨 (World Space TMP) | "유럽" "아시아" 같이 표시 |
| 미니맵 (Render Texture) | 우상단에 작은 전체 지도 |

이 가이드는 그때까지의 임시 단순 큐브 시스템 안내.
