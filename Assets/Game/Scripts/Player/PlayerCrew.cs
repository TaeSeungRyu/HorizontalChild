using System.Collections.Generic;
using Game.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Player
{
    /// <summary>
    /// 플레이어가 고용한 NPC 선원 명부 (최대 10명).
    ///
    /// NPC 는 더 이상 선장 교체가 아니라 "선원 아이템" — 고용 시 hireBonus 가
    /// 플레이어 능력치에 합산됨 (ShipController·CombatService·CombatSequence 가 조회).
    ///
    /// 제약:
    ///   - 최대 10명
    ///   - NpcDefinition.requiredGoodReputation / requiredBadReputation 명성 게이트
    ///   - NpcDefinition.hireBasePrice 비용
    /// </summary>
    public class PlayerCrew : MonoBehaviour
    {
        public static PlayerCrew Instance { get; private set; }

        [Range(1, 20)] public int maxCrew = 10;

        public UnityEvent onCrewChanged;

        private readonly List<NpcDefinition> _crew = new();

        public IReadOnlyList<NpcDefinition> Crew => _crew;
        public int Count => _crew.Count;
        public bool IsFull => _crew.Count >= maxCrew;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool Contains(NpcDefinition npc) => npc != null && _crew.Contains(npc);

        public bool TryHire(NpcDefinition npc)
        {
            if (npc == null || IsFull || _crew.Contains(npc)) return false;
            _crew.Add(npc);
            onCrewChanged?.Invoke();
            return true;
        }

        public bool Dismiss(NpcDefinition npc)
        {
            if (npc == null) return false;
            if (_crew.Remove(npc))
            {
                onCrewChanged?.Invoke();
                return true;
            }
            return false;
        }

        public void Clear()
        {
            if (_crew.Count == 0) return;
            _crew.Clear();
            onCrewChanged?.Invoke();
        }

        // 능력치 보너스 합산 — 선장 stats 에 더해짐
        public Vector3Int TotalHireBonus()
        {
            var sum = Vector3Int.zero;
            foreach (var n in _crew)
            {
                if (n != null) sum += n.hireBonus;
            }
            return sum;
        }
        public int BraveryBonus => TotalHireBonus().x;
        public int SeamanshipBonus => TotalHireBonus().y;
        public int KeenEyeBonus => TotalHireBonus().z;
    }
}
