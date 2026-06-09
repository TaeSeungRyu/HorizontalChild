# 배 3D 모델 — 내가 할 일

코드는 이미 준비됨. 아래 작업만 하면 게임에서 자동 반영.

---

## A. 모델링할 때 지킬 규칙

| 항목 | 값 |
|---|---|
| **앞 방향** | +Z (뱃머리가 Z 양의 방향) |
| **위 방향** | +Y |
| **피벗 (Origin)** | 선체 바닥 = Y 0 (수면 높이) |
| **크기** | 약 1 Unity 유닛 (게임에서 자동 6배 확대) |
| **비율** | 길이 1.2 × 폭 0.6 × 높이 0.8 권장 |
| **폴리곤** | 한 배당 < 2000 tri (모바일 타깃) |
| **머티리얼** | 1~2개로 통합 (선체+돛 정도) |
| **콜라이더** | 넣지 말기 (게임이 자동 추가) |

---

## B. Blender 에서 Export

1. 배 완성 → 선체 바닥을 Y=0 에 위치
2. 뱃머리를 +Y 글로벌 방향으로 회전 *(Blender 의 +Y = Unity 의 +Z)*
3. `Ctrl + A → Scale` 로 Scale 정규화
4. `File ▸ Export ▸ FBX (.fbx)` — 옵션:
   - **Forward**: `-Z Forward`
   - **Up**: `Y Up`
   - **Apply Scalings**: `FBX All`
   - **Add Leaf Bones**: 끄기

---

## C. Unity 임포트

1. `.fbx` 파일을 **`Assets/Game/Art/Ships/Models/`** 폴더에 드래그
2. 임포트된 모델 클릭 → Inspector:
   - **Model 탭**: Scale Factor 1 (필요 시 조정), Read/Write Enabled 끄기
   - **Rig 탭**: Animation Type = None
   - **Apply** 클릭
3. 모델이 분홍색이면 → `Window ▸ Rendering ▸ Render Pipeline Converter ▸ Built-in to URP ▸ Material Upgrade`

---

## D. Prefab 만들기

1. 임포트된 모델을 Hierarchy 로 드래그 → Scene 에 인스턴스 생성
2. Transform 위치 (0,0,0), 회전 (0,0,0)
3. Scene Gizmo 로 뱃머리가 **+Z** 인지 확인
4. 방향이 틀리면: 빈 GameObject 부모로 감싸기 → 부모는 그대로 두고 안쪽 모델만 회전
5. Hierarchy 의 GameObject 를 **`Assets/Game/Art/Ships/Prefabs/`** 로 드래그 → Prefab 생성
6. 이름 권장: `Ship_<배이름>_Visual.prefab`

---

## E. ShipData 에셋에 할당

배 1종마다:

1. `Assets/Game/Data/Ships/Ship_<배이름>.asset` 선택
2. Inspector 의 **Prefab 3D** 필드에 위에서 만든 Prefab 드래그
3. Ctrl+S

ShipData 에셋 목록 (15종):
- Ship_Caravel
- Ship_CaravelaLatina
- Ship_Dhow
- Ship_Cog
- Ship_Carrack
- Ship_Galley
- Ship_Junk
- Ship_Fluyt
- Ship_SantaMaria
- Ship_Galleon
- Ship_Galleass
- Ship_Panokseon
- Ship_Geobukseon
- Ship_Clipper
- Ship_EastIndiaman

> prefab3D 미할당 배는 자동으로 절차적 큐브 모양으로 나옴. 점진적으로 채워도 OK.

---

## F. 적용 확인

1. **플레이어 배**: Play → 자동 적용. 옛 큐브 비주얼이 남아 있으면 PlayerShip GameObject 자식에서 수동 삭제.
2. **NPC**: 시드 재실행하면 자동 분배.
   - `Game ▸ Seed M3 NPCs` → 100명 NPC 가 각자 ShipData 할당받음
   - `Game ▸ Refresh All Catalogs` → 카탈로그 갱신
   - Play → 새 모양으로 spawn

---

## G. 검증 체크리스트

Play 후 Hierarchy 에서 NpcShip 자식 펼쳐서 확인:

- [ ] **"ShipVisual"** GameObject 가 생겼는지 (prefab3D 할당 완료 신호)
- [ ] 모델이 분홍색 아닌지
- [ ] 뱃머리가 진행 방향 (NPC 가 가는 방향) 향하는지
- [ ] 크기가 합리적인지 (너무 작거나 크지 않은지)

---

## H. 무료 모델 소스 추천

직접 모델링 안 할 경우:
- **Kenney.nl** — `Pirate Kit` 무료 로우폴리
- **Sketchfab** — "ship low poly" 검색, CC0 / CC-BY 필터
- **Unity Asset Store** — "Stylized Ships", "Low Poly Boats"
- **OpenGameArt.org** — 라이선스 확인 필수

---

## 자주 막히는 부분

| 증상 | 원인 | 해결 |
|---|---|---|
| 분홍색 | URP 미호환 셰이더 | Render Pipeline Converter → Material Upgrade |
| 옆으로 누움 | Export axis 설정 | Blender Export: -Z Forward, Y Up |
| 너무 크/작음 | Scale 매핑 | 임포터 Scale Factor 조정 |
| 옆으로 향함 | 모델의 forward 가 +Z 아님 | 빈 부모 GameObject 로 감싸 회전 보정 |
| NPC 클릭 안 됨 | 모델 안 콜라이더가 root 콜라이더 가림 | 모델 prefab 에서 콜라이더 직접 제거 |

문제 생기면 Prefab Inspector 스크린샷 보내주세요.
