# 자금/명성 HUD 셋업 가이드

화면 우상단(또는 좌상단) 에 작은 패널 — 돈/좋은 평판/나쁜 평판 표시.

---

## 단계 1 — HUD 패널 만들기

1. Hierarchy 의 **Canvas** 아래에 **UI ▸ Panel** 추가, 이름 **`WalletHUD`**
2. RectTransform:
   - **Anchor**: top-right (우상단) — AnchorButton 과 겹치지 않게
     - 또는 top-left 옆 ActiveMissionHUD 위/아래
   - **Width**: 500, **Height**: 200
3. 배경 색: 어두운 갈색/베이지 (옛 일지 톤) + 알파 180

> 위치 충돌 주의 — AnchorButton/EnterPortButton 이 우상단에 있으면 살짝 이동 (예: 우상단 X=-50, Y=-180).

---

## 단계 2 — 자식 UI

`WalletHUD` 하위:

| 자식 | 종류 | 이름 | 비고 |
|---|---|---|---|
| 1 | UI ▸ Text - TextMeshPro | `MoneyText` | "5,000 원" — 가장 큼 (32pt) |
| 2 | UI ▸ Text - TextMeshPro | `GoodReputationText` | "좋은 평판 100" (24pt, 옅은 녹색) |
| 3 | UI ▸ Text - TextMeshPro | `BadReputationText` | "위험한 평판 0" (24pt, 옅은 빨강) — 자동 숨김 |

자동 레이아웃을 적용하면 텍스트 길이가 변해도 패널이 따라 늘어남:

1. **`WalletHUD`** 에 Add Component → **Vertical Layout Group**
   - Child Alignment: Upper Left
   - Control Child Size: Width ☑, Height ☐
   - Use Child Scale: 둘 다 ☐
   - Child Force Expand: Width ☑, Height ☐
   - Padding: Left/Right/Top/Bottom 모두 15
   - Spacing: 5
2. **`WalletHUD`** 에 Add Component → **Content Size Fitter**
   - Vertical Fit: **Preferred Size** (높이 자동)

---

## 단계 3 — 컴포넌트 부착

1. **`WalletHUD`** 선택
2. Add Component → **`Wallet HUD`** 검색·추가
3. 필드 채우기:

| 필드 | 값 |
|---|---|
| Money Text | 자식 MoneyText 드래그 |
| Good Reputation Text | 자식 GoodReputationText 드래그 |
| Bad Reputation Text | 자식 BadReputationText 드래그 |
| Player State | 비워둠 (런타임 자동) |
| Hide While Any Active | 배열 크기 5 — 다른 HUD 와 동일 (PortArrivalDialog / PortScreen / MissionGiverPanel / DiscoveryFoundPanel / MissionCompletedPanel) |

---

## 단계 4 — 테스트

1. ▶ Play
2. 시작 직후 HUD 확인:
   ```
   5,000 원
   좋은 평판 0
   ```
   (`BadReputationText` 는 0 이라 안 보임)
3. 의뢰 받기 → 발견 → 리스본 복귀 → 보상 받기 후 HUD 자동 갱신:
   ```
   6,000 원              ← 5,000 + 1,000
   좋은 평판 100
   ```

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| HUD 가 안 보임 | RectTransform 위치/크기 확인. AnchorButton 영역과 겹쳤을 수도 |
| 보상 받아도 갱신 안 됨 | PlayerState 가 GameManager 에 부착되었는지 / Console 에 "PlayerState 인스턴스 없음" 경고 |
| 위험한 평판이 항상 보임 | 정상 — 0 이면 자동 숨김, 0보다 크면 표시 (M1 에선 안 쌓이므로 사실상 안 보일 것) |
| 텍스트가 패널 밖으로 튀어 나옴 | Vertical Layout Group + Content Size Fitter 적용 안 됨. 단계 2 재확인 |

---

## 동작 흐름

```
[게임 시작] PlayerState.Money = 5000, GoodRep = 0
  ↓
HUD: "5,000 원 / 좋은 평판 0"
  ↓
[의뢰 완료 → 보상]
  PlayerState.AddMoney(1000)
  → onMoneyChanged.Invoke(6000)
  → WalletHUD.OnMoneyChanged(6000)
  ↓
HUD: "6,000 원 / 좋은 평판 100"
```

---

이로써 M1 의 "보상이 어디 갔는지 모름" 문제 해결.

다음 단계 후보:
- **패널 자동 레이아웃 일괄 적용** — 다른 패널들도 정리
- **M2 진입** — 8개국 선택 + 각국 시작 항구
- **도감 보기 UI** — 발견한 항목 모아보기
- **종료 버튼 / 메인 메뉴** — 어린이 친화
