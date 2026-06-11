using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "Nation_", menuName = "Game/Data/Nation Data")]
    public class NationData : ScriptableObject
    {
        [Header("Identity")]
        public string nationId;
        public string displayNameKo;
        public string displayNameOriginal;
        public Sprite flagIcon;
        public Color accentColor = Color.white;

        [Header("Start State")]
        public PortData startingPort;
        [Tooltip("이 국가로 시작하면 PlayerShip.captain 에 자동 할당될 캐릭터.")]
        public CharacterData startingCharacter;
        [Tooltip("이 국가로 시작할 때 자동 할당될 배. 비우면 ShipCatalog 첫 번째 배로 fallback.")]
        public ShipData startingShip;
        public int startingYear = 1415;

        [Header("Start Spawn — 항구 좌표 + offset")]
        [Tooltip("배가 항구 좌표보다 바다 쪽으로 떨어진 위도 오프셋 (도 단위). 양수=북쪽.")]
        [Range(-5f, 5f)] public float startingSeaOffsetLatitude = 0f;
        [Tooltip("바다 쪽 경도 오프셋. 양수=동쪽.")]
        [Range(-5f, 5f)] public float startingSeaOffsetLongitude = 0f;

        [Header("Texts (어린이용)")]
        [TextArea(1, 2)] public string shortIntro;
        [TextArea(2, 4)] public string greeting;

        [Header("Author Notes")]
        [TextArea(2, 5)] public string designerNotes;
        public string sourceUrl;
    }
}
