# 지형 에디터 — 사용 가이드 (대항해시대 2 풍)

지도에서 바다·땅을 마우스로 칠해서 다듬는 도구. 저장 누르면 메쉬 즉시 갱신.

> ⚠ **Unity Editor Play 모드 전용** — 빌드(.exe/.apk) 에선 저장 부분 동작 안 함.

---

## A. 한 번만 셋업

### 1. 카탈로그 + 시작 카브 시드 (선택)
Unity 메뉴 → `Game ▸ Seed M10 Map Subtracts`
- `Assets/Game/Data/_Catalogs/MapSubtractCatalog.asset` 생성
- 나일강·아마존강 예시 등록

처음부터 빈 상태로 시작하고 싶으면 시드 안 해도 됨 — `MapSubtractCatalog.asset` 만 수동 생성 (Project 창 우클릭 → Create → Game/Data → Map Subtract Catalog).

### 2. MapSubtractEditor GameObject 추가
1. Hierarchy 빈 공간 우클릭 → Create Empty
2. 이름: `TerrainEditor`
3. Add Component → **Map Subtract Editor**

### 3. Inspector 필드 할당

| 필드 | 값 |
|---|---|
| **Catalog** | `_Catalogs/MapSubtractCatalog.asset` 드래그 |
| **Main Camera** | 메인 카메라. 비우면 자동 검색 |
| **Player Ship** | 플레이어 배 (입력 잠금용) |
| **UI Font** | `Art/Pretendard-Regular SDF.asset` 드래그 (한글 라벨 필수) |
| **Brush Km** | 기본 20 (5~200 슬라이더) |
| **Enable On Start** | ☐ 평소엔 끔. Play 후 ContextMenu 로 수동 활성 |

---

## B. 사용법 (4 단계만 기억)

### 1) 활성화
Inspector → MapSubtractEditor 우클릭 → **Enable Editor Mode**
화면 하단에 4개 버튼 [**바다**] [**땅**] [**저장**] [**취소**] 가 나타남.

### 2) 모드 선택
- [**바다**] 클릭 → 파란 강조. 우클릭하면 바다로 만들기 모드.
- [**땅**] 클릭 → 주황 강조. 우클릭하면 땅으로 만들기 모드.
- 같은 버튼 다시 클릭 또는 **Enter** 키 → 모드 해제 (회색).

### 3) 칠하기
**모드가 활성** 일 때:
- 마우스 커서 위치에 노랑 원이 따라다님 (브러시 미리보기)
- **마우스 오른쪽 버튼 클릭** → 그 자리에 20km 원이 추가 (메모리만, 디스크에 안 들어감 아직)
- 연속 클릭 가능 — 클릭할 때마다 원 하나씩

**모드가 해제** 일 때:
- 우클릭 드래그 → 카메라 팬 (지도 이동)
- 마우스 휠 → 카메라 줌

**Smart Undo**:
- 땅 모드에서 기존 바다 카브 위 클릭 → 그 바다 삭제 예약 (회색으로 변함)
- 바다 모드에서 기존 땅 영역 위 클릭 → 그 땅 삭제 예약
- 같은 자리 또 클릭 → 삭제 예약 취소

### 4) 저장
[**저장**] 버튼 클릭 → 한꺼번에:
- 모든 pending 원 → MapSubtractData SO 로 저장
- 모든 삭제 예약 SO → 디스크에서 삭제
- 메쉬 자동 재베이크 (몇 초)
- 씬의 WorldLand 즉시 갱신

[**취소**] → pending 모두 버림. 삭제 예약도 복원. 디스크는 손대지 않음.

---

## C. 키보드 단축키

| 키 | 효과 |
|---|---|
| **Enter** | 모드 해제 (지도 이동 모드로) |
| **`[`** | 브러시 -5km |
| **`]`** | 브러시 +5km |
| **휠** | 카메라 줌 |

---

## D. 시각 색 표

| 색 | 의미 |
|---|---|
| 🔵 파랑 진하게 | 새로 추가한 바다 (pending) |
| 🟠 주황 진하게 | 새로 추가한 땅 (pending) |
| ⚪ 흰색 흐리게 | 기존 저장된 영역 (수정 안 한 것) |
| ⚫ 회색 흐리게 | 삭제 예약된 영역 |
| 🟡 노랑 (커서) | 현재 브러시 미리보기 |

---

## E. 데이터 모델

`MapSubtractData` SO 한 개 = 영역 한 개:

| 필드 | 의미 |
|---|---|
| `kind` | **Sea** = 바다로 (육지 삭제) / **Land** = 땅으로 (새 폴리곤 추가) |
| `widthKm` | 0 = 폴리곤 (points 가 정점) / >0 = 폴리라인 (강 등) |
| `points` | `Vector2[]` — x=longitude, y=latitude |
| `enabled` | ☐ 면 베이크 무시 (일시 비활성) |

에디터는 클릭마다 widthKm=0 인 24각형 원으로 저장.
기존 폴리라인 영역(나일·아마존)은 그대로 유지됨.

---

## F. 베이크 동작

`Game ▸ Bake World Land Mesh from GeoJSON`:
1. NE GeoJSON 폴리곤 로드 (기존)
2. **MapSubtractCatalog 의 Land 영역** → NE 폴리곤 리스트에 합쳐 삼각화 (없던 땅 생성)
3. **Sea 영역** → 삼각형 centroid 가 안에 있으면 제거 (있던 땅 삭제)
4. 결과 메쉬 → `WorldLand.mesh` 갱신
5. 씬의 MeshCollider 자동 새로고침 (저장 버튼이 처리)

순서: Land 추가 → Sea 제거. 같은 자리에 둘 다 있으면 Sea 가 이김 → "지움" 효과.

---

## G. 문제 해결

| 증상 | 해결 |
|---|---|
| 한글 안 보임 | Inspector UI Font 에 한글 SDF 폰트 할당 |
| 버튼 클릭 안 됨 | 씬에 EventSystem 없음 → 에디터가 자동 추가하지만 안 되면 수동 추가 |
| 우클릭이 자꾸 카메라 팬 | 모드가 None 임. [바다] 또는 [땅] 먼저 클릭 |
| 저장 후에도 메쉬 그대로 | Console 에 "베이크 완료" 메시지 확인. 없으면 Game ▸ Bake World Land 수동 |
| 원이 너무 작아서 안 보임 | [ ] 키로 브러시 크게 |
| 잘못 칠한 곳 | 반대 모드로 같은 자리 클릭 (Smart Undo) 또는 [취소] 버튼 |

---

## H. 작업 흐름 예시 — 동남아 작은 섬 정리

1. **Enable Editor Mode**
2. 우클릭 드래그로 동남아 줌인
3. **[바다]** 클릭 (파란 강조)
4. 작은 섬 위에 우클릭 — 파란 원 표시
5. 다른 작은 섬에도 클릭 클릭 — 5~10 곳
6. 잘못 찍었으면 **[땅]** 클릭 → 그 파란 원 위에 클릭 → 취소
7. 만족하면 **[저장]** → 메쉬 갱신
8. **Enter** → 모드 해제 → 카메라 이동하면서 결과 확인
