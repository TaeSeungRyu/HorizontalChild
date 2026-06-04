# 미니맵 셋업 가이드

우상단에 작은 세계지도 + 실시간 플레이어 위치 마커.

기획서: GAME_PREP.md §12.2 M3 "항해 길잡이 — 미니맵"

1차 구현 = 정적 베이스맵 + 플레이어 마커. 항구/발견물 마커는 2차에서 추가 예정.

---

## 동작

- 세계지도 텍스처를 미니맵 배경으로 표시 (S23 가로 기준 ~370×185 px 권장)
- 플레이어 위/경도를 UV 로 환산해 마커 위치 갱신 (매 프레임)
- 옵션: 진행 방향(yaw) 에 따라 마커 회전

---

## 단계 1 — UI 구조 만들기

기존 Canvas 가 있다면 그 안에 추가. 없으면 새로 만들기.

### Canvas (이미 있을 가능성 큼)

```
Canvas (Screen Space Overlay)
└ MinimapPanel        ← 미니맵 컨테이너
   └ BasemapImage     ← 세계지도 RawImage
      └ PlayerMarker  ← 플레이어 위치 표시 (BasemapImage 의 자식!)
```

### A. MinimapPanel (RectTransform)

1. Canvas 우클릭 → Create Empty → 이름 `MinimapPanel`
2. RectTransform 설정:

| 항목 | 값 |
|---|---|
| Anchor Preset | top-right (Alt + Shift 누르고 우상단 클릭) |
| Pivot | (1, 1) |
| Anchored Position | (-10, -10) — 우상단에서 10px 안쪽 |
| Width × Height | 370 × 185 |

### B. BasemapImage (RawImage)

1. MinimapPanel 우클릭 → UI → Raw Image → 이름 `BasemapImage`
2. RectTransform: anchor stretch-stretch, offsets 모두 0 (Panel 전체 채움)
3. **RawImage 컴포넌트** → `Texture` 필드에 **`Assets/Game/Art/Map/HYP_LR_SR_W.tif`** 드래그
4. **Raycast Target** ☐ 해제 (미니맵 위의 다른 UI 클릭 가로채지 않게)

### C. PlayerMarker (Image)

1. BasemapImage 우클릭 → UI → Image → 이름 `PlayerMarker`
2. RectTransform 설정:

| 항목 | 값 |
|---|---|
| Anchor | middle-center |
| Pivot | (0.5, 0.5) |
| Width × Height | 16 × 16 |
| Anchored Position | (0, 0) — 매 프레임 스크립트가 갱신 |

3. **Image** → 색상 진한 빨강 또는 노랑 (배경과 대비)
4. (선택) Source Image 에 작은 화살표 sprite 사용 — `rotateMarkerByHeading` ☑ 일 때 방향 표시
5. **Raycast Target** ☐ 해제

---

## 단계 2 — MinimapHUD 컴포넌트 부착

1. `MinimapPanel` 선택 → Add Component → **`Minimap HUD`**
2. 필드 연결:

| 필드 | 값 |
|---|---|
| **Basemap Image** | `BasemapImage` (RawImage) |
| **Player Marker** | `PlayerMarker` (RectTransform) |
| **Player Ship** | 씬의 `PlayerShip` (비워두면 자동 검색) |
| **Rotate Marker By Heading** | ☑ 권장 |

---

## 단계 3 — Play 테스트

1. ▶ Play
2. 국가 선택 → 시작 항구 spawn
3. 미니맵 우상단 표시 확인
4. 플레이어 마커가 시작 항구 위치에 표시되어야 함 (예: 리스본이면 미니맵 서유럽 쪽)
5. 배를 움직이면 마커도 실시간 이동

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 미니맵 자체가 안 보임 | Canvas Render Mode = Screen Space Overlay 확인 / MinimapPanel Active ☑ |
| 베이스맵 텍스처 비어 보임 | BasemapImage 의 Texture 필드에 HYP_LR_SR_W.tif 할당했는지 |
| 플레이어 마커 안 보임 | PlayerMarker 가 BasemapImage 의 **자식** 인지 / Image 색 alpha 0 아닌지 |
| 마커 위치가 미니맵 영역 밖 | PlayerMarker 의 anchor 가 middle-center 인지 / pivot (0.5, 0.5) 인지 |
| 마커가 안 움직임 | MinimapHUD 의 PlayerShip 필드에 ShipController 가 연결됐는지 |
| 미니맵 영역이 다른 UI 가림 | MinimapPanel 의 위치/크기 조정, 다른 HUD 와 겹치지 않게 |

---

## 다음 폴리시 (M3 후속)

| 작업 | 시간 |
|---|---|
| 발견한 발견물 위치에 작은 점 표시 (MissionService.DiscoveredIds 기반) | 30분 |
| 발견한 항구 위치에 점 표시 | 30분 |
| 현재 활성 의뢰 목적지에 화살표 / 깜빡임 | 30분 |
| 카메라 위치를 보여주는 사각 프레임 | 1시간 |
| 미니맵 탭 시 큰 지도 모드 열기 | OpenMap 액션 연결 |

이 항목들은 GAME_PREP.md M3 의 "항해 길잡이" 섹션 후속.

---

## 화면 폭에 비례한 자동 크기 조정 (선택)

해상도가 다른 기기에서도 미니맵이 일정 비율로 보이게 하려면:
- MinimapPanel 의 width 를 Canvas Scaler 의 Reference Resolution 기준 1/5 정도로 (S23 가로 2340 → ~470)
- 또는 Canvas Scaler 의 Match 값 조정

기본 370×185 는 S23 가로(2340×1080) 에서 ~16% 폭이라 적당. 다른 기기에서 비율이 안 맞으면 Canvas Scaler 의 Match 값을 0.5 로.
