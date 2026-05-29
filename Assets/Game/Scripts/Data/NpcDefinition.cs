using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "Npc_", menuName = "Game/Data/NPC Definition")]
    public class NpcDefinition : ScriptableObject
    {
        [Header("Character")]
        public CharacterData character;
        public NpcType type;

        [Header("Behavior")]
        [Tooltip("거점 항구. 해적의 경우 활동 반경 기준")]
        public PortData homePort;

        [Header("Hire Bonuses")]
        [Tooltip("x = 용기, y = 항해, z = 눈썰미. 음수 가능. GAME_MECHANICS §1.4")]
        public Vector3Int hireBonus;
        public int hireBasePrice = 1000;
    }
}
