using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 배 풀 시드 — 15종 (다양한 문화권 + 가격대 + 역할).
    ///
    /// 메뉴: Game/Seed M3 Ships
    /// 시드 후 Game ▸ Refresh All Catalogs 권장.
    ///
    /// 기존 에셋이 있으면 덮어쓰지 않음 (인스펙터 튜닝 보존).
    /// </summary>
    public static class M3ShipSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M3 Ships")]
        public static void SeedM3Ships()
        {
            EnsureFolder($"{DataRoot}/Ships");

            // ─── Tier 1 — 입문/저가 (600 ~ 1,500G) ─────────────────────────────
            CreateOrLoadShip("Ship_CaravelaLatina.asset", s =>
            {
                s.shipId = "ship.caravela_latina";
                s.displayName = "라틴 카라벨";
                s.cannonPower = 2; s.speed = 8; s.cargoCapacity = 50; s.maxDurability = 50;
                s.attackInterval = 1.4f;
                s.basePrice = 800;
                s.shortDescription = "삼각 돛이 달린 작고 빠른 탐험선이에요.";
                s.longDescription = "포르투갈 탐험가들이 아프리카 해안을 더듬을 때 즐겨 탔어요. 작아도 바람을 잘 잡아 빨라요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/카라벨";
            });

            CreateOrLoadShip("Ship_Dhow.asset", s =>
            {
                s.shipId = "ship.dhow";
                s.displayName = "다우선";
                s.cannonPower = 2; s.speed = 7; s.cargoCapacity = 80; s.maxDurability = 55;
                s.attackInterval = 1.5f;
                s.basePrice = 1200;
                s.shortDescription = "아라비아 상인들의 빠른 무역선이에요.";
                s.longDescription = "인도양에서 향신료를 옮기던 배. 삼각 돛으로 계절풍을 잘 타고 다녀요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/다우선";
            });

            CreateOrLoadShip("Ship_Cog.asset", s =>
            {
                s.shipId = "ship.cog";
                s.displayName = "코그선";
                s.cannonPower = 3; s.speed = 3; s.cargoCapacity = 250; s.maxDurability = 90;
                s.attackInterval = 2.0f;
                s.basePrice = 1500;
                s.shortDescription = "튼튼하고 짐을 많이 싣는 중세 무역선이에요.";
                s.longDescription = "북유럽 한자동맹 도시들이 즐겨 쓰던 배. 느리지만 듬직해요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/코그";
            });

            // ─── Tier 2 — 중급 무역/탐험 (2,500 ~ 4,000G) ───────────────────────
            CreateOrLoadShip("Ship_Carrack.asset", s =>
            {
                s.shipId = "ship.carrack";
                s.displayName = "캐릭선";
                s.cannonPower = 6; s.speed = 4; s.cargoCapacity = 200; s.maxDurability = 100;
                s.attackInterval = 1.8f;
                s.basePrice = 2500;
                s.shortDescription = "튼튼하고 짐을 많이 실을 수 있는 큰 배예요.";
                s.longDescription = "원래 대형 화물을 옮기던 배인데, 대항해 시대에 멀리 가는 항해에 자주 쓰였답니다.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/캐릭선";
            });

            CreateOrLoadShip("Ship_Galley.asset", s =>
            {
                s.shipId = "ship.galley";
                s.displayName = "갤리선";
                s.cannonPower = 7; s.speed = 7; s.cargoCapacity = 60; s.maxDurability = 70;
                s.attackInterval = 1.4f;
                s.basePrice = 3000;
                s.shortDescription = "노와 돛을 함께 쓰는 빠른 지중해 배예요.";
                s.longDescription = "베네치아·제노바 같은 지중해 도시국가의 함대. 바람이 없어도 노로 움직여요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/갤리선";
            });

            CreateOrLoadShip("Ship_Junk.asset", s =>
            {
                s.shipId = "ship.junk";
                s.displayName = "정크선";
                s.cannonPower = 5; s.speed = 5; s.cargoCapacity = 280; s.maxDurability = 110;
                s.attackInterval = 1.7f;
                s.basePrice = 3500;
                s.shortDescription = "방수 격벽이 들어간 튼튼한 중국 무역선이에요.";
                s.longDescription = "정화 함대가 인도양까지 왕래할 때 탄 배. 큰 화물칸과 좋은 안정성이 자랑이에요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/정크선";
            });

            CreateOrLoadShip("Ship_Fluyt.asset", s =>
            {
                s.shipId = "ship.fluyt";
                s.displayName = "플라이트선";
                s.cannonPower = 4; s.speed = 6; s.cargoCapacity = 350; s.maxDurability = 80;
                s.attackInterval = 2.0f;
                s.basePrice = 4000;
                s.shortDescription = "네덜란드의 짐을 많이 싣는 빠른 배예요.";
                s.longDescription = "선체가 둥글고 적은 사람으로 운항해서 무역에 유리했어요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/플라이트선";
            });

            // ─── Tier 3 — 대형 전투/탐험 (5,000 ~ 6,500G) ───────────────────────
            CreateOrLoadShip("Ship_SantaMaria.asset", s =>
            {
                s.shipId = "ship.santa_maria";
                s.displayName = "산타마리아호";
                s.cannonPower = 7; s.speed = 5; s.cargoCapacity = 180; s.maxDurability = 110;
                s.attackInterval = 1.9f;
                s.basePrice = 5000;
                s.shortDescription = "콜럼버스가 신대륙을 향해 탔던 유명한 배예요.";
                s.longDescription = "1492년 대서양을 건너 아메리카에 도착한 카락선. 탐험과 무역에 두루 적당했어요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/산타_마리아호";
            });

            CreateOrLoadShip("Ship_Galleon.asset", s =>
            {
                s.shipId = "ship.galleon";
                s.displayName = "갈레온선";
                s.cannonPower = 14; s.speed = 5; s.cargoCapacity = 180; s.maxDurability = 150;
                s.attackInterval = 1.7f;
                s.basePrice = 6000;
                s.shortDescription = "대포가 많은 큰 전투용 배예요.";
                s.longDescription = "스페인이 신대륙 무역과 함대 호위에 쓰던 강한 배. 무겁지만 든든해요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/갈레온";
            });

            CreateOrLoadShip("Ship_Galleass.asset", s =>
            {
                s.shipId = "ship.galleass";
                s.displayName = "갤리어스";
                s.cannonPower = 12; s.speed = 6; s.cargoCapacity = 140; s.maxDurability = 130;
                s.attackInterval = 1.6f;
                s.basePrice = 6500;
                s.shortDescription = "갤리선에 대포를 듬뿍 단 큰 군함이에요.";
                s.longDescription = "베네치아가 레판토 해전에서 쓴 묵직한 전선. 빠르고 강해서 무서운 상대였어요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/갈레아스";
            });

            CreateOrLoadShip("Ship_Panokseon.asset", s =>
            {
                s.shipId = "ship.panokseon";
                s.displayName = "판옥선";
                s.cannonPower = 15; s.speed = 4; s.cargoCapacity = 100; s.maxDurability = 160;
                s.attackInterval = 1.7f;
                s.basePrice = 6500;
                s.shortDescription = "조선 수군의 주력 전투선이에요.";
                s.longDescription = "위에 판자로 만든 옥(屋)이 있어 노 젓는 병사를 보호해요. 평저선이라 우리 바다에 잘 어울려요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/판옥선";
            });

            // ─── Tier 4 — 최상급 / 특수 (8,000G+) ───────────────────────────────
            CreateOrLoadShip("Ship_Geobukseon.asset", s =>
            {
                s.shipId = "ship.geobukseon";
                s.displayName = "거북선";
                s.cannonPower = 18; s.speed = 5; s.cargoCapacity = 120; s.maxDurability = 180;
                s.attackInterval = 1.6f;
                s.basePrice = 8000;
                s.shortDescription = "조선의 거북 모양 철갑선. 단단하고 대포가 많아요.";
                s.longDescription = "이순신 장군이 한산도 대첩에서 활용한 조선의 자랑스러운 배. 등에 가시가 박힌 거북이 모양이에요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/거북선";
            });

            CreateOrLoadShip("Ship_Clipper.asset", s =>
            {
                s.shipId = "ship.clipper";
                s.displayName = "클리퍼";
                s.cannonPower = 6; s.speed = 9; s.cargoCapacity = 110; s.maxDurability = 90;
                s.attackInterval = 1.5f;
                s.basePrice = 10000;
                s.shortDescription = "바람을 가르며 가장 빠르게 달리는 배예요.";
                s.longDescription = "긴 선체와 큰 돛으로 차(茶)·향신료 같은 귀한 화물을 빨리 옮기려고 만든 배. 속도 하나는 최고예요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/클리퍼";
            });

            CreateOrLoadShip("Ship_EastIndiaman.asset", s =>
            {
                s.shipId = "ship.east_indiaman";
                s.displayName = "동인도무역선";
                s.cannonPower = 16; s.speed = 5; s.cargoCapacity = 500; s.maxDurability = 170;
                s.attackInterval = 1.7f;
                s.basePrice = 12000;
                s.shortDescription = "큰 화물과 든든한 대포를 함께 갖춘 무역선이에요.";
                s.longDescription = "네덜란드·영국 동인도회사가 인도양·동남아 무역에 띄운 거대 상선. 해적도 만만하게 못 봐요.";
                s.sourceUrl = "https://ko.wikipedia.org/wiki/동인도회사";
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M3ShipSeeder] 완료 — 총 15종 배 시드 (기존 에셋은 보존).\n" +
                "  Tier 1: Caravel · 라틴 카라벨 · 다우선 · 코그선\n" +
                "  Tier 2: 캐릭선 · 갤리선 · 정크선 · 플라이트선\n" +
                "  Tier 3: 산타마리아호 · 갈레온선 · 갤리어스 · 판옥선\n" +
                "  Tier 4: 거북선 · 클리퍼 · 동인도무역선\n" +
                "다음: Game ▸ Refresh All Catalogs → ShipCatalog 자동 채움.");
        }

        private static ShipData CreateOrLoadShip(string fileName, Action<ShipData> setup)
        {
            var path = $"{DataRoot}/Ships/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<ShipData>(path);
            if (existing != null) return existing;   // 기존 에셋 보존 — 인스펙터 튜닝 안 덮어씀

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
