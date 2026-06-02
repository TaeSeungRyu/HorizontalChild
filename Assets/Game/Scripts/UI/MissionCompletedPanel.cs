using Game.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 의뢰 완료 알림 패널.
    /// PortScreen 위에 띄워 보상(돈·명성)을 표시.
    ///
    /// 어린이 친화 톤:
    ///   - "수고했어요!" 같은 축하 메시지
    ///   - 돈은 "+1,000원", 명성은 "+100" 식으로 명료하게
    ///   - 확인 버튼 한 개로 단순
    /// </summary>
    public class MissionCompletedPanel : MonoBehaviour
    {
        [Header("Refs")]
        public GameObject panelRoot;

        public TMP_Text headerText;            // "수고했어요!"
        public TMP_Text missionTitleText;      // 의뢰 제목 반복
        public TMP_Text rewardText;            // 보상 내역
        public Button okButton;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);

            if (okButton != null) okButton.onClick.AddListener(Close);
        }

        /// <summary>의뢰 완료 시 호출.</summary>
        public void Show(MissionTemplate completed)
        {
            if (completed == null) return;
            if (panelRoot == null) panelRoot = gameObject; // 비활성 GameObject 호출 안전장치

            if (headerText != null)
            {
                headerText.text = "수고했어요!";
            }

            if (missionTitleText != null)
            {
                missionTitleText.text = $"\"{completed.title}\" 의뢰를 마쳤어요.";
            }

            if (rewardText != null)
            {
                rewardText.text = BuildRewardText(completed);
            }

            panelRoot.SetActive(true);
        }

        public void Close()
        {
            panelRoot.SetActive(false);
        }

        private static string BuildRewardText(MissionTemplate mission)
        {
            string moneyLine = mission.rewardMoney > 0
                ? $"돈 +{mission.rewardMoney:N0}원"
                : "";

            string repLine = mission.rewardGoodReputation > 0
                ? $"좋은 평판 +{mission.rewardGoodReputation:N0}"
                : "";

            if (!string.IsNullOrEmpty(moneyLine) && !string.IsNullOrEmpty(repLine))
            {
                return $"{moneyLine}\n{repLine}";
            }
            return moneyLine + repLine;
        }
    }
}
