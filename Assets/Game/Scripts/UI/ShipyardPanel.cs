using System.Collections.Generic;
using Game.Data;
using Game.Player;
using Game.Save;
using Game.Ship;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 조선소 패널 — 항구의 ShipCatalog 에 등록된 배를 매매.
    ///
    /// 동작:
    ///   - 각 행: 이름 + 능력치 (대포·속도·화물·내구) + 가격 + [사기] 버튼
    ///   - 사기 클릭 → 돈 차감 + playerShip.shipData 교체
    ///   - 현재 사용 중인 배는 "현재 사용 중" 표시 (구매 비활성)
    ///
    /// 시각: MarketPanel 패턴 (Auto Layout, 풀스크린 불투명, 동적 행).
    /// </summary>
    public class ShipyardPanel : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Header("Refs — Auto Layout 가능")]
        public TMP_Text titleText;
        public TMP_Text moneyText;
        public TMP_Text currentShipText;
        public RectTransform rowsContainer;
        public Button closeButton;

        [Header("Data")]
        public ShipCatalog shipCatalog;
        public ShipController playerShip;

        private PortData _currentPort;
        private PlayerState _state;
        private readonly List<GameObject> _spawnedRows = new();

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        public void OpenForPort(PortData port)
        {
            if (port == null) return;
            if (panelRoot == null) panelRoot = gameObject;
            _currentPort = port;
            _state = PlayerState.Instance;
            if (playerShip == null) playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
            if (_state != null)
            {
                _state.onMoneyChanged.RemoveListener(OnMoneyChanged);
                _state.onMoneyChanged.AddListener(OnMoneyChanged);
            }

            Refresh();
            panelRoot.SetActive(true);
            if (closeButton != null) closeButton.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (_state != null) _state.onMoneyChanged.RemoveListener(OnMoneyChanged);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnMoneyChanged(int _) => Refresh();

        private void Refresh()
        {
            if (titleText != null) titleText.text = $"{(_currentPort != null ? _currentPort.displayNameKo : "")} 조선소";
            if (moneyText != null && _state != null) moneyText.text = $"잔돈 {_state.Money:N0} G";
            if (currentShipText != null && playerShip != null && playerShip.shipData != null)
                currentShipText.text = $"현재 배: <b>{playerShip.shipData.displayName}</b>";

            BuildRows();
        }

        private void BuildRows()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            foreach (var go in _spawnedRows)
            {
                if (go == null) continue;
                go.transform.SetParent(null);
                Destroy(go);
            }
            _spawnedRows.Clear();

            if (rowsContainer == null || shipCatalog == null || shipCatalog.all == null) return;

            foreach (var ship in shipCatalog.all)
            {
                if (ship == null) continue;
                _spawnedRows.Add(BuildShipRow(ship));
            }
        }

        private GameObject BuildShipRow(ShipData ship)
        {
            var row = new GameObject($"Row_{ship.shipId}",
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
            le.preferredHeight = 80f;

            bool isCurrent = playerShip != null && playerShip.shipData == ship;
            bool canAfford = _state != null && _state.Money >= ship.basePrice;

            AddText(row.transform, ship.displayName, 220f, TextAlignmentOptions.Left);
            AddText(row.transform,
                $"대포 {ship.cannonPower} / 속 {ship.speed}\n화물 {ship.cargoCapacity} / 내구 {ship.maxDurability}",
                360f, TextAlignmentOptions.Left, 20f);
            AddText(row.transform, $"{ship.basePrice:N0} G", 160f, TextAlignmentOptions.Right);

            var buyBtn = AddButton(row.transform, isCurrent ? "사용 중" : "구매", 120f);
            buyBtn.interactable = !isCurrent && canAfford;
            buyBtn.onClick.AddListener(() => OnBuy(ship));

            return row;
        }

        private void OnBuy(ShipData ship)
        {
            if (ship == null || playerShip == null || _state == null) return;
            if (playerShip.shipData == ship) return;
            if (!_state.TrySpend(ship.basePrice))
            {
                Debug.Log($"[ShipyardPanel] 돈 부족: 필요 {ship.basePrice}");
                return;
            }
            playerShip.shipData = ship;
            playerShip.RefreshVisual();   // prefab3D 갱신 — 인스펙터에 모델 할당돼 있으면 즉시 외형 교체
            SaveService.Instance?.SaveGame();   // 구매 즉시 저장 — 다음 로드에서 이 배로 시작
            Debug.Log($"[ShipyardPanel] 새 배 구매: {ship.displayName} ({ship.basePrice}G)");
            Refresh();
        }

        // ─── UI 헬퍼 (MarketPanel 과 동일 패턴) ─────────────────────────────

        private static TMP_Text AddText(Transform parent, string text, float width, TextAlignmentOptions align, float fontSize = 24f)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
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
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.4f, 0.4f, 0.4f);
            cb.fadeDuration = 0f;
            btn.colors = cb;
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

        // ─── 자동 레이아웃 ──────────────────────────────────────────────────

        [ContextMenu("Auto Layout")]
        private void AutoLayout()
        {
            UIScrollPanelLayout.ApplyFullscreenWithScrollList(
                panelRoot != null ? panelRoot : gameObject,
                titleText, new TMP_Text[] { moneyText, currentShipText },
                ref rowsContainer, closeButton);
            Debug.Log("[ShipyardPanel] Auto Layout 적용 완료.");
        }
    }
}
