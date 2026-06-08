using Game.Data;
using Game.Player;
using Game.Save;
using Game.Ship;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 항구 ▸ 배 수리 패널 (GAME_MECHANICS §2.3 공식).
    ///
    /// 수리 비용:
    ///   repairCost = shipPrice × floor(damagedRatio × 10) × 0.01
    ///   damagedRatio = (max − current) / max
    ///   → 잃은 내구도의 10% 마다 판매가의 1% 부과.
    ///
    /// 동작:
    ///   - 현재/최대 내구도 표시
    ///   - 수리비 표시 (잔돈 부족하면 비활성)
    ///   - [수리하기] → 돈 차감 + playerShip.RestoreDurability() + 즉시 저장
    ///
    /// 손상 0 이면 수리비 0 — 버튼 비활성, "이미 깔끔해요" 표시.
    /// </summary>
    public class ShipRepairPanel : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Header("Refs — Auto Layout 가능")]
        public TMP_Text titleText;
        public TMP_Text moneyText;
        public TMP_Text durabilityText;
        public TMP_Text costText;
        public TMP_Text statusText;
        public Button repairButton;
        public Button closeButton;

        [Header("Game Refs")]
        public ShipController playerShip;

        private PortData _currentPort;
        private PlayerState _state;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
            if (repairButton != null)
            {
                repairButton.onClick.RemoveListener(OnRepair);
                repairButton.onClick.AddListener(OnRepair);
            }
        }

        public void OpenForPort(PortData port)
        {
            if (port == null) return;
            if (panelRoot == null) panelRoot = gameObject;
            _currentPort = port;
            _state = PlayerState.Instance;
            if (playerShip == null) playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);

            if (_state != null)
            {
                _state.onMoneyChanged.RemoveListener(OnMoneyChanged);
                _state.onMoneyChanged.AddListener(OnMoneyChanged);
            }

            Refresh();
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (_state != null) _state.onMoneyChanged.RemoveListener(OnMoneyChanged);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnMoneyChanged(int _) => Refresh();

        private void Refresh()
        {
            if (titleText != null)
                titleText.text = $"{(_currentPort != null ? _currentPort.displayNameKo : "")} 배 수리";
            if (moneyText != null && _state != null)
                moneyText.text = $"잔돈 {_state.Money:N0} G";

            if (playerShip == null || playerShip.shipData == null)
            {
                if (durabilityText != null) durabilityText.text = "배 정보 없음";
                if (costText != null) costText.text = "";
                if (statusText != null) statusText.text = "";
                if (repairButton != null) repairButton.interactable = false;
                return;
            }

            int cur = playerShip.CurrentDurability;
            int max = playerShip.MaxDurability;
            int cost = ComputeRepairCost(playerShip.shipData, cur, max);

            if (durabilityText != null)
                durabilityText.text = $"내구도 <b>{cur} / {max}</b>";
            if (costText != null)
                costText.text = cost > 0 ? $"수리비 {cost:N0} G" : "수리 필요 없음";

            bool damaged = cur < max;
            bool canAfford = _state != null && _state.Money >= cost;
            if (repairButton != null)
            {
                repairButton.interactable = damaged && canAfford;
                var label = repairButton.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = damaged ? "수리하기" : "깔끔함";
            }

            if (statusText != null)
            {
                if (!damaged) statusText.text = "배가 깨끗해요. 출항할 준비 됐어요.";
                else if (!canAfford) statusText.text = $"돈이 {cost - (_state != null ? _state.Money : 0):N0} G 부족해요.";
                else statusText.text = "수리하기 버튼을 누르세요.";
            }
        }

        /// <summary>§2.3 공식: shipPrice × floor(damagedRatio × 10) × 0.01.</summary>
        public static int ComputeRepairCost(ShipData ship, int current, int max)
        {
            if (ship == null || max <= 0 || current >= max) return 0;
            float damagedRatio = (max - current) / (float)max;
            int tens = Mathf.FloorToInt(damagedRatio * 10f);   // 0~10
            return ship.basePrice * tens / 100;
        }

        private void OnRepair()
        {
            if (playerShip == null || playerShip.shipData == null || _state == null) return;
            int cost = ComputeRepairCost(playerShip.shipData, playerShip.CurrentDurability, playerShip.MaxDurability);
            if (cost <= 0) return;
            if (!_state.TrySpend(cost))
            {
                Debug.Log($"[ShipRepairPanel] 돈 부족: 필요 {cost}");
                Refresh();
                return;
            }
            playerShip.RestoreDurability();
            Debug.Log($"[ShipRepairPanel] 수리 완료 — {cost} G 차감, 내구도 {playerShip.CurrentDurability}/{playerShip.MaxDurability}");
            SaveService.Instance?.SaveGame();
            Refresh();
        }

        // ─── 자동 레이아웃 ──────────────────────────────────────────────────

        [ContextMenu("Auto Layout")]
        private void AutoLayout()
        {
            if (panelRoot == null) panelRoot = gameObject;
            var rt = panelRoot.GetComponent<RectTransform>();
            if (rt == null) return;

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = panelRoot.GetComponent<Image>();
            if (bg == null) bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.15f, 0.2f, 0.95f);
            bg.raycastTarget = true;

            if (titleText != null)
                LayoutText(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f),
                    new Vector2(1000f, 100f), 64f, TextAlignmentOptions.Center, Color.white);
            if (moneyText != null)
                LayoutText(moneyText.rectTransform, new Vector2(1f, 1f), new Vector2(-200f, -100f),
                    new Vector2(360f, 60f), 32f, TextAlignmentOptions.Right, new Color(0.9f, 0.85f, 0.4f));
            if (durabilityText != null)
                LayoutText(durabilityText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f),
                    new Vector2(800f, 80f), 40f, TextAlignmentOptions.Center, Color.white);
            if (costText != null)
                LayoutText(costText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f),
                    new Vector2(800f, 60f), 36f, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.4f));
            if (statusText != null)
                LayoutText(statusText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f),
                    new Vector2(900f, 50f), 26f, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.85f));
            if (repairButton != null)
                LayoutRect(repairButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -160f), new Vector2(360f, 100f));
            if (closeButton != null)
                LayoutRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                    new Vector2(0f, 100f), new Vector2(260f, 80f));

            Debug.Log("[ShipRepairPanel] Auto Layout 완료.");
        }

        private static void LayoutText(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size,
            float fontSize, TextAlignmentOptions align, Color color)
        {
            if (rt == null) return;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var tmp = rt.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.fontSize = fontSize;
                tmp.alignment = align;
                tmp.color = color;
                tmp.enableWordWrapping = true;
            }
        }

        private static void LayoutRect(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }
    }
}
