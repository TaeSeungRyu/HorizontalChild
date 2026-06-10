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
    /// 선원 명부 패널 — 현재 고용한 선원 목록 + 해고 기능.
    ///
    /// 표시: 선원 N/10, 총 보너스 합, 각 선원 행 (이름·능력·보너스·[해고]).
    /// 해고 시 PlayerCrew 에서 제거 + 저장. 한 번 해고하면 다시 데려올 수 없음.
    /// </summary>
    public class CrewPanel : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Header("Refs — Auto Layout 가능")]
        public TMP_Text titleText;
        public TMP_Text summaryText;
        public RectTransform rowsContainer;
        public Button closeButton;

        [Header("Game Refs")]
        public ShipController playerShip;

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

        public void Open()
        {
            if (panelRoot == null) panelRoot = gameObject;
            if (playerShip == null) playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);
            Refresh();
            panelRoot.SetActive(true);
            if (closeButton != null) closeButton.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Refresh()
        {
            var crew = PlayerCrew.Instance;
            int n = crew != null ? crew.Count : 0;
            int max = crew != null ? crew.maxCrew : 10;

            if (titleText != null) titleText.text = $"선원 명부 ({n}/{max})";

            if (summaryText != null)
            {
                if (crew != null)
                {
                    var bonus = crew.TotalHireBonus();
                    summaryText.text =
                        $"<b>능력치 합계 보너스</b>\n" +
                        $"용기 {Sign(bonus.x)} · 항해 {Sign(bonus.y)} · 눈썰미 {Sign(bonus.z)}";
                }
                else summaryText.text = "선원 정보 없음";
            }

            BuildRows();
        }

        private static string Sign(int n) => n > 0 ? $"+{n}" : n.ToString();

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
            var crew = PlayerCrew.Instance;
            if (crew == null || crew.Count == 0)
            {
                _spawnedRows.Add(BuildEmptyRow());
                return;
            }

            foreach (var npc in crew.Crew)
            {
                if (npc == null || npc.character == null) continue;
                _spawnedRows.Add(BuildCrewRow(npc));
            }
        }

        private GameObject BuildEmptyRow()
        {
            var row = new GameObject("Row_Empty", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(rowsContainer, false);
            row.GetComponent<LayoutElement>().preferredHeight = 80f;
            var tmp = row.AddComponent<TextMeshProUGUI>();
            tmp.text = "고용한 선원이 없어요. 항구 광장에서 선원을 모집해보세요.";
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.8f, 0.8f, 0.8f);
            tmp.enableWordWrapping = true;
            return row;
        }

        private GameObject BuildCrewRow(NpcDefinition def)
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

            // 이름
            AddText(row.transform, ch.displayNameKo, 220f, TextAlignmentOptions.Left);

            // 능력치
            AddText(row.transform,
                $"용 {ch.bravery} / 항 {ch.seamanship} / 눈 {ch.keenEye}",
                240f, TextAlignmentOptions.Left, 20f);

            // 보너스
            AddText(row.transform,
                $"<color=#9CDCFE>{Sign(def.hireBonus.x)}/{Sign(def.hireBonus.y)}/{Sign(def.hireBonus.z)}</color>",
                160f, TextAlignmentOptions.Left, 20f);

            // 해고 버튼
            var dismissBtn = AddButton(row.transform, "해고", 120f);
            dismissBtn.onClick.AddListener(() => OnDismiss(def));

            return row;
        }

        private void OnDismiss(NpcDefinition def)
        {
            if (def == null) return;
            var crew = PlayerCrew.Instance;
            if (crew == null) return;
            if (crew.Dismiss(def))
            {
                // 해고된 선원은 본거지 광장으로 복귀 — 다시 고용 가능
                var spawner = NpcSpawner.Instance
                    ?? FindAnyObjectByType<NpcSpawner>(FindObjectsInactive.Include);
                if (spawner != null && def.homePort != null)
                {
                    spawner.SendNpcToPort(def, 0f, def.homePort);
                }

                SaveService.Instance?.SaveGame();
                Debug.Log($"[CrewPanel] 해고: {def.character.displayNameKo} → {(def.homePort != null ? def.homePort.displayNameKo : "고향 없음")} 광장 복귀");
                Refresh();
            }
        }

        // ─── UI 헬퍼 (PlazaPanel 과 동일 패턴) ───────────────────────────────

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
            img.color = new Color(0.5f, 0.25f, 0.25f, 1f);
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
                titleText, new TMP_Text[] { summaryText },
                ref rowsContainer, closeButton);
            Debug.Log("[CrewPanel] Auto Layout 적용 완료.");
        }
    }
}
