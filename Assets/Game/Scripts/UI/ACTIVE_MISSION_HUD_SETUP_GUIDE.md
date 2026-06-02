# 활성 의뢰 HUD 셋업 가이드 (B3)

화면 좌상단에 작은 HUD 추가 — 항해 중에도 현재 의뢰가 무엇인지 잊지 않게.

---

## 단계 1 — HUD 패널 만들기

1. Hierarchy 의 **Canvas** 아래에 **UI ▸ Panel** 추가, 이름 **`ActiveMissionHUD`**
2. RectTransform 으로 **화면 좌상단** 에 작은 박스 (예: width 600, height 200)
   - Anchor: top-left
   - Pos X: 30, Pos Y: -30 (상단에서 약간 띄움)
3. 배경 색은 어두운 반투명 (검정 알파 150 정도) — 글씨가 잘 보이게

---

## 단계 2 — 자식 UI

`ActiveMissionHUD` 하위:

| 자식 | 종류 | 이름 | 위치 / 비고 |
|---|---|---|---|
| 1 | UI ▸ Text - TextMeshPro | `TitleText` | 상단 — "현재 의뢰" (20pt, 옅은 색) |
| 2 | UI ▸ Text - TextMeshPro | `MissionTitleText` | 중단 — 의뢰 제목 (28pt, 굵게) |
| 3 | UI ▸ Text - TextMeshPro | `ProgressText` | 하단 — 진행 안내 (20pt) |

각 TMP_Text:
- Anchor stretch (Alt+우하단)
- Left/Right 패딩 적당히 (10~20)
- 흰색 또는 옅은 노란색

---

## 단계 3 — 컴포넌트 부착

1. **`ActiveMissionHUD`** 선택
2. Add Component → **`Active Mission HUD`** 검색·추가
3. 필드 채우기:

| 필드 | 값 |
|---|---|
| Title Text | 자식 TitleText 드래그 |
| Mission Title Text | 자식 MissionTitleText 드래그 |
| Progress Text | 자식 ProgressText 드래그 |
| Mission Service | 비워둠 (런타임 자동) |
| Hide While Any Active | 배열 크기 **5** — PortArrivalDialog / PortScreen / MissionGiverPanel / DiscoveryFoundPanel / MissionCompletedPanel 모두 등록 |

---

## 단계 4 — 첫 테스트

### 시나리오 1 — 의뢰 없음

1. ▶ Play
2. 시작 시 HUD 가 보이며:
   ```
   현재 의뢰
   (없음)
   항구의 모험가 조합에서 의뢰를 받아 보세요.
   ```

### 시나리오 2 — 의뢰 받기

1. 리스본 도착 → 모험가 조합 → 지브롤터 의뢰 수락
2. 바다로 나오면 HUD 가:
   ```
   현재 의뢰
   두 바다가 만나는 좁은 길을 찾아봐요
   지브롤터 해협 을(를) 찾아보세요.
   ```

### 시나리오 3 — 발견 후

1. 지브롤터 좌표 근처에서 "정박 및 탐색" → 발견
2. HUD 가:
   ```
   현재 의뢰
   두 바다가 만나는 좁은 길을 찾아봐요
   지브롤터 해협 을(를) 찾았어요!
   리스본 으로 돌아가 보고하세요.
   ```

### 시나리오 4 — 완료

1. 리스본 복귀 → MissionCompletedPanel
2. 닫고 나오면 HUD 가 다시 "(없음)" 상태로

### 시나리오 5 — 패널 떠 있을 때

- PortScreen / 모험가 조합 / 발견 패널 / 완료 패널 떠 있는 동안 HUD 자동 숨김 (CanvasGroup alpha 0)
- 패널 닫히면 다시 표시

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| HUD 자체가 안 보임 | RectTransform 위치가 화면 밖이거나 크기 0. Anchor / Pos / Width / Height 확인 |
| 의뢰 받아도 텍스트 안 바뀜 | MissionService 가 씬에 부착되었는지 확인 (GameManager) |
| 패널 떠있어도 안 숨겨짐 | `Hide While Any Active` 배열에 패널들이 등록 안 됨 |
| (없음) 만 계속 표시 | MissionService.Instance 가 null 일 수도 — Console 확인 |

---

## 동작 흐름 요약

```
의뢰 없음 → HUD "(없음) — 의뢰를 받아 보세요"
   ↓ 의뢰 수락 (onMissionAccepted)
HUD "지브롤터 해협 을(를) 찾아보세요"
   ↓ 발견 (onDiscoveryRegistered)
HUD "지브롤터 해협 을(를) 찾았어요! 리스본 으로 돌아가 보고하세요"
   ↓ 의뢰 항구 복귀 → 완료 (onMissionCompleted)
HUD "(없음) — 의뢰를 받아 보세요"
```

이로써 **B 시리즈 작업 모두 완성** — 의뢰 받기부터 보상까지의 핵심 루프 + HUD.

---

## B 시리즈 완료 후 다음 단계

| # | 작업 | 우선도 |
|---|---|---|
| C | UI 가상 조이스틱 (모바일 입력) | ⭐⭐⭐ — 모바일 전용 앱이라 필수 |
| D | Android 빌드 + S23 실기 | ⭐⭐⭐ — 실기 동작 확인 |
| 자금/명성 HUD | PlayerState 값을 화면에 표시 | ⭐⭐ |
| 시각 폴리시 | 색·아이콘·애니메이션 | ⭐ (M3 이후) |
