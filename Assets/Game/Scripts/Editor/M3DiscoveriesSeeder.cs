using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M3.2 — 발견물 카테고리 균형. 기존은 Landmark 위주라 FloraFauna/Ruin/Event 보강.
    /// 카테고리별 최소 5개 (M3 작업 항목) 달성.
    ///
    /// 메뉴: Game/Seed M3 Discoveries
    ///
    /// 신규 발견물 15개:
    ///   • FloraFauna 5: 스라소니, 퍼핀, 호랑이(조선), 양쯔강 악어, 나일 악어
    ///   • Ruin 5: 기자 피라미드, 알렉산드리아 등대, 카르타고 유적, 에페소스, 폼페이
    ///   • Event 5: 콘스탄티노폴리스 함락, 콜럼버스 출항, 사그레스 항해학교, 정화 원정, 한산도 해전
    ///
    /// 이미 존재하는 .asset 은 건드리지 않음 (재실행 안전).
    /// 새 발견물 추가 후 Game ▸ Refresh All Catalogs 도 같이 실행 권장.
    /// </summary>
    public static class M3DiscoveriesSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M3 Discoveries")]
        public static void SeedM3Discoveries()
        {
            EnsureFolder($"{DataRoot}/Discoveries");

            var portugal    = LoadNation("Nation_Portugal");
            var spain       = LoadNation("Nation_Spain");
            var italy       = LoadNation("Nation_Italy");
            var england     = LoadNation("Nation_England");
            var ottoman     = LoadNation("Nation_Ottoman");
            var joseon      = LoadNation("Nation_Joseon");
            var china       = LoadNation("Nation_China");

            // ─── FloraFauna 5개 ───────────────────────────────────────────

            CreateOrLoadDiscovery("Discovery_IberianLynx.asset", d =>
            {
                d.discoveryId = "disc.iberian_lynx";
                d.displayNameKo = "스라소니";
                d.category = DiscoveryCategory.FloraFauna;
                d.latitude = 37.0f;
                d.longitude = -6.5f;
                d.searchToleranceBase = 0.04f;
                d.mainDescription =
                    "이베리아 반도 남쪽 갈대밭에 사는 줄무늬 들고양이예요. 뾰족한 귀끝 털과 짧은 꼬리가 특징이에요. 토끼를 잘 잡는다고 옛날 사람들 사이에서 이름이 났답니다.";
                d.moreInfo = "지금은 세계에서 가장 보기 힘든 들고양이 중 하나래요.";
                d.eraLabel = "역사 내내";
                d.relatedNation = spain;
                d.relatedFigures = "";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/이베리아스라소니";
                d.sensitiveExpressionChecked = true;
            });

            CreateOrLoadDiscovery("Discovery_AtlanticPuffin.asset", d =>
            {
                d.discoveryId = "disc.atlantic_puffin";
                d.displayNameKo = "대서양 퍼핀";
                d.category = DiscoveryCategory.FloraFauna;
                d.latitude = 50.55f;
                d.longitude = -4.95f;
                d.searchToleranceBase = 0.04f;
                d.mainDescription =
                    "콘월 바닷가 절벽에 사는 둥근 얼굴의 작은 새예요. 부리가 알록달록한 빨강과 노랑이라 '바다 앵무새' 라고 불리기도 했답니다.";
                d.moreInfo = "물고기를 한 입에 여러 마리 물고 다녀요.";
                d.eraLabel = "역사 내내";
                d.relatedNation = england;
                d.relatedFigures = "";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/대서양퍼핀";
                d.sensitiveExpressionChecked = true;
            });

            CreateOrLoadDiscovery("Discovery_KoreanTiger.asset", d =>
            {
                d.discoveryId = "disc.korean_tiger";
                d.displayNameKo = "조선 호랑이";
                d.category = DiscoveryCategory.FloraFauna;
                d.latitude = 34.95f;
                d.longitude = 127.5f;
                d.searchToleranceBase = 0.04f;
                d.mainDescription =
                    "옛 조선의 산과 들에서 흔히 만날 수 있던 큰 줄무늬 짐승이에요. 임금님 그림과 옛이야기 속에 자주 등장했답니다.";
                d.moreInfo = "지금은 한반도에서는 거의 만나기 어려워요.";
                d.eraLabel = "조선 시대";
                d.relatedNation = joseon;
                d.relatedFigures = "";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/한국호랑이";
                d.sensitiveExpressionChecked = true;
            });

            CreateOrLoadDiscovery("Discovery_ChineseAlligator.asset", d =>
            {
                d.discoveryId = "disc.chinese_alligator";
                d.displayNameKo = "양쯔강 악어";
                d.category = DiscoveryCategory.FloraFauna;
                d.latitude = 30.7f;
                d.longitude = 121.4f;
                d.searchToleranceBase = 0.04f;
                d.mainDescription =
                    "중국 큰 강 어귀의 갈대밭에 숨어 사는 짧은 악어예요. 다른 나라 악어들보다 작고, 차가운 겨울엔 굴 속에서 잠들어요.";
                d.moreInfo = "옛 사람들은 '용의 새끼' 라고 부르기도 했답니다.";
                d.eraLabel = "역사 내내";
                d.relatedNation = china;
                d.relatedFigures = "";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/양쯔강악어";
                d.sensitiveExpressionChecked = true;
            });

            CreateOrLoadDiscovery("Discovery_NileCrocodile.asset", d =>
            {
                d.discoveryId = "disc.nile_crocodile";
                d.displayNameKo = "나일 악어";
                d.category = DiscoveryCategory.FloraFauna;
                d.latitude = 30.5f;
                d.longitude = 31.0f;
                d.searchToleranceBase = 0.04f;
                d.mainDescription =
                    "이집트의 큰 강에서 헤엄치는 크고 무서운 악어예요. 옛 이집트에서는 신의 모습으로 받들어지기도 했답니다.";
                d.moreInfo = "튼튼한 이빨로 큰 동물도 잡아요.";
                d.eraLabel = "역사 내내";
                d.relatedNation = ottoman;
                d.relatedFigures = "";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/나일악어";
                d.sensitiveExpressionChecked = true;
            });

            // ─── Ruin 5개 ─────────────────────────────────────────────────

            CreateOrLoadDiscovery("Discovery_PyramidsGiza.asset", d =>
            {
                d.discoveryId = "disc.pyramids_giza";
                d.displayNameKo = "기자 피라미드";
                d.category = DiscoveryCategory.Ruin;
                d.latitude = 30.05f;
                d.longitude = 31.0f;
                d.searchToleranceBase = 0.03f;
                d.mainDescription =
                    "이집트의 모래 위에 우뚝 솟은 거대한 돌무덤이에요. 4500년 전 옛 왕들을 위해 만든 거예요. 멀리서도 한눈에 보일 만큼 커요.";
                d.moreInfo = "어떻게 그렇게 큰 돌을 쌓았는지 지금도 신기해요.";
                d.eraLabel = "기원전 2500년경";
                d.relatedNation = ottoman;
                d.relatedFigures = "쿠푸 왕";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/기자_피라미드";
                d.sensitiveExpressionChecked = true;
            });

            CreateOrLoadDiscovery("Discovery_LighthouseAlexandria.asset", d =>
            {
                d.discoveryId = "disc.lighthouse_alexandria";
                d.displayNameKo = "알렉산드리아 등대";
                d.category = DiscoveryCategory.Ruin;
                d.latitude = 31.21f;
                d.longitude = 29.89f;
                d.searchToleranceBase = 0.03f;
                d.mainDescription =
                    "옛 세계 7대 불가사의 중 하나였던 큰 등대예요. 밤이면 큰 불이 켜져 멀리서 오는 배들의 길잡이가 되었답니다. 지진으로 무너져 지금은 흔적만 남아 있어요.";
                d.moreInfo = "거의 천오백 년 동안 바다를 비추었어요.";
                d.eraLabel = "기원전 280년경 ~ 1480년경";
                d.relatedNation = ottoman;
                d.relatedFigures = "프톨레마이오스 1세, 소스트라투스";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/알렉산드리아의_등대";
                d.sensitiveExpressionChecked = true;
            });

            CreateOrLoadDiscovery("Discovery_CarthageRuins.asset", d =>
            {
                d.discoveryId = "disc.carthage_ruins";
                d.displayNameKo = "카르타고 유적";
                d.category = DiscoveryCategory.Ruin;
                d.latitude = 36.85f;
                d.longitude = 10.32f;
                d.searchToleranceBase = 0.04f;
                d.mainDescription =
                    "옛 북아프리카 바닷가에 있던 큰 도시의 흔적이에요. 한때 로마와 다투던 강한 나라였어요. 지금은 무너진 돌기둥과 신전 자리가 남아 있답니다.";
                d.moreInfo = "코끼리를 타고 산을 넘은 한니발 장군이 살던 곳이래요.";
                d.eraLabel = "기원전 814년 ~ 기원전 146년";
                d.relatedNation = null;
                d.relatedFigures = "한니발 (Hannibal)";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/카르타고";
                d.sensitiveExpressionChecked = true;
            });

            CreateOrLoadDiscovery("Discovery_Ephesus.asset", d =>
            {
                d.discoveryId = "disc.ephesus";
                d.displayNameKo = "에페소스 유적";
                d.category = DiscoveryCategory.Ruin;
                d.latitude = 37.94f;
                d.longitude = 27.34f;
                d.searchToleranceBase = 0.04f;
                d.mainDescription =
                    "오스만 땅의 바닷가 가까이에 있던 옛 그리스·로마 도시예요. 큰 도서관과 신전이 있던 멋진 도시였답니다. 오랜 세월 흙에 묻혔다가 다시 모습을 보이고 있어요.";
                d.moreInfo = "옛 세계 7대 불가사의 중 하나인 아르테미스 신전이 있던 곳이에요.";
                d.eraLabel = "기원전 10세기 ~ 기원후 15세기";
                d.relatedNation = ottoman;
                d.relatedFigures = "";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/에페소스";
                d.sensitiveExpressionChecked = true;
            });

            CreateOrLoadDiscovery("Discovery_Pompeii.asset", d =>
            {
                d.discoveryId = "disc.pompeii";
                d.displayNameKo = "폼페이";
                d.category = DiscoveryCategory.Ruin;
                d.latitude = 40.75f;
                d.longitude = 14.49f;
                d.searchToleranceBase = 0.04f;
                d.mainDescription =
                    "이탈리아 남쪽 화산 아래 묻혀 있던 옛 로마 도시예요. 한순간에 재로 덮여 그대로 굳어 버렸답니다. 그 덕분에 옛날 사람들의 모습이 그대로 남아 있어요.";
                d.moreInfo = "오랜 시간 잊혀 있다가 우연히 다시 발견되었어요.";
                d.eraLabel = "기원후 79년 (베수비오 화산 폭발)";
                d.relatedNation = italy;
                d.relatedFigures = "";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/폼페이";
                d.sensitiveExpressionChecked = true;
            });

            // ─── Event 5개 ────────────────────────────────────────────────

            CreateOrLoadDiscovery("Discovery_FallConstantinople.asset", d =>
            {
                d.discoveryId = "disc.fall_constantinople";
                d.displayNameKo = "콘스탄티노폴리스 함락";
                d.category = DiscoveryCategory.Event;
                d.latitude = 41.02f;
                d.longitude = 28.95f;
                d.searchToleranceBase = 0.03f;
                d.mainDescription =
                    "1453년, 오스만의 젊은 왕이 천 년이 넘는 큰 도시 콘스탄티노폴리스를 차지한 사건이에요. 이때부터 도시는 이스탄불이라 불리게 되었답니다.";
                d.moreInfo = "이 일을 두고 어떤 사람은 새로운 시대의 시작이라 불러요.";
                d.eraLabel = "1453년";
                d.relatedNation = ottoman;
                d.relatedFigures = "메흐메드 2세 (Mehmed II)";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/콘스탄티노폴리스의_함락";
                d.sensitiveExpressionChecked = true;
            });

            CreateOrLoadDiscovery("Discovery_ColumbusDeparture.asset", d =>
            {
                d.discoveryId = "disc.columbus_departure";
                d.displayNameKo = "콜럼버스 출항";
                d.category = DiscoveryCategory.Event;
                d.latitude = 37.23f;
                d.longitude = -6.90f;
                d.searchToleranceBase = 0.04f;
                d.mainDescription =
                    "1492년 봄, 콜럼버스가 작은 배 세 척을 이끌고 스페인 항구를 떠나 큰 바다를 건너간 일이에요. 가다 보니 옛 사람들이 모르던 새 땅을 만났답니다.";
                d.moreInfo = "그가 만난 땅은 사실 인도가 아니라 아메리카였어요.";
                d.eraLabel = "1492년";
                d.relatedNation = spain;
                d.relatedFigures = "크리스토퍼 콜럼버스 (Cristóbal Colón)";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/크리스토퍼_콜럼버스";
                d.sensitiveExpressionChecked = true;
            });

            CreateOrLoadDiscovery("Discovery_SagresSchool.asset", d =>
            {
                d.discoveryId = "disc.sagres_school";
                d.displayNameKo = "사그레스 항해학교";
                d.category = DiscoveryCategory.Event;
                d.latitude = 37.0f;
                d.longitude = -8.95f;
                d.searchToleranceBase = 0.04f;
                d.mainDescription =
                    "포르투갈 남서쪽 끝 작은 마을에서 항해사 엔히크 왕자가 세운 배움터예요. 별을 보는 법, 바람을 읽는 법을 가르쳤답니다. 여기서 배운 뱃사람들이 멀고 새로운 바다로 나섰어요.";
                d.moreInfo = "대항해시대의 시작점 중 하나로 꼽혀요.";
                d.eraLabel = "1419년경";
                d.relatedNation = portugal;
                d.relatedFigures = "엔히크 왕자 (Infante D. Henrique)";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/엔히크_왕자";
                d.sensitiveExpressionChecked = true;
            });

            CreateOrLoadDiscovery("Discovery_ZhengHe.asset", d =>
            {
                d.discoveryId = "disc.zheng_he";
                d.displayNameKo = "정화의 대원정";
                d.category = DiscoveryCategory.Event;
                d.latitude = 25.0f;
                d.longitude = 118.7f;
                d.searchToleranceBase = 0.04f;
                d.mainDescription =
                    "1405년부터 명나라 정화가 거대한 보물선 여러 척을 이끌고 멀리 인도와 아프리카까지 다녀온 항해예요. 유럽 사람들보다 훨씬 먼 바다를 먼저 다녀왔답니다.";
                d.moreInfo = "정화의 배는 콜럼버스의 배보다 다섯 배 가까이 컸어요.";
                d.eraLabel = "1405년 ~ 1433년";
                d.relatedNation = china;
                d.relatedFigures = "정화 (鄭和)";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/정화_(명나라)";
                d.sensitiveExpressionChecked = true;
            });

            CreateOrLoadDiscovery("Discovery_HansandoBattle.asset", d =>
            {
                d.discoveryId = "disc.hansando_battle";
                d.displayNameKo = "한산도 대첩";
                d.category = DiscoveryCategory.Event;
                d.latitude = 34.78f;
                d.longitude = 128.42f;
                d.searchToleranceBase = 0.04f;
                d.mainDescription =
                    "1592년 한산도 앞바다에서 이순신 장군이 거북선을 앞세워 큰 함대를 막아낸 바다 싸움이에요. 일본 수군 대부분이 이날 무너졌답니다.";
                d.moreInfo = "이순신 장군의 학익진(鶴翼陣) 전법으로 유명해요.";
                d.eraLabel = "1592년";
                d.relatedNation = joseon;
                d.relatedFigures = "이순신 (李舜臣)";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/한산도_대첩";
                d.sensitiveExpressionChecked = true;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M3DiscoveriesSeeder] 완료. 15개 발견물 추가 (카테고리 균형).\n" +
                "  • FloraFauna(5): 스라소니, 퍼핀, 조선 호랑이, 양쯔강 악어, 나일 악어\n" +
                "  • Ruin(5): 기자 피라미드, 알렉산드리아 등대, 카르타고, 에페소스, 폼페이\n" +
                "  • Event(5): 콘스탄티노폴리스 함락, 콜럼버스 출항, 사그레스 학교, 정화 원정, 한산도 대첩\n" +
                "\n다음 단계: Game ▸ Refresh All Catalogs 실행 → DiscoveryCatalog 자동 갱신.");
        }

        // ─── 헬퍼 ───────────────────────────────────────────────────────────

        private static NationData LoadNation(string baseName)
        {
            return AssetDatabase.LoadAssetAtPath<NationData>($"{DataRoot}/Nations/{baseName}.asset");
        }

        private static DiscoveryData CreateOrLoadDiscovery(string fileName, Action<DiscoveryData> setup)
        {
            var path = $"{DataRoot}/Discoveries/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<DiscoveryData>(path);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<DiscoveryData>();
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
