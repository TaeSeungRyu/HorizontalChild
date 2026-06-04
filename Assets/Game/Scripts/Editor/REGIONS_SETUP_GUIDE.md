# 지역(Region) 시스템 셋업 가이드

발견·항구를 지역으로 묶고, 항구 방문 시 그 지역이 자동 해제 → 도감에 "지역 N/모두 6" 진행도 표시.

기획서: GAME_PREP.md §12.2 M3 "`RegionData` 의 잠금 해제 시스템 작동"

---

## 6개 지역

| 지역 | 시작 해제 국가 | 항구 수 | 발견물 수 |
|---|---|---|---|
| 이베리아 반도 | Portugal, Spain | 6 | 6 |
| 지중해 서부 | Italy | 4 | 3 |
| 북해 | Netherlands, England | 6 | 3 |
| 동지중해와 이집트 | Ottoman | 3 | 6 |
| 조선 해역 | Joseon | 3 | 3 |
| 중국 해역 | China | 3 | 4 |
| **합계** | | **25** | **25** |

---

## 단계 1 — 지역 시드

1. Unity 메뉴 **`Game ▸ Seed M3 Regions`** 클릭
2. Console 확인 — 6개 지역 정리 로그
3. `Assets/Game/Data/Regions/` 폴더에 6개 `.asset` 파일

기존 `Region_Iberia` 는 내용만 갱신 (참조 보존).

---

## 단계 2 — RegionCatalog 생성

1. `Assets/Game/Data/_Catalogs/` 폴더 (없으면 만들기)
2. 우클릭 → **Create → Game/Data → Region Catalog** → 이름 `RegionCatalog`
3. 메뉴 **`Game ▸ Refresh All Catalogs`** 실행
4. RegionCatalog Inspector 에서 `all` 배열에 6개 항목 자동 채워짐 확인

---

## 단계 3 — MissionService 에 RegionCatalog 연결

1. Hierarchy 에서 **MissionService** GameObject 클릭 (GameManager 또는 그와 같은 곳)
2. Inspector → **Mission Service** 컴포넌트
3. **`Region Catalog`** 필드 ← `RegionCatalog` SO 드래그

---

## 단계 4 — (선택) JournalPanel 에 RegionCatalog 연결

도감에 지역 진행도 표시하려면:
1. Hierarchy 의 `JournalPanel` 클릭
2. Inspector → **Journal Panel** 컴포넌트
3. **`Region Catalog`** 필드 ← `RegionCatalog` SO 드래그

도감 열면 상단에 "발견 5 / 모두 25     지역 2 / 모두 6" 식으로 표시.

---

## 동작 원리

```
[국가 선택]
  NationSelectionPanel.OnConfirmClicked
    → MissionService.RegisterStartingNation(nation)
        → RegionData.unlockedAtStartFor 에 그 국가가 있는 지역들 자동 해제

[항구 도착]
  SeaWorldManager.PublishPortArrival(port)
    → MissionService.RegisterPortVisit(port)
        → FindRegionForPort → 해당 지역 자동 해제
        → onRegionUnlocked 이벤트 발행 (UI 토스트 연결 가능)
```

지역 해제 = `MissionService.UnlockedRegionIds` 에 ID 추가.

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 지역 안 보임 | `Game ▸ Seed M3 Regions` 실행했는지 / `Refresh All Catalogs` 실행했는지 |
| Catalog 비어있음 | RegionCatalog SO 가 `_Catalogs` 폴더에 존재하는지 |
| 시작 국가 지역이 자동 해제 안 됨 | RegionData 의 `unlockedAtStartFor` 배열에 그 국가가 들어있는지 / MissionService 에 RegionCatalog 연결됐는지 |
| 항구 방문해도 해제 안 됨 | MissionService 인스턴스가 씬에 존재하는지 (singleton) |

---

## 추후 폴리시 (M3.5 또는 M4)

- 새 지역 해제 시 토스트 알림 ("새 지역 해제: 지중해 서부!")
- BigMapPanel 에 미해제 지역은 안개 마스킹
- 의뢰 발급을 unlocked 지역으로 제한 (gating)
- 도감 카테고리에 "지역별" 탭 추가
