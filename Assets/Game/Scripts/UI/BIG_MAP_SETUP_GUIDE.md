# 큰 지도 모드 셋업 가이드

전체 화면 세계 지도 — 모든 항구 + 발견한 발견물 + 플레이어 위치 표시.

기획서: GAME_PREP.md §12.2 M3 "항해 길잡이 — 큰 지도 모드"

---

## 동작

- HUD 버튼(`BigMapButton`) 탭 → 큰 지도 전체 화면 표시
- 항구 마커 (빨간 점) — 모든 항구 표시
- 발견물 마커 — 발견한 것만 표시 (카테고리별 색)
- 플레이어 위치 마커 — 매 프레임 갱신
- Close 버튼 또는 같은 토글 버튼 다시 탭 → 닫기

---

## 단계 1 — UI 구조 만들기

기존 Canvas 안에 추가:

```
Canvas (Screen Space Overlay)
└ BigMapPanel              ← 이 컴포넌트 부착, 시작 시 비활성
   ├ Background            ← 어두운 풀스크린 오버레이 (탭 차단)
   ├ MapContainer          ← 화면 중앙, 적당히 큰 사각형
   │  └ BasemapImage       ← RawImage + EARTH.jpg
   │     └ MarkersContainer ← 마커들의 부모 (anchor stretch)
   │        └ PlayerMarker ← 플레이어 위치 표시
   ├ TitleText             ← TMP_Text "세계 지도"
   └ CloseButton           ← Button + Image (X 또는 "닫기")
```

### A. BigMapPanel (RectTransform)

1. Canvas 우클릭 → Create Empty → 이름 **`BigMapPanel`**
2. RectTransform: anchor stretch-stretch, offsets 모두 0 (전체 화면)

### B. Background (Image)

1. BigMapPanel 우클릭 → UI → Image → 이름 **`Background`**
2. Anchor stretch, offsets 0
3. Image color: 검정 alpha 0.7 (반투명 어둠)
4. **Raycast Target ☑** (뒤쪽 클릭 차단)

### C. MapContainer (RectTransform)

1. BigMapPanel 우클릭 → Create Empty → 이름 **`MapContainer`**
2. Anchor middle-center, pivot (0.5, 0.5)
3. Size 2000 × 1000 (or 화면에 적당히 — 2:1 비율 권장)

### D. BasemapImage (RawImage)

1. MapContainer 우클릭 → UI → Raw Image → 이름 **`BasemapImage`**
2. Anchor stretch, offsets 0 (MapContainer 전체 채움)
3. **Texture** ← `Assets/Game/Art/Map/EARTH.jpg` 드래그
4. Raycast Target ☐ 해제

### E. MarkersContainer (RectTransform)

1. BasemapImage 우클릭 → Create Empty → 이름 **`MarkersContainer`**
2. Anchor stretch, offsets 0 (BasemapImage 전체 덮음)
3. 추가 컴포넌트 없음

### F. PlayerMarker (Image, MarkersContainer 자식)

1. MarkersContainer 우클릭 → UI → Image → 이름 **`PlayerMarker`**
2. Anchor middle-center, pivot (0.5, 0.5)
3. Size 24 × 24 (미니맵보다 좀 더 큼)
4. Color 진한 빨강 또는 노랑
5. Raycast Target ☐

### G. TitleText (TMP_Text)

1. BigMapPanel 우클릭 → UI → Text — TextMeshPro → 이름 **`TitleText`**
2. Anchor top-center, 적당히 위치
3. Text "세계 지도", 크기 36~48, 색 흰색

### H. CloseButton (Button)

1. BigMapPanel 우클릭 → UI → Button → 이름 **`CloseButton`**
2. Anchor top-right, pivot (1, 1), position (-20, -20)
3. Text 또는 X 아이콘
4. 사이즈 60×60 정도

---

## 단계 2 — BigMapPanel 스크립트 부착·연결

1. **BigMapPanel** GameObject 선택 → Add Component → **`Big Map Panel`**
2. 필드 연결:

| 필드 | 값 |
|---|---|
| **Panel Root** | BigMapPanel 자기 자신 (또는 비워두면 자동) |
| **Basemap Image** | `BasemapImage` (RawImage) |
| **Markers Container** | `MarkersContainer` |
| **Player Marker** | `PlayerMarker` |
| **Title Text** | `TitleText` |
| **Close Button** | `CloseButton` |
| **Port Catalog** | `PortCatalog` SO 드래그 |
| **Discovery Catalog** | `DiscoveryCatalog` SO 드래그 |
| **Mission Service** | 씬의 GameManager (MissionService 가 있는 GameObject) |
| **Player Ship** | `PlayerShip` (비워두면 자동 검색) |

---

## 단계 3 — 미니맵을 큰 지도 열기 버튼으로 만들기

별도 버튼 없이 **미니맵 자체를 탭하면 큰 지도가 열림.**

1. Hierarchy 에서 **`MinimapPanel`** 클릭
2. Inspector → **Add Component** → **`Button`** 추가
3. Button 컴포넌트의 **`Target Graphic`** ← `MinimapPanel` 자신의 Image (RoundedPanel 이 추가해놓은 Image 자동 인식)
4. **OnClick ()** 리스트 → `+` 버튼으로 항목 추가:
   - 첫 칸 (Object Reference) ← Hierarchy 의 **`BigMapPanel`** 드래그
   - 두 번째 드롭다운 ← **`BigMapPanel ▸ Toggle()`** 선택

설정 후 미니맵 어디든 탭하면 → `BigMapPanel.Toggle()` 호출 → 큰 지도 열림.

> Tip: 큰 지도 열린 상태에선 미니맵이 숨겨지므로 (단계 4 의 hideWhileAnyActive), 미니맵으로 닫을 수 없음. 닫을 때는 **CloseButton** 사용.

---

## 단계 4 — 다른 HUD 의 hideWhileAnyActive 에 등록

`BigMapPanel` 이 열리면 다른 HUD 가 가려져야 함:

- `JournalButton` → Hide While Any Active 에 BigMapPanel 추가
- `MinimapHUD` → Hide While Any Active 에 BigMapPanel 추가
- `WalletHUD`, `AnchorButton`, `EnterPortButton`, `ActiveMissionHUD` 등 동일

---

## 단계 5 — Play 테스트

1. ▶ Play
2. 국가 선택 → 항해 시작
3. **미니맵 탭** → 큰 지도 패널 등장
4. **모든 항구가 빨간 점으로** 표시 (25개 — 8개국 시작항구 + 16개 신규)
5. **발견한 발견물만 카테고리 색 점으로** 표시 (처음엔 없음, 발견하면 추가)
6. **플레이어 위치 마커**가 현재 lat/lng 에 표시
7. **CloseButton** 탭 → 닫기 → 미니맵 다시 등장

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 미니맵 탭해도 안 열림 | MinimapPanel 의 Button 컴포넌트 OnClick 에 `BigMapPanel.Toggle()` 연결 확인 / Target Graphic 비어있지 않은지 |
| 지도가 안 보임 | BasemapImage 의 Texture 에 EARTH.jpg 할당했는지 |
| 마커가 안 보임 / 잘못된 위치 | MarkersContainer 가 BasemapImage 의 **자식** 이고 anchor stretch 인지 |
| 항구 마커가 모두 같은 위치에 뭉침 | PortCatalog 의 all 배열이 비어있음 — `Game ▸ Refresh All Catalogs` 실행 |
| 발견물 마커가 안 보임 | 정상 — 아직 발견 안 했으니까. 발견 후 패널 다시 열면 보임 |
| 플레이어 마커가 안 움직임 | Player Ship 필드 연결 확인 |
| 닫기 후 다른 HUD 안 돌아옴 | 다른 HUD 의 hideWhileAnyActive 에 BigMapPanel 추가했는지 확인 (반대로 등록한 거 아닌지) |
| 마커가 화면 밖으로 나감 | MapContainer 크기를 화면보다 작게 (예: 1800x900) — 마커 좌표가 컨테이너 비율 |

---

## 다음 폴리시 (선택)

| 작업 | 효과 |
|---|---|
| 마커 클릭 → 항구 정보 팝업 | 항구 학습 강화 |
| 핀치 줌 + 두 손가락 팬 | 항구 모인 곳 확대해서 보기 |
| 활성 의뢰 목적지 깜빡임 / 화살표 | 어디로 가야 할지 시각 |
| 발견물 마커 클릭 → 발견 패널 재실행 (Reopen Discovery) | 도감 보완 |
| 시각 스타일 — 항해도 톤 (낡은 종이 + 잉크) | 게임 콘셉트 강화 |

---

## 카탈로그 자동 갱신 잊지 말 것

새 항구·발견물 시드 후 반드시:
- **`Game ▸ Refresh All Catalogs`**

안 하면 BigMapPanel 이 새 데이터를 못 보고 옛 카탈로그 기준으로만 마커 spawn.
