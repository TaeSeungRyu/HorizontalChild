using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M9 — 중동 5 + 미국 서부 3. 항구 75 → 83.
    ///
    /// 신규 특산물 2: 레바논삼나무, 해달가죽
    /// (유향·몰약·대추야자는 이전 milestone 에서 이미 추가됨 — 재사용)
    ///
    /// 메뉴: Game/Seed M9 Ports
    /// 기존 .asset 은 건드리지 않음. 시드 후 Game ▸ Refresh All Catalogs 실행 필요.
    /// </summary>
    public static class M9PortsSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M9 Ports")]
        public static void SeedM9Ports()
        {
            EnsureFolders();

            // ─── 기존 특산물 로드 ─────────────────────────────────────────────
            var coffee       = LoadProduct("Product_Coffee.asset");
            var pearl        = LoadProduct("Product_Pearl.asset");
            var silk         = LoadProduct("Product_Silk.asset");
            var persianRug   = LoadProduct("Product_PersianCarpet.asset");
            var silver       = LoadProduct("Product_Silver.asset");
            var gold         = LoadProduct("Product_Gold.asset");
            var leather      = LoadProduct("Product_Leather.asset");
            var fur          = LoadProduct("Product_Fur.asset");
            var whaleOil     = LoadProduct("Product_WhaleOil.asset");
            var cotton       = LoadProduct("Product_Cotton.asset");
            var frankincense = LoadProduct("Product_Frankincense.asset");
            var myrrh        = LoadProduct("Product_Myrrh.asset");
            var dates        = LoadProduct("Product_Dates.asset");

            // ─── 신규 특산물 2 ────────────────────────────────────────────────
            var lebaneseCedar = CreateProduct("Product_LebaneseCedar.asset", "product.lebanese_cedar", "레바논삼나무", 230,
                "수천 년 된 큰 삼나무. 향기롭고 단단해서 옛 신전과 배를 지을 때 썼어요.",
                "https://ko.wikipedia.org/wiki/레바논_삼나무");

            var seaOtter = CreateProduct("Product_SeaOtter.asset", "product.sea_otter", "해달가죽", 240,
                "북태평양의 바다 수달 가죽. 부드럽고 따뜻해서 한겨울 외투로 최고예요.",
                "https://ko.wikipedia.org/wiki/해달");

            // ─── 중동 5 ───────────────────────────────────────────────────────
            CreateOrLoadPort("Port_Muscat.asset", p =>
            {
                p.portId = "port.muscat";
                p.displayNameKo = "무스카트";
                p.displayNameOriginal = "Muscat";
                p.latitude = 23.61f;
                p.longitude = 58.59f;
                p.shortDescription = "아라비아 반도 동쪽 끝, 진주잡이 배가 가득한 오만의 항구.";
                p.commonProducts = new[] { pearl, frankincense, dates };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/무스카트";
            });

            CreateOrLoadPort("Port_Basra.asset", p =>
            {
                p.portId = "port.basra";
                p.displayNameKo = "바스라";
                p.displayNameOriginal = "Basra";
                p.latitude = 30.50f;
                p.longitude = 47.79f;
                p.shortDescription = "티그리스·유프라테스 두 강이 합쳐지는 메소포타미아의 옛 무역항.";
                p.commonProducts = new[] { dates, persianRug, silk };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/바스라";
            });

            CreateOrLoadPort("Port_Jeddah.asset", p =>
            {
                p.portId = "port.jeddah";
                p.displayNameKo = "지다";
                p.displayNameOriginal = "Jeddah";
                p.latitude = 21.49f;
                p.longitude = 39.19f;
                p.shortDescription = "메카 순례자들이 모이는 홍해의 큰 항구.";
                p.commonProducts = new[] { frankincense, myrrh, pearl };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/지다";
            });

            CreateOrLoadPort("Port_Mocha.asset", p =>
            {
                p.portId = "port.mocha";
                p.displayNameKo = "모카";
                p.displayNameOriginal = "Mocha";
                p.latitude = 13.32f;
                p.longitude = 43.24f;
                p.shortDescription = "예멘의 작은 항구. 세상에서 가장 향긋한 커피의 고향이에요.";
                p.commonProducts = new[] { coffee, myrrh, frankincense };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/모카";
            });

            CreateOrLoadPort("Port_Beirut.asset", p =>
            {
                p.portId = "port.beirut";
                p.displayNameKo = "베이루트";
                p.displayNameOriginal = "Beirut";
                p.latitude = 33.89f;
                p.longitude = 35.50f;
                p.shortDescription = "지중해 동쪽 해안의 페니키아 옛 도시. 거대한 삼나무가 항구로 실려와요.";
                p.commonProducts = new[] { lebaneseCedar, silk, leather };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/베이루트";
            });

            // ─── 미국 서부 3 ──────────────────────────────────────────────────
            CreateOrLoadPort("Port_SanFrancisco.asset", p =>
            {
                p.portId = "port.san_francisco";
                p.displayNameKo = "샌프란시스코";
                p.displayNameOriginal = "San Francisco";
                p.latitude = 37.77f;
                p.longitude = -122.42f;
                p.shortDescription = "캘리포니아의 큰 만에 자리잡은 황금을 캐는 사람들의 항구.";
                p.commonProducts = new[] { gold, fur, leather };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/샌프란시스코";
            });

            CreateOrLoadPort("Port_Monterey.asset", p =>
            {
                p.portId = "port.monterey";
                p.displayNameKo = "몬테레이";
                p.displayNameOriginal = "Monterey";
                p.latitude = 36.60f;
                p.longitude = -121.89f;
                p.shortDescription = "스페인이 캘리포니아에 세운 수도. 선교사와 카우보이가 오가는 항구.";
                p.commonProducts = new[] { silver, leather, cotton };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/몬테레이_(캘리포니아주)";
            });

            CreateOrLoadPort("Port_NootkaSound.asset", p =>
            {
                p.portId = "port.nootka_sound";
                p.displayNameKo = "누트카 사운드";
                p.displayNameOriginal = "Nootka Sound";
                p.latitude = 49.60f;
                p.longitude = -126.62f;
                p.shortDescription = "북태평양의 깊은 만. 해달 가죽을 노리는 모피 사냥꾼들의 거점.";
                p.commonProducts = new[] { seaOtter, whaleOil, fur };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/누트카_사운드";
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M9PortsSeeder] 완료.\n" +
                "  • 항구 8개 추가 (총 83)\n" +
                "  • 특산물 2개 신규 (레바논삼나무·해달가죽)\n" +
                "지역: 중동(5) · 미국 서부(3)\n" +
                "\n다음:\n" +
                "  1) Game ▸ Refresh All Catalogs → PortCatalog · ProductCatalog 자동 채움\n" +
                "  2) Game ▸ Seed M8 Missions (Auto) → 새 항구에도 미션 자동 발급");
        }

        // ─── 헬퍼 ───────────────────────────────────────────────────────────

        private static ProductData CreateProduct(string fileName, string id, string name, int price,
            string desc, string url)
        {
            var path = $"{DataRoot}/Products/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<ProductData>(path);
            if (existing != null) return existing;
            var so = ScriptableObject.CreateInstance<ProductData>();
            so.productId = id;
            so.displayNameKo = name;
            so.basePrice = price;
            so.shortDescription = desc;
            so.sourceUrl = url;
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static ProductData LoadProduct(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<ProductData>($"{DataRoot}/Products/{fileName}");
        }

        private static PortData CreateOrLoadPort(string fileName, Action<PortData> setup)
        {
            var path = $"{DataRoot}/Ports/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<PortData>(path);
            if (existing != null) return existing;
            var so = ScriptableObject.CreateInstance<PortData>();
            setup(so);
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static void EnsureFolders()
        {
            EnsureFolder($"{DataRoot}/Ports");
            EnsureFolder($"{DataRoot}/Products");
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
