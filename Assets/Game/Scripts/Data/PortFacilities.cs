using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "PortFacilities_", menuName = "Game/Data/Port Facilities")]
    public class PortFacilities : ScriptableObject
    {
        [Header("Port")]
        public PortData port;

        [Header("Facility Availability")]
        public bool hasMarket = true;
        public bool hasGuild = true;
        public bool hasShipyard = true;
        public bool hasPlaza = true;
        public bool hasHarbor = true;

        [Header("Shipyard Catalog")]
        [Tooltip("이 항구의 조선소에서 판매하는 배 목록")]
        public ShipData[] shipyardCatalog;

        [Header("Guild Missions")]
        [Tooltip("이 항구의 모험가 조합이 발급하는 의뢰")]
        public MissionTemplate[] guildMissions;
    }
}
