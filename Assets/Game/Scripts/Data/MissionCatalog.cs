using UnityEngine;

namespace Game.Data
{
    /// <summary>모든 MissionTemplate 의 카탈로그. CatalogRefresher 가 자동 채움.</summary>
    [CreateAssetMenu(fileName = "MissionCatalog", menuName = "Game/Data/Mission Catalog")]
    public class MissionCatalog : ScriptableObject
    {
        public MissionTemplate[] all;
    }
}
