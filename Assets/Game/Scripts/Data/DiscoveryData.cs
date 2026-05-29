using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "Discovery_", menuName = "Game/Data/Discovery Data")]
    public class DiscoveryData : ScriptableObject
    {
        [Header("Identity")]
        public string discoveryId;
        public string displayNameKo;
        public DiscoveryCategory category;

        [Header("Geography")]
        [Range(-90f, 90f)] public float latitude;
        [Range(-180f, 180f)] public float longitude;
        [Tooltip("발견 허용 오차 비율. 기본 0.03 (±3%). 눈썰미로 확대됨 — GAME_MECHANICS §1.1·§5.2")]
        public float searchToleranceBase = 0.03f;

        [Header("Texts (어린이용)")]
        [TextArea(2, 4)] public string mainDescription;
        [TextArea(1, 2)] public string moreInfo;

        [Header("Era & Affiliation")]
        public string eraLabel;
        [Tooltip("null = 특정 국가와 무관")]
        public NationData relatedNation;

        [Header("Visuals")]
        public Sprite illustration;

        [Header("Author Notes")]
        [TextArea(1, 3)] public string relatedFigures;
        public string sourceUrl;
        public bool sensitiveExpressionChecked = false;
    }
}
