# 항구 UI 셋업 가이드 (M1)

`PortArrivalDialog` + `PortScreen` 두 패널을 만들고 SeaWorldManager 와 연결.

## 사전 — TextMeshPro Essentials Import

처음 TMP_Text 를 사용하면 Unity 가 "TMP Essentials Import" 다이얼로그를 띄움 → **Import TMP Essentials** 클릭. 한 번만 하면 됨.

---

## 단계 1 — Canvas 만들기 (이미 있으면 건너뜀)

1. Hierarchy 우클릭 ▸ **UI ▸ Canvas**
2. Canvas Scaler:
   - UI Scale Mode = **Scale With Screen Size**
   - Reference Resolution = **2340 × 1080**
   - Match = **0.5**
3. EventSystem 이 자동 생성됨 (Canvas 추가 시)

---

## 단계 2 — PortArrivalDialog 패널 만들기

### 2-1. 패널 GameObject

1. Canvas 하위에 **UI ▸ Panel** 추가, 이름 **`PortArrivalDialog`**
2. RectTransform 으로 화면 가운데에 작은 박스 모양 (예: 800×400)
3. 색상은 반투명 어두운 색 (Color 의 A=200 정도)

### 2-2. 자식 UI

PortArrivalDialog 하위에 다음을 추가:

| 자식 | 종류 | 이름 |
|---|---|---|
| 1 | UI ▸ Text - TextMeshPro | `TitleText` |
| 2 | UI ▸ Text - TextMeshPro | `MessageText` |
| 3 | UI ▸ Button - TextMeshPro | `YesButton` |
| 4 | UI ▸ Button - TextMeshPro | `CancelButton` |

각각 적당히 배치 (Title 위, Message 가운데, 버튼 두 개 아래쪽 좌우).

버튼의 텍스트:
- YesButton 의 하위 Text → "예, 들어갈게요"
- CancelButton 의 하위 Text → "아직 안 들어가요"

### 2-3. 컴포넌트 부착

1. **`PortArrivalDialog`** 선택
2. Add Component → **`Port Arrival Dialog`**
3. 필드 채우기:

| 필드 | 값 |
|---|---|
| Panel Root | 비워둠 (자동으로 자기 자신) |
| Title Text | 자식 TitleText 드래그 |
| Message Text | 자식 MessageText 드래그 |
| Yes Button | 자식 YesButton 드래그 |
| Cancel Button | 자식 CancelButton 드래그 |
| Port Screen | 다음 단계에서 만든 후 할당 |
| World Manager | Hierarchy 의 GameManager 드래그 |
| Player Ship | Hierarchy 의 PlayerShip 드래그 |

---

## 단계 3 — PortScreen 패널 만들기

### 3-1. 패널 GameObject

1. Canvas 하위에 **UI ▸ Panel** 추가, 이름 **`PortScreen`**
2. RectTransform 으로 화면 거의 전체를 덮는 큰 패널 (예: 2200×1000)
3. 색상은 따뜻한 베이지 / 갈색 (옛 양피지 느낌)

### 3-2. 자식 UI

PortScreen 하위:

| 자식 | 종류 | 이름 |
|---|---|---|
| 1 | UI ▸ Text - TextMeshPro | `NameText` |
| 2 | UI ▸ Text - TextMeshPro | `DescriptionText` |
| 3 | UI ▸ Text - TextMeshPro | `ProductListText` |
| 4 | UI ▸ Button - TextMeshPro | `LeaveButton` |

NameText 는 상단(큰 폰트, 60pt), DescriptionText 는 그 아래(중간 30pt), ProductListText 는 본문 영역(20pt, 텍스트 박스 크게), LeaveButton 은 우하단.

LeaveButton 의 하위 Text → "🏠 항구를 떠난다"

### 3-3. 컴포넌트 부착

1. **`PortScreen`** 선택
2. Add Component → **`Port Screen`**
3. 필드 채우기:

| 필드 | 값 |
|---|---|
| Panel Root | 비워둠 |
| Name Text | 자식 NameText 드래그 |
| Description Text | 자식 DescriptionText 드래그 |
| Product List Text | 자식 ProductListText 드래그 |
| Leave Button | 자식 LeaveButton 드래그 |

---

## 단계 4 — PortArrivalDialog ↔ PortScreen 연결

이제 PortArrivalDialog 의 비어두었던 `Port Screen` 필드를 채움:

1. **`PortArrivalDialog`** 선택
2. Inspector 의 `Port Screen` 칸에 Hierarchy 의 **`PortScreen`** 드래그

---

## 단계 5 — SeaWorldManager 이벤트에 다이얼로그 연결

마지막 — 항구 도착 시 다이얼로그가 자동으로 표시되도록:

1. Hierarchy 의 **`GameManager`** 선택
2. Inspector 의 `Sea World Manager` 컴포넌트에서 **`On Port Arrived (PortData)`** 이벤트 찾기
3. 그 옆의 **[+] 버튼** 클릭 → 새 슬롯 생김
4. **Object 슬롯**(왼쪽 None) 에 Hierarchy 의 **`PortArrivalDialog`** 드래그
5. **Function 드롭다운**(오른쪽 No Function) 클릭 → **`PortArrivalDialog ▸ Show (PortData)`** 선택
   - ⚠ 동적 매개변수(Dynamic Port Data) 의 `Show` 를 선택해야 함 — Static 매개변수가 아닌 쪽

---

## 단계 6 — 첫 테스트

1. PortArrivalDialog 와 PortScreen 패널을 **Inactive** 로 시작하도록 설정 (체크박스 해제) — 또는 `Awake()` 에서 자동으로 SetActive(false) 호출되므로 필수는 아님
2. ▶ Play
3. 키보드로 배를 리스본 좌표 근처로 이동:
   - 시작 위치 (-100, 0.5, 560) 이면 거의 도착 거리
   - ↑ 누르고 천천히 W·A·D 로 미세 조정
4. 약 20 unit 이내 들어오면 **"리스본 도착 — 들어가시겠습니까?" 다이얼로그**가 자동으로 떠야 함
5. "예" 클릭 → 항구 화면 (도시명·설명·특산물 리스트) 표시
6. "항구를 떠난다" → 다시 바다 화면, 그리고 멀어질 때까지 같은 항구 재트리거 안 함

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 다이얼로그가 안 뜸 | GameManager 의 On Port Arrived 이벤트에 PortArrivalDialog.Show 가 연결됐는지 / Player Ship 이 항구에 충분히 가까운지 (arrivalRadiusUnits=20 이내) |
| 다이얼로그는 뜨는데 "예" 누르면 화면이 빈 채로 | PortArrivalDialog 의 Port Screen 필드가 비어있음. PortScreen 드래그 |
| "예" 누르면 항구 화면은 뜨는데 특산물 안 나옴 | PortScreen 의 ProductListText 가 비어있거나 PortData 의 commonProducts 가 비어있음. M1 Content Seeder 가 정상 동작했다면 자동 채워졌어야 함 |
| 떠난 직후 즉시 다시 다이얼로그가 뜸 | SuppressPort 호출이 안 됨. PortScreen 의 LeaveButton 이 PortArrivalDialog.OnPortScreenClosed 를 호출하는지 확인 (코드상으로는 자동) |
| 너무 멀리 떨어진 곳에서도 다이얼로그 뜸 | arrivalRadiusUnits 값을 작게. 기본 20 unit ≈ 148km |
