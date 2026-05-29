using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "Mission_", menuName = "Game/Data/Mission Template")]
    public class MissionTemplate : ScriptableObject
    {
        [Header("Identity")]
        public string missionId;
        public MissionType type;

        [Header("Issuer")]
        public PortData issuerPort;

        [Header("Target — Discovery 의뢰는 targetDiscovery만, Trade 의뢰는 targetProduct + targetPort만 채움")]
        public DiscoveryData targetDiscovery;
        public ProductData targetProduct;
        public PortData targetPort;
        [Min(1)] public int targetProductQuantity = 5;

        [Header("Rewards")]
        public int rewardMoney = 500;
        [Range(0, 50000)] public int rewardGoodReputation = 50;

        [Header("UI Texts (어린이용)")]
        public string title;
        [TextArea(2, 4)] public string description;
        [Tooltip("Discovery 의뢰 전용 — 지도 아이템 이름")]
        public string mapItemName;
    }
}
