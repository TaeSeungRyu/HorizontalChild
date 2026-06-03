using UnityEngine;

namespace Game.Data
{
    /// <summary>모든 PortData 의 카탈로그. CatalogRefresher 가 자동 채움.</summary>
    [CreateAssetMenu(fileName = "PortCatalog", menuName = "Game/Data/Port Catalog")]
    public class PortCatalog : ScriptableObject
    {
        public PortData[] all;
    }
}
