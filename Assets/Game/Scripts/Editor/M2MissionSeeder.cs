using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M2.4 — 다른 7개국 시작 항구의 발견물 + 발견물 의뢰 시드.
    /// M1 (포르투갈/리스본/지브롤터) 과 합쳐 8개국 모두 발견물 의뢰 체험 가능.
    ///
    /// 메뉴: Game/Seed M2 Missions
    ///
    /// 생성:
    ///   - 8개 발견물 (Discovery_*) — CONTENT_DESIGN.md §2.4 시드
    ///     • 세우타→보자도르 곶
    ///     • 세비야→카나리아 제도
    ///     • 베네치아→푸른 동굴
    ///     • 암스테르담→텍셀 섬 바다표범
    ///     • 런던→도버의 흰 절벽
    ///     • 이스탄불→보스포루스 해협
    ///     • 부산→한라산
    ///     • 광저우→주강 삼각주
    ///   - 8개 의뢰 (Mission_*) — CONTENT_DESIGN.md §2.7 시드
    ///
    /// 이미 존재하는 .asset 은 건드리지 않음 (재실행 안전).
    /// </summary>
    public static class M2MissionSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M2 Missions")]
        public static void SeedM2Missions()
        {
            EnsureFolder($"{DataRoot}/Discoveries");
            EnsureFolder($"{DataRoot}/Missions");

            // ─── 국가·항구 참조 로드 (M1·M2.1 시드 전제) ────────────────────

            var portugal = LoadNation("Nation_Portugal");
            var spain = LoadNation("Nation_Spain");
            var italy = LoadNation("Nation_Italy");
            var netherlands = LoadNation("Nation_Netherlands");
            var england = LoadNation("Nation_England");
            var ottoman = LoadNation("Nation_Ottoman");
            var joseon = LoadNation("Nation_Joseon");
            var china = LoadNation("Nation_China");

            var ceuta = LoadPort("Port_Ceuta");
            var sevilla = LoadPort("Port_Sevilla");
            var venezia = LoadPort("Port_Venezia");
            var amsterdam = LoadPort("Port_Amsterdam");
            var london = LoadPort("Port_London");
            var istanbul = LoadPort("Port_Istanbul");
            var busan = LoadPort("Port_Busan");
            var guangzhou = LoadPort("Port_Guangzhou");

            // ─── 8개 발견물 ────────────────────────────────────────────────

            var capeBojador = CreateOrLoadDiscovery("Discovery_CapeBojador.asset", d =>
            {
                d.discoveryId = "disc.cape_bojador";
                d.displayNameKo = "보자도르 곶";
                d.category = DiscoveryCategory.Event;
                d.latitude = 26.1f;
                d.longitude = -14.5f;
                d.searchToleranceBase = 0.03f;
                d.mainDescription =
                    "서아프리카 바다에 있는 무서운 소문이 돌던 곶이에요. 옛 뱃사람들은 '여기를 지나면 다시 못 돌아온다' 고 믿었어요. 1434년 포르투갈의 질 이아네스라는 항해사가 처음으로 이곳을 무사히 지나갔답니다.";
                d.moreInfo = "그 뒤로 더 멀리 가는 항해가 시작되었어요.";
                d.eraLabel = "1434 (질 이아네스)";
                d.relatedNation = portugal;
                d.relatedFigures = "질 이아네스 (Gil Eanes)";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/보자도르_곶";
                d.sensitiveExpressionChecked = true;
            });

            var canaryIslands = CreateOrLoadDiscovery("Discovery_CanaryIslands.asset", d =>
            {
                d.discoveryId = "disc.canary_islands";
                d.displayNameKo = "카나리아 제도";
                d.category = DiscoveryCategory.Landmark;
                d.latitude = 28.3f;
                d.longitude = -16.6f;
                d.searchToleranceBase = 0.03f;
                d.mainDescription =
                    "스페인에서 큰 바다로 가기 전에 들르는 일곱 개의 따뜻한 섬이에요. 햇볕이 좋아 바다거북도 살고, 화산 모양 산도 있어요. 콜럼버스도 큰 바다로 떠나기 전 이곳에 잠깐 머물렀답니다.";
                d.moreInfo = "섬마다 다른 화산이 있고, 바람을 따라 새들이 많이 모여 살아요.";
                d.eraLabel = "15세기 (정복 완료 1496)";
                d.relatedNation = spain;
                d.relatedFigures = "1492 콜럼버스의 첫 항해 중간 기착지";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/카나리아_제도";
                d.sensitiveExpressionChecked = true;
            });

            var blueGrotto = CreateOrLoadDiscovery("Discovery_BlueGrotto.asset", d =>
            {
                d.discoveryId = "disc.blue_grotto";
                d.displayNameKo = "푸른 동굴";
                d.category = DiscoveryCategory.Landmark;
                d.latitude = 40.6f;
                d.longitude = 14.2f;
                d.searchToleranceBase = 0.03f;
                d.mainDescription =
                    "이탈리아 남쪽 작은 섬 옆에 숨어 있는 신비한 동굴이에요. 바닷물이 햇빛을 받아 동굴 안이 온통 푸른빛으로 빛나요. 들어가려면 작은 배에 누워서 가야 해요.";
                d.moreInfo = "옛 로마 사람들도 이 동굴을 알고 즐겼다고 해요.";
                d.eraLabel = "고대부터";
                d.relatedNation = italy;
                d.relatedFigures = "고대 로마 황제 티베리우스의 휴양지 카프리섬";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/푸른_동굴";
                d.sensitiveExpressionChecked = true;
            });

            var texelSeals = CreateOrLoadDiscovery("Discovery_TexelSeals.asset", d =>
            {
                d.discoveryId = "disc.texel_seals";
                d.displayNameKo = "텍셀 섬의 바다표범";
                d.category = DiscoveryCategory.FloraFauna;
                d.latitude = 53.0f;
                d.longitude = 4.8f;
                d.searchToleranceBase = 0.03f;
                d.mainDescription =
                    "네덜란드 북쪽 바다의 작은 섬이에요. 모래 위에서 바다표범들이 햇볕을 쬐며 쉬고 있어요. 가까이 가면 깜짝 놀라서 바다로 풍덩 들어가지요.";
                d.moreInfo = "겨울에는 아기 바다표범도 태어나요. 이곳은 와덴해라는 큰 바다 보호 구역의 일부예요.";
                d.eraLabel = "자연";
                d.relatedNation = netherlands;
                d.relatedFigures = "자연 보호 구역 (와덴해)";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/텍설";
                d.sensitiveExpressionChecked = true;
            });

            var whiteCliffs = CreateOrLoadDiscovery("Discovery_WhiteCliffsDover.asset", d =>
            {
                d.discoveryId = "disc.white_cliffs_of_dover";
                d.displayNameKo = "도버의 흰 절벽";
                d.category = DiscoveryCategory.Landmark;
                d.latitude = 51.1f;
                d.longitude = 1.3f;
                d.searchToleranceBase = 0.03f;
                d.mainDescription =
                    "영국 남쪽 바닷가에 우뚝 솟은 새하얀 절벽이에요. 분필처럼 부드러운 흰 돌로 되어 있어, 멀리서 봐도 눈에 잘 띄지요. 옛날부터 배들이 영국을 알아보는 큰 표지였답니다.";
                d.moreInfo = "맑은 날에는 바다 건너 프랑스가 보여요.";
                d.eraLabel = "자연";
                d.relatedNation = england;
                d.relatedFigures = "영불 해협의 자연 랜드마크";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/도버_절벽";
                d.sensitiveExpressionChecked = true;
            });

            var bosphorus = CreateOrLoadDiscovery("Discovery_BosphorusStrait.asset", d =>
            {
                d.discoveryId = "disc.bosphorus_strait";
                d.displayNameKo = "보스포루스 해협";
                d.category = DiscoveryCategory.Landmark;
                d.latitude = 41.1f;
                d.longitude = 29.1f;
                d.searchToleranceBase = 0.03f;
                d.mainDescription =
                    "이스탄불 한가운데를 지나는 좁은 바닷길이에요. 한쪽은 유럽, 다른 한쪽은 아시아라서, 다리 하나만 건너면 대륙이 바뀐답니다. 옛날부터 배가 가장 많이 오가는 길 중 하나였어요.";
                d.moreInfo = "흑해와 지중해를 이어 주는 좁은 해협이에요.";
                d.eraLabel = "고대부터";
                d.relatedNation = ottoman;
                d.relatedFigures = "1453 콘스탄티노폴리스 함락 이후 오스만의 교통 중심";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/보스포루스_해협";
                d.sensitiveExpressionChecked = true;
            });

            var hallasan = CreateOrLoadDiscovery("Discovery_Hallasan.asset", d =>
            {
                d.discoveryId = "disc.hallasan_mountain";
                d.displayNameKo = "한라산";
                d.category = DiscoveryCategory.Landmark;
                d.latitude = 33.4f;
                d.longitude = 126.5f;
                d.searchToleranceBase = 0.03f;
                d.mainDescription =
                    "우리나라 남쪽 끝 제주에 있는 가장 큰 산이에요. 아주 옛날에는 불을 뿜는 화산이었어요. 꼭대기에는 동그란 호수가 숨어 있답니다.";
                d.moreInfo = "꼭대기 호수는 '백록담' 이라고 해요. 흰 사슴이 마시는 물이라는 이야기가 전해져요.";
                d.eraLabel = "자연";
                d.relatedNation = joseon;
                d.relatedFigures = "조선 시대에도 잘 알려진 자연 명소";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/한라산";
                d.sensitiveExpressionChecked = true;
            });

            var pearlRiver = CreateOrLoadDiscovery("Discovery_PearlRiverDelta.asset", d =>
            {
                d.discoveryId = "disc.pearl_river_delta";
                d.displayNameKo = "주강 삼각주";
                d.category = DiscoveryCategory.Landmark;
                d.latitude = 22.6f;
                d.longitude = 113.4f;
                d.searchToleranceBase = 0.03f;
                d.mainDescription =
                    "중국 남쪽의 큰 강이 바다와 만나는 곳이에요. 강물이 여러 갈래로 갈라져 작은 섬들을 만들어요. 옛날부터 도자기와 비단을 실은 배들이 모이는 큰 시장이었답니다.";
                d.moreInfo = "광저우는 이 강 어귀의 가장 큰 항구예요.";
                d.eraLabel = "자연";
                d.relatedNation = china;
                d.relatedFigures = "명·청 시대 광저우 무역의 중심 길";
                d.sourceUrl = "https://ko.wikipedia.org/wiki/주장강";
                d.sensitiveExpressionChecked = true;
            });

            // ─── 8개 발견물 의뢰 ────────────────────────────────────────────

            CreateOrLoadMission("Mission_DiscCeutaCapeBojador.asset", m =>
            {
                m.missionId = "mission.disc.ceuta.cape_bojador";
                m.issuerPort = ceuta;
                m.targetDiscovery = capeBojador;
                m.rewardMoney = 1500;
                m.rewardGoodReputation = 150;
                m.title = "남쪽 바다의 알 수 없는 곶을 알아봐 주세요";
                m.description = "세우타에서 더 남쪽으로 내려가면, 옛 뱃사람들이 잘 모르던 곶이 있다고 해요. 직접 가서 어떤 곳인지 살펴봐 주세요.";
                m.mapItemName = "보자도르 곶의 지도";
            });

            CreateOrLoadMission("Mission_DiscSevillaCanary.asset", m =>
            {
                m.missionId = "mission.disc.sevilla.canary";
                m.issuerPort = sevilla;
                m.targetDiscovery = canaryIslands;
                m.rewardMoney = 1500;
                m.rewardGoodReputation = 150;
                m.title = "큰 바다로 가기 전, 따뜻한 섬들을 찾아봐요";
                m.description = "세비야에서 남서쪽 큰 바다로 나가면 일곱 개의 따뜻한 섬이 있다고 해요. 그곳을 직접 가 봐 주세요.";
                m.mapItemName = "카나리아 제도로 가는 지도";
            });

            CreateOrLoadMission("Mission_DiscVeneziaBlueGrotto.asset", m =>
            {
                m.missionId = "mission.disc.venezia.blue_grotto";
                m.issuerPort = venezia;
                m.targetDiscovery = blueGrotto;
                m.rewardMoney = 1500;
                m.rewardGoodReputation = 150;
                m.title = "남쪽 바다의 푸른 동굴을 찾아봐요";
                m.description = "베네치아 남쪽 작은 섬 옆에 빛이 푸르게 빛나는 동굴이 있대요. 그곳을 직접 보고 와 주세요.";
                m.mapItemName = "푸른 동굴의 지도";
            });

            CreateOrLoadMission("Mission_DiscAmsterdamTexelSeals.asset", m =>
            {
                m.missionId = "mission.disc.amsterdam.texel_seals";
                m.issuerPort = amsterdam;
                m.targetDiscovery = texelSeals;
                m.rewardMoney = 1000;
                m.rewardGoodReputation = 100;
                m.title = "북쪽 섬의 바다표범을 만나 봐요";
                m.description = "암스테르담 북쪽 바다의 작은 섬에 바다표범 가족이 산다고 해요. 멀리서 살짝 인사하고 와 주세요.";
                m.mapItemName = "텍셀 섬의 지도";
            });

            CreateOrLoadMission("Mission_DiscLondonDover.asset", m =>
            {
                m.missionId = "mission.disc.london.dover";
                m.issuerPort = london;
                m.targetDiscovery = whiteCliffs;
                m.rewardMoney = 1000;
                m.rewardGoodReputation = 100;
                m.title = "남쪽 바닷가의 흰 절벽을 만나 봐요";
                m.description = "런던 남쪽 바다 끝에 새하얀 절벽이 우뚝 솟아 있다고 해요. 멀리서 그 모습을 보고 와 주세요.";
                m.mapItemName = "도버 절벽의 지도";
            });

            CreateOrLoadMission("Mission_DiscIstanbulBosphorus.asset", m =>
            {
                m.missionId = "mission.disc.istanbul.bosphorus";
                m.issuerPort = istanbul;
                m.targetDiscovery = bosphorus;
                m.rewardMoney = 1000;
                m.rewardGoodReputation = 100;
                m.title = "두 대륙을 잇는 좁은 바닷길을 찾아봐요";
                m.description = "이스탄불 한가운데를 지나는 좁은 바닷길이 있대요. 그곳에 점을 찍어 주세요.";
                m.mapItemName = "보스포루스 해협의 지도";
            });

            CreateOrLoadMission("Mission_DiscBusanHallasan.asset", m =>
            {
                m.missionId = "mission.disc.busan.hallasan";
                m.issuerPort = busan;
                m.targetDiscovery = hallasan;
                m.rewardMoney = 1500;
                m.rewardGoodReputation = 150;
                m.title = "남쪽 바다 끝 큰 산을 찾아봐요";
                m.description = "부산에서 남쪽으로 내려가면 큰 산이 있는 섬이 있어요. 그 산을 직접 보고 돌아와 주세요.";
                m.mapItemName = "한라산 가는 지도";
            });

            CreateOrLoadMission("Mission_DiscGuangzhouPearlRiver.asset", m =>
            {
                m.missionId = "mission.disc.guangzhou.pearl_river";
                m.issuerPort = guangzhou;
                m.targetDiscovery = pearlRiver;
                m.rewardMoney = 1000;
                m.rewardGoodReputation = 100;
                m.title = "큰 강이 바다를 만나는 곳을 찾아봐요";
                m.description = "광저우의 큰 강이 바다와 만나는 곳을 직접 살펴보고 와 주세요.";
                m.mapItemName = "주강 삼각주 지도";
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M2MissionSeeder] 완료. M2 의뢰 시드 추가 16개:\n" +
                "  • 8개 DiscoveryData (보자도르 곶 / 카나리아 / 푸른 동굴 / 텍셀 바다표범 / 도버 절벽 / 보스포루스 / 한라산 / 주강 삼각주)\n" +
                "  • 8개 MissionTemplate (각 시작 항구별 발견물 의뢰)\n" +
                "\nM1 의 지브롤터 의뢰 + 본 8개 = 총 9개 발견물 + 9개 의뢰. 8개국 모두 풀 게임 루프 체험 가능.\n" +
                "\n다음 작업:\n" +
                "  1. SeaWorldManager 의 Active Discoveries 배열에 신규 8개 추가\n" +
                "  2. MissionService 의 All Missions 배열에 신규 8개 추가\n" +
                "  3. JournalPanel 의 All Discoveries 배열에 신규 8개 추가");
        }

        // ─── 헬퍼 ──────────────────────────────────────────────────────────────

        private static NationData LoadNation(string name)
        {
            var path = $"{DataRoot}/Nations/{name}.asset";
            var result = AssetDatabase.LoadAssetAtPath<NationData>(path);
            if (result == null)
                Debug.LogWarning($"[M2MissionSeeder] NationData 없음: {path} (먼저 M1·M2.1 시드를 돌렸나요?)");
            return result;
        }

        private static PortData LoadPort(string name)
        {
            var path = $"{DataRoot}/Ports/{name}.asset";
            var result = AssetDatabase.LoadAssetAtPath<PortData>(path);
            if (result == null)
                Debug.LogWarning($"[M2MissionSeeder] PortData 없음: {path}");
            return result;
        }

        private static DiscoveryData CreateOrLoadDiscovery(string fileName, Action<DiscoveryData> setup)
        {
            var path = $"{DataRoot}/Discoveries/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<DiscoveryData>(path);
            if (existing != null)
            {
                Debug.Log($"[M2MissionSeeder] Skipping (exists): {path}");
                return existing;
            }
            var so = ScriptableObject.CreateInstance<DiscoveryData>();
            setup(so);
            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[M2MissionSeeder] Created: {path}");
            return so;
        }

        private static MissionTemplate CreateOrLoadMission(string fileName, Action<MissionTemplate> setup)
        {
            var path = $"{DataRoot}/Missions/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<MissionTemplate>(path);
            if (existing != null)
            {
                Debug.Log($"[M2MissionSeeder] Skipping (exists): {path}");
                return existing;
            }
            var so = ScriptableObject.CreateInstance<MissionTemplate>();
            setup(so);
            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[M2MissionSeeder] Created: {path}");
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
