# 모험가 조합 UI 셋업 가이드 (B1·B2)

`MissionService` 컴포넌트 부착 + `MissionGiverPanel` 패널 만들기 + `PortScreen` 에 "모험가 조합" 버튼 추가.

---

## 단계 1 — MissionService 컴포넌트 부착

`MissionService` 는 의뢰 상태 관리자. 어디서든 `MissionService.Instance` 로 접근 가능.

1. Hierarchy 에서 **`GameManager`** 클릭
2. Inspector 의 **`Add Component`** → 검색창에 **`Mission`** → **`Mission Service`** 클릭
3. 새 컴포넌트의 **`All Missions`** 배열에 의뢰 SO 등록:
   - Size = **1**
   - Element 0 = `Assets/Game/Data/Missions/Mission_DiscLisbonGibraltar.asset` 드래그

> 추후 의뢰가 늘어나면 배열 Size 를 늘려서 추가.

---

## 단계 2 — MissionGiverPanel 패널 만들기

### 2-1. 패널 GameObject

1. Hierarchy 의 **Canvas** 아래에 **UI ▸ Panel** 추가, 이름 **`MissionGiverPanel`**
2. RectTransform 으로 화면 중앙에 큰 패널 (예: 1800×800)
3. 색상은 PortScreen 과 비슷한 옅은 베이지 + 약간 어두운 톤

### 2-2. 자식 UI

`MissionGiverPanel` 하위:

| 자식 | 종류 | 이름 | 위치 |
|---|---|---|---|
| 1 | UI ▸ Text - TextMeshPro | `PortNameText` | 상단 — "리스본 모험가 조합" 형태 |
| 2 | UI ▸ Text - TextMeshPro | `MissionTitleText` | 중상단 — 의뢰 제목 |
| 3 | UI ▸ Text - TextMeshPro | `MissionDescriptionText` | 중단 — 의뢰 설명 |
| 4 | UI ▸ Text - TextMeshPro | `StatusText` | 중단 — "이미 의뢰 중" 같은 안내 |
| 5 | UI ▸ Button - TextMeshPro | `AcceptButton` | 하단 좌측 — "예, 받겠어요" |
| 6 | UI ▸ Button - TextMeshPro | `CloseButton` | 하단 우측 — "닫기" |

> 모든 TMP_Text 는 이전과 같이 **Anchor stretch + 적당한 여백** 으로 설정.

### 2-3. 컴포넌트 부착

1. **`MissionGiverPanel`** 선택
2. Add Component → **`Mission Giver Panel`** 검색·추가
3. 필드 채우기:

| 필드 | 값 |
|---|---|
| Panel Root | 비워둠 (자동) |
| Port Name Text | 자식 PortNameText 드래그 |
| Mission Title Text | 자식 MissionTitleText 드래그 |
| Mission Description Text | 자식 MissionDescriptionText 드래그 |
| Status Text | 자식 StatusText 드래그 |
| Accept Button | 자식 AcceptButton 드래그 |
| Close Button | 자식 CloseButton 드래그 |
| Mission Service | 비워둠 (런타임 자동 검색) |

---

## 단계 3 — PortScreen 에 "모험가 조합" 버튼 추가

PortScreen 안에 새 버튼을 만들고 MissionGiverPanel 과 연결.

### 3-1. 버튼 추가

1. Hierarchy 에서 **`PortScreen`** 펼치기
2. PortScreen 하위에 **UI ▸ Button - TextMeshPro** 추가, 이름 **`GuildButton`**
3. 위치는 LeaveButton 옆 (좌하단 또는 본문 영역)
4. GuildButton 의 자식 Text → **"모험가 조합 가기"** 로 변경

### 3-2. PortScreen 컴포넌트에 연결

1. **`PortScreen`** 선택
2. Inspector 의 PortScreen 컴포넌트에서 새로 추가된 필드 확인:
   - **`Mission Giver Panel`** 칸에 Hierarchy 의 **MissionGiverPanel** 드래그
   - **`Guild Button`** 칸에 자식 **GuildButton** 드래그

---

## 단계 4 — 첫 테스트

1. ▶ Play
2. 키보드로 리스본에 접근 → 도착 다이얼로그 → "예, 들어갈게요"
3. PortScreen 표시됨 → **"모험가 조합 가기"** 클릭
4. MissionGiverPanel 이 PortScreen 위에 표시되어야 함
5. 의뢰 제목 ("두 바다가 만나는 좁은 길을 찾아봐요") + 설명 보임
6. **"예, 받겠어요"** 클릭 → Console 에 다음 로그:
   ```
   [MissionService] 의뢰 수락: mission.disc.lisbon.gibraltar
     (두 바다가 만나는 좁은 길을 찾아봐요)
   ```
7. 다시 모험가 조합 가기 클릭 → 이번에는 "이미 다른 의뢰를 받고 있어요" 안내가 나타나야 함

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| Add Component 에서 `Mission Service` 안 보임 | 컴파일 에러 가능성. Console 확인 |
| 패널은 열리는데 의뢰 정보 비어있음 | MissionService 의 All Missions 배열에 Mission_DiscLisbonGibraltar 가 들어있는지 확인 |
| 의뢰 정보 비었고 "지금은 받을 의뢰가 없어요" 표시 | issuerPort 가 다른 항구로 매칭되었거나, Mission SO 의 Issuer Port 필드가 Port_Lisbon 이 아님 — SO 다시 확인 |
| 수락해도 "이미 다른 의뢰 진행 중" 메시지 | 이전에 받은 의뢰가 메모리에 남아 있음 — Play 다시 시작 (M1 은 저장 없음) |
| MissionGiverPanel 이 PortScreen 보다 아래에 가려짐 | Hierarchy 에서 MissionGiverPanel 을 PortScreen 보다 **아래쪽**(나중)에 배치 (UI 는 아래일수록 위에 그려짐) |

---

## 동작 흐름 요약

```
배가 리스본 도착
  ↓
PortArrivalDialog (예/취소)
  ↓ "예"
PortScreen (도시명·특산물)
  ↓ "모험가 조합 가기"
MissionGiverPanel (의뢰 제목·설명·수락)
  ↓ "예, 받겠어요"
MissionService.TryAcceptMission()
  ↓
MissionService.CurrentMission = 지브롤터 의뢰
  ↓
패널 닫힘 — 이제 활성 의뢰 보유 상태
```

다음 단계 (B3~B6): 이 활성 의뢰가 화면 어딘가에 표시되고(B3), "정박 및 탐색" 으로 발견하고(B4·B5), 리스본 복귀 시 완료 보상(B6).
