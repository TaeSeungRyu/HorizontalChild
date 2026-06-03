using System.Text;
using Game.Data;
using Game.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 도감 (Journal) 패널 — 발견한 항목 모아보기.
    /// 사용자가 "도감" 버튼 클릭 시 표시. 발견한 항목과 아직 못 찾은 항목을 같이 보여줌.
    ///
    /// M1 단순 버전:
    ///   - 단일 큰 TMP_Text 에 모든 항목 동적 채움 (별도 entry prefab 없음)
    ///   - 발견한 항목: 카테고리 + 이름 + 메인 해설
    ///   - 못 찾은 항목: "??? (아직 발견하지 않음)"
    ///   - 발견 / 전체 카운트 상단 표시
    ///
    /// M3 폴리시:
    ///   - 카테고리별 탭
    ///   - 항목 클릭 시 상세 보기 (DiscoveryFoundPanel 재사용)
    ///   - 일러스트 카드 형식
    /// </summary>
    public class JournalPanel : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Header("Refs")]
        public TMP_Text titleText;
        public TMP_Text countText;
        public TMP_Text entryListText;
        public Button closeButton;

        [Header("Catalog — DiscoveryCatalog 우선, 비어 있으면 배열 fallback")]
        [Tooltip("DiscoveryCatalog SO. Game ▸ Refresh All Catalogs 로 자동 채워짐. 우선 사용.")]
        public DiscoveryCatalog discoveryCatalog;
        [Tooltip("Fallback: 카탈로그 없을 때 직접 등록할 발견물 배열.")]
        public DiscoveryData[] allDiscoveries;

        private DiscoveryData[] EffectiveDiscoveries =>
            (discoveryCatalog != null && discoveryCatalog.all != null && discoveryCatalog.all.Length > 0)
                ? discoveryCatalog.all : allDiscoveries;

        [Header("Service")]
        public MissionService missionService;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);

            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public void Show()
        {
            if (panelRoot == null) panelRoot = gameObject;
            if (missionService == null) missionService = MissionService.Instance;

            Refresh();
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            panelRoot.SetActive(false);
        }

        private void Refresh()
        {
            if (titleText != null) titleText.text = "도감";

            var discoveries = EffectiveDiscoveries;
            int total = discoveries?.Length ?? 0;
            int found = 0;
            if (missionService != null && discoveries != null)
            {
                foreach (var disc in discoveries)
                {
                    if (disc == null) continue;
                    if (missionService.DiscoveredIds.Contains(disc.discoveryId)) found++;
                }
            }

            if (countText != null)
            {
                countText.text = $"발견 {found} / 모두 {total}";
            }

            if (entryListText != null)
            {
                entryListText.text = BuildEntryList();
            }
        }

        private string BuildEntryList()
        {
            var discoveries = EffectiveDiscoveries;
            if (discoveries == null || discoveries.Length == 0)
            {
                return "아직 등록된 항목이 없어요.";
            }

            var sb = new StringBuilder();
            bool service = missionService != null;

            foreach (var disc in discoveries)
            {
                if (disc == null) continue;
                bool isFound = service && missionService.DiscoveredIds.Contains(disc.discoveryId);

                if (isFound)
                {
                    sb.AppendLine($"<b>[{CategoryToKorean(disc.category)}] {disc.displayNameKo}</b>");
                    sb.AppendLine($"  {disc.mainDescription}");
                }
                else
                {
                    sb.AppendLine($"<color=#888888><b>[???]</b> 아직 발견하지 못한 곳</color>");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string CategoryToKorean(DiscoveryCategory category)
        {
            return category switch
            {
                DiscoveryCategory.Landmark => "랜드마크",
                DiscoveryCategory.FloraFauna => "동식물",
                DiscoveryCategory.Ruin => "유적",
                DiscoveryCategory.Event => "사건",
                _ => "발견물"
            };
        }
    }
}
