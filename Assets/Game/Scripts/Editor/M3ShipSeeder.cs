using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M3 — 배 풀 확장. M1 의 Caravel 외 4척 추가.
    ///
    /// 메뉴: Game/Seed M3 Ships
    /// 시드 후 Game ▸ Refresh All Catalogs 권장.
    ///
    /// 배 5종:
    ///   1. Caravel (M1, 기본) — 600 G
    ///   2. Carrack — 2500 G (큰 화물선)
    ///   3. Galleon — 6000 G (전투형)
    ///   4. Fluyt — 4000 G (네덜란드, 화물 특화)
    ///   5. Geobukseon — 8000 G (조선 거북선, 명성 필요)
    /// </summary>
    public static class M3ShipSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M3 Ships")]
        public static void SeedM3Ships()
        {
            EnsureFolder($"{DataRoot}/Ships");

            CreateOrLoadShip("Ship_Carrack.asset", s =>
            {
                s.shipId = "ship.carrack";
                s.displayName = "캐릭선";
                s.cannonPower = 6;
                s.speed = 4;
                s.cargoCapacity = 200;
                s.maxDurability = 100;
                s.basePrice = 2500;
                s.shortDescription = "튼튼하고 짐을 많이 실을 수 있는 큰 배예요.";
                s.longDescription = "원래 대형 화물을 옮기던 배인데, 대항해 시대에 멀리 가는 항해에 자주 쓰였답니다.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/캐릭선";
            });

            CreateOrLoadShip("Ship_Galleon.asset", s =>
            {
                s.shipId = "ship.galleon";
                s.displayName = "갈레온선";
                s.cannonPower = 14;
                s.speed = 5;
                s.cargoCapacity = 180;
                s.maxDurability = 150;
                s.basePrice = 6000;
                s.shortDescription = "대포가 많은 큰 전투용 배예요.";
                s.longDescription = "스페인이 신대륙 무역과 함대 호위에 쓰던 강한 배. 무겁지만 든든해요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/갈레온";
            });

            CreateOrLoadShip("Ship_Fluyt.asset", s =>
            {
                s.shipId = "ship.fluyt";
                s.displayName = "플라이트선";
                s.cannonPower = 4;
                s.speed = 6;
                s.cargoCapacity = 350;
                s.maxDurability = 80;
                s.basePrice = 4000;
                s.shortDescription = "네덜란드의 짐을 많이 싣는 빠른 배예요.";
                s.longDescription = "선체가 둥글고 적은 사람으로 운항해서 무역에 유리했어요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/플라이트선";
            });

            CreateOrLoadShip("Ship_Geobukseon.asset", s =>
            {
                s.shipId = "ship.geobukseon";
                s.displayName = "거북선";
                s.cannonPower = 18;
                s.speed = 5;
                s.cargoCapacity = 120;
                s.maxDurability = 180;
                s.basePrice = 8000;
                s.shortDescription = "조선의 거북 모양 철갑선. 단단하고 대포가 많아요.";
                s.longDescription = "이순신 장군이 한산도 대첩에서 활용한 조선의 자랑스러운 배. 등에 가시가 박힌 거북이 모양이에요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/거북선";
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M3ShipSeeder] 완료. 4개 배 추가 (Caravel 은 M1 에서 이미 시드):\n" +
                "  • 캐릭선 (Carrack) — 2,500 G, 화물 많음\n" +
                "  • 갈레온선 — 6,000 G, 대포 많음\n" +
                "  • 플라이트선 — 4,000 G, 화물 최대\n" +
                "  • 거북선 — 8,000 G, 단단하고 강함\n" +
                "\n다음: Game ▸ Refresh All Catalogs → ShipCatalog 자동 채움.");
        }

        private static ShipData CreateOrLoadShip(string fileName, Action<ShipData> setup)
        {
            var path = $"{DataRoot}/Ships/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<ShipData>(path);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<ShipData>();
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
