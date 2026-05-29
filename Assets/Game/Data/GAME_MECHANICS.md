# 어린이 대항해 시대 — 시스템 디자인 (Game Mechanics)

> 이 문서는 게임의 **시스템 규칙(메카닉)**을 다룬다. 정책·플랫폼·빌드는 리포 루트의 `GAME_PREP.md`, 실제 항구·발견물 등의 콘텐츠 데이터는 같은 폴더의 `CONTENT_DESIGN.md` 가 다룬다.

작성 기준: 사용자가 2026-05-29에 제공한 기획 초안. **모든 결정은 그대로 반영**하되, 모호한 부분과 어린이 친화 톤 충돌 가능성을 별도 섹션으로 표시한다.

---

## 0. 책임 분리

| 다룬다 | 다루지 않는다 |
|---|---|
| 능력치·전투·명성·미션 등 시스템 규칙 | 빌드/플랫폼/정책 (→ `GAME_PREP.md`) |
| 데이터 모델 (캐릭터·배·시장·미션 등) | 실제 항구/발견물/특산물 데이터 (→ `CONTENT_DESIGN.md`) |
| 수치 범위·확률·시간 흐름 정의 | 일러스트·사운드·UI 시안 (→ 별도 아트 문서) |
| 어린이 친화 톤 조정 이슈 | 코드 구현 |

---

## 1. 캐릭터 · NPC 능력치

플레이어와 모든 NPC는 동일한 능력치 구조를 갖는다. NPC만 추가 항목이 있다.

### 1.1 공통 능력치

| 능력치 | 타입 | 범위 (잠정) | 효과 |
|---|---|---|---|
| **성별 (Gender)** | enum | Male / Female | **외형 only.** 게임 로직에 영향 없음. |
| **용기 (Bravery)** | int | 1 ~ 100 | **대포 공격력의 베이스 가중치.** 실제 데미지 = 배의 cannonPower × f(bravery). |
| **항해 능력 (Seamanship)** | int | 1 ~ 100 | **배 이동 속도에 +α.** 실제 속도 = 배의 speed × g(seamanship). |
| **눈썰미 (KeenEye)** | int | 1 ~ 100 | **발견물 탐색 시 허용 오차 확대.** 기본 ±3% → 눈썰미 보너스로 최대 ±5%까지. *[해석 결정 필요 — §8.1]* |
| **좋은 명성 (GoodReputation)** | int | 0 ~ 50,000 | §1.3 |
| **나쁜 명성 (BadReputation)** | int | 0 ~ 50,000 | §1.3 |

### 1.2 플레이어 전용

| 항목 | 타입 | 비고 |
|---|---|---|
| **돈 (Money)** | int | 특산물·배·수리·고용 비용 지불. 음수 불가. |
| **고용된 NPC (Crew)** | List<NpcRef> | 최대 **10명**. |
| **소유 배 (CurrentShip)** | ShipRef | 동시 1척 (멀티 함대 여부 미정 — §8.2). |

### 1.3 명성 (Reputation) — 양방향 트래킹

- **좋은 명성 (0~50,000)**: 의뢰 성공 또는 해적 퇴치로 누적.
- **나쁜 명성 (0~50,000)**: 상업선·호위선을 공격하면 누적.
- **두 값은 독립적으로 동시 보유 가능.** 한쪽이 늘어난다고 다른 쪽이 줄지 않는다. *[해석 결정 필요 — §8.3]*

명성에 따라 해제되는 것:

| 명성 종류 | 해제 대상 |
|---|---|
| 좋은 명성 | 일부 특산물·발견물 (의뢰), 고급 배, 정의로운 NPC 고용 |
| 나쁜 명성 | 일부 배(해적선 등), 일부 특산물(밀거래성), 거친 NPC 고용 |

> **⚠ Designed for Families 정책 충돌 위험**: "나쁜 명성을 쌓아야 얻을 수 있는 것"이 게임의 일부가 되면 어린이 앱 카테고리 심사에서 이슈가 될 수 있다. §6에서 톤 조정안을 별도로 다룬다.

### 1.4 NPC 고용 추가 항목

NPC는 1.1 공통 능력치에 더해 다음을 갖는다.

| 항목 | 타입 | 비고 |
|---|---|---|
| **NPC 유형 (Type)** | enum | Merchant(상업) / Escort(호위) / Pirate(해적) |
| **고용 보너스 (HireBonus)** | { bravery, seamanship, keenEye } | 고용되면 **플레이어 능력치에 +/− 합산**. 음수 가능. *[해석 결정 필요 — §8.4]* |
| **본거지 항구 (HomePortId)** | PortId | 해적의 경우 활동 반경 기준. |
| **상태 플래그** | enum | `AtPort` / `AtSea` / `Hired` / `Defeated` |
| **고용 비용 (HirePrice)** | int | 능력치·명성에 따라 자동 산출. |

### 1.5 데이터 모델

```csharp
public enum Gender { Male, Female }
public enum NpcType { Merchant, Escort, Pirate }
public enum NpcState { AtPort, AtSea, Hired, Defeated }

[CreateAssetMenu] public class CharacterData : ScriptableObject {
    public string characterId;
    public string displayName;
    public Gender gender;
    public Sprite portrait;            // 미니멀 톤 아바타
    public int bravery;                // 1~100
    public int seamanship;             // 1~100
    public int keenEye;                // 1~100
    // 명성은 런타임 상태(저장 데이터)로 분리 — SO에는 시작값만
    public int startingGoodReputation;
    public int startingBadReputation;
}

[CreateAssetMenu] public class NpcDefinition : ScriptableObject {
    public CharacterData character;
    public NpcType type;
    public string homePortId;          // for Pirate, also start point for others
    public Vector3Int hireBonus;       // x=bravery, y=seamanship, z=keenEye (음수 가능)
    public int hireBasePrice;
}

// 런타임 상태 — 저장 대상
[Serializable] public class NpcRuntimeState {
    public string npcId;
    public NpcState state;
    public string currentMissionId;
    public float missionEndsAtGameDay;  // 게임 내 시간 — §7
    public int currentDurability;       // 배의 내구도
}
```

---

## 2. 도시 (Port) 기능 5종

모든 항구가 5개 기능을 **모두** 갖는지, 항구별로 가진 기능이 다른지는 *[해석 결정 필요 — §8.5]*. 1차 가정: 모든 시작 항구 8개는 5개 기능을 모두 가진다.

### 2.1 시장 (Market)

- 특산물을 **사고 팔 수 있는 곳.**
- 항구별로 **3개 종류의 특산물**을 판매(자세한 거래 흐름은 §4).
- 도시별 기준 가격이 있고, **±20% 시세 변동**.
- **시세 갱신 시점**: 입항 시점에 새로 결정하는 것이 가장 단순. 동일 항구에 머무는 동안에는 고정. *[해석 결정 필요 — §8.6]*

### 2.2 모험가 조합 (Adventurers' Guild)

- **발견물 의뢰** 또는 **교역 의뢰** 를 받는 곳.
- **발견물 의뢰**: 좌표가 그려진 지도 아이템을 함께 받음. 발견물에 대한 짧은 정보 제공.
- **교역 의뢰**: "특정 지역에서 특정 특산물을 사 와라" 또는 "특정 지역에 특정 특산물을 가져다 주어라".
- **동시 보유 의뢰는 최대 1개.** 수행 중이면 다른 의뢰는 받을 수 없다.
- 의뢰 성공 시: 돈 + 좋은 명성. (실패/포기 정책은 *[해석 결정 필요 — §8.7]*)

### 2.3 조선소 (Shipyard)

배의 **매매 / 개조 / 업그레이드 / 수리**.

#### 배 능력 범위

| 능력 | 범위 |
|---|---|
| 대포 공격력 (cannonPower) | 1 ~ 30 |
| 이동 속도 (speed) | 1 ~ 10 |
| 적재 크기 (cargoCapacity) | 10 ~ 1000 |
| 내구도 (durability) | 10 ~ 200 |

#### 수리 비용 공식

> 사용자 기획 원문: *"수리는 내구도에 따라 판매한 금액의 내구도 10%당 판매한 금액의 1% 조건을 가짐, 내구도가 90% 파괴되어 있으면 판매한 금액의 9%"*

해석:
- 배의 판매가(혹은 구매가)를 기준으로,
- **잃은 내구도가 max의 10%마다 → 판매가의 1%** 가 수리비.
- 잃은 비율이 90%면 수리비는 판매가의 9%.

```
repairCost = shipPrice × floor(damagedRatio × 10) × 0.01
// damagedRatio = (maxDurability - currentDurability) / maxDurability
```

검증 예시:
- 100,000원짜리 배, 내구도 200 중 180 잃음 (90% 손상) → 100,000 × 9 × 0.01 = 9,000원. ✓

#### 개조·업그레이드

기획에 언급은 있으나 디테일 미정 — *[해석 결정 필요 — §8.8]*.

### 2.4 광장 (Plaza)

- **모험가(NPC)를 고용하는 곳.**
- 명성에 따라 **노출되는 NPC가 다르다** (좋은/나쁜 명성 기반).
- 광장에 있는 NPC는 §3.5 의 "비활성 풀(70%)"에 속하는 NPC.
- 한 번 고용되면 더 이상 바다에서 만나지 않는다. NPC 풀에서 제외.

### 2.5 항구 (Harbor)

- 배가 출항하는 지점.
- 시각적으로는 도시 입구이며, "출항" 버튼만 있는 단순 화면.

### 2.6 데이터 모델

```csharp
[CreateAssetMenu] public class PortFacilities : ScriptableObject {
    public string portId;
    public bool hasMarket = true;
    public bool hasGuild  = true;
    public bool hasShipyard = true;
    public bool hasPlaza = true;
    public bool hasHarbor = true;
    public List<ProductData> marketProducts;   // 3개 권장
    public List<ShipData> shipyardCatalog;     // 판매 배 목록
    public List<MissionTemplate> guildMissions;
}
```

---

## 3. 바다 (Sea) 시스템

### 3.1 세계지도 크기 / 이동 속도

- **가장 빠른 배(speed=10) + 최대 항해 능력 보정** 으로 **세계일주 10분.**
- 즉 게임 내 시간 / 실제 시간이 분리되어야 한다 (§7).
- 이동은 **바다 위에서만**. 육지 클릭은 무시(단, "정박 및 탐색"은 §3.2).
- 좌표 시스템은 위/경도 기반(콘텐츠에서 사용 중)이지만 실제 월드 좌표는 Unity Unit으로 변환 → 별도 매핑 테이블 필요. *[해석 결정 필요 — §8.9]*

### 3.2 항구 클릭 / 정박 및 탐색

- **바다 위에서 항구 아이콘 클릭** → 배가 그 항구로 **자동 이동**.
- 항구 도착 시 다이얼로그: **"항구로 들어가시겠습니까?"** (예 / 취소).
- **땅 또는 바다 클릭** → "정박 및 탐색" 가능. 현재 발견물 의뢰가 있고 좌표 ±3% 범위 내라면 발견물 획득. (자세한 흐름은 §5.)

### 3.3 NPC 분류와 행동

| 유형 | 인구 | 주요 행동 | 공격받으면 | 활동 범위 |
|---|---|---|---|---|
| **상업선 (Merchant)** | 50 | 가까운 또는 먼 지역으로 미션 이동 | **도망** | 전 세계 |
| **호위선 (Escort)** | 30 | 근처를 순찰 | **반격** | 근처 |
| **해적선 (Pirate)** | 20 | 본거지 근처를 떠돌다 일정 거리 내 적 발견 시 공격 | (공격자) | **본거지 항구 주변만** |

해적의 공격 규칙:
- 일정 거리 안으로 플레이어 / 상업선 / 호위선이 들어오면 추격·공격.
- 일정 거리 밖으로 벗어나면 추격 중단.

플레이어가 상업선·호위선·해적선을 공격하려면 **해상에서 상대를 직접 탭** 해야 한다 (§3.4 트리거 1). 의도하지 않은 충돌로 전투가 시작되는 일은 없다 — 단, **해적은 예외**로 NPC가 접근만 해도 트리거됨. 즉 어린이가 "실수로 공격해 나쁜 명성이 쌓이는" 사고는 발생하지 않도록 설계.

### 3.4 전투 시스템

#### 결과

| 상황 | 플레이어가 이김 | 플레이어가 짐 |
|---|---|---|
| 해적 상대 | **좋은 명성** + 돈 획득 | 돈 50% 손실, 배 50% 파괴 |
| 상업선/호위선 상대 | **나쁜 명성** + 돈 획득 | 돈 50% 손실, 배 50% 파괴 |

#### NPC 행동 (전투 후)
- 진 NPC: 사라지고 **랜덤 항구로 이동** (재배치).
- 이긴 NPC: 본인의 미션 계속 수행, 미션 완료 후 본거지 복귀.
- 미션 완료 시 NPC의 배 **내구도 100% 자동 회복.**

#### "50% 파괴" 의 정의
- 배의 현재 내구도가 **maxDurability의 50%로 깎임**(maxDurability=200이면 currentDurability=100). 0이 되지 않음.
- 즉 "배는 사라지지 않는다." 어린이 친화. *[해석 결정 필요 — §8.10]*

#### 전투 진행 방식 ✅ 확정 (2026-05-29)

**탭 → 자동 전투** 방식.

- **트리거 1 (플레이어 주도)**: 해상에서 상대 배 아이콘을 **탭** → 즉시 자동 전투 시작.
- **트리거 2 (해적 NPC 주도)**: 해적이 일정 거리 내로 접근하면 NPC 측에서 자동으로 전투 트리거.
- 전투 자체는 두 경우 모두 동일한 **자동 진행 + 결과 화면** 으로 통일.
- 자동 전투 결과는 다음 식의 가중 비교로 결정:
  ```
  attackerScore = (배의 cannonPower) × f(용기)
  // f(용기) 예시: 1.0 + (용기 / 100) × 0.5  → 용기 1일 때 1.005배, 100일 때 1.5배
  // 양측 score를 비교하되 약간의 랜덤성을 더해 결정 (±10% 정도)
  ```
- 어린이 친화 연출: 결과 화면 전 짧은 컷 — 두 배가 신호 깃발을 나누고, 노란 별·거품 이펙트, 패배 측은 돛이 펄럭이며 물러섬.
- 미니게임·턴제 도입 안 함.

### 3.5 NPC 인구 / 활동률 관리

#### 총 100명 분포

| 유형 | 수 | 활동률 | 동시 활동 최대 |
|---|---|---|---|
| 상업선 | 50 | 30% | ~15명 |
| 호위선 | 30 | 30% | ~9명 |
| 해적선 | 20 | 50% | ~10명 |

#### 활동 ↔ 비활동 전이 규칙

- **상업선 / 호위선**: 미션 종료 시 다시 30% 확률로 새 미션 추첨.
- **해적선**: 미션 종료 시 **본거지 항구로 복귀** → 다시 50% 확률로 추첨.
- 해적의 "본거지 체류 기간"은 **게임 내 10~20일** (랜덤). 그 기간이 지나면 다시 추첨.
- 비활동 NPC(상업/호위/해적 가리지 않고)는 **모두 광장에 노출** — 고용 대상.
- **고용된 NPC는 풀에서 영구 제외**.

#### 의문점
- 미션 종료된 NPC가 새로 추첨되어 활동에 못 들어간 경우(30% 실패): 바다에서 사라지고 광장으로 가야 하나? *[해석 결정 필요 — §8.12]*
- 광장에서 노출되는 NPC 목록은 입항 시점에 새로 결정되나? 매번 다른가? *[해석 결정 필요 — §8.13]*

### 3.6 데이터 모델

```csharp
[CreateAssetMenu] public class ShipData : ScriptableObject {
    public string shipId;
    public string displayName;
    public Sprite icon;
    public GameObject prefab3D;
    public int cannonPower;        // 1~30
    public int speed;              // 1~10
    public int cargoCapacity;      // 10~1000
    public int maxDurability;      // 10~200
    public int basePrice;
    public ReputationGate gate;    // 어느 명성 조건에서 구매 가능
}

[Serializable] public class ReputationGate {
    public int requiredGoodReputation;  // 0 이면 제한 없음
    public int requiredBadReputation;
}

public class NpcPool : MonoBehaviour {
    // 100명을 SO 정의에서 로드, 활동/비활동 상태를 매 게임일 갱신
    // 시드 기반 결정론적 추첨 권장 (저장/재현 안정성)
}
```

---

## 4. 특산물 · 시세 시스템

### 4.1 일반 vs 스페셜

| 종류 | 미션 없이도 매매? | 비고 |
|---|---|---|
| **일반 특산물** | ✅ 가능 (해당 항구에서 항상 매매) | 항구당 **3종** 노출. |
| **스페셜 특산물** | ❌ 미션을 통해서만 구매 가능 | 미션을 통해 **원하는 만큼** 구매 가능. |

### 4.2 미션 완료 후 일반화

- 특산물 매매 미션을 성공하면, **해당 특산물은 별도 미션 없이도 일반 매매 가능**해진다.
- 즉 도감(컬렉션) 진행에 따라 "거래 가능 특산물 목록"이 늘어난다 → **긴 모험의 진행감 핵심 메카닉.**

### 4.3 시세 변동 (±20%)

- 각 도시의 각 특산물은 **기준 가격(basePrice)** 보강.
- 입항 시점에 **−20% ~ +20%** 사이에서 임의 결정 (균등 분포 가정. *[해석 결정 필요 — §8.6]*).
- 같은 특산물의 시세가 **항구 A에서 −20%, 항구 B에서 +20%** 이면 차익 거래로 돈 벌이 가능 → 교역 미션의 동기.

### 4.4 데이터 모델

```csharp
[CreateAssetMenu] public class ProductData : ScriptableObject {
    public string productId;
    public string displayName;
    public Sprite icon;
    public int basePrice;
    public bool isSpecial;          // true면 미션을 통해서만 구매 가능
    public string originPortId;     // 원산지 (해설용)
}

// 런타임 — 입항 시점에 새로 계산
[Serializable] public class MarketSnapshot {
    public string portId;
    public List<MarketEntry> entries;
}

[Serializable] public class MarketEntry {
    public string productId;
    public float priceMultiplier;   // 0.8 ~ 1.2
    public int availableQuantity;
}
```

---

## 5. 발견물 미션 시스템

### 5.1 의뢰 받기 → 지도 아이템 획득

1. 항구의 모험가 조합에서 의뢰 리스트 표시.
2. 발견물 의뢰 선택 → **간단한 설명** (어린이용 1~2문장) 표시.
3. 수락 시 **지도 아이템 (MapItem)** 인벤토리에 추가.
4. 지도 아이템에는 **해당 발견물 지역명 + 좌표** 표시.

### 5.2 좌표 ±3% 탐색

- 플레이어가 지도 좌표 **±3%** 범위 내에 진입.
- 그 상태에서 "정박 및 탐색" 실행.
- 발견물 획득 + 도감 등록.
- 눈썰미 능력치가 높으면 허용 오차가 늘어남(§1.1 KeenEye 참고).

### 5.3 의뢰 항구 복귀

- 발견물을 가지고 의뢰 항구로 돌아가면 미션 성공.
- 보상: 돈 + 좋은 명성 + 도감 등록(영구).

### 5.4 항구별 고유

- 한 발견물 의뢰는 **특정 항구만** 발급할 수 있다(중복 없음).
- 즉 모든 항구를 방문해야 모든 발견물 의뢰를 풀 수 있다 → **항구 순회의 동기.**

### 5.5 발견물은 미션 없이 발견 불가

- 우연히 좌표에 도달해도 의뢰 없이는 발견 안 됨.
- 시스템적으로는 "활성화된 의뢰 매핑이 있는 지점만 검출".

### 5.6 데이터 모델

```csharp
public enum MissionType { Discovery, TradeBuy, TradeDeliver }

[CreateAssetMenu] public class MissionTemplate : ScriptableObject {
    public string missionId;
    public MissionType type;
    public string issuerPortId;
    public string targetPortId;        // for Trade
    public ProductData targetProduct;  // for Trade
    public DiscoveryData targetDiscovery; // for Discovery
    public int rewardMoney;
    public int rewardGoodReputation;
}

[Serializable] public class MapItem {
    public string mapItemId;
    public string discoveryId;
    public Vector2 targetCoordinate;     // 위도/경도
    public float searchToleranceBase = 0.03f; // ±3%
}
```

---

## 6. 어린이 친화 톤 조정 (중요 · 결정 필요)

> **이슈**: 사용자의 기획에는 "공격" "전투" "파괴" "나쁜 명성" "해적" 같은 요소가 자연스럽게 들어 있다. 게임 메카닉으로는 매력적이지만, **Designed for Families** 카테고리는 폭력·갈등 묘사에 매우 엄격하다. Play Store에서 어린이 앱 분류 심사가 막힐 위험이 실제 존재한다.

### 6.1 표현 톤 조정 권장안

| 시스템 용어 | 어린이 UI 표현 (권장) |
|---|---|
| 대포 공격력 | "용기" (이미 능력치명이 용기로 맞춰져 있음) |
| 전투 / 싸움 | **"신호 깃발 대결" / "겨루기"** |
| 공격하다 | "겨루다" / "도전하다" |
| 파괴 / 손상 | **"돛이 찢어짐" / "휴식이 필요함"** |
| 진다 / 패배 | "물러섬" / "지친 채 돌아옴" |
| 죽는다 | (사용 안 함. 배가 사라지거나 누가 죽는 표현 금지) |
| 해적 | (시각 표현은 무섭지 않게, 만화 톤. "바다의 무법자" 정도 표현 가능) |
| 나쁜 명성 | **"위험한 평판" / "거친 평판"** |

### 6.2 시각·청각 연출 규칙

- 빨간 폭발·핏빛 표현 **사용 금지.**
- 명중 시: 노란 별, 작은 거품, 가벼운 효과음.
- 패배 시: 짧은 컷씬으로 항구 자동 귀환. "다시 도전!" 같은 격려 문구.
- 해적 NPC의 외모: 무서운 해골/흉터 X. 모자·앵무새 같은 친근 클리셰만.

### 6.3 "나쁜 명성" 시스템 — ✅ 선택지 A 확정 (2026-05-29)

사용자가 원 기획대로 **그대로 유지**를 선택. 시스템은 변경 없음:
- 상업선·호위선 공격 → 나쁜 명성 누적.
- 나쁜 명성으로만 살 수 있는 배·특산물·고용 가능 NPC 존재.

§6.1·§6.2 의 **표현 톤 조정은 그대로 적용** (시스템이 유지된다고 시각·언어 표현까지 거칠어지는 것은 아님).

#### Play 카테고리 분류에 미치는 영향 — 사전 인지 필요

- "다른 캐릭터에 대한 부정적 행동(공격)을 명시적 메카닉(나쁜 명성 → 보상)으로 보상하는 게임"은 **Designed for Families** 신청이 거절될 가능성이 높다.
- 대안 카테고리는 §11.5(`GAME_PREP.md`)에 반영 — 권장은 **만 9~12세 + 만 13세 이상 혼합(Mixed Audience)** 또는 **만 13세 이상 단독**. 두 경우 모두 어린이 친화 톤(§6.1)은 그대로 유지하며 출시 가능.
- 즉 어린이 교육이라는 게임의 목적·톤은 보존하되, **Play Console의 공식 "Designed for Families" 라벨은 포기**할 가능성이 높다. 출시 자체는 가능.
- IARC 콘텐츠 등급: "환상 폭력(Cartoon Violence)" 항목에 해당될 수 있음. 빨강 폭발·핏빛 없음을 들어 최저 등급 신고 가능.

---

## 7. 게임 내 시간 시스템 (보강 필요)

기획에 직접 정의되지 않았지만 여러 곳에서 "시간" 이 등장:

- "세계일주 10분" → 실제 시간(분).
- "10~20일이라는 조건에 의해" → 게임 내 시간(일).
- 시세 변동 주기 → 게임 내 시간 단위?

### 7.1 잠정 정의

- **실시간 1초 ≈ 게임 내 6분**? → 실시간 10분에 게임 내 1일이 흐른다? *[수치 조정 필요]*
- **시간 흐름은 항해 중에만 진행**. 항구 안에서는 멈춤(어린이 친화 — 압박감 제거).
- **달력 / 계절**: 도입하지 않음 (1차 마일스톤 기준).

### 7.2 데이터 모델

```csharp
public static class GameClock {
    public static float CurrentGameDay { get; private set; }  // 0.0 ~ ∞
    public static bool IsTimePaused { get; set; }              // 항구 안 = true
    public static float SecondsPerGameDay = 600f;              // 잠정 — *[튜닝 필요]*
    public static void Tick(float deltaSeconds) {
        if (!IsTimePaused) CurrentGameDay += deltaSeconds / SecondsPerGameDay;
    }
}
```

---

## 8. 추가 결정이 필요한 항목 (사용자 확인 요청)

기획서를 시스템으로 정리하다 보니 더 정해야 할 것들. **모두 결정 시점은 1차 마일스톤 이후로 미뤄도 무방**하지만 일찍 정할수록 데이터 모델이 확정된다.

### §8.1 — "눈썰미: +α 지역 제공"의 정확한 의미
- 잠정 해석: 발견물 좌표 허용 오차가 ±3% → 눈썰미 보너스로 최대 ±5%.
- **대안 해석**: 미니맵에 발견물 후보 지역이 추가 표시됨. / 발견 가능 여부를 사전 알림. / 두 효과 모두.

### §8.2 — 함대(Fleet) 개념
- 플레이어는 동시에 1척만 운용? 여러 척 보유 가능?
- 잠정: 1척만. NPC 고용은 "동승 크루" 개념.

### §8.3 — 좋은 명성 ↔ 나쁜 명성 상쇄
- 둘이 독립 (양쪽 다 쌓일 수 있음)?
- 아니면 한쪽이 늘면 다른 쪽이 깎임?

### §8.4 — 고용 보너스의 적용 방식
- 단순 합산: 플레이어 용기 50 + 고용 NPC 보너스 5 → 55.
- 또는 최대치 갱신, 평균, 배수 등 다른 방식?
- 음수 보너스 NPC가 존재 가능한가?

### §8.5 — 모든 항구가 5개 기능 다 가지나
- 잠정: 시작 항구 8개는 모두 5개 보유. 작은 항구는 일부만 보유 가능.
- 어떻게 구분?

### §8.6 — 시세 변동 갱신 주기
- 잠정: 입항 시점 1회 결정 후 머무는 동안 고정.
- 또는 게임 내 1일마다 갱신, 항구별로 다른 주기 등.

### §8.7 — 의뢰 실패/포기 시 패널티
- 시간 초과? 정보 손실? 명성 깎임? 페널티 없음?

### §8.8 — 조선소의 "개조 / 업그레이드" 디테일
- 능력치별 업그레이드 비용·상한.
- 어떤 능력치를 올릴 수 있나? 다 올릴 수 있나?

### §8.9 — 위/경도 → Unity 월드 좌표 매핑
- 메르카토르 도법 사용 가정.
- 지구 곡률은 무시하는 평면 매핑.
- "세계일주 10분" 을 만족하는 월드 가로 길이는?

### §8.10 — "배 50% 파괴" 후 재항해 가능?
- 잠정: maxDurability의 50%로 깎이고 0은 안 됨. 항구에서 수리 가능.
- 또는 자동으로 가장 가까운 항구로 이동되어 강제 수리?

### §8.11 — 전투 진행 방식 ✅ 확정
- **탭 → 자동 전투** + 결과 화면 (§3.4 참조).
- 해적은 NPC 측 접근 트리거로 자동 전투 가능 (§3.3).

### §8.12 — 미션 완료 후 추첨 실패한 NPC의 표시 위치
- 즉시 광장으로 가서 고용 가능?
- 또는 본거지 항구로 자동 이동 후 광장 노출?

### §8.13 — 광장 NPC 목록 새로고침
- 입항 시점에 새로 결정? 항상 같은 풀? 게임 내 1일마다?

### §8.14 — "나쁜 명성" 시스템 ✅ 확정
- **선택지 A (그대로 유지).** §6.3 참조.
- Play "Designed for Families" 카테고리는 포기, 만 9~12세 + 만 13세 이상 혼합 또는 만 13세 이상 카테고리로 출시.

### §8.15 — 패배 시 50% 손실의 사용자 친화 처리
- "돈 50% 손실" 이 어린이에게 강한 좌절이 될 수 있음.
- 대안: 일정 금액 이하로는 깎이지 않음(예: 항상 1,000원은 남김), 또는 비율 더 작게.

---

## 9. 데이터 모델 — 전체 요약 (코드 시작점)

| ScriptableObject | 인스턴스 수 (1차 마일스톤) | 용도 |
|---|---|---|
| `NationData` | 8 | 국가 정의 |
| `PortData` | 2 (M1) → 8 (M2) → ~30 (M4) | 항구 정의 |
| `PortFacilities` | 항구당 1 | 항구 기능 매핑 |
| `ProductData` | ~3 (M1) → ~100 (M4) | 특산물 |
| `ShipData` | 5~10 | 배 종류 카탈로그 |
| `CharacterData` | 100 NPC + 8 플레이어 후보 = 108 | 캐릭터 정의 |
| `NpcDefinition` | 100 | NPC 본거지·유형·보너스 |
| `DiscoveryData` | 1 (M1) → 100 (M4) | 발견물 정의 |
| `MissionTemplate` | 항구당 수 개 | 의뢰 정의 |
| `RegionData` | 5~12 | 지역 묶음 |

런타임 저장 데이터(JSON):
- `SaveSlot`: 플레이어 명성, 돈, 보유 배, 현재 미션, 도감 진행, 일반화된 특산물 목록, 게임 시간, NPC 풀 상태(시드 또는 명시적 상태 배열).

---

## 10. 다음 작업 제안

1. **§8.1 ~ §8.15 결정 받기** — 사용자 확인 후 본문에 반영.
2. **`CONTENT_DESIGN.md` 확장** — 능력치를 가진 NPC 100명·플레이어 후보 8명을 채워나갈 표 추가.
3. **`GAME_PREP.md §5 데이터 모델` 보강** — 위 §9 표를 GAME_PREP.md 에 짧게 복사 / 링크.
4. **1차 마일스톤(M1) 스코프 재확인** — M1에 전투·고용·시장·미션을 모두 넣을 수는 없으므로, 어디까지 넣을지 명시.

### M1 권장 스코프 (재정의)
- 항해 + 항구 클릭 + 입항 다이얼로그
- 시장(특산물 매매) - 단순
- 모험가 조합 - 발견물 의뢰 1개 (지브롤터 해협)
- 발견물 탐색 (정박 및 탐색)
- 미션 보상 (돈 + 좋은 명성)
- **제외**: 전투, NPC 풀, 광장(고용), 조선소(매매/개조)

전투·고용·조선소·100명 NPC 풀은 **M2 이후**에 넣는 게 현실적.
