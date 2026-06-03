# 국적 선택 UI 셋업 가이드 (M2.2)

게임 시작 시 8개국 중 하나를 선택 → 선택된 국적의 시작 항구로 배 배치.

---

## 단계 1 — GameSession 컴포넌트 부착

`GameSession` 은 현재 게임의 메타 상태 (선택된 국적 등) 보관.

1. Hierarchy 의 **`GameManager`** 클릭
2. Add Component → **`Game Session`** 검색·추가
3. 필드는 비워둠 — 런타임에 자동 채워짐

---

## 단계 2 — SeaWorldManager 에 9개 항구 모두 등록

기존 2개 (리스본·세우타) 외에 신규 7개 항구도 월드에 표시.

1. Hierarchy 의 **`GameManager`** 클릭
2. Inspector 의 **`Sea World Manager`** 의 **`Active Ports`** 배열:
   - **Size** = **9**
   - Element 0~1: 기존 (Port_Lisbon, Port_Ceuta)
   - Element 2: **Port_Sevilla** 드래그
   - Element 3: **Port_Venezia** 드래그
   - Element 4: **Port_Amsterdam** 드래그
   - Element 5: **Port_London** 드래그
   - Element 6: **Port_Istanbul** 드래그
   - Element 7: **Port_Busan** 드래그
   - Element 8: **Port_Guangzhou** 드래그

→ Play 시 9개 빨간 원기둥이 각 좌표에 표시됨.

---

## 단계 3 — NationButton Prefab 만들기

각 국가 버튼의 시각 prefab.

### 3-1. 빈 GameObject

1. Hierarchy 의 **Canvas** 아래에 임시로 **UI ▸ Button - TextMeshPro** 추가, 이름 **`NationButtonTemplate`**
2. RectTransform 크기: **width 400, height 200** (적당한 카드 크기)
3. Image 색상은 흰색 (코드에서 NationData.accentColor 로 덮어씀)

### 3-2. NationButton 컴포넌트 추가

1. **`NationButtonTemplate`** 선택
2. Add Component → **`Nation Button`** 검색·추가
3. 필드 자동 채워짐:
   - Button: 자기 자신
   - Background Image: 자기 Image
   - Name Label: 자식 Text - TextMeshPro

### 3-3. Prefab 화

1. Hierarchy 의 **`NationButtonTemplate`** 을 **`Assets/Game/Prefabs/`** 폴더로 드래그 → prefab 생성
2. Hierarchy 의 원본 삭제 (씬에서 제거)

---

## 단계 4 — NationSelectionPanel 만들기

### 4-1. 패널 GameObject

1. Hierarchy 의 **Canvas** 아래에 **UI ▸ Panel** 추가, 이름 **`NationSelectionPanel`**
2. RectTransform 으로 화면 전체 덮음 (Anchor stretch + Left/Top/Right/Bottom 0)
3. 배경 색은 어두운 파랑 (옛 항해 일지 분위기) + 알파 240

### 4-2. 자식 UI

`NationSelectionPanel` 하위:

| 자식 | 종류 | 이름 | 비고 |
|---|---|---|---|
| 1 | UI ▸ Text - TextMeshPro | `TitleText` | "어느 나라로 시작할까요?" (60pt 큰 폰트) |
| 2 | UI ▸ Panel (또는 빈 GameObject + Image) | `ButtonContainer` | 8개 버튼 들어갈 컨테이너 |
| 3 | UI ▸ Text - TextMeshPro | `SelectedNameText` | 선택된 국가 이름 |
| 4 | UI ▸ Text - TextMeshPro | `SelectedGreetingText` | 선택된 국가 인사말 |
| 5 | UI ▸ Button - TextMeshPro | `ConfirmButton` | "이 나라로 시작" |

### 4-3. ButtonContainer 에 GridLayoutGroup

8개 버튼을 4x2 또는 2x4 격자로 배치:

1. **`ButtonContainer`** 선택
2. Add Component → **Grid Layout Group**
3. 필드:
   - **Cell Size**: X=400, Y=200 (NationButtonTemplate 크기)
   - **Spacing**: X=20, Y=20
   - **Start Corner**: Upper Left
   - **Constraint**: Fixed Column Count, **Count = 4** (4열 2행)
4. ButtonContainer 의 RectTransform 크기: 약 1700×440 (가로 4개 * (400+20) - 20)

### 4-4. NationSelectionPanel 컴포넌트 부착

1. **`NationSelectionPanel`** 선택
2. Add Component → **`Nation Selection Panel`** 검색·추가
3. 필드 채우기:

| 필드 | 값 |
|---|---|
| Panel Root | 비워둠 |
| Auto Show On Start | ☑ (시작 시 자동 표시) |
| Nations | **배열 크기 8** — 8개 NationData 드래그:<br>① Nation_Portugal<br>② Nation_Spain<br>③ Nation_Italy<br>④ Nation_Netherlands<br>⑤ Nation_England<br>⑥ Nation_Ottoman<br>⑦ Nation_Joseon<br>⑧ Nation_China |
| Button Container | 자식 `ButtonContainer` 드래그 |
| Nation Button Prefab | `Assets/Game/Prefabs/NationButtonTemplate` 드래그 |
| Selected Name Text | 자식 `SelectedNameText` 드래그 |
| Selected Greeting Text | 자식 `SelectedGreetingText` 드래그 |
| Confirm Button | 자식 `ConfirmButton` 드래그 |
| Player Ship | Hierarchy 의 `PlayerShip` 드래그 |
| Game Session | 비워둠 (런타임 자동) |

### 4-5. 자동 레이아웃 (선택)

패널이 자동으로 늘어나게:

- **Selected Greeting Text** 에 Wrapping Enabled (기본값)
- (선택) **NationSelectionPanel** 에 Vertical Layout Group + Content Size Fitter

---

## 단계 5 — 다른 HUD 의 hideWhileAnyActive 에 추가

국적 선택 떠 있는 동안 다른 UI 가려야 자연스러움.

다음 5개 컴포넌트의 `Hide While Any Active` 배열에 **NationSelectionPanel** 추가:

1. **AnchorButton** → 배열 크기 +1, NationSelectionPanel 추가
2. **EnterPortButton** → 같은 방식
3. **ActiveMissionHUD** → 같은 방식
4. **WalletHUD** → 같은 방식

---

## 단계 6 — 첫 테스트

1. ▶ Play
2. 게임 시작 직후 **국적 선택 패널** 자동 표시:
   ```
   어느 나라로 시작할까요?

   [포르투갈] [스페인]  [이탈리아] [네덜란드]
   [영국]     [오스만]  [조선]     [중국]

   (인사말 영역)

   [이 나라로 시작]
   ```
3. **포르투갈** 클릭 → 선택된 국가 영역에:
   ```
   포르투갈

   환영합니다, 어린 항해사님!
   우리 포르투갈은 가장 먼저 큰 바다로 나갔답니다.
   함께 새로운 땅을 찾아볼까요?

   [이 나라로 시작]   ← 활성화됨
   ```
4. **[이 나라로 시작]** 클릭 → 패널 사라지고 PlayerShip 이 **리스본 좌표 (-136, 0, 580)** 로 자동 이동
5. Console 로그:
   ```
   [GameSession] 국적 선택: nation.portugal (포르투갈)
   ```
6. 다른 국적 선택 시 해당 시작 항구로 이동:
   - 조선 → 부산 (1935, 0, 527)
   - 중국 → 광저우 (1700, 0, 346)
   - 등

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 8개 버튼 안 보임 | Nations 배열에 SO 들이 들어있는지, ButtonContainer/NationButtonPrefab 이 할당됐는지 |
| 버튼 색이 다 흰색 | NationButton 의 Background Image 가 자동 인식 안 됨. Inspector 에서 직접 할당 |
| 시작 클릭 후 PlayerShip 이동 안 함 | Player Ship 필드가 비어있음. Hierarchy 의 PlayerShip 드래그 |
| 패널이 너무 작거나 화면 밖 | RectTransform Anchor stretch + Left/Top/Right/Bottom 0 으로 화면 전체 |

---

## 동작 흐름

```
[Play 시작]
  → NationSelectionPanel.Show() 자동 호출
  → 8개 NationButton 동적 생성 + GridLayout 배치
  → 각 버튼의 배경색 = NationData.accentColor

[사용자가 포르투갈 카드 클릭]
  → OnNationClicked(portugal)
  → 인사말 영역에 portugal.greeting 표시
  → ConfirmButton 활성화

[사용자가 "이 나라로 시작" 클릭]
  → GameSession.SetSelectedNation(portugal)
  → PlayerShip.position = LatLngToWorld(38.7, -9.1) — 리스본
  → PlayerShip.HardStop()
  → 패널 숨김 → 게임 시작
```

---

## 다음 단계 후보

- **M2.3** — 선택된 국적의 시작 자금/명성 적용 (CharacterData 의 startingGoodReputation 등)
- **M2.4** — 선택된 국적의 캐릭터를 PlayerShip.captain 에 자동 할당
- **M3 진입** — 메카닉 풀세트, 전투, NPC 100명 등
- **저장 시스템** — 선택된 국적·진행도 JSON 저장
