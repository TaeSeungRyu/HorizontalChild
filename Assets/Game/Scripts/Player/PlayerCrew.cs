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

        /// <summary>
        /// 전문가 보너스 — 각 선원의 **가장 높은 능력치(전공)** 에만 보너스 부여.
        ///   bonus = clamp(maxStat / 20, floor, 5)
        ///   floor: 해적 = 3 / 그 외 = 1
        /// 한 선원은 한 stat 에만 기여하며, 균등 시 우선순위는 용기 > 항해 > 눈썰미.
        /// 합산값은 ShipController·CombatSequence·발견 시스템이 captain.stat 에 더함.
        /// </summary>
        public Vector3Int TotalHireBonus()
        {
            int b = 0, s = 0, k = 0;
            foreach (var n in _crew)
            {
                if (n == null || n.character == null) continue;
                var c = n.character;
                int maxStat = Mathf.Max(c.bravery, Mathf.Max(c.seamanship, c.keenEye));
                int floor = (n.type == NpcType.Pirate) ? 3 : 1;
                int bonus = Mathf.Clamp(maxStat / 20, floor, 5);

                if (c.bravery >= c.seamanship && c.bravery >= c.keenEye) b += bonus;
                else if (c.seamanship >= c.keenEye) s += bonus;
                else k += bonus;
            }
            return new Vector3Int(b, s, k);
        }
        public int BraveryBonus => TotalHireBonus().x;
        public int SeamanshipBonus => TotalHireBonus().y;
        public int KeenEyeBonus => TotalHireBonus().z;
    }
}
