using System;
using System.Collections.Generic;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M3.3 — 지역(Region) 시드.
    /// 기존 Region_Iberia 1개에 더해 5개 추가, 모든 항구·발견물·국가를 지역에 묶어 분류.
    ///
    /// 메뉴: Game/Seed M3 Regions
    ///
    /// 6개 지역:
    ///   1. Iberia (이베리아 반도) — Portugal, Spain
    ///   2. Mediterranean (지중해 서부) — Italy
    ///   3. NorthSea (북해) — Netherlands, England
    ///   4. EasternMediterranean (동지중해 + 이집트) — Ottoman
    ///   5. KoreaSea (조선 해역) — Joseon
    ///   6. ChinaSea (중국 해역) — China
    ///
    /// 이미 존재하면 ports/discoveries/unlockedAtStartFor 만 갱신 (참조 보존).
    /// 시드 후 `Game ▸ Refresh All Catalogs` 실행 권장.
    /// </summary>
    public static class M3RegionsSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M3 Regions")]
        public static void SeedM3Regions()
        {
            EnsureFolder($"{DataRoot}/Regions");

            // ─── 국가 로드 ────────────────────────────────────────────────
            var portugal    = LoadNation("Nation_Portugal");
            var spain       = LoadNation("Nation_Spain");
            var italy       = LoadNation("Nation_Italy");
            var netherlands = LoadNation("Nation_Netherlands");
            var england     = LoadNation("Nation_England");
            var ottoman     = LoadNation("Nation_Ottoman");
            var joseon      = LoadNation("Nation_Joseon");
            var china       = LoadNation("Nation_China");

            // ─── 1. Iberia ────────────────────────────────────────────────
            CreateOrUpdateRegion("Region_Iberia.asset", r =>
            {
                r.regionId = "region.iberia";
                r.displayNameKo = "이베리아 반도";
                r.ports = LoadPorts("Port_Lisbon", "Port_Porto", "Port_Funchal", "Port_Ceuta",
                    "Port_Sevilla", "Port_Malaga");
                r.discoveries = LoadDiscoveries(
                    "Discovery_GibraltarStrait", "Discovery_CapeBojador", "Discovery_CanaryIslands",
                    "Discovery_SagresSchool", "Discovery_ColumbusDeparture", "Discovery_IberianLynx");
                r.unlockedAtStartFor = NonNullArray(portugal, spain);
                r.unlockHint = "포르투갈·스페인 국적 선택 시 시작부터 해제.";
            });

            // ─── 2. Mediterranean ─────────────────────────────────────────
            CreateOrUpdateRegion("Region_Mediterranean.asset", r =>
            {
                r.regionId = "region.mediterranean";
                r.displayNameKo = "지중해 서부";
                r.ports = LoadPorts("Port_Venezia", "Port_Genova", "Port_Napoli", "Port_Barcelona");
                r.discoveries = LoadDiscoveries(
                    "Discovery_BlueGrotto", "Discovery_Pompeii", "Discovery_CarthageRuins");
                r.unlockedAtStartFor = NonNullArray(italy);
                r.unlockHint = "이탈리아 국적 선택 시 시작부터 해제 / 다른 국가는 항구 방문 시 해제.";
            });

            // ─── 3. NorthSea ─────────────────────────────────────────────
            CreateOrUpdateRegion("Region_NorthSea.asset", r =>
            {
                r.regionId = "region.north_sea";
                r.displayNameKo = "북해";
                r.ports = LoadPorts(
                    "Port_Amsterdam", "Port_Rotterdam", "Port_Antwerpen",
                    "Port_London", "Port_Plymouth", "Port_Bristol");
                r.discoveries = LoadDiscoveries(
                    "Discovery_TexelSeals", "Discovery_WhiteCliffsDover", "Discovery_AtlanticPuffin");
                r.unlockedAtStartFor = NonNullArray(netherlands, england);
                r.unlockHint = "네덜란드·영국 국적 선택 시 시작부터 해제.";
            });

            // ─── 4. EasternMediterranean ──────────────────────────────────
            CreateOrUpdateRegion("Region_EasternMediterranean.asset", r =>
            {
                r.regionId = "region.eastern_mediterranean";
                r.displayNameKo = "동지중해와 이집트";
                r.ports = LoadPorts("Port_Istanbul", "Port_Izmir", "Port_Alexandria");
                r.discoveries = LoadDiscoveries(
                    "Discovery_BosphorusStrait", "Discovery_Ephesus",
                    "Discovery_LighthouseAlexandria", "Discovery_PyramidsGiza",
                    "Discovery_FallConstantinople", "Discovery_NileCrocodile");
                r.unlockedAtStartFor = NonNullArray(ottoman);
                r.unlockHint = "오스만 국적 선택 시 시작부터 해제.";
            });

            // ─── 5. KoreaSea ─────────────────────────────────────────────
            CreateOrUpdateRegion("Region_KoreaSea.asset", r =>
            {
                r.regionId = "region.korea_sea";
                r.displayNameKo = "조선 해역";
                r.ports = LoadPorts("Port_Busan", "Port_Jemulpo", "Port_Mokpo");
                r.discoveries = LoadDiscoveries(
                    "Discovery_Hallasan", "Discovery_KoreanTiger", "Discovery_HansandoBattle");
                r.unlockedAtStartFor = NonNullArray(joseon);
                r.unlockHint = "조선 국적 선택 시 시작부터 해제.";
            });

            // ─── 6. ChinaSea ─────────────────────────────────────────────
            CreateOrUpdateRegion("Region_ChinaSea.asset", r =>
            {
                r.regionId = "region.china_sea";
                r.displayNameKo = "중국 해역";
                r.ports = LoadPorts("Port_Guangzhou", "Port_Quanzhou", "Port_Hangzhou");
                r.discoveries = LoadDiscoveries(
                    "Discovery_PearlRiverDelta", "Discovery_ChineseAlligator",
                    "Discovery_ZhengHe", "Discovery_SouthChinaTiger");
                r.unlockedAtStartFor = NonNullArray(china);
                r.unlockHint = "중국 국적 선택 시 시작부터 해제.";
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M3RegionsSeeder] 완료. 6개 지역 정리:\n" +
                "  • 이베리아 반도 (포르투갈, 스페인)\n" +
                "  • 지중해 서부 (이탈리아)\n" +
                "  • 북해 (네덜란드, 영국)\n" +
                "  • 동지중해와 이집트 (오스만)\n" +
                "  • 조선 해역 (조선)\n" +
                "  • 중국 해역 (중국)\n" +
                "\n다음 단계: Game ▸ Refresh All Catalogs → RegionCatalog 자동 갱신.");
        }

        // ─── 헬퍼 ───────────────────────────────────────────────────────────

        private static NationData LoadNation(string baseName) =>
            AssetDatabase.LoadAssetAtPath<NationData>($"{DataRoot}/Nations/{baseName}.asset");

        private static PortData[] LoadPorts(params string[] baseNames)
        {
            var list = new List<PortData>();
            foreach (var name in baseNames)
            {
                var port = AssetDatabase.LoadAssetAtPath<PortData>($"{DataRoot}/Ports/{name}.asset");
                if (port != null) list.Add(port);
            }
            return list.ToArray();
        }

        private static DiscoveryData[] LoadDiscoveries(params string[] baseNames)
        {
            var list = new List<DiscoveryData>();
            foreach (var name in baseNames)
            {
                var d = AssetDatabase.LoadAssetAtPath<DiscoveryData>(
                    $"{DataRoot}/Discoveries/{name}.asset");
                if (d != null) list.Add(d);
            }
            return list.ToArray();
        }

        private static NationData[] NonNullArray(params NationData[] items)
        {
            var list = new List<NationData>();
            foreach (var item in items)
            {
                if (item != null) list.Add(item);
            }
            return list.ToArray();
        }

        private static void CreateOrUpdateRegion(string fileName, Action<RegionData> setup)
        {
            var path = $"{DataRoot}/Regions/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<RegionData>(path);

            if (existing != null)
            {
                setup(existing);
                EditorUtility.SetDirty(existing);
                return;
            }

            var so = ScriptableObject.CreateInstance<RegionData>();
            setup(so);
            AssetDatabase.CreateAsset(so, path);
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
