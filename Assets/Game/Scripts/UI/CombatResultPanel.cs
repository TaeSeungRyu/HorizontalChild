using Game.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 전투 결과 표시 패널.
    /// CombatResult 받아서 헤더("승리!" / "패배...") + 양측 이름·전력 + 보상 / 패널티 메시지.
    /// OK 버튼으로 닫음.
    ///
    /// 시각 셋업: 기본 컴포넌트만 부착 후 인스펙터 ⋮ → Auto Layout (TODO).
    /// 일단 수동 셋업 또는 다른 패널 (DiscoveryFoundPanel 등) 처럼 자식 TMP·Button 직접 만들어 필드 연결.
    /// </summary>
    public class CombatResultPanel : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Header("Refs")]
        public TMP_Text headerText;
        public TMP_Text playerInfoText;
        public TMP_Text npcInfoText;
        public TMP_Text rewardText;
        public TMP_Text messageText;
        public Button okButton;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);
            if (okButton != null)
            {
                okButton.onClick.RemoveListener(Close);
                okButton.onClick.AddListener(Close);
            }
        }

        public void Show(CombatResult result)
        {
            if (panelRoot == null) panelRoot = gameObject;

            if (headerText != null)
            {
                headerText.text = result.playerWon ? "승리!" : "패배...";
                headerText.color = result.playerWon
                    ? new Color(0.95f, 0.85f, 0.30f)  // 노랑
                    : new Color(0.85f, 0.30f, 0.30f); // 빨강
            }

            if (playerInfoText != null)
                playerInfoText.text = $"<b>{result.playerName}</b>\n전력 {result.playerPower}";

            if (npcInfoText != null)
                npcInfoText.text = $"<b>{result.npcName}</b>\n전력 {result.npcPower}";

            if (rewardText != null)
            {
                var sb = new System.Text.StringBuilder();
                if (result.moneyDelta != 0)
                    sb.AppendLine($"잔돈 {result.moneyDelta:+#,##0;-#,##0;0} G");
                if (result.repGoodDelta > 0)
                    sb.AppendLine($"좋은 명성 +{result.repGoodDelta}");
                if (result.repBadDelta > 0)
                    sb.AppendLine($"나쁜 명성 +{result.repBadDelta}");
                rewardText.text = sb.ToString().TrimEnd();
            }

            if (messageText != null)
                messageText.text = result.message;

            // 다른 패널(도감 등) 위로 — 최상단 sibling
            panelRoot.transform.SetAsLastSibling();
            panelRoot.SetActive(true);

            // OK 리스너 안전망 재등록
            if (okButton != null)
            {
                okButton.onClick.RemoveListener(Close);
                okButton.onClick.AddListener(Close);
            }
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ─── 자동 레이아웃 — JournalPanel / MarketPanel 패턴 ────────────────

        [ContextMenu("Auto Layout")]
        private void AutoLayout()
        {
            if (panelRoot == null) panelRoot = gameObject;
            var panelRT = panelRoot.GetComponent<RectTransform>();
            if (panelRT == null) return;

            // 1) 패널 — 화면 중앙 1200x700 (전투 결과는 풀스크린보다 모달이 자연스러움)
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(1200f, 700f);
            panelRT.anchoredPosition = Vector2.zero;

            // 불투명 배경
            var bgImg = panelRoot.GetComponent<Image>();
            if (bgImg == null) bgImg = panelRoot.AddComponent<Image>();
            bgImg.color = new Color(0.10f, 0.13f, 0.18f, 1f);
            bgImg.raycastTarget = true;

            // 2) Header — 상단 중앙
            if (headerText != null)
            {
                SetRect(headerText.rectTransform,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -40f), new Vector2(1120f, 100f));
                headerText.alignment = TextAlignmentOptions.Center;
                headerText.fontSize = 64f;
            }

            // 3) PlayerInfo — 중앙 좌측
            if (playerInfoText != null)
            {
                SetRect(playerInfoText.rectTransform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-300f, 100f), new Vector2(440f, 140f));
                playerInfoText.alignment = TextAlignmentOptions.Center;
                playerInfoText.fontSize = 32f;
            }

            // 4) NpcInfo — 중앙 우측
            if (npcInfoText != null)
            {
                SetRect(npcInfoText.rectTransform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(300f, 100f), new Vector2(440f, 140f));
                npcInfoText.alignment = TextAlignmentOptions.Center;
                npcInfoText.fontSize = 32f;
            }

            // 5) Reward — Player/Npc 아래
            if (rewardText != null)
            {
                SetRect(rewardText.rectTransform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -50f), new Vector2(1000f, 120f));
                rewardText.alignment = TextAlignmentOptions.Center;
                rewardText.fontSize = 28f;
            }

            // 6) Message — Reward 아래
            if (messageText != null)
            {
                SetRect(messageText.rectTransform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -180f), new Vector2(1000f, 80f));
                messageText.alignment = TextAlignmentOptions.Center;
                messageText.fontSize = 26f;
                messageText.enableWordWrapping = true;
            }

            // 7) OK Button — 하단 중앙, 최상위
            if (okButton != null)
            {
                SetRect(okButton.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 40f), new Vector2(240f, 80f));
                okButton.transform.SetAsLastSibling();
            }

            Debug.Log("[CombatResultPanel] Auto Layout 적용 완료.");
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
    }
}
