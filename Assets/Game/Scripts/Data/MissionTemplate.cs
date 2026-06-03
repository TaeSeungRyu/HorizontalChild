using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 발견물 의뢰 템플릿. 2026-05-31 단순화: 교역 의뢰 제거 → 모든 의뢰는 발견물 의뢰.
    /// </summary>
    [CreateAssetMenu(fileName = "Mission_", menuName = "Game/Data/Mission Template")]
    public class MissionTemplate : ScriptableObject
    {
        [Header("Identity")]
        public string missionId;

        [Header("Issuer & Target")]
        [Tooltip("의뢰 발급 항구. 발견물 가지고 이 항구로 돌아오면 완료.")]
        public PortData issuerPort;
        [Tooltip("찾아야 하는 발견물.")]
        public DiscoveryData targetDiscovery;

        [Header("Rewards")]
        public int rewardMoney = 500;
        [Range(0, 50000)] public int rewardGoodReputation = 50;

        [Header("UI Texts (어린이용)")]
        public string title;
        [TextArea(2, 4)] public string description;
        [Tooltip("지도 아이템 이름 (예: '지브롤터로 가는 지도')")]
        public string mapItemName;
    }
}
