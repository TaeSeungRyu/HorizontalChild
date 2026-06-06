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
    }
}
