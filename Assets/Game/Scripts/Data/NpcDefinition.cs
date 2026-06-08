using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "Npc_", menuName = "Game/Data/NPC Definition")]
    public class NpcDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Save/Load 시 식별자. 시더에서 자동 설정.")]
        public string npcId;

        [Header("Character")]
        public CharacterData character;
        public NpcType type;

        [Header("Behavior")]
        [Tooltip("거점 항구. 해적·호위선의 활동 중심점.")]
        public PortData homePort;

        [Header("Patrol — 해적·호위선용")]
        [Tooltip("homePort 에서 이만큼 떨어지면 복귀. 0 이면 무한 wander.")]
        public float patrolRange = 200f;

        [Header("Trade Route — 상선용")]
        [Tooltip("두 항구 이상 등록 시 순차 왕복. 1개 이하면 일반 wander.")]
        public PortData[] patrolPorts;

        [Header("Hire Bonuses")]
        [Tooltip("x = 용기, y = 항해, z = 눈썰미. 음수 가능. GAME_MECHANICS §1.4")]
        public Vector3Int hireBonus;
        public int hireBasePrice = 1000;
    }
}
