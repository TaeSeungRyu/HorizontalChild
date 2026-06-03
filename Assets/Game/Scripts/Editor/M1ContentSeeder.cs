using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// CONTENT_DESIGN.md 의 M1 마일스톤 콘텐츠를 ScriptableObject 인스턴스로 자동 생성.
    /// 메뉴: Game/Seed M1 Content
    ///
    /// 이미 존재하는 .asset 은 건드리지 않음 (덮어쓰기 X — 안전).
    /// 다시 깨끗하게 시드하려면 해당 .asset 파일을 먼저 삭제하고 메뉴 재실행.
    /// </summary>
    public static class M1ContentSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M1 Content")]
        public static void SeedM1Content()
        {
            EnsureFolders();

            // ─── 1단계: 의존성 없는 ScriptableObject 먼저 생성 ─────────────
            // Product는 originPort 참조가 있지만 일단 null로 두고 마지막에 채움.

            var saltCod = CreateOrLoad<ProductData>(
                $"{DataRoot}/Products/Product_SaltCod.asset",
                p =>
                {
                    p.productId = "product.salt_cod";
                    p.displayNameKo = "소금 대구";
                    p.basePrice = 20;
                    p.isSpecial = false;
                    p.shortDescription = "긴 항해에 든든한, 소금에 절인 대구예요.";
                    p.sourceUrl = "https://en.wikipedia.org/wiki/Bacalhau";
                });

            var cork = CreateOrLoad<ProductData>(
                $"{DataRoot}/Products/Product_Cork.asset",
                p =>
                {
                    p.productId = "product.cork";
                    p.displayNameKo = "코르크";
                    p.basePrice = 30;
                    p.isSpecial = false;
                    p.shortDescription = "포도주병의 마개로 쓰이는 가벼운 나무껍질이에요.";
                    p.sourceUrl = "https://ko.wikipedia.org/wiki/코르크";
                });

            var dates = CreateOrLoad<ProductData>(
                $"{DataRoot}/Products/Product_Dates.asset",
                p =>
                {
                    p.productId = "product.dates";
                    p.displayNameKo = "대추야자";
                    p.basePrice = 25;
                    p.isSpecial = false;
                    p.shortDescription = "사막을 건너온 달콤하고 쫀득한 열매예요.";
                    p.sourceUrl = "https://ko.wikipedia.org/wiki/대추야자";
                });

            var caravel = CreateOrLoad<ShipData>(
                $"{DataRoot}/Ships/Ship_Caravel.asset",
                s =>
                {
                    s.shipId = "ship.caravel";
                    s.displayName = "캐러벨";
                    s.cannonPower = 3;
                    s.speed = 8;
                    s.cargoCapacity = 60;
                    s.maxDurability = 50;
                    s.basePrice = 5000;
                    s.gate = new ReputationGate();
                    s.shortDescription = "작고 빠른, 큰 바다 모험에 딱 좋은 작은 배예요.";
                    s.longDescription = "삼각 돛과 사각 돛을 같이 달아 바람을 잘 잡아요. 포르투갈 사람들이 가장 먼저 큰 바다로 나갈 때 탔답니다.";
                    s.sourceUrl = "https://ko.wikipedia.org/wiki/캐러벨";
                });

            // ─── 2단계: Port (nation 임시 null) ─────────────────────────────
            var lisbon = CreateOrLoad<PortData>(
                $"{DataRoot}/Ports/Port_Lisbon.asset",
                p =>
                {
                    p.portId = "port.lisbon";
                    p.displayNameKo = "리스본";
                    p.displayNameOriginal = "Lisboa";
                    p.latitude = 38.7f;
                    p.longitude = -9.1f;
                    p.shortDescription = "큰 바다로 가는 문이 활짝 열린 도시.";
                    p.commonProducts = new[] { saltCod, cork };
                    p.specialProducts = Array.Empty<ProductData>();
                    p.sourceUrl = "https://ko.wikipedia.org/wiki/리스본";
                });

            var ceuta = CreateOrLoad<PortData>(
                $"{DataRoot}/Ports/Port_Ceuta.asset",
                p =>
                {
                    p.portId = "port.ceuta";
                    p.displayNameKo = "세우타";
                    p.displayNameOriginal = "Ceuta";
                    p.latitude = 35.9f;
                    p.longitude = -5.3f;
                    p.shortDescription = "바다를 건너 새 길이 시작된 작은 항구.";
                    p.commonProducts = new[] { dates };
                    p.specialProducts = Array.Empty<ProductData>();
                    p.sourceUrl = "https://ko.wikipedia.org/wiki/세우타";
                });

            // ─── 3단계: Discovery (relatedNation 임시 null) ────────────────
            var gibraltar = CreateOrLoad<DiscoveryData>(
                $"{DataRoot}/Discoveries/Discovery_GibraltarStrait.asset",
                d =>
                {
                    d.discoveryId = "disc.gibraltar_strait";
                    d.displayNameKo = "지브롤터 해협";
                    d.category = DiscoveryCategory.Landmark;
                    d.latitude = 36.0f;
                    d.longitude = -5.6f;
                    d.searchToleranceBase = 0.03f;
                    d.mainDescription =
                        "유럽과 아프리카 사이에 있는 좁은 바닷길이에요. 큰 바다(대서양)와 잔잔한 바다(지중해)를 이어주지요. 옛날 사람들은 이곳 양쪽에 큰 기둥이 서 있다고 상상했답니다.";
                    d.moreInfo =
                        "오늘날에도 이 좁은 길을 수많은 배가 오갑니다. 가장 좁은 곳은 약 14km밖에 되지 않아요.";
                    d.eraLabel = "고대부터";
                    d.relatedFigures = "헤라클레스의 기둥 전설 (전설이라는 점 명시)";
                    d.sourceUrl = "https://ko.wikipedia.org/wiki/지브롤터_해협";
                    d.sensitiveExpressionChecked = true;
                });

            // ─── 4단계: Nation (startingPort 채움) ──────────────────────────
            var portugal = CreateOrLoad<NationData>(
                $"{DataRoot}/Nations/Nation_Portugal.asset",
                n =>
                {
                    n.nationId = "nation.portugal";
                    n.displayNameKo = "포르투갈";
                    n.displayNameOriginal = "Portugal";
                    n.accentColor = new Color(0.06f, 0.40f, 0.20f); // 짙은 녹색
                    n.startingPort = lisbon;
                    n.startingYear = 1415;
                    n.shortIntro = "큰 바다로 가장 먼저 모험을 떠난 작은 나라.";
                    n.greeting =
                        "환영합니다, 어린 항해사님! 우리 포르투갈은 가장 먼저 큰 바다로 나갔답니다. 함께 새로운 땅을 찾아볼까요?";
                    n.designerNotes = "엔리케 왕자의 항해 학교, 카라벨선, 아프리카 서해안 탐험.";
                    n.sourceUrl = "https://ko.wikipedia.org/wiki/포르투갈_제국";
                });

            // ─── 5단계: Character / Mission / Region ─────────────────────────
            var henrique = CreateOrLoad<CharacterData>(
                $"{DataRoot}/Characters/Character_Henrique.asset",
                c =>
                {
                    c.characterId = "character.henrique";
                    c.displayNameKo = "엔리케 왕자";
                    c.gender = Gender.Male;
                    c.role = CharacterRole.Adventurer;
                    c.bravery = 70;
                    c.seamanship = 60;
                    c.keenEye = 70;
                    c.startingGoodReputation = 50000;
                    c.startingBadReputation = 0;
                    c.nation = portugal;
                    c.homePort = lisbon;
                    c.shortIntro = "포르투갈의 왕자이자, 항해사들을 도와 큰 바다로 보낸 사람이에요.";
                    c.moreInfo =
                        "엔리케 왕자는 직접 큰 바다로 나가지는 않았지만, 어린 항해사들을 가르치고 도와 새로운 길을 열었답니다.";
                    c.sourceUrl = "https://ko.wikipedia.org/wiki/엔히크_(포르투갈)";
                });

            var missionGibraltar = CreateOrLoad<MissionTemplate>(
                $"{DataRoot}/Missions/Mission_DiscLisbonGibraltar.asset",
                m =>
                {
                    m.missionId = "mission.disc.lisbon.gibraltar";
                    m.issuerPort = lisbon;
                    m.targetDiscovery = gibraltar;
                    m.rewardMoney = 1000;
                    m.rewardGoodReputation = 100;
                    m.title = "두 바다가 만나는 좁은 길을 찾아봐요";
                    m.description =
                        "리스본 남쪽 바다 어딘가에, 큰 바다와 잔잔한 바다를 잇는 좁은 바닷길이 있대요. 그곳을 찾아 지도에 점을 찍어 주세요.";
                    m.mapItemName = "지브롤터로 가는 지도";
                });

            var iberia = CreateOrLoad<RegionData>(
                $"{DataRoot}/Regions/Region_Iberia.asset",
                r =>
                {
                    r.regionId = "region.iberia";
                    r.displayNameKo = "이베리아 반도";
                    r.ports = new[] { lisbon };
                    r.discoveries = new[] { gibraltar };
                    r.unlockedAtStartFor = new[] { portugal };
                    r.unlockHint = "포르투갈·스페인 국적 선택 시 시작부터 해제.";
                });

            // ─── 6단계: 순환 참조 채우기 ─────────────────────────────────────
            // 의존 순서상 1~3단계에서 못 채운 참조들을 이제 설정.

            SetNationOnPort(lisbon, portugal);
            SetNationOnPort(ceuta, portugal);     // 세우타는 1415 정복 후 포르투갈령
            SetOriginPort(saltCod, lisbon);
            SetOriginPort(cork, lisbon);
            SetOriginPort(dates, ceuta);
            SetRelatedNation(gibraltar, portugal);

            // ─── 저장 ────────────────────────────────────────────────────────
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M1Seeder] 완료. M1 콘텐츠 시드 11개 생성/확인:\n" +
                "  • Nation_Portugal\n" +
                "  • Port_Lisbon, Port_Ceuta\n" +
                "  • Product_SaltCod, Product_Cork, Product_Dates\n" +
                "  • Discovery_GibraltarStrait\n" +
                "  • Ship_Caravel\n" +
                "  • Character_Henrique\n" +
                "  • Mission_DiscLisbonGibraltar\n" +
                "  • Region_Iberia");
        }

        // ─── 보조 메서드 ──────────────────────────────────────────────────────

        private static void EnsureFolders()
        {
            string[] subFolders =
            {
                "Nations", "Ports", "Products", "Discoveries",
                "Regions", "Characters", "Missions", "Ships"
            };
            foreach (var sub in subFolders)
            {
                EnsureFolder($"{DataRoot}/{sub}");
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;
            EnsureFolder(parent); // 부모 폴더부터 재귀적으로
            AssetDatabase.CreateFolder(parent, name);
        }

        private static T CreateOrLoad<T>(string path, Action<T> setup) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                Debug.Log($"[M1Seeder] Skipping (exists): {path}");
                return existing;
            }

            var so = ScriptableObject.CreateInstance<T>();
            setup(so);
            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[M1Seeder] Created: {path}");
            return so;
        }

        private static void SetNationOnPort(PortData port, NationData nation)
        {
            if (port == null || port.nation == nation) return;
            port.nation = nation;
            EditorUtility.SetDirty(port);
        }

        private static void SetOriginPort(ProductData product, PortData port)
        {
            if (product == null || product.originPort == port) return;
            product.originPort = port;
            EditorUtility.SetDirty(product);
        }

        private static void SetRelatedNation(DiscoveryData discovery, NationData nation)
        {
            if (discovery == null || discovery.relatedNation == nation) return;
            discovery.relatedNation = nation;
            EditorUtility.SetDirty(discovery);
        }
    }
}
