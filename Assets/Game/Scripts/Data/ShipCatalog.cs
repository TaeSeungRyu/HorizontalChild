using UnityEngine;

namespace Game.Data
{
    /// <summary>모든 ShipData 를 모아 조선소 UI 가 풀에서 선택할 수 있게.</summary>
    [CreateAssetMenu(fileName = "ShipCatalog", menuName = "Game/Data/Ship Catalog")]
    public class ShipCatalog : ScriptableObject
    {
        public ShipData[] all;
    }
}
