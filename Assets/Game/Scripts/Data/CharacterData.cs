using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "Character_", menuName = "Game/Data/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        public string characterId;
        public string displayNameKo;
        public Gender gender;
        [Tooltip("Adventurer = 바다 가능, Townsperson = 항구 전용 (GAME_MECHANICS §8.17 옵션 A)")]
        public CharacterRole role = CharacterRole.Adventurer;

        [Header("Visuals")]
        public Sprite portrait;

        [Header("Stats (1~100)")]
        [Range(1, 100)] public int bravery = 50;
        [Range(1, 100)] public int seamanship = 50;
        [Range(1, 100)] public int keenEye = 50;

        [Header("Starting Reputation (저장 데이터 시작값)")]
        [Range(0, 50000)] public int startingGoodReputation = 0;
        [Range(0, 50000)] public int startingBadReputation = 0;

        [Header("Affiliation")]
        public NationData nation;
        public PortData homePort;

        [Header("Texts (어린이용)")]
        [TextArea(1, 2)] public string shortIntro;
        [TextArea(1, 3)] public string moreInfo;

        [Header("Author Notes")]
        public string sourceUrl;
    }
}
