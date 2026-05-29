using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "Port_", menuName = "Game/Data/Port Data")]
    public class PortData : ScriptableObject
    {
        [Header("Identity")]
        public string portId;
        public string displayNameKo;
        public string displayNameOriginal;

        [Header("Geography")]
        [Tooltip("null = 중립 항구 (GAME_MECHANICS §8.16 옵션 A)")]
        public NationData nation;
        [Range(-90f, 90f)] public float latitude;
        [Range(-180f, 180f)] public float longitude;

        [Header("Texts (어린이용)")]
        [TextArea(1, 2)] public string shortDescription;

        [Header("Visuals")]
        public Sprite cityIllustration;

        [Header("Market — 일반 특산물 (시장에 항상 노출)")]
        public ProductData[] commonProducts;

        [Header("Market — 스페셜 특산물 (미션 전용)")]
        public ProductData[] specialProducts;

        [Header("Author Notes")]
        public string sourceUrl;
    }
}
