namespace Game.Data
{
    public enum Gender { Male, Female }

    public enum NpcType { Merchant, Escort, Pirate }

    public enum NpcState { AtPort, AtSea, Hired, Defeated }

    // §8.17 옵션 A: Adventurer = 바다 가능, Townsperson = 항구 전용
    public enum CharacterRole { Adventurer, Townsperson }

    // 2026-05-31 단순화: 교역 의뢰 제거. 모든 의뢰는 발견물 의뢰로 통일 → MissionType enum 자체 제거.

    public enum DiscoveryCategory { Landmark, FloraFauna, Ruin, Event }
}
