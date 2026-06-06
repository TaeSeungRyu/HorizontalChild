using System.Collections.Generic;
using Game.Data;
using Game.Market;
using Game.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 시장 패널 — 항구의 특산물 매매.
    ///
    /// 항구에 도착하면 PortScreen 의 "시장" 버튼이 본 패널을 OpenForPort 로 호출.
    /// 표시:
    ///   - 항구 이름 + 현재 잔돈 + 화물 (사용/용량)
    ///   - 거래 가능 항목 목록 (항구 상품 + 플레이어 화물 합집합)
    ///     각 행: 이름 / 사가 / 팔가 / 보유 / [사기 1] [팔기 1] 버튼
    ///   - 닫기 버튼
    ///
    /// 시각 셋업:
    ///   기본 컴포넌트만 부착하면 ⋮ → "Auto Layout" 으로 UI 자동 구성.
    ///   세부 셋업은 MARKET_SETUP_GUIDE.md.
    /// </summary>
    public class MarketPanel : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Header("Refs — 자동 생성 가능 (Auto Layout)")]
        public TMP_Text titleText;
        public TMP_Text moneyText;
        public TMP_Text cargoText;
        public RectTransform rowsContainer;
        public Button closeButton;

        // ─── 런타임 상태 ─────────────────────────────────────────────────
        private PortData _currentPort;
        private MarketService _market;
        private PlayerCargo _cargo;
        private PlayerState _state;
        private readonly List<GameObject> _spawnedRows = new();

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public void OpenForPort(PortData port)
        {
            if (port == null) return;
            if (panelRoot == null) panelRoot = gameObject;
            _currentPort = port;

            _market = MarketService.Instance;
            _cargo = PlayerCargo.Instance;
            _state = PlayerState.Instance;

            // 이벤트 연결 — 거래 후 자동 갱신
            if (_cargo != null) _cargo.onCargoChanged.AddListener(Refresh);
            if (_state != null) _state.onMoneyChanged.AddListener(_ => Refresh());

            Refresh();
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (_cargo != null) _cargo.onCargoChanged.RemoveListener(Refresh);
            if (_state != null) _state.onMoneyChanged.RemoveListener(_ => Refresh());
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ─── UI 갱신 ──────────────────────────────────────────────────────

        private void Refresh()
        {
            if (_currentPort == null) return;

            if (titleText != null) titleText.text = $"{_currentPort.displayNameKo} 시장";

            if (moneyText != null && _state != null)
                moneyText.text = $"잔돈 {_state.Money:N0} G";

            if (cargoText != null && _cargo != null)
                cargoText.text = $"화물 {_cargo.TotalQuantity} / {_cargo.Capacity}";

            BuildRows();
        }

        private void BuildRows()
        {
            // 기존 행 제거
            foreach (var go in _spawnedRows) if (go != null) Destroy(go);
            _spawnedRows.Clear();

            if (rowsContainer == null || _currentPort == null) return;

            // 표시할 product 집합 — 항구 상품(common+special) ∪ 플레이어 화물
            var productSet = new Dictionary<string, ProductData>();
            AddRange(productSet, _currentPort.commonProducts);
            AddRange(productSet, _currentPort.specialProducts);
            if (_cargo != null)
            {
                foreach (var kvp in _cargo.Items)
                {
                    if (kvp.Value.data != null && !productSet.ContainsKey(kvp.Key))
                        productSet[kvp.Key] = kvp.Value.data;
                }
            }

            foreach (var product in productSet.Values)
            {
                var row = BuildProductRow(product);
                _spawnedRows.Add(row);
            }
        }

        private void AddRange(Dictionary<string, ProductData> dict, ProductData[] arr)
        {
            if (arr == null) return;
            foreach (var p in arr)
            {
                if (p != null && !dict.ContainsKey(p.productId)) dict[p.productId] = p;
            }
        }

        private GameObject BuildProductRow(ProductData product)
        {
            // 행: Horizontal Layout — Name / Buy Price / Sell Price / Owned / [Buy] [Sell]
            var row = new GameObject($"Row_{product.productId}",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(rowsContainer, false);

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(16, 16, 8, 8);
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 70f;

            // 항구 상품이면 구매 가능
            bool sellableAtPort = _currentPort.commonProducts != null;
            bool inPortList = ContainsProduct(_currentPort.commonProducts, product) ||
                              ContainsProduct(_currentPort.specialProducts, product);

            int buyPrice = inPortList && _market != null ? _market.GetBuyPrice(_currentPort, product) : 0;
            int sellPrice = _market != null ? _market.GetSellPrice(_currentPort, product) : 0;
            int owned = _cargo != null ? _cargo.GetQuantity(product) : 0;

            // 이름
            AddText(row.transform, product.displayNameKo, 260f, TextAlignmentOptions.Left);
            // 구매가
            AddText(row.transform,
                inPortList ? $"사기 {buyPrice}G" : "-",
                160f, TextAlignmentOptions.Right);
            // 판매가
            AddText(row.transform,
                $"팔기 {sellPrice}G",
                160f, TextAlignmentOptions.Right);
            // 보유
            AddText(row.transform, $"보유 {owned}", 120f, TextAlignmentOptions.Right);

            // Buy 버튼
            var buyBtn = AddButton(row.transform, "사기", 100f);
            buyBtn.interactable = inPortList;
            buyBtn.onClick.AddListener(() => OnBuy(product));

            // Sell 버튼
            var sellBtn = AddButton(row.transform, "팔기", 100f);
            sellBtn.interactable = owned > 0;
            sellBtn.onClick.AddListener(() => OnSell(product));

            return row;
        }

        private static bool ContainsProduct(ProductData[] arr, ProductData target)
        {
            if (arr == null) return false;
            foreach (var p in arr) if (p == target) return true;
            return false;
        }

        // 거래 처리
        private void OnBuy(ProductData product)
        {
            if (_market == null) return;
            _market.TryBuy(_currentPort, product, 1);
            // onCargoChanged / onMoneyChanged 가 자동으로 Refresh 호출
        }

        private void OnSell(ProductData product)
        {
            if (_market == null) return;
            _market.TrySell(_currentPort, product, 1);
        }

        // ─── UI 헬퍼 — 프로그래매틱 자식 생성 ─────────────────────────────

        private static TMP_Text AddText(Transform parent, string text, float width, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24f;
            tmp.alignment = align;
            tmp.color = Color.white;

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = width;
            return tmp;
        }

        private static Button AddButton(Transform parent, string label, float width)
        {
            var go = new GameObject($"Button_{label}",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.25f, 0.35f, 0.5f, 1f);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = width;

            var textGO = new GameObject("Label", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            textRT.anchoredPosition = Vector2.zero;

            return btn;
        }

        // ─── 자동 레이아웃 — JournalPanel 패턴과 동일 ─────────────────────

        [ContextMenu("Auto Layout")]
        private void AutoLayout()
        {
            if (panelRoot == null) panelRoot = gameObject;
            var panelRT = panelRoot.GetComponent<RectTransform>();
            if (panelRT == null) return;

            // 패널 — 화면 중앙 1800x900
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(1800f, 900f);
            panelRT.anchoredPosition = Vector2.zero;

            if (titleText != null) SetRect(titleText.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -40f), new Vector2(1720f, 70f));

            if (moneyText != null) SetRect(moneyText.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-400f, -120f), new Vector2(800f, 40f));

            if (cargoText != null) SetRect(cargoText.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(400f, -120f), new Vector2(800f, 40f));

            EnsureRowsScrollView(panelRT);

            if (closeButton != null)
            {
                SetRect(closeButton.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 40f), new Vector2(240f, 80f));
            }

            Debug.Log("[MarketPanel] Auto Layout 적용 완료.");
        }

        private static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax,
            Vector2 pivot, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private void EnsureRowsScrollView(RectTransform panelRT)
        {
            // 이미 rowsContainer 가 있고 ScrollRect 안이면 그것 사용, 위치만 조정
            if (rowsContainer != null)
            {
                var scroll = rowsContainer.GetComponentInParent<ScrollRect>();
                if (scroll != null)
                {
                    SetRect(scroll.GetComponent<RectTransform>(),
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -20f), new Vector2(1720f, 540f));
                    return;
                }
            }

            // 새로 만들기
            var scrollGO = new GameObject("RowsScrollView",
                typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(panelRoot.transform, false);
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            var bg = scrollGO.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.15f);
            bg.raycastTarget = true;
            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

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

            var contentGO = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = Vector2.zero;

            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = contentGO.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRT;
            scrollRect.content = contentRT;

            rowsContainer = contentRT;

            SetRect(scrollRT,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f), new Vector2(1720f, 540f));
        }
    }
}
