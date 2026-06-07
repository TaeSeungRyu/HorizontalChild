using System.Collections.Generic;
using System.Linq;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 4개 카탈로그 (Discovery / Mission / Port / Nation) 를 폴더 스캔으로 자동 채움.
    ///
    /// 사용:
    ///   1. (한 번만) 카탈로그 SO 4개 생성:
    ///      Project 창 우클릭 → Create → Game/Data/Discovery Catalog 등 4번
    ///      경로 권장: Assets/Game/Data/_Catalogs/
    ///   2. 신규 SO 추가 시 메뉴: Game ▸ Refresh All Catalogs
    ///      → 폴더 안의 모든 SO 가 카탈로그의 all 배열에 자동 등록됨
    ///      → 컴포넌트들(SeaWorldManager 등) 은 카탈로그만 참조하므로 자동 갱신
    ///
    /// 메뉴 옵션:
    ///   - Game ▸ Refresh All Catalogs — 4개 모두 갱신
    ///   - Game ▸ Refresh Discovery Catalog 등 개별 갱신
    /// </summary>
    public static class CatalogRefresher
    {
        [MenuItem("Game/Refresh All Catalogs")]
        public static void RefreshAll()
        {
            int total = 0;
            total += RefreshCatalogOfType<DiscoveryCatalog, DiscoveryData>(c => c.all, (c, arr) => c.all = arr);
            total += RefreshCatalogOfType<MissionCatalog, MissionTemplate>(c => c.all, (c, arr) => c.all = arr);
            total += RefreshCatalogOfType<PortCatalog, PortData>(c => c.all, (c, arr) => c.all = arr);
            total += RefreshCatalogOfType<NationCatalog, NationData>(c => c.all, (c, arr) => c.all = arr);
            total += RefreshCatalogOfType<LandmassCatalog, LandmassData>(c => c.all, (c, arr) => c.all = arr);
            total += RefreshCatalogOfType<RegionCatalog, RegionData>(c => c.all, (c, arr) => c.all = arr);
            total += RefreshCatalogOfType<ProductCatalog, ProductData>(c => c.all, (c, arr) => c.all = arr);
            total += RefreshCatalogOfType<NpcCatalog, NpcDefinition>(c => c.all, (c, arr) => c.all = arr);
            total += RefreshCatalogOfType<ShipCatalog, ShipData>(c => c.all, (c, arr) => c.all = arr);
            total += RefreshCatalogOfType<CharacterCatalog, CharacterData>(c => c.all, (c, arr) => c.all = arr);

            AssetDatabase.SaveAssets();
            Debug.Log($"[CatalogRefresher] 완료. 총 {total}개 SO 가 카탈로그에 등록됨.");
        }

        [MenuItem("Game/Refresh Discovery Catalog")]
        public static void RefreshDiscovery() =>
            RunSingle<DiscoveryCatalog, DiscoveryData>(c => c.all, (c, arr) => c.all = arr);

        [MenuItem("Game/Refresh Mission Catalog")]
        public static void RefreshMission() =>
            RunSingle<MissionCatalog, MissionTemplate>(c => c.all, (c, arr) => c.all = arr);

        [MenuItem("Game/Refresh Port Catalog")]
        public static void RefreshPort() =>
            RunSingle<PortCatalog, PortData>(c => c.all, (c, arr) => c.all = arr);

        [MenuItem("Game/Refresh Nation Catalog")]
        public static void RefreshNation() =>
            RunSingle<NationCatalog, NationData>(c => c.all, (c, arr) => c.all = arr);

        [MenuItem("Game/Refresh Landmass Catalog")]
        public static void RefreshLandmass() =>
            RunSingle<LandmassCatalog, LandmassData>(c => c.all, (c, arr) => c.all = arr);

        [MenuItem("Game/Refresh Region Catalog")]
        public static void RefreshRegion() =>
            RunSingle<RegionCatalog, RegionData>(c => c.all, (c, arr) => c.all = arr);

        [MenuItem("Game/Refresh Product Catalog")]
        public static void RefreshProduct() =>
            RunSingle<ProductCatalog, ProductData>(c => c.all, (c, arr) => c.all = arr);

        private static void RunSingle<TCatalog, TItem>(
            System.Func<TCatalog, TItem[]> getter,
            System.Action<TCatalog, TItem[]> setter)
            where TCatalog : ScriptableObject
            where TItem : ScriptableObject
        {
            int n = RefreshCatalogOfType<TCatalog, TItem>(getter, setter);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CatalogRefresher] {typeof(TCatalog).Name} 갱신 완료 — {n}개 항목.");
        }

        /// <summary>
        /// 프로젝트 안의 모든 TCatalog 인스턴스에 대해 — TItem 전부를 찾아 all 배열에 채워줌.
        /// 카탈로그가 없으면 무시 (로그만).
        /// </summary>
        private static int RefreshCatalogOfType<TCatalog, TItem>(
            System.Func<TCatalog, TItem[]> getter,
            System.Action<TCatalog, TItem[]> setter)
            where TCatalog : ScriptableObject
            where TItem : ScriptableObject
        {
            // 1) 모든 카탈로그 인스턴스
            var catalogGuids = AssetDatabase.FindAssets($"t:{typeof(TCatalog).Name}");
            if (catalogGuids.Length == 0)
            {
                Debug.LogWarning(
                    $"[CatalogRefresher] {typeof(TCatalog).Name} 인스턴스 없음. " +
                    $"Project 창 우클릭 → Create → Game/Data 메뉴에서 먼저 생성해 주세요.");
                return 0;
            }

            // 2) 모든 TItem 인스턴스 수집 (TItem 의 상속 카탈로그 자체는 제외)
            var itemGuids = AssetDatabase.FindAssets($"t:{typeof(TItem).Name}");
            var items = new List<TItem>();
            foreach (var guid in itemGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<TItem>(path);
                if (item == null) continue;
                // catalog SO 가 TItem 을 상속하지 않으므로 안전. 단 혹시 모를 동명 타입 방어:
                if (item.GetType() != typeof(TItem)) continue;
                items.Add(item);
            }

            // 안정적 정렬 (asset path 기준)
            items.Sort((a, b) =>
                string.Compare(
                    AssetDatabase.GetAssetPath(a),
                    AssetDatabase.GetAssetPath(b),
                    System.StringComparison.Ordinal));

            // 3) 각 카탈로그 인스턴스에 채움 (보통 1개)
            int count = 0;
            foreach (var catGuid in catalogGuids)
            {
                var catPath = AssetDatabase.GUIDToAssetPath(catGuid);
                var catalog = AssetDatabase.LoadAssetAtPath<TCatalog>(catPath);
                if (catalog == null) continue;

                var arr = items.ToArray();
                setter(catalog, arr);
                EditorUtility.SetDirty(catalog);
                count = arr.Length;
                Debug.Log($"[CatalogRefresher] {catPath} ← {arr.Length}개 {typeof(TItem).Name}");
            }

            return count;
        }
    }
}
