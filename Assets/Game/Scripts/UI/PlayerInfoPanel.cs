using System.Text;
using Game.Missions;
using Game.Player;
using Game.Ship;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 내 정보 패널 — 선장 능력치(기본 + 선원 보너스 + 합계) + 배 + 명성 + 현재 의뢰.
    ///
    /// PlayerInfoButton 클릭으로 열림. 단일 TMP_Text 에 포맷된 정보 표시.
    /// </summary>
    public class PlayerInfoPanel : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Header("Refs — Auto Layout 가능")]
        public TMP_Text titleText;
        public TMP_Text contentText;
        public Button closeButton;

        [Header("Game Refs")]
        public ShipController playerShip;

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
            panelRoot.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Refresh()
        {
            if (titleText != null) titleText.text = "내 정보";
            if (contentText != null) contentText.text = BuildContent();
        }

        private string BuildContent()
        {
            var sb = new StringBuilder();

            // ─── 선장 + 배 + 잔돈 + 명성 ───
            var captain = playerShip != null ? playerShip.captain : null;
            string captainName = captain != null ? captain.displayNameKo : "선장";
            sb.Append("<b>선장</b>: ").AppendLine(captainName);

            if (playerShip != null && playerShip.shipData != null)
            {
                sb.Append("<b>배</b>: ").Append(playerShip.shipData.displayName)
                  .Append("  ·  내구 ").Append(playerShip.CurrentDurability)
                  .Append("/").Append(playerShip.MaxDurability)
                  .AppendLine();
            }
            else sb.AppendLine("<b>배</b>: —");

            var ps = PlayerState.Instance;
            if (ps != null)
            {
                sb.Append("<b>잔돈</b>: <color=#FFD86B>").Append(ps.Money.ToString("N0")).AppendLine(" G</color>");
                sb.Append("<b>명성</b>: 좋은 ").Append(ps.GoodReputation)
                  .Append("  ·  나쁜 ").Append(ps.BadReputation).AppendLine();
            }

            sb.AppendLine();

            // ─── 능력치 표 ───
            int baseB = captain != null ? captain.bravery : 0;
            int baseS = captain != null ? captain.seamanship : 0;
            int baseK = captain != null ? captain.keenEye : 0;
            var crew = PlayerCrew.Instance;
            int bonusB = crew != null ? crew.BraveryBonus : 0;
            int bonusS = crew != null ? crew.SeamanshipBonus : 0;
            int bonusK = crew != null ? crew.KeenEyeBonus : 0;

            sb.AppendLine("<b>── 능력치 ──</b>");
            sb.AppendLine("<mspace=4em>          기본    보너스    합계</mspace>");
            sb.AppendLine(StatRow("용기  ", baseB, bonusB));
            sb.AppendLine(StatRow("항해  ", baseS, bonusS));
            sb.AppendLine(StatRow("눈썰미", baseK, bonusK));

            sb.AppendLine();

            // ─── 선원 요약 ───
            int crewCount = crew != null ? crew.Count : 0;
            int maxCrew = crew != null ? crew.maxCrew : 10;
            sb.Append("<b>── 선원 ").Append(crewCount).Append("/").Append(maxCrew).AppendLine(" ──</b>");
            if (crew != null && crew.Count > 0)
            {
                int shown = 0;
                foreach (var n in crew.Crew)
                {
                    if (n == null || n.character == null) continue;
                    if (shown > 0) sb.Append(" · ");
                    sb.Append(n.character.displayNameKo);
                    shown++;
                }
                sb.AppendLine();
            }
            else sb.AppendLine("<color=#999999>(고용한 선원 없음)</color>");

            sb.AppendLine();

            // ─── 현재 의뢰 ───
            sb.AppendLine("<b>── 현재 의뢰 ──</b>");
            sb.Append(BuildMissionStatus());

            return sb.ToString();
        }

        private static string StatRow(string label, int baseVal, int bonusVal)
        {
            int total = baseVal + bonusVal;
            string bonusStr = bonusVal > 0 ? $"<color=#9CDCFE>+{bonusVal}</color>"
                            : bonusVal < 0 ? $"<color=#F47C7C>{bonusVal}</color>"
                            : "0";
            return $"<mspace=4em>{label}    {baseVal,3}    {bonusStr,8}    <b>{total,3}</b></mspace>";
        }

        private string BuildMissionStatus()
        {
            var ms = MissionService.Instance;
            if (ms == null || !ms.HasActiveMission) return "<color=#999999>진행 중인 의뢰가 없어요.</color>";

            var m = ms.CurrentMission;
            var sb = new StringBuilder();
            sb.Append("<b>").Append(m.title).AppendLine("</b>");

            // 발견 단계 — DiscoveredIds 에 targetDiscovery 가 있는지
            bool found = m.targetDiscovery != null
                && ms.DiscoveredIds.Contains(m.targetDiscovery.discoveryId);

            if (found)
            {
                string portName = m.issuerPort != null ? m.issuerPort.displayNameKo : "의뢰 항구";
                sb.Append("<color=#7CCD7C>발견 완료! ").Append(portName).Append(" 으로 돌아가 보고하세요.</color>");
            }
            else if (m.targetDiscovery != null)
            {
                sb.Append("‘").Append(m.targetDiscovery.displayNameKo).Append("’ 을(를) 찾아보세요.");
            }
            return sb.ToString();
        }

        // ─── Auto Layout ──────────────────────────────────────────────────

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
            bg.color = new Color(0.07f, 0.1f, 0.15f, 0.95f);
            bg.raycastTarget = true;

            if (titleText != null)
                LayoutText(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f),
                    new Vector2(1000f, 100f), 56f, TextAlignmentOptions.Center, Color.white);

            if (contentText != null)
                LayoutText(contentText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f),
                    new Vector2(1200f, 700f), 28f, TextAlignmentOptions.TopLeft,
                    new Color(0.9f, 0.9f, 0.9f));

            if (closeButton != null)
                LayoutRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                    new Vector2(0f, 100f), new Vector2(260f, 80f));

            Debug.Log("[PlayerInfoPanel] Auto Layout 완료.");
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
                tmp.richText = true;
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
