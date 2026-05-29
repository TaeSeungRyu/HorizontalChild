# Input Actions 셋업 가이드

게임 입력 시스템 연결 단계. M1 마일스톤 기준.

## 0. 현재 상태

- ✅ `Assets/Game/Settings/GameControls.inputactions` — 액션 정의 완료
  - 액션 맵: **`Ship`**
  - 액션 6개:
    - `Steer` (Vector2)
    - `Throttle` (Axis, -1 ~ +1)
    - `Confirm` (Button)
    - `OpenJournal` (Button)
    - `OpenMap` (Button)
    - `PauseStop` (Button)
  - 컨트롤 스킴: **`Touch`** (Touchscreen 디바이스)
- ⏳ **바인딩(Bindings) 은 비어 있음** — UI 컴포넌트(On-Screen Stick/Button)가 만들어진 뒤 자동으로 연결되거나, 사용자가 명시적으로 추가.

---

## 1. 액션 확인 (5분)

1. Unity 에디터에서 `Assets/Game/Settings/GameControls.inputactions` **더블클릭**
2. Input Actions 편집 창이 열림. 좌측에 **Ship** 액션 맵이 보이고, 우측에 6개 액션이 보임.
3. 각 액션 클릭 → 우측 패널에서 타입과 Expected Control Type 이 위와 같이 설정되어 있는지 확인.

---

## 2. UI Canvas 와 On-Screen Stick / Button 추가 (씬 작업 시)

`World.unity` 씬을 만들 때, 다음과 같이 터치 UI를 구성:

### 2.1 Canvas 생성
1. Hierarchy 우클릭 → **UI ▸ Canvas** 추가
2. Canvas 의 `Render Mode` = `Screen Space - Overlay`
3. `Canvas Scaler`:
   - `UI Scale Mode` = **Scale With Screen Size**
   - `Reference Resolution` = **2340 × 1080** (Galaxy S23 가로 기준)
   - `Match` 슬라이더 = **0.5**

### 2.2 가상 조이스틱 (Steer)
1. Canvas 하위에 빈 GameObject 추가, 이름 `Joystick_Steer`
2. 왼손 영역(화면 좌하단)에 RectTransform 위치
3. `+ Add Component` → **`On-Screen Stick`** (Unity InputSystem 포함)
4. `On-Screen Stick` 컴포넌트의 **Control Path** 를 `Steer` 액션과 연결:
   - 우측 동그라미(●) 클릭 → "Steer" 액션 선택
   - 또는 직접 `<Gamepad>/leftStick` 같이 입력 후 액션이 그 경로를 듣게 설정 (간단하지 않으므로 다음 방법 권장)
   - **간단한 방법**: Joystick GameObject 의 **PlayerInput** 컴포넌트로 액션 맵을 자동 바인딩 (3.1 참조)
5. 보기 좋은 원형 이미지(Background + Handle) 두 개를 자식으로 두어 시각화

### 2.3 추진 버튼 (Throttle)
1. Canvas 하위에 `Button_ThrottleUp` 추가 (화면 우하단 위쪽 화살표)
2. `+ Add Component` → **`On-Screen Button`**
3. **Control Path** 를 양수 입력(+1)으로 설정
4. 마찬가지로 `Button_ThrottleDown` (음수 입력, -1)
5. `PauseStop` 버튼은 별도 (정지)

> **간편 방법**: On-Screen Stick/Button 컴포넌트는 본인이 액션을 자동으로 등록해 줍니다. PlayerInput 컴포넌트와 함께 쓰면 ShipController 에 바인딩이 자동으로 연결됩니다.

---

## 3. ShipController 와 액션 연결

### 3.1 권장 방법 — PlayerInput 컴포넌트 사용

1. 배 프리팹(또는 씬 안의 배 GameObject)에 **`PlayerInput`** 컴포넌트 추가
2. **Actions** 필드에 `Assets/Game/Settings/GameControls` 드래그
3. **Default Map** = `Ship`
4. **Behavior** = `Invoke Unity Events` (또는 `Send Messages`)
5. PlayerInput 의 Events 섹션에서 각 액션의 콜백을 ShipController 함수에 연결 — 또는 ShipController 의 `steerAction`/`throttleAction` 필드에 InputActionReference 를 직접 할당

### 3.2 권장 방법 (간단) — InputActionReference 직접 할당

1. 배 GameObject 의 ShipController 컴포넌트 인스펙터를 열기
2. **Steer Action** 필드의 원형 아이콘(●) 클릭
3. 검색창에 `Steer` → `GameControls/Ship/Steer` 항목 선택
4. **Throttle Action** 도 같은 방식 — `GameControls/Ship/Throttle` 선택
5. 그러면 ShipController 가 자동으로 액션을 Enable/Disable 하며 값을 읽음

---

## 4. 빌드 전 확인 사항

- [ ] `Edit ▸ Project Settings ▸ Player ▸ Other Settings` — **Active Input Handling = Input System Package (New)** ✅ (이미 확인됨)
- [ ] 배 GameObject 의 ShipController 의 `Steer Action`, `Throttle Action` 필드가 비어있지 않음
- [ ] Canvas 하위에 On-Screen Stick / Button 들이 액션 이름으로 잘 매핑됨
- [ ] EventSystem 이 씬에 한 개 있어야 함 (UI ▸ Canvas 추가 시 자동 생성됨)

---

## 5. 트러블슈팅

| 증상 | 원인 | 해결 |
|---|---|---|
| 배가 움직이지 않음 | InputActionReference 미할당 | ShipController 인스펙터에서 Steer/Throttle 할당 |
| On-Screen Stick 눌러도 반응 없음 | Action enabled 안 됨 | PlayerInput 컴포넌트로 액션맵 활성화 |
| 컴파일 에러 `InputAction` 인식 안 됨 | Game.asmdef 가 Unity.InputSystem 참조 안 함 | Game.asmdef references 에 "Unity.InputSystem" 있는지 확인 ✅ (이미 추가됨) |
| 키보드/마우스로 테스트하고 싶음 | Touch 컨트롤 스킴만 있음 | inputactions 의 bindings 에 키보드 바인딩 (`<Keyboard>/leftArrow` 등) 추가 |

---

## 6. M1 이후 확장

- `Confirm` 액션 — 항구 도착 다이얼로그·발견물 패널 진행
- `OpenJournal` — 도감(발견물 컬렉션) 열기
- `OpenMap` — 큰 지도 / 미니맵 확장
- `PauseStop` — 배 정지(돛 내림) / 일시정지

각 액션을 UI 컴포넌트에 바인딩하면 자동 동작.
