# 토스트 알림 셋업 가이드

화면 상단에 짧게 떠 사라지는 메시지 — 진행 피드백.

---

## 자동 hookup

`ToastService.Start` 에서 자동 연결:
- `MissionService.onRegionUnlocked` → "새 지역 해제: {지역명}"
- `MissionService.onMissionCompleted` → "의뢰 완료: {제목}"

다른 곳에서 수동 호출:
```csharp
ToastService.Instance.Show("저장됨");
ToastService.Instance.Show("화물이 가득 찼어요");
```

---

## 단계 1 — ToastService GameObject 생성

1. Hierarchy 의 **Canvas** 아래에 빈 GameObject 추가 → 이름 **`ToastService`**
2. Add Component → **`Toast Service`**
3. Inspector 의 ⋮ → **`Auto Layout`** 클릭
   - 자동으로 자식 ToastRoot (RectTransform) + Background (Image) + MessageText (TMP_Text) 생성
   - 화면 상단 중앙에 박스 셋업

---

## 단계 2 — 테스트

### 방법 A — 컨텍스트 메뉴
1. ToastService 컴포넌트 ⋮ → **`Test Toast`** 클릭
2. Scene/Game View 에서 "테스트 토스트 — 잘 보이나요?" 가 페이드 인 → 유지 → 페이드 아웃 되어야 정상

### 방법 B — Play 테스트
1. ▶ Play → 다른 지역 항구 방문 → "새 지역 해제: ..." 토스트 떠야 함
2. 의뢰 완료 → "의뢰 완료: ..." 토스트

---

## 단계 3 — 다른 시스템에서 추가 호출 (선택)

원하시면 다음 위치에 한 줄씩 추가:

### SaveService — 저장 알림
[SaveService.cs](../Save/SaveService.cs) 의 `SaveGame` 메서드 끝에:
```csharp
ToastService.Instance?.Show("저장됨");
```

### CombatService — 전투 결과
[CombatService.cs](../Combat/CombatService.cs) — 이미 CombatResultPanel 이 알려주므로 토스트는 생략 가능.

### MarketService — 거래 알림
[MarketService.cs](../Market/MarketService.cs) — 빈번해서 토스트 띄우면 시끄러움. 추천 X.

---

## 시각 조정

ToastService Inspector:

| 필드 | 의미 |
|---|---|
| Visible Seconds | 보이는 시간 (기본 2.5초) |
| Fade In / Out Seconds | 페이드 시간 |
| ToastRoot RectTransform | 위치·크기 (Auto Layout 이 상단 중앙 800×100 으로 셋팅) |
| Background color | 배경 색 (기본 짙은 남색 + alpha 0.85) |

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 토스트 안 보임 | ToastService GameObject 가 Canvas 아래에 있나 / Auto Layout 실행했나 |
| 위치 이상 | ToastRoot 의 RectTransform 직접 조정 |
| 동시에 여러 알림 | 큐로 자동 처리됨 — 하나씩 차례로 표시 |
| 일시정지 중 안 떠야 함 | Time.unscaledDeltaTime 사용해서 일시정지 중에도 표시됨 (오히려 정상) |

---

## 추후 폴리시

- 토스트 아이콘 (Image) — 카테고리별 (지역=깃발, 의뢰=두루마리)
- 사운드 — 토스트 등장 시 효과음
- 다양한 위치 (상단/하단/중앙)
- 색상 변형 (성공·실패·정보)
