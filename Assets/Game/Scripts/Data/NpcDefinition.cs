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
        [Tooltip("[Deprecated 다중 항로] 두 항구 이상 등록 시 순차 왕복. 신규는 destinationPort 사용.")]
        public PortData[] patrolPorts;

        [Tooltip("상선의 무역 목적지 항구. home ↔ destination 왕복.")]
        public PortData destinationPort;

        [Header("Hire Bonuses")]
        [Tooltip("x = 용기, y = 항해, z = 눈썰미. 음수 가능. 고용 시 플레이어 능력치에 합산.")]
        public Vector3Int hireBonus;
        public int hireBasePrice = 1000;

        [Header("Hire Gate")]
        [Tooltip("고용에 필요한 최소 좋은 명성.")]
        public int requiredGoodReputation = 0;
        [Tooltip("고용에 필요한 최소 나쁜 명성 (해적 영입 등).")]
        public int requiredBadReputation = 0;

        [Header("Combat Stats (M3.5 Phase 2)")]
        [Range(1, 30)] public int cannonPower = 3;
        [Range(10, 200)] public int maxDurability = 40;
        [Tooltip("포탄 발사 간격(초). 작을수록 빠른 연사.")]
        [Range(0.3f, 4f)] public float attackInterval = 1.6f;

        [Header("Visual")]
        [Tooltip("이 NPC 가 타는 배. ShipData.prefab3D 가 있으면 그 모델로, 없으면 절차적 배 모양.")]
        public ShipData shipData;
    }
}
