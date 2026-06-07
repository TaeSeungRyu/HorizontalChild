# 일시정지 메뉴 셋업 가이드

게임 일시정지 + 새 게임 + 종료 흐름. 사용자 경험의 마지막 빈자리.

기획서 보강 항목 — 메인 메뉴는 추후 (현재는 NationSelectionPanel 이 시작 화면 역할).

---

## 동작

- HUD 의 **☰ 버튼** 탭 → 일시정지 메뉴
- 또는 **Esc 키** (Editor 테스트) / **Android Back 키** → 일시정지
- 3가지 선택:
  - **Resume**: 게임 재개 (Time.timeScale = 1)
  - **새 게임 시작**: 저장 파일 삭제 + 씬 재로드 (국가 선택부터)
  - **종료**: Application.Quit (Editor 에서는 Play 중지)

패널 떠 있는 동안 Time.timeScale = 0 — 항해도 멈춤.

---

## 단계 1 — PauseMenuPanel UI 만들기

1. Canvas 우클릭 → Create Empty → 이름 **`PauseMenuPanel`**
2. Add Component → **`Pause Menu Panel`**
3. 자식 GameObject:
   - **TitleText** (UI ▸ Text - TextMeshPro)
   - **ResumeButton** (UI ▸ Button - TextMeshPro, 라벨 "계속")
   - **NewGameButton** (라벨 "새 게임")
   - **QuitButton** (라벨 "종료")
4. PauseMenuPanel 컴포넌트의 각 필드에 자식 드래그
5. ⋮ → **Auto Layout** → 풀스크린 어두운 오버레이 + 가운데 정렬

---

## 단계 2 — PauseButton 만들기

HUD 어딘가에 작은 메뉴 버튼:

1. Canvas 아래 Button 추가 → 이름 **`PauseButton`**
2. 라벨 "☰" 또는 "메뉴" (TMP)
3. 위치: 상단 좌측 또는 상단 우측 (보통 미니맵 반대편)
4. Add Component → **`Pause Button`**
5. 필드:
   - **Button** ← 자기 자신 (자동)
   - **Pause Menu Panel** ← Hierarchy 의 `PauseMenuPanel`
   - **Hide Until Nation Selected** ☑
   - **Listen Back Key** ☑
   - **Hide While Any Active** — 다른 풀스크린 패널들 (PortScreen, MarketPanel 등) 추가

---

## 단계 3 — 다른 HUD 의 hideWhileAnyActive 에 PauseMenuPanel 추가

일시정지 떠 있는 동안 다른 HUD 가 숨겨지도록:
- JournalButton, MinimapHUD, AnchorButton, EnterPortButton, ActiveMissionHUD, WalletHUD, BigMapButton (있다면) 등

---

## 단계 4 — Play 테스트

1. ▶ Play → 국가 선택 → 항해
2. **☰ 버튼** 또는 **Esc** → 일시정지 메뉴 등장 + 화면 어두워짐
3. **Resume** → 다시 항해
4. **새 게임** → 저장 삭제 + 씬 리로드 + 국가 선택부터
5. **종료** → Editor 에서는 Play 중지, 빌드에선 앱 종료

---

## Android 빌드에서

- Application.Quit() 작동 — 앱이 정상 종료
- Back 키 → PauseButton 의 listenBackKey 가 처리
- 이미 메뉴 떠 있을 때 Back → Toggle 로 닫힘

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 메뉴 떴는데 배가 움직임 | `Pause Game Time` ☑ 확인 (Time.timeScale = 0) |
| 새 게임 후에도 옛 진행 남음 | 씬 재로드는 GameObject 상태만 초기화. 싱글톤 안에 static 값이 남으면 별도 처리 필요 (이번 구조엔 없음) |
| Esc 가 작동 안 함 | PauseButton 의 `Listen Back Key` ☑ / Input System 활성 확인 |
| 종료 후 다시 실행 시 새 게임으로 시작 | 정상 — Save 가 삭제됐기 때문. Resume 만 누르고 종료해야 진행 유지 |
| 패널 떠도 어두워지지 않음 | Auto Layout 안 함 → ⋮ → Auto Layout 클릭 |

---

## 추후 폴리시

| 작업 | 효과 |
|---|---|
| 메인 메뉴 씬 (앱 진입 시 별도) | 새 게임 / 계속 / 옵션 / 종료 4개 선택 |
| 설정 패널 (Settings) | BGM·SFX 볼륨, 자막 크기 |
| 새 게임 확인 다이얼로그 | "정말 새 게임? 진행이 사라져요" |
| 종료 확인 다이얼로그 | 실수 종료 방지 |
| 진행 요약 (현재 발견 N/M, 잔돈 등) | 일시정지 화면에 정보 표시 |
| BGM 일시 정지·페이드 | 오디오 도입 후 |
