using System.Collections.Generic;

namespace Game.Market
{
    /// <summary>
    /// 한 항구의 시세 스냅샷. productId → 가격 배수 (0.8 ~ 1.2 권장).
    /// 한 번 생성되면 게임 세션 동안 유지 (재방문 시 같은 가격).
    /// Save 시스템 도입 후엔 직렬화 대상.
    /// </summary>
    public class MarketSnapshot
    {
        public string portId;
        public readonly Dictionary<string, float> priceMultipliers = new();

        public float GetMultiplier(string productId)
        {
            return priceMultipliers.TryGetValue(productId, out var m) ? m : 1f;
        }
    }
}
