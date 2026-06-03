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
        public int startingYear = 1415;

        [Header("Texts (어린이용)")]
        [TextArea(1, 2)] public string shortIntro;
        [TextArea(2, 4)] public string greeting;

        [Header("Author Notes")]
        [TextArea(2, 5)] public string designerNotes;
        public string sourceUrl;
    }
}
