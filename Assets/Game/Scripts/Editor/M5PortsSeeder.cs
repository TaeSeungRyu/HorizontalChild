using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M5 — 콘텐츠 확장. 항구 40 → 55, 신대륙·동남아 확장.
    ///
    /// 추가:
    ///   - 북미 5: Boston, New York, Quebec, Charleston, Veracruz
    ///   - 남미 5: Lima(Callao), Buenos Aires, Salvador, Rio de Janeiro, Valparaíso
    ///   - 동남아 5: Macau, Hoi An, Ayutthaya, Saigon, Penang
    ///   - 신규 특산물 9: 쌀, 옥수수, 브라질우드, 마테차, 메이플시럽, 토마토, 망고, 키니네, 호박
    ///
    /// 메뉴: Game/Seed M5 Ports
    /// 기존 .asset 은 건드리지 않음. 시드 후 Game ▸ Refresh All Catalogs 실행 필요.
    /// </summary>
    public static class M5PortsSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M5 Ports")]
        public static void SeedM5Ports()
        {
            EnsureFolders();

            // ─── 기존 특산물 로드 ─────────────────────────────────────────────
            var pepper        = LoadProduct("Product_Pepper.asset");
            var spiceClove    = LoadProduct("Product_SpiceClove.asset");
            var cotton        = LoadProduct("Product_Cotton.asset");
            var indigo        = LoadProduct("Product_Indigo.asset");
            var silver        = LoadProduct("Product_Silver.asset");
            var gold          = LoadProduct("Product_Gold.asset");
            var sugar         = LoadProduct("Product_Sugar.asset");
            var cocoa         = LoadProduct("Product_Cocoa.asset");
            var tobacco       = LoadProduct("Product_Tobacco.asset");
            var vanilla       = LoadProduct("Product_Vanilla.asset");
            var fur           = LoadProduct("Product_Fur.asset");
            var leather       = LoadProduct("Product_Leather.asset");
            var fish          = LoadProduct("Product_Codfish.asset");
            var rum           = LoadProduct("Product_Rum.asset");
            var tea           = LoadProduct("Product_Tea.asset");
            var porcelain     = LoadProduct("Product_Porcelain.asset");
            var silk          = LoadProduct("Product_Silk.asset");
            var sandalwood    = LoadProduct("Product_Sandalwood.asset");
            var ivory         = LoadProduct("Product_Ivory.asset");
            var orange        = LoadProduct("Product_Orange.asset");

            // ─── 신규 특산물 9 ────────────────────────────────────────────────
            var rice = CreateProduct("Product_Rice.asset", "product.rice", "쌀", 35,
                "물 댄 논에서 자라는 알알이 굵은 곡식이에요. 동아시아의 주식.",
                "https://ko.wikipedia.org/wiki/쌀");

            var maize = CreateProduct("Product_Maize.asset", "product.maize", "옥수수", 30,
                "노란 알갱이가 줄지어 박힌 신대륙의 곡식이에요.",
                "https://ko.wikipedia.org/wiki/옥수수");

            var brazilwood = CreateProduct("Product_Brazilwood.asset", "product.brazilwood", "브라질우드", 140,
                "빨간 염료를 얻는 남미의 단단한 나무예요. 브라질이라는 나라 이름의 유래.",
                "https://ko.wikipedia.org/wiki/파우브라질");

            var mate = CreateProduct("Product_Mate.asset", "product.mate", "마테차", 75,
                "남미 사람들이 호리병에서 빨대로 마시는 향긋한 차예요.",
                "https://ko.wikipedia.org/wiki/마테차");

            var mapleSyrup = CreateProduct("Product_MapleSyrup.asset", "product.maple_syrup", "메이플시럽", 90,
                "북미 단풍나무에서 흘러나오는 달콤한 황금빛 진액이에요.",
                "https://ko.wikipedia.org/wiki/메이플_시럽");

            var tomato = CreateProduct("Product_Tomato.asset", "product.tomato", "토마토", 25,
                "신대륙에서 건너온 빨간 열매. 처음엔 독이 있다고 오해받았어요.",
                "https://ko.wikipedia.org/wiki/토마토");

            var mango = CreateProduct("Product_Mango.asset", "product.mango", "망고", 55,
                "달콤한 노란 과육이 가득한 동남아의 열대 과일이에요.",
                "https://ko.wikipedia.org/wiki/망고");

            var quinine = CreateProduct("Product_Quinine.asset", "product.quinine", "키니네", 230,
                "안데스 산맥 나무껍질에서 얻는 말라리아 치료약이에요.",
                "https://ko.wikipedia.org/wiki/퀴닌");

            var pumpkin = CreateProduct("Product_Pumpkin.asset", "product.pumpkin", "호박", 28,
                "두꺼운 주황 껍질의 큰 열매. 신대륙에서 자라요.",
                "https://ko.wikipedia.org/wiki/호박_(식물)");

            // ─── 북미 항구 5 ──────────────────────────────────────────────────
            CreateOrLoadPort("Port_Boston.asset", p =>
            {
                p.portId = "port.boston";
                p.displayNameKo = "보스턴";
                p.displayNameOriginal = "Boston";
                p.latitude = 42.36f;
                p.longitude = -71.06f;
                p.shortDescription = "북미 동해안 영국 식민지의 활기찬 무역 항구.";
                p.commonProducts = new[] { fur, fish, rum };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/보스턴";
            });

            CreateOrLoadPort("Port_NewYork.asset", p =>
            {
                p.portId = "port.new_york";
                p.displayNameKo = "뉴욕";
                p.displayNameOriginal = "New York";
                p.latitude = 40.71f;
                p.longitude = -74.01f;
                p.shortDescription = "네덜란드가 세우고 영국이 이름 바꾼 신대륙의 큰 도시.";
                p.commonProducts = new[] { fur, leather, maize };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/뉴욕";
            });

            CreateOrLoadPort("Port_Quebec.asset", p =>
            {
                p.portId = "port.quebec";
                p.displayNameKo = "퀘벡";
                p.displayNameOriginal = "Québec";
                p.latitude = 46.81f;
                p.longitude = -71.21f;
                p.shortDescription = "프랑스 사람들이 세운 추운 강가 도시. 모피 무역의 중심.";
                p.commonProducts = new[] { fur, fish, mapleSyrup };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/퀘벡_시";
            });

            CreateOrLoadPort("Port_Charleston.asset", p =>
            {
                p.portId = "port.charleston";
                p.displayNameKo = "찰스턴";
                p.displayNameOriginal = "Charleston";
                p.latitude = 32.78f;
                p.longitude = -79.93f;
                p.shortDescription = "남쪽 따뜻한 평야의 영국 식민지 항구.";
                p.commonProducts = new[] { rice, indigo, cotton };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/찰스턴_(사우스캐롤라이나주)";
            });

            CreateOrLoadPort("Port_Veracruz.asset", p =>
            {
                p.portId = "port.veracruz";
                p.displayNameKo = "베라크루스";
                p.displayNameOriginal = "Veracruz";
                p.latitude = 19.18f;
                p.longitude = -96.13f;
                p.shortDescription = "스페인이 멕시코에서 보물을 실어 보내던 큰 항구.";
                p.commonProducts = new[] { silver, cocoa, vanilla };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/베라크루스";
            });

            // ─── 남미 항구 5 ──────────────────────────────────────────────────
            CreateOrLoadPort("Port_Callao.asset", p =>
            {
                p.portId = "port.callao";
                p.displayNameKo = "카야오";
                p.displayNameOriginal = "Callao";
                p.latitude = -12.05f;
                p.longitude = -77.13f;
                p.shortDescription = "잉카 위에 세워진 페루의 스페인 부왕령 항구.";
                p.commonProducts = new[] { silver, quinine, maize };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/카야오";
            });

            CreateOrLoadPort("Port_BuenosAires.asset", p =>
            {
                p.portId = "port.buenos_aires";
                p.displayNameKo = "부에노스아이레스";
                p.displayNameOriginal = "Buenos Aires";
                p.latitude = -34.61f;
                p.longitude = -58.38f;
                p.shortDescription = "라플라타 강이 바다와 만나는 곳에 세워진 큰 평원의 도시.";
                p.commonProducts = new[] { leather, mate, maize };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/부에노스아이레스";
            });

            CreateOrLoadPort("Port_Salvador.asset", p =>
            {
                p.portId = "port.salvador";
                p.displayNameKo = "살바도르";
                p.displayNameOriginal = "Salvador";
                p.latitude = -12.97f;
                p.longitude = -38.51f;
                p.shortDescription = "포르투갈이 브라질에 처음 세운 식민 도시.";
                p.commonProducts = new[] { sugar, tobacco, brazilwood };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/사우바도르";
            });

            CreateOrLoadPort("Port_RioDeJaneiro.asset", p =>
            {
                p.portId = "port.rio_de_janeiro";
                p.displayNameKo = "리우데자네이루";
                p.displayNameOriginal = "Rio de Janeiro";
                p.latitude = -22.91f;
                p.longitude = -43.17f;
                p.shortDescription = "큰 만을 따라 자리잡은 브라질의 아름다운 항구 도시.";
                p.commonProducts = new[] { sugar, cocoa, brazilwood };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/리우데자네이루";
            });

            CreateOrLoadPort("Port_Valparaiso.asset", p =>
            {
                p.portId = "port.valparaiso";
                p.displayNameKo = "발파라이소";
                p.displayNameOriginal = "Valparaíso";
                p.latitude = -33.05f;
                p.longitude = -71.61f;
                p.shortDescription = "안데스 산맥 너머 칠레 해안의 무역 항구.";
                p.commonProducts = new[] { silver, tomato, quinine };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/발파라이소";
            });

            // ─── 동남아 항구 5 ────────────────────────────────────────────────
            CreateOrLoadPort("Port_Macau.asset", p =>
            {
                p.portId = "port.macau";
                p.displayNameKo = "마카오";
                p.displayNameOriginal = "Macau";
                p.latitude = 22.20f;
                p.longitude = 113.55f;
                p.shortDescription = "포르투갈이 중국 해안에 임대한 작은 무역 항구.";
                p.commonProducts = new[] { tea, porcelain, silk };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/마카오";
            });

            CreateOrLoadPort("Port_HoiAn.asset", p =>
            {
                p.portId = "port.hoi_an";
                p.displayNameKo = "호이안";
                p.displayNameOriginal = "Hội An";
                p.latitude = 15.88f;
                p.longitude = 108.34f;
                p.shortDescription = "베트남 중부 강가의 아담한 옛 무역 도시.";
                p.commonProducts = new[] { silk, porcelain, sandalwood };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/호이안";
            });

            CreateOrLoadPort("Port_Ayutthaya.asset", p =>
            {
                p.portId = "port.ayutthaya";
                p.displayNameKo = "아유타야";
                p.displayNameOriginal = "Ayutthaya";
                p.latitude = 14.35f;
                p.longitude = 100.57f;
                p.shortDescription = "시암 왕국의 큰 도시. 강줄기가 사방으로 흘러요.";
                p.commonProducts = new[] { rice, ivory, sandalwood };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/아유타야";
            });

            CreateOrLoadPort("Port_Saigon.asset", p =>
            {
                p.portId = "port.saigon";
                p.displayNameKo = "사이공";
                p.displayNameOriginal = "Sài Gòn";
                p.latitude = 10.78f;
                p.longitude = 106.70f;
                p.shortDescription = "메콩강 삼각주 위에 자라난 베트남 남쪽 항구.";
                p.commonProducts = new[] { rice, pepper, mango };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/호찌민시";
            });

            CreateOrLoadPort("Port_Penang.asset", p =>
            {
                p.portId = "port.penang";
                p.displayNameKo = "페낭";
                p.displayNameOriginal = "Pulau Pinang";
                p.latitude = 5.41f;
                p.longitude = 100.34f;
                p.shortDescription = "말라카 해협 북쪽의 향신료 섬 항구.";
                p.commonProducts = new[] { spiceClove, pepper, sandalwood };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/피낭주";
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M5PortsSeeder] 완료.\n" +
                "  • 항구 15개 추가 (총 55)\n" +
                "  • 특산물 9개 추가\n" +
                "지역: 북미(5) · 남미(5) · 동남아(5)\n" +
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
