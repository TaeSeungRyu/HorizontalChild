# 가상 조이스틱 UI 셋업 가이드 (C)

모바일 실기에서 배 조작 — 좌측 하단 조이스틱(조타) + 우측 하단 버튼 두 개(가속/감속).

기존 키보드 입력(WASD/방향키)은 그대로 유지 — 에디터 테스트에서도 동작.

---

## 사전 — Gamepad 바인딩 확인

`GameControls.inputactions` 에 다음 바인딩이 이미 추가되어 있어야 합니다 (코드에서 자동 처리됨):

- **Steer**: `<Gamepad>/leftStick` (OnScreenStick 가 emulate)
- **Throttle**: `<Gamepad>/dpad/up` / `<Gamepad>/dpad/down` (OnScreenButton 두 개가 emulate)

추가로 키보드 바인딩도 그대로 유지 (WASD/Arrows).

---

## 단계 1 — 좌측 조이스틱 (Steer)

### 1-1. Joystick 배경

1. Hierarchy 의 **Canvas** 아래에 **UI ▸ Image** 추가, 이름 **`JoystickBackground`**
2. RectTransform:
   - **Anchor**: bottom-left (좌하단)
   - **Pos X**: 200, **Pos Y**: 200 (화면 좌하단에서 200px 떨어진 위치)
   - **Width**: 300, **Height**: 300 (큰 원형)
3. Image 설정:
   - **Source Image**: Knob (UI/Knob — Unity 기본 원형 스프라이트)
   - **Color**: 흰색, 알파 100 정도 (반투명)

### 1-2. Joystick 핸들 (자식)

1. **`JoystickBackground`** 아래에 **UI ▸ Image** 추가, 이름 **`JoystickHandle`**
2. RectTransform:
   - **Anchor**: middle-center
   - **Pos X**: 0, **Pos Y**: 0
   - **Width**: 120, **Height**: 120 (배경보다 작게)
3. Image 설정:
   - **Source Image**: Knob 또는 UISprite
   - **Color**: 흰색 알파 200 (더 진하게)

### 1-3. On-Screen Stick 컴포넌트 부착

1. **`JoystickHandle`** 선택 (배경이 아닌 **핸들**)
2. Add Component → **`On-Screen Stick`** 검색·추가
3. 필드 채우기:

| 필드 | 값 |
|---|---|
| **Movement Range** | **80** (배경 안에서 핸들이 움직이는 반경) |
| **Use Isolated Input Actions** | ☐ 비활성 (기본값) |
| **Control Path** | **`<Gamepad>/leftStick`** 입력 |

⚠ Control Path 입력 방법:
- 옆의 ▼ 드롭다운 클릭
- **Gamepad ▸ Left Stick** 선택
- 또는 직접 `<Gamepad>/leftStick` 타이핑

---

## 단계 2 — 우측 가속/감속 버튼 (Throttle)

### 2-1. 가속(▲) 버튼

1. Canvas 아래에 **UI ▸ Button - TextMeshPro** 추가, 이름 **`ThrottleUpButton`**
2. RectTransform:
   - **Anchor**: bottom-right (우하단)
   - **Pos X**: -200, **Pos Y**: 320 (우하단에서 들여서 위쪽)
   - **Width**: 200, **Height**: 200 (큰 사각형)
3. 버튼 자식 Text → **"▲"** (위 화살표) 또는 **"가속"**
4. Image (Button 자체) 색은 옅은 녹색

### 2-2. 가속 버튼에 On-Screen Button 추가

1. **`ThrottleUpButton`** 선택
2. Add Component → **`On-Screen Button`**
3. 필드:
   - **Control Path**: **`<Gamepad>/dpad/up`** 입력 (또는 드롭다운 Gamepad ▸ Dpad ▸ Up)

### 2-3. 감속(▼) 버튼

1. Canvas 아래에 **UI ▸ Button - TextMeshPro** 추가, 이름 **`ThrottleDownButton`**
2. RectTransform:
   - **Anchor**: bottom-right
   - **Pos X**: -200, **Pos Y**: 100 (가속 버튼 아래)
   - **Width**: 200, **Height**: 200
3. 자식 Text → **"▼"** 또는 **"멈춤"**
4. 색은 옅은 빨강

### 2-4. 감속 버튼에 On-Screen Button 추가

1. **`ThrottleDownButton`** 선택
2. Add Component → **`On-Screen Button`**
3. **Control Path**: **`<Gamepad>/dpad/down`**

---

## 단계 3 — Hide While Any Active 등록

UI 패널이 떠있을 동안 조이스틱·버튼도 숨겨야 자연스럽습니다.

가장 간단 — 세 UI 를 한 빈 GameObject 의 자식으로 묶기:

1. Hierarchy 의 Canvas 아래에 **빈 GameObject** 추가, 이름 **`TouchControls`**
2. RectTransform 으로 화면 전체 (stretch) 설정
3. `JoystickBackground`, `ThrottleUpButton`, `ThrottleDownButton` 을 모두 **`TouchControls`** 의 자식으로 옮김 (드래그)

이제 `TouchControls` GameObject 의 활성 상태만 토글하면 됨.

### Hide While 처리 (AnchorButton 패턴 응용)

AnchorButton/EnterPortButton 의 `Hide While Any Active` 배열에 **TouchControls 도 영향받게** 하려면 별도 컴포넌트가 필요한데, 가장 단순한 방법:

1. `TouchControls` 에 **Canvas Group** 추가
2. AnchorButton 컴포넌트와 유사한 컨트롤러 컴포넌트를 작성

**M1 단순화 — 그냥 그대로 두기**:
- 패널 위에 조이스틱이 살짝 보여도 동작 X (패널이 raycast 가로채면 조이스틱 클릭 안 됨)
- 시각적으로 조금 어수선하지만 기능엔 문제 없음
- 추후 폴리시에서 자동 숨김 컴포넌트 추가

---

## 단계 4 — Game 창 비율 설정

에디터에서 모바일 가로 모드처럼 보려면 Game 창의 비율 변경:

1. Game 창 상단의 비율 드롭다운 (예: "Free Aspect")
2. **Add Aspect Ratio** 또는 **2340 x 1080 Landscape** 선택
3. 또는 **Add** → 직접 추가: Width 2340, Height 1080, Landscape

---

## 단계 5 — 첫 테스트

1. ▶ Play
2. Game 창에서:
   - **마우스로 좌측 조이스틱을 잡고 좌/우로 드래그** → 배가 회전
   - **마우스로 우측 ▲ 버튼 누르기** → 배가 가속
   - **▼ 버튼 누르기** → 배가 감속/멈춤
3. 키보드 입력도 그대로 동작 (WASD/Arrows)

### Console 에러가 있다면

- **On-Screen Stick 컴포넌트가 안 보임** — Input System 패키지 1.4 이상 필요. 우리 1.16 이라 OK.
- **Control Path 가 잘못된 값** — 위 안내대로 `<Gamepad>/leftStick` 그대로 입력

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 조이스틱 끌어도 배 안 움직임 | On-Screen Stick 의 Control Path 가 `<Gamepad>/leftStick` 인지 확인. inputactions 에 Gamepad 바인딩이 있는지 확인 |
| 버튼 눌러도 가속 안 됨 | On-Screen Button 의 Control Path 가 `<Gamepad>/dpad/up` (가속) 또는 `<Gamepad>/dpad/down` (감속) 인지 확인 |
| 조이스틱 핸들이 안 움직임 | On-Screen Stick 의 Movement Range 가 너무 작음 (50 이상 권장). 또는 핸들이 배경의 자식이 아님 |
| 키보드 입력이 안 됨 | (이전에 동작했으면) 기존 키보드 바인딩이 inputactions 에 그대로 있는지. 사라졌으면 가이드의 키보드 바인딩 부분 다시 추가 |

---

## 동작 흐름

```
사용자가 조이스틱 잡고 우측으로 끔
  → OnScreenStick 가 <Gamepad>/leftStick = (1, 0) emulate
  → inputactions 의 Steer 액션이 그 binding 듣고 트리거
  → ShipController.Update() 가 ReadValue<Vector2>().x = 1 읽음
  → 배가 우측 회전
```

```
사용자가 ▲ 버튼 누름
  → OnScreenButton 가 <Gamepad>/dpad/up = pressed emulate
  → Throttle 액션의 1DAxis composite (GamepadDpad) positive = 1
  → ShipController.Update() 가 ReadValue<float>() = 1 읽음
  → 배가 가속
```

---

## 모바일 실기 (D 단계 진입 후)

이 가상 조이스틱은 **마우스/터치 모두 동작** 합니다 (Unity 의 OnScreen 컴포넌트가 자동 처리). Android 빌드 후 S23 에 설치하면 손가락 터치로 동작.

다음 단계는 **D — Android 빌드 + S23 실기 테스트**.
