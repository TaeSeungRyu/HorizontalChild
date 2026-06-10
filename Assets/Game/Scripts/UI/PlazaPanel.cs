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
    /// 광장 (Plaza) 패널 — 항구 dwell NPC 를 선원으로 고용 (최대 10명).
    ///
    /// 새 모델:
    ///   - NPC 는 선장 교체가 아니라 "선원 아이템".
    ///   - 고용 시 NpcDefinition.hireBonus 가 PlayerCrew 에 합산되어 능력치 증감.
    ///   - 제약: 선원 < 10 + 좋은/나쁜 명성 게이트 + hireBasePrice 비용.
    ///
    /// 각 행: 이름 / 본인 stats / 보너스(±) / 비용 / 명성 요구 / [고용] 버튼.
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

            // 선장 + 현재 선원 수 표시
            if (currentCaptainText != null)
            {
                var crew = PlayerCrew.Instance;
                int n = crew != null ? crew.Count : 0;
                int max = crew != null ? crew.maxCrew : 10;
                string captainName = playerShip != null && playerShip.captain != null
                    ? playerShip.captain.displayNameKo : "—";
                currentCaptainText.text = $"선장: <b>{captainName}</b>  ·  선원 {n}/{max}";
            }

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
            le.preferredHeight = 90f;

            int price = HirePrice(def);
            var crew = PlayerCrew.Instance;
            var ps = _state;

            bool alreadyHired = crew != null && crew.Contains(def);
            bool crewFull = crew != null && crew.IsFull;
            bool repOk = ps == null ||
                (ps.GoodReputation >= def.requiredGoodReputation
                 && ps.BadReputation >= def.requiredBadReputation);
            bool canAfford = ps != null && ps.Money >= price;
            bool canHire = !alreadyHired && !crewFull && repOk && canAfford;

            // 이름
            AddText(row.transform, ch.displayNameKo, 200f, TextAlignmentOptions.Left);

            // 본인 능력치
            AddText(row.transform,
                $"용 {ch.bravery} / 항 {ch.seamanship} / 눈 {ch.keenEye}",
                240f, TextAlignmentOptions.Left, 20f);

            // 보너스 (hireBonus)
            AddText(row.transform, FormatBonus(def.hireBonus),
                200f, TextAlignmentOptions.Left, 20f);

            // 명성 요구
            AddText(row.transform, FormatGate(def),
                160f, TextAlignmentOptions.Left, 18f);

            // 가격
            AddText(row.transform, $"{price:N0} G", 120f, TextAlignmentOptions.Right);

            // 버튼 라벨
            string label = alreadyHired ? "고용됨"
                         : crewFull ? "선원 가득"
                         : !repOk ? "명성 부족"
                         : !canAfford ? "돈 부족"
                         : "고용";
            var hireBtn = AddButton(row.transform, label, 130f);
            hireBtn.interactable = canHire;
            hireBtn.onClick.AddListener(() => OnHire(def));

            return row;
        }

        private static string FormatBonus(Vector3Int b)
        {
            string Sign(int n) => n > 0 ? $"+{n}" : n.ToString();
            return $"<color=#9CDCFE>{Sign(b.x)}/{Sign(b.y)}/{Sign(b.z)}</color>";
        }

        private static string FormatGate(NpcDefinition def)
        {
            if (def.requiredGoodReputation <= 0 && def.requiredBadReputation <= 0) return "—";
            string g = def.requiredGoodReputation > 0 ? $"좋은 ≥{def.requiredGoodReputation}" : "";
            string b = def.requiredBadReputation > 0 ? $"나쁜 ≥{def.requiredBadReputation}" : "";
            return string.IsNullOrEmpty(g) ? b : (string.IsNullOrEmpty(b) ? g : $"{g} · {b}");
        }

        private int HirePrice(NpcDefinition def)
        {
            if (def.hireBasePrice > 0) return def.hireBasePrice;
            var ch = def.character;
            return ch != null ? (ch.bravery + ch.seamanship + ch.keenEye) * 10 : 1500;
        }

        private void OnHire(NpcDefinition def)
        {
            if (def == null || _state == null) return;
            var crew = PlayerCrew.Instance;
            if (crew == null)
            {
                Debug.LogError("[PlazaPanel] PlayerCrew 인스턴스 없음 — 씬에 PlayerCrew 컴포넌트 추가 필요.");
                return;
            }

            if (crew.Contains(def)) return;
            if (crew.IsFull) { Debug.Log("[PlazaPanel] 선원 가득 (10명 한도)."); return; }
            if (_state.GoodReputation < def.requiredGoodReputation || _state.BadReputation < def.requiredBadReputation)
            {
                Debug.Log("[PlazaPanel] 명성 부족."); return;
            }

            int price = HirePrice(def);
            if (!_state.TrySpend(price))
            {
                Debug.Log($"[PlazaPanel] 돈 부족: 필요 {price}");
                return;
            }

            crew.TryHire(def);

            // 풀 영구 제외 (§3.5)
            var spawner = NpcSpawner.Instance
                ?? FindAnyObjectByType<NpcSpawner>(FindObjectsInactive.Include);
            spawner?.HireNpcFromPort(def);

            SaveService.Instance?.SaveGame();
            Debug.Log($"[PlazaPanel] 선원 고용: {def.character.displayNameKo} ({price}G), " +
                $"보너스 ({def.hireBonus.x},{def.hireBonus.y},{def.hireBonus.z}) — 풀에서 영구 제외");
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
