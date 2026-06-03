using UnityEngine;

namespace Game.Data
{
    /// <summary>모든 LandmassData 의 카탈로그. CatalogRefresher 가 자동 채움.</summary>
    [CreateAssetMenu(fileName = "LandmassCatalog", menuName = "Game/Data/Landmass Catalog")]
    public class LandmassCatalog : ScriptableObject
    {
        public LandmassData[] all;
    }
}
