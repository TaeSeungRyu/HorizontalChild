# 저장 (Save) 시스템 셋업 가이드

자동 저장·자동 로드. JSON 단일 슬롯. GAME_PREP §11.4 기반.

---

## 저장 대상

| 항목 | 비고 |
|---|---|
| 선택 국가 | NationData.nationId |
| 발견한 항목 IDs | MissionService.DiscoveredIds |
| 완료 의뢰 IDs | MissionService.CompletedMissionIds |
| 해제 지역 IDs | MissionService.UnlockedRegionIds |
| 현재 의뢰 ID | nullable, 1건 |
| 잔돈 / 좋은 명성 / 나쁜 명성 | PlayerState |
| 화물 (productId, qty) | PlayerCargo |
| 마지막 위치 (x, z) | 월드 좌표 |

저장 안 함 (추후): 시세 스냅샷, 캐릭터 / 배 변경, 게임 시간.

---

## 자동 저장 트리거

코드에서 자동 등록:
1. 항구 입항 직후 (`SeaWorldManager.onPortArrived`)
2. 발견물 획득 (`MissionService.onDiscoveryRegistered`)
3. 새 지역 해제 (`MissionService.onRegionUnlocked`)
4. 의뢰 완료 (`MissionService.onMissionCompleted`)
5. 거래 (cargo 변동, `PlayerCargo.onCargoChanged`)
6. 앱 일시정지 (`OnApplicationPause(true)`)

수동 저장 버튼은 어린이용이라 만들지 않음 (자동 저장 신뢰).

---

## 단계 1 — ProductCatalog 생성

Save/Load 시 화물의 productId → ProductData 매핑에 필요.

1. Project 창의 `Assets/Game/Data/_Catalogs/` 폴더
2. 우클릭 → **Create → Game/Data → Product Catalog** → 이름 `ProductCatalog`
3. 메뉴 **`Game ▸ Refresh All Catalogs`** 실행 → 자동 채움

---

## 단계 2 — SaveService GameObject 셋업

1. Hierarchy 의 **`GameManager`** (또는 다른 영구 GameObject) 선택
2. Add Component → **`Save Service`**
3. Inspector 필드 채우기:

| 필드 | 값 |
|---|---|
| Nation Catalog | `NationCatalog` SO |
| Discovery Catalog | `DiscoveryCatalog` SO |
| Mission Catalog | `MissionCatalog` SO |
| Region Catalog | `RegionCatalog` SO |
| Product Catalog | `ProductCatalog` SO |
| Player Ship | 씬의 `PlayerShip` (비워두면 자동 검색) |

`On Saved` / `On Loaded` UnityEvent 는 토스트 알림 등에 연결 가능 (선택).

---

## 단계 3 — Play 테스트

### 첫 실행 (저장 없음)
1. ▶ Play
2. **국가 선택 화면** 나옴 → 한 국가 선택
3. 항해·발견·거래 → 자동 저장 작동 (Console 에 `[SaveService] 저장 완료 →` 로그)
4. Stop

### 재실행 (저장 있음)
1. ▶ Play
2. **국가 선택 화면 자동 skip** — 이미 로드됨
3. 콘솔에 `[SaveService] 로드 완료. 저장 시각: ...` 메시지
4. 잔돈·발견·화물 등 모두 복원됨

---

## 저장 파일 위치

```
Windows: C:\Users\<user>\AppData\LocalLow\<company>\<product>\save_slot_0.json
Mac:     ~/Library/Application Support/<company>/<product>/save_slot_0.json
Android: /data/data/<package>/files/save_slot_0.json
```

Unity 콘솔 로그에 경로가 정확히 찍힘.

---

## 디버그 메뉴

SaveService 컴포넌트 ⋮:

| 메뉴 | 동작 |
|---|---|
| **Save Now** | 즉시 저장 |
| **Load Now** | 즉시 로드 (현재 진행 덮어씀) |
| **Delete Save** | 저장 파일 삭제 (다음 실행은 새 게임) |

테스트 중 새 게임 시작하려면 → **Delete Save** → ▶ Play.

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 저장 안 됨 | GameSession.SelectedNation 이 null 일 때 SaveGame() 무시함 (정상 — 국가 선택 전엔 저장 안 함). 국가 선택 후에 트리거 발동되는지 확인 |
| 화물 복원 안 됨 | ProductCatalog 필드 비어있나 / Catalog 의 all 배열이 채워졌나 (`Refresh All Catalogs`) |
| 국가 선택 화면이 또 뜸 | NationCatalog 의 nationId 와 저장된 nationId 일치 안 함. 시드 다시 한 뒤 NationCatalog 재생성 필요할 수 있음 |
| 현재 의뢰가 복원 안 됨 | MissionCatalog 의 missionId 와 저장값 불일치 / 이미 완료 IDs 에 들어있어서 거부됨 |
| 위치가 (0,0) 으로 복원 | playerShip 필드 비어있고 자동 검색 실패. 직접 드래그 |

---

## 추후 폴리시

| 작업 | 시간 |
|---|---|
| 멀티 슬롯 (1~3개) | 1~2시간 — Save 슬롯 선택 UI 추가 |
| 마켓 스냅샷 저장 | 30분 — MarketSnapshot 직렬화 추가 |
| 게임 시간 추적 | 30분 — 누적 플레이 시간 |
| 토스트 알림 (저장됨!) | 30분 — onSaved 이벤트에 UI 연결 |
| Manual Save 버튼 | 어린이용엔 불필요하지만 디버그용으로 가능 |
| 클라우드 동기화 | M5+ 정식 출시 시점 (현재는 § 11 정책상 로컬 only) |
