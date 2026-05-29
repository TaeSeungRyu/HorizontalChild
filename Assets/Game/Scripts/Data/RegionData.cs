using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "Region_", menuName = "Game/Data/Region Data")]
    public class RegionData : ScriptableObject
    {
        [Header("Identity")]
        public string regionId;
        public string displayNameKo;

        [Header("Contents")]
        public PortData[] ports;
        public DiscoveryData[] discoveries;

        [Header("Unlock")]
        [Tooltip("국적 선택 시 자동 해제되는 국가들 (CONTENT_DESIGN.md §2.5)")]
        public NationData[] unlockedAtStartFor;
        [TextArea(1, 3)] public string unlockHint;
    }
}
