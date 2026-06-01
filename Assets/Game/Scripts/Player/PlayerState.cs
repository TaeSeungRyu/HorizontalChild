using UnityEngine;
using UnityEngine.Events;

namespace Game.Player
{
    /// <summary>
    /// 플레이어 런타임 상태 — 자금 + 좋은/나쁜 명성.
    ///
    /// 싱글톤. GameManager 또는 PlayerShip GameObject 에 부착 권장.
    /// 다른 컴포넌트는 PlayerState.Instance 로 접근.
    ///
    /// 어린이 친화 (`GAME_MECHANICS.md` §8.15):
    ///   - 최저 자금 보장(`minMoneyFloor`) — 패배 시에도 최소 자금은 남음. M1 에서는 패배 시스템 미구현이라 적용 안 됨.
    ///   - 음수 자금 불가.
    ///   - 명성은 0 ~ 50,000 클램프.
    ///
    /// M1 단순화: 메모리만. JSON 저장은 M3 의 Save 시스템에서.
    /// </summary>
    public class PlayerState : MonoBehaviour
    {
        public static PlayerState Instance { get; private set; }

        [Header("Starting Values")]
        [Tooltip("게임 시작 시 자금.")]
        public int startingMoney = 5000;

        [Tooltip("최저 자금 — 패배 등으로 깎여도 이 아래로는 안 떨어짐.")]
        public int minMoneyFloor = 100;

        [Header("Events")]
        public UnityEvent<int> onMoneyChanged;
        public UnityEvent<int> onGoodReputationChanged;
        public UnityEvent<int> onBadReputationChanged;

        public int Money { get; private set; }
        public int GoodReputation { get; private set; }
        public int BadReputation { get; private set; }

        private const int MaxReputation = 50000;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[PlayerState] 인스턴스가 둘 이상. {gameObject.name} 무시.");
                Destroy(this);
                return;
            }
            Instance = this;

            Money = startingMoney;
            GoodReputation = 0;
            BadReputation = 0;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─── 자금 ────────────────────────────────────────────────────────────

        public void AddMoney(int amount)
        {
            if (amount == 0) return;
            Money = Mathf.Max(0, Money + amount);
            onMoneyChanged?.Invoke(Money);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (Money < amount) return false;
            Money -= amount;
            onMoneyChanged?.Invoke(Money);
            return true;
        }

        /// <summary>패배 시 자금 비율 차감 — minMoneyFloor 까지만 깎임.</summary>
        public int LosePortion(float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            int toLose = Mathf.RoundToInt(Money * fraction);
            int newMoney = Mathf.Max(minMoneyFloor, Money - toLose);
            int actualLoss = Money - newMoney;
            Money = newMoney;
            if (actualLoss != 0) onMoneyChanged?.Invoke(Money);
            return actualLoss;
        }

        // ─── 명성 ────────────────────────────────────────────────────────────

        public void AddGoodReputation(int amount)
        {
            if (amount == 0) return;
            GoodReputation = Mathf.Clamp(GoodReputation + amount, 0, MaxReputation);
            onGoodReputationChanged?.Invoke(GoodReputation);
        }

        public void AddBadReputation(int amount)
        {
            if (amount == 0) return;
            BadReputation = Mathf.Clamp(BadReputation + amount, 0, MaxReputation);
            onBadReputationChanged?.Invoke(BadReputation);
        }
    }
}
