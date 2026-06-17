# 배 성능·전투 효과 적용 가이드

대항해 시대 함선 17종에 적용된 **크기 (소형/중형/대형) 다중 포탄 시각**, **배별 포탄 색**, **다양화된 발사 간격** 적용·검증·튜닝 가이드.

## 1. 한눈에 보기 — 무엇이 바뀌었나

| 효과 | 동작 | 데미지 영향 |
|------|------|-------------|
| 📏 **크기 (ShipSize)** | 소형 = 포탄 1발 / 중형 = 2발 / 대형 = 3발. 추가 발사는 **0.1초씩 딜레이** | ❌ 데미지는 항상 첫 발만 적용 (시각만 늘어남) |
| 🎨 **포탄색 (cannonColor)** | 배 SO 마다 다른 색의 sphere 발사 | ❌ |
| ⏱ **발사 간격 (attackInterval)** | 0.8 ~ 2.5 초로 배별 다양화 | ✔ DPS 에 직접 영향 |

전 17종 분포: **소형 4종 / 중형 10종 / 대형 3종** ([Assets/Game/Art/SHIP_REFERENCE.md](Art/SHIP_REFERENCE.md) 표 참조).

## 2. 파일 구조 — 어디서 무엇을 만짐?

| 파일 | 역할 | 수정 빈도 |
|------|------|----------|
| [Scripts/Data/ShipData.cs](Scripts/Data/ShipData.cs) | `ShipSize` enum + `cannonColor` Color 필드 선언 | 거의 안 함 (스키마) |
| [Data/Ships/Ship_*.asset](Data/Ships/) | **실제 값**. 각 배의 size·color·power·duration | **여기서 튜닝** |
| [Scripts/Combat/CombatSequence.cs](Scripts/Combat/CombatSequence.cs) | 발사 루프 — `BallCountFor(ship)` 1/2/3 분기, `multiShotDelay` 0.1s | 튜닝 (다중샷 간격 등) |
| [Scripts/Editor/M3ShipSeeder.cs](Scripts/Editor/M3ShipSeeder.cs) | 최초 1회 .asset 생성 시더. **이미 있으면 덮어쓰지 않음** | 거의 안 함 |
| [Art/SHIP_REFERENCE.md](Art/SHIP_REFERENCE.md) | 모델링·디자인 레퍼런스 (모든 배 한눈에) | 값 변경 시 동기화 |

> **중요**: `M3ShipSeeder.cs` 는 **마스터 데이터가 아님**. 실제 값은 `.asset` 파일이 정답. 시더는 처음 한 번 생성하고 나면 의미 없음.

## 3. 적용 검증 절차 (Unity Editor)

### 3.1 인스펙터에서 확인
1. Unity 열기 → Project 창에서 `Assets/Game/Data/Ships/Ship_Galleon.asset` 선택
2. Inspector 에 새 필드 보여야 함:
   - **Size** dropdown: `Large` (갈레온은 대형)
   - **Cannon Color** swatch: 황금색 (R 0.95, G 0.70, B 0.10)
3. 다른 .asset 선택해도 모두 size·cannonColor 가 채워져 있어야 함.

만약 **size 가 비어있거나 cannonColor 가 검정**이면:
- .asset 파일을 텍스트 에디터로 열어 `size:` / `cannonColor:` 라인이 있는지 확인
- 없으면 Asset 메타 재로딩 (Unity 재시작 또는 우클릭 → Reimport)

### 3.2 게임 플레이로 검증
1. Play → 항구에서 갈레온 (대형) 또는 동인도무역선 (대형) 탑승
2. 해적 NPC 클릭 → 전투 시작
3. **확인 포인트**:
   - 플레이어가 한 라운드에 **3발** 의 황금 색 포탄을 발사 (0.1초 간격)
   - 해적 NPC 는 자신의 배 size 에 따라 1~3발 + 자기 색의 포탄
   - 데미지는 한 라운드당 한 번만 들어감 (durability 게이지가 한 번만 감소)
4. **시각 비교용** — 갈리선 (소형) 로 갈아타고 같은 전투 → 포탄 1발만 보여야 함

### 3.3 빠른 디버그용 추천 조합

| 시나리오 | 플레이어 배 | NPC 배 | 기대 결과 |
|---------|------------|--------|----------|
| 최소 단발 | 갈리선 (Galley, 소형) | 라틴 카라벨 (소형) | 양쪽 다 1발씩 |
| 최대 다중샷 | 갈레온 (대형) | 베네치아 갤리어스 (대형) | 양쪽 다 3발씩 |
| 비대칭 | 갈레온 (대형) | 다우선 (소형) | 플레이어 3발 vs NPC 1발 |

## 4. 튜닝 — 자주 만지는 곳

### 4.1 다중샷 간격 변경
[CombatSequence.cs](Scripts/Combat/CombatSequence.cs) 인스펙터의 `Multi Shot Delay` 슬라이더. 기본 0.1초.
- **0.05초** — 거의 동시 (속사포 느낌)
- **0.15~0.2초** — 또렷한 연사
- **0.3초+** — 발사 간격이 attackInterval 보다 커지면 의도 깨짐 — 권장 ≤ 0.2s

### 4.2 배별 색 변경
.asset 파일을 인스펙터에서 열고 `Cannon Color` swatch 클릭 → 색 선택. 저장은 자동.

### 4.3 배 크기 재분류
.asset 인스펙터에서 `Size` dropdown 변경:
- **Small (0)** — 약하거나 작은 배 (탐험선·소형 어선·갤리)
- **Medium (1)** — 표준 무역선·중형 전투선
- **Large (2)** — 주력 전열함·기함

> 너무 많은 배를 Large 로 두면 전투가 화려해지지만 산만해질 수 있음. 현재 대형 3종 (Galleon·VenetianGalleass·EastIndiaman) 권장.

### 4.4 발사 간격 (DPS) 튜닝
.asset 의 `Attack Interval` (0.3~4.0). 다양화 분포:

| 가장 빠름 (0.8~1.0) | 중간 (1.1~1.5) | 느림 (1.6~2.0) | 가장 느림 (2.2~2.5) |
|---|---|---|---|
| Galley 0.8, CaravelaLatina 0.9, Dhow/Clipper 1.0, LaReal 1.0 | Caravel 1.1, VenetianGalleass 1.1, Galleass 1.2, Geobukseon 1.3, Fluyt 1.4, Panokseon 1.5 | Galleon 1.6, Carrack 1.8, Junk 2.0 | SantaMaria 2.2, EastIndiaman 2.3, Cog 2.5 |

소형·갤리계열 = 속사 / 대형·박스형 = 묵직한 한 방. 그 정도 패턴.

## 5. 알려진 미완 작업

### 5.1 LaReal / VenetianGalleass .asset 미생성
[SHIP_REFERENCE.md](Art/SHIP_REFERENCE.md) 에는 17종이 등록돼 있지만 실제 SO 는 15개:
- ❌ `Ship_LaReal.asset` — 없음
- ❌ `Ship_VenetianGalleass.asset` — 없음

생성하려면 [M3ShipSeeder.cs](Scripts/Editor/M3ShipSeeder.cs) 에 두 항목 추가 + `Game ▸ Seed M3 Ships` 메뉴 재실행. 추천 스탯은 SHIP_REFERENCE.md 의 표 값.

### 5.2 M3ShipSeeder.cs 의 옛 값
시더 안의 attackInterval 값들이 다양화 이전의 옛 값. 시더는 기존 .asset 가 있으면 건드리지 않으므로 **실해는 없음**. 그러나 누가 .asset 을 지우고 시더를 재실행하면 옛 값으로 복원됨.

원하면 시더를 .asset 과 동기화 (+ size·cannonColor 기본값 + LaReal·VenetianGalleass 추가) 가능 — 요청 시 처리.

### 5.3 절차적 큐브 배 (ProceduralShipBuilder)
[Scripts/Combat/ProceduralShipBuilder.cs](Scripts/Combat/ProceduralShipBuilder.cs) — 3D 모델이 없는 NPC 가 사용하는 큐브 배. 현재 NpcType (Pirate/Merchant/Escort) 별 색만 다름. ShipSize 별 시각 차이 (예: 대형은 더 큼) 는 아직 없음. 필요 시 추가 작업.

## 6. 한 줄 요약

> **배의 성능은 [.asset 파일](Data/Ships/) 이 정답**. `M3ShipSeeder` 는 처음 한 번뿐. `ShipSize` + `cannonColor` 는 시각 효과, `attackInterval` 만 DPS 에 영향. 다중 포탄은 첫 발만 데미지.
