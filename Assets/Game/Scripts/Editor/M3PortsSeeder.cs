using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M3 마일스톤 — 지역 확장. 8개국 시작 항구 외에 각 국가별 인접 항구 2개씩 추가.
    /// 총 +16 항구, +14 신규 특산물.
    ///
    /// 메뉴: Game/Seed M3 Ports
    /// 이미 존재하는 .asset 은 건드리지 않음 (재실행 안전).
    ///
    /// 새 항구가 추가되면 Game ▸ Refresh All Catalogs 도 함께 실행 권장.
    /// </summary>
    public static class M3PortsSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M3 Ports")]
        public static void SeedM3Ports()
        {
            EnsureFolders();

            // ─── 기존 특산물 로드 (없으면 새로 만든 게 아니므로 그냥 통과) ────────────
            var oliveOil      = LoadProduct("Product_OliveOil.asset");
            var orange        = LoadProduct("Product_Orange.asset");
            var pepper        = LoadProduct("Product_Pepper.asset");
            var dutchCheese   = LoadProduct("Product_DutchCheese.asset");
            var wool          = LoadProduct("Product_Wool.asset");
            var tin           = LoadProduct("Product_Tin.asset");
            var persianCarpet = LoadProduct("Product_PersianCarpet.asset");
            var ginseng       = LoadProduct("Product_Ginseng.asset");
            var celadon       = LoadProduct("Product_Celadon.asset");
            var silk          = LoadProduct("Product_Silk.asset");
            var cork          = LoadProduct("Product_Cork.asset");

            // ─── 14개 신규 특산물 ────────────────────────────────────────────
            var portWine = CreateOrLoadProduct("Product_PortWine.asset", p =>
            {
                p.productId = "product.port_wine";
                p.displayNameKo = "포트와인";
                p.basePrice = 90;
                p.shortDescription = "포르투갈 북쪽에서 만든 단맛 나는 와인이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/포트와인";
            });

            var sugar = CreateOrLoadProduct("Product_Sugar.asset", p =>
            {
                p.productId = "product.sugar";
                p.displayNameKo = "설탕";
                p.basePrice = 70;
                p.shortDescription = "사탕수수에서 짜낸 달콤한 하얀 가루예요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/설탕";
            });

            var sherry = CreateOrLoadProduct("Product_Sherry.asset", p =>
            {
                p.productId = "product.sherry";
                p.displayNameKo = "셰리주";
                p.basePrice = 85;
                p.shortDescription = "스페인 남부에서 만든 향기 좋은 술이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/셰리주";
            });

            var cotton = CreateOrLoadProduct("Product_Cotton.asset", p =>
            {
                p.productId = "product.cotton";
                p.displayNameKo = "목화";
                p.basePrice = 45;
                p.shortDescription = "구름처럼 폭신폭신한 흰 솜이 자라는 식물이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/목화";
            });

            var velvet = CreateOrLoadProduct("Product_Velvet.asset", p =>
            {
                p.productId = "product.velvet";
                p.displayNameKo = "벨벳";
                p.basePrice = 110;
                p.shortDescription = "만지면 보들보들한 광택 나는 고급 천이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/벨벳";
            });

            var lemon = CreateOrLoadProduct("Product_Lemon.asset", p =>
            {
                p.productId = "product.lemon";
                p.displayNameKo = "레몬";
                p.basePrice = 30;
                p.shortDescription = "노랗고 새콤한 동그란 과일이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/레몬";
            });

            var herring = CreateOrLoadProduct("Product_Herring.asset", p =>
            {
                p.productId = "product.herring";
                p.displayNameKo = "청어";
                p.basePrice = 40;
                p.shortDescription = "북쪽 바다에서 잡아 절여 둔 작은 생선이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/청어";
            });

            var diamond = CreateOrLoadProduct("Product_Diamond.asset", p =>
            {
                p.productId = "product.diamond";
                p.displayNameKo = "다이아몬드";
                p.basePrice = 250;
                p.shortDescription = "세상에서 가장 단단한 반짝이는 보석이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/다이아몬드";
            });

            var figs = CreateOrLoadProduct("Product_Figs.asset", p =>
            {
                p.productId = "product.figs";
                p.displayNameKo = "말린 무화과";
                p.basePrice = 55;
                p.shortDescription = "햇빛에 잘 말려 단맛이 진해진 과일이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/무화과";
            });

            var coffee = CreateOrLoadProduct("Product_Coffee.asset", p =>
            {
                p.productId = "product.coffee";
                p.displayNameKo = "커피콩";
                p.basePrice = 100;
                p.shortDescription = "볶으면 진한 향이 나는 까만 콩이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/커피";
            });

            var hanji = CreateOrLoadProduct("Product_Hanji.asset", p =>
            {
                p.productId = "product.hanji";
                p.displayNameKo = "한지";
                p.basePrice = 65;
                p.shortDescription = "닥나무로 떠서 만든 우리나라의 튼튼한 종이예요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/한지";
            });

            var driedGim = CreateOrLoadProduct("Product_DriedGim.asset", p =>
            {
                p.productId = "product.dried_gim";
                p.displayNameKo = "마른 김";
                p.basePrice = 35;
                p.shortDescription = "바다에서 자라는 풀을 얇게 펴서 말린 음식이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/김_(음식)";
            });

            var tea = CreateOrLoadProduct("Product_Tea.asset", p =>
            {
                p.productId = "product.tea";
                p.displayNameKo = "차";
                p.basePrice = 75;
                p.shortDescription = "찻잎을 따 말려 우려 마시는 향기 좋은 음료예요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/차_(음료)";
            });

            var lacquerware = CreateOrLoadProduct("Product_Lacquerware.asset", p =>
            {
                p.productId = "product.lacquerware";
                p.displayNameKo = "칠기";
                p.basePrice = 95;
                p.shortDescription = "옻나무 즙을 여러 번 발라 반들반들하게 만든 그릇이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/칠기";
            });

            // ─── 16개 신규 항구 ─────────────────────────────────────────────

            // 포르투갈
            CreateOrLoadPort("Port_Porto.asset", p =>
            {
                p.portId = "port.porto";
                p.displayNameKo = "포르투";
                p.displayNameOriginal = "Porto";
                p.latitude = 41.15f;
                p.longitude = -8.62f;
                p.shortDescription = "강 위에 빨간 다리가 걸린, 와인이 익어가는 도시.";
                p.commonProducts = new[] { portWine, cork };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/포르투";
            });

            CreateOrLoadPort("Port_Funchal.asset", p =>
            {
                p.portId = "port.funchal";
                p.displayNameKo = "푼샬";
                p.displayNameOriginal = "Funchal";
                p.latitude = 32.65f;
                p.longitude = -16.91f;
                p.shortDescription = "푸른 바다 한가운데 있는 마데이라 섬의 항구.";
                p.commonProducts = new[] { sugar, portWine };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/푼샬";
            });

            // 스페인
            CreateOrLoadPort("Port_Malaga.asset", p =>
            {
                p.portId = "port.malaga";
                p.displayNameKo = "말라가";
                p.displayNameOriginal = "Málaga";
                p.latitude = 36.72f;
                p.longitude = -4.42f;
                p.shortDescription = "햇볕이 따뜻한 지중해 바닷가의 옛 항구.";
                p.commonProducts = new[] { sherry, orange };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/말라가";
            });

            CreateOrLoadPort("Port_Barcelona.asset", p =>
            {
                p.portId = "port.barcelona";
                p.displayNameKo = "바르셀로나";
                p.displayNameOriginal = "Barcelona";
                p.latitude = 41.38f;
                p.longitude = 2.18f;
                p.shortDescription = "지중해를 바라보는 큰 무역 도시.";
                p.commonProducts = new[] { cotton, wool };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/바르셀로나";
            });

            // 이탈리아
            CreateOrLoadPort("Port_Genova.asset", p =>
            {
                p.portId = "port.genova";
                p.displayNameKo = "제노바";
                p.displayNameOriginal = "Genova";
                p.latitude = 44.41f;
                p.longitude = 8.93f;
                p.shortDescription = "베네치아와 다투며 지중해를 누비던 자랑스러운 항구.";
                p.commonProducts = new[] { velvet, oliveOil };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/제노바";
            });

            CreateOrLoadPort("Port_Napoli.asset", p =>
            {
                p.portId = "port.napoli";
                p.displayNameKo = "나폴리";
                p.displayNameOriginal = "Napoli";
                p.latitude = 40.84f;
                p.longitude = 14.25f;
                p.shortDescription = "큰 산이 옆에 우뚝 선, 따뜻한 남쪽 항구.";
                p.commonProducts = new[] { lemon, oliveOil };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/나폴리";
            });

            // 네덜란드
            CreateOrLoadPort("Port_Rotterdam.asset", p =>
            {
                p.portId = "port.rotterdam";
                p.displayNameKo = "로테르담";
                p.displayNameOriginal = "Rotterdam";
                p.latitude = 51.92f;
                p.longitude = 4.48f;
                p.shortDescription = "북해로 나가는 큰 강 어귀의 분주한 항구.";
                p.commonProducts = new[] { herring, dutchCheese };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/로테르담";
            });

            CreateOrLoadPort("Port_Antwerpen.asset", p =>
            {
                p.portId = "port.antwerpen";
                p.displayNameKo = "안트베르펜";
                p.displayNameOriginal = "Antwerpen";
                p.latitude = 51.22f;
                p.longitude = 4.40f;
                p.shortDescription = "보석을 다듬는 장인이 모인 강가의 도시.";
                p.commonProducts = new[] { diamond, dutchCheese };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/안트베르펜";
            });

            // 영국
            CreateOrLoadPort("Port_Plymouth.asset", p =>
            {
                p.portId = "port.plymouth";
                p.displayNameKo = "플리머스";
                p.displayNameOriginal = "Plymouth";
                p.latitude = 50.37f;
                p.longitude = -4.14f;
                p.shortDescription = "용감한 뱃사람들이 새 땅을 향해 떠나는 작은 항구.";
                p.commonProducts = new[] { wool, tin };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/플리머스_(잉글랜드)";
            });

            CreateOrLoadPort("Port_Bristol.asset", p =>
            {
                p.portId = "port.bristol";
                p.displayNameKo = "브리스틀";
                p.displayNameOriginal = "Bristol";
                p.latitude = 51.45f;
                p.longitude = -2.60f;
                p.shortDescription = "강이 만나는 곳에 자리한 옛 무역항.";
                p.commonProducts = new[] { wool, tin };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/브리스틀";
            });

            // 오스만
            CreateOrLoadPort("Port_Izmir.asset", p =>
            {
                p.portId = "port.izmir";
                p.displayNameKo = "이즈미르";
                p.displayNameOriginal = "İzmir";
                p.latitude = 38.42f;
                p.longitude = 27.14f;
                p.shortDescription = "에게해를 마주 보는 따뜻한 오스만의 항구.";
                p.commonProducts = new[] { figs, persianCarpet };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/이즈미르";
            });

            CreateOrLoadPort("Port_Alexandria.asset", p =>
            {
                p.portId = "port.alexandria";
                p.displayNameKo = "알렉산드리아";
                p.displayNameOriginal = "Iskenderiye";
                p.latitude = 31.20f;
                p.longitude = 29.92f;
                p.shortDescription = "이집트 큰 강 어귀에 자리한 옛 학문의 도시.";
                p.commonProducts = new[] { coffee, pepper };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/알렉산드리아";
            });

            // 조선
            CreateOrLoadPort("Port_Jemulpo.asset", p =>
            {
                p.portId = "port.jemulpo";
                p.displayNameKo = "제물포";
                p.displayNameOriginal = "濟物浦";
                p.latitude = 37.45f;
                p.longitude = 126.65f;
                p.shortDescription = "한양으로 들어가는 큰 길목의 서해 항구.";
                p.commonProducts = new[] { hanji, ginseng };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/제물포_(인천)";
            });

            CreateOrLoadPort("Port_Mokpo.asset", p =>
            {
                p.portId = "port.mokpo";
                p.displayNameKo = "목포";
                p.displayNameOriginal = "木浦";
                p.latitude = 34.81f;
                p.longitude = 126.39f;
                p.shortDescription = "남서쪽 끝에서 작은 섬들을 바라보는 항구.";
                p.commonProducts = new[] { driedGim, celadon };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/목포시";
            });

            // 중국
            CreateOrLoadPort("Port_Quanzhou.asset", p =>
            {
                p.portId = "port.quanzhou";
                p.displayNameKo = "취안저우";
                p.displayNameOriginal = "泉州";
                p.latitude = 24.87f;
                p.longitude = 118.67f;
                p.shortDescription = "옛날에 자이툰이라 불리던 큰 비단의 도시.";
                p.commonProducts = new[] { tea, silk };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/취안저우시";
            });

            CreateOrLoadPort("Port_Hangzhou.asset", p =>
            {
                p.portId = "port.hangzhou";
                p.displayNameKo = "항저우";
                p.displayNameOriginal = "杭州";
                p.latitude = 30.25f;
                p.longitude = 120.15f;
                p.shortDescription = "맑은 호수 옆에 비단과 차를 빚는 옛 도시.";
                p.commonProducts = new[] { lacquerware, tea };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/항저우시";
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M3PortsSeeder] 완료. 16개 항구 + 14개 특산물 추가.\n" +
                "  • 포르투갈: 포르투, 푼샬\n" +
                "  • 스페인: 말라가, 바르셀로나\n" +
                "  • 이탈리아: 제노바, 나폴리\n" +
                "  • 네덜란드: 로테르담, 안트베르펜\n" +
                "  • 영국: 플리머스, 브리스틀\n" +
                "  • 오스만: 이즈미르, 알렉산드리아\n" +
                "  • 조선: 제물포, 목포\n" +
                "  • 중국: 취안저우, 항저우\n" +
                "\n다음 단계: Game ▸ Refresh All Catalogs 실행 → 카탈로그에 자동 등록.");
        }

        // ─── 헬퍼 ───────────────────────────────────────────────────────────

        private static ProductData LoadProduct(string fileName)
        {
            var path = $"{DataRoot}/Products/{fileName}";
            return AssetDatabase.LoadAssetAtPath<ProductData>(path);
        }

        private static ProductData CreateOrLoadProduct(string fileName, Action<ProductData> setup)
        {
            var path = $"{DataRoot}/Products/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<ProductData>(path);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<ProductData>();
            setup(so);
            AssetDatabase.CreateAsset(so, path);
            return so;
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
