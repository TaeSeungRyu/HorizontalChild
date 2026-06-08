# 튜토리얼 오버레이 셋업 가이드

첫 진입 안내 — 어린이가 5~10분 안에 첫 항해 시작할 수 있게.

기획서: GAME_PREP.md §12.2 M4 — "튜토리얼 오버레이, 첫 진입 안내".

---

## 동작

- 국가 선택 직후 자동 표시 (PlayerPrefs 로 한 번만)
- 4 단계 — 환영 → 방향 → 속도 → 항구·발견물
- "다음" / "넘기기" 버튼
- 마지막 단계에서 "시작!" 버튼

PlayerPrefs Key: `tutorial_shown_v1` — 변경 시 모든 사용자에게 재표시.

---

## 단계 1 — TutorialOverlay 만들기

1. Canvas 우클릭 → Create Empty → 이름 **`TutorialOverlay`**
2. Add Component → **`Tutorial Overlay`**
3. 자식 GameObject 만들기:
   - **TitleText** (TMP_Text)
   - **BodyText** (TMP_Text)
   - **ProgressText** (TMP_Text — "1 / 4")
   - **NextButton** (Button — 라벨 자동 갱신: "다음" / "시작!")
   - **SkipButton** (Button — "넘기기")
4. TutorialOverlay 컴포넌트의 각 필드에 자식 드래그
5. ⋮ → **`Auto Layout`** → 풀스크린 어두운 오버레이 + 중앙 메시지 + 하단 버튼

---

## 단계 2 — Play 테스트

### 첫 실행 (튜토리얼 표시)
1. PlayerPrefs 가 비어 있는 상태에서 → ▶ Play
2. 국가 선택 → 튜토리얼 4단계 진행
3. "시작!" 버튼 → 게임 진행
4. 이후 같은 세션에선 다시 안 뜸

### 재실행 (튜토리얼 skip)
1. 다시 ▶ Play (저장 있어도 됨)
2. 튜토리얼 안 뜨고 바로 게임 진행

### 다시 보기 (디버그)
- TutorialOverlay 컴포넌트 ⋮ → **`Reset Tutorial Flag`** → 다음 Play 에 다시 표시
- 또는 ⋮ → **`Show Tutorial Now`** → 즉시 표시

---

## 메시지 커스터마이즈

[TutorialOverlay.cs](TutorialOverlay.cs) 의 `_steps` 배열 수정:
```csharp
private readonly (string title, string body)[] _steps =
{
    ("환영합니다!", "함께 큰 바다로 ..."),
    ("방향 — 조이스틱", "왼쪽 아래 조이스틱 ..."),
    ("속도 — 위 / 아래 버튼", "오른쪽 ..."),
    ("항구·발견물", "항구 가까이 ..."),
};
```

스텝 추가/제거 자유롭게.

---

## 다른 HUD 와의 관계

튜토리얼 떠 있는 동안 다른 HUD 가 보이긴 하지만 (조작 못 함 — 패널이 raycast 차단).

엄격하게 가리고 싶으면 다른 HUD 의 `hideWhileAnyActive` 에 TutorialOverlay 추가.

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 튜토리얼 안 뜸 | `Show Only Once` ☑ + 이전에 봤음. ⋮ → `Reset Tutorial Flag` 후 다시 Play |
| 처음에 뜨고 매번 또 뜸 | `Show Only Once` ☑ 확인 |
| 다음 버튼이 "시작!" 으로 안 바뀜 | Next Button 자식 안에 TMP_Text 있는지 (Button - TextMeshPro 로 만들어야) |
| 진행 막대(1/4) 안 보임 | Progress Text 필드 연결 |

---

## 추후 폴리시

- 배경 흐림 (blur) 효과
- 손가락 가이드 일러스트 (Image)
- 단계별 화살표로 실제 UI 가리킴 (예: 첫 단계에서 조이스틱 가리키는 화살표)
- 음성 / 효과음 동기화 (BGM/SFX 도입 후)
- 다국어 (Unity Localization 도입 후)
- 첫 진입 외 — "다시 보기" 옵션을 Pause Menu 에 추가
