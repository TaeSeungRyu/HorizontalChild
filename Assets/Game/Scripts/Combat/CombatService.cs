using Game.Data;
using Game.Player;
using Game.Ship;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Combat
{
    /// <summary>
    /// 해상 전투 서비스 — 자동 전투 (어린이 친화: 즉시 해결, 데미지 없음).
    ///
    /// 공식 (어린이 친화 ~70% 승률 목표):
    ///   playerPower = ship.cannonPower × 15 + captain.bravery + 무작위(0~30)
    ///   npcPower    = npcChar.bravery + 무작위(0~30)
    ///   playerPower ≥ npcPower → 승리
    ///
    /// 예시 (ship.cannonPower=3, captain.bravery=60):
    ///   playerPower 평균 ≈ 45 + 60 + 15 = 120
    ///   해적(검은수염, bravery=85) 평균 ≈ 85 + 15 = 100  → 플레이어 우세
    ///   해적(약함, bravery=50) 평균 ≈ 65                  → 압도
    ///   강한 해적(bravery=100) 평균 ≈ 115                 → 박빙
    ///
    /// 보상·패널티 (GAME_MECHANICS §6.3):
    ///   해적 격파: +돈 +좋은명성
    ///   상선 약탈: +돈 +나쁜명성
    ///   호위선 승리: +돈
    ///   패배: −돈 (durability 손실은 추후 ShipDurabilityService 도입 시)
    ///
    /// 추후 확장 (MVP → 풀세트):
    ///   - NPC 이동 AI (random walk / chase / flee)
    ///   - 실시간 전투 — 양측 turn 으로 공격, ship.durability 차감
    ///   - 전투 중 도주 옵션 (속도 비교)
    ///   - 명성 게이트 — 낮으면 약한 NPC, 높으면 강한 NPC 만남
    /// </summary>
    public class CombatService : MonoBehaviour
    {
        public static CombatService Instance { get; private set; }

        [Header("Reward Range")]
        public int winMoneyMin = 100;
        public int winMoneyMax = 500;
        public int loseMoneyMin = 50;
        public int loseMoneyMax = 200;

        public int winReputationGain = 10;
        public int badReputationGain = 10;

        [Header("Events")]
        public UnityEvent<CombatResult> onCombatResolved;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>전투 해결 + 보상 적용. CombatResult 반환.</summary>
        public CombatResult Resolve(ShipController player, NpcDefinition npc)
        {
            var ship = player != null ? player.shipData : null;
            var pCaptain = player != null ? player.captain : null;
            var npcChar = npc != null ? npc.character : null;

            int playerPower = (ship != null ? ship.cannonPower : 3) * 15
                              + (pCaptain != null ? pCaptain.bravery : 50)
                              + Random.Range(0, 30);
            int npcPower = (npcChar != null ? npcChar.bravery : 50)
                           + Random.Range(0, 30);
            bool win = playerPower >= npcPower;

            var result = new CombatResult
            {
                playerName = pCaptain != null ? pCaptain.displayNameKo : "선장",
                npcName = npcChar != null ? npcChar.displayNameKo : "낯선 배",
                playerPower = playerPower,
                npcPower = npcPower,
                playerWon = win,
            };

            if (win)
            {
                result.moneyDelta = Random.Range(winMoneyMin, winMoneyMax);
                if (npc != null && npc.type == NpcType.Pirate)
                {
                    result.repGoodDelta = winReputationGain;
                    result.message = "해적을 무찔렀어요! 사람들이 칭찬해요.";
                }
                else if (npc != null && npc.type == NpcType.Merchant)
                {
                    result.repBadDelta = badReputationGain;
                    result.message = "상선을 약탈했어요. 나쁜 소문이 돌아요.";
                }
                else
                {
                    result.message = "전투에서 이겼어요.";
                }
            }
            else
            {
                result.moneyDelta = -Random.Range(loseMoneyMin, loseMoneyMax);
                result.message = "전투에 졌어요. 잔돈을 조금 잃었어요.";
            }

            // 적용
            var state = PlayerState.Instance;
            if (state != null)
            {
                if (result.moneyDelta != 0) state.AddMoney(result.moneyDelta);
                if (result.repGoodDelta > 0) state.AddGoodReputation(result.repGoodDelta);
                if (result.repBadDelta > 0) state.AddBadReputation(result.repBadDelta);
            }

            onCombatResolved?.Invoke(result);
            Debug.Log(
                $"[CombatService] {result.playerName}({result.playerPower}) vs " +
                $"{result.npcName}({result.npcPower}) — {(win ? "승리" : "패배")} " +
                $"돈 {result.moneyDelta:+#;-#;0}, 좋은명성 +{result.repGoodDelta}, 나쁜명성 +{result.repBadDelta}");
            return result;
        }
    }
}
