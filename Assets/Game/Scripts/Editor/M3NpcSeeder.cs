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

            // 추가 항구 로드 — 무역 항로용
            var sevilla = LoadPort("Port_Sevilla");
            var london = LoadPort("Port_London");
            var guangzhou = LoadPort("Port_Guangzhou");
            var alexandria = LoadPort("Port_Alexandria");
            var funchal = LoadPort("Port_Funchal");
            var quanzhou = LoadPort("Port_Quanzhou");

            // 해적 4 (영역 순찰)
            CreateOrLoadPirate("Npc_Blackbeard.asset", "npc.blackbeard", blackbeard, ceuta, range: 180f);
            CreateOrLoadPirate("Npc_SilverJack.asset", "npc.silver_jack", silverJack, lisbon, range: 200f);
            CreateOrLoadPirate("Npc_CaptainMaya.asset", "npc.captain_maya", captainMaya, istanbul, range: 200f);
            CreateOrLoadPirate("Npc_WokouRed.asset", "npc.wokou_red", wokouRed, busan, range: 200f);
            CreateOrLoadPirate("Npc_PirateMurad.asset", "npc.pirate_murad", pirateMurad, sevilla, range: 180f);

            // 상선 4 (왕복 무역 항로)
            CreateOrLoadMerchant("Npc_MerchantMarco.asset", "npc.merchant_marco", merchantMarco, venezia,
                new[] { venezia, istanbul });               // 지중해
            CreateOrLoadMerchant("Npc_MerchantSofia.asset", "npc.merchant_sofia", merchantSofia, lisbon,
                new[] { lisbon, funchal });                  // 마데이라 항로
            CreateOrLoadMerchant("Npc_MerchantOmar.asset", "npc.merchant_omar", merchantOmar, istanbul,
                new[] { istanbul, alexandria });             // 동지중해
            CreateOrLoadMerchant("Npc_MerchantWang.asset", "npc.merchant_wang", merchantWang, guangzhou,
                new[] { guangzhou, quanzhou });              // 중국 해안

            // 호위선 3 (좁은 영역 순찰)
            CreateOrLoadEscort("Npc_EscortHans.asset", "npc.escort_hans", escortHans, amsterdam, range: 120f);
            CreateOrLoadEscort("Npc_EscortDrake.asset", "npc.escort_drake", escortDrake, london, range: 120f);
            CreateOrLoadEscort("Npc_EscortKim.asset", "npc.escort_kim", escortKim, busan, range: 120f);

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

        private static NpcDefinition CreateOrLoadPirate(string fileName, string npcId, CharacterData character,
            PortData homePort, float range)
        {
            return CreateOrUpdateNpc(fileName, n =>
            {
                n.npcId = npcId;
                n.character = character;
                n.type = NpcType.Pirate;
                n.homePort = homePort;
                n.patrolRange = range;
                n.patrolPorts = System.Array.Empty<PortData>();
            });
        }

        private static NpcDefinition CreateOrLoadEscort(string fileName, string npcId, CharacterData character,
            PortData homePort, float range)
        {
            return CreateOrUpdateNpc(fileName, n =>
            {
                n.npcId = npcId;
                n.character = character;
                n.type = NpcType.Escort;
                n.homePort = homePort;
                n.patrolRange = range;
                n.patrolPorts = System.Array.Empty<PortData>();
            });
        }

        private static NpcDefinition CreateOrLoadMerchant(string fileName, string npcId, CharacterData character,
            PortData homePort, PortData[] route)
        {
            return CreateOrUpdateNpc(fileName, n =>
            {
                n.npcId = npcId;
                n.character = character;
                n.type = NpcType.Merchant;
                n.homePort = homePort;
                n.patrolRange = 0f;
                n.patrolPorts = route ?? System.Array.Empty<PortData>();
            });
        }

        /// <summary>이미 존재하면 필드만 갱신 (참조 보존). 없으면 새로 생성.</summary>
        private static NpcDefinition CreateOrUpdateNpc(string fileName, System.Action<NpcDefinition> setup)
        {
            var path = $"{DataRoot}/Npcs/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<NpcDefinition>(path);
            if (existing != null)
            {
                setup(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            var so = ScriptableObject.CreateInstance<NpcDefinition>();
            setup(so);
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
