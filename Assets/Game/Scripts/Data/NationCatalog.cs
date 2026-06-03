using UnityEngine;

namespace Game.Data
{
    /// <summary>모든 NationData 의 카탈로그. CatalogRefresher 가 자동 채움.</summary>
    [CreateAssetMenu(fileName = "NationCatalog", menuName = "Game/Data/Nation Catalog")]
    public class NationCatalog : ScriptableObject
    {
        public NationData[] all;
    }
}
