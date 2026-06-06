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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M3NpcSeeder] 완료. NPC 6명 + 캐릭터 6명 추가:\n" +
                "  해적: 검은수염, 은빛 잭, 선장 마야, 붉은 왜구\n" +
                "  상선: 상인 마르코\n" +
                "  호위선: 호위병 한스\n" +
                "\n다음 단계: Game ▸ Refresh All Catalogs → NpcCatalog 자동 갱신.");
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
