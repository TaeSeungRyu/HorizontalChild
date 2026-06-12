using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M6 — 항구 55 → 75. 아메리카 5 / 러시아 5 / 동남아 5 / 아프리카 5.
    ///
    /// 추가 특산물 7: 밀, 캐비어, 고래기름, 장뇌, 흑단, 대마, 타르
    ///
    /// 메뉴: Game/Seed M6 Ports
    /// 기존 .asset 은 건드리지 않음. 시드 후 Game ▸ Refresh All Catalogs 실행 필요.
    /// </summary>
    public static class M6PortsSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M6 Ports")]
        public static void SeedM6Ports()
        {
            EnsureFolders();

            // ─── 기존 특산물 로드 ─────────────────────────────────────────────
            var pepper        = LoadProduct("Product_Pepper.asset");
            var spiceClove    = LoadProduct("Product_SpiceClove.asset");
            var cinnamon      = LoadProduct("Product_Cinnamon.asset");
            var sandalwood    = LoadProduct("Product_Sandalwood.asset");
            var sugar         = LoadProduct("Product_Sugar.asset");
            var cotton        = LoadProduct("Product_Cotton.asset");
            var indigo        = LoadProduct("Product_Indigo.asset");
            var tobacco       = LoadProduct("Product_Tobacco.asset");
            var cocoa         = LoadProduct("Product_Cocoa.asset");
            var brazilwood    = LoadProduct("Product_Brazilwood.asset");
            var coffee        = LoadProduct("Product_Coffee.asset");
            var rum           = LoadProduct("Product_Rum.asset");
            var silver        = LoadProduct("Product_Silver.asset");
            var gold          = LoadProduct("Product_Gold.asset");
            var pearl         = LoadProduct("Product_Pearl.asset");
            var fur           = LoadProduct("Product_Fur.asset");
            var fish          = LoadProduct("Product_Codfish.asset");
            var amber         = LoadProduct("Product_Amber.asset");
            var ivory         = LoadProduct("Product_Ivory.asset");
            var silk          = LoadProduct("Product_Silk.asset");
            var persianCarpet = LoadProduct("Product_PersianCarpet.asset");
            var vanilla       = LoadProduct("Product_Vanilla.asset");
            var leather       = LoadProduct("Product_Leather.asset");
            var coral         = LoadProduct("Product_Coral.asset");
            var rice          = LoadProduct("Product_Rice.asset");
            var portWine      = LoadProduct("Product_PortWine.asset");

            // ─── 신규 특산물 7 ────────────────────────────────────────────────
            var wheat = CreateProduct("Product_Wheat.asset", "product.wheat", "밀", 30,
                "황금빛 이삭이 흔들리는 광활한 평원의 곡식이에요.",
                "https://ko.wikipedia.org/wiki/밀");

            var caviar = CreateProduct("Product_Caviar.asset", "product.caviar", "캐비어", 300,
                "철갑상어 알. 카스피해 어부들이 황금처럼 귀하게 다뤄요.",
                "https://ko.wikipedia.org/wiki/캐비어");

            var whaleOil = CreateProduct("Product_WhaleOil.asset", "product.whale_oil", "고래기름", 160,
                "북극 고래에서 짜낸 기름. 등불에 밝게 타요.",
                "https://ko.wikipedia.org/wiki/고래기름");

            var camphor = CreateProduct("Product_Camphor.asset", "product.camphor", "장뇌", 140,
                "녹나무에서 얻는 향과 약 재료. 향긋한 흰 결정이에요.",
                "https://ko.wikipedia.org/wiki/장뇌");

            var ebony = CreateProduct("Product_Ebony.asset", "product.ebony", "흑단", 210,
                "밤처럼 검고 단단한 귀한 나무. 가구·악기를 만들어요.",
                "https://ko.wikipedia.org/wiki/흑단");

            var hemp = CreateProduct("Product_Hemp.asset", "product.hemp", "대마", 45,
                "튼튼한 줄과 돛 천을 만드는 식물 섬유예요.",
                "https://ko.wikipedia.org/wiki/대마");

            var tar = CreateProduct("Product_Tar.asset", "product.tar", "타르", 55,
                "나무를 태워 얻는 끈끈한 검은 액체. 배 바닥에 발라 물을 막아요.",
                "https://ko.wikipedia.org/wiki/타르");

            // ─── 아메리카 5 ───────────────────────────────────────────────────
            CreateOrLoadPort("Port_NewOrleans.asset", p =>
            {
                p.portId = "port.new_orleans";
                p.displayNameKo = "뉴올리언스";
                p.displayNameOriginal = "New Orleans";
                p.latitude = 29.95f;
                p.longitude = -90.07f;
                p.shortDescription = "미시시피 강 어귀에 자리잡은 프랑스의 큰 식민 항구.";
                p.commonProducts = new[] { cotton, sugar, indigo };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/뉴올리언스";
            });

            CreateOrLoadPort("Port_Portobelo.asset", p =>
            {
                p.portId = "port.portobelo";
                p.displayNameKo = "포르토벨로";
                p.displayNameOriginal = "Portobelo";
                p.latitude = 9.55f;
                p.longitude = -79.65f;
                p.shortDescription = "신대륙 은이 한곳에 모이는 파나마의 작은 요새 항구.";
                p.commonProducts = new[] { silver, pearl, cocoa };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/포르토벨로";
            });

            CreateOrLoadPort("Port_Kingston.asset", p =>
            {
                p.portId = "port.kingston";
                p.displayNameKo = "킹스턴";
                p.displayNameOriginal = "Kingston";
                p.latitude = 17.97f;
                p.longitude = -76.79f;
                p.shortDescription = "영국 해적과 상인이 모이던 자메이카의 큰 항구.";
                p.commonProducts = new[] { sugar, rum, tobacco };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/킹스턴_(자메이카)";
            });

            CreateOrLoadPort("Port_Recife.asset", p =>
            {
                p.portId = "port.recife";
                p.displayNameKo = "헤시피";
                p.displayNameOriginal = "Recife";
                p.latitude = -8.05f;
                p.longitude = -34.88f;
                p.shortDescription = "사탕수수 농장이 끝없이 펼쳐진 브라질 동북부의 항구.";
                p.commonProducts = new[] { sugar, cocoa, brazilwood };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/헤시피";
            });

            CreateOrLoadPort("Port_SanJuan.asset", p =>
            {
                p.portId = "port.san_juan";
                p.displayNameKo = "산후안";
                p.displayNameOriginal = "San Juan";
                p.latitude = 18.47f;
                p.longitude = -66.10f;
                p.shortDescription = "스페인이 푸에르토리코에 세운 굳건한 요새 항구.";
                p.commonProducts = new[] { sugar, rum, tobacco };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/산후안_(푸에르토리코)";
            });

            // ─── 러시아 5 ─────────────────────────────────────────────────────
            CreateOrLoadPort("Port_Arkhangelsk.asset", p =>
            {
                p.portId = "port.arkhangelsk";
                p.displayNameKo = "아르한겔스크";
                p.displayNameOriginal = "Arkhangelsk";
                p.latitude = 64.55f;
                p.longitude = 40.55f;
                p.shortDescription = "백해에 자리잡은 러시아의 첫 국제 무역항.";
                p.commonProducts = new[] { fur, fish, amber };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/아르한겔스크";
            });

            CreateOrLoadPort("Port_StPetersburg.asset", p =>
            {
                p.portId = "port.st_petersburg";
                p.displayNameKo = "상트페테르부르크";
                p.displayNameOriginal = "Sankt-Peterburg";
                p.latitude = 59.93f;
                p.longitude = 30.34f;
                p.shortDescription = "표트르 대제가 발트해 가에 세운 러시아의 새 수도.";
                p.commonProducts = new[] { fur, hemp, wheat };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/상트페테르부르크";
            });

            CreateOrLoadPort("Port_Azov.asset", p =>
            {
                p.portId = "port.azov";
                p.displayNameKo = "아조프";
                p.displayNameOriginal = "Azov";
                p.latitude = 47.11f;
                p.longitude = 39.42f;
                p.shortDescription = "돈 강이 흑해로 흘러드는 자리의 옛 요새 항구.";
                p.commonProducts = new[] { wheat, caviar, fish };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/아조프";
            });

            CreateOrLoadPort("Port_Astrakhan.asset", p =>
            {
                p.portId = "port.astrakhan";
                p.displayNameKo = "아스트라한";
                p.displayNameOriginal = "Astrakhan";
                p.latitude = 46.35f;
                p.longitude = 48.04f;
                p.shortDescription = "볼가 강 끝, 카스피해와 만나는 향신료의 도시.";
                p.commonProducts = new[] { caviar, silk, persianCarpet };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/아스트라한";
            });

            CreateOrLoadPort("Port_Okhotsk.asset", p =>
            {
                p.portId = "port.okhotsk";
                p.displayNameKo = "오호츠크";
                p.displayNameOriginal = "Okhotsk";
                p.latitude = 59.36f;
                p.longitude = 143.24f;
                p.shortDescription = "시베리아 끝, 태평양으로 나가는 러시아의 차가운 항구.";
                p.commonProducts = new[] { fur, whaleOil, fish };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/오호츠크";
            });

            // ─── 동남아 5 ─────────────────────────────────────────────────────
            CreateOrLoadPort("Port_Banten.asset", p =>
            {
                p.portId = "port.banten";
                p.displayNameKo = "반탐";
                p.displayNameOriginal = "Banten";
                p.latitude = -6.04f;
                p.longitude = 106.15f;
                p.shortDescription = "자바 서쪽, 후추가 산처럼 쌓이는 술탄국의 항구.";
                p.commonProducts = new[] { pepper, spiceClove, sandalwood };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/반텐주";
            });

            CreateOrLoadPort("Port_Brunei.asset", p =>
            {
                p.portId = "port.brunei";
                p.displayNameKo = "브루나이";
                p.displayNameOriginal = "Brunei";
                p.latitude = 4.93f;
                p.longitude = 114.94f;
                p.shortDescription = "보르네오 북쪽 정글에 둘러싸인 술탄의 황금 도시.";
                p.commonProducts = new[] { camphor, sandalwood, gold };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/브루나이";
            });

            CreateOrLoadPort("Port_Makassar.asset", p =>
            {
                p.portId = "port.makassar";
                p.displayNameKo = "마카사르";
                p.displayNameOriginal = "Makassar";
                p.latitude = -5.13f;
                p.longitude = 119.41f;
                p.shortDescription = "술라웨시 섬의 향신료 무역 거점. 부기인들의 배가 모여요.";
                p.commonProducts = new[] { spiceClove, sandalwood, pearl };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/마카사르";
            });

            CreateOrLoadPort("Port_Dili.asset", p =>
            {
                p.portId = "port.dili";
                p.displayNameKo = "딜리";
                p.displayNameOriginal = "Dili";
                p.latitude = -8.55f;
                p.longitude = 125.58f;
                p.shortDescription = "포르투갈이 자리잡은 향나무의 섬 티모르의 항구.";
                p.commonProducts = new[] { sandalwood, coffee, pepper };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/딜리";
            });

            CreateOrLoadPort("Port_PhnomPenh.asset", p =>
            {
                p.portId = "port.phnom_penh";
                p.displayNameKo = "프놈펜";
                p.displayNameOriginal = "Phnom Penh";
                p.latitude = 11.56f;
                p.longitude = 104.92f;
                p.shortDescription = "메콩 강이 갈라지는 곳에 자리한 캄보디아의 옛 수도.";
                p.commonProducts = new[] { rice, silk, pepper };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/프놈펜";
            });

            // ─── 아프리카 5 ───────────────────────────────────────────────────
            CreateOrLoadPort("Port_CapeTown.asset", p =>
            {
                p.portId = "port.cape_town";
                p.displayNameKo = "케이프타운";
                p.displayNameOriginal = "Cape Town";
                p.latitude = -33.92f;
                p.longitude = 18.42f;
                p.shortDescription = "희망봉 아래, 인도로 가는 모든 배가 들르는 보급지.";
                p.commonProducts = new[] { portWine, leather, fur };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/케이프타운";
            });

            CreateOrLoadPort("Port_Zanzibar.asset", p =>
            {
                p.portId = "port.zanzibar";
                p.displayNameKo = "잔지바르";
                p.displayNameOriginal = "Zanzibar";
                p.latitude = -6.16f;
                p.longitude = 39.20f;
                p.shortDescription = "정향 향기가 가득한 동아프리카의 향신료 섬.";
                p.commonProducts = new[] { spiceClove, ivory, sandalwood };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/잔지바르";
            });

            CreateOrLoadPort("Port_Mozambique.asset", p =>
            {
                p.portId = "port.mozambique";
                p.displayNameKo = "모잠비크";
                p.displayNameOriginal = "Ilha de Moçambique";
                p.latitude = -15.04f;
                p.longitude = 40.74f;
                p.shortDescription = "포르투갈이 동아프리카에 세운 작은 산호섬 무역항.";
                p.commonProducts = new[] { ivory, gold, ebony };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/모잠비크섬";
            });

            CreateOrLoadPort("Port_Algiers.asset", p =>
            {
                p.portId = "port.algiers";
                p.displayNameKo = "알제";
                p.displayNameOriginal = "Alger";
                p.latitude = 36.75f;
                p.longitude = 3.04f;
                p.shortDescription = "지중해 남쪽 해안의 큰 바르바리 항구.";
                p.commonProducts = new[] { persianCarpet, leather, coral };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/알제";
            });

            CreateOrLoadPort("Port_Toamasina.asset", p =>
            {
                p.portId = "port.toamasina";
                p.displayNameKo = "투아마시나";
                p.displayNameOriginal = "Toamasina";
                p.latitude = -18.15f;
                p.longitude = 49.40f;
                p.shortDescription = "마다가스카르 동쪽 해안의 향신료 항구.";
                p.commonProducts = new[] { vanilla, spiceClove, ebony };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/투아마시나";
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M6PortsSeeder] 완료.\n" +
                "  • 항구 20개 추가 (총 75)\n" +
                "  • 특산물 7개 추가 (총 73)\n" +
                "지역: 아메리카(5) · 러시아(5) · 동남아(5) · 아프리카(5)\n" +
                "\n다음: Game ▸ Refresh All Catalogs → PortCatalog · ProductCatalog 자동 채움.");
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
