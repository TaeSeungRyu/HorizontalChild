# 조선소 / 광장 셋업 가이드

PortScreen 의 새 두 버튼.
- **조선소**: 배 구매 (잔돈 차감 + playerShip.shipData 교체)
- **광장**: 선장 고용 (잔돈 차감 + playerShip.captain 교체)

GAME_PREP.md §12.2 M3 메카닉 풀세트 — 마지막 항목.

---

## 단계 1 — 시드 + 카탈로그

### 배 5종

1. **`Game ▸ Seed M3 Ships`** 클릭 — 4개 추가 (M1 의 Caravel 외)
   - 캐릭선 2,500G / 갈레온선 6,000G / 플라이트선 4,000G / 거북선 8,000G
2. `_Catalogs/` 우클릭 → **Create → Game/Data → Ship Catalog** → 이름 `ShipCatalog`
3. `_Catalogs/` 우클릭 → **Create → Game/Data → Character Catalog** → 이름 `CharacterCatalog`
4. **`Game ▸ Refresh All Catalogs`** → ShipCatalog (5개), CharacterCatalog (현재 캐릭터 전부) 자동 채움

---

## 단계 2 — ShipyardPanel UI

1. Canvas 우클릭 → Create Empty → 이름 **`ShipyardPanel`**
2. Add Component → **`Shipyard Panel`**
3. 자식 GameObject 만들기 (수동):
   - TitleText (TMP_Text)
   - MoneyText (TMP_Text)
   - CurrentShipText (TMP_Text)
   - CloseButton (Button)
4. ShipyardPanel 컴포넌트 필드에 위 자식 + **`Ship Catalog`** + **`Player Ship`** 연결
5. ⋮ → **Auto Layout** 클릭 → 풀스크린 + ScrollView 자동 생성

---

## 단계 3 — PlazaPanel UI

1. Canvas 우클릭 → Create Empty → 이름 **`PlazaPanel`**
2. Add Component → **`Plaza Panel`**
3. 자식: TitleText / MoneyText / CurrentCaptainText / CloseButton (TMP_Text + Button)
4. 컴포넌트 필드 + **`Character Catalog`** + **`Player Ship`** 연결
5. ⋮ → **Auto Layout**

---

## 단계 4 — PortScreen 에 버튼 추가

1. PortScreen GameObject 선택
2. 자식으로 새 Button 2개 생성:
   - 이름 **`ShipyardButton`** ("조선소")
   - 이름 **`PlazaButton`** ("광장")
3. PortScreen 컴포넌트 필드에 연결:
   - **Shipyard Panel** ← `ShipyardPanel`
   - **Shipyard Button** ← `ShipyardButton`
   - **Plaza Panel** ← `PlazaPanel`
   - **Plaza Button** ← `PlazaButton`

위치·크기는 기존 시장·모험가조합 버튼과 같은 줄에 배치 권장.

---

## 단계 5 — 다른 HUD 의 hideWhileAnyActive

다음 HUD 의 hide 배열에 `ShipyardPanel`, `PlazaPanel` 추가:
- JournalButton, MinimapHUD, AnchorButton, EnterPortButton, ActiveMissionHUD, WalletHUD

---

## 단계 6 — Play 테스트

1. ▶ Play → 항구 입항
2. **조선소** 탭 → 5개 배 표시 → 잔돈 충분하면 [구매] → 새 배로 교체
3. **광장** 탭 → 모험가 캐릭터들 표시 → [고용] → 새 선장으로 교체
4. 다시 항해 → 새 배 능력치 (cannonPower 등) 적용

---

## 가격·고용 공식

**배**: `ShipData.basePrice` 그대로

**선장**: `(bravery + seamanship + keenEye) × 10`
- 약한 선장 (각 50): 1,500G
- 평균 (각 70): 2,100G
- 강한 선장 (각 90): 2,700G

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 조선소 / 광장 버튼 안 보임 | PortScreen 의 필드 연결 / 버튼 자체 active |
| 패널 빈 행 | Catalog 의 all 배열 비었나 (Refresh All Catalogs) |
| 광장에 NPC 가 나옴 | CharacterRole.Adventurer 만 표시되므로 NPC (Townsperson) 는 자동 필터됨 — 정상 |
| 구매 버튼 비활성 (회색) | 잔돈 부족 또는 현재 사용 중인 배 / 선장 |
| 시장 / 조선소 패널이 겹침 | hideWhileAnyActive 에 서로 추가 |

---

## 추후 폴리시

| 작업 | 효과 |
|---|---|
| 항구별 거래 가능 배 제한 (ReputationGate 활용) | 명성 따라 게이팅 |
| 선원 풀 (10명 동시 고용) | 깊이 있는 전투 |
| 배 수리 (durability 시스템 도입 후) | 손상 회복 |
| 배·선장 일러스트 카드 | 시각 풍부 |
| 거북선 잠금 (조선 명성 높을 때만) | 콘텐츠 게이트 |
