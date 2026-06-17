using System.Collections.Generic;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Editor
{
    /// <summary>
    /// M3.5 — NPC 100명 풀세트 시드. (해적 20 / 호위선 40 / 상선 40)
    /// 모두 1000~1830 시기 위키피디아 등재 실존 인물.
    /// 명단·근거: [NPC_HISTORICAL_ROSTER.md](../../NPC_HISTORICAL_ROSTER.md)
    ///
    /// 주인공과 중복되지 않도록 8명 제외 (엔리케·엘카노·카다모스토·바렌츠·허드슨·피리 레이스·임상옥·정화).
    /// 이순신은 주인공 → 호위선 NPC #16 으로 이동.
    ///
    /// 스탯 보정: 강조 인물(검은수염·이순신·넬슨·마르코폴로 등)은 명시된 값,
    /// 그 외는 타입별 랜덤 범위 (Pirate: 용 60~95 / 호위선: 균형 / 상선: 눈 60~90).
    ///
    /// 메뉴: Game/Seed M3 NPCs
    /// 시드 후 Game ▸ Refresh All Catalogs 권장.
    /// </summary>
    public static class M3NpcSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        /// <summary>한 NPC 의 모든 메타. stat 이 -1 이면 타입별 랜덤 적용.</summary>
        private readonly struct Entry
        {
            public readonly string NameKo;
            public readonly Gender Gender;
            public readonly NpcType Type;
            public readonly int Bravery;    // -1 = random
            public readonly int Seamanship; // -1 = random
            public readonly int KeenEye;    // -1 = random
            public readonly string WikiUrl;
            public readonly string ShortIntro;

            public Entry(string nameKo, Gender gender, NpcType type,
                int bravery, int seamanship, int keenEye,
                string wikiUrl, string shortIntro)
            {
                NameKo = nameKo; Gender = gender; Type = type;
                Bravery = bravery; Seamanship = seamanship; KeenEye = keenEye;
                WikiUrl = wikiUrl; ShortIntro = shortIntro;
            }
        }

        [MenuItem("Game/Seed M3 NPCs")]
        public static void SeedM3Npcs()
        {
            EnsureFolder($"{DataRoot}/Characters");
            EnsureFolder($"{DataRoot}/Npcs");

            // 결정적 시드 — 같은 명단·항구에 대해 같은 결과
            Random.InitState(20260617);

            var allPorts = LoadAllPorts();
            if (allPorts.Count == 0)
            {
                Debug.LogError("[M3NpcSeeder] PortData 에셋이 없어요. 먼저 항구 시드를 실행하세요.");
                return;
            }

            // 타입별 ShipData 풀 로드 — 없으면 빈 배열 → 절차적 fallback
            var pirateShips = LoadShips(new[] {
                "Ship_Galleon", "Ship_Geobukseon", "Ship_Panokseon", "Ship_Galleass", "Ship_Carrack" });
            var escortShips = LoadShips(new[] {
                "Ship_Galleon", "Ship_Galleass", "Ship_Panokseon", "Ship_Carrack", "Ship_SantaMaria" });
            var merchantShips = LoadShips(new[] {
                "Ship_Fluyt", "Ship_Junk", "Ship_EastIndiaman", "Ship_Cog", "Ship_Dhow", "Ship_Carrack" });

            CleanOldAssets();

            var entries = BuildEntries();
            int idx = 0;
            foreach (var e in entries)
            {
                var home = allPorts[Random.Range(0, allPorts.Count)];
                PortData dest = null;
                if (e.Type != NpcType.Pirate)
                {
                    for (int t = 0; t < 8 && dest == null; t++)
                    {
                        var pick = allPorts[Random.Range(0, allPorts.Count)];
                        if (pick != home) dest = pick;
                    }
                }
                var pool = e.Type switch
                {
                    NpcType.Pirate => pirateShips,
                    NpcType.Escort => escortShips,
                    _ => merchantShips,
                };
                var ship = PickShip(pool);
                CreateNpc(e, home, dest, ship, idx++);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[M3NpcSeeder] {entries.Count}명 실존인물 NPC 생성 완료.\n" +
                "  해적 20 / 호위선 40 / 상선 40\n" +
                "  명단: Assets/Game/NPC_HISTORICAL_ROSTER.md\n" +
                "다음: Game ▸ Refresh All Catalogs → NpcCatalog 갱신.\n" +
                "  → NpcSpawner.Spawn Count 를 100 이상 으로 조정.");
        }

        // ─── 실존인물 100명 큐레이션 ────────────────────────────────────────

        private static List<Entry> BuildEntries()
        {
            // 스탯 보정: -1 = 타입별 랜덤. 명시된 값은 그대로 사용.
            var list = new List<Entry>(100);

            // ════════════════ ☠ 해적 (20) — bravery 우세 ════════════════
            list.Add(new Entry("검은수염 에드워드 티치", Gender.Male, NpcType.Pirate,
                95, -1, -1,
                "https://ko.wikipedia.org/wiki/검은수염",
                "수염에 불을 붙이고 카리브해를 공포에 떨게 한 전설의 해적이에요."));
            list.Add(new Entry("헨리 모건", Gender.Male, NpcType.Pirate,
                90, 80, -1,
                "https://ko.wikipedia.org/wiki/헨리_모건",
                "웨일스 출신 사략선장. 자메이카 부총독까지 오른 입지전적 인물이에요."));
            list.Add(new Entry("윌리엄 키드", Gender.Male, NpcType.Pirate,
                -1, -1, -1,
                "https://ko.wikipedia.org/wiki/윌리엄_키드",
                "왕의 허가를 받았다가 해적으로 몰린 비운의 스코틀랜드 선장이에요."));
            list.Add(new Entry("바솔로뮤 로버츠", Gender.Male, NpcType.Pirate,
                90, -1, -1,
                "https://ko.wikipedia.org/wiki/바솔로뮤_로버츠",
                "3년간 400척 넘는 배를 나포한 황금기 해적의 정점이에요."));
            list.Add(new Entry("헨리 에이버리", Gender.Male, NpcType.Pirate,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Henry_Every",
                "인도양에서 무굴 보물선을 털고 흔적도 없이 사라진 해적왕이에요."));
            list.Add(new Entry("스티드 보넷", Gender.Male, NpcType.Pirate,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Stede_Bonnet",
                "바베이도스 부유한 농장주가 해적이 된 ‘신사 해적’ 이에요."));
            list.Add(new Entry("새뮤얼 벨러미", Gender.Male, NpcType.Pirate,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Samuel_Bellamy",
                "황금기 최고 부자 해적. ‘블랙 샘’ 으로 불렸어요."));
            list.Add(new Entry("에드워드 잉글랜드", Gender.Male, NpcType.Pirate,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Edward_England",
                "포로에게 자비를 베푼 ‘인정 많은 해적’ 으로 알려졌어요."));
            list.Add(new Entry("캘리코 잭 래컴", Gender.Male, NpcType.Pirate,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Calico_Jack",
                "해골과 칼 두 자루의 해적기 디자인으로 유명한 카리브 해적이에요."));
            list.Add(new Entry("앤 보니", Gender.Female, NpcType.Pirate,
                88, -1, -1,
                "https://ko.wikipedia.org/wiki/앤_보니",
                "남장하고 카리브해를 누빈 아일랜드 출신 여성 해적이에요."));
            list.Add(new Entry("메리 리드", Gender.Female, NpcType.Pirate,
                85, -1, -1,
                "https://ko.wikipedia.org/wiki/메리_리드",
                "앤 보니와 함께 캘리코 잭의 배에 탔던 영국 여성 해적이에요."));
            list.Add(new Entry("올리비에 르바쇠르", Gender.Male, NpcType.Pirate,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Olivier_Levasseur",
                "처형 직전 ‘내 보물을 찾을 수 있는 자, 누구든 가져라!’ 외친 프랑스 해적이에요."));
            list.Add(new Entry("하이르 앗 딘 바르바로사", Gender.Male, NpcType.Pirate,
                90, -1, -1,
                "https://ko.wikipedia.org/wiki/하이르_앗_딘_바르바로사",
                "지중해를 호령한 오스만의 붉은 수염 코르세어 형제의 동생이에요."));
            list.Add(new Entry("우루지 바르바로사", Gender.Male, NpcType.Pirate,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Oruç_Reis",
                "지중해 코르세어 형제의 형, 알제 정복으로 유명해요."));
            list.Add(new Entry("투르구트 레이스", Gender.Male, NpcType.Pirate,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Turgut_Reis",
                "‘드라구트’ 라 불린 오스만 명코르세어. 몰타까지 위협했어요."));
            list.Add(new Entry("클라우스 슈퇴르테베커", Gender.Male, NpcType.Pirate,
                85, -1, -1,
                "https://en.wikipedia.org/wiki/Klaus_Störtebeker",
                "한자동맹 시대 발트해 ‘식량 친구단’ 의 우두머리예요."));
            list.Add(new Entry("얀 얀스존", Gender.Male, NpcType.Pirate,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Jan_Janszoon",
                "네덜란드 출신으로 무라트 레이스가 된 코르세어예요."));
            list.Add(new Entry("왕직", Gender.Male, NpcType.Pirate,
                85, -1, -1,
                "https://ko.wikipedia.org/wiki/왕직_(명나라)",
                "명나라 시대 동중국해 왜구를 이끈 휘저우 출신 두목이에요."));
            list.Add(new Entry("정지룡", Gender.Male, NpcType.Pirate,
                85, 80, -1,
                "https://ko.wikipedia.org/wiki/정지룡",
                "민난 출신 해상왕. 정성공의 아버지로 청-명 사이를 누볐어요."));
            list.Add(new Entry("정이 부인", Gender.Female, NpcType.Pirate,
                95, -1, 80,
                "https://ko.wikipedia.org/wiki/정일사오",
                "남중국해 8만 해적을 통솔한 청대 최강 여해적 두목이에요."));

            // ════════════════ ⚔ 호위선 / 해군 (40) — 균형형 ════════════════
            list.Add(new Entry("로제르 데 라우리아", Gender.Male, NpcType.Escort,
                -1, 90, -1,
                "https://en.wikipedia.org/wiki/Roger_of_Lauria",
                "13세기 시칠리아 만에서 단 한 번도 패하지 않은 아라곤 제독이에요."));
            list.Add(new Entry("베토르 피사니", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Vettor_Pisani",
                "키오자 전쟁에서 베네치아를 구한 영웅 제독이에요."));
            list.Add(new Entry("카를로 제노", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Carlo_Zeno",
                "베네치아 함대를 이끌고 제노바와 맞선 14세기 명장이에요."));
            list.Add(new Entry("페로 니뇨", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Pero_Niño",
                "카스티야의 ‘무패의 백작’ — 영불해협을 휩쓴 기사 제독이에요."));
            list.Add(new Entry("안드레아 도리아", Gender.Male, NpcType.Escort,
                -1, 90, -1,
                "https://ko.wikipedia.org/wiki/안드레아_도리아",
                "제노바 공화국의 위대한 제독이자 정치가예요."));
            list.Add(new Entry("돈 가르시아 데 톨레도", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/García_Álvarez_de_Toledo,_4th_Marquis_of_Villafranca",
                "몰타 공방전에서 스페인 함대를 이끈 명장이에요."));
            list.Add(new Entry("돈 후안 데 아우스트리아", Gender.Male, NpcType.Escort,
                90, 85, -1,
                "https://ko.wikipedia.org/wiki/돈_후안_데_아우스트리아",
                "1571년 레판토 해전 기독교 연합 함대 총사령관이에요."));
            list.Add(new Entry("찰스 하워드", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Charles_Howard,_1st_Earl_of_Nottingham",
                "1588년 스페인 무적함대를 격퇴한 영국 제독이에요."));
            list.Add(new Entry("존 호킨스", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://ko.wikipedia.org/wiki/존_호킨스",
                "엘리자베스 1세 시대 영국 해군을 현대화시킨 제독이에요."));
            list.Add(new Entry("마틴 프로비셔", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Martin_Frobisher",
                "북서항로를 찾아 세 차례 항해한 영국 탐험가 겸 제독이에요."));
            list.Add(new Entry("프랜시스 드레이크", Gender.Male, NpcType.Escort,
                85, 90, -1,
                "https://ko.wikipedia.org/wiki/프랜시스_드레이크",
                "영국 최초로 세계를 일주한 사략선장 겸 제독이에요."));
            list.Add(new Entry("유대유", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Yu_Dayou",
                "명나라 항왜장군. 척계광과 함께 왜구를 격파했어요."));
            list.Add(new Entry("척계광", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://ko.wikipedia.org/wiki/척계광",
                "명나라 항왜장군. ‘기효신서’ 의 저자로도 유명해요."));
            list.Add(new Entry("피얄레 파샤", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Piyale_Pasha",
                "오스만 해군 총사령관(카푸단 파샤). 제르바·키프로스 정복의 주역이에요."));
            list.Add(new Entry("울루지 알리 레이스", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Uluç_Ali_Reis",
                "이탈리아 출신 오스만 제독. 레판토 후 함대를 재건했어요."));
            list.Add(new Entry("이순신", Gender.Male, NpcType.Escort,
                95, 85, 90,
                "https://ko.wikipedia.org/wiki/이순신",
                "거북선과 함께 우리나라 바다를 잘 지킨 장군이에요. 난중일기로도 유명해요."));
            list.Add(new Entry("원균", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://ko.wikipedia.org/wiki/원균",
                "조선 임진왜란기 수군 장군. 이순신과 종종 비교돼요."));
            list.Add(new Entry("이억기", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://ko.wikipedia.org/wiki/이억기",
                "전라우수사. 한산도 대첩 등에서 이순신과 함께 싸웠어요."));
            list.Add(new Entry("마르턴 트롬프", Gender.Male, NpcType.Escort,
                -1, 90, -1,
                "https://en.wikipedia.org/wiki/Maarten_Tromp",
                "네덜란드 황금기 제독. 영국 함대를 다운스 해전에서 격파했어요."));
            list.Add(new Entry("위터 더 빗", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Witte_Corneliszoon_de_With",
                "네덜란드 명제독. 영국·스웨덴 함대와 여러 차례 맞섰어요."));
            list.Add(new Entry("로버트 블레이크", Gender.Male, NpcType.Escort,
                -1, 90, -1,
                "https://en.wikipedia.org/wiki/Robert_Blake_(admiral)",
                "영국 해군의 아버지로 불리는 크롬웰 시대 제독이에요."));
            list.Add(new Entry("조지 멍크", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/George_Monck,_1st_Duke_of_Albemarle",
                "왕정 복고를 이끈 영국 육·해군의 거물이에요."));
            list.Add(new Entry("미힐 더 라위터르", Gender.Male, NpcType.Escort,
                -1, 95, -1,
                "https://ko.wikipedia.org/wiki/미힐_더_라위터르",
                "네덜란드 황금기 최고의 제독. 4일 해전·메드웨이 습격으로 유명해요."));
            list.Add(new Entry("코르넬리스 트롬프", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Cornelis_Tromp",
                "마르턴 트롬프의 아들. 네덜란드·덴마크에서 활약한 제독이에요."));
            list.Add(new Entry("에드워드 마운터규", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Edward_Montagu,_1st_Earl_of_Sandwich",
                "샌드위치 백작. 영국 왕정 복고와 해군 개혁의 주역이에요."));
            list.Add(new Entry("투르빌 백작", Gender.Male, NpcType.Escort,
                -1, 90, -1,
                "https://en.wikipedia.org/wiki/Anne_Hilarion_de_Tourville",
                "루이 14세 시대 프랑스 해군 원수. 비치 해드 해전에서 영네 연합을 격파했어요."));
            list.Add(new Entry("장 바르", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Jean_Bart",
                "프랑스 됭케르크 출신 사략선장 겸 해군 제독이에요."));
            list.Add(new Entry("에드워드 러셀", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Edward_Russell,_1st_Earl_of_Orford",
                "라 호그 해전에서 프랑스 함대를 격파한 영국 제독이에요."));
            list.Add(new Entry("에드워드 버넌", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Edward_Vernon",
                "‘올드 그로그’ 별명의 영국 제독. 그로그주 이름의 유래예요."));
            list.Add(new Entry("조지 앤슨", Gender.Male, NpcType.Escort,
                -1, 88, -1,
                "https://en.wikipedia.org/wiki/George_Anson,_1st_Baron_Anson",
                "1740~44년 세계 일주 항해를 마친 영국 제독이에요."));
            list.Add(new Entry("에드워드 보스코웬", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Edward_Boscawen",
                "7년 전쟁기 영국 제독. 라구스 해전 승리로 유명해요."));
            list.Add(new Entry("어거스터스 케펠", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Augustus_Keppel,_1st_Viscount_Keppel",
                "미국 독립전쟁기 영국 해군 사령관이에요."));
            list.Add(new Entry("콩트 드 그라스", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/François_Joseph_Paul_de_Grasse",
                "체사피크 해전에서 영국 함대를 막아 미국 독립을 도운 프랑스 제독이에요."));
            list.Add(new Entry("피에르 드 쉬프랑", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Pierre_André_de_Suffren",
                "인도양에서 영국 함대와 5차례 격전을 벌인 프랑스 제독이에요."));
            list.Add(new Entry("리처드 켐펜펠트", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Richard_Kempenfelt",
                "영국 해군 전술가. 우샹 곶 해전으로 유명해요."));
            list.Add(new Entry("새뮤얼 후드", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Samuel_Hood,_1st_Viscount_Hood",
                "넬슨의 멘토로도 알려진 영국 명제독이에요."));
            list.Add(new Entry("존 폴 존스", Gender.Male, NpcType.Escort,
                90, -1, -1,
                "https://ko.wikipedia.org/wiki/존_폴_존스",
                "미국 해군의 아버지로 불리는 스코틀랜드 출신 제독이에요."));
            list.Add(new Entry("호레이쇼 넬슨", Gender.Male, NpcType.Escort,
                90, 95, -1,
                "https://ko.wikipedia.org/wiki/허레이쇼_넬슨",
                "트라팔가 해전에서 영국을 구하고 전사한 영웅 제독이에요."));
            list.Add(new Entry("커스버트 콜링우드", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Cuthbert_Collingwood,_1st_Baron_Collingwood",
                "트라팔가에서 넬슨 사후 함대를 이끈 영국 제독이에요."));
            list.Add(new Entry("애덤 던컨", Gender.Male, NpcType.Escort,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Adam_Duncan,_1st_Viscount_Duncan",
                "캄퍼다운 해전에서 네덜란드 함대를 격파한 스코틀랜드 제독이에요."));

            // ════════════════ 💰 상선 / 탐험가 (40) — keenEye 우세 ════════════════
            list.Add(new Entry("마르코 폴로", Gender.Male, NpcType.Merchant,
                -1, 80, 95,
                "https://ko.wikipedia.org/wiki/마르코_폴로",
                "베네치아 상인 가문 출신. ‘동방견문록’ 으로 동아시아를 유럽에 소개했어요."));
            list.Add(new Entry("니콜로 폴로", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Niccolò_and_Maffeo_Polo",
                "마르코 폴로의 아버지. 처음 원나라까지 다녀온 베네치아 상인이에요."));
            list.Add(new Entry("마페오 폴로", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Niccolò_and_Maffeo_Polo",
                "마르코 폴로의 숙부. 형 니콜로와 함께 동방을 여행했어요."));
            list.Add(new Entry("이븐 바투타", Gender.Male, NpcType.Merchant,
                -1, -1, 95,
                "https://ko.wikipedia.org/wiki/이븐_바투타",
                "30년간 12만 km 를 여행한 모로코 출신 여행가예요."));
            list.Add(new Entry("왕대연", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Wang_Dayuan",
                "원나라 시대 동남아·인도·아라비아·동아프리카까지 항해한 여행가예요."));
            list.Add(new Entry("니콜로 데 콘티", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Niccolò_de%27_Conti",
                "15세기 인도·동남아·중국을 25년간 여행한 베네치아 상인이에요."));
            list.Add(new Entry("아흐마드 이븐 마지드", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Ahmad_ibn_Majid",
                "아라비아의 사자. 바스쿠 다 가마를 인도까지 안내한 위대한 항해사예요."));
            list.Add(new Entry("질 에아네스", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Gil_Eanes",
                "‘죽음의 곶’ 보자도르 곶을 처음 넘어선 포르투갈 항해사예요."));
            list.Add(new Entry("바르톨로메우 디아스", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://ko.wikipedia.org/wiki/바르톨로메우_디아스",
                "1488년 처음 희망봉을 돌아 동방으로 가는 길을 연 포르투갈 탐험가예요."));
            list.Add(new Entry("크리스토퍼 콜럼버스", Gender.Male, NpcType.Merchant,
                85, 85, -1,
                "https://ko.wikipedia.org/wiki/크리스토퍼_콜럼버스",
                "1492년 대서양을 건너 신대륙에 도착한 제노바 출신 항해가예요."));
            list.Add(new Entry("존 캐벗", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://ko.wikipedia.org/wiki/존_캐벗",
                "이탈리아 출신, 영국 깃발 아래 북아메리카 본토에 도달했어요."));
            list.Add(new Entry("아메리고 베스푸치", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://ko.wikipedia.org/wiki/아메리고_베스푸치",
                "‘아메리카’ 라는 대륙 이름의 어원이 된 이탈리아 항해사예요."));
            list.Add(new Entry("바스쿠 다 가마", Gender.Male, NpcType.Merchant,
                -1, 90, 85,
                "https://ko.wikipedia.org/wiki/바스쿠_다_가마",
                "1498년 인도 캘리컷에 처음 도착한 포르투갈 항해사예요."));
            list.Add(new Entry("페드루 알바르스 카브랄", Gender.Male, NpcType.Merchant,
                -1, 85, -1,
                "https://ko.wikipedia.org/wiki/페드루_알바르스_카브랄",
                "1500년 항해 중 우연히 브라질을 발견한 포르투갈 항해사예요."));
            list.Add(new Entry("디오고 캉", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Diogo_Cão",
                "콩고강 어귀를 처음 본 유럽인. 포르투갈 항해사예요."));
            list.Add(new Entry("알폰수 드 알부케르크", Gender.Male, NpcType.Merchant,
                85, 85, -1,
                "https://ko.wikipedia.org/wiki/아폰수_드_알부케르크",
                "고아·말라카·호르무즈를 차례로 정복한 포르투갈 인도 총독이에요."));
            list.Add(new Entry("코시모 데 메디치", Gender.Male, NpcType.Merchant,
                -1, -1, 90,
                "https://ko.wikipedia.org/wiki/코시모_데_메디치",
                "메디치 가문의 부를 쌓은 피렌체 은행가·정치가예요."));
            list.Add(new Entry("로렌초 데 메디치", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://ko.wikipedia.org/wiki/로렌초_데_메디치",
                "‘위대한 자(일 마니피코)’. 르네상스 피렌체의 후원자였어요."));
            list.Add(new Entry("야코프 푸거", Gender.Male, NpcType.Merchant,
                -1, -1, 95,
                "https://ko.wikipedia.org/wiki/야코프_푸거",
                "‘부자 야코프’. 아우크스부르크에서 황제도 돈을 빌린 거상이에요."));
            list.Add(new Entry("안톤 푸거", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Anton_Fugger",
                "야코프의 조카. 푸거 가문의 부를 최고조로 끌어올렸어요."));
            list.Add(new Entry("페르디난드 마젤란", Gender.Male, NpcType.Merchant,
                90, 90, -1,
                "https://ko.wikipedia.org/wiki/페르디난드_마젤란",
                "최초 세계 일주 항해를 시작한 포르투갈 출신 탐험가예요."));
            list.Add(new Entry("세바스찬 캐벗", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Sebastian_Cabot_(explorer)",
                "존 캐벗의 아들. 영국·스페인에서 활약한 탐험가·지도제작자예요."));
            list.Add(new Entry("에르난 코르테스", Gender.Male, NpcType.Merchant,
                85, -1, -1,
                "https://ko.wikipedia.org/wiki/에르난_코르테스",
                "1521년 아즈텍 제국을 정복한 스페인 콘키스타도르예요."));
            list.Add(new Entry("페로 타푸르", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Pero_Tafur",
                "15세기 유럽·중동·이집트를 여행하고 기행문을 남긴 카스티야 귀족이에요."));
            list.Add(new Entry("얀 피터르스존 콘", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Jan_Pieterszoon_Coen",
                "네덜란드 동인도회사 4대 총독. 바타비아(자카르타)를 세웠어요."));
            list.Add(new Entry("안토니 판 디멘", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Anthony_van_Diemen",
                "네덜란드 VOC 총독. 타스만을 후원해 호주 탐험을 보냈어요."));
            list.Add(new Entry("아벨 타스만", Gender.Male, NpcType.Merchant,
                -1, 85, 80,
                "https://ko.wikipedia.org/wiki/아벨_타스만",
                "타스마니아·뉴질랜드를 처음 본 네덜란드 탐험가예요."));
            list.Add(new Entry("헨드릭 브라우어", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Hendrik_Brouwer",
                "‘브라우어 항로’ 를 개척한 네덜란드 VOC 총독이에요."));
            list.Add(new Entry("변승업", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://ko.wikipedia.org/wiki/변승업",
                "조선 후기 역관 출신 거상. 박지원 ‘허생전’ 의 모델이에요."));
            list.Add(new Entry("조사이아 차일드", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Josiah_Child",
                "영국 동인도회사를 일으킨 17세기 거상이에요."));
            list.Add(new Entry("잡 차녹", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Job_Charnock",
                "캘커타(콜카타)를 건설한 영국 동인도회사 상인이에요."));
            list.Add(new Entry("로버트 클라이브", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://ko.wikipedia.org/wiki/로버트_클라이브",
                "플라시 전투로 인도 지배의 기반을 다진 영국 EIC 의 거물이에요."));
            list.Add(new Entry("피에르 에스프리 라디송", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Pierre-Esprit_Radisson",
                "허드슨 만 회사 설립의 단초가 된 프랑스 모피상이에요."));
            list.Add(new Entry("스티븐 지라드", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Stephen_Girard",
                "프랑스 출신 미국 거상. 미국에서 가장 부자였다는 평을 받아요."));
            list.Add(new Entry("미쓰이 다카토시", Gender.Male, NpcType.Merchant,
                -1, -1, 90,
                "https://ko.wikipedia.org/wiki/미쓰이_다카토시",
                "에도 시대 미쓰이 그룹의 창업자. ‘에치고야’ 포목점을 열었어요."));
            list.Add(new Entry("스미토모 마사토모", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Sumitomo_family",
                "스미토모 가문의 시조. 동(銅) 정련술로 부를 쌓았어요."));
            list.Add(new Entry("나야 스케자에몬", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://en.wikipedia.org/wiki/Sukezaemon_Naya",
                "사카이의 거상. 루손(필리핀)까지 무역하여 ‘루손스케’ 라 불렸어요."));
            list.Add(new Entry("박지원", Gender.Male, NpcType.Merchant,
                -1, -1, 90,
                "https://ko.wikipedia.org/wiki/박지원_(1737년)",
                "조선의 실학자. 청나라 사신단으로 베이징에 다녀와 ‘열하일기’ 를 썼어요."));
            list.Add(new Entry("홍대용", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://ko.wikipedia.org/wiki/홍대용",
                "조선 후기 북학파 학자. 청나라 견문을 ‘담헌연기’ 로 남겼어요."));
            list.Add(new Entry("박제가", Gender.Male, NpcType.Merchant,
                -1, -1, -1,
                "https://ko.wikipedia.org/wiki/박제가",
                "조선 실학자. ‘북학의’ 에서 청 문물 수용과 상업 진흥을 주장했어요."));

            return list;
        }

        // ─── 핵심 생성 ──────────────────────────────────────────────────────

        private static void CreateNpc(Entry e, PortData homePort, PortData destinationPort, ShipData ship, int idx)
        {
            string typeTag = e.Type switch
            {
                NpcType.Pirate => "Pirate",
                NpcType.Escort => "Escort",
                NpcType.Merchant => "Merchant",
                _ => "Npc",
            };

            string charFile = $"Char_Npc{idx:000}.asset";

            var character = ScriptableObject.CreateInstance<CharacterData>();
            character.characterId = $"char.npc{idx:000}";
            character.displayNameKo = e.NameKo;
            character.gender = e.Gender;
            character.role = CharacterRole.Adventurer;

            var stats = StatsFor(e.Type);
            character.bravery = e.Bravery >= 0 ? e.Bravery : stats.bravery;
            character.seamanship = e.Seamanship >= 0 ? e.Seamanship : stats.seamanship;
            character.keenEye = e.KeenEye >= 0 ? e.KeenEye : stats.keenEye;
            character.shortIntro = e.ShortIntro;
            character.sourceUrl = e.WikiUrl;
            AssetDatabase.CreateAsset(character, $"{DataRoot}/Characters/{charFile}");

            var def = ScriptableObject.CreateInstance<NpcDefinition>();
            def.npcId = $"npc.{typeTag.ToLower()}{idx:000}";
            def.character = character;
            def.type = e.Type;
            def.homePort = homePort;
            def.destinationPort = destinationPort;
            def.patrolPorts = System.Array.Empty<PortData>();

            def.patrolRange = e.Type switch
            {
                NpcType.Pirate => 180f,
                NpcType.Escort => 120f,
                _ => 0f,
            };

            var combat = CombatStatsFor(e.Type);
            def.cannonPower = combat.cannonPower;
            def.maxDurability = combat.maxDurability;
            def.attackInterval = combat.attackInterval;

            int basePrice = (character.bravery + character.seamanship + character.keenEye) * 10;
            def.hireBasePrice = e.Type switch
            {
                NpcType.Pirate => (int)(basePrice * 1.2f),
                NpcType.Escort => (int)(basePrice * 1.1f),
                _ => basePrice,
            };

            // 명성 게이트 — 타입별
            def.requiredGoodReputation = e.Type switch
            {
                NpcType.Escort => 5,
                NpcType.Merchant => 0,
                _ => 0,
            };
            def.requiredBadReputation = e.Type switch
            {
                NpcType.Pirate => 10,
                _ => 0,
            };

            def.shipData = ship;

            AssetDatabase.CreateAsset(def, $"{DataRoot}/Npcs/Npc_{typeTag}{idx:000}.asset");
        }

        private static List<ShipData> LoadShips(string[] names)
        {
            var list = new List<ShipData>();
            foreach (var n in names)
            {
                var s = AssetDatabase.LoadAssetAtPath<ShipData>($"{DataRoot}/Ships/{n}.asset");
                if (s != null) list.Add(s);
            }
            return list;
        }

        private static ShipData PickShip(List<ShipData> pool)
        {
            if (pool == null || pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }

        // ─── 능력치 ─────────────────────────────────────────────────────────

        private static (int bravery, int seamanship, int keenEye) StatsFor(NpcType type)
        {
            return type switch
            {
                NpcType.Pirate => (Random.Range(60, 96), Random.Range(50, 81), Random.Range(40, 61)),
                NpcType.Escort => (Random.Range(55, 86), Random.Range(55, 81), Random.Range(50, 71)),
                NpcType.Merchant => (Random.Range(25, 56), Random.Range(45, 71), Random.Range(60, 91)),
                _ => (50, 50, 50),
            };
        }

        private static (int cannonPower, int maxDurability, float attackInterval) CombatStatsFor(NpcType type)
        {
            return type switch
            {
                NpcType.Pirate => (Random.Range(4, 9), Random.Range(35, 61), Random.Range(1.4f, 2.0f)),
                NpcType.Escort => (Random.Range(3, 7), Random.Range(40, 71), Random.Range(1.5f, 2.2f)),
                NpcType.Merchant => (Random.Range(2, 5), Random.Range(30, 51), Random.Range(1.8f, 2.5f)),
                _ => (3, 40, 1.6f),
            };
        }

        // ─── 헬퍼 ───────────────────────────────────────────────────────────

        private static List<PortData> LoadAllPorts()
        {
            var list = new List<PortData>();
            var guids = AssetDatabase.FindAssets("t:PortData", new[] { $"{DataRoot}/Ports" });
            foreach (var g in guids)
            {
                var p = AssetDatabase.LoadAssetAtPath<PortData>(AssetDatabase.GUIDToAssetPath(g));
                if (p != null) list.Add(p);
            }
            return list;
        }

        private static void CleanOldAssets()
        {
            var npcGuids = AssetDatabase.FindAssets("t:NpcDefinition", new[] { $"{DataRoot}/Npcs" });
            foreach (var g in npcGuids)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(g));
            }

            // Char_Npc* 패턴만 삭제 — 다른 캐릭터(주인공 Character_*) 보존
            var charGuids = AssetDatabase.FindAssets("t:CharacterData", new[] { $"{DataRoot}/Characters" });
            foreach (var g in charGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var name = Path.GetFileName(path);
                if (name.StartsWith("Char_Npc")) AssetDatabase.DeleteAsset(path);
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
