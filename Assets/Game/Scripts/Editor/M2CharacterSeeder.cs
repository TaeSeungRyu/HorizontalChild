using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M2.3 — 8개국 대표 실존 인물 캐릭터 SO 생성.
    /// CONTENT_DESIGN.md §2.6 의 실존 인물 8명 중 포르투갈 (엔리케 왕자) 은 M1 시더에서 이미 생성됨.
    /// 본 시더는 나머지 7명 추가 + 모든 NationData 의 startingCharacter 자동 연결.
    ///
    /// 메뉴: Game/Seed M2 Characters
    /// </summary>
    public static class M2CharacterSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M2 Characters")]
        public static void SeedM2Characters()
        {
            EnsureFolder($"{DataRoot}/Characters");

            // ─── 국가·항구 참조 로드 ─────────────────────────────────────────
            // M1 + M2.1 시드가 미리 돌았다는 전제.

            var portugal = LoadNation("Nation_Portugal");
            var spain = LoadNation("Nation_Spain");
            var italy = LoadNation("Nation_Italy");
            var netherlands = LoadNation("Nation_Netherlands");
            var england = LoadNation("Nation_England");
            var ottoman = LoadNation("Nation_Ottoman");
            var joseon = LoadNation("Nation_Joseon");
            var china = LoadNation("Nation_China");

            var lisbon = LoadPort("Port_Lisbon");
            var sevilla = LoadPort("Port_Sevilla");
            var venezia = LoadPort("Port_Venezia");
            var amsterdam = LoadPort("Port_Amsterdam");
            var london = LoadPort("Port_London");
            var istanbul = LoadPort("Port_Istanbul");
            var busan = LoadPort("Port_Busan");
            var guangzhou = LoadPort("Port_Guangzhou");

            // ─── 7명 실존 인물 (포르투갈 엔리케 왕자는 M1Seeder 에서) ────────

            var elcano = CreateOrLoadCharacter("Character_Elcano.asset", c =>
            {
                c.characterId = "character.elcano";
                c.displayNameKo = "후안 세바스티안 엘카노";
                c.gender = Gender.Male;
                c.role = CharacterRole.Adventurer;
                c.bravery = 80;
                c.seamanship = 95;
                c.keenEye = 75;
                c.startingGoodReputation = 45000;
                c.startingBadReputation = 0;
                c.nation = spain;
                c.homePort = sevilla;
                c.shortIntro = "세상을 한 바퀴 돌아 처음으로 다시 돌아온 항해사예요.";
                c.moreInfo = "1522년, 마젤란이 시작한 큰 항해를 끝까지 마치고 스페인으로 돌아왔어요.";
                c.sourceUrl = "https://ko.wikipedia.org/wiki/후안_세바스티안_엘카노";
            });

            var cadamosto = CreateOrLoadCharacter("Character_Cadamosto.asset", c =>
            {
                c.characterId = "character.cadamosto";
                c.displayNameKo = "알비제 카다모스토";
                c.gender = Gender.Male;
                c.role = CharacterRole.Adventurer;
                c.bravery = 65;
                c.seamanship = 75;
                c.keenEye = 80;
                c.startingGoodReputation = 30000;
                c.startingBadReputation = 0;
                c.nation = italy;
                c.homePort = venezia;
                c.shortIntro = "베네치아에서 태어나, 아프리카 바닷길을 글로 남긴 항해사예요.";
                c.moreInfo = "그가 쓴 항해 일기 덕분에 사람들이 멀리 있는 곳의 모습을 알게 되었답니다.";
                c.sourceUrl = "https://en.wikipedia.org/wiki/Alvise_Cadamosto";
            });

            var barents = CreateOrLoadCharacter("Character_Barents.asset", c =>
            {
                c.characterId = "character.barents";
                c.displayNameKo = "빌럼 바렌츠";
                c.gender = Gender.Male;
                c.role = CharacterRole.Adventurer;
                c.bravery = 85;
                c.seamanship = 80;
                c.keenEye = 80;
                c.startingGoodReputation = 40000;
                c.startingBadReputation = 0;
                c.nation = netherlands;
                c.homePort = amsterdam;
                c.shortIntro = "얼음으로 가득한 북쪽 바다를 세 번이나 탐험한 사람이에요.";
                c.moreInfo = "그의 이름을 딴 '바렌츠해' 가 지금도 있답니다.";
                c.sourceUrl = "https://ko.wikipedia.org/wiki/빌럼_바렌츠";
            });

            var hudson = CreateOrLoadCharacter("Character_Hudson.asset", c =>
            {
                c.characterId = "character.hudson";
                c.displayNameKo = "헨리 허드슨";
                c.gender = Gender.Male;
                c.role = CharacterRole.Adventurer;
                c.bravery = 80;
                c.seamanship = 75;
                c.keenEye = 85;
                c.startingGoodReputation = 35000;
                c.startingBadReputation = 0;
                c.nation = england;
                c.homePort = london;
                c.shortIntro = "북쪽으로 가는 길을 찾으려 한 영국의 탐험가예요.";
                c.moreInfo = "그의 이름을 딴 '허드슨강' 과 '허드슨만' 이 북아메리카에 있어요.";
                c.sourceUrl = "https://ko.wikipedia.org/wiki/헨리_허드슨";
            });

            var pirireis = CreateOrLoadCharacter("Character_PiriReis.asset", c =>
            {
                c.characterId = "character.pirireis";
                c.displayNameKo = "피리 레이스";
                c.gender = Gender.Male;
                c.role = CharacterRole.Adventurer;
                c.bravery = 70;
                c.seamanship = 80;
                c.keenEye = 95;
                c.startingGoodReputation = 40000;
                c.startingBadReputation = 0;
                c.nation = ottoman;
                c.homePort = istanbul;
                c.shortIntro = "오스만의 항해사이자, 세계 지도를 그린 멋진 지도 제작자예요.";
                c.moreInfo = "그가 1513년에 그린 지도에는 그 시대 새로 알려진 땅들이 담겨 있답니다.";
                c.sourceUrl = "https://ko.wikipedia.org/wiki/피리_레이스";
            });

            var yiSunsin = CreateOrLoadCharacter("Character_YiSunsin.asset", c =>
            {
                c.characterId = "character.yi_sunsin";
                c.displayNameKo = "이순신";
                c.gender = Gender.Male;
                c.role = CharacterRole.Adventurer;
                c.bravery = 95;
                c.seamanship = 85;
                c.keenEye = 90;
                c.startingGoodReputation = 50000;
                c.startingBadReputation = 0;
                c.nation = joseon;
                c.homePort = busan;
                c.shortIntro = "거북선과 함께 우리나라 바다를 잘 지킨 장군이에요.";
                c.moreInfo = "꼼꼼히 일기를 써서 그날그날의 바다를 기록으로 남겼답니다.";
                c.sourceUrl = "https://ko.wikipedia.org/wiki/이순신";
            });

            var zhengHe = CreateOrLoadCharacter("Character_ZhengHe.asset", c =>
            {
                c.characterId = "character.zheng_he";
                c.displayNameKo = "정화";
                c.gender = Gender.Male;
                c.role = CharacterRole.Adventurer;
                c.bravery = 80;
                c.seamanship = 95;
                c.keenEye = 80;
                c.startingGoodReputation = 50000;
                c.startingBadReputation = 0;
                c.nation = china;
                c.homePort = guangzhou;
                c.shortIntro = "아주 큰 배들을 이끌고 일곱 번이나 멀리 바다를 누빈 명나라 사령관이에요.";
                c.moreInfo = "동남아시아·인도·아프리카 동쪽 바닷가까지 가서 사람들을 만났답니다.";
                c.sourceUrl = "https://ko.wikipedia.org/wiki/정화_(명나라)";
            });

            // ─── NationData.startingCharacter 자동 연결 ──────────────────────

            var henrique = AssetDatabase.LoadAssetAtPath<CharacterData>(
                $"{DataRoot}/Characters/Character_Henrique.asset");

            LinkStartingCharacter(portugal, henrique);
            LinkStartingCharacter(spain, elcano);
            LinkStartingCharacter(italy, cadamosto);
            LinkStartingCharacter(netherlands, barents);
            LinkStartingCharacter(england, hudson);
            LinkStartingCharacter(ottoman, pirireis);
            LinkStartingCharacter(joseon, yiSunsin);
            LinkStartingCharacter(china, zhengHe);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M2CharacterSeeder] 완료. 7개 신규 캐릭터 + NationData.startingCharacter 자동 연결:\n" +
                "  • 엘카노 (스페인)\n" +
                "  • 카다모스토 (이탈리아)\n" +
                "  • 바렌츠 (네덜란드)\n" +
                "  • 허드슨 (영국)\n" +
                "  • 피리 레이스 (오스만)\n" +
                "  • 이순신 (조선)\n" +
                "  • 정화 (중국)\n" +
                "\n포르투갈/엔리케 왕자 (M1) 와 합쳐 총 8명 실존 인물.\n" +
                "NationSelectionPanel 에서 국적 선택 → PlayerShip.captain 자동 할당됨.");
        }

        // ─── 헬퍼 ──────────────────────────────────────────────────────────────

        private static NationData LoadNation(string name)
        {
            var path = $"{DataRoot}/Nations/{name}.asset";
            var result = AssetDatabase.LoadAssetAtPath<NationData>(path);
            if (result == null) Debug.LogWarning($"[M2CharacterSeeder] NationData 없음: {path} (먼저 M1·M2 시드를 돌렸나요?)");
            return result;
        }

        private static PortData LoadPort(string name)
        {
            var path = $"{DataRoot}/Ports/{name}.asset";
            var result = AssetDatabase.LoadAssetAtPath<PortData>(path);
            if (result == null) Debug.LogWarning($"[M2CharacterSeeder] PortData 없음: {path}");
            return result;
        }

        private static CharacterData CreateOrLoadCharacter(string fileName, Action<CharacterData> setup)
        {
            var path = $"{DataRoot}/Characters/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (existing != null)
            {
                Debug.Log($"[M2CharacterSeeder] Skipping (exists): {path}");
                return existing;
            }
            var so = ScriptableObject.CreateInstance<CharacterData>();
            setup(so);
            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[M2CharacterSeeder] Created: {path}");
            return so;
        }

        private static void LinkStartingCharacter(NationData nation, CharacterData character)
        {
            if (nation == null || character == null) return;
            if (nation.startingCharacter != character)
            {
                nation.startingCharacter = character;
                EditorUtility.SetDirty(nation);
                Debug.Log($"[M2CharacterSeeder] {nation.nationId}.startingCharacter = {character.characterId}");
            }
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
