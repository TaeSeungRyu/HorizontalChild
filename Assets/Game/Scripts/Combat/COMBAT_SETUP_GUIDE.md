# 전투 / NPC 풀 셋업 가이드

해상 NPC 배 + 탭 시 자동 전투 + 결과 패널. 어린이 친화: 즉시 해결, 데미지 X.

기획서: GAME_PREP.md §12.2 M3 메카닉 풀세트 — "전투 시스템 (자동 전투 권장)".

---

## 시스템 구성

| 컴포넌트            | 위치                   | 역할                                   |
| ------------------- | ---------------------- | -------------------------------------- |
| `NpcDefinition`     | SO (기존)              | NPC 정의 — character + type + homePort |
| `NpcCatalog`        | SO (신규)              | 모든 NPC 모음                          |
| `CombatService`     | GameManager 싱글톤     | 전투 공식 + 보상 적용                  |
| `NpcSpawner`        | 빈 GameObject 에 부착  | 게임 시작 시 NPC 풀 spawn              |
| `NpcShip`           | 자동 (spawner 가 부착) | 클릭 시 전투 트리거                    |
| `CombatResultPanel` | Canvas UI              | 결과 표시                              |

---

## 전투 공식

```
playerPower = ship.cannonPower × 10 + captain.bravery + 무작위(0~50)
npcPower    = npcChar.bravery + npcChar.seamanship + 무작위(0~50)
playerPower ≥ npcPower → 승리
```

**보상 (승리)**:

- 해적: +돈 100~500, 좋은 명성 +10
- 상선: +돈 100~500, 나쁜 명성 +10 (약탈)
- 호위선: +돈 100~500

**패널티 (패배)**:

- 돈 -50 ~ -200

---

## 단계 1 — NPC 시드

1. Unity 메뉴 **`Game ▸ Seed M3 NPCs`** 클릭
2. Console 에 "NPC 6명 + 캐릭터 6명" 로그 확인
3. `Assets/Game/Data/Npcs/` 에 6개 NpcDefinition asset
4. `Assets/Game/Data/Characters/` 에 새 캐릭터 6개

생성된 NPC:

- 해적 4: 검은수염 / 은빛 잭 / 선장 마야 / 붉은 왜구
- 상선 1: 상인 마르코
- 호위선 1: 호위병 한스

---

## 단계 2 — NpcCatalog 생성 + 갱신

1. Project 창의 `Assets/Game/Data/_Catalogs/` 우클릭 → **Create → Game/Data → Npc Catalog** → 이름 `NpcCatalog`
2. **`Game ▸ Refresh All Catalogs`** 실행 → 자동 채움

---

## 단계 3 — CombatService 부착

1. Hierarchy 의 **`GameManager`** 선택
2. Add Component → **`Combat Service`**
3. 필드는 기본값 그대로 (Reward Range 등 추후 조정 가능)

---

## 단계 4 — CombatResultPanel UI 만들기

1. Canvas 우클릭 → Create Empty → 이름 **`CombatResultPanel`**
2. Add Component → **`Combat Result Panel`**
3. 자식 UI 추가 (필요한 만큼):
   - HeaderText (TMP_Text — "승리!" 또는 "패배...")
   - PlayerInfoText (TMP_Text — 플레이어 이름 + 전력)
   - NpcInfoText (TMP_Text — NPC 이름 + 전력)
   - RewardText (TMP_Text — 돈·명성 변화)
   - MessageText (TMP_Text — 어린이 톤 한 줄 설명)
   - OkButton (Button — "확인")
4. 각 자식을 CombatResultPanel 컴포넌트의 필드에 드래그

(자세한 레이아웃은 DiscoveryFoundPanel 참고 — 동일 패턴)

## 단계 5 — NpcSpawner 부착

1. Hierarchy 빈 곳 → Create Empty → 이름 **`NpcSpawner`**
2. Add Component → **`Npc Spawner`**
3. 필드:
   - **Npc Catalog** ← `NpcCatalog` SO 드래그
   - **Result Panel** ← `CombatResultPanel` 드래그 (비워두면 자동 검색)
   - **Spawn Count** — 6 (기본)
   - **Npc Size** — 6 (큐브 크기)

---

## 단계 6 — Play 테스트

1. ▶ Play → 국가 선택 → 항해 시작
2. **타입별 색깔 큐브가 해상에 6개 spawn**:
   - 빨강 = 해적
   - 노랑 = 상선
   - 파랑 = 호위선
3. **큐브 탭** → 전투 결과 패널 등장
4. 승리 → 큐브 사라짐 + 돈/명성 증가
5. 패배 → 큐브 남음 + 돈 감소

(Main Camera 에 Physics Raycaster 이미 부착됨 — 발견물 마커 셋업에서 추가)

---

## 자주 발생하는 문제

| 증상                              | 해결                                                                              |
| --------------------------------- | --------------------------------------------------------------------------------- |
| NPC 큐브 안 보임                  | NpcSpawner 의 Npc Catalog 필드 채워졌나 / Catalog 비어있나 (Refresh All Catalogs) |
| 큐브 탭해도 반응 없음             | Main Camera 의 Physics Raycaster 컴포넌트 있나                                    |
| 전투 결과 패널 안 뜸              | CombatResultPanel 필드 필드 채워졌나 (Result Panel)                               |
| Console 에 "CombatService 없음"   | GameManager 에 CombatService 컴포넌트 부착                                        |
| 승리·패배 결과 너무 한쪽에 치우침 | CombatService Inspector 의 보상 범위 조정 / 무작위 폭 조정                        |

---

## 추후 폴리시

| 작업                                       | 효과                |
| ------------------------------------------ | ------------------- |
| NPC 이동 AI (random walk)                  | 살아있는 바다 느낌  |
| NPC 동적 재스폰                            | 영구 콘텐츠         |
| 호위선이 플레이어 도와줌                   | 협력 전투           |
| 해적이 플레이어 추적                       | 긴장감              |
| ShipData.prefab3D 사용                     | 큐브 → 진짜 배 모델 |
| 명성 게이트 — 명성 낮으면 강한 NPC 못 이김 | 진행 게이팅         |
| 전투 일시정지·애니메이션                   | 시각 완성도         |
| 화물 약탈 (상선 승리 시 cargo 일부 획득)   | 전리품 시스템       |
