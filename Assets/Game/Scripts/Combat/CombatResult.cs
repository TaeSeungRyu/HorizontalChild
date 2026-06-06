namespace Game.Combat
{
    /// <summary>전투 결과 — UI 와 서비스 간 전달용 POCO.</summary>
    public struct CombatResult
    {
        public string playerName;
        public string npcName;
        public int playerPower;
        public int npcPower;
        public bool playerWon;
        public int moneyDelta;        // + 획득, − 손실
        public int repGoodDelta;
        public int repBadDelta;
        public string message;        // 어린이 친화 한 줄 설명
    }
}
