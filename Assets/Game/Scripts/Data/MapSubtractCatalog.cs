using UnityEngine;

namespace Game.Data
{
    /// <summary>모든 MapSubtractData 의 카탈로그. CatalogRefresher 가 자동 채움.</summary>
    [CreateAssetMenu(fileName = "MapSubtractCatalog", menuName = "Game/Data/Map Subtract Catalog")]
    public class MapSubtractCatalog : ScriptableObject
    {
        public MapSubtractData[] all;
    }
}
