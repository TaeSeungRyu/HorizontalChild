using System;

namespace Game.Data
{
    [Serializable]
    public struct ReputationGate
    {
        public int requiredGoodReputation;
        public int requiredBadReputation;
    }
}
