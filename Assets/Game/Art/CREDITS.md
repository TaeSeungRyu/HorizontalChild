# Assets/Game — 외부 에셋 출처 (Credits)

게임에 사용된 외부 에셋의 **출처·라이선스·저자**를 누적 기록한다.
새 에셋을 추가할 때마다 한 항목씩 추가할 것 (`GAME_PREP.md` §7.3 규칙).

## 3D 모델

### Kenney Pirate Kit
- **출처**: https://kenney.nl/assets/pirate-kit
- **라이선스**: **CC0** (Public Domain — 저작자 표기 의무 없음, 상업/수정 자유)
- **저자**: Kenney (Asbjørn Thirslund)
- **저장 위치**: `Assets/kenney_pirate-kit/`
  - ↳ 추후 `Assets/ThirdParty/kenney_pirate-kit/` 으로 이동 권장 (`GAME_PREP.md` §6 폴더 구조). 이동은 Unity 에디터에서 드래그하여 GUID·메타 보존.
- **사용 항목 (예정)**:
  - 배 모델 9종: `ship-small` / `ship-medium` / `ship-large` / `ship-pirate-small` / `ship-pirate-medium` / `ship-pirate-large` / `ship-ghost` / `ship-wreck` / `boat-row-*`
  - 항구 구조물: `castle-*`, `flag-*`
  - 소품: `barrel`, `crate`, `chest`, `cannon`, `bottle`
- **ShipData 매핑 (잠정)**:
  - `ship.caravel` → `ship-small`
  - `ship.carrack` / `ship.fluyt` → `ship-medium`
  - `ship.galleon` / `ship.galleass` / `ship.bao_chuan` → `ship-large`
  - `ship.pirate_ship` → `ship-pirate-medium`
  - `ship.junk` / `ship.geobukseon` / `ship.dhow` / `ship.galera` → 임시로 `ship-medium` 사용. 추후 동아시아·중동 모델 별도 확보 필요.
- **다운로드 일**: 2026-05-29 이전
- **메모**: CC0 라이선스이므로 표기 의무는 없으나 본 파일에 기록.

## 2D 아이콘 · UI
- (없음 — 추후 Game-icons.net / Kenney UI Pack 추가 시 등록)

## 폰트
- (없음 — Pretendard OFL 도입 시 등록)

## 사운드 · 음악
- (없음 — 1차 마일스톤에서는 도입하지 않음)

## 지도 · 지리 데이터
- (없음 — Natural Earth shaded relief / Wikimedia 고지도 도입 시 등록)

---

## 라이선스 분류별 정리

### CC0 (저작자 표기 의무 없음)
- Kenney Pirate Kit

### CC-BY (저작자 표기 필요)
- (없음)

### OFL (폰트 라이선스)
- (없음)

### Asset Store EULA
- (없음)
