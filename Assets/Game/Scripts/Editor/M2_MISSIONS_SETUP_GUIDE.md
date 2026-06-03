# M2.4 — 8개국 발견물 의뢰 시드 셋업 가이드

다른 7개국 시작 항구에서도 발견물 의뢰 받을 수 있게 데이터 추가.

---

## 단계 1 — 시더 메뉴 실행

1. Unity 에디터 메뉴 바: **`Game ▸ Seed M2 Missions`** 클릭
2. Console 에 16개 .asset 생성 로그:
   ```
   [M2MissionSeeder] Created: Discovery_CapeBojador.asset
   [M2MissionSeeder] Created: Discovery_CanaryIslands.asset
   ... (8개 발견물)
   [M2MissionSeeder] Created: Mission_DiscCeutaCapeBojador.asset
   ... (8개 의뢰)
   [M2MissionSeeder] 완료. M2 의뢰 시드 추가 16개
   ```
3. Project 창에서 확인:
   - `Assets/Game/Data/Discoveries/` 에 9개 .asset (지브롤터 + 8개 추가)
   - `Assets/Game/Data/Missions/` 에 9개 .asset

---

## 단계 2 — SeaWorldManager 의 `Active Discoveries` 배열 확장

새 발견물들이 탐색 시스템에 등록되어야 합니다.

1. Hierarchy 의 **`GameManager`** 클릭
2. Inspector 의 **`Sea World Manager`** 컴포넌트
3. **`Active Discoveries`** 배열:
   - **Size** = **9** (기존 1 → 9)
   - Element 0: 기존 `Discovery_GibraltarStrait`
   - Element 1: **`Discovery_CapeBojador`** 드래그
   - Element 2: **`Discovery_CanaryIslands`** 드래그
   - Element 3: **`Discovery_BlueGrotto`** 드래그
   - Element 4: **`Discovery_TexelSeals`** 드래그
   - Element 5: **`Discovery_WhiteCliffsDover`** 드래그
   - Element 6: **`Discovery_BosphorusStrait`** 드래그
   - Element 7: **`Discovery_Hallasan`** 드래그
   - Element 8: **`Discovery_PearlRiverDelta`** 드래그

---

## 단계 3 — MissionService 의 `All Missions` 배열 확장

새 의뢰들이 모험가 조합에서 발급되도록.

1. Hierarchy 의 **`GameManager`** 클릭
2. Inspector 의 **`Mission Service`** 컴포넌트
3. **`All Missions`** 배열:
   - **Size** = **9** (기존 1 → 9)
   - Element 0: 기존 `Mission_DiscLisbonGibraltar`
   - Element 1: **`Mission_DiscCeutaCapeBojador`** 드래그
   - Element 2: **`Mission_DiscSevillaCanary`** 드래그
   - Element 3: **`Mission_DiscVeneziaBlueGrotto`** 드래그
   - Element 4: **`Mission_DiscAmsterdamTexelSeals`** 드래그
   - Element 5: **`Mission_DiscLondonDover`** 드래그
   - Element 6: **`Mission_DiscIstanbulBosphorus`** 드래그
   - Element 7: **`Mission_DiscBusanHallasan`** 드래그
   - Element 8: **`Mission_DiscGuangzhouPearlRiver`** 드래그

---

## 단계 4 — JournalPanel 의 `All Discoveries` 배열 확장

도감에서도 신규 발견물 보이게.

1. Hierarchy 의 **`JournalPanel`** 클릭
2. Inspector 의 **`Journal Panel`** 컴포넌트
3. **`All Discoveries`** 배열:
   - **Size** = **9**
   - Element 0~8: 단계 2 와 동일한 9개 발견물 드래그

---

## 단계 5 — 테스트 시나리오

### 시나리오 1 — 다른 국적으로 시작

1. ▶ Play
2. 국적 선택 화면에서 **스페인** 클릭 → "이 나라로 시작"
3. PlayerShip 이 세비야 좌표 (-89, 0, 561) 로 자동 이동
4. **항구 들어가기** 버튼 → 세비야 입장
5. **모험가 조합 가기** 클릭
6. 의뢰 표시:
   ```
   세비야 모험가 조합
   큰 바다로 가기 전, 따뜻한 섬들을 찾아봐요
   ...
   [예, 받겠어요]
   ```
7. 수락 → 카나리아 제도(28.3°N, 16.6°W ≈ -249, 0, 425) 로 항해
8. 좌표 근처에서 "정박 및 탐색" → DiscoveryFoundPanel 표시
9. 세비야 복귀 → 자동 완료 + 보상

### 시나리오 2 — 다른 7개국 모두 같은 흐름

각 국적별로 시작 항구의 발견물을 찾는 흐름이 동일하게 동작:
- 이탈리아/베네치아 → 푸른 동굴 (카프리섬 좌표)
- 네덜란드/암스테르담 → 텍셀 섬 바다표범 (북쪽)
- 영국/런던 → 도버의 흰 절벽 (남쪽)
- 오스만/이스탄불 → 보스포루스 해협 (현지)
- 조선/부산 → 한라산 (제주, 남쪽)
- 중국/광저우 → 주강 삼각주 (현지)

### 시나리오 3 — 도감 확인

1. 발견 1개 한 후 도감 버튼 클릭
2. JournalPanel:
   ```
   도감
   발견 1 / 모두 9

   [랜드마크] 카나리아 제도
     스페인에서 큰 바다로 가기 전에...

   [???] 아직 발견하지 못한 곳     (× 8 — 나머지)

   [닫기]
   ```

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 메뉴 클릭해도 "NationData 없음" 경고 | M1·M2.1 시드를 먼저 돌렸는지 확인 (`Seed M1 Content`, `Seed M2 Content`) |
| 다른 국적 항구의 모험가 조합 비어있음 | MissionService 의 All Missions 배열에 새 9개가 다 등록됐는지 |
| 정박 및 탐색 후 발견 안 됨 | SeaWorldManager 의 Active Discoveries 배열에 해당 발견물 등록됐는지 |
| 도감에 ??? 만 보임 | JournalPanel 의 All Discoveries 배열에 9개 다 등록됐는지 |
| 좌표 너무 멀어서 못 찾음 | GeoCoordinate 스케일 (15 unit/도) 로 변환한 위치 확인. 너무 멀면 `clickEnterRadiusUnits` 같은 값 조정 |

---

## 거리 안내 (대략)

각 시작 항구 → 발견물까지의 대략 거리 (Unity Unit, 1 unit ≈ 7.4 km):

| 시작 항구 | 발견물 | 직선 거리 (대략) | 분류 |
|---|---|---|---|
| 리스본 | 지브롤터 해협 | ~60 units | 단거리 |
| 세우타 | 보자도르 곶 | ~150 units | 중거리 |
| 세비야 | 카나리아 제도 | ~190 units | 중거리 |
| 베네치아 | 푸른 동굴 | ~85 units | 단거리 |
| 암스테르담 | 텍셀 바다표범 | ~15 units | 매우 가까움 |
| 런던 | 도버의 흰 절벽 | ~22 units | 매우 가까움 |
| 이스탄불 | 보스포루스 해협 | ~5 units | 거의 즉시 |
| 부산 | 한라산 (제주) | ~45 units | 단거리 |
| 광저우 | 주강 삼각주 | ~7 units | 즉시 |

→ 보스포루스/주강 삼각주 는 시작 위치 거의 바로. 어린이 첫 의뢰로 좋음.

---

## M2 완성

이 작업으로 **M2 (8개국 + 시작 항구 + 캐릭터 + 의뢰) 완전 완성**.
다음은 M3 — 메카닉 풀세트 (전투·NPC·시세·조선소).
