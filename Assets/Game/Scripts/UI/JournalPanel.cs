using System.Collections.Generic;
using System.Text;
using Game.Data;
using Game.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 도감 (Journal) 패널 — 발견한 항목 모아보기.
    /// 사용자가 "도감" 버튼 클릭 시 표시. 발견한 항목과 아직 못 찾은 항목을 같이 보여줌.
    ///
    /// 이번 버전 (M3 완성):
    ///   - 카테고리별 그룹화 (랜드마크 / 동식물 / 유적 / 사건)
    ///   - 각 카테고리 진행도 (발견/전체) 표시
    ///   - 발견 항목 이름이 TMP &lt;link&gt; 로 감싸져 있어 탭 시 DiscoveryFoundPanel 재실행
    ///   - 미발견 항목은 "???" 로 표시 (스포일러 방지)
    /// </summary>
    public class JournalPanel : MonoBehaviour, IPointerClickHandler
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Header("Refs")]
        public TMP_Text titleText;
        public TMP_Text countText;
        public TMP_Text entryListText;
        public Button closeButton;

        [Tooltip("발견 항목 이름 탭 시 띄울 상세 패널. 비어 있으면 자동 검색 (비활성 객체 포함).")]
        public DiscoveryFoundPanel reopenPanel;

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
            if (reopenPanel == null)
                reopenPanel = FindAnyObjectByType<DiscoveryFoundPanel>(FindObjectsInactive.Include);

            Refresh();
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            panelRoot.SetActive(false);
        }

        // ─── 클릭 처리 — TMP link 탭 시 상세 패널 재실행 ──────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (entryListText == null) return;
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(
                entryListText, eventData.position, eventData.pressEventCamera);
            if (linkIndex < 0) return;

            var link = entryListText.textInfo.linkInfo[linkIndex];
            var id = link.GetLinkID();

            var discoveries = EffectiveDiscoveries;
            if (discoveries == null) return;

            foreach (var d in discoveries)
            {
                if (d != null && d.discoveryId == id)
                {
                    if (reopenPanel != null) reopenPanel.Show(d);
                    return;
                }
            }
        }

        // ─── 본문 빌드 ────────────────────────────────────────────────────

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

            // 카테고리 순서 고정 — Landmark, FloraFauna, Ruin, Event
            DiscoveryCategory[] order =
            {
                DiscoveryCategory.Landmark,
                DiscoveryCategory.FloraFauna,
                DiscoveryCategory.Ruin,
                DiscoveryCategory.Event,
            };

            foreach (var cat in order)
            {
                var inCat = new List<DiscoveryData>();
                foreach (var d in discoveries)
                {
                    if (d != null && d.category == cat) inCat.Add(d);
                }
                if (inCat.Count == 0) continue;

                int catFound = 0;
                if (service)
                {
                    foreach (var d in inCat)
                    {
                        if (missionService.DiscoveredIds.Contains(d.discoveryId)) catFound++;
                    }
                }

                sb.AppendLine($"<size=130%><b>[{CategoryToKorean(cat)}]</b></size> {catFound}/{inCat.Count} 발견");
                sb.AppendLine();

                foreach (var d in inCat)
                {
                    bool isFound = service && missionService.DiscoveredIds.Contains(d.discoveryId);
                    if (isFound)
                    {
                        // <link> 로 감싸 OnPointerClick 에서 탭 감지
                        sb.AppendLine(
                            $"<link=\"{d.discoveryId}\"><b><u>{d.displayNameKo}</u></b></link>");
                        if (!string.IsNullOrEmpty(d.mainDescription))
                        {
                            sb.AppendLine($"  {d.mainDescription}");
                        }
                    }
                    else
                    {
                        sb.AppendLine("<color=#888888>  ???  (아직 발견하지 못한 곳)</color>");
                    }
                    sb.AppendLine();
                }
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

        // ─── 자동 레이아웃 ──────────────────────────────────────────────────
        // 인스펙터의 컴포넌트 우상단 ⋮ → "Auto Layout" 으로 호출
        // 자식 위치·크기 정리 + EntryListText 를 ScrollView 로 감쌈 (이미 있으면 재사용)

        [ContextMenu("Auto Layout")]
        private void AutoLayout()
        {
            if (panelRoot == null) panelRoot = gameObject;
            var panelRT = panelRoot.GetComponent<RectTransform>();
            if (panelRT == null)
            {
                Debug.LogWarning("[JournalPanel] panelRoot 에 RectTransform 이 없음");
                return;
            }

            // 1) 패널 크기 — 화면 중앙 1800x900
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(1800f, 900f);
            panelRT.anchoredPosition = Vector2.zero;

            // 2) Title — 상단 (위쪽 40 마진, 높이 70)
            if (titleText != null)
                SetRect(titleText.rectTransform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    pivot: new Vector2(0.5f, 1f),
                    pos: new Vector2(0f, -40f), size: new Vector2(1720f, 70f));

            // 3) Count — Title 바로 아래 (간격 짧게)
            if (countText != null)
                SetRect(countText.rectTransform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    pivot: new Vector2(0.5f, 1f),
                    pos: new Vector2(0f, -120f), size: new Vector2(1720f, 40f));

            // 4) ScrollView — 중앙 (Count 아래 ~ Close 위 사이 영역)
            EnsureEntryScrollView(panelRT);

            // 5) Close — 하단 중앙 (아래 40 마진)
            if (closeButton != null)
            {
                var crt = closeButton.GetComponent<RectTransform>();
                SetRect(crt,
                    anchorMin: new Vector2(0.5f, 0f), anchorMax: new Vector2(0.5f, 0f),
                    pivot: new Vector2(0.5f, 0f),
                    pos: new Vector2(0f, 40f), size: new Vector2(240f, 80f));
            }

            Debug.Log("[JournalPanel] Auto Layout 적용 완료. ScrollView 도 자동 생성·연결됨.");
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private void EnsureEntryScrollView(RectTransform panelRT)
        {
            if (entryListText == null) return;

            // 이미 ScrollRect 안에 있으면 그것의 RectTransform 만 크기 조정
            var existing = entryListText.GetComponentInParent<ScrollRect>();
            ScrollRect scrollRect;
            RectTransform scrollRT;

            if (existing != null)
            {
                scrollRect = existing;
                scrollRT = scrollRect.GetComponent<RectTransform>();
            }
            else
            {
                // 새로 생성 — ScrollView / Viewport / Content
                var scrollGO = new GameObject("EntryScrollView",
                    typeof(RectTransform), typeof(Image), typeof(ScrollRect));
                scrollGO.transform.SetParent(panelRoot.transform, false);
                scrollRT = scrollGO.GetComponent<RectTransform>();

                var bg = scrollGO.GetComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.15f); // 살짝 어두운 배경
                bg.raycastTarget = true;

                scrollRect = scrollGO.GetComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;

                // Viewport
                var viewportGO = new GameObject("Viewport",
                    typeof(RectTransform), typeof(Image), typeof(Mask));
                viewportGO.transform.SetParent(scrollGO.transform, false);
                var viewportRT = viewportGO.GetComponent<RectTransform>();
                viewportRT.anchorMin = Vector2.zero;
                viewportRT.anchorMax = Vector2.one;
                viewportRT.sizeDelta = Vector2.zero;
                viewportRT.anchoredPosition = Vector2.zero;
                viewportRT.pivot = new Vector2(0f, 1f);

                var viewportImage = viewportGO.GetComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0.004f);
                var mask = viewportGO.GetComponent<Mask>();
                mask.showMaskGraphic = false;

                // Content
                var contentGO = new GameObject("Content",
                    typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                contentGO.transform.SetParent(viewportGO.transform, false);
                var contentRT = contentGO.GetComponent<RectTransform>();
                contentRT.anchorMin = new Vector2(0f, 1f);
                contentRT.anchorMax = new Vector2(1f, 1f);
                contentRT.pivot = new Vector2(0.5f, 1f);
                contentRT.anchoredPosition = Vector2.zero;
                contentRT.sizeDelta = new Vector2(0f, 0f);

                var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(24, 24, 24, 24);
                vlg.spacing = 8f;
                vlg.childAlignment = TextAnchor.UpperLeft;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                var fitter = contentGO.GetComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // EntryListText 를 Content 안으로 이동
                entryListText.transform.SetParent(contentGO.transform, false);
                var textRT = entryListText.rectTransform;
                textRT.anchorMin = new Vector2(0f, 1f);
                textRT.anchorMax = new Vector2(1f, 1f);
                textRT.pivot = new Vector2(0.5f, 1f);
                textRT.anchoredPosition = Vector2.zero;
                textRT.sizeDelta = new Vector2(0f, 100f); // 초기값 — VLG/Fitter 가 갱신

                entryListText.enableWordWrapping = true;

                scrollRect.viewport = viewportRT;
                scrollRect.content = contentRT;
            }

            // ScrollView 의 위치·크기 — 패널 중앙, 560 높이 (Count 와 Close 사이)
            SetRect(scrollRT,
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                pos: new Vector2(0f, 0f), size: new Vector2(1720f, 560f));
        }
    }
}
