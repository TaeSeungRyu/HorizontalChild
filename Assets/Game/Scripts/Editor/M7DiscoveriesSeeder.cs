using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M7 — 발견물 100개 완성. 기존 24 + 76 신규.
    /// 전부 위키피디아 검증 가능한 실제 명소·유적·자연·역사·동물.
    ///
    /// 메뉴: Game/Seed M7 Discoveries
    /// 기존 .asset 은 건드리지 않음. 시드 후 Game ▸ Refresh All Catalogs 실행.
    /// </summary>
    public static class M7DiscoveriesSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M7 Discoveries")]
        public static void SeedM7Discoveries()
        {
            EnsureFolder($"{DataRoot}/Discoveries");

            // ─── 아메리카 (18) ──────────────────────────────────────────────
            Create("Discovery_NiagaraFalls.asset", "disc.niagara_falls", "나이아가라 폭포",
                DiscoveryCategory.Landmark, 43.08f, -79.07f,
                "굉음을 내며 떨어지는 거대한 폭포. 캐나다와 미국 국경에 있어요.",
                "수만 톤의 물이 매초 떨어져 무지개를 만들어요.",
                "근세", "https://ko.wikipedia.org/wiki/나이아가라_폭포");

            Create("Discovery_GrandCanyon.asset", "disc.grand_canyon", "그랜드 캐니언",
                DiscoveryCategory.Landmark, 36.10f, -112.10f,
                "콜로라도 강이 수백만 년에 걸쳐 깎아낸 거대한 협곡이에요.",
                "깊이 1.6 km, 길이 446 km 의 자연 박물관.",
                "고생대~", "https://ko.wikipedia.org/wiki/그랜드_캐니언");

            Create("Discovery_Yellowstone.asset", "disc.yellowstone", "옐로스톤 간헐천",
                DiscoveryCategory.Landmark, 44.43f, -110.59f,
                "땅속에서 펄펄 끓는 물이 솟구쳐 오르는 신기한 들판이에요.",
                "거대한 화산 위에 자리한 세계 최초의 국립공원.",
                "원시", "https://ko.wikipedia.org/wiki/옐로스톤_국립공원");

            Create("Discovery_MississippiRiver.asset", "disc.mississippi", "미시시피 강",
                DiscoveryCategory.Landmark, 32.30f, -90.90f,
                "북미 한가운데를 가로지르는 길고 굵은 큰 강이에요.",
                "원주민과 무역선이 함께 다니던 북미의 동맥.",
                "—", "https://ko.wikipedia.org/wiki/미시시피_강");

            Create("Discovery_AmericanBison.asset", "disc.american_bison", "아메리카 들소",
                DiscoveryCategory.FloraFauna, 44.45f, -103.50f,
                "넓은 초원을 떼지어 달리는 거대한 들소예요.",
                "원주민이 식량과 옷, 도구로 두루 썼어요.",
                "—", "https://ko.wikipedia.org/wiki/아메리카들소");

            Create("Discovery_ChichenItza.asset", "disc.chichen_itza", "치첸이트사",
                DiscoveryCategory.Ruin, 20.68f, -88.57f,
                "마야 사람들이 세운 거대한 계단 피라미드 신전이에요.",
                "춘분에 햇빛이 뱀이 내려오는 모양 그림자를 만들어요.",
                "마야 600~1200년", "https://ko.wikipedia.org/wiki/치첸_이트사");

            Create("Discovery_Tenochtitlan.asset", "disc.tenochtitlan", "테노치티틀란",
                DiscoveryCategory.Ruin, 19.43f, -99.13f,
                "호수 가운데에 세워진 아즈텍 제국의 화려한 수도였어요.",
                "현재의 멕시코시티 자리. 스페인이 1521년 정복.",
                "아즈텍 1325~1521", "https://ko.wikipedia.org/wiki/테노치티틀란");

            Create("Discovery_CaribbeanFlamingo.asset", "disc.caribbean_flamingo", "카리브 홍학",
                DiscoveryCategory.FloraFauna, 22.50f, -78.50f,
                "분홍빛 다리로 한 발로 서서 자는 우아한 큰 새예요.",
                "먹는 새우의 색소가 깃털을 분홍으로 물들여요.",
                "—", "https://ko.wikipedia.org/wiki/아메리카홍학");

            Create("Discovery_MachuPicchu.asset", "disc.machu_picchu", "마추픽추",
                DiscoveryCategory.Ruin, -13.16f, -72.55f,
                "구름 위 안데스 산꼭대기에 잉카가 지은 비밀 도시예요.",
                "1911년 다시 발견되기까지 500년간 숨어 있었어요.",
                "잉카 1450~1572", "https://ko.wikipedia.org/wiki/마추픽추");

            Create("Discovery_AmazonRiver.asset", "disc.amazon_river", "아마존 강",
                DiscoveryCategory.Landmark, -3.10f, -60.03f,
                "세계에서 물이 가장 많은 강. 푸른 정글이 끝없이 펼쳐져요.",
                "수천 종의 동식물이 어울려 사는 거대한 생명의 강.",
                "—", "https://ko.wikipedia.org/wiki/아마존_강");

            Create("Discovery_Galapagos.asset", "disc.galapagos", "갈라파고스 제도",
                DiscoveryCategory.Landmark, -0.93f, -90.96f,
                "거대한 거북과 신기한 새들이 사는 외딴섬들이에요.",
                "다윈이 진화론의 영감을 받은 자연의 박물관.",
                "—", "https://ko.wikipedia.org/wiki/갈라파고스_제도");

            Create("Discovery_IguazuFalls.asset", "disc.iguazu_falls", "이과수 폭포",
                DiscoveryCategory.Landmark, -25.69f, -54.44f,
                "수많은 줄기로 갈라지며 떨어지는 거대한 정글 폭포.",
                "남미 브라질과 아르헨티나 국경에 있는 자연의 경이.",
                "—", "https://ko.wikipedia.org/wiki/이과수_폭포");

            Create("Discovery_AndesMountains.asset", "disc.andes", "안데스 산맥",
                DiscoveryCategory.Landmark, -32.65f, -70.01f,
                "남미 서쪽을 길게 가로지르는 세계에서 가장 긴 산맥이에요.",
                "잉카 사람들이 산 능선을 따라 길을 만들었어요.",
                "—", "https://ko.wikipedia.org/wiki/안데스_산맥");

            Create("Discovery_Llama.asset", "disc.llama", "라마",
                DiscoveryCategory.FloraFauna, -16.50f, -68.15f,
                "안데스 산에서 짐을 나르는 부드러운 털의 동물이에요.",
                "잉카는 라마 없이는 산속을 다닐 수 없었어요.",
                "—", "https://ko.wikipedia.org/wiki/라마");

            Create("Discovery_EasterIsland.asset", "disc.easter_island", "이스터섬 모아이",
                DiscoveryCategory.Ruin, -27.12f, -109.36f,
                "거대한 사람 얼굴 돌상이 바다를 바라보는 외딴섬이에요.",
                "어떻게 옮겼는지 아직도 수수께끼.",
                "라파누이 1250~1500", "https://ko.wikipedia.org/wiki/이스터_섬");

            Create("Discovery_Cusco.asset", "disc.cusco", "쿠스코",
                DiscoveryCategory.Ruin, -13.52f, -71.97f,
                "잉카 제국의 수도. 사람 모양으로 설계된 신성한 도시.",
                "안데스 한가운데, '세상의 배꼽' 으로 불렸어요.",
                "잉카", "https://ko.wikipedia.org/wiki/쿠스코");

            Create("Discovery_MagellanStrait.asset", "disc.magellan_strait", "마젤란 해협",
                DiscoveryCategory.Event, -54.00f, -70.00f,
                "남미 끝, 두 대양을 잇는 좁고 위험한 바다 길.",
                "마젤란이 1520년 처음 통과해 태평양에 닿았어요.",
                "1520", "https://ko.wikipedia.org/wiki/마젤란_해협");

            Create("Discovery_PolynesianNavigation.asset", "disc.polynesian_nav", "폴리네시안 항해술",
                DiscoveryCategory.Event, -17.55f, -149.55f,
                "별과 바람만으로 태평양을 누빈 폴리네시아 사람들의 지혜.",
                "지도 없이 카누로 수천 km 를 건넜어요.",
                "고대~", "https://ko.wikipedia.org/wiki/폴리네시아");

            // ─── 유럽 (10) ──────────────────────────────────────────────────
            Create("Discovery_Alps.asset", "disc.alps", "알프스 산맥",
                DiscoveryCategory.Landmark, 46.50f, 8.50f,
                "유럽 한가운데 솟은 눈 덮인 거대한 산맥이에요.",
                "한니발이 코끼리를 끌고 넘었던 곳.",
                "—", "https://ko.wikipedia.org/wiki/알프스_산맥");

            Create("Discovery_Stonehenge.asset", "disc.stonehenge", "스톤헨지",
                DiscoveryCategory.Ruin, 51.18f, -1.83f,
                "들판 한복판에 동그랗게 늘어선 거대한 돌기둥들.",
                "수천 년 전 누가 왜 세웠는지 정확히 몰라요.",
                "기원전 3000년경", "https://ko.wikipedia.org/wiki/스톤헨지");

            Create("Discovery_Vesuvius.asset", "disc.vesuvius", "베수비오 화산",
                DiscoveryCategory.Landmark, 40.82f, 14.43f,
                "나폴리만 옆에 솟은 화산. 79년 폼페이를 삼켰어요.",
                "지금도 가끔 연기를 뿜는 살아있는 화산.",
                "고대~", "https://ko.wikipedia.org/wiki/베수비오_산");

            Create("Discovery_NotreDame.asset", "disc.notre_dame", "노트르담 대성당",
                DiscoveryCategory.Landmark, 48.85f, 2.35f,
                "파리 강 한복판 섬에 세워진 거대한 고딕 성당이에요.",
                "850년이 넘는 시간을 견딘 아름다운 종탑.",
                "1163~1345", "https://ko.wikipedia.org/wiki/노트르담_대성당");

            Create("Discovery_Versailles.asset", "disc.versailles", "베르사유 궁전",
                DiscoveryCategory.Landmark, 48.80f, 2.12f,
                "루이 14세가 지은 황금빛 거대한 궁전과 정원.",
                "거울의 방, 분수, 끝없는 정원이 화려해요.",
                "1682~", "https://ko.wikipedia.org/wiki/베르사유_궁전");

            Create("Discovery_Colosseum.asset", "disc.colosseum", "콜로세움",
                DiscoveryCategory.Ruin, 41.89f, 12.49f,
                "로마 한복판에 우뚝 선 거대한 원형 경기장이에요.",
                "검투사가 싸우던 곳. 5만 명을 수용했어요.",
                "80년", "https://ko.wikipedia.org/wiki/콜로세움");

            Create("Discovery_SantiagoCompostela.asset", "disc.santiago_compostela", "산티아고 데 콤포스텔라",
                DiscoveryCategory.Landmark, 42.88f, -8.55f,
                "유럽 사람들이 천 년 동안 걸어가던 순례의 종착지.",
                "성 야고보의 무덤이 있는 곳으로 알려졌어요.",
                "9세기~", "https://ko.wikipedia.org/wiki/산티아고_데_콤포스텔라");

            Create("Discovery_DanubeRiver.asset", "disc.danube", "도나우 강",
                DiscoveryCategory.Landmark, 44.50f, 28.70f,
                "유럽을 동서로 가로질러 흑해까지 흐르는 큰 강이에요.",
                "독일·오스트리아·헝가리·루마니아 여러 나라를 지나가요.",
                "—", "https://ko.wikipedia.org/wiki/도나우_강");

            Create("Discovery_RhineRiver.asset", "disc.rhine", "라인 강",
                DiscoveryCategory.Landmark, 50.00f, 7.50f,
                "독일을 가르며 흐르는 거대한 강. 양옆에 옛 성들이 즐비.",
                "유럽 상선과 뗏목 무역의 중심 통로.",
                "—", "https://ko.wikipedia.org/wiki/라인_강");

            Create("Discovery_Geirangerfjord.asset", "disc.geirangerfjord", "게이랑게르 피요르드",
                DiscoveryCategory.Landmark, 62.10f, 7.20f,
                "노르웨이 산 사이로 깊게 들어온 바닷길. 폭포가 줄지어 떨어져요.",
                "빙하가 깎아낸 자연의 수로.",
                "—", "https://ko.wikipedia.org/wiki/게이랑게르피오르");

            // ─── 아프리카 (12) ──────────────────────────────────────────────
            Create("Discovery_SaharaDesert.asset", "disc.sahara", "사하라 사막",
                DiscoveryCategory.Landmark, 23.42f, 25.32f,
                "끝없이 펼쳐진 황금빛 모래 바다. 세계에서 가장 큰 더운 사막.",
                "낙타와 함께 사람들이 별을 보며 길을 찾았어요.",
                "—", "https://ko.wikipedia.org/wiki/사하라");

            Create("Discovery_Kilimanjaro.asset", "disc.kilimanjaro", "킬리만자로",
                DiscoveryCategory.Landmark, -3.07f, 37.35f,
                "적도 가까이 솟은 5895 m 의 화산. 꼭대기는 만년설.",
                "아프리카에서 가장 높은 산.",
                "—", "https://ko.wikipedia.org/wiki/킬리만자로_산");

            Create("Discovery_VictoriaFalls.asset", "disc.victoria_falls", "빅토리아 폭포",
                DiscoveryCategory.Landmark, -17.92f, 25.85f,
                "잠베지 강이 거대한 절벽으로 떨어지는 천둥 같은 폭포.",
                "현지인들은 '천둥이 치는 안개' 라고 불러요.",
                "—", "https://ko.wikipedia.org/wiki/빅토리아_폭포");

            Create("Discovery_RingTailedLemur.asset", "disc.ring_tailed_lemur", "고리꼬리 여우원숭이",
                DiscoveryCategory.FloraFauna, -22.00f, 46.70f,
                "검은 줄무늬 꼬리를 흔드는 마다가스카르의 귀여운 동물.",
                "여러 가족이 햇볕을 함께 쬐며 살아요.",
                "—", "https://ko.wikipedia.org/wiki/고리꼬리여우원숭이");

            Create("Discovery_AfricanLion.asset", "disc.african_lion", "아프리카 사자",
                DiscoveryCategory.FloraFauna, -2.33f, 34.83f,
                "황금빛 갈기를 휘날리며 초원을 다스리는 동물의 왕이에요.",
                "암컷들이 함께 사냥하고 새끼를 키워요.",
                "—", "https://ko.wikipedia.org/wiki/사자");

            Create("Discovery_AfricanElephant.asset", "disc.african_elephant", "아프리카 코끼리",
                DiscoveryCategory.FloraFauna, -19.50f, 23.50f,
                "땅에서 가장 큰 동물. 큰 귀와 긴 코를 가졌어요.",
                "가족이 함께 다니며 서로 돌봐요.",
                "—", "https://ko.wikipedia.org/wiki/아프리카코끼리");

            Create("Discovery_MaasaiMara.asset", "disc.maasai_mara", "마사이 마라",
                DiscoveryCategory.Landmark, -1.50f, 35.14f,
                "수백만 마리 누가 무리지어 이동하는 거대한 초원이에요.",
                "사자·치타·코끼리가 함께 사는 야생의 무대.",
                "—", "https://ko.wikipedia.org/wiki/마사이마라");

            Create("Discovery_AbuSimbel.asset", "disc.abu_simbel", "아부심벨 신전",
                DiscoveryCategory.Ruin, 22.34f, 31.62f,
                "산을 통째로 깎아 만든 거대한 람세스 2세 신전이에요.",
                "20세기에 댐 공사로 신전을 통째 옮겼어요.",
                "기원전 1264", "https://ko.wikipedia.org/wiki/아부심벨_신전");

            Create("Discovery_LuxorTemple.asset", "disc.luxor_temple", "룩소르 신전",
                DiscoveryCategory.Ruin, 25.70f, 32.64f,
                "나일강 동쪽에 자리한 거대한 기둥의 도시 신전.",
                "스핑크스가 늘어선 길로 카르낙 신전과 이어졌어요.",
                "기원전 1400", "https://ko.wikipedia.org/wiki/룩소르_신전");

            Create("Discovery_SaoTome.asset", "disc.sao_tome", "상투메 섬",
                DiscoveryCategory.Landmark, 0.34f, 6.73f,
                "적도 위 작은 화산섬. 사탕수수와 카카오의 첫 식민 농장.",
                "포르투갈이 15세기 발견 후 무역 거점으로 삼았어요.",
                "1470~", "https://ko.wikipedia.org/wiki/상투메섬");

            Create("Discovery_CapeVerde.asset", "disc.cape_verde", "카보베르데 제도",
                DiscoveryCategory.Landmark, 15.10f, -23.62f,
                "아프리카 서쪽 대서양에 떠 있는 화산섬 무리예요.",
                "대서양을 건너는 배들의 보급지.",
                "1456~", "https://ko.wikipedia.org/wiki/카보베르데");

            Create("Discovery_AyeAye.asset", "disc.aye_aye", "아이아이",
                DiscoveryCategory.FloraFauna, -15.50f, 47.40f,
                "긴 손가락으로 나무를 두드려 곤충을 찾는 마다가스카르의 신비한 동물.",
                "어두운 밤에만 활동하는 야행성 영장류.",
                "—", "https://ko.wikipedia.org/wiki/아이아이");

            // ─── 중동 / 페르시아 (5) ────────────────────────────────────────
            Create("Discovery_Petra.asset", "disc.petra", "페트라",
                DiscoveryCategory.Ruin, 30.33f, 35.45f,
                "사막의 분홍빛 절벽을 통째로 깎아 만든 비밀의 도시.",
                "나바테아 사람들의 옛 수도. 좁은 협곡을 지나면 보여요.",
                "기원전 4세기~", "https://ko.wikipedia.org/wiki/페트라");

            Create("Discovery_Persepolis.asset", "disc.persepolis", "페르세폴리스",
                DiscoveryCategory.Ruin, 29.94f, 52.89f,
                "고대 페르시아 제국의 화려한 의식 도시였어요.",
                "다리우스 1세가 세우고 알렉산드로스가 불태웠어요.",
                "기원전 515", "https://ko.wikipedia.org/wiki/페르세폴리스");

            Create("Discovery_Mecca.asset", "disc.mecca", "메카 카바",
                DiscoveryCategory.Landmark, 21.42f, 39.83f,
                "이슬람 신자들이 평생 한 번 순례하는 거룩한 성소.",
                "검은 비단으로 덮인 사각 신전. 매년 수백만이 모여요.",
                "고대~", "https://ko.wikipedia.org/wiki/카바");

            Create("Discovery_DamascusMosque.asset", "disc.damascus_mosque", "다마스쿠스 우마이야 모스크",
                DiscoveryCategory.Landmark, 33.51f, 36.30f,
                "세계에서 가장 오래된 큰 모스크 중 하나예요.",
                "옛 로마 신전과 비잔틴 성당 위에 세워졌어요.",
                "705~715", "https://ko.wikipedia.org/wiki/우마이야_대모스크");

            Create("Discovery_Jerusalem.asset", "disc.jerusalem", "예루살렘 옛 도시",
                DiscoveryCategory.Landmark, 31.78f, 35.22f,
                "유대·기독·이슬람 세 종교가 거룩하게 여기는 도시.",
                "통곡의 벽, 황금 돔, 예수 무덤 성당이 모두 있어요.",
                "고대~", "https://ko.wikipedia.org/wiki/예루살렘");

            // ─── 인도 / 남아시아 (5) ────────────────────────────────────────
            Create("Discovery_TajMahal.asset", "disc.taj_mahal", "타지마할",
                DiscoveryCategory.Landmark, 27.18f, 78.04f,
                "황제가 사랑한 왕비를 위해 지은 새하얀 대리석 무덤.",
                "달빛 아래에서 푸르게 빛나는 인도의 보석.",
                "1632~1653", "https://ko.wikipedia.org/wiki/타지마할");

            Create("Discovery_GangesRiver.asset", "disc.ganges", "갠지스 강",
                DiscoveryCategory.Landmark, 25.32f, 83.01f,
                "인도 사람들이 거룩하게 여기는 큰 강이에요.",
                "강가에서 기도하고 등불을 떠내려 보내요.",
                "—", "https://ko.wikipedia.org/wiki/갠지스_강");

            Create("Discovery_BengalTiger.asset", "disc.bengal_tiger", "벵골 호랑이",
                DiscoveryCategory.FloraFauna, 27.50f, 88.35f,
                "주황 바탕에 검은 줄무늬가 선명한 인도 정글의 사냥꾼.",
                "조용히 걷는 발걸음 — 풀잎도 흔들리지 않아요.",
                "—", "https://ko.wikipedia.org/wiki/벵골호랑이");

            Create("Discovery_IndianElephant.asset", "disc.indian_elephant", "인도 코끼리",
                DiscoveryCategory.FloraFauna, 11.46f, 76.69f,
                "사람과 친하게 일하는 똑똑한 코끼리예요.",
                "큰 나무를 옮기거나 왕족의 행렬에 참여했어요.",
                "—", "https://ko.wikipedia.org/wiki/인도코끼리");

            Create("Discovery_AjantaCaves.asset", "disc.ajanta_caves", "아잔타 석굴",
                DiscoveryCategory.Ruin, 20.55f, 75.70f,
                "절벽을 깎아 만든 30개의 동굴 안에 부처 그림이 가득.",
                "1500년 전 인도 화가들이 어둠 속에서 그렸어요.",
                "기원전 2세기~", "https://ko.wikipedia.org/wiki/아잔타_석굴");

            // ─── 동남아 (8) ────────────────────────────────────────────────
            Create("Discovery_Borobudur.asset", "disc.borobudur", "보로부두르",
                DiscoveryCategory.Ruin, -7.61f, 110.20f,
                "자바 섬 한가운데 세워진 거대한 9층 불교 사원이에요.",
                "수백 년간 정글에 묻혀 있다가 발견됐어요.",
                "9세기", "https://ko.wikipedia.org/wiki/보로부두르");

            Create("Discovery_AngkorWat.asset", "disc.angkor_wat", "앙코르와트",
                DiscoveryCategory.Ruin, 13.41f, 103.87f,
                "캄보디아 정글 속에 자리한 세계 최대의 종교 건축물.",
                "다섯 개의 탑이 우주의 산을 상징해요.",
                "12세기", "https://ko.wikipedia.org/wiki/앙코르_와트");

            Create("Discovery_MekongRiver.asset", "disc.mekong", "메콩 강",
                DiscoveryCategory.Landmark, 12.00f, 105.00f,
                "동남아 다섯 나라를 흐르는 길고 풍요로운 큰 강이에요.",
                "쌀과 물고기로 수많은 사람을 먹여요.",
                "—", "https://ko.wikipedia.org/wiki/메콩_강");

            Create("Discovery_KomodoDragon.asset", "disc.komodo_dragon", "코모도왕도마뱀",
                DiscoveryCategory.FloraFauna, -8.55f, 119.49f,
                "인도네시아 작은 섬에만 사는 세계에서 가장 큰 도마뱀.",
                "몸길이 3 m. 천천히 걷지만 사슴도 사냥해요.",
                "—", "https://ko.wikipedia.org/wiki/코모도왕도마뱀");

            Create("Discovery_Orangutan.asset", "disc.orangutan", "오랑우탄",
                DiscoveryCategory.FloraFauna, 1.07f, 110.34f,
                "보르네오와 수마트라 정글에 사는 붉은 털의 큰 원숭이예요.",
                "나뭇가지로 잠자리를 만들 줄 알아요.",
                "—", "https://ko.wikipedia.org/wiki/오랑우탄");

            Create("Discovery_ManilaGalleon.asset", "disc.manila_galleon", "마닐라 갈레온 무역",
                DiscoveryCategory.Event, 14.50f, 121.00f,
                "스페인 배가 멕시코와 마닐라를 250년간 오간 태평양 무역로.",
                "중국 비단·도자기가 신대륙 은으로 거래됐어요.",
                "1565~1815", "https://ko.wikipedia.org/wiki/마닐라_갈레온");

            Create("Discovery_Bali.asset", "disc.bali", "발리 섬",
                DiscoveryCategory.Landmark, -8.30f, 115.09f,
                "계단식 논과 힌두 사원이 어우러진 인도네시아의 아름다운 섬.",
                "벼농사·춤·조각이 매일 어우러져요.",
                "—", "https://ko.wikipedia.org/wiki/발리섬");

            Create("Discovery_HaLongBay.asset", "disc.ha_long_bay", "하롱베이",
                DiscoveryCategory.Landmark, 20.92f, 107.18f,
                "푸른 바다 위로 1600개의 돌섬이 솟아 있는 베트남 만.",
                "전설에 따르면 용이 내려와 만든 풍경.",
                "—", "https://ko.wikipedia.org/wiki/하롱_만");

            // ─── 동아시아 (10) ──────────────────────────────────────────────
            Create("Discovery_GreatWall.asset", "disc.great_wall", "만리장성",
                DiscoveryCategory.Landmark, 40.43f, 116.57f,
                "산을 따라 끝없이 이어진 거대한 돌과 흙의 성벽이에요.",
                "여러 왕조가 2000년에 걸쳐 쌓아 올렸어요.",
                "기원전 7세기~", "https://ko.wikipedia.org/wiki/만리장성");

            Create("Discovery_ForbiddenCity.asset", "disc.forbidden_city", "자금성",
                DiscoveryCategory.Landmark, 39.92f, 116.39f,
                "황제만 들어갈 수 있던 황금 지붕의 거대한 궁궐.",
                "베이징 한복판에 980채의 건물이 있어요.",
                "1406~1420", "https://ko.wikipedia.org/wiki/자금성");

            Create("Discovery_MountFuji.asset", "disc.mount_fuji", "후지산",
                DiscoveryCategory.Landmark, 35.36f, 138.73f,
                "일본을 대표하는 완벽한 원뿔 모양의 흰 화산.",
                "옛 화가들이 가장 즐겨 그린 풍경.",
                "—", "https://ko.wikipedia.org/wiki/후지산");

            Create("Discovery_Kinkakuji.asset", "disc.kinkakuji", "킨카쿠지 (금각사)",
                DiscoveryCategory.Landmark, 35.04f, 135.73f,
                "교토의 황금으로 덮인 작은 누각. 연못 위에 비쳐 두 배로 빛나요.",
                "쇼군이 별궁으로 지었다가 사찰이 됐어요.",
                "1397", "https://ko.wikipedia.org/wiki/금각사");

            Create("Discovery_GreatBuddhaKamakura.asset", "disc.great_buddha_kamakura", "카마쿠라 대불",
                DiscoveryCategory.Landmark, 35.32f, 139.54f,
                "야외에 앉아 있는 13.35 m 의 거대한 청동 불상이에요.",
                "쓰나미에 신전이 휩쓸린 뒤 700년간 비바람을 견뎠어요.",
                "1252", "https://ko.wikipedia.org/wiki/카마쿠라_대불");

            Create("Discovery_GiantPanda.asset", "disc.giant_panda", "자이언트 판다",
                DiscoveryCategory.FloraFauna, 30.25f, 102.96f,
                "중국 쓰촨 대나무 숲에 사는 흑백의 둥근 곰이에요.",
                "하루 12시간 대나무를 먹어요.",
                "—", "https://ko.wikipedia.org/wiki/대왕판다");

            Create("Discovery_YangtzeRiver.asset", "disc.yangtze", "양쯔강",
                DiscoveryCategory.Landmark, 31.00f, 113.00f,
                "중국을 동서로 가르는 아시아에서 가장 긴 강이에요.",
                "강 안에 사는 흰 돌고래가 한때 살았어요.",
                "—", "https://ko.wikipedia.org/wiki/양쯔강");

            Create("Discovery_Yoshino.asset", "disc.yoshino", "요시노 벚꽃",
                DiscoveryCategory.Landmark, 34.36f, 135.86f,
                "산 가득 분홍 벚꽃이 파도처럼 피는 일본의 명소.",
                "30,000 그루의 벚꽃이 봄마다 동시에 피어요.",
                "—", "https://ko.wikipedia.org/wiki/요시노산");

            Create("Discovery_PotalaPalace.asset", "disc.potala_palace", "포탈라 궁",
                DiscoveryCategory.Landmark, 29.66f, 91.12f,
                "티베트 라싸의 산 위에 우뚝 솟은 흰 궁전이에요.",
                "달라이 라마가 살던 13층 건물.",
                "1645", "https://ko.wikipedia.org/wiki/포탈라_궁");

            Create("Discovery_PrzewalskiHorse.asset", "disc.przewalski_horse", "프르제발스키 말",
                DiscoveryCategory.FloraFauna, 47.50f, 100.00f,
                "몽골 초원에 사는 야생 그대로의 말이에요.",
                "사람이 한 번도 길들이지 못한 진짜 야생마.",
                "—", "https://ko.wikipedia.org/wiki/프르제발스키의_말");

            // ─── 북극·고지대 (3) ────────────────────────────────────────────
            Create("Discovery_PolarBear.asset", "disc.polar_bear", "북극곰",
                DiscoveryCategory.FloraFauna, 78.00f, -20.00f,
                "얼음 위를 어슬렁거리는 세상에서 가장 큰 흰 곰이에요.",
                "물범을 사냥하며 수영을 잘 해요.",
                "—", "https://ko.wikipedia.org/wiki/북극곰");

            Create("Discovery_GreenlandIce.asset", "disc.greenland_ice", "그린란드 빙상",
                DiscoveryCategory.Landmark, 72.00f, -40.00f,
                "세계에서 두 번째로 큰 얼음 대륙. 두께가 3 km 가 넘어요.",
                "100만 년 동안 쌓인 눈이 단단히 굳어 만들어졌어요.",
                "—", "https://ko.wikipedia.org/wiki/그린란드_빙상");

            Create("Discovery_MountEverest.asset", "disc.mount_everest", "에베레스트",
                DiscoveryCategory.Landmark, 27.99f, 86.93f,
                "히말라야 꼭대기, 8849 m 세계 최고봉이에요.",
                "공기가 절반밖에 안 되는 '죽음의 지대'.",
                "—", "https://ko.wikipedia.org/wiki/에베레스트산");

            // ─── 오세아니아·시베리아 (5) ────────────────────────────────────
            Create("Discovery_GreatBarrierReef.asset", "disc.great_barrier_reef", "그레이트 배리어 리프",
                DiscoveryCategory.Landmark, -18.00f, 147.00f,
                "오스트레일리아 동쪽 바다의 거대한 산호초 벽.",
                "수천 종의 물고기와 거북이 함께 살아요. 우주에서도 보일 만큼 커요.",
                "—", "https://ko.wikipedia.org/wiki/그레이트배리어리프");

            Create("Discovery_Kiwi.asset", "disc.kiwi", "키위 새",
                DiscoveryCategory.FloraFauna, -40.90f, 174.90f,
                "날개가 작고 코로 냄새를 맡는 뉴질랜드의 신비한 새.",
                "밤에만 움직이는 야행성. 작은 갈색 키위 과일과 닮았어요.",
                "—", "https://ko.wikipedia.org/wiki/키위_(새)");

            Create("Discovery_Uluru.asset", "disc.uluru", "울루루",
                DiscoveryCategory.Landmark, -25.34f, 131.04f,
                "호주 한복판에 우뚝 솟은 거대한 붉은 돌산.",
                "원주민 아낭구 사람들에게 신성한 곳.",
                "—", "https://ko.wikipedia.org/wiki/울루루");

            Create("Discovery_Kangaroo.asset", "disc.kangaroo", "캥거루",
                DiscoveryCategory.FloraFauna, -23.70f, 133.90f,
                "주머니에 아기를 넣고 뛰어다니는 호주의 큰 동물.",
                "한 번에 9 m 를 점프해요.",
                "—", "https://ko.wikipedia.org/wiki/캥거루");

            Create("Discovery_LakeBaikal.asset", "disc.lake_baikal", "바이칼 호수",
                DiscoveryCategory.Landmark, 53.50f, 108.00f,
                "시베리아 한가운데 깊고 맑은 세계에서 가장 오래된 호수.",
                "물에는 다른 곳엔 없는 바이칼 물범이 살아요.",
                "—", "https://ko.wikipedia.org/wiki/바이칼_호");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M7DiscoveriesSeeder] 완료. 76개 추가 (총 100).\n" +
                "  아메리카 18 · 유럽 10 · 아프리카 12 · 중동 5 · 인도 5\n" +
                "  동남아 8 · 동아시아 10 · 북극·고지대 3 · 오세아니아·시베리아 5\n" +
                "\n다음: Game ▸ Refresh All Catalogs → DiscoveryCatalog 자동 채움.");
        }

        // ─── 헬퍼 ───────────────────────────────────────────────────────────

        private static DiscoveryData Create(string fileName, string id, string name,
            DiscoveryCategory category, float lat, float lng,
            string mainDesc, string moreInfo, string era, string url)
        {
            var path = $"{DataRoot}/Discoveries/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<DiscoveryData>(path);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<DiscoveryData>();
            so.discoveryId = id;
            so.displayNameKo = name;
            so.category = category;
            so.latitude = lat;
            so.longitude = lng;
            so.searchToleranceBase = 0.03f;
            so.mainDescription = mainDesc;
            so.moreInfo = moreInfo;
            so.eraLabel = era;
            so.sourceUrl = url;
            so.sensitiveExpressionChecked = true;
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
