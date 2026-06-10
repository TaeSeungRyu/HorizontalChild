# 내 정보 패널 — 내가 할 일

코드 준비 완료. 인스펙터 셋업만 하면 됨.

---

## A. PlayerInfoPanel 만들기 (팝업)

1. Hierarchy 의 HUD Canvas 하위에 빈 GameObject 만들고 이름 `PlayerInfoPanel`
2. Inspector → Add Component → **Player Info Panel**
3. 우클릭 → **Auto Layout** → 풀스크린 배경 + 중앙 타이틀/콘텐츠 + 하단 닫기 버튼 자동 생성
4. `Player Ship` 필드에 PlayerShip 드래그 (자동 검색되지만 명시 권장)

생성 직후엔 비활성 상태 — 정상.

---

## B. PlayerInfoButton 만들기 (HUD)

기존 ActiveMissionHUD 위치(좌상단)에 들어가는 버튼.

1. Hierarchy 의 HUD Canvas 하위에 빈 GameObject 만들고 이름 `PlayerInfoButton`
2. Inspector → Add Component → **Player Info Button**
3. 우클릭 → **Auto Layout** → 좌상단 (30, -30) 위치에 원형 아바타 + 이름 자동 생성
4. 필드 할당:
   - `Player Ship` — Hierarchy 의 PlayerShip 드래그
   - `Info Panel` — A 에서 만든 PlayerInfoPanel 드래그
5. (옵션) `Hide While Any Active` — 다른 패널 떠 있을 때 숨기려면:
   - PortScreen, BigMapPanel, JournalPanel, NationSelectionPanel, PauseMenuPanel 등 드래그

---

## C. ActiveMissionHUD 처리 (선택)

미션 정보가 이제 PlayerInfoPanel 안에 통합됨. 기존 좌상단 미션 HUD 는:

**옵션 1 — 그대로 둠**: 상시 미션 표시 유지. PlayerInfoButton 은 그 옆 / 위 / 아래로 위치 조정.

**옵션 2 — 비활성화**: Hierarchy 에서 ActiveMissionHUD GameObject 의 체크 해제 → 안 보임. 미션 정보는 PlayerInfoPanel 클릭으로만 확인.

**추천**: 옵션 2 — 화면 깔끔. 미션 상태는 자주 안 봐도 됨 (PlayerInfoButton 클릭하면 됨).

---

## D. 검증

1. Play → 좌상단에 원형 아바타 + 선장 이름 표시
2. 클릭 → 풀스크린 패널 열림:
   - 선장 이름 / 배 이름 + 내구 / 잔돈 / 명성
   - 능력치 표: 기본 / 보너스(+) / 합계
   - 선원 N/10 + 이름 나열
   - 현재 의뢰 상태 (없음 / 찾기 / 발견 완료)
3. 선원 고용 → 다시 열어 보너스 합산 확인
4. 의뢰 수락 → 다시 열어 의뢰 정보 확인
5. 닫기 버튼 → 패널 닫힘

---

## 문제 해결

| 증상 | 원인 | 해결 |
|---|---|---|
| 아바타 클릭해도 안 열림 | Info Panel 미할당 | B-4 단계 |
| 이름이 "선장" 으로 표시 | playerShip 또는 captain 미설정 | 국가 선택 안 한 상태 — 게임 진행 |
| 능력치 보너스 0 | PlayerCrew 컴포넌트 없음 | CREW_SETUP_TODO.md 의 A 단계 |
| 의뢰 정보 안 보임 | MissionService 미존재 | 씬에 MissionService 컴포넌트 추가 |
| 좌상단 위치 겹침 | 기존 ActiveMissionHUD 와 충돌 | C 옵션 2 (HUD 비활성) |

---

## 향후 확장

- **아바타 이미지**: `PlayerInfoButton.avatarImage.sprite` 에 실제 이미지 할당. 현재는 placeholder 원에 이름 첫 글자.
- **국적 깃발**: 패널에 nation 깃발 표시 추가
- **능력치 게이지바**: 막대그래프로 시각화
- **선원 행 상세**: PlayerInfoPanel 의 선원 섹션을 클릭하면 CrewPanel 열기
