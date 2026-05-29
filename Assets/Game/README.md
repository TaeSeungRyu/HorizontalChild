# Assets/Game/

**어린이 대항해 시대** 게임 코드·에셋·콘텐츠의 루트 폴더.

Editor Terminal(`Assets/Bitpurr Digital/`)과 분리된 게임 전용 영역. 모든 게임 코드는 `Game.asmdef` 어셈블리에 격리된다.

---

## 폴더 구조

```
Assets/Game/
├── Art/                     ← 게임 아트 · 라이선스 출처 (CREDITS.md)
│   └── CREDITS.md           외부 에셋 출처·라이선스 누적 기록
├── Audio/                   ← BGM·SFX (1차 마일스톤에서는 비어 있음)
├── Data/                    ← 디자인 문서 + ScriptableObject 인스턴스
│   ├── GAME_MECHANICS.md    게임 시스템 규칙 (능력치·전투·NPC·미션·시세)
│   ├── CONTENT_DESIGN.md    실제 콘텐츠 데이터 워크북 (국가·항구·발견물·캐릭터…)
│   ├── Nations/             NationData .asset 인스턴스
│   ├── Ports/               PortData .asset 인스턴스
│   ├── Products/            ProductData .asset 인스턴스
│   ├── Discoveries/         DiscoveryData .asset 인스턴스
│   ├── Regions/             RegionData .asset 인스턴스
│   ├── Characters/          CharacterData / NpcDefinition .asset 인스턴스
│   ├── Missions/            MissionTemplate .asset 인스턴스
│   └── Ships/               ShipData .asset 인스턴스
├── Prefabs/                 ← Ship / Port / DiscoveryTrigger 등 (M1 이후 채움)
├── Scenes/                  ← World.unity / Port.unity (M1 시작 시 작성)
├── Scripts/                 ← 모든 게임 C# 코드
│   ├── Game.asmdef          어셈블리 정의 (Editor Terminal과 분리)
│   ├── Ship/                ShipController, ShipMovement
│   ├── World/               WorldMap, CameraRig, GeoCoordinate
│   ├── Ports/               PortTrigger, PortPanelUI
│   ├── Discovery/           DiscoveryTrigger, DiscoveryPanelUI, Journal
│   ├── Data/                NationData.cs / PortData.cs / ... (SO 정의)
│   ├── Input/               터치 입력 wrapper
│   ├── UI/                  HUD, 메뉴, 도감 UI
│   └── Save/                JSON 저장 시스템
└── Settings/                ← 게임 전역 SO (속도·UI 톤 등)
```

---

## 참고 문서 (리포 루트)

- **`/GAME_PREP.md`** — 비전·정책·플랫폼·빌드·폴더 구조·앱 정책(오프라인·권한0·광고0).
- **`Assets/Game/Data/GAME_MECHANICS.md`** — 게임 메카닉 / 시스템 규칙.
- **`Assets/Game/Data/CONTENT_DESIGN.md`** — 콘텐츠 데이터 워크북.

---

## 외부 에셋

받은 에셋은 `Assets/kenney_pirate-kit/` 에 있음 (Kenney Pirate Kit, CC0). 추후 `Assets/ThirdParty/` 하위로 이동 권장 — Unity 에디터에서 드래그하여 GUID 보존.

자세한 출처·라이선스는 [`Art/CREDITS.md`](Art/CREDITS.md) 참조.

---

## 어셈블리

`Scripts/Game.asmdef` 가 모든 게임 코드를 `Game` 어셈블리로 묶는다:
- Editor Terminal 어셈블리(`EditorTerminal`)와 격리.
- 빌드 시 Editor 전용 코드 자동 제외.
- 추후 `Game.Tests.asmdef` 추가로 테스트 분리 가능.

Unity Input System 패키지 참조는 사용자가 에디터에서 직접 추가:
1. Project 창에서 `Game.asmdef` 선택
2. Inspector → Assembly Definition References → `+` → "Unity.InputSystem" 선택
3. Apply

---

## 다음 작업 (M1 진입)

1. **SO 정의 코드 작성** (`Scripts/Data/NationData.cs` 등 8개 클래스)
2. **M1 SO 인스턴스 생성** — `CONTENT_DESIGN.md` 의 1차 콘텐츠를 `.asset` 으로 옮김
3. **`World.unity` 씬 생성** — Cinemachine 카메라 + 임시 평면 + 배 프리팹
4. **`ShipController.cs`** — 터치 입력으로 조타/추진
5. **첫 빌드** — `.apk` 를 Galaxy S23 실기에 설치, 조작감 확인
