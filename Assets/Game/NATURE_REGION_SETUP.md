# NatureStreamer 지역별 식생 셋업 가이드

지역에 따라 다른 식물·바위가 나오도록 NatureStreamer 의 `Region Sets` 를 채우는 작업 가이드.

## 1. 작업 목적

세계 지도 위에서 위·경도 박스로 지역을 나누고, 각 지역에 어울리는 prefab 들을 배치.
예: 유럽엔 침엽수, 사하라엔 모래·바위, 동남아엔 야자수 등.

## 2. 사전 준비

- `NatureStreamer` GameObject 를 씬에서 선택
- Inspector → `Prefabs (기본)` 에는 **모든 prefab** 드래그 (어디에도 매칭 안 될 때 fallback)
- 그 아래 `Region Sets` 에 다음 표대로 영역 추가

## 3. 지역별 식생 표 (12개 영역)

위에서 아래로 순서대로 배치하면 우선순위대로 매칭됨.

| # | Region Name | latMin | latMax | lngMin | lngMax | 권장 prefab 예시 |
|---|---|---|---|---|---|---|
| 1 | 유럽 | 35 | 71 | -10 | 40 | Tree_Pine, Tree_Round, Bush, Rock |
| 2 | 사하라·중동 사막 | 15 | 35 | -20 | 60 | Bush, Rock, Grass_Tuft (희소) |
| 3 | 열대 아프리카 | -35 | 15 | -20 | 50 | palm, Tree_Round, Bush, Mountain |
| 4 | 시베리아 | 50 | 75 | 40 | 180 | Tree_Dead, Tree_Pine (희소) |
| 5 | 동아시아 (한·중·일) | 20 | 50 | 95 | 150 | Tree_Pine, Tree_Round, Flower |
| 6 | 동남아·인도 | -10 | 25 | 70 | 140 | palm, Tree_Round, Bush |
| 7 | 북미 본토 | 25 | 50 | -130 | -65 | Tree_Pine, Tree_Round, Mountain |
| 8 | 알래스카·캐나다 북부 | 50 | 75 | -170 | -50 | Tree_Pine, Tree_Dead, Hill_Round |
| 9 | 카리브·중미 | 8 | 30 | -110 | -60 | palm, Tree_Round, Bush |
| 10 | 남미 북부 (아마존) | -15 | 12 | -85 | -35 | palm, Tree_Round, Bush, Mountain |
| 11 | 남미 남부 (안데스) | -55 | -15 | -85 | -35 | Tree_Pine, Bush, Mountain |
| 12 | 호주·뉴질랜드 | -47 | -10 | 110 | 180 | Bush, Rock, Tree_Dead, Grass_Tuft |

## 4. prefab 카탈로그 (참고)

`Assets/Game/Art/Models/Nature/` 위치한 prefab 들:

| prefab | 어울리는 지역 |
|---|---|
| `palm` 계열 (palm-detailed-straight 등) | 열대 (3, 6, 9, 10) |
| `Tree_Pine` | 침엽수림 (1, 4, 5, 7, 8, 11) |
| `Tree_Round` | 활엽수·열대 (1, 3, 5, 6, 7, 9, 10) |
| `Tree_Dead` | 한대·사막 (4, 8, 12) |
| `Bush` | 사막·관목지 (2, 3, 6, 10, 11, 12) |
| `Rock`, `Rock_Small` | 어디든 (전부) |
| `Grass_Tuft` | 사막·초원 (2, 12) |
| `Flower` | 온대 (1, 5) |
| `Mountain_Big`, `Mountain_Peak` | 산악 (3, 7, 10, 11) |
| `Hill_Round` | 평원·언덕 (1, 5, 7, 8) |
| `Stump` | 어디든 |

## 5. Inspector 셋업 절차

### Step 1 — 기본 Prefabs 채우기

NatureStreamer → Inspector → **Prefabs** (지역 매칭 없을 때 fallback).
모든 prefab 을 여기 드래그. Project 창에서 `Assets/Game/Art/Models/Nature/` 폴더 열고 전부 선택 → 드래그.

### Step 2 — Region Sets 펼치기

`Region Sets` 라벨 옆 ▶ 클릭 → **Size: 12** 입력. 12개 Element 슬롯 생성.

### Step 3 — 각 영역 채우기

위 표 순서대로 `Element 0 ~ 11` 채우기:

**예시: Element 0 = 유럽**
1. Region Name: `유럽` 입력
2. Lat Min: `35`, Lat Max: `71`
3. Lng Min: `-10`, Lng Max: `40`
4. Prefabs: `Tree_Pine`, `Tree_Round`, `Bush`, `Rock` 드래그
   - **palm 계열 제외** ← 핵심 (유럽에 야자수 안 나오게)

**예시: Element 2 = 열대 아프리카**
1. Region Name: `열대 아프리카`
2. Lat Min: `-35`, Lat Max: `15`
3. Lng Min: `-20`, Lng Max: `50`
4. Prefabs: `palm-detailed-straight`, `Tree_Round`, `Bush`, `Mountain_Big`
   - **palm 포함** ← 야자수 적도 지역에 나오게

(나머지 영역 동일 패턴)

### Step 4 — 저장

Inspector 변경은 자동 저장 (Ctrl+S 한 번 권장).

### Step 5 — 테스트

1. Play
2. 배를 유럽 항구 (예: 리스본, 런던) 근처로 이동
3. 카메라가 새 chunk 진입 시 → **유럽엔 침엽수만, 야자수 없음**
4. 적도 부근 (예: 잔지바르, 칼리만탄) 가면 → **야자수 등장**

## 6. 영역 정의 팁

- **위·경도 범위는 대략적** 이라 살짝 겹쳐도 OK. 첫 매칭이 우선이라 표 순서가 곧 우선순위
- **빈 영역** (예: 대서양 한가운데, 남극) 은 정의 안 함 → 기본 `Prefabs[]` fallback
- 식생이 너무 적으면 → `Per Chunk` (Inspector) 값 키우기
- 야자수가 어디서 나오는지 다시 확인하려면 → Region Set 에서 palm 들어간 영역의 lat/lng 박스 확인

## 7. 조정 후 적용

Inspector 변경은 즉시 반영되지 않을 수 있음 (이미 로드된 chunk 는 기존 식생 유지). 카메라가 멀리 갔다가 다시 오면 새 chunk 로 재배치 → 새 설정 적용.

깔끔하게 보려면 Play 재시작.

## 8. 향후 확장

- prefab 더 추가하고 싶으면 `Assets/Game/Art/Models/Nature/` 에 .fbx 넣고 prefab 화 → 해당 지역 Region Set 의 Prefabs 에 드래그
- 지역을 더 세분화하고 싶으면 (예: 일본 vs 한국) → Region Sets Size 늘리고 새 영역 추가 (해당 lat/lng 박스 좁게)
- 강 위는 자동 제외 (RiverRegistry 가 처리) — 별도 작업 X
