using System.Collections;
using System.Collections.Generic;
using Game.Data;
using Game.Save;
using Game.Ship;
using Game.UI;
using Game.World;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// M3.5 Phase 2 — 실제 시뮬레이션 기반 시각 전투 (다중 NPC 지원).
    ///
    /// 흐름:
    ///   1) 시작 시점에 같은 영역의 모든 해적 NPC 가 attacker 리스트에 포함 (2:1, 3:1 ...).
    ///   2) 양측 입력·이동 락 — ShipController.LockInput, NpcShip.LockMovement.
    ///      ShipController 는 LockInput=true 순간 _currentSpeed 즉시 0.
    ///   3) 발사 루프:
    ///      - 플레이어 fire loop 1개 — 매 발 살아있는 NPC 중 가장 가까운 것 자동 타겟.
    ///      - 각 NPC 별로 fire loop 1개 — 모두 플레이어 향해 발사.
    ///   4) 종료 조건: player.durability ≤ 0 (패배) 또는 모든 NPC.durability ≤ 0 (승리) 또는 timeout.
    ///   5) _combatOver=true → 비행 중인 마지막 포탄들은 도착 시 자기 소멸 (무효).
    ///   6) 결과:
    ///      - 승리 → 리스트 내 모든 NPC DefeatByCombat (재추첨 큐).
    ///      - 패배 → 가장 가까운 항구로 즉시 텔레포트.
    ///   7) 락 해제 + 결과 패널 + 저장.
    /// </summary>
    public class CombatSequence : MonoBehaviour
    {
        public static CombatSequence Instance { get; private set; }

        [Header("Tuning — 시뮬레이션")]
        [Tooltip("플레이어 기본 명중률(50). seamanship 1~100 보정 적용.")]
        [Range(0.2f, 0.95f)] public float playerBaseHitChance = 0.65f;

        [Tooltip("NPC 기본 명중률. bravery 1~100 보정 적용.")]
        [Range(0.2f, 0.95f)] public float npcBaseHitChance = 0.55f;

        [Tooltip("최대 전투 시간(초). 안전장치 — 양측 다 안 죽으면 더 큰 durability 가 승.")]
        [Range(5f, 60f)] public float maxCombatSeconds = 20f;

        [Header("Tuning — 포탄 비주얼")]
        [Tooltip("한 발 비행 시간(초).")]
        public float cannonballFlightSeconds = 0.7f;

        [Tooltip("포탄 sphere 직경.")]
        public float cannonballSize = 1.4f;

        [Tooltip("발사·도착 Y 오프셋 (배 위에서 발사).")]
        public float cannonHeightOffset = 1.5f;

        [Tooltip("parabolic 정점 높이.")]
        public float cannonballArcHeight = 4f;

        public Color playerCannonColor = new Color(0.95f, 0.85f, 0.2f);
        public Color npcCannonColor = new Color(0.15f, 0.15f, 0.15f);

        [Header("Tuning — Hit Flash")]
        public Color hitFlashColor = Color.white;
        public float hitFlashSeconds = 0.18f;

        public bool IsBusy { get; private set; }
        private bool _combatOver;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>전투 시퀀스 시작. attackers 는 1명 이상 (2:1 가능).</summary>
        public void Begin(ShipController player, List<NpcShip> attackers, CombatResultPanel resultPanel)
        {
            if (IsBusy || player == null || attackers == null || attackers.Count == 0) return;
            StartCoroutine(RunSequence(player, attackers, resultPanel));
        }

        private IEnumerator RunSequence(ShipController player, List<NpcShip> attackers, CombatResultPanel resultPanel)
        {
            IsBusy = true;
            _combatOver = false;

            // 1) 초기화 — durability 재충전
            if (player.CurrentDurability <= 0) player.RestoreDurability();
            foreach (var npc in attackers)
            {
                if (npc != null) npc.ResetDurabilityForCombat();
            }

            // 2) 락 — 즉시 정지 (ShipController.LockInput setter 가 속도 0 으로 리셋)
            player.LockInput = true;
            foreach (var npc in attackers)
            {
                if (npc != null) npc.LockMovement = true;
            }

            // 3) 발사 루프
            var ship = player.shipData;
            float playerInterval = (ship != null && ship.attackInterval >= 0.3f) ? ship.attackInterval : 1.5f;
            int playerDmg = (ship != null && ship.cannonPower >= 1) ? ship.cannonPower : 3;
            float playerHit = ComputePlayerHitChance(player);
            StartCoroutine(PlayerFireLoop(player, attackers, playerInterval, playerDmg, playerHit));

            for (int i = 0; i < attackers.Count; i++)
            {
                var npc = attackers[i];
                if (npc == null) continue;
                // NPC 들끼리 발사 타이밍 살짝 분산 — 동시 발사 부자연스러움 회피
                StartCoroutine(NpcFireLoop(player, npc, ComputeNpcHitChance(npc), 0.55f + i * 0.25f));
            }

            // 4) 종료 대기
            float t = 0f;
            while (t < maxCombatSeconds && player != null && player.CurrentDurability > 0 && AnyAlive(attackers))
            {
                t += Time.deltaTime;
                yield return null;
            }

            _combatOver = true;
            // 비행 중 포탄들이 도착 후 자기 무효화될 시간
            yield return new WaitForSeconds(cannonballFlightSeconds + 0.1f);

            // 5) 승자 판정
            bool playerWon = player != null && player.CurrentDurability > 0;

            // 6) 락 해제 — 이동 다시 가능
            if (player != null) player.LockInput = false;
            foreach (var npc in attackers)
            {
                if (npc != null) npc.LockMovement = false;
            }

            // 7) 보상 — primary NPC (리스트 첫 번째 또는 살아있던 마지막) 기준 메시지
            NpcShip primary = attackers.Count > 0 ? attackers[0] : null;
            var result = CombatService.Instance != null
                ? CombatService.Instance.ApplyResult(player, primary != null ? primary.definition : null, playerWon)
                : default;

            if (playerWon)
            {
                // 승리 — 모든 NPC 격침 (잔여 HP 있어도 도주로 간주)
                foreach (var npc in attackers)
                {
                    if (npc != null) npc.DefeatByCombat();
                }
            }
            else if (player != null)
            {
                TeleportToNearestPort(player);
            }

            if (resultPanel != null) resultPanel.Show(result);
            SaveService.Instance?.SaveGame();

            IsBusy = false;
        }

        // ─── 헬퍼 ───────────────────────────────────────────────────────────

        private static bool AnyAlive(List<NpcShip> npcs)
        {
            for (int i = 0; i < npcs.Count; i++)
            {
                var n = npcs[i];
                if (n != null && n.CurrentDurability > 0) return true;
            }
            return false;
        }

        private static NpcShip FindClosestAlive(List<NpcShip> npcs, Vector3 from)
        {
            NpcShip best = null;
            float bestSq = float.MaxValue;
            for (int i = 0; i < npcs.Count; i++)
            {
                var n = npcs[i];
                if (n == null || n.CurrentDurability <= 0) continue;
                var d = n.transform.position - from;
                float sq = d.x * d.x + d.z * d.z;
                if (sq < bestSq) { bestSq = sq; best = n; }
            }
            return best;
        }

        private float ComputePlayerHitChance(ShipController player)
        {
            int seamanship = (player != null && player.captain != null) ? player.captain.seamanship : 50;
            return Mathf.Clamp(playerBaseHitChance + (seamanship - 50) * 0.003f, 0.2f, 0.95f);
        }

        private float ComputeNpcHitChance(NpcShip npcShip)
        {
            int bravery = (npcShip != null && npcShip.definition != null && npcShip.definition.character != null)
                ? npcShip.definition.character.bravery : 50;
            return Mathf.Clamp(npcBaseHitChance + (bravery - 50) * 0.003f, 0.2f, 0.95f);
        }

        // ─── 발사 루프 ──────────────────────────────────────────────────────

        private IEnumerator PlayerFireLoop(ShipController player, List<NpcShip> targets,
            float interval, int damage, float hitChance)
        {
            yield return new WaitForSeconds(0.3f);
            while (!_combatOver)
            {
                if (player == null) yield break;
                var target = FindClosestAlive(targets, player.transform.position);
                if (target == null) yield break;
                bool willHit = Random.value <= hitChance;
                StartCoroutine(FireCannonball(player.transform, target.transform, playerCannonColor,
                    damage, willHit, attackerIsPlayer: true, player, target));
                yield return new WaitForSeconds(interval);
            }
        }

        private IEnumerator NpcFireLoop(ShipController player, NpcShip npc, float hitChance, float initialDelay)
        {
            yield return new WaitForSeconds(initialDelay);
            int damage = npc.CannonPower;
            float interval = npc.AttackInterval;
            while (!_combatOver)
            {
                if (npc == null || npc.CurrentDurability <= 0) yield break;
                if (player == null) yield break;
                bool willHit = Random.value <= hitChance;
                StartCoroutine(FireCannonball(npc.transform, player.transform, npcCannonColor,
                    damage, willHit, attackerIsPlayer: false, player, npc));
                yield return new WaitForSeconds(interval);
            }
        }

        // ─── 포탄·이펙트 ────────────────────────────────────────────────────

        private IEnumerator FireCannonball(Transform from, Transform to, Color color,
            int damage, bool willHit, bool attackerIsPlayer, ShipController playerRef, NpcShip npcRef)
        {
            if (from == null || to == null) yield break;

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Cannonball";
            ball.transform.localScale = Vector3.one * cannonballSize;

            var col = ball.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var renderer = ball.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                renderer.material = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            Vector3 startPos = from.position + Vector3.up * cannonHeightOffset;
            float elapsed = 0f;
            while (elapsed < cannonballFlightSeconds)
            {
                if (ball == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / cannonballFlightSeconds);
                Vector3 endPos = (to != null ? to.position : startPos) + Vector3.up * cannonHeightOffset;
                var pos = Vector3.Lerp(startPos, endPos, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * cannonballArcHeight;
                ball.transform.position = pos;
                yield return null;
            }

            // 도착 — 무효화 검사
            if (!_combatOver && willHit)
            {
                if (attackerIsPlayer && npcRef != null && npcRef.CurrentDurability > 0)
                {
                    npcRef.ApplyDamage(damage);
                    StartCoroutine(HitFlash(npcRef.gameObject));
                }
                else if (!attackerIsPlayer && playerRef != null && playerRef.CurrentDurability > 0)
                {
                    playerRef.ApplyDamage(damage);
                    StartCoroutine(HitFlash(playerRef.gameObject));
                }
            }

            if (ball != null) Destroy(ball);
        }

        private IEnumerator HitFlash(GameObject target)
        {
            if (target == null) yield break;
            // 절차적 배는 root 에 Renderer 없고 children 만 있음 → GetComponentsInChildren
            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0) yield break;

            // 원래 색 저장하고 hitFlashColor 로 모두 깜빡
            var originalColors = new Color[renderers.Length];
            var props = new string[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                var mat = renderers[i] != null ? renderers[i].material : null;
                if (mat == null) continue;
                props[i] = mat.HasProperty("_BaseColor") ? "_BaseColor"
                         : mat.HasProperty("_Color") ? "_Color" : null;
                if (props[i] != null)
                {
                    originalColors[i] = mat.GetColor(props[i]);
                    mat.SetColor(props[i], hitFlashColor);
                }
            }

            yield return new WaitForSeconds(hitFlashSeconds);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i].material == null || props[i] == null) continue;
                renderers[i].material.SetColor(props[i], originalColors[i]);
            }
        }

        // ─── 패배 텔레포트 ──────────────────────────────────────────────────

        private void TeleportToNearestPort(ShipController player)
        {
            var spawner = NpcSpawner.Instance;
            PortCatalog catalog = spawner != null ? spawner.portCatalog : null;
            if (catalog == null || catalog.all == null || catalog.all.Length == 0) return;

            Vector3 from = player.transform.position;
            PortData nearest = null;
            float bestSq = float.MaxValue;
            foreach (var port in catalog.all)
            {
                if (port == null) continue;
                var p = GeoCoordinate.LatLngToWorld(port.latitude, port.longitude);
                float dx = p.x - from.x;
                float dz = p.z - from.z;
                float sq = dx * dx + dz * dz;
                if (sq < bestSq) { bestSq = sq; nearest = port; }
            }
            if (nearest == null) return;

            var target = GeoCoordinate.LatLngToWorld(nearest.latitude, nearest.longitude);
            player.transform.position = new Vector3(target.x, from.y, target.z);
            Debug.Log($"[CombatSequence] 패배 — {nearest.displayNameKo} 항구로 강제 귀환");
        }
    }
}
