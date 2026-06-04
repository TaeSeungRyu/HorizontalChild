# 도감 (Journal) UI 셋업 가이드

발견한 항목 모아보기. 화면 어딘가에 "도감" 버튼 + 클릭 시 큰 패널 표시.

---

## 단계 1 — JournalPanel 만들기

### 1-1. 패널 GameObject

1. Hierarchy 의 **Canvas** 아래에 **UI ▸ Panel** 추가, 이름 **`JournalPanel`**
2. RectTransform 으로 화면 중앙 큰 패널 (예: 2000×950)
3. 배경 색은 옛 일지 톤 (짙은 갈색/베이지) + 알파 240 (다른 UI 가림)

### 1-2. 자식 UI

`JournalPanel` 하위:

| 자식 | 종류 | 이름 | 비고 |
|---|---|---|---|
| 1 | UI ▸ Text - TextMeshPro | `TitleText` | "도감" (60pt) |
| 2 | UI ▸ Text - TextMeshPro | `CountText` | "발견 1 / 모두 20" (28pt, 연한 회색) |
| 3 | UI ▸ Text - TextMeshPro | `EntryListText` | 본문 — 발견물 목록 (22pt). 자동 줄바꿈 ☑ |
| 4 | UI ▸ Button - TextMeshPro | `CloseButton` | "닫기" (하단 우측) |

### 1-3. EntryListText 자동 레이아웃 권장

본문이 길어질 수 있으니:
1. **`JournalPanel`** 또는 별도 컨테이너에 **Vertical Layout Group**
2. **EntryListText** 의 Wrapping Enabled (기본값)
3. (선택) 더 많은 항목 대비 **Scroll View** 사용 — M3 폴리시에서

M1 에는 발견물 1~몇 개이므로 스크롤 없이도 OK.

### 1-4. JournalPanel 컴포넌트 부착

1. **`JournalPanel`** 선택
2. Add Component → **`Journal Panel`** 검색·추가
3. 필드 채우기:

| 필드 | 값 |
|---|---|
| Panel Root | 비워둠 (자동) |
| Title Text | 자식 TitleText 드래그 |
| Count Text | 자식 CountText 드래그 |
| Entry List Text | 자식 EntryListText 드래그 |
| Close Button | 자식 CloseButton 드래그 |
| **Reopen Panel** | 씬의 `DiscoveryFoundPanel` 드래그 (비워두면 자동 검색) |
| **Discovery Catalog** | `DiscoveryCatalog` SO 드래그 — Catalog 우선 사용 |
| All Discoveries | 비워둠 (Catalog 가 채워주므로) — Catalog 없을 때만 fallback |
| Mission Service | 비워둠 (런타임 자동) |

### M3 추가 기능 — 발견 항목 탭 시 상세 패널 재실행

- 발견한 항목은 이름이 밑줄·굵게 표시되고 클릭 가능
- 탭 시 `DiscoveryFoundPanel` 이 재실행 (일러스트 + 해설 다시 표시)
- 보상 없는 정보 열람 전용 — 발견 시 보상은 이미 처음에만 지급됨
- 카테고리별 그룹화 + 진행도 표시 (예: "[랜드마크] 2/8 발견")

> `Reopen Panel` 필드를 채워두면 즉시 작동. 비워두면 Show() 시 자동으로 씬에서 검색.

---

## 단계 2 — JournalButton 만들기

### 2-1. 버튼 GameObject

1. Canvas 아래에 **UI ▸ Button - TextMeshPro** 추가, 이름 **`JournalButton`**
2. RectTransform — 화면 어딘가 (예: 우상단 AnchorButton 아래, 또는 좌상단 ActiveMissionHUD 옆)
3. 크기: 200×100 정도
4. 자식 Text → **"도감"**

### 2-2. 컴포넌트 부착

1. **`JournalButton`** 선택
2. Add Component → **`Journal Button`** 검색·추가
3. 필드:

| 필드 | 값 |
|---|---|
| Button | 비워둠 (자동) |
| Journal Panel | Hierarchy 의 `JournalPanel` 드래그 |
| Hide While Any Active | 배열 크기 6 (기존 5 + JournalPanel 자체) — 다음 6개 패널 모두 등록:<br>① PortArrivalDialog<br>② PortScreen<br>③ MissionGiverPanel<br>④ DiscoveryFoundPanel<br>⑤ MissionCompletedPanel<br>⑥ JournalPanel (도감 자기 자신도) |

> JournalPanel 자기 자신을 등록하는 이유: 도감 열려 있을 때 도감 버튼도 같이 가려져야 자연스러움.

---

## 단계 3 — 다른 HUD 의 hideWhileAnyActive 에 추가

도감 패널이 떠있는 동안 다른 HUD 도 가려야 자연스러움.

다음 5개 컴포넌트의 `Hide While Any Active` 배열 마지막에 **JournalPanel** 추가:

1. **AnchorButton** → Size +1
2. **EnterPortButton** → Size +1
3. **ActiveMissionHUD** → Size +1
4. **WalletHUD** → Size +1
5. (있다면) **NationSelectionPanel 옆 영역의 다른 HUD**

각 컴포넌트에 한 항목씩 추가.

---

## 단계 4 — 첫 테스트

### 시나리오 1 — 발견 전

1. ▶ Play
2. 의뢰 받기 전 또는 발견 전 상태에서 **"도감"** 버튼 클릭
3. 도감 패널 표시:
   ```
   도감
   발견 0 / 모두 1

   [???] 아직 발견하지 못한 곳

   [닫기]
   ```
4. "닫기" → 다시 게임 화면

### 시나리오 2 — 발견 후

1. 의뢰 받기 → 지브롤터 발견
2. **"도감"** 버튼 클릭
3. 도감 패널 표시:
   ```
   도감
   발견 1 / 모두 1

   [랜드마크] 지브롤터 해협
     유럽과 아프리카 사이에 있는 좁은 바닷길이에요.
     큰 바다(대서양)와 잔잔한 바다(지중해)를 이어주지요.
     ...

   [닫기]
   ```

### 시나리오 3 — 발견물 카탈로그 늘리기 (M3+)

추후 발견물 SO 가 추가되면 JournalPanel 의 `All Discoveries` 배열에 추가:
- 발견 안 한 것: "???"
- 발견한 것: 카테고리 + 이름 + 해설

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 도감 버튼 누르면 패널이 안 뜸 | JournalButton 의 `Journal Panel` 필드가 비어있음. 드래그 |
| 도감 패널은 뜨는데 내용 비어있음 | `All Discoveries` 배열에 SO 가 안 들어있음. Project 창에서 DiscoveryData 들 드래그 |
| "발견 0 / 모두 0" 만 표시 | All Discoveries 배열 자체가 비어있음 |
| 발견했는데도 "???" 로 표시 | MissionService.DiscoveredIds 에 등록 안 됨. AnchorButton 발견 흐름 다시 확인 |
| 도감 버튼이 패널 위에 보임 | JournalButton 의 Hide While Any Active 배열에 JournalPanel 자기 자신도 등록 |

---

## 동작 흐름

```
[게임 진행 중]
  도감 버튼 표시 (다른 패널 없을 때만)
  ↓
[도감 버튼 클릭]
  JournalPanel.Show()
  → MissionService.DiscoveredIds 와 allDiscoveries 매칭
  → 발견한 항목: 카테고리 + 이름 + 해설
  → 못 찾은 항목: "???"
  → 발견 / 전체 카운트 표시
  ↓
[닫기]
  panelRoot.SetActive(false)
  → 다른 HUD 다시 표시
```

---

## M1 §4 체크리스트 마지막 항목 완료

원래 GAME_PREP.md §4 의 M1 마일스톤 체크리스트:
- [x] 도감 ScriptableObject + 발견 시 등록 + 도감 보기 UI

→ **"도감 보기 UI" 가 본 작업으로 완성**. M1 의 모든 항목 ✅.

다음 단계 후보:
- **M3 진입** — 메카닉 풀세트 (전투·NPC 풀·시세·조선소)
- **패널 자동 레이아웃 일괄 적용** — 폴리시
- **각국별 시작 자금/의뢰 차등** — M2 풍부함
- **종료 버튼 / 메인 메뉴** — UX 개선
