# 세계지도 텍스처 셋업 가이드 (M3)

Natural Earth Hypsometric 이미지를 SeaPlane 에 매핑해 진짜 세계지도 배경 생성.

기존 LandmassData 큐브(충돌)는 그대로 두고 시각만 추가 — 큐브가 화면을 덮으면 안 보이므로 알파 또는 색상 조정 권장.

---

## 사전 조건

`Assets/Game/Art/Map/HYP_LR_SR_W.tif` 파일이 있어야 함.

(Natural Earth → "Cross Blended Hypsometric Tints with Relief and Water" → Low resolution 다운로드 → 압축 풀어서 .tif 만 위 경로로 복사)

---

## 단계 1 — 메뉴 한 번 실행

1. Unity 메뉴: **`Game ▸ Setup World Map (Sea Plane)`** 클릭
2. Console 확인 — 세 줄 OK 로그:
   - 텍스처 Import 설정 적용됨
   - `Assets/Game/Art/Map/WorldMap.mat` 머티리얼 생성
   - 씬에 `SeaPlane` GameObject 생성 (Scale 540×1×270, Y -0.05)
3. Scene View 에서 갈색·녹색·푸른색 세계지도가 평평하게 깔린 것 확인

---

## 단계 2 — Play 테스트

▶ Play 후:
- 배가 푸른 바다 위에 있는지
- 대륙 윤곽이 자연스럽게 보이는지
- 시작 항구 위치가 지도상 해안과 일치하는지 (이전 큐브 좌표 기준이라 살짝 차이 가능)

---

## 단계 3 (선택) — 큐브 알파 낮추기

기존 갈색 큐브가 지도를 덮어 칠합니다. 두 가지 옵션:

### 옵션 A — 큐브 완전 숨기기 (충돌만 유지)
1. Hierarchy → `LandmassRoot` 펼치기
2. 각 큐브 선택 → MeshRenderer 컴포넌트 ☐ 체크 해제
3. (귀찮으면) LandmassPlacer 코드에서 `renderer.enabled = false` 한 줄 추가

→ 시각은 텍스처가, 충돌은 큐브 BoxCollider 가 담당. **권장.**

### 옵션 B — 큐브 반투명
1. `Assets/Game/Data/Landmasses/*.asset` 각각 클릭
2. `color` 의 알파 값 0.3 ~ 0.5
3. 큐브 머티리얼 Rendering Mode 를 Transparent 로 (현재 Lit shader 가 Opaque 라 작업 필요)

→ 큐브 영역이 살짝 보여 충돌 디버깅에 유리. 코드 작업 필요.

---

## 단계 4 (선택) — 카메라 살짝 기울이기

평평한 지도보다 살짝 비스듬한 게 입체감 있음.

1. Hierarchy → `Main Camera` 선택
2. Inspector → Transform → Rotation X = **25** (0 → 25)
3. CameraFollow 가 부착돼 있으면 그 컴포넌트의 `pitch` 또는 회전 관련 필드 조정

→ 옛 항해도 같은 분위기.

---

## 텍스처 Import 설정 (자동 적용됨, 참고)

`M3WorldMapSeeder` 가 다음을 강제합니다:

| 항목 | 값 | 이유 |
|---|---|---|
| Texture Type | Default | 일반 컬러 텍스처 |
| sRGB | ☑ | 색 정확성 |
| Wrap Mode | Clamp | 가장자리 픽셀이 무한 반복되면 안 됨 |
| Filter Mode | Bilinear | 모바일 표준 |
| Mip Maps | ☑ | 멀리서 깨짐 방지 |
| Read/Write | ☐ | 메모리 절약 |
| Max Size (Desktop) | 2048 | 원본 그대로 쓰면 모바일 무거움 |
| Max Size (Android) | 2048 / ASTC 6×6 | 모바일 압축 |

수동으로 다른 값으로 바꿔야 한다면 Texture 선택 → Inspector 에서 조정 후 Apply.

---

## 자주 발생하는 문제

| 증상 | 해결 |
|---|---|
| 메뉴 클릭 시 "텍스처를 찾을 수 없습니다" | `Assets/Game/Art/Map/HYP_LR_SR_W.tif` 경로 확인 |
| 지도가 분홍색 (Missing Shader) | URP 가 아닌 다른 파이프라인. 머티리얼을 직접 Standard Unlit 등으로 |
| 지도가 안 보임 (검정) | SeaPlane 의 Y 위치가 너무 낮음. -0.05 가 기본. 또는 카메라 Far Clip 이 너무 작음 (1000 이상 권장) |
| 남북이 뒤집힘 | 발생 안 함 — Unity Plane 의 UV 와 equirectangular 가 일치. 만약 뒤집혔다면 SeaPlane 의 Rotation 이 잘못됨 (Y 180°), Reset 후 메뉴 재실행 |
| 항구 위치가 지도상 바다 한가운데 | 정상 — 항구 좌표는 실제 위/경도 (예: 리스본 38.7N 9.1W) 라 이미지의 정확한 도시 위치와 일치. 큐브 영역이 살짝 안 맞을 수도 있음 |
| 모바일에서 멈춤 | Android Max Size 가 4096 이상이면 부담. 2048 유지 |
| 카메라가 잘려 보임 | SeaPlane 이 매우 큼(540×270). Camera Far Clip Plane 1500 이상 권장 |

---

## 정점 수 / 성능

- Plane primitive 는 10×10 = 200 정점. 540 배 스케일이라도 정점 수는 같음.
- 텍스처 메모리: 2048×1024 ASTC 6×6 ≈ 1MB. 모바일 부담 거의 없음.
- 드로콜: SeaPlane 1, Landmass 큐브 12, 항구 9 → 총 22개. 모바일 60fps 여유.

---

## 다음 폴리시 단계 (선택)

| 작업 | 효과 | 시간 |
|---|---|---|
| 노멀맵 추가 (NE 같은 사이트의 Shaded Relief 회색조) | 빛에 따라 산 그림자 | 30분 |
| 옛 항해도 톤 텍스처로 교체 | 콘셉트 강화 | 1시간 |
| Bloom + Vignette 포스트프로세스 | 모바일 친화 톤매핑 | 30분 |
| 미니맵 (RawImage + 같은 텍스처) | 우상단 작은 지도 | 1시간 |
| 대륙 라벨 World Space TMP | "유럽" "아시아" 표시 | 1시간 |
