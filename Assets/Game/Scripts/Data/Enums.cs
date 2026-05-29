namespace Game.Data
{
    public enum Gender { Male, Female }

    public enum NpcType { Merchant, Escort, Pirate }

    public enum NpcState { AtPort, AtSea, Hired, Defeated }

    // §8.17 옵션 A: Adventurer = 바다 가능, Townsperson = 항구 전용
    public enum CharacterRole { Adventurer, Townsperson }

    public enum MissionType { Discovery, TradeBuy, TradeDeliver }

    public enum DiscoveryCategory { Landmark, FloraFauna, Ruin, Event }
}
