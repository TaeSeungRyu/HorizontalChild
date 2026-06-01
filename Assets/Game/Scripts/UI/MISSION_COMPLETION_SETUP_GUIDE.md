# 의뢰 완료 보상 셋업 가이드 (B6)

`PlayerState` 컴포넌트 + `MissionCompletedPanel` 패널 + PortScreen 연결.

---

## 단계 1 — PlayerState 컴포넌트 부착

`PlayerState` 는 자금·명성 매니저. MissionService 와 함께 GameManager 에 둠.

1. Hierarchy 에서 **`GameManager`** 클릭
2. Inspector 의 **`Add Component`** → 검색창에 **`Player State`** → 클릭
3. 필드:

| 필드 | 값 |
|---|---|
| Starting Money | **5000** (기본값) — 게임 시작 시 자금 |
| Min Money Floor | **100** (기본값) — 패배 시 최저 보장 |
| Events (3개) | 비워둠 — 추후 HUD 연결 시 사용 |

---

## 단계 2 — MissionCompletedPanel 만들기

### 2-1. 패널 GameObject

1. Canvas 아래에 **UI ▸ Panel** 추가, 이름 **`MissionCompletedPanel`**
2. 화면 중앙에 중간 크기 (예: 1600×800)
3. 배경 색은 따뜻한 금색/베이지 톤 + 알파 240

### 2-2. 자식 UI

`MissionCompletedPanel` 하위:

| 자식 | 종류 | 이름 | 위치 / 비고 |
|---|---|---|---|
| 1 | UI ▸ Text - TextMeshPro | `HeaderText` | 상단 — "수고했어요!" (큰 폰트 60pt) |
| 2 | UI ▸ Text - TextMeshPro | `MissionTitleText` | 중상단 — 의뢰 제목 표시 (28pt) |
| 3 | UI ▸ Text - TextMeshPro | `RewardText` | 중간 — 보상 내역 (가운데 정렬, 40pt) |
| 4 | UI ▸ Button - TextMeshPro | `OkButton` | 하단 — "좋아요" 또는 "확인" |

### 2-3. 컴포넌트 부착

1. **`MissionCompletedPanel`** 선택
2. Add Component → **`Mission Completed Panel`**
3. 필드 채우기:

| 필드 | 값 |
|---|---|
| Panel Root | 비워둠 |
| Header Text | 자식 HeaderText 드래그 |
| Mission Title Text | 자식 MissionTitleText 드래그 |
| Reward Text | 자식 RewardText 드래그 |
| Ok Button | 자식 OkButton 드래그 |

---

## 단계 3 — PortScreen 에 연결

1. Hierarchy 에서 **`PortScreen`** 클릭
2. Inspector 의 PortScreen 컴포넌트에 새 필드 **`Mission Completed Panel`** 이 보임
3. Hierarchy 의 **`MissionCompletedPanel`** 을 그 칸에 드래그

---

## 단계 4 — AnchorButton 의 hideWhileAnyActive 에 추가

새 패널도 떠 있을 동안 AnchorButton 이 숨어야 합니다.

1. Hierarchy 의 **`AnchorButton`** 선택
2. Inspector 의 `Hide While Any Active` 배열:
   - **Size** 를 **5** 로 (기존 4 → 5)
   - 새로 생긴 **Element 4** 에 **MissionCompletedPanel** 드래그

---

## 단계 5 — 첫 테스트

### 시나리오 — 의뢰 받기 → 발견 → 의뢰 항구 복귀

1. ▶ Play
2. 리스본 도착 → 입장 → 모험가 조합 → **지브롤터 의뢰 수락**
3. 바다로 나가 지브롤터 좌표 (-84, 540) 근처로 이동
4. **"정박 및 탐색"** → 발견 패널 표시
5. 발견 패널 닫기 → **다시 리스본으로 항해**
6. 리스본 입항 → 도착 다이얼로그 "예" → **PortScreen 떠오름과 동시에 MissionCompletedPanel 자동 표시:**
   ```
   수고했어요!
   "두 바다가 만나는 좁은 길을 찾아봐요" 의뢰를 마쳤어요.

   돈 +1,000원
   좋은 평판 +100
   ```
7. Console 에 다음 로그:
   ```
   [MissionService] 의뢰 완료! mission.disc.lisbon.gibraltar — 보상 돈 1000, 좋은 명성 +100
   ```
8. **"좋아요"** 클릭 → MissionCompletedPanel 닫히고 PortScreen 그대로 표시
9. 다시 모험가 조합 → 이미 완료한 의뢰는 더 이상 안 보임 ("지금은 받을 의뢰가 없어요")

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 리스본 복귀해도 완료 패널이 안 뜸 | PortScreen 의 `Mission Completed Panel` 필드가 비어있음. 드래그 |
| 완료 패널 떴는데 보상 텍스트가 빈 줄 | Mission_DiscLisbonGibraltar SO 의 Reward Money / Reward Good Reputation 값이 0 이거나 음수 |
| Console 에 "[MissionService] PlayerState 인스턴스 없음" 경고 | GameManager 에 Player State 컴포넌트가 안 붙음. 단계 1 다시 |
| 발견 안 한 채로 리스본 가도 완료됨 | MissionService.TryCompleteAtPort 가 발견물 의뢰의 경우 DiscoveredIds 확인하므로 그럴 리 없는데, 발생하면 Discovery SO 의 discoveryId 가 비어있을 가능성 |
| 완료 후에도 모험가 조합에 같은 의뢰 다시 보임 | CompletedMissionIds 에 추가 안 됨. M1 SO 가 자동 생성 시점에 missionId 가 비었을 수 있음 — 시드 SO 확인 |

---

## 동작 흐름 (B 작업 전체 완성)

```
[리스본 도착] → 도착 다이얼로그 → [예]
  → PortScreen 열림 (보상 패널은 안 뜸, 아직 의뢰 안 받음)
  → "모험가 조합 가기"
  → MissionGiverPanel — 지브롤터 의뢰 수락
  → 패널 닫힘

[바다로 나감] → 지브롤터 좌표로 이동
  → "정박 및 탐색" 버튼
  → DiscoveryFoundPanel — 지브롤터 해협 발견 (도감 등록)
  → 패널 닫힘

[리스본 복귀] → 도착 다이얼로그 → [예]
  → PortScreen 열림 + MissionCompletedPanel 자동 표시
  → 돈 +1,000원, 명성 +100 — PlayerState 에 누적
  → "좋아요" → 패널 닫힘
  → PortScreen 그대로 / 모험가 조합 가도 의뢰 없음 (이미 완료)
```

이로써 **M1 의 교육 게임 핵심 루프** (의뢰→항해→발견→귀항→보상) 가 완성됩니다.

---

## 다음 단계 후보

| # | 작업 | 메모 |
|---|---|---|
| B3 | 활성 의뢰 HUD | 화면 상단에 "현재 의뢰: 지브롤터 해협 찾기" + 좌표 힌트 |
| C | UI 가상 조이스틱 | 모바일 실기 입력. 현재는 키보드만 |
| D | Android 빌드 + S23 실기 | Mobile_RPAsset, Player Settings, .apk |
| Etc | UI 폴리시 / 자금·명성 HUD / 진행도 저장 | M3 이후 |
