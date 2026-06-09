# 배 3D 모델 에셋 적용 가이드

ShipData 의 `prefab3D` 필드에 3D 모델 프리팹을 할당하면, 플레이어 배와 NPC 배 모두 자동으로 그 모델을 사용합니다. 미할당 시 `ProceduralShipBuilder` 의 큐브 조합 모양으로 fallback.

---

## 1. 모델 작성 요건 (Blender / Maya / etc.)

### 좌표·축
- **+Z 가 배의 전진 방향** (Unity 기본). 모델링할 때 뱃머리가 Z 양의 방향을 향하게.
- **Y 축이 위** (수직).
- **피벗(Origin) 위치**: 수면 높이 (선체 바닥보다 살짝 위 — 게임에서 `transform.position.y = 0` 이면 수면)

### 크기 (Scale)
- 모델 자체 크기는 **약 1 Unity 유닛** 이 표준. NpcSpawner 가 `npcSize`(기본 6) 로 배율 적용 → 실제 화면 크기 ≈ 6 유닛.
- 모델이 너무 크면 (예: 100×100 짜리) Unity 임포터에서 Scale Factor 조정하거나 Blender 에서 정규화 후 export.
- 비율 권장: **길이(Z) × 폭(X) × 높이(Y) ≈ 1.2 × 0.6 × 0.8** (절차적 배와 비슷)

### 메시·재질
- **로우폴리 권장** — 모바일 (Galaxy S23) 타깃. 한 배당 < 2000 tri 권장.
- **재질**: URP Lit 호환. Standard / Built-in 셰이더는 Unity 의 Render Pipeline Converter 로 변환 필요.
- **머티리얼 한두 개** 만 (선체·돛 등). 너무 많으면 드로우콜 늘어남.
- **텍스처 atlas** 가 베스트 — 한 텍스처에 선체·돛·디테일 모두.

### 콜라이더
- **모델 안에 콜라이더 넣지 마세요.** NpcSpawner / ShipController 가 root 에 BoxCollider 추가합니다. 모델 안 콜라이더는 자동으로 비활성화되지만 깔끔하게 빼는 게 좋음.

### 애니메이션
- 정적 모델 OK. 돛 펄럭임 등은 추후 Animator 로 추가 가능.

---

## 2. Blender 에서 작업 후 Export

1. 배 모델 완성 후 **선체 바닥이 Y=0** 에 오도록 위치 조정 (Object → Set Origin → Origin to Geometry or 직접 입력)
2. 뱃머리가 **+Y 글로벌 방향**(Blender 기본) 을 향하게 회전. Blender 의 +Y = Unity 의 +Z 로 자동 매핑.
3. Scale Apply (`Ctrl + A → Scale`) — Transform Scale 1,1,1 으로 정규화.
4. File → Export → FBX (.fbx)
   - **Forward**: `-Z Forward` (Blender 의 -Z = Unity 의 +Z)
   - **Up**: `Y Up`
   - **Apply Scalings**: `FBX All`
   - **Add Leaf Bones**: 끄기 (필요 없음)

> 자동 변환이 헷갈리면 Unity 임포트 후 모델 회전을 확인하고 부모 빈 GameObject 로 감싸서 추가 회전을 줄 수 있음.

---

## 3. Unity 임포트

1. `.fbx` 파일을 `Assets/Game/Art/Ships/Models/` 폴더에 드래그
2. 임포터(Inspector) 설정:
   - **Model 탭**:
     - Scale Factor 보통 1
     - Mesh Compression: Low~Medium
     - Read/Write Enabled: 끄기 (런타임 메쉬 수정 안 함)
   - **Rig 탭**: Animation Type = None (정적 모델인 경우)
   - **Materials 탭**: Material Creation Mode = Standard, Location = Use External Materials (재질 따로 관리 원하면) 또는 In-place
3. **Apply** 클릭

### URP 셰이더 변환 (필요 시)
- 임포트된 머티리얼이 분홍색이면 Built-in 셰이더가 URP 와 호환 안 됨
- `Window ▸ Rendering ▸ Render Pipeline Converter ▸ Built-in to URP ▸ Material Upgrade`

---

## 4. Prefab 만들기

1. `Assets/Game/Art/Ships/Models/` 의 imported 모델을 Hierarchy 로 드래그 → Scene 에 인스턴스 생성
2. 위치를 원점(0,0,0) 으로
3. 보이는 방향 확인 — 뱃머리가 +Z 인지 (Scene Gizmo)
4. 필요하면 부모 빈 GameObject 로 감싸서 회전·스케일 보정
5. Hierarchy 의 GameObject 를 `Assets/Game/Art/Ships/Prefabs/` 로 드래그 → Prefab 생성
6. Prefab 이름은 `Ship_<이름>_Visual.prefab` 권장 (예: `Ship_Caravel_Visual.prefab`)

---

## 5. ShipData 에셋에 할당

1. `Assets/Game/Data/Ships/Ship_Caravel.asset` 같은 ShipData 에셋 선택
2. Inspector 의 **Prefab 3D** 필드에 위에서 만든 Prefab 드래그
3. 저장 (Ctrl+S)

이게 끝. NpcSpawner 와 ShipController 가 자동으로 prefab3D 인스턴스화.

---

## 6. NPC 가 이 모델 쓰게 하기

NPC 가 어떤 ShipData 를 타는지는 `NpcDefinition.shipData` 필드로 결정.

### 자동 (시더 사용)
- `Game ▸ Seed M3 NPCs` 실행 → 시더가 NpcType 별로 적절한 ShipData 풀에서 무작위 할당
  - Pirate: Galleon / Geobukseon / Panokseon / Galleass / Carrack
  - Escort: Galleon / Galleass / Panokseon / Carrack / SantaMaria
  - Merchant: Fluyt / Junk / EastIndiaman / Cog / Dhow / Carrack
- 단, 시더가 기존 NPC 에셋을 삭제하고 새로 만들기 때문에 시드 전 인스펙터 튜닝은 사라짐

### 수동
1. `Assets/Game/Data/Npcs/Npc_XXX.asset` 선택
2. Inspector 의 **Ship Data** 필드에 원하는 ShipData 드래그

---

## 7. 플레이어 배 적용

1. 플레이어가 사용 중인 ShipData (씬의 PlayerShip 의 ShipController.shipData) 에 prefab3D 할당
2. Play → `ShipController.Start()` 에서 `RefreshVisual()` 호출 → 자동 적용
3. 조선소에서 다른 배 구매 시 `ShipyardPanel.OnBuy` 가 `RefreshVisual()` 호출 → 즉시 외형 교체

### 기존 큐브 정리
- 씬 PlayerShip GameObject 의 자식 중 옛 큐브 비주얼이 있으면 수동 삭제 (게임에서 자동 안 함)
- 프리팹 인스턴스라면 Prefab 자체에서 큐브 제거 → 모든 인스턴스에 적용

---

## 8. 검증 체크리스트

- [ ] FBX 임포트 시 회전이 (0,0,0) 인지 (인스펙터 → Transform)
- [ ] 모델이 정원점 기준 합리적 크기인지 (Scale 6 배 곱해도 깨끗하게 보이는지)
- [ ] URP 머티리얼 분홍색 없는지
- [ ] 뱃머리 +Z 방향인지 (NpcShip 이 진행 방향을 transform.forward 로 회전)
- [ ] ShipData.prefab3D 필드에 Prefab 할당됐는지
- [ ] Play 했을 때 NPC root GameObject 아래에 "ShipVisual" 자식 생기는지 (Hierarchy 확인)

---

## 9. 추천 무료 소스

- **Kenney.nl** — `Pirate Kit` 등 게임용 로우폴리 무료
- **Sketchfab** — CC0 / CC-BY 로 필터해서 검색. "ship low poly"
- **Unity Asset Store** — "Stylized Ships", "Low Poly Boats" 등 무료/유료
- **OpenGameArt.org** — 무료 게임 에셋, 라이선스 확인 필수

---

## 10. 트러블슈팅

| 증상 | 원인 | 해결 |
|---|---|---|
| 분홍색 모델 | Built-in 셰이더가 URP 와 호환 X | Render Pipeline Converter 로 Material Upgrade |
| 모델이 옆으로 누워있음 | export 시 axis 설정 잘못 | Blender export: -Z Forward, Y Up |
| 모델이 너무 크거나 작음 | Scale 매핑 잘못 | 임포터 Scale Factor 조정 or Blender 에서 Apply Scale |
| ProceduralBuilder 큐브가 같이 보임 | prefab3D 할당 후에도 NpcSpawner 가 prefab 우선 처리 — 큐브 안 보여야 정상 | 코드 동작 확인 — `def.shipData?.prefab3D != null` 체크 |
| NpcShip 클릭이 안 됨 | Prefab 안 콜라이더가 root BoxCollider 막음 | NpcSpawner 가 자동으로 자식 콜라이더 비활성화. 그래도 문제면 모델 prefab 에서 콜라이더 직접 제거 |
| 배가 진행 방향이 아닌 곳을 봄 | 모델 forward axis 가 +Z 아님 | Prefab 안에 부모 GameObject 한 단계 추가하고 그 안에서 모델 회전 |

문제 생기면 모델 prefab 의 Inspector 스크린샷과 함께 알려주세요.
