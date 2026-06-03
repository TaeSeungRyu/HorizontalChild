using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M2 마일스톤 — 8개국 선택 + 각국 시작 항구.
    /// M1 의 포르투갈 외에 7개국 NationData + 7개 시작 항구 PortData + 14개 일반 특산물 추가.
    ///
    /// 메뉴: Game/Seed M2 Content
    ///
    /// 이미 존재하는 .asset 은 건드리지 않음 (재실행 안전).
    /// 스페셜 특산물·캐릭터·미션·발견물 등은 M3 시더에서 처리.
    /// </summary>
    public static class M2ContentSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M2 Content")]
        public static void SeedM2Content()
        {
            EnsureFolders();

            // ─── 14개 일반 특산물 ─────────────────────────────────────────────
            // CONTENT_DESIGN.md §2.3 의 일반(common) 특산물만. 스페셜은 M3.

            var oliveOil = CreateOrLoadProduct("Product_OliveOil.asset", p =>
            {
                p.productId = "product.olive_oil";
                p.displayNameKo = "올리브유";
                p.basePrice = 40;
                p.shortDescription = "올리브 열매를 짜서 만든 노란 기름이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/올리브유";
            });

            var orange = CreateOrLoadProduct("Product_Orange.asset", p =>
            {
                p.productId = "product.orange";
                p.displayNameKo = "세비야 오렌지";
                p.basePrice = 25;
                p.shortDescription = "달콤하고 새콤한 둥근 과일이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/오렌지";
            });

            var muranoGlass = CreateOrLoadProduct("Product_MuranoGlass.asset", p =>
            {
                p.productId = "product.murano_glass";
                p.displayNameKo = "무라노 유리";
                p.basePrice = 80;
                p.shortDescription = "베네치아 옆 작은 섬에서만 만든 색깔 유리예요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/무라노";
            });

            var pepper = CreateOrLoadProduct("Product_Pepper.asset", p =>
            {
                p.productId = "product.pepper";
                p.displayNameKo = "후추";
                p.basePrice = 120;
                p.shortDescription = "음식의 맛을 매콤하게 해 주는 작고 까만 알갱이예요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/후추";
            });

            var dutchCheese = CreateOrLoadProduct("Product_DutchCheese.asset", p =>
            {
                p.productId = "product.dutch_cheese";
                p.displayNameKo = "네덜란드 치즈";
                p.basePrice = 50;
                p.shortDescription = "둥글고 노란, 우유로 만든 음식이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/네덜란드_치즈";
            });

            var tulip = CreateOrLoadProduct("Product_Tulip.asset", p =>
            {
                p.productId = "product.tulip";
                p.displayNameKo = "튤립";
                p.basePrice = 60;
                p.shortDescription = "봄에 피는 큰 꽃이에요. 옛날엔 금처럼 비쌌대요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/튤립";
            });

            var wool = CreateOrLoadProduct("Product_Wool.asset", p =>
            {
                p.productId = "product.wool";
                p.displayNameKo = "양털";
                p.basePrice = 35;
                p.shortDescription = "양에서 깎아낸 따뜻한 털이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/양털";
            });

            var tin = CreateOrLoadProduct("Product_Tin.asset", p =>
            {
                p.productId = "product.tin";
                p.displayNameKo = "주석";
                p.basePrice = 70;
                p.shortDescription = "잘 휘어지는 은빛 금속이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/주석_(원소)";
            });

            var persianCarpet = CreateOrLoadProduct("Product_PersianCarpet.asset", p =>
            {
                p.productId = "product.persian_carpet";
                p.displayNameKo = "페르시아 양탄자";
                p.basePrice = 200;
                p.shortDescription = "예쁜 무늬가 가득한 두툼한 깔개예요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/페르시아_양탄자";
            });

            var spiceClove = CreateOrLoadProduct("Product_SpiceClove.asset", p =>
            {
                p.productId = "product.spice_clove";
                p.displayNameKo = "정향";
                p.basePrice = 100;
                p.shortDescription = "꽃봉오리를 말린 향기로운 양념이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/정향";
            });

            var celadon = CreateOrLoadProduct("Product_Celadon.asset", p =>
            {
                p.productId = "product.celadon";
                p.displayNameKo = "청자";
                p.basePrice = 90;
                p.shortDescription = "푸르스름한 색이 도는 우리나라 도자기예요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/청자";
            });

            var ginseng = CreateOrLoadProduct("Product_Ginseng.asset", p =>
            {
                p.productId = "product.ginseng";
                p.displayNameKo = "인삼";
                p.basePrice = 150;
                p.shortDescription = "몸에 좋다고 알려진 우리나라 약초예요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/인삼";
            });

            var silk = CreateOrLoadProduct("Product_Silk.asset", p =>
            {
                p.productId = "product.silk";
                p.displayNameKo = "비단";
                p.basePrice = 110;
                p.shortDescription = "누에가 만들어 주는 부드럽고 반짝이는 천이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/비단";
            });

            var porcelain = CreateOrLoadProduct("Product_Porcelain.asset", p =>
            {
                p.productId = "product.porcelain";
                p.displayNameKo = "도자기";
                p.basePrice = 95;
                p.shortDescription = "흰 흙으로 구워 만든 단단하고 예쁜 그릇이에요.";
                p.sourceUrl = "https://ko.wikipedia.org/wiki/도자기";
            });

            // ─── 7개 시작 항구 (nation 은 4단계에서 연결) ───────────────────

            var sevilla = CreateOrLoadPort("Port_Sevilla.asset", p =>
            {
                p.portId = "port.sevilla";
                p.displayNameKo = "세비야";
                p.displayNameOriginal = "Sevilla";
                p.latitude = 37.4f;
                p.longitude = -5.9f;
                p.shortDescription = "큰 강을 따라 새 땅의 보물이 모이는 도시.";
                p.commonProducts = new[] { oliveOil, orange };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/세비야";
            });

            var venezia = CreateOrLoadPort("Port_Venezia.asset", p =>
            {
                p.portId = "port.venezia";
                p.displayNameKo = "베네치아";
                p.displayNameOriginal = "Venezia";
                p.latitude = 45.4f;
                p.longitude = 12.3f;
                p.shortDescription = "물길이 거리가 된 신기한 도시.";
                p.commonProducts = new[] { muranoGlass, pepper };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/베네치아";
            });

            var amsterdam = CreateOrLoadPort("Port_Amsterdam.asset", p =>
            {
                p.portId = "port.amsterdam";
                p.displayNameKo = "암스테르담";
                p.displayNameOriginal = "Amsterdam";
                p.latitude = 52.4f;
                p.longitude = 4.9f;
                p.shortDescription = "운하 위로 배들이 쉴 새 없이 오가는 도시.";
                p.commonProducts = new[] { dutchCheese, tulip };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/암스테르담";
            });

            var london = CreateOrLoadPort("Port_London.asset", p =>
            {
                p.portId = "port.london";
                p.displayNameKo = "런던";
                p.displayNameOriginal = "London";
                p.latitude = 51.5f;
                p.longitude = -0.1f;
                p.shortDescription = "안개 낀 큰 강가에서 배들이 출발하는 도시.";
                p.commonProducts = new[] { wool, tin };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/런던";
            });

            var istanbul = CreateOrLoadPort("Port_Istanbul.asset", p =>
            {
                p.portId = "port.istanbul";
                p.displayNameKo = "이스탄불";
                p.displayNameOriginal = "İstanbul";
                p.latitude = 41.0f;
                p.longitude = 28.9f;
                p.shortDescription = "동쪽과 서쪽 사이의 큰 시장이 열리는 도시.";
                p.commonProducts = new[] { persianCarpet, spiceClove };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/이스탄불";
            });

            var busan = CreateOrLoadPort("Port_Busan.asset", p =>
            {
                p.portId = "port.busan";
                p.displayNameKo = "부산";
                p.displayNameOriginal = "釜山";
                p.latitude = 35.1f;
                p.longitude = 129.0f;
                p.shortDescription = "동쪽 바다로 나가는 우리나라 큰 항구.";
                p.commonProducts = new[] { celadon, ginseng };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/부산광역시";
            });

            var guangzhou = CreateOrLoadPort("Port_Guangzhou.asset", p =>
            {
                p.portId = "port.guangzhou";
                p.displayNameKo = "광저우";
                p.displayNameOriginal = "廣州";
                p.latitude = 23.1f;
                p.longitude = 113.3f;
                p.shortDescription = "남쪽 큰 강 어귀에서 도자기와 비단이 모이는 도시.";
                p.commonProducts = new[] { porcelain, silk };
                p.specialProducts = Array.Empty<ProductData>();
                p.sourceUrl = "https://ko.wikipedia.org/wiki/광저우";
            });

            // ─── 7개 국가 ──────────────────────────────────────────────────

            var spain = CreateOrLoadNation("Nation_Spain.asset", n =>
            {
                n.nationId = "nation.spain";
                n.displayNameKo = "스페인";
                n.displayNameOriginal = "España";
                n.accentColor = new Color(0.78f, 0.10f, 0.10f); // 진한 빨강
                n.startingPort = sevilla;
                n.startingYear = 1492;
                n.shortIntro = "큰 바다 너머 새로운 땅을 처음 만난 나라예요.";
                n.greeting = "환영합니다, 어린 항해사! 우리 스페인은 큰 바다를 건너 새 땅을 만났답니다. 이번엔 어느 곳을 만나러 갈까요?";
                n.designerNotes = "콜럼버스의 항해, 신대륙과의 만남, 갈레온선.";
                n.sourceUrl = "https://ko.wikipedia.org/wiki/스페인_제국";
            });

            var italy = CreateOrLoadNation("Nation_Italy.asset", n =>
            {
                n.nationId = "nation.italy";
                n.displayNameKo = "이탈리아";
                n.displayNameOriginal = "Italia";
                n.accentColor = new Color(0.35f, 0.10f, 0.55f); // 짙은 보라
                n.startingPort = venezia;
                n.startingYear = 1450;
                n.shortIntro = "물 위에 떠 있는 신기한 도시에서 바다로 나가는 나라예요.";
                n.greeting = "안녕하세요! 우리 베네치아는 물길이 거리가 된 도시랍니다. 함께 동쪽으로 가는 길을 찾아볼까요?";
                n.designerNotes = "베네치아 공화국, 지중해 향신료 무역, 갤리선, 마르코 폴로.";
                n.sourceUrl = "https://ko.wikipedia.org/wiki/베네치아_공화국";
            });

            var netherlands = CreateOrLoadNation("Nation_Netherlands.asset", n =>
            {
                n.nationId = "nation.netherlands";
                n.displayNameKo = "네덜란드";
                n.displayNameOriginal = "Nederland";
                n.accentColor = new Color(1.00f, 0.55f, 0.00f); // 오렌지
                n.startingPort = amsterdam;
                n.startingYear = 1602;
                n.shortIntro = "작지만 가장 많은 배를 가진 바다의 나라예요.";
                n.greeting = "어서 와요! 우리 네덜란드는 작은 나라지만, 배는 누구보다 많이 가지고 있답니다. 함께 멀리까지 가 볼까요?";
                n.designerNotes = "네덜란드 동인도회사(VOC), 운하 도시, 풍차, 튤립.";
                n.sourceUrl = "https://ko.wikipedia.org/wiki/네덜란드_동인도_회사";
            });

            var england = CreateOrLoadNation("Nation_England.asset", n =>
            {
                n.nationId = "nation.england";
                n.displayNameKo = "영국";
                n.displayNameOriginal = "England";
                n.accentColor = new Color(0.10f, 0.20f, 0.55f); // 짙은 파랑
                n.startingPort = london;
                n.startingYear = 1580;
                n.shortIntro = "안개 낀 섬에서 큰 바다로 나간 나라예요.";
                n.greeting = "어서 와요! 우리 영국은 안개 가득한 섬나라랍니다. 작은 배를 타고 같이 새로운 곳을 찾아봐요!";
                n.designerNotes = "영국 동인도회사(EIC), 엘리자베스 1세, 후발 주자.";
                n.sourceUrl = "https://ko.wikipedia.org/wiki/영국_동인도_회사";
            });

            var ottoman = CreateOrLoadNation("Nation_Ottoman.asset", n =>
            {
                n.nationId = "nation.ottoman";
                n.displayNameKo = "오스만";
                n.displayNameOriginal = "Osmanlı";
                n.accentColor = new Color(0.00f, 0.55f, 0.55f); // 청록
                n.startingPort = istanbul;
                n.startingYear = 1470;
                n.shortIntro = "두 대륙이 만나는 항구를 가진 큰 나라예요.";
                n.greeting = "어서 오세요! 우리 오스만은 동쪽과 서쪽이 만나는 곳에 있어요. 어떤 신기한 물건들을 보러 갈까요?";
                n.designerNotes = "콘스탄티노폴리스 정복, 동지중해·흑해 무역, 향신료 육로.";
                n.sourceUrl = "https://ko.wikipedia.org/wiki/오스만_제국";
            });

            var joseon = CreateOrLoadNation("Nation_Joseon.asset", n =>
            {
                n.nationId = "nation.joseon";
                n.displayNameKo = "조선";
                n.displayNameOriginal = "朝鮮";
                n.accentColor = new Color(0.60f, 0.85f, 0.90f); // 청자색
                n.startingPort = busan;
                n.startingYear = 1450;
                n.shortIntro = "동쪽 바다 작은 반도에서 이웃 나라로 떠나는 나라예요.";
                n.greeting = "어서 와요, 어린 항해사! 우리 조선은 동쪽의 작은 반도예요. 이웃 나라로 가서 무엇이 있는지 함께 살펴볼까요?";
                n.designerNotes = "한글 창제 시기, 일본·명나라 통신사, 청자, 인삼.";
                n.sourceUrl = "https://ko.wikipedia.org/wiki/조선";
            });

            var china = CreateOrLoadNation("Nation_China.asset", n =>
            {
                n.nationId = "nation.china";
                n.displayNameKo = "중국";
                n.displayNameOriginal = "中國";
                n.accentColor = new Color(0.85f, 0.10f, 0.10f); // 진한 빨강 + 금색 톤
                n.startingPort = guangzhou;
                n.startingYear = 1420;
                n.shortIntro = "가장 큰 배를 만들어 멀리 바다를 누볐던 나라예요.";
                n.greeting = "안녕하세요! 우리는 아주 큰 배를 만들 줄 알아요. 함께 어디까지 갈 수 있는지 떠나 볼까요?";
                n.designerNotes = "명나라, 정화의 일곱 차례 원정, 보선, 도자기·차·비단.";
                n.sourceUrl = "https://ko.wikipedia.org/wiki/정화_(명나라)";
            });

            // ─── 4단계: 항구 ↔ 국가 양방향 연결 ─────────────────────────────

            LinkPortToNation(sevilla, spain);
            LinkPortToNation(venezia, italy);
            LinkPortToNation(amsterdam, netherlands);
            LinkPortToNation(london, england);
            LinkPortToNation(istanbul, ottoman);
            LinkPortToNation(busan, joseon);
            LinkPortToNation(guangzhou, china);

            // 특산물의 originPort 도 연결
            SetOriginPort(oliveOil, sevilla);
            SetOriginPort(orange, sevilla);
            SetOriginPort(muranoGlass, venezia);
            SetOriginPort(pepper, venezia);
            SetOriginPort(dutchCheese, amsterdam);
            SetOriginPort(tulip, amsterdam);
            SetOriginPort(wool, london);
            SetOriginPort(tin, london);
            SetOriginPort(persianCarpet, istanbul);
            SetOriginPort(spiceClove, istanbul);
            SetOriginPort(celadon, busan);
            SetOriginPort(ginseng, busan);
            SetOriginPort(silk, guangzhou);
            SetOriginPort(porcelain, guangzhou);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M2Seeder] 완료. M2 콘텐츠 시드 추가 28개:\n" +
                "  • 7개 NationData (스페인 / 이탈리아 / 네덜란드 / 영국 / 오스만 / 조선 / 중국)\n" +
                "  • 7개 PortData (세비야 / 베네치아 / 암스테르담 / 런던 / 이스탄불 / 부산 / 광저우)\n" +
                "  • 14개 ProductData (각 항구의 일반 특산물 2종씩)\n" +
                "\nM1 의 포르투갈/리스본/세우타 와 합쳐 총 8개국 + 9개 항구 + 17개 일반 특산물 완성.\n" +
                "다음 작업: SeaWorldManager 의 Active Ports 배열에 신규 항구 추가, 메인 메뉴 국적 선택 UI.");
        }

        // ─── 헬퍼 ──────────────────────────────────────────────────────────────

        private static void EnsureFolders()
        {
            string[] subFolders = { "Nations", "Ports", "Products" };
            foreach (var sub in subFolders) EnsureFolder($"{DataRoot}/{sub}");
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

        private static T CreateOrLoad<T>(string path, Action<T> setup) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                Debug.Log($"[M2Seeder] Skipping (exists): {path}");
                return existing;
            }
            var so = ScriptableObject.CreateInstance<T>();
            setup(so);
            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[M2Seeder] Created: {path}");
            return so;
        }

        private static ProductData CreateOrLoadProduct(string fileName, Action<ProductData> setup) =>
            CreateOrLoad<ProductData>($"{DataRoot}/Products/{fileName}", setup);

        private static PortData CreateOrLoadPort(string fileName, Action<PortData> setup) =>
            CreateOrLoad<PortData>($"{DataRoot}/Ports/{fileName}", setup);

        private static NationData CreateOrLoadNation(string fileName, Action<NationData> setup) =>
            CreateOrLoad<NationData>($"{DataRoot}/Nations/{fileName}", setup);

        private static void LinkPortToNation(PortData port, NationData nation)
        {
            if (port == null || nation == null) return;
            if (port.nation != nation)
            {
                port.nation = nation;
                EditorUtility.SetDirty(port);
            }
        }

        private static void SetOriginPort(ProductData product, PortData port)
        {
            if (product == null || port == null) return;
            if (product.originPort != port)
            {
                product.originPort = port;
                EditorUtility.SetDirty(product);
            }
        }
    }
}
