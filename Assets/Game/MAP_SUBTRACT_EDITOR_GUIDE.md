# 맵 카브 에디터 — 사용 가이드

베이크된 `WorldLand.mesh` 에서 사각형 또는 폴리라인(강·해협) 영역을 잘라내는 도구.
시각적으로 진짜 구멍이 뚫리고 충돌도 동시에 풀려서 배가 통과 가능해진다.

> ⚠ **Unity Editor Play 모드 전용** — 빌드(.exe/.apk)에선 저장/베이크 부분 동작 안 함.

---

## A. 한 번만 셋업

### 1. 카탈로그 + 예시 영역 시드
Unity 메뉴 → `Game ▸ Seed M10 Map Subtracts`
- `Assets/Game/Data/_Catalogs/MapSubtractCatalog.asset` 생성
- 4개 예시 영역 자동 등록: 나일강·아마존강·말라카·스카게라크

### 2. 첫 베이크
Unity 메뉴 → `Game ▸ Bake World Land Mesh from GeoJSON`
- `WorldLand.mesh` 가 시드된 4개 영역을 잘라낸 상태로 다시 구워짐
- 씬에 이미 `WorldLand` prefab 인스턴스가 있으면 자동 반영 (MeshFilter 가 같은 mesh asset 참조)

### 3. MapSubtractEditor GameObject 추가
1. Hierarchy 빈 공간 우클릭 → Create Empty
2. 이름: `MapSubtractEditor`
3. Add Component → **Map Subtract Editor**

### 4. Inspector 필드 할당

| 필드 | 값 |
|---|---|
| **Catalog** | `_Catalogs/MapSubtractCatalog.asset` 드래그 |
| **Main Camera** | 메인 카메라. 비우면 자동 검색 |
| **Player Ship** | 플레이어 배 GameObject (입력 잠금용) |
| **Label Font** | `Art/Pretendard-Regular SDF.asset` 드래그 (한글 라벨) |
| **Enable On Start** | ☐ 평소엔 끔. Play 후 ContextMenu 로 수동 활성 |

---

## B. 사용법

### 활성화
- Inspector → MapSubtractEditor 컴포넌트 우클릭 → **Enable Editor Mode**
- 또는 enableOnStart ☑ 후 Play 재시작

### 모드 토글 (M 키)
| Mode | 동작 |
|---|---|
| **Rectangle** | 좌클릭 드래그 두 모서리 → 사각형. 떼면 즉시 SO 저장 |
| **Polyline** | 좌클릭으로 점 추가 → Enter 로 확정 (강·해협 모양). 마우스 휠로 폭 조절 |

### 카메라
| 동작 | 효과 |
|---|---|
| **우클릭 드래그** | 팬 (자유 이동) |
| **마우스 휠** (작업 중 아닐 때) | 줌 (Y 높이 변화) |
| **마우스 휠** (폴리라인 작성 중) | 폴리라인 폭 조절 (10~500km) |

### 기존 영역 편집
- 외곽선 클릭 → **선택** (초록색으로 변함)
- **Delete** 키 → SO 에셋 삭제 + 시각 제거
- 현재 위치 수정은 SO 직접 편집 또는 삭제 → 재드로우

### 기타 단축키
| 키 | 효과 |
|---|---|
| **M** | Mode 토글 |
| **B** | Re-bake — 메쉬 즉시 재구성 + 씬 갱신 (몇 초 걸림) |
| **Enter** | 폴리라인 확정 |
| **Esc** | 작성 중인 폴리라인/사각형 취소 |
| **Del** | 선택된 영역 삭제 |

### 비활성화
- 컴포넌트 우클릭 → **Disable Editor Mode**

---

## C. 작업 흐름 예시

### 예: 동남아시아 섬 제거
1. 우클릭 드래그로 카메라를 인도네시아·말레이시아 쪽으로 이동
2. 마우스 휠로 줌인 — 군도 보임
3. **M** → Rectangle 모드 확인
4. 보르네오 동쪽 작은 섬 위를 좌클릭 드래그로 박스 → 떼기 → 저장됨
5. 같은 방식으로 술라웨시 북쪽, 필리핀 작은 섬들 등 반복
6. **B** → Re-bake → 메쉬 갱신 (5~10초)
7. 카메라 다시 줌아웃해서 결과 확인

### 예: 강 만들기 (아프리카 가상 운하)
1. **M** → Polyline 모드로 토글
2. 지중해 쪽 시작점 좌클릭
3. 아프리카 내륙으로 클릭 클릭 클릭...
4. 마우스 휠로 폭 조절 (50~100km 추천)
5. **Enter** → 확정 + 저장
6. **B** → Re-bake → 강이 메쉬에 구멍으로 표현됨

---

## D. 데이터 구조

### `MapSubtractData.cs` (SO 한 개 = 한 영역)
| 필드 | 의미 |
|---|---|
| `subtractId` | 코드 참조용 안정적 id |
| `displayNameKo` | 한글 표시 이름 (예: "나일강") |
| `widthKm` | **0 = 폴리곤 모드** (points 가 정점). **>0 = 폴리라인 모드** (점들을 잇는 띠) |
| `points` | `Vector2[]` — x=longitude, y=latitude. PortData 와 같은 관례 |
| `enabled` | ☐ 면 베이크 무시. 임시 비활성 |
| `notes` | 자유 메모 |

### `MapSubtractCatalog.cs`
- `all: MapSubtractData[]` 모든 영역 리스트
- M3WorldMeshBaker 가 `_Catalogs/MapSubtractCatalog.asset` 를 자동 로드
- 새 SO 추가 시 `Game ▸ Refresh All Catalogs` 가 자동 채움 (또는 에디터가 즉시 추가)

---

## E. 베이크 동작

`Game ▸ Bake World Land Mesh from GeoJSON`:
1. ne_110m_land.geojson 읽음 (기존)
2. **MapSubtractCatalog 의 활성 영역 로드** (신규)
3. 폴리라인 → 사각 띠 변환 (각 세그먼트 = 사각형)
4. 각 NE 폴리곤 삼각화 후 — 삼각형 centroid 가 subtract 영역 안에 있으면 **삭제**
5. Side wall 도 같은 방식으로 필터
6. 결과 메쉬 → `WorldLand.mesh` (in-place 갱신, prefab 자동 반영)

### 한계
- **Cut edge 가 약간 jagged**: 삼각형 단위로 자르므로 cut 경계가 삼각형 모서리에 맞춰짐. MaxEdgeWorldUnits = 200 으로 작아서 거의 안 보임 (육안 1~2 픽셀)
- **시각만 — 콜라이더 별도**: MeshCollider 도 같이 갱신되지만 **PhysX 캐시** 때문에 첫 베이크 후 Play 모드에서 즉시 안 먹힘. Re-bake 버튼 (B 키) 이 sharedMesh 재할당으로 PhysX 강제 갱신

---

## F. 문제 해결

| 증상 | 원인 | 해결 |
|---|---|---|
| 영역 그렸는데 메쉬가 안 바뀜 | Re-bake 안 함 | **B** 키 또는 컴포넌트 우클릭 Re-bake World Land |
| Re-bake 후에도 배가 안 통과 | MeshCollider PhysX 캐시 | Re-bake 가 sharedMesh 재할당으로 갱신해야 함. 안 되면 Play 종료 → 재시작 |
| 영역 라벨 안 보임 | TMP 폰트 미할당 | Inspector Label Font |
| 카탈로그가 비어있다고 함 | 시드 안 함 | `Game ▸ Seed M10 Map Subtracts` |
| 영역 점이 0개로 저장됨 | 폴리라인 1점만 찍고 Enter | 폴리라인은 2점 이상 필요 |
| Re-bake 가 너무 느림 | 50+ 개 subtract | 일부 enabled = ☐ 처리 또는 큰 영역으로 통합 |

---

## G. 향후 확장 아이디어

- **선택한 영역 드래그 이동** (현재는 삭제 → 재드로우만 가능)
- **폴리곤 모드에서 점 추가/제거** (현재는 사각형 4점 고정)
- **카브 영역 표시 토글** (Inspector 옵션) — 시각 확인용 / 게임 플레이용 분리
- **Auto re-bake** (영역 추가 즉시 백그라운드 베이크) — 현재는 명시적 B 키
