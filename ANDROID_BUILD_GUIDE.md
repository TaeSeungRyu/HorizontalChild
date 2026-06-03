# Android 빌드 + Galaxy S23 실기 테스트 가이드 (D)

M1 의 마지막 단계 — 지금까지 만든 게 실제 폰에서 동작하는지 확인.

---

## 사전 점검

### Unity 환경
1. **Unity Hub** 의 우리 Unity 버전 (6000.3.0f1) 옆에 **Android Build Support** 가 설치돼 있어야 함
2. 확인 방법:
   - Unity Hub → Installs → Unity 버전 옆 ⚙ → **Add modules**
   - 다음 3개가 ☑ 되어 있어야 함:
     - ☑ **Android Build Support**
       - ☑ **OpenJDK**
       - ☑ **Android SDK & NDK Tools**
3. 누락된 게 있으면 체크 후 Install (수십 분 걸릴 수 있음)

### Galaxy S23 (실기)
1. **개발자 옵션** 활성화:
   - 설정 → 휴대전화 정보 → 소프트웨어 정보 → **빌드 번호** 7번 연타
   - "개발자 옵션이 켜졌습니다" 알림
2. **USB 디버깅** 활성화:
   - 설정 → 개발자 옵션 → **USB 디버깅** ☑
3. **USB 케이블로 PC 연결**:
   - 연결 직후 폰에 "USB 디버깅을 허용하시겠습니까?" 팝업 → 허용

---

## 단계 1 — 플랫폼을 Android 로 전환

1. Unity 메뉴: **`File ▸ Build Profiles`** (또는 `Build Settings`)
2. 좌측 플랫폼 목록에서 **`Android`** 클릭
3. 우하단 **`Switch Platform`** 클릭
4. 모든 에셋이 재임포트됨 — **수 분 걸림**. 진행 바가 끝날 때까지 대기

✅ 전환 완료되면 좌측 Android 옆에 Unity 로고가 표시됨.

---

## 단계 2 — 빌드할 씬 등록

1. **Build Profiles** 창 상단 **`Scene List`**
2. 만약 `Scenes/World` 가 없으면:
   - **`Add Open Scenes`** 클릭 (현재 열린 World 씬 추가)
   - 또는 Project 창의 `Assets/Game/Scenes/World` 를 Scene List 로 드래그
3. 체크박스 ☑ 활성화 + Build Index 0 (첫 번째 씬)

---

## 단계 3 — Mobile_RPAsset 적용

URP 의 Mobile 프로필을 사용. 모바일 GPU 친화 설정.

1. Unity 메뉴: **`Edit ▸ Project Settings`**
2. 왼쪽 트리에서 **Graphics** 클릭
3. **Default Render Pipeline Asset** 필드에 `Assets/Settings/Mobile_RPAsset` 드래그
4. 또는 **Quality** 설정에서:
   - Quality 의 Android 열 (안드로이드 마크) 활성화된 레벨 선택
   - **Render Pipeline Asset** 에 Mobile_RPAsset 할당

---

## 단계 4 — Player Settings

1. **`Edit ▸ Project Settings ▸ Player`** (또는 Build Profiles 의 Player Settings 버튼)
2. **Company Name**: `RTS RYS RYY` 같은 이름 (혹은 단순화)
3. **Product Name**: **`어린이 대항해 시대`** (스토어 표시명)

### Other Settings 펼쳐서 채울 것

| 항목 | 값 |
|---|---|
| **Package Name** | `com.rts.rys.ryy.head.to.sea` (이미 결정된 패키지 ID) |
| **Version** | `0.1` (M1 임시) |
| **Bundle Version Code** | `1` |
| **Minimum API Level** | **API 23 (Android 6.0)** 또는 그 이상 (Galaxy S23 은 Android 13+) |
| **Target API Level** | **Automatic (Highest Installed)** |
| **Scripting Backend** | **IL2CPP** |
| **Api Compatibility Level** | .NET Standard 2.1 (기본값) |
| **Target Architectures** | ☑ **ARMv7**, ☑ **ARM64** (둘 다 체크. Play Store 정식 출시는 ARM64 필수) |

### Resolution and Presentation 섹션

| 항목 | 값 |
|---|---|
| **Default Orientation** | **Landscape Left** |
| Auto Rotation: Portrait | ☐ 해제 |
| Auto Rotation: Portrait Upside Down | ☐ 해제 |
| Auto Rotation: Landscape Left | ☑ |
| Auto Rotation: Landscape Right | ☑ |

→ 가로 모드 고정 (좌/우 두 방향)

---

## 단계 5 — 첫 빌드 → S23 설치

1. **Build Profiles** 창
2. **Run Device** 드롭다운에서 USB 연결된 **`Galaxy S23`** 선택
   - 안 보이면 옆의 ↻ 새로고침 또는 USB 재연결
3. **`Build And Run`** 클릭
4. 저장 경로: `D:/unity-editor/workspace/HorizontalChild/Builds/` 같은 폴더에 (없으면 만들기)
5. 파일 이름: **`HeadToSea-M1.apk`** 같은 형태
6. 빌드 → 자동으로 S23 에 설치 → 자동 실행 (**수 분~십 수 분 걸림**)

### 빌드 중 발생 가능한 에러

| 에러 | 해결 |
|---|---|
| `Gradle build failed` | Console 의 빨간 에러 확인. 보통 SDK/NDK 누락 또는 패키지 충돌 |
| `INSTALL_FAILED_VERSION_DOWNGRADE` | 같은 패키지 ID 의 다른 버전이 폰에 이미 있음. 폰에서 기존 앱 삭제 후 재시도 |
| `Unable to detect target devices` | USB 디버깅 허용 안 됨. 폰에서 팝업 확인 후 허용 |
| `JAVA_HOME / JDK not found` | Unity Hub 의 Android Build Support 의 OpenJDK 모듈 누락. Add Modules 에서 설치 |

---

## 단계 6 — 실기 동작 확인

1. S23 가로로 들고 앱 자동 실행 확인
2. 확인 항목:

| 영역 | 확인 |
|---|---|
| 화면 회전 | 가로 모드 고정. 세로로 들어도 화면 안 돌아감 |
| 배 표시 | 캐러벨 모델 보임 |
| 좌측 조이스틱 | 손가락으로 끌면 배가 회전 |
| 우측 ▲ 버튼 | 누르면 가속 |
| 우측 ▼ 버튼 | 누르면 감속 |
| 좌상단 HUD | "현재 의뢰 (없음)" 표시 |
| 항구 빨간 원기둥 | 보임 |
| 항구 들어가기 버튼 | 누르면 가까운 항구로 진입 |
| 모험가 조합 의뢰 수락 | UI 정상 동작 |
| "정박 및 탐색" 버튼 | 발견물 좌표에서 동작 |
| 발견 패널 | 한글 정상 표시 |
| Safe Area | 펀치홀(좌상단 또는 우상단) 에 UI 가 가리지 않음 |

---

## 단계 7 — 흔한 문제

| 증상 | 해결 |
|---|---|
| 폰에 설치는 됐는데 검은 화면 | Console (Unity 가 켜져 있으면) 또는 `adb logcat` 으로 에러 확인. 보통 SO 인스턴스 누락 |
| 조이스틱·버튼 안 보임 | Canvas Scaler 의 Reference Resolution (2340×1080) 과 Match (0.5) 확인 |
| 조이스틱 동작 안 함 | OnScreenStick 의 Control Path 확인. 또는 EventSystem 누락 |
| 폰트가 다 네모 (□) | Pretendard SDF 가 Static 인데 Atlas 가 빌드에 포함 안 됨. Dynamic 모드로 변경 또는 Atlas 더 크게 다시 빌드 |
| 펀치홀에 UI 가림 | Player Settings → Resolution → **Render Outside Safe Area** 옵션 확인. 또는 Canvas Scaler 의 Safe Area 처리 추가 |
| 앱 아이콘이 기본 Unity 로고 | Player Settings → Icon 섹션에서 아이콘 설정 (M2 이후) |

---

## 단계 8 — 다음 시도

빌드/실기 동작이 OK 면 M1 완료. 다음 단계:

- **자금/명성 HUD** — 의뢰 보상이 어디 들어갔는지 화면에 표시
- **도감 보기 UI** — 발견한 항목을 모아보는 UI (M2)
- **패널 자동 레이아웃 일괄 적용** — 텍스트 길이 자동 대응
- **M2 — 8개국 선택** + 시작 항구 + 국적 분리
- **M3 — 메카닉 풀세트** (전투, NPC 100명, 시세, 조선소…)
- **M4·M5 — 콘텐츠 본판 + Play 출시 준비** (아이콘, 스크린샷, release.jks 등)

---

## 부록 — 키스토어 (지금은 무시)

| keystore | 용도 | 만들 시점 |
|---|---|---|
| `debug.keystore` | 개발 테스트 — Unity 자동 생성 | 자동 |
| `release.jks` | Play Store 정식 출시 | M4~M5 (출시 직전) |

Release 키스토어 만들 시점에 별도 가이드 작성. 지금은 무시.
