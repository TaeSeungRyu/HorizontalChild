# World.unity 씬 셋업 가이드 (M1)

리스본↔세우타 마일스톤의 실행 가능한 씬을 만드는 단계.

> 가이드는 큰 단계 8개로 나뉘어 있습니다. 각 단계 끝나면 채팅에서 알려주시면 다음 단계로 안내드립니다.

---

## 단계 1 — 새 씬 만들기

1. 메뉴: **File ▸ New Scene**
2. 템플릿 선택 창에서 **"Basic (URP)"** 또는 **"Standard (URP)"** 선택
3. **Create** 클릭
4. 메뉴: **File ▸ Save As...**
5. 위치: `Assets/Game/Scenes/`
6. 이름: **`World`** → Save
7. Project 창에서 `Assets/Game/Scenes/World.unity` 가 생긴 것을 확인

> Hierarchy 에 기본으로 `Main Camera` 와 `Directional Light` 가 있어야 정상.

---

## 단계 2 — 바다(평면) 배치

1. Hierarchy 창 비어 있는 곳 **우클릭 ▸ 3D Object ▸ Plane**
2. 이름을 **`Sea`** 로 변경
3. Inspector 의 `Transform`:
   - Position: **0, 0, 0**
   - Rotation: **0, 0, 0**
   - Scale: **100, 1, 100** (큰 바다)
4. (선택) Material 을 파란색으로:
   - Project 창 우클릭 ▸ Create ▸ Material → 이름 `Mat_Sea`
   - 색깔(Base Color) 을 파란색으로 설정
   - Sea 의 Inspector ▸ Mesh Renderer ▸ Materials 의 Element 0 에 드래그

---

## 단계 3 — 플레이어 배 배치 (Kenney ship-small)

1. Project 창에서 다음 경로로 이동:
   ```
   Assets/kenney_pirate-kit/Models/FBX format/
   ```
2. **`ship-small.fbx`** 를 Hierarchy 로 드래그
3. 이름을 **`PlayerShip`** 으로 변경
4. Transform:
   - Position: **0, 0.5, 0** (바다 위에 살짝 올라옴)
   - Rotation: **0, 0, 0**
   - Scale: **1, 1, 1** (크면 줄임)

> 배가 너무 작거나 크면 Scale 을 0.5~3 사이에서 조절.
>
> 모델이 안 보이면 Camera 의 Clipping Planes 의 Far 값이 너무 작거나, 배가 카메라 뒤에 있을 수 있음. Scene 창에서 직접 확인.

---

## 단계 4 — ShipController 컴포넌트 연결

1. Hierarchy 에서 **`PlayerShip`** 선택
2. Inspector 의 **`Add Component`** 버튼 클릭
3. 검색창에 **`Ship Controller`** 입력 → 선택 (또는 그냥 `Ship` 입력하면 보임)
4. ShipController 컴포넌트가 추가됨. 필드 채우기:

| 필드 | 값 |
|---|---|
| **Ship Data** | `Assets/Game/Data/Ships/Ship_Caravel.asset` 드래그 |
| **Captain** | `Assets/Game/Data/Characters/Character_Henrique.asset` 드래그 |
| **Steer Action** | 원형 ● 클릭 → `GameControls ▸ Ship ▸ Steer` 선택 |
| **Throttle Action** | 원형 ● 클릭 → `GameControls ▸ Ship ▸ Throttle` 선택 |
| Max Turn Rate | 60 (기본값 그대로) |
| Acceleration | 2 (기본값) |
| Passive Deceleration | 0.5 |
| Active Deceleration | 3 |

> InputActionReference 의 원형 ● 클릭 시 검색 창이 뜸. 거기에 `Steer` 입력 → 자동 필터됨.

---

## 단계 5 — SeaWorldManager 추가

1. Hierarchy 빈 곳 **우클릭 ▸ Create Empty** → 이름 **`GameManager`**
2. GameManager 선택 → Add Component → **`Sea World Manager`**
3. 필드 채우기:

| 필드 | 값 |
|---|---|
| **Player Ship** | Hierarchy 의 `PlayerShip` 드래그 |
| **Active Ports** | 배열 크기 **2**:<br>– Element 0: `Port_Lisbon.asset`<br>– Element 1: `Port_Ceuta.asset` |
| **Active Discoveries** | 배열 크기 **1**:<br>– Element 0: `Discovery_GibraltarStrait.asset` |
| Port Icon Prefab | (M1 임시) 비워둠 — 단계 7에서 만듦 |
| Port Icons Parent | 비워둠 |

---

## 단계 6 — 카메라 셋업

1. Hierarchy 에서 **`Main Camera`** 선택
2. Inspector 의 Transform 을 다음과 같이:
   - Position: **0, 30, -15**
   - Rotation: **65, 0, 0**
3. Add Component → **`Camera Follow`**
4. 필드 채우기:

| 필드 | 값 |
|---|---|
| **Target** | `PlayerShip` 드래그 |
| Offset | 0, 30, -15 (기본값) |
| Tilt Angle | 65 (기본값) |
| Position Lerp Speed | 5 |

> Game 창에서 미리보기:
>   배가 화면 가운데에 보이고, 카메라가 약간 기울어진 top-down 시점이어야 함.

---

## 단계 7 — 항구 아이콘 Prefab (단순)

1. Project 창에서 `Assets/Game/Prefabs/` 폴더가 없으면 생성:
   - `Assets/Game` 선택 → 우클릭 → Create ▸ Folder → 이름 `Prefabs`
2. Hierarchy 빈 곳 우클릭 → **3D Object ▸ Cylinder** (또는 Sphere)
3. 이름 **`PortIcon_Template`**, Scale **2, 2, 2**
4. Add Component → **`Port Marker`**
5. Hierarchy 의 `PortIcon_Template` 을 Project 창의 `Assets/Game/Prefabs/` 폴더로 드래그 → Prefab 생성됨
6. Hierarchy 에 있는 원본 `PortIcon_Template` 은 **Delete** (씬에 남기지 않음)
7. `GameManager` 선택 → SeaWorldManager 의 **Port Icon Prefab** 필드에 방금 만든 Prefab 드래그

> Kenney 의 항구 모델(예: `castle-gate.fbx` 등)을 prefab 으로 만들어 더 예쁘게 할 수도 있지만, M1 에서는 단순 원기둥으로 충분.

---

## 단계 8 — Canvas + 터치 UI (가상 조이스틱·버튼)

자세한 절차는 [`Assets/Game/Scripts/Input/INPUT_ACTIONS_GUIDE.md`](../Scripts/Input/INPUT_ACTIONS_GUIDE.md) 의 §2 참고.

핵심 요약:
1. Hierarchy 우클릭 → **UI ▸ Canvas**
2. Canvas 의 Canvas Scaler:
   - UI Scale Mode = **Scale With Screen Size**
   - Reference Resolution = **2340 × 1080**
   - Match = **0.5**
3. Canvas 하위에 **UI ▸ Image** 추가 → 왼쪽 하단 배치 → Add Component **`On-Screen Stick`** → Control Path 를 `<Gamepad>/leftStick` 입력
4. Canvas 하위에 **UI ▸ Button** 추가 → 오른쪽 하단 배치 → Add Component **`On-Screen Button`** → Control Path 를 `<Gamepad>/buttonSouth` 입력
5. (PlayerInput 컴포넌트로 GameControls 액션맵을 활성화 — 또는 ShipController 의 InputActionReference 가 이미 액션을 활성화하므로 그대로 동작)

---

## 단계 9 — 첫 테스트

1. Hierarchy 의 EventSystem 이 있는지 확인 (Canvas 추가 시 자동 생성됨)
2. 메뉴 바의 **▶ Play 버튼** 클릭
3. 확인:
   - 항구 아이콘 2개가 리스본·세우타 위치에 보임
   - PlayerShip 이 카메라에 따라잡힘
   - 가상 조이스틱·버튼을 마우스로 드래그/클릭 → 배가 회전·전진
4. 정지하려면 Play 버튼 다시 클릭

---

## 단계 10 — Mobile 빌드 설정 (실기 테스트)

1. 메뉴: **File ▸ Build Profiles** (또는 Build Settings)
2. **Android** 플랫폼 선택 → `Switch Platform` 클릭 (몇 분 걸림)
3. **Player Settings**:
   - Resolution and Presentation:
     - Default Orientation = **Landscape Left** (가로 모드 고정)
     - 다른 방향(Portrait 등) 모두 체크 해제
   - Quality / Graphics:
     - URP 의 Mobile RPAsset 적용 (`Assets/Settings/Mobile_RPAsset`)
4. Galaxy S23 을 USB 로 연결, USB 디버깅 활성화
5. **Build And Run** → APK 가 폰에 설치되어 실행

---

## 문제 해결 (자주 발생)

| 증상 | 해결 |
|---|---|
| 컴파일 에러 `using Game.Ports;` 같은 누락 | 코드 파일들의 namespace 일관성 확인 |
| 배가 안 움직임 | ShipController 의 Steer/Throttle Action 이 할당되었는지 확인 |
| 카메라가 배를 안 따라감 | CameraFollow 의 Target 에 PlayerShip 이 할당됐는지 확인 |
| 항구 아이콘이 안 보임 | PortIcon Prefab 이 SeaWorldManager 에 할당됐는지, PortData 의 좌표가 맞는지 확인 |
| UI 조이스틱·버튼이 안 먹힘 | EventSystem 이 씬에 있는지, On-Screen 컴포넌트의 Control Path 가 입력됐는지 확인 |
