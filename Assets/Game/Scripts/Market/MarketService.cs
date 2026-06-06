using System.Collections.Generic;
using Game.Data;
using Game.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Market
{
    /// <summary>
    /// 시장 시스템 — 항구별 시세 관리 + 매매 로직.
    ///
    /// 싱글톤. GameManager 에 부착 권장.
    /// MarketPanel UI 가 본 서비스를 통해 가격 조회·거래 수행.
    ///
    /// 책임:
    ///   - 항구마다 MarketSnapshot 보관 (productId → 가격 배수)
    ///   - GetBuyPrice / GetSellPrice 로 가격 조회
    ///   - TryBuy / TrySell 로 거래 실행 (PlayerCargo + PlayerState.Money 갱신)
    /// </summary>
    public class MarketService : MonoBehaviour
    {
        public static MarketService Instance { get; private set; }

        [Header("Price Fluctuation")]
        [Tooltip("최소 가격 배수. basePrice * minMultiplier 가 가장 싼 가격.")]
        [Range(0.5f, 1f)] public float minMultiplier = 0.8f;

        [Tooltip("최대 가격 배수. basePrice * maxMultiplier 가 가장 비싼 가격.")]
        [Range(1f, 1.5f)] public float maxMultiplier = 1.2f;

        [Tooltip("판매가는 구매가에 이 비율을 곱함. 0.9 = 10% 손해.")]
        [Range(0.5f, 1f)] public float sellRatio = 0.9f;

        [Header("Events")]
        public UnityEvent<ProductData, int> onBought;  // (product, qty)
        public UnityEvent<ProductData, int> onSold;    // (product, qty)

        private readonly Dictionary<string, MarketSnapshot> _snapshots = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public MarketSnapshot GetSnapshot(PortData port)
        {
            if (port == null) return null;
            if (_snapshots.TryGetValue(port.portId, out var s)) return s;
            return CreateSnapshot(port);
        }

        private MarketSnapshot CreateSnapshot(PortData port)
        {
            var s = new MarketSnapshot { portId = port.portId };
            AddPriceFor(s, port.commonProducts);
            AddPriceFor(s, port.specialProducts);
            _snapshots[port.portId] = s;
            return s;
        }

        private void AddPriceFor(MarketSnapshot s, ProductData[] products)
        {
            if (products == null) return;
            foreach (var p in products)
            {
                if (p == null) continue;
                if (s.priceMultipliers.ContainsKey(p.productId)) continue;
                s.priceMultipliers[p.productId] = Random.Range(minMultiplier, maxMultiplier);
            }
        }

        /// <summary>해당 항구에서 product 의 현재 구매 가격 (정수).</summary>
        public int GetBuyPrice(PortData port, ProductData product)
        {
            if (port == null || product == null) return 0;
            var snap = GetSnapshot(port);
            float mult = snap.GetMultiplier(product.productId);
            return Mathf.Max(1, Mathf.RoundToInt(product.basePrice * mult));
        }

        /// <summary>해당 항구에서 product 의 현재 판매 가격 (정수).</summary>
        public int GetSellPrice(PortData port, ProductData product)
        {
            if (port == null || product == null) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(GetBuyPrice(port, product) * sellRatio));
        }

        /// <summary>구매 시도. 돈·화물용량 부족이면 false.</summary>
        public bool TryBuy(PortData port, ProductData product, int quantity)
        {
            if (port == null || product == null || quantity <= 0) return false;

            var cargo = PlayerCargo.Instance;
            var state = PlayerState.Instance;
            if (cargo == null || state == null) return false;

            int unitPrice = GetBuyPrice(port, product);
            int totalCost = unitPrice * quantity;

            if (state.Money < totalCost)
            {
                Debug.Log($"[MarketService] 돈 부족: 필요 {totalCost}, 보유 {state.Money}");
                return false;
            }
            if (cargo.RemainingCapacity < quantity)
            {
                Debug.Log($"[MarketService] 화물 용량 부족: 필요 {quantity}, 남은 {cargo.RemainingCapacity}");
                return false;
            }

            if (!state.TrySpend(totalCost)) return false;
            if (!cargo.TryAdd(product, quantity))
            {
                // 롤백 — 거의 발생 안 함
                state.AddMoney(totalCost);
                return false;
            }

            onBought?.Invoke(product, quantity);
            Debug.Log($"[MarketService] 구매: {product.displayNameKo} x{quantity} = {totalCost}G");
            return true;
        }

        /// <summary>판매 시도. 보유 수량 부족이면 false.</summary>
        public bool TrySell(PortData port, ProductData product, int quantity)
        {
            if (port == null || product == null || quantity <= 0) return false;

            var cargo = PlayerCargo.Instance;
            var state = PlayerState.Instance;
            if (cargo == null || state == null) return false;

            if (cargo.GetQuantity(product) < quantity)
            {
                Debug.Log($"[MarketService] 판매 불가 — 보유 수량 부족: 필요 {quantity}");
                return false;
            }

            int unitPrice = GetSellPrice(port, product);
            int totalRevenue = unitPrice * quantity;

            if (!cargo.TryRemove(product, quantity)) return false;
            state.AddMoney(totalRevenue);

            onSold?.Invoke(product, quantity);
            Debug.Log($"[MarketService] 판매: {product.displayNameKo} x{quantity} = {totalRevenue}G");
            return true;
        }
    }
}
