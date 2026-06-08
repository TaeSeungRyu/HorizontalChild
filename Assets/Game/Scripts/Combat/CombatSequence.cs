using System.Collections;
using Game.Data;
using Game.Save;
using Game.Ship;
using Game.UI;
using Game.World;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// M3.5 Phase 2 — 실제 시뮬레이션 기반 시각 전투.
    ///
    /// 시뮬레이션:
    ///   - 양측이 각자 attackInterval(초) 마다 포탄 1발 발사.
    ///   - 포탄은 cannonballFlightSeconds 동안 parabolic arc 로 비행.
    ///   - 명중 시 cannonPower 만큼 차감, hit flash. 명중 확률은 captain.seamanship / npc.bravery 보정.
    ///   - 한쪽 currentDurability 가 먼저 0 도달 → 전투 종료. 이미 비행 중이던 반대측 포탄은
    ///     도착 시점에 _combatOver==true 면 무효 (조용히 소멸).
    ///
    /// 결과:
    ///   - 시뮬레이션 결과를 CombatService.ApplyResult(playerWon) 에 전달 → 보상 + 메시지.
    ///   - 패배 NPC 는 NpcSpawner 재추첨 큐로.
    ///   - 플레이어 패배 시: ShipController.CurrentDurability 가 0 인 채 유지 → 항구에서 수리 필요.
    ///
    /// 어린이 친화: 데미지 숫자 표시 X. 결과 패널 메시지로 톤 마무리.
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

        public Color playerCannonColor = new Color(0.95f, 0.85f, 0.2f);   // 노란색
        public Color npcCannonColor = new Color(0.15f, 0.15f, 0.15f);     // 검은색

        [Header("Tuning — Hit Flash")]
        public Color hitFlashColor = Color.white;
        public float hitFlashSeconds = 0.18f;

        public bool IsBusy { get; private set; }
        private bool _combatOver;   // 한쪽 durability 0 도달 — 비행 중 포탄 무효화 신호

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>전투 시퀀스 시작. NpcShip 이 호출.</summary>
        public void Begin(ShipController player, NpcShip npcShip, CombatResultPanel resultPanel)
        {
            if (IsBusy || player == null || npcShip == null) return;
            StartCoroutine(RunSequence(player, npcShip, resultPanel));
        }

        private IEnumerator RunSequence(ShipController player, NpcShip npcShip, CombatResultPanel resultPanel)
        {
            IsBusy = true;
            _combatOver = false;

            // 1) 양측 초기화
            if (player.CurrentDurability <= 0) player.RestoreDurability();   // 안전장치 — 0 인데 전투 진입 막혔어야 함
            npcShip.ResetDurabilityForCombat();

            // 2) 락
            player.LockInput = true;
            npcShip.LockMovement = true;

            // 3) 양측 발사 코루틴 동시 실행
            // 옛 에셋 0 값 fallback — 인스펙터에서 명시 설정된 값은 그대로 사용
            var ship = player.shipData;
            float playerInterval = (ship != null && ship.attackInterval >= 0.3f) ? ship.attackInterval : 1.5f;
            int playerDmg = (ship != null && ship.cannonPower >= 1) ? ship.cannonPower : 3;
            float playerHit = ComputePlayerHitChance(player);
            StartCoroutine(FireLoop(player.transform, npcShip, playerInterval, playerDmg, playerHit,
                playerCannonColor, attackerIsPlayer: true, player));

            float npcInterval = npcShip.AttackInterval;
            int npcDmg = npcShip.CannonPower;
            float npcHit = ComputeNpcHitChance(npcShip);
            StartCoroutine(FireLoop(npcShip.transform, null, npcInterval, npcDmg, npcHit,
                npcCannonColor, attackerIsPlayer: false, player, npcShip));

            // 4) 종료 대기 — durability 0 또는 timeout
            float t = 0f;
            while (t < maxCombatSeconds && player != null && npcShip != null
                && player.CurrentDurability > 0 && npcShip.CurrentDurability > 0)
            {
                t += Time.deltaTime;
                yield return null;
            }

            _combatOver = true;

            // 약간의 dwell 시간 — 마지막 비행 중인 포탄이 도착 후 자기 소멸할 시간
            yield return new WaitForSeconds(cannonballFlightSeconds + 0.1f);

            // 5) 승자 판정
            bool playerWon;
            if (player == null || player.CurrentDurability <= 0) playerWon = false;
            else if (npcShip == null || npcShip.CurrentDurability <= 0) playerWon = true;
            else playerWon = player.CurrentDurability >= npcShip.CurrentDurability;   // timeout — 더 많이 남은 쪽

            // 6) 락 해제
            if (player != null) player.LockInput = false;
            if (npcShip != null) npcShip.LockMovement = false;

            // 7) 보상 + 메시지
            var result = CombatService.Instance != null
                ? CombatService.Instance.ApplyResult(player, npcShip != null ? npcShip.definition : null, playerWon)
                : default;

            if (playerWon && npcShip != null)
            {
                npcShip.DefeatByCombat();
            }
            else if (!playerWon && player != null)
            {
                // 패배 — 가장 가까운 항구로 즉시 귀환
                TeleportToNearestPort(player);
            }

            if (resultPanel != null) resultPanel.Show(result);
            SaveService.Instance?.SaveGame();

            IsBusy = false;
        }

        /// <summary>플레이어 위치 기준 가장 가까운 항구로 텔레포트.</summary>
        private void TeleportToNearestPort(ShipController player)
        {
            var spawner = NpcSpawner.Instance;
            PortCatalog catalog = spawner != null ? spawner.portCatalog : null;
            if (catalog == null || catalog.all == null || catalog.all.Length == 0) return;

            Vector3 from = player.transform.position;
            PortData nearest = null;
            float bestSq = float.MaxValue;
            foreach (var port in catalog.all)            {
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

        private float ComputePlayerHitChance(ShipController player)
        {
            int seamanship = (player != null && player.captain != null) ? player.captain.seamanship : 50;
            // 1~100 seamanship → ±15% 보정
            return Mathf.Clamp(playerBaseHitChance + (seamanship - 50) * 0.003f, 0.2f, 0.95f);
        }

        private float ComputeNpcHitChance(NpcShip npcShip)
        {
            int bravery = (npcShip != null && npcShip.definition != null && npcShip.definition.character != null)
                ? npcShip.definition.character.bravery : 50;
            return Mathf.Clamp(npcBaseHitChance + (bravery - 50) * 0.003f, 0.2f, 0.95f);
        }

        /// <summary>
        /// 한쪽 측의 발사 루프. attackInterval 마다 포탄 발사 코루틴 spawn.
        /// _combatOver 가 true 가 되면 새 포탄 발사 중단.
        /// </summary>
        private IEnumerator FireLoop(Transform from, NpcShip targetNpc,
            float interval, int damage, float hitChance, Color color,
            bool attackerIsPlayer, ShipController playerRef, NpcShip selfNpc = null)
        {
            // 초기 약간 딜레이 — 양측 동시 발사 어색함 회피
            yield return new WaitForSeconds(attackerIsPlayer ? 0.3f : 0.55f);

            while (!_combatOver)
            {
                if (from == null) yield break;
                Transform toTransform = attackerIsPlayer
                    ? (targetNpc != null ? targetNpc.transform : null)
                    : (playerRef != null ? playerRef.transform : null);
                if (toTransform == null) yield break;

                bool willHit = Random.value <= hitChance;
                StartCoroutine(FireCannonball(from, toTransform, color, damage, willHit,
                    attackerIsPlayer, playerRef, targetNpc ?? selfNpc));

                yield return new WaitForSeconds(interval);
            }
        }

        private IEnumerator FireCannonball(Transform from, Transform to, Color color,
            int damage, bool willHit, bool attackerIsPlayer, ShipController playerRef, NpcShip npcShipRef)
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
                if (attackerIsPlayer && npcShipRef != null && npcShipRef.CurrentDurability > 0)
                {
                    npcShipRef.ApplyDamage(damage);
                    StartCoroutine(HitFlash(npcShipRef.gameObject));
                }
                else if (!attackerIsPlayer && playerRef != null && playerRef.CurrentDurability > 0)
                {
                    playerRef.ApplyDamage(damage);
                    StartCoroutine(HitFlash(playerRef.gameObject));
                }
            }
            // _combatOver 면 그냥 ball 만 소멸 — 명중 무효

            if (ball != null) Destroy(ball);
        }

        private IEnumerator HitFlash(GameObject target)
        {
            if (target == null) yield break;
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null || renderer.material == null) yield break;
            var mat = renderer.material;

            string prop = mat.HasProperty("_BaseColor") ? "_BaseColor"
                        : mat.HasProperty("_Color") ? "_Color" : null;
            if (prop == null) yield break;

            Color original = mat.GetColor(prop);
            mat.SetColor(prop, hitFlashColor);
            yield return new WaitForSeconds(hitFlashSeconds);
            if (target != null && renderer != null && renderer.material != null)
            {
                renderer.material.SetColor(prop, original);
            }
        }
    }
}
