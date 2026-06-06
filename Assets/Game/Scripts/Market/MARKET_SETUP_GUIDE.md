# 시장 (Market) 시스템 셋업 가이드

항구 상품 매매 + 항구별 시세 ±20% 변동.

기획서: GAME_PREP.md §12.2 M3 메카닉 풀세트 — "시세 ±20% 변동"

---

## 시스템 구성

| 컴포넌트 | 위치 | 역할 |
|---|---|---|
| `PlayerCargo` | GameManager (싱글톤) | 화물 인벤토리 + ShipData.cargoCapacity 기반 용량 |
| `MarketService` | GameManager (싱글톤) | 항구별 시세 스냅샷 + 매매 로직 (돈/화물 갱신) |
| `MarketPanel` | Canvas (UI) | 매매 UI — Auto Layout 지원 |
| `PortScreen` 갱신 | Canvas | "시장" 버튼 추가됨 |

---

## 단계 1 — 싱글톤 컴포넌트 부착

### PlayerCargo

1. Hierarchy 의 **GameManager** (또는 PlayerShip) GameObject 선택
2. Add Component → **`Player Cargo`**
3. 필드:
   - **Player Ship** — 비워두면 자동 검색
   - **Default Capacity** — 100 (ShipData 가 없을 때 fallback)

### MarketService

1. **GameManager** GameObject 선택
2. Add Component → **`Market Service`**
3. 필드:
   - **Min Multiplier** — 0.8 (기본)
   - **Max Multiplier** — 1.2 (기본)
   - **Sell Ratio** — 0.9 (구매가의 90% 로 판매)

---

## 단계 2 — MarketPanel UI 만들기

### 2-1. 패널 GameObject

1. Canvas 우클릭 → **Create Empty** → 이름 **`MarketPanel`**
2. Add Component → **`Market Panel`**
3. **(선택)** UI 자식들 미리 추가 — 아래 자동 레이아웃이 알아서 만들어주므로 안 해도 OK:
   - TitleText (TMP_Text)
   - MoneyText (TMP_Text)
   - CargoText (TMP_Text)
   - CloseButton (Button)

### 2-2. 필드 연결

이미 추가한 자식이 있으면 드래그, 없으면 그대로 두기 (Auto Layout 이 만들어줌):

| 필드 | 값 |
|---|---|
| Panel Root | 자기 자신 (비워두면 자동) |
| Title Text | TitleText (있으면) |
| Money Text | MoneyText (있으면) |
| Cargo Text | CargoText (있으면) |
| Rows Container | 비워둠 (Auto Layout 이 ScrollView/Content 자동 생성) |
| Close Button | CloseButton (있으면) |

### 2-3. Auto Layout 실행

Inspector 의 Market Panel 컴포넌트 ⋮ → **`Auto Layout`** 클릭.
- 패널 크기 1800×900 중앙
- TitleText 상단
- MoneyText / CargoText 양옆
- ScrollView + Content 자동 생성 (rowsContainer 자동 연결)
- CloseButton 하단

---

## 단계 3 — PortScreen 에 시장 버튼 연결

1. Hierarchy 의 **PortScreen** GameObject 선택
2. Inspector → **Port Screen (Script)**
3. 필드:
   - **Market Panel** ← Hierarchy 의 `MarketPanel` GameObject 드래그
   - **Market Button** ← PortScreen 안에 새 Button 추가 (예: "시장" 라벨) 후 드래그

(PortScreen 의 UI 구조에 시장 버튼 자리가 없으면 임시로 화면 아무 곳에 Button 만 추가해도 됨 — UX 폴리시 단계에 자리 잡으면 됨.)

---

## 단계 4 — 다른 HUD 의 hideWhileAnyActive 에 MarketPanel 추가

MarketPanel 떠있는 동안 미니맵·도감버튼·앵커버튼 등이 가려지도록:

각 HUD 의 `Hide While Any Active` 배열에 **MarketPanel** 추가.

---

## 단계 5 — Play 테스트

1. ▶ Play → 국가 선택 → 시작 항구 입항
2. PortScreen 의 "시장" 버튼 클릭
3. MarketPanel 표시 — 각 행:
   ```
   올리브유    사기 38G    팔기 34G    보유 0    [사기] [팔기]
   ```
4. 사기 클릭 → 잔돈 -38G, 화물 +1, 보유 1
5. 팔기 클릭 → 잔돈 +34G, 화물 -1, 보유 0
6. 다른 항구로 항해 → 같은 상품 가격이 다를 수 있음 (스냅샷이 항구마다 다름)
7. 외부 항구 화물도 판매 가능 (그 항구의 판매가 적용)

---

## 가격 계산

```
구매가 = product.basePrice × snapshot.multiplier[product]
판매가 = 구매가 × sellRatio   (기본 0.9)
```

- snapshot.multiplier 는 항구 첫 방문 시 0.8 ~ 1.2 사이 랜덤 결정
- 한 번 결정된 스냅샷은 세션 동안 유지 (Save 시스템 도입 후 영구 저장)

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 시장 버튼 비활성 | PortScreen 의 Market Panel / Market Button 필드 채워졌나 |
| 패널 열려도 행이 없음 | 항구의 commonProducts / specialProducts 가 비어있나 (PortData asset 확인) |
| 사기 버튼 비활성 | 잔돈 부족 또는 화물 용량 초과 (Inspector 의 PlayerCargo 보기) |
| 팔기 버튼 비활성 | 보유 수량 0 |
| 가격이 항상 같음 | MarketService.Instance 가 작동 중인지 확인 / 항구별 다른 스냅샷이 생성됐는지 |
| Auto Layout 안 보임 | Unity 컴파일 끝나야 컨텍스트 메뉴 등장 |

---

## 추후 폴리시 (M3.5+)

| 작업 | 효과 |
|---|---|
| 수량 슬라이더 (1·5·10·전부) | 클릭 횟수 ↓ |
| 가격 변동 표시 (↑/↓ 색) | 매매 시각 직관 |
| 가격 변동 (시간 흐름) | 시세 게임플레이 |
| 특산물 vs 일반 가격 구분 | 깊이 |
| 항구별 가격 비교 모달 | 거래 계획 보조 |
| 거래 알림 토스트 | 피드백 |
