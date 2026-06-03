# 카탈로그(Catalog) 시스템 셋업 가이드

## 무엇을 해결하나

매번 새 SO 를 추가할 때마다 SeaWorldManager·MissionService·JournalPanel·NationSelectionPanel 의 배열에 일일이 드래그하는 부담 → **카탈로그 1개만 컴포넌트에 연결, 새 SO 는 메뉴 한 번으로 자동 등록**.

```
[옛 방식]                          [새 방식]
새 Discovery SO 추가              새 Discovery SO 추가
  ↓                                  ↓
SeaWorldManager 배열에 드래그        Game ▸ Refresh All Catalogs 메뉴 클릭
JournalPanel 배열에 드래그           ↓
M2MissionSeeder 가이드 갱신          DiscoveryCatalog 자동 갱신
  ↓                                  ↓
3곳 모두 수동 작업                  3개 컴포넌트가 카탈로그 참조 → 자동
```

---

## 한 번 셋업 (5분)

### 단계 1 — 4개 Catalog SO 생성

Project 창에서 한 번에 생성:

1. Project 창의 **`Assets/Game/Data/`** 폴더 클릭
2. 우클릭 → **`Create ▸ Folder`** → 이름 **`_Catalogs`**
3. 그 폴더 안에서 **우클릭 → Create → Game/Data/**:
   - **`Discovery Catalog`** → 이름 그대로 `DiscoveryCatalog`
   - **`Mission Catalog`** → `MissionCatalog`
   - **`Port Catalog`** → `PortCatalog`
   - **`Nation Catalog`** → `NationCatalog`

→ `Assets/Game/Data/_Catalogs/` 에 4개 SO 생성됨.

### 단계 2 — 메뉴로 자동 채우기

1. 메뉴 바: **`Game ▸ Refresh All Catalogs`** 클릭
2. Console 로그:
   ```
   [CatalogRefresher] DiscoveryCatalog.asset ← 9개 DiscoveryData
   [CatalogRefresher] MissionCatalog.asset ← 9개 MissionTemplate
   [CatalogRefresher] PortCatalog.asset ← 9개 PortData
   [CatalogRefresher] NationCatalog.asset ← 8개 NationData
   [CatalogRefresher] 완료. 총 35개 SO 가 카탈로그에 등록됨.
   ```
3. 각 Catalog SO 클릭 → Inspector 의 `all` 배열에 SO 들이 자동으로 채워졌는지 확인

### 단계 3 — 4개 컴포넌트에 카탈로그 연결 (한 번만)

#### 3-1. SeaWorldManager (GameManager)
1. Hierarchy 의 **`GameManager`** 클릭
2. Inspector 의 **`Sea World Manager`** 에서:
   - **`Port Catalog`** 칸에 `PortCatalog` 드래그
   - **`Discovery Catalog`** 칸에 `DiscoveryCatalog` 드래그
3. (선택) 기존 `Active Ports` / `Active Discoveries` 배열은 비워둬도 OK — 카탈로그가 우선

#### 3-2. MissionService (GameManager)
1. 같은 GameManager 의 **`Mission Service`**:
   - **`Mission Catalog`** 칸에 `MissionCatalog` 드래그
2. 기존 `All Missions` 배열은 비워도 OK

#### 3-3. JournalPanel
1. Hierarchy 의 **`JournalPanel`** 클릭
2. Inspector 의 **`Journal Panel`** 에서:
   - **`Discovery Catalog`** 칸에 `DiscoveryCatalog` 드래그
3. 기존 `All Discoveries` 배열 비워도 OK

#### 3-4. NationSelectionPanel
1. Hierarchy 의 **`NationSelectionPanel`** 클릭
2. Inspector 의 **`Nation Selection Panel`** 에서:
   - **`Nation Catalog`** 칸에 `NationCatalog` 드래그
3. 기존 `Nations` 배열 비워도 OK

---

## 이후 워크플로 — 매번 1단계만

새 SO 를 시더 또는 수동으로 추가했을 때:

1. **Project 창에서 새 SO 가 정상 생성됐는지** 확인
2. **메뉴: `Game ▸ Refresh All Catalogs`** 클릭
3. → 카탈로그 자동 갱신 → 모든 컴포넌트가 자동으로 새 SO 인식

**드래그 작업 0회.** 끝.

---

## 메뉴 옵션

| 메뉴 | 동작 |
|---|---|
| `Game ▸ Refresh All Catalogs` | 4개 카탈로그 모두 갱신 (가장 자주 사용) |
| `Game ▸ Refresh Discovery Catalog` | DiscoveryCatalog 만 갱신 |
| `Game ▸ Refresh Mission Catalog` | MissionCatalog 만 갱신 |
| `Game ▸ Refresh Port Catalog` | PortCatalog 만 갱신 |
| `Game ▸ Refresh Nation Catalog` | NationCatalog 만 갱신 |

---

## 동작 원리

각 컴포넌트에 `EffectiveXxx` 프로퍼티가 추가됨:

```csharp
// 카탈로그가 채워져 있으면 그것을 우선, 아니면 인스펙터 배열 fallback
public PortData[] EffectivePorts =>
    (portCatalog != null && portCatalog.all != null && portCatalog.all.Length > 0)
        ? portCatalog.all : activePorts;
```

→ 카탈로그를 등록하지 않은 옛 씬·코드는 그대로 동작 (배열 fallback).
→ 카탈로그 등록 후엔 자동으로 카탈로그 우선 사용.

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 메뉴 클릭해도 "Discovery Catalog 인스턴스 없음" 경고 | 단계 1 (4개 카탈로그 SO 생성) 안 했음. Project 우클릭 → Create → Game/Data |
| 카탈로그는 채워졌는데 게임에 반영 안 됨 | 컴포넌트의 카탈로그 필드에 SO 가 드래그됐는지 확인 |
| 게임은 동작하는데 새 SO 안 보임 | `Game ▸ Refresh All Catalogs` 실행했는지 |
| Console 에 빨간 컴파일 에러 | 4개 카탈로그 SO 의 코드(`DiscoveryCatalog.cs` 등) 가 빠졌는지 확인 |

---

## 카탈로그 확장 (M3 이후)

추가로 ProductCatalog / CharacterCatalog / ShipCatalog 등이 필요해지면:

1. 같은 패턴으로 `XxxCatalog.cs` 추가
2. `CatalogRefresher.cs` 의 `RefreshAll()` 에 한 줄 추가:
   ```csharp
   total += RefreshCatalogOfType<XxxCatalog, XxxData>(c => c.all, (c, arr) => c.all = arr);
   ```
3. 컴포넌트에 동일한 `EffectiveXxx` 패턴

---

## 시드 vs 카탈로그 vs 컴포넌트 관계

```
[시드]  M1ContentSeeder / M2*Seeder
   ↓ SO 인스턴스 생성
[SO]   Assets/Game/Data/Discoveries/Discovery_*.asset 등
   ↓ Refresh 메뉴
[카탈로그]  DiscoveryCatalog.all = [모든 Discovery SO]
   ↓ Inspector 1회 드래그
[컴포넌트]  SeaWorldManager.discoveryCatalog → EffectiveDiscoveries → 게임 동작
```

새 SO 추가 흐름:
1. 시더 메뉴 클릭 또는 SO 수동 생성
2. `Refresh All Catalogs` 메뉴 클릭
3. 끝

기존 컴포넌트 배열에 드래그할 필요 없음.
