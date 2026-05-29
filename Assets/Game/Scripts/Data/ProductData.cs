using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "Product_", menuName = "Game/Data/Product Data")]
    public class ProductData : ScriptableObject
    {
        [Header("Identity")]
        public string productId;
        public string displayNameKo;
        public Sprite icon;

        [Header("Economy")]
        public int basePrice = 10;
        [Tooltip("true이면 미션을 통해서만 구매 가능 (GAME_MECHANICS §4.1)")]
        public bool isSpecial = false;
        public PortData originPort;

        [Header("Texts (어린이용)")]
        [TextArea(1, 2)] public string shortDescription;

        [Header("Author Notes")]
        public string sourceUrl;
    }
}
