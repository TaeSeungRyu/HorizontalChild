# 선원 시스템 — 내가 할 일

코드는 이미 준비됨. 아래 순서대로 인스펙터·씬 작업하면 게임에서 동작.

---

## A. PlayerCrew 컴포넌트 추가

**선원 명부를 보관할 싱글톤** 이 씬에 있어야 함.

1. Hierarchy 빈 공간 우클릭 → Create Empty
2. 이름 `PlayerCrew` 로 변경
3. Inspector → Add Component → **Player Crew**
4. (옵션) `Max Crew` 값 확인 — 기본 10

> 또는 PlayerShip 같은 영구 GameObject 에 PlayerCrew 컴포넌트 추가해도 됨.

---

## B. SaveService 카탈로그 할당

선원 저장·복원에 NpcCatalog 필요.

1. Hierarchy 의 **SaveService** 컴포넌트 선택
2. Inspector 의 **Npc Catalog** 필드에 `Assets/Game/Data/_Catalogs/NpcCatalog.asset` 드래그

> ShipCatalog 도 같은 곳에 할당했어야 함 — 빠졌으면 같이.

---

## C. 시드 재실행 (능력치 보너스 + 명성 게이트 적용)

기존 NPC 에셋엔 `requiredGoodReputation` 등 새 필드가 없거나 hireBonus 가 0.

1. 메뉴 `Game ▸ Seed M3 NPCs` 실행 — 100명 새로 생성 (hireBonus·gate 자동 채워짐)
2. 메뉴 `Game ▸ Refresh All Catalogs` — NpcCatalog 갱신

**자동 설정 결과**:
- hireBonus = `(stat - 50) / 10` 클램프 -2 ~ +5 → 능력치 좋을수록 +값
- 해적: 나쁜 명성 ≥ 10 필요
- 호위선: 좋은 명성 ≥ 5 필요
- 상선: 게이트 없음

---

## D. 선원 명부 패널 (CrewPanel) 만들기

### 1. GameObject 생성
1. 항구 화면(Port UI) Canvas 하위에 빈 GameObject 만들고 이름 `CrewPanel`
2. Inspector → Add Component → **Crew Panel**
3. 우클릭 → **Auto Layout** → 풀스크린 + 스크롤 리스트 자동 생성

### 2. 필드 할당
- `Player Ship` — Hierarchy 의 PlayerShip 드래그 (자동 검색되지만 명시 권장)
- 나머지 텍스트·버튼 필드는 Auto Layout 으로 자동 채워짐

### 3. PortScreen 에 연결
1. **PortScreen** 컴포넌트 선택
2. **Crew Panel** 필드에 위에서 만든 CrewPanel 드래그
3. **Crew Button** 필드에 도시 화면에 추가할 새 버튼 드래그

### 4. 도시 화면 (PortScreen) 에 "선원 명부" 버튼 추가
- Hierarchy 에서 시장·조선소·광장 버튼들이 있는 컨테이너에 Button 추가
- 이름 예: `Button_Crew`, Label "👥 선원 명부"
- 위의 **Crew Button** 필드에 드래그

---

## E. 검증

### 광장에서 고용
1. Play → 항구 진입 → 광장 열기
2. 행 형식 확인: `이름 / 능력치 / 보너스(+X/+Y/+Z) / 명성 요구 / 가격 / [고용]` 버튼
3. 상단 헤더: `선장: 이름 · 선원 N/10` 표시
4. 고용 → 잔돈 차감 + 선원 N → N+1

### 명부 확인
1. 항구 화면에서 `선원 명부` 버튼 → CrewPanel 열림
2. 고용한 선원 행 + 총 보너스 합 표시
3. **해고** 버튼 → 명부에서 제거 (영구 — 다시 데려올 수 없음)

### 능력치 합산
1. 고용 전 vs 후 비교:
   - 항해 속도 (seamanship 보너스)
   - 전투 결과 (bravery 보너스)
   - 명중률 (CombatSequence seamanship)
2. 효과 즉시 적용 — 별도 동작 불필요

### 명성 게이트
1. 새 게임 시작 → 좋은/나쁜 명성 모두 0
2. 해적 NPC 광장에서 버튼 라벨 "명성 부족" (나쁜 명성 ≥ 10 필요)
3. 호위선 광장에서도 "명성 부족" (좋은 명성 ≥ 5 필요)
4. 미션 완료해서 명성 쌓으면 → 해제

### 저장/복원
1. 선원 3명 고용 → 잔돈 확인 → 게임 종료
2. 다시 시작 → 명부에 3명 그대로 + 능력치 보너스 유지

---

## 문제 발생 시

| 증상 | 원인 | 해결 |
|---|---|---|
| 광장에서 고용해도 능력치 변화 없음 | PlayerCrew 컴포넌트 없음 | A 단계 |
| 저장 후 재시작하면 선원 사라짐 | SaveService.npcCatalog 미할당 | B 단계 |
| 광장에서 행이 빈 채로 뜸 | 옛 NPC 에셋이 hireBonus 0 | C 단계 (시드 재실행) |
| 명부 패널 안 열림 | PortScreen.crewPanel / crewButton 미할당 | D-3 단계 |
| 모든 NPC 가 "고용됨" 표시 | PlayerCrew 가 씬에 두 개 중복 | Hierarchy 검색해 하나만 남기기 |

---

## 새 게임 시작 권장

옛 세이브에는 crewNpcIds 가 없어서 빈 상태로 시작 — 그건 정상. 하지만 옛 NPC 데이터에 hireBonus / 명성 게이트가 비어있으면 광장 표시가 이상할 수 있음 → **C 단계 시드 재실행 후 SaveService Delete Save → 새 게임 시작 권장**.
