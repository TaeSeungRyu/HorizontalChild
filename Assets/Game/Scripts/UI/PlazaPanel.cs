using System.Collections.Generic;
using Game.Combat;
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
    /// 광장 (Plaza) 패널 — 그 항구에 dwell 중인 NPC 만 고용 대상으로 표시 (§3.5).
    ///
    /// 동작:
    ///   - NpcSpawner.GetNpcsAtPort(_currentPort) 로 현재 항구에 머무는 NPC 조회
    ///   - 각 행: 이름 + 능력치 (용기·항해·눈썰미) + 고용가 + [고용] 버튼
    ///   - 고용 → 잔돈 차감 + playerShip.captain 교체 + NpcSpawner.HireNpcFromPort (풀 영구 제외)
    ///   - 항구에 NPC 가 없으면 안내 메시지
    ///
    /// 고용가: NpcDefinition.hireBasePrice 우선, 없으면 (용기+항해+눈썰미)×10 fallback.
    /// </summary>
    public class PlazaPanel : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Header("Refs — Auto Layout 가능")]
        public TMP_Text titleText;
        public TMP_Text moneyText;
        public TMP_Text currentCaptainText;
        public RectTransform rowsContainer;
        public Button closeButton;

        [Header("Data")]
        [Tooltip("Legacy — 광장이 비어있을 때 fallback 으로 사용 (옵션). NpcSpawner 의 dwell 풀이 우선.")]
        public CharacterCatalog characterCatalog;
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
            if (titleText != null) titleText.text = $"{(_currentPort != null ? _currentPort.displayNameKo : "")} 광장";
            if (moneyText != null && _state != null) moneyText.text = $"잔돈 {_state.Money:N0} G";
            if (currentCaptainText != null && playerShip != null && playerShip.captain != null)
                currentCaptainText.text = $"현재 선장: <b>{playerShip.captain.displayNameKo}</b>";

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

            if (rowsContainer == null) return;

            // 이 항구에 dwell 중인 NPC 들 (§3.5 비활동 풀)
            var spawner = NpcSpawner.Instance
                ?? FindAnyObjectByType<NpcSpawner>(FindObjectsInactive.Include);
            var npcs = spawner != null ? spawner.GetNpcsAtPort(_currentPort) : null;

            if (npcs == null || npcs.Count == 0)
            {
                _spawnedRows.Add(BuildEmptyRow());
                return;
            }

            foreach (var def in npcs)
            {
                if (def == null || def.character == null) continue;
                _spawnedRows.Add(BuildNpcRow(def));
            }
        }

        private GameObject BuildEmptyRow()
        {
            var row = new GameObject("Row_Empty",
                typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(rowsContainer, false);
            row.GetComponent<LayoutElement>().preferredHeight = 80f;
            var tmp = row.AddComponent<TextMeshProUGUI>();
            tmp.text = "지금 광장에 머무는 모험가가 없어요. 잠시 후 다시 들러보세요.";
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.8f, 0.8f, 0.8f);
            tmp.enableWordWrapping = true;
            return row;
        }

        private GameObject BuildNpcRow(NpcDefinition def)
        {
            var ch = def.character;
            var row = new GameObject($"Row_{def.npcId}",
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

            bool isCurrent = playerShip != null && playerShip.captain == ch;
            int price = HirePrice(def);
            bool canAfford = _state != null && _state.Money >= price;

            AddText(row.transform, ch.displayNameKo, 220f, TextAlignmentOptions.Left);
            AddText(row.transform,
                $"용기 {ch.bravery} / 항해 {ch.seamanship} / 눈썰미 {ch.keenEye}",
                400f, TextAlignmentOptions.Left, 22f);
            AddText(row.transform, $"{price:N0} G", 140f, TextAlignmentOptions.Right);

            var hireBtn = AddButton(row.transform, isCurrent ? "현재 선장" : "고용", 120f);
            hireBtn.interactable = !isCurrent && canAfford;
            hireBtn.onClick.AddListener(() => OnHire(def));

            return row;
        }

        private int HirePrice(NpcDefinition def)
        {
            if (def.hireBasePrice > 0) return def.hireBasePrice;
            var ch = def.character;
            return ch != null ? (ch.bravery + ch.seamanship + ch.keenEye) * 10 : 1500;
        }

        private void OnHire(NpcDefinition def)
        {
            if (def == null || def.character == null || playerShip == null || _state == null) return;
            var ch = def.character;
            if (playerShip.captain == ch) return;
            int price = HirePrice(def);
            if (!_state.TrySpend(price))
            {
                Debug.Log($"[PlazaPanel] 돈 부족: 필요 {price}");
                return;
            }
            playerShip.captain = ch;

            // 풀 영구 제외 (§3.5: 고용된 NPC 는 풀에서 영구 제외)
            var spawner = NpcSpawner.Instance
                ?? FindAnyObjectByType<NpcSpawner>(FindObjectsInactive.Include);
            spawner?.HireNpcFromPort(def);

            SaveService.Instance?.SaveGame();
            Debug.Log($"[PlazaPanel] 새 선장: {ch.displayNameKo} ({price}G) — 풀에서 영구 제외");
            Refresh();
        }

        // ─── UI 헬퍼 ────────────────────────────────────────────────────────

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

        [ContextMenu("Auto Layout")]
        private void AutoLayout()
        {
            UIScrollPanelLayout.ApplyFullscreenWithScrollList(
                panelRoot != null ? panelRoot : gameObject,
                titleText, new TMP_Text[] { moneyText, currentCaptainText },
                ref rowsContainer, closeButton);
            Debug.Log("[PlazaPanel] Auto Layout 적용 완료.");
        }
    }
}
