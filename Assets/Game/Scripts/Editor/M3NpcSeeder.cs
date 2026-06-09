using System.Collections.Generic;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Editor
{
    /// <summary>
    /// M3.5 — NPC 100명 풀세트 시드. (해적 20 / 호위선 40 / 상선 40)
    /// 각 NPC 는 고유 캐릭터 + 본거지 + (상선) 목적지를 가짐.
    /// 항구는 25개 — 한 항구를 여러 NPC 가 본거지로 공유.
    ///
    /// 메뉴: Game/Seed M3 NPCs
    ///
    /// 시드 후 Game ▸ Refresh All Catalogs 권장.
    /// </summary>
    public static class M3NpcSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M3 NPCs")]
        public static void SeedM3Npcs()
        {
            EnsureFolder($"{DataRoot}/Characters");
            EnsureFolder($"{DataRoot}/Npcs");

            // 결정적 시드
            Random.InitState(20260609);

            var allPorts = LoadAllPorts();
            if (allPorts.Count == 0)
            {
                Debug.LogError("[M3NpcSeeder] PortData 에셋이 없어요. 먼저 항구 시드를 실행하세요.");
                return;
            }

            // 기존 NPC / 캐릭터(Char_Npc*) 정리
            CleanOldAssets();

            int created = 0;
            int idx = 0;

            // 해적 20명
            for (int i = 0; i < 20; i++)
            {
                var home = allPorts[Random.Range(0, allPorts.Count)];
                CreateNpc(NpcType.Pirate, home, null, idx++);
                created++;
            }

            // 호위선 40명 — home + destination 사이 왕복 (상선과 동일한 cycle)
            for (int i = 0; i < 40; i++)
            {
                var home = allPorts[Random.Range(0, allPorts.Count)];
                PortData dest = null;
                for (int t = 0; t < 8 && dest == null; t++)
                {
                    var pick = allPorts[Random.Range(0, allPorts.Count)];
                    if (pick != home) dest = pick;
                }
                CreateNpc(NpcType.Escort, home, dest, idx++);
                created++;
            }

            // 상선 40명 — home + destination 왕복
            for (int i = 0; i < 40; i++)
            {
                var home = allPorts[Random.Range(0, allPorts.Count)];
                PortData dest = null;
                for (int t = 0; t < 8 && dest == null; t++)
                {
                    var pick = allPorts[Random.Range(0, allPorts.Count)];
                    if (pick != home) dest = pick;
                }
                CreateNpc(NpcType.Merchant, home, dest, idx++);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[M3NpcSeeder] {created}명 NPC + 캐릭터 생성 완료.\n" +
                "  해적 20 / 호위선 40 / 상선 40\n" +
                "다음 단계: Game ▸ Refresh All Catalogs → NpcCatalog 갱신.\n" +
                "  → NpcSpawner.Spawn Count 를 100 이상 으로 조정.");
        }

        // ─── 핵심 생성 ──────────────────────────────────────────────────────

        private static void CreateNpc(NpcType type, PortData homePort, PortData destinationPort, int idx)
        {
            string typeTag = type switch
            {
                NpcType.Pirate => "Pirate",
                NpcType.Escort => "Escort",
                NpcType.Merchant => "Merchant",
                _ => "Npc",
            };

            var (firstName, familyName) = PickName(idx);
            string displayName = string.IsNullOrEmpty(familyName) ? firstName : $"{familyName} {firstName}";
            string charId = $"char.npc{idx:000}";
            string charFile = $"Char_Npc{idx:000}.asset";

            var character = ScriptableObject.CreateInstance<CharacterData>();
            character.characterId = charId;
            character.displayNameKo = displayName;
            character.gender = (idx % 4 == 0) ? Gender.Female : Gender.Male;
            character.role = CharacterRole.Adventurer;
            var stats = StatsFor(type);
            character.bravery = stats.bravery;
            character.seamanship = stats.seamanship;
            character.keenEye = stats.keenEye;
            AssetDatabase.CreateAsset(character, $"{DataRoot}/Characters/{charFile}");

            var def = ScriptableObject.CreateInstance<NpcDefinition>();
            def.npcId = $"npc.{typeTag.ToLower()}{idx:000}";
            def.character = character;
            def.type = type;
            def.homePort = homePort;
            def.destinationPort = destinationPort;
            def.patrolPorts = System.Array.Empty<PortData>();

            def.patrolRange = type switch
            {
                NpcType.Pirate => 180f,
                NpcType.Escort => 120f,
                _ => 0f,
            };

            var combat = CombatStatsFor(type);
            def.cannonPower = combat.cannonPower;
            def.maxDurability = combat.maxDurability;
            def.attackInterval = combat.attackInterval;

            int basePrice = (character.bravery + character.seamanship + character.keenEye) * 10;
            def.hireBasePrice = type switch
            {
                NpcType.Pirate => (int)(basePrice * 1.2f),
                NpcType.Escort => (int)(basePrice * 1.1f),
                _ => basePrice,
            };
            def.hireBonus = new Vector3Int(0, 0, 0);

            AssetDatabase.CreateAsset(def, $"{DataRoot}/Npcs/Npc_{typeTag}{idx:000}.asset");
        }

        // ─── 능력치 ─────────────────────────────────────────────────────────

        private static (int bravery, int seamanship, int keenEye) StatsFor(NpcType type)
        {
            return type switch
            {
                NpcType.Pirate => (Random.Range(60, 96), Random.Range(50, 81), Random.Range(40, 61)),
                NpcType.Escort => (Random.Range(55, 86), Random.Range(55, 81), Random.Range(50, 71)),
                NpcType.Merchant => (Random.Range(25, 56), Random.Range(45, 71), Random.Range(60, 91)),
                _ => (50, 50, 50),
            };
        }

        private static (int cannonPower, int maxDurability, float attackInterval) CombatStatsFor(NpcType type)
        {
            return type switch
            {
                NpcType.Pirate => (Random.Range(4, 9), Random.Range(35, 61), Random.Range(1.4f, 2.0f)),
                NpcType.Escort => (Random.Range(3, 7), Random.Range(40, 71), Random.Range(1.5f, 2.2f)),
                NpcType.Merchant => (Random.Range(2, 5), Random.Range(30, 51), Random.Range(1.8f, 2.5f)),
                _ => (3, 40, 1.6f),
            };
        }

        // ─── 이름 풀 ────────────────────────────────────────────────────────

        private static readonly string[] FirstNames =
        {
            "마르코","안톤","에르난","디에고","페드로","마누엘","카를로스","후안","호세","미겔",
            "라파엘","파울로","루이스","비센테","산티아고","엔리코","조반니","로렌초","마테오","니콜로",
            "사이드","오마르","알리","유수프","칼리드","함자","무사","이브라힘","무라드","파흐드",
            "왕","리","장","조","천","손","주","오","웡","림",
            "유","민","수민","현우","지호","도윤","서준","주원","건우","시우",
            "한솔","경수","민호","준영","태원","승현","재훈","동현","원진","상우",
            "다비드","올라프","에릭","스벤","요나스","피터","한스","빌헬름","오토","루드",
            "토마스","제임스","리처드","에드워드","윌리엄","조나단","헨리","조지","찰스","로버트",
            "이사벨","마리아","안나","엘레나","소피아","리나","아미라","수잔","엠마","줄리아",
            "수아","서연","지우","하윤","채원","유나","지민","예린","서아","연우",
        };

        private static readonly string[] FamilyNames =
        {
            "데실바","코르테스","곤잘레스","페레이라","마르티네스","로페즈","산체스","가르시아","로드리게즈","에레라",
            "메디치","스포르차","비스콘티","곤차가","말라테스타","오르시니","콜론나","파르네세","피사니","모로시니",
            "이븐","알사이드","빈자이드","엘아민","압달라",
            "왕가","이가","장가","조가","송가",
            "김","이","박","최","정","강","윤","장","신","한",
            "반에이크","드프리스","얀센","드용","바커","드그루트","스미트","피셔",
            "스미스","존스","브라운","데이비스","윌슨","무어","테일러","앤더슨",
        };

        private static (string first, string family) PickName(int idx)
        {
            string first = FirstNames[(idx * 7 + 3) % FirstNames.Length];
            string family = FamilyNames[(idx * 11 + 5) % FamilyNames.Length];
            return (first, family);
        }

        // ─── 헬퍼 ───────────────────────────────────────────────────────────

        private static List<PortData> LoadAllPorts()
        {
            var list = new List<PortData>();
            var guids = AssetDatabase.FindAssets("t:PortData", new[] { $"{DataRoot}/Ports" });
            foreach (var g in guids)
            {
                var p = AssetDatabase.LoadAssetAtPath<PortData>(AssetDatabase.GUIDToAssetPath(g));
                if (p != null) list.Add(p);
            }
            return list;
        }

        private static void CleanOldAssets()
        {
            var npcGuids = AssetDatabase.FindAssets("t:NpcDefinition", new[] { $"{DataRoot}/Npcs" });
            foreach (var g in npcGuids)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(g));
            }

            // Char_Npc* 패턴만 삭제 — 다른 캐릭터(플레이어 기본 등) 보존
            var charGuids = AssetDatabase.FindAssets("t:CharacterData", new[] { $"{DataRoot}/Characters" });
            foreach (var g in charGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var name = Path.GetFileName(path);
                if (name.StartsWith("Char_Npc")) AssetDatabase.DeleteAsset(path);
            }
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
