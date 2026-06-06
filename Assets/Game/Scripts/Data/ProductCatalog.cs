using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 모든 ProductData 를 한 곳에 모아 ID → 인스턴스 lookup 에 사용.
    /// Save/Load 시 cargo 복원에 필수.
    /// CatalogRefresher 가 자동 채움.
    /// </summary>
    [CreateAssetMenu(fileName = "ProductCatalog", menuName = "Game/Data/Product Catalog")]
    public class ProductCatalog : ScriptableObject
    {
        public ProductData[] all;

        public ProductData FindById(string productId)
        {
            if (all == null || string.IsNullOrEmpty(productId)) return null;
            foreach (var p in all)
            {
                if (p != null && p.productId == productId) return p;
            }
            return null;
        }
    }
}
