using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M4 — 콘텐츠 확장. 항구 25 → 40, 특산물 31 → 65+.
    ///
    /// 추가 지역:
    ///   - 인도양·페르시아만 (Hormuz, Aden, Calicut, Goa)
    ///   - 동남아·동아시아 (Malacca, Manila, Jakarta, Nagasaki)
    ///   - 아프리카 (Mombasa, Mina, Tangier)
    ///   - 신대륙·카리브 (Havana, Cartagena, Santo Domingo)
    ///   - 북대서양 (Reykjavik)
    ///
    /// 메뉴: Game/Seed M4 Ports
    /// 기존 .asset 은 건드리지 않음 (재실행 안전).
    /// 시드 후 Game ▸ Refresh All Catalogs 실행 필요.
    /// </summary>
    public static class M4PortsSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M4 Ports")]
        public static void SeedM4Ports()
        {
            EnsureFolders();

            // ─── 기존 특산물 로드 ─────────────────────────────────────────────
            var pepper        = LoadProduct("Product_Pepper.asset");
            var spiceClove    = LoadProduct("Product_SpiceClove.asset");
            var wool          = LoadProduct("Product_Wool.asset");
            var persianCarpet = LoadProduct("Product_PersianCarpet.asset");
            var sugar         = LoadProduct("Product_Sugar.asset");
            var coffee        = LoadProduct("Product_Coffee.asset");
            var porcelain     = LoadProduct("Product_Porcelain.asset");
            var lacquerware   = LoadProduct("Product_Lacquerware.asset");
            var figs          = LoadProduct("Product_Figs.asset");
            var herring       = LoadProduct("Product_Herring.asset");

            // ─── 신규 특산물 ──────────────────────────────────────────────────
            var pearl = CreateProduct("Product_Pearl.asset", "product.pearl", "진주", 220,
                "조개 안에서 자라난 동그란 보석이에요.",
                "https://ko.wikipedia.org/wiki/진주");

            var cinnamon = CreateProduct("Product_Cinnamon.asset", "product.cinnamon", "계피", 130,
                "나무 껍질을 말려 만든 향긋한 향신료예요.",
                "https://ko.wikipedia.org/wiki/계피");

            var nutmeg = CreateProduct("Product_Nutmeg.asset", "product.nutmeg", "육두구", 150,
                "동남아 작은 섬에서만 자라는 귀한 향신료예요.",
                "https://ko.wikipedia.org/wiki/육두구");

            var cardamom = CreateProduct("Product_Cardamom.asset", "product.cardamom", "카르다몸", 140,
                "차와 카레에 향을 더하는 작은 씨앗이에요.",
                "https://ko.wikipedia.org/wiki/카르다몸");

            var ginger = CreateProduct("Product_Ginger.asset", "product.ginger", "생강", 50,
                "톡 쏘는 매운맛과 약효를 가진 뿌리예요.",
                "https://ko.wikipedia.org/wiki/생강");

            var saffron = CreateProduct("Product_Saffron.asset", "product.saffron", "사프란", 280,
                "노란 꽃에서 한 송이당 세 개씩 얻는 세상에서 가장 비싼 향신료예요.",
                "https://ko.wikipedia.org/wiki/사프란");

            var frankincense = CreateProduct("Product_Frankincense.asset", "product.frankincense", "유향", 160,
                "나무의 진을 굳혀 만든 향기 좋은 보석 같은 향료예요.",
                "https://ko.wikipedia.org/wiki/유향");

            var myrrh = CreateProduct("Product_Myrrh.asset", "product.myrrh", "몰약", 170,
                "고대부터 향과 약으로 쓰인 나무 진이에요.",
                "https://ko.wikipedia.org/wiki/몰약");

            var sandalwood = CreateProduct("Product_Sandalwood.asset", "product.sandalwood", "백단향", 180,
                "은은한 향이 나는 귀한 나무로 부채와 향을 만들어요.",
                "https://ko.wikipedia.org/wiki/단향");

            var silver = CreateProduct("Product_Silver.asset", "product.silver", "은", 200,
                "달처럼 반짝이는 하얀 금속이에요. 동전이나 그릇으로 써요.",
                "https://ko.wikipedia.org/wiki/은");

            var gold = CreateProduct("Product_Gold.asset", "product.gold", "금", 500,
                "햇살처럼 노랗게 빛나는 가장 귀한 금속이에요.",
                "https://ko.wikipedia.org/wiki/금");

            var ivory = CreateProduct("Product_Ivory.asset", "product.ivory", "상아", 240,
                "코끼리의 큰 어금니로, 옛 사람들은 조각으로 만들었어요.",
                "https://ko.wikipedia.org/wiki/상아");

            var emerald = CreateProduct("Product_Emerald.asset", "product.emerald", "에메랄드", 380,
                "초록빛이 영롱한 보석. 신대륙에서 많이 나왔어요.",
                "https://ko.wikipedia.org/wiki/에메랄드");

            var tobacco = CreateProduct("Product_Tobacco.asset", "product.tobacco", "담배잎", 95,
                "신대륙에서 가져온 식물. 어른들이 말려서 피웠어요.",
                "https://ko.wikipedia.org/wiki/담배");

            var cocoa = CreateProduct("Product_Cocoa.asset", "product.cocoa", "카카오", 110,
                "초콜릿의 원료가 되는 갈색 씨앗이에요. 아즈텍 사람들이 음료로 마셨어요.",
                "https://ko.wikipedia.org/wiki/카카오");

            var vanilla = CreateProduct("Product_Vanilla.asset", "product.vanilla", "바닐라", 190,
                "달콤한 향이 나는 꼬투리. 디저트에 향을 더해요.",
                "https://ko.wikipedia.org/wiki/바닐라");

            var pineapple = CreateProduct("Product_Pineapple.asset", "product.pineapple", "파인애플", 80,
                "왕관 모양 꼭지가 달린 노란 열대 과일이에요.",
                "https://ko.wikipedia.org/wiki/파인애플");

            var coconut = CreateProduct("Product_Coconut.asset", "product.coconut", "코코넛", 35,
                "야자수 위에 달리는 큰 갈색 열매. 안에 시원한 물이 들어있어요.",
                "https://ko.wikipedia.org/wiki/코코넛");

            var rum = CreateProduct("Product_Rum.asset", "product.rum", "럼주", 100,
                "사탕수수로 만든 도수 높은 술. 카리브 해적들이 좋아했어요.",
                "https://ko.wikipedia.org/wiki/럼");

            var coral = CreateProduct("Product_Coral.asset", "product.coral", "산호", 170,
                "바다 속에서 자란 빨간 보석. 장신구로 인기였어요.",
                "https://ko.wikipedia.org/wiki/산호");

            var leather = CreateProduct("Product_Leather.asset", "product.leather", "가죽", 60,
                "동물 껍질을 부드럽게 다듬어 옷·신발을 만들어요.",
                "https://ko.wikipedia.org/wiki/가죽");

            var fur = CreateProduct("Product_Fur.asset", "product.fur", "모피", 130,
                "북쪽 추운 지방의 동물 털가죽. 따뜻한 외투를 만들어요.",
                "https://ko.wikipedia.org/wiki/모피");

            var fish = CreateProduct("Product_Codfish.asset", "product.codfish", "대구", 40,
                "북대서양에서 잡히는 큰 흰살 생선. 말려서 오래 보관해요.",
                "https://ko.wikipedia.org/wiki/대구_(어류)");

            var indigo = CreateProduct("Product_Indigo.asset", "product.indigo", "쪽빛 염료", 120,
                "식물에서 얻는 깊은 파란색 천 염색 재료예요.",
                "https://ko.wikipedia.org/wiki/인디고");

            var amber = CreateProduct("Product_Amber.asset", "product.amber", "호박", 200,
                "수천 년 전 나무 진이 굳어 만들어진 노란 보석이에요.",
                "https://ko.wikipedia.org/wiki/호박_(보석)");

            var jade = CreateProduct("Product_Jade.asset", "product.jade", "옥", 250,
                "초록·하양색이 어우러진 동아시아의 귀한 돌이에요.",
                "https://ko.wikipedia.org/wiki/옥_(보석)");

            // ─── 신규 항구 ────────────────────────────────────────────────────

            // 인도양 · 페르시아만
            CreateOrLoadPort("Port_Hormuz.asset", p =>
            {
                p.portId = "port.hormuz";
                p.displayNameKo = "호르무즈";
                p.displayNameOriginal = "Hormuz";
                p.latitude = 27.10f;
                p.longitude = 56.45f;
                p.shortDescription = "동방의 향신료가 모이는 페르시아만 입구의 항구.";
                p.commonProducts = new[] { pepper, cinnamon, pearl };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/호르무즈";
            });

            CreateOrLoadPort("Port_Aden.asset", p =>
            {
                p.portId = "port.aden";
                p.displayNameKo = "아덴";
                p.displayNameOriginal = "Aden";
                p.latitude = 12.78f;
                p.longitude = 45.04f;
                p.shortDescription = "홍해와 인도양이 만나는 향료의 도시.";
                p.commonProducts = new[] { coffee, frankincense, myrrh };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/아덴";
            });

            CreateOrLoadPort("Port_Calicut.asset", p =>
            {
                p.portId = "port.calicut";
                p.displayNameKo = "캘리컷";
                p.displayNameOriginal = "Kozhikode";
                p.latitude = 11.25f;
                p.longitude = 75.78f;
                p.shortDescription = "바스코 다 가마가 발견한 인도의 향신료 항구.";
                p.commonProducts = new[] { pepper, cinnamon, cardamom };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/코지코드";
            });

            CreateOrLoadPort("Port_Goa.asset", p =>
            {
                p.portId = "port.goa";
                p.displayNameKo = "고아";
                p.displayNameOriginal = "Goa";
                p.latitude = 15.50f;
                p.longitude = 73.83f;
                p.shortDescription = "포르투갈 사람들이 세운 인도의 무역 거점.";
                p.commonProducts = new[] { pepper, ginger, vanilla };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/고아_주";
            });

            // 동남아 · 동아시아
            CreateOrLoadPort("Port_Malacca.asset", p =>
            {
                p.portId = "port.malacca";
                p.displayNameKo = "말라카";
                p.displayNameOriginal = "Melaka";
                p.latitude = 2.20f;
                p.longitude = 102.25f;
                p.shortDescription = "동양과 서양을 잇는 좁은 해협의 황금 무역항.";
                p.commonProducts = new[] { spiceClove, nutmeg, sandalwood };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/믈라카주";
            });

            CreateOrLoadPort("Port_Manila.asset", p =>
            {
                p.portId = "port.manila";
                p.displayNameKo = "마닐라";
                p.displayNameOriginal = "Manila";
                p.latitude = 14.60f;
                p.longitude = 120.98f;
                p.shortDescription = "스페인이 만든 동남아의 큰 무역 도시.";
                p.commonProducts = new[] { silver, tobacco, pineapple };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/마닐라";
            });

            CreateOrLoadPort("Port_Jakarta.asset", p =>
            {
                p.portId = "port.jakarta";
                p.displayNameKo = "자카르타";
                p.displayNameOriginal = "Batavia";
                p.latitude = -6.20f;
                p.longitude = 106.85f;
                p.shortDescription = "네덜란드 동인도회사가 세운 자바섬의 향신료 항구.";
                p.commonProducts = new[] { nutmeg, spiceClove, coffee };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/자카르타";
            });

            CreateOrLoadPort("Port_Nagasaki.asset", p =>
            {
                p.portId = "port.nagasaki";
                p.displayNameKo = "나가사키";
                p.displayNameOriginal = "Nagasaki";
                p.latitude = 32.75f;
                p.longitude = 129.87f;
                p.shortDescription = "포르투갈·네덜란드 배가 드나든 일본의 작은 항구.";
                p.commonProducts = new[] { silver, porcelain, lacquerware };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/나가사키시";
            });

            // 아프리카
            CreateOrLoadPort("Port_Mombasa.asset", p =>
            {
                p.portId = "port.mombasa";
                p.displayNameKo = "몸바사";
                p.displayNameOriginal = "Mombasa";
                p.latitude = -4.05f;
                p.longitude = 39.67f;
                p.shortDescription = "동아프리카 해안의 스와힐리 무역 도시.";
                p.commonProducts = new[] { ivory, gold, coconut };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/몸바사";
            });

            CreateOrLoadPort("Port_Mina.asset", p =>
            {
                p.portId = "port.mina";
                p.displayNameKo = "엘미나";
                p.displayNameOriginal = "Elmina";
                p.latitude = 5.08f;
                p.longitude = -1.35f;
                p.shortDescription = "포르투갈이 황금을 사러 세운 서아프리카 요새 항구.";
                p.commonProducts = new[] { gold, ivory, pepper };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/엘미나";
            });

            CreateOrLoadPort("Port_Tangier.asset", p =>
            {
                p.portId = "port.tangier";
                p.displayNameKo = "탕헤르";
                p.displayNameOriginal = "Tanger";
                p.latitude = 35.78f;
                p.longitude = -5.81f;
                p.shortDescription = "지브롤터 너머 모로코 북쪽의 옛 도시.";
                p.commonProducts = new[] { leather, persianCarpet, figs };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/탕헤르";
            });

            // 신대륙 · 카리브
            CreateOrLoadPort("Port_Havana.asset", p =>
            {
                p.portId = "port.havana";
                p.displayNameKo = "아바나";
                p.displayNameOriginal = "La Habana";
                p.latitude = 23.13f;
                p.longitude = -82.38f;
                p.shortDescription = "스페인 보물선이 모이는 카리브해의 큰 항구.";
                p.commonProducts = new[] { sugar, tobacco, rum };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/아바나";
            });

            CreateOrLoadPort("Port_Cartagena.asset", p =>
            {
                p.portId = "port.cartagena";
                p.displayNameKo = "카르타헤나";
                p.displayNameOriginal = "Cartagena";
                p.latitude = 10.39f;
                p.longitude = -75.51f;
                p.shortDescription = "신대륙의 황금과 보석이 모이는 강한 요새 도시.";
                p.commonProducts = new[] { gold, emerald, cocoa };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/카르타헤나_(콜롬비아)";
            });

            CreateOrLoadPort("Port_SantoDomingo.asset", p =>
            {
                p.portId = "port.santo_domingo";
                p.displayNameKo = "산토도밍고";
                p.displayNameOriginal = "Santo Domingo";
                p.latitude = 18.47f;
                p.longitude = -69.89f;
                p.shortDescription = "신대륙에 처음 세워진 유럽 도시.";
                p.commonProducts = new[] { sugar, tobacco, cocoa };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/산토도밍고";
            });

            // 북대서양
            CreateOrLoadPort("Port_Reykjavik.asset", p =>
            {
                p.portId = "port.reykjavik";
                p.displayNameKo = "레이캬비크";
                p.displayNameOriginal = "Reykjavík";
                p.latitude = 64.13f;
                p.longitude = -21.94f;
                p.shortDescription = "북쪽 끝 차가운 섬나라의 작은 어촌 항구.";
                p.commonProducts = new[] { fish, wool, fur };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/레이캬비크";
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M4PortsSeeder] 완료.\n" +
                "  • 항구 15개 추가 (총 40)\n" +
                "  • 특산물 26개 추가 (총 57)\n" +
                "지역: 인도양·동남아·동아시아·아프리카·카리브·북대서양\n" +
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
