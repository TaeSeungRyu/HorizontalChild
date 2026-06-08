using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M3 — NPC 풀 시드. 6명 (해적 4 / 상선 1 / 호위선 1) + 해당 캐릭터 + NpcDefinition.
    ///
    /// 메뉴: Game/Seed M3 NPCs
    ///
    /// 시드 후 Game ▸ Refresh All Catalogs 실행 권장.
    /// </summary>
    public static class M3NpcSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M3 NPCs")]
        public static void SeedM3Npcs()
        {
            EnsureFolder($"{DataRoot}/Characters");
            EnsureFolder($"{DataRoot}/Npcs");

            // ─── 해적 4명 (Pirate) ────────────────────────────────────────
            var blackbeard = CreateOrLoadCharacter("Char_Blackbeard.asset", c =>
            {
                c.characterId = "char.blackbeard";
                c.displayNameKo = "검은수염";
                c.gender = Gender.Male;
                c.role = CharacterRole.Adventurer;
                c.bravery = 85;
                c.seamanship = 60;
                c.keenEye = 40;
            });
            var silverJack = CreateOrLoadCharacter("Char_SilverJack.asset", c =>
            {
                c.characterId = "char.silver_jack";
                c.displayNameKo = "은빛 잭";
                c.gender = Gender.Male;
                c.role = CharacterRole.Adventurer;
                c.bravery = 70;
                c.seamanship = 75;
                c.keenEye = 55;
            });
            var captainMaya = CreateOrLoadCharacter("Char_CaptainMaya.asset", c =>
            {
                c.characterId = "char.captain_maya";
                c.displayNameKo = "선장 마야";
                c.gender = Gender.Female;
                c.role = CharacterRole.Adventurer;
                c.bravery = 75;
                c.seamanship = 70;
                c.keenEye = 65;
            });
            var wokouRed = CreateOrLoadCharacter("Char_WokouRed.asset", c =>
            {
                c.characterId = "char.wokou_red";
                c.displayNameKo = "붉은 왜구";
                c.gender = Gender.Male;
                c.role = CharacterRole.Adventurer;
                c.bravery = 65;
                c.seamanship = 80;
                c.keenEye = 50;
            });

            // ─── 상선 1명 (Merchant) ──────────────────────────────────────
            var merchantMarco = CreateOrLoadCharacter("Char_MerchantMarco.asset", c =>
            {
                c.characterId = "char.merchant_marco";
                c.displayNameKo = "상인 마르코";
                c.gender = Gender.Male;
                c.role = CharacterRole.Townsperson;
                c.bravery = 30;
                c.seamanship = 55;
                c.keenEye = 75;
            });

            // ─── 호위선 1명 (Escort) ───────────────────────────────────────
            var escortHans = CreateOrLoadCharacter("Char_EscortHans.asset", c =>
            {
                c.characterId = "char.escort_hans";
                c.displayNameKo = "호위병 한스";
                c.gender = Gender.Male;
                c.role = CharacterRole.Adventurer;
                c.bravery = 60;
                c.seamanship = 65;
                c.keenEye = 60;
            });

            // ─── 추가 NPC — 모든 시작 항구 근처에 상선·호위선 다양화 ─────────
            var merchantSofia = CreateOrLoadCharacter("Char_MerchantSofia.asset", c =>
            {
                c.characterId = "char.merchant_sofia";
                c.displayNameKo = "상인 소피아";
                c.gender = Gender.Female;
                c.role = CharacterRole.Townsperson;
                c.bravery = 35; c.seamanship = 60; c.keenEye = 70;
            });
            var merchantWang = CreateOrLoadCharacter("Char_MerchantWang.asset", c =>
            {
                c.characterId = "char.merchant_wang";
                c.displayNameKo = "상인 왕";
                c.gender = Gender.Male;
                c.role = CharacterRole.Townsperson;
                c.bravery = 30; c.seamanship = 65; c.keenEye = 80;
            });
            var merchantOmar = CreateOrLoadCharacter("Char_MerchantOmar.asset", c =>
            {
                c.characterId = "char.merchant_omar";
                c.displayNameKo = "상인 오마르";
                c.gender = Gender.Male;
                c.role = CharacterRole.Townsperson;
                c.bravery = 40; c.seamanship = 55; c.keenEye = 75;
            });
            var escortDrake = CreateOrLoadCharacter("Char_EscortDrake.asset", c =>
            {
                c.characterId = "char.escort_drake";
                c.displayNameKo = "호위병 드레이크";
                c.gender = Gender.Male;
                c.role = CharacterRole.Adventurer;
                c.bravery = 70; c.seamanship = 70; c.keenEye = 60;
            });
            var escortKim = CreateOrLoadCharacter("Char_EscortKim.asset", c =>
            {
                c.characterId = "char.escort_kim";
                c.displayNameKo = "호위병 김 별군관";
                c.gender = Gender.Male;
                c.role = CharacterRole.Adventurer;
                c.bravery = 65; c.seamanship = 75; c.keenEye = 60;
            });
            var pirateMurad = CreateOrLoadCharacter("Char_PirateMurad.asset", c =>
            {
                c.characterId = "char.pirate_murad";
                c.displayNameKo = "바르바리 무라드";
                c.gender = Gender.Male;
                c.role = CharacterRole.Adventurer;
                c.bravery = 80; c.seamanship = 70; c.keenEye = 55;
            });

            // ─── NpcDefinition ────────────────────────────────────────────
            // homePort 는 선택 — 활동 반경 기준. 없으면 spawner 가 무작위 위치 배치.
            var lisbon = LoadPort("Port_Lisbon");
            var ceuta = LoadPort("Port_Ceuta");
            var venezia = LoadPort("Port_Venezia");
            var istanbul = LoadPort("Port_Istanbul");
            var busan = LoadPort("Port_Busan");
            var amsterdam = LoadPort("Port_Amsterdam");

            CreateOrLoadNpc("Npc_Blackbeard.asset", blackbeard, NpcType.Pirate, ceuta);
            CreateOrLoadNpc("Npc_SilverJack.asset", silverJack, NpcType.Pirate, lisbon);
            CreateOrLoadNpc("Npc_CaptainMaya.asset", captainMaya, NpcType.Pirate, istanbul);
            CreateOrLoadNpc("Npc_WokouRed.asset", wokouRed, NpcType.Pirate, busan);
            CreateOrLoadNpc("Npc_MerchantMarco.asset", merchantMarco, NpcType.Merchant, venezia);
            CreateOrLoadNpc("Npc_EscortHans.asset", escortHans, NpcType.Escort, amsterdam);

            // 추가 NPC 6명 — 시작 항구 다양한 곳에 상선·호위선 배치
            var sevilla = LoadPort("Port_Sevilla");
            var london = LoadPort("Port_London");
            var guangzhou = LoadPort("Port_Guangzhou");
            CreateOrLoadNpc("Npc_MerchantSofia.asset", merchantSofia, NpcType.Merchant, lisbon);
            CreateOrLoadNpc("Npc_MerchantOmar.asset", merchantOmar, NpcType.Merchant, istanbul);
            CreateOrLoadNpc("Npc_MerchantWang.asset", merchantWang, NpcType.Merchant, guangzhou);
            CreateOrLoadNpc("Npc_EscortDrake.asset", escortDrake, NpcType.Escort, london);
            CreateOrLoadNpc("Npc_EscortKim.asset", escortKim, NpcType.Escort, busan);
            CreateOrLoadNpc("Npc_PirateMurad.asset", pirateMurad, NpcType.Pirate, sevilla);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M3NpcSeeder] 완료. NPC 12명 + 캐릭터 12명:\n" +
                "  해적 5: 검은수염(세우타), 은빛 잭(리스본), 선장 마야(이스탄불), 붉은 왜구(부산), 바르바리 무라드(세비야)\n" +
                "  상선 4: 마르코(베네치아), 소피아(리스본), 오마르(이스탄불), 왕(광저우)\n" +
                "  호위선 3: 한스(암스테르담), 드레이크(런던), 김 별군관(부산)\n" +
                "\n다음 단계: Game ▸ Refresh All Catalogs → NpcCatalog 자동 갱신.\n" +
                "  → NpcSpawner 의 Spawn Count 를 12 이상으로 조정 필요.");
        }

        // ─── 헬퍼 ───────────────────────────────────────────────────────────

        private static PortData LoadPort(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<PortData>($"{DataRoot}/Ports/{fileName}.asset");
        }

        private static CharacterData CreateOrLoadCharacter(string fileName, Action<CharacterData> setup)
        {
            var path = $"{DataRoot}/Characters/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<CharacterData>();
            setup(so);
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static NpcDefinition CreateOrLoadNpc(string fileName, CharacterData character, NpcType type, PortData homePort)
        {
            var path = $"{DataRoot}/Npcs/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<NpcDefinition>(path);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<NpcDefinition>();
            so.character = character;
            so.type = type;
            so.homePort = homePort;
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
