# 발견물 탐색 UI 셋업 가이드 (B4·B5)

`AnchorButton` 과 `DiscoveryFoundPanel` 두 UI 컴포넌트 셋업.

---

## 단계 1 — AnchorButton 만들기

### 1-1. 버튼 GameObject

1. Hierarchy 의 **Canvas** 아래에 **UI ▸ Button - TextMeshPro** 추가, 이름 **`AnchorButton`**
2. RectTransform 으로 화면 **우상단** 적당한 위치에 배치 (예: width 320, height 100)
3. 버튼 자식 Text 텍스트 → **"정박 및 탐색"**

### 1-2. 상태 메시지 텍스트 만들기

AnchorButton 아래에 결과 메시지를 표시할 텍스트를 추가:

1. Hierarchy 의 **Canvas** 아래에 (AnchorButton 옆에) **UI ▸ Text - TextMeshPro** 추가
2. 이름 **`AnchorStatusText`**
3. RectTransform 으로 AnchorButton 바로 아래쪽에 배치 (예: width 600, height 80, AnchorButton 아래 ~120px)
4. 처음엔 비활성화 상태로 — 컴포넌트에서 자동 처리되므로 GameObject 비활성은 안 해도 됨

### 1-3. AnchorButton 컴포넌트 부착

1. **`AnchorButton`** 선택
2. Add Component → **`Anchor Button`** 검색·추가
3. 필드 채우기:

| 필드                   | 값                                                  |
| ---------------------- | --------------------------------------------------- |
| Button                 | 비워둠 (자동으로 자기 자신의 Button 사용)           |
| Status Text            | 자식 또는 형제 GameObject `AnchorStatusText` 드래그 |
| Status Visible Seconds | 3 (기본값)                                          |
| Player Ship            | Hierarchy 의 PlayerShip 드래그                      |
| Mission Service        | 비워둠 (런타임 자동)                                |
| Discovery Found Panel  | 다음 단계에서 만든 후 할당                          |

---

## 단계 2 — DiscoveryFoundPanel 만들기

### 2-1. 패널 GameObject

1. Canvas 아래에 **UI ▸ Panel** 추가, 이름 **`DiscoveryFoundPanel`**
2. RectTransform 으로 화면 중앙에 큰 패널 (예: 2000×950)
3. 배경 색은 어두운 베이지 (옛 일지 분위기), 알파 240 (PortScreen 가리기)

### 2-2. 자식 UI

`DiscoveryFoundPanel` 하위:

| 자식 | 종류                      | 이름                        | 위치 / 비고                                          |
| ---- | ------------------------- | --------------------------- | ---------------------------------------------------- |
| 1    | UI ▸ Text - TextMeshPro   | `HeaderText`                | 상단 — "새로운 곳을 발견했어요!" (큰 폰트)           |
| 2    | UI ▸ Text - TextMeshPro   | `NameText`                  | 헤더 아래 — 발견물 이름 (50pt)                       |
| 3    | UI ▸ Text - TextMeshPro   | `CategoryText`              | 이름 옆 또는 아래 — 카테고리 (20pt 작게)             |
| 4    | UI ▸ Image                | `IllustrationImage`         | 중간 영역 — 일러스트 (M1 임시 비어둠)                |
| 5    | UI ▸ Panel(또는 Image)    | `NoIllustrationPlaceholder` | IllustrationImage 자리에 같이, 일러스트 없을 때 보임 |
| 6    | UI ▸ Text - TextMeshPro   | `DescriptionText`           | 본문 영역 — 메인 해설 (28pt)                         |
| 7    | UI ▸ Text - TextMeshPro   | `MoreInfoText`              | 그 아래 — 더 보기 (24pt, 회색)                       |
| 8    | UI ▸ Button - TextMeshPro | `CloseButton`               | 하단 우측 — "닫기"                                   |

> 일러스트는 M1 에서 비어 있어도 됩니다. `NoIllustrationPlaceholder` 는 단순 회색 사각형 + "그림은 추후 추가됩니다" 텍스트 정도로 두면 됨.

### 2-3. 컴포넌트 부착

1. **`DiscoveryFoundPanel`** 선택
2. Add Component → **`Discovery Found Panel`** 검색·추가
3. 필드 채우기:

| 필드                        | 값                                                    |
| --------------------------- | ----------------------------------------------------- |
| Panel Root                  | 비워둠 (자동)                                         |
| Header Text                 | 자식 HeaderText 드래그                                |
| Name Text                   | 자식 NameText 드래그                                  |
| Category Text               | 자식 CategoryText 드래그                              |
| Description Text            | 자식 DescriptionText 드래그                           |
| More Info Text              | 자식 MoreInfoText 드래그                              |
| Illustration Image          | 자식 IllustrationImage 드래그 (없으면 비워둠)         |
| No Illustration Placeholder | 자식 NoIllustrationPlaceholder 드래그 (없으면 비워둠) |
| Close Button                | 자식 CloseButton 드래그                               |

---

## 단계 3 — AnchorButton 에 DiscoveryFoundPanel 연결

1. **`AnchorButton`** 선택
2. AnchorButton 컴포넌트의 `Discovery Found Panel` 칸에 Hierarchy 의 **DiscoveryFoundPanel** 드래그

---

## 단계 4 — 첫 테스트

### 시나리오 1 — 의뢰 없이 정박 시도

1. ▶ Play
2. 의뢰를 받지 않은 상태에서 화면의 **"정박 및 탐색"** 버튼 클릭
3. AnchorStatusText 에 **"지금은 정박해도 찾을 게 없어요. 먼저 의뢰를 받아 보세요."** 표시됨
4. 3초 후 자동 사라짐

### 시나리오 2 — 의뢰 받고 발견물 위치까지 항해

1. 리스본 도착 → 입장 → 모험가 조합 → 지브롤터 의뢰 수락
2. PortScreen 떠나기 → 다시 바다
3. **지브롤터 해협 좌표 (36.0°N, 5.6°W)** 로 이동:
   - GeoCoordinate 스케일 (15 unit/도) 로 → 월드 좌표 약 **(-84, 0, 540)**
   - PlayerShip 시작 위치 (-100, 0.5, 560) 에서 동남쪽으로 살짝 이동
4. 좌표 근처에서 **"정박 및 탐색"** 클릭
5. 거리 ±3% 안이면 **DiscoveryFoundPanel** 표시:
   - 헤더 "새로운 곳을 발견했어요!"
   - 이름 "지브롤터 해협"
   - 카테고리 "랜드마크"
   - 메인 해설 + 더 보기
6. Console 에 `[MissionService] 발견물 등록: disc.gibraltar_strait` 로그

### 시나리오 3 — 거리가 멀 때

좌표에서 멀리 떨어진 곳에서 정박:

- 거리 ratio < 2× → "거의 다 왔어요!"
- ratio < 5× → "아직 멀어요."
- 그 이상 → "여기엔 아무것도 없어요."

---

## 자주 발생하는 문제

| 증상                                          | 해결                                                                                                                                            |
| --------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| AnchorButton 컴포넌트가 검색에 안 나옴        | 컴파일 에러 확인 (Console). AnchorButton.cs 가 namespace Game.UI 인지 확인                                                                      |
| 클릭해도 반응 없음                            | Player Ship / Discovery Found Panel 필드가 비어있는지. AnchorButton 컴포넌트의 Button 칸이 자동으로 찼는지 (자기 GameObject 의 Button 컴포넌트) |
| 발견 시 패널이 안 뜸                          | AnchorButton 의 Discovery Found Panel 필드가 DiscoveryFoundPanel 로 채워졌는지                                                                  |
| "지금은 정박해도 찾을 게 없어요" 가 계속 나옴 | 의뢰가 활성 상태가 아님. MissionGiverPanel 에서 의뢰를 받았는지, Console 의 `[MissionService] 의뢰 수락` 로그로 확인                            |
| 발견물 좌표에 도착해도 발견 안 됨             | searchToleranceBase (DiscoveryData 의 0.03) 가 너무 작거나, 좌표 계산이 잘못. Scene 창에서 Discovery 의 월드 좌표 (15 × lng, 15 × lat) 확인     |
| 의뢰 받기 전에 정박 버튼 클릭하면 배가 멈춤   | 의도된 동작. 어린이가 우연히 누른 경우에도 자연스러움                                                                                           |

---

## 동작 흐름 요약

```
의뢰 수락 (지브롤터 해협 찾기)
  ↓
바다로 나가 좌표 근처로 이동
  ↓
"정박 및 탐색" 버튼 클릭
  ↓
ShipController.HardStop() — 배 정지
  ↓
거리 체크 (눈썰미 보너스 포함)
  ├─ 안에 있음 → MissionService.RegisterDiscovery
  │             → DiscoveryFoundPanel.Show(target)
  │             → 도감에 추가됨 (Console 로그)
  └─ 거리 밖 → 거리별 안내 메시지 (3초 후 사라짐)
```

다음 단계 (B6): 의뢰 항구로 돌아가 완료 보고 + 보상 표시.
