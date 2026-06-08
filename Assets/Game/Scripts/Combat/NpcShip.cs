using Game.Data;
using Game.Ship;
using Game.UI;
using Game.World;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Combat
{
    /// <summary>
    /// 해상 NPC 배 — 이동 AI + 클릭 시 전투.
    ///
    /// M3.5 Phase 1 행동:
    ///   - 해적 (patrolRange 안에서):
    ///     • 플레이어 근접 → 추적
    ///     • 평소 → random walk
    ///     • homePort 에서 patrolRange 초과 시 → 복귀
    ///   - 상선 (patrolPorts 2개 이상):
    ///     • 플레이어 근접 → 도주
    ///     • 평소 → 다음 항구 향해 항해, 도착 시 다음 항구
    ///   - 호위선:
    ///     • homePort 주변 순찰 (좁은 patrolRange)
    ///
    /// 저장 가능 상태: Position + currentRouteIndex.
    /// SaveService 가 NpcSpawner 를 통해 일괄 수집·복원.
    /// </summary>
    public class NpcShip : MonoBehaviour, IPointerClickHandler
    {
        public NpcDefinition definition;
        public CombatResultPanel resultPanel;

        [Header("AI — 공통")]
        public float wanderSpeed = 4f;
        public float directionChangeInterval = 4f;

        [Header("AI — 타입별")]
        public float pirateChaseRange = 80f;
        public float pirateChaseSpeed = 7f;
        public float merchantFleeRange = 60f;
        public float merchantFleeSpeed = 6f;
        public float tradeCruiseSpeed = 5f;

        [Header("World Bounds")]
        public float worldHalfWidth = 2700f;
        public float worldHalfDepth = 1350f;

        [Header("Land Avoidance")]
        public float landCheckRadius = 3f;

        [Tooltip("항구 도착 인정 거리.")]
        public float portArriveDistance = 12f;

        // 런타임 — 저장 가능
        private Vector3 _wanderDirection;
        private float _nextDirectionChange;
        private int _routeIndex;
        private ShipController _playerShip;
        private static readonly Collider[] _landBuffer = new Collider[8];

        // 외부 (SaveService) 가 사용
        public int RouteIndex { get => _routeIndex; set => _routeIndex = value; }

        public void Bind(NpcDefinition def, CombatResultPanel panel)
        {
            definition = def;
            resultPanel = panel;
            if (def != null && def.character != null)
            {
                name = $"Npc_{def.type}_{def.character.displayNameKo}";
            }
        }

        private void Start()
        {
            _playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);
            PickNewWanderDirection();
        }

        private void Update()
        {
            if (definition == null) return;
            UpdateMovement();
        }

        // ─── AI ──────────────────────────────────────────────────────────

        private void UpdateMovement()
        {
            Vector3 desiredDir = Vector3.zero;
            float speed = wanderSpeed;

            // 플레이어 상호작용 우선
            float playerDist = float.MaxValue;
            Vector3 playerOffset = Vector3.zero;
            if (_playerShip != null)
            {
                playerOffset = _playerShip.transform.position - transform.position;
                playerOffset.y = 0f;
                playerDist = playerOffset.magnitude;
            }

            // 1) 해적 추적 / 상선 도주
            if (definition.type == NpcType.Pirate && playerDist <= pirateChaseRange && playerDist > 1f)
            {
                desiredDir = playerOffset.normalized;
                speed = pirateChaseSpeed;
            }
            else if (definition.type == NpcType.Merchant && playerDist <= merchantFleeRange && playerDist > 1f)
            {
                desiredDir = -playerOffset.normalized;
                speed = merchantFleeSpeed;
            }
            else
            {
                desiredDir = ComputeRouteOrPatrolDirection(out speed);
            }

            // 2) 이동 + 육지 충돌 검사
            var nextPos = transform.position + desiredDir * speed * Time.deltaTime;
            if (IsLandAt(nextPos))
            {
                _wanderDirection = -desiredDir;
                _nextDirectionChange = Time.time + 1f;
                return;
            }

            // 3) 세계 경계 clamp
            nextPos.x = Mathf.Clamp(nextPos.x, -worldHalfWidth, worldHalfWidth);
            nextPos.z = Mathf.Clamp(nextPos.z, -worldHalfDepth, worldHalfDepth);
            if (Mathf.Abs(nextPos.x) >= worldHalfWidth - 0.1f ||
                Mathf.Abs(nextPos.z) >= worldHalfDepth - 0.1f)
            {
                PickNewWanderDirection();
            }
            transform.position = nextPos;

            // 4) 회전
            if (desiredDir.sqrMagnitude > 0.01f)
            {
                var lookRot = Quaternion.LookRotation(new Vector3(desiredDir.x, 0f, desiredDir.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 3f * Time.deltaTime);
            }
        }

        /// <summary>
        /// 플레이어 우선 행동이 아닐 때 — 타입별 정상 행동.
        /// 상선: 항로 다음 항구로 향함
        /// 해적/호위선: patrolRange 밖이면 home 복귀, 안이면 random walk
        /// </summary>
        private Vector3 ComputeRouteOrPatrolDirection(out float speed)
        {
            speed = wanderSpeed;

            // 상선 무역 항로
            if (definition.type == NpcType.Merchant && definition.patrolPorts != null
                && definition.patrolPorts.Length >= 2)
            {
                var port = definition.patrolPorts[_routeIndex % definition.patrolPorts.Length];
                if (port != null)
                {
                    var targetPos = GeoCoordinate.LatLngToWorld(port.latitude, port.longitude);
                    var offset = targetPos - transform.position;
                    offset.y = 0f;
                    float dist = offset.magnitude;
                    if (dist <= portArriveDistance)
                    {
                        _routeIndex = (_routeIndex + 1) % definition.patrolPorts.Length;
                    }
                    else
                    {
                        speed = tradeCruiseSpeed;
                        return offset.normalized;
                    }
                }
            }

            // 해적/호위선 — homePort 에서 멀어졌으면 복귀
            if (definition.homePort != null && definition.patrolRange > 0f)
            {
                var homePos = GeoCoordinate.LatLngToWorld(
                    definition.homePort.latitude, definition.homePort.longitude);
                var toHome = homePos - transform.position;
                toHome.y = 0f;
                float distFromHome = toHome.magnitude;
                if (distFromHome > definition.patrolRange)
                {
                    return toHome.normalized;
                }
            }

            // 평소 — random walk
            if (Time.time >= _nextDirectionChange) PickNewWanderDirection();
            return _wanderDirection;
        }

        private void PickNewWanderDirection()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            _wanderDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            _nextDirectionChange = Time.time + directionChangeInterval + Random.Range(-1f, 1f);
        }

        private bool IsLandAt(Vector3 worldPos)
        {
            int count = Physics.OverlapSphereNonAlloc(worldPos, landCheckRadius, _landBuffer);
            for (int i = 0; i < count; i++)
            {
                if (_landBuffer[i] == null) continue;
                if (_landBuffer[i].GetComponentInParent<Landmass>() != null) return true;
            }
            return false;
        }

        // ─── 클릭 → 전투 ─────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (definition == null) return;
            if (_playerShip == null) _playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);
            if (_playerShip == null) return;
            var service = CombatService.Instance;
            if (service == null)
            {
                Debug.LogWarning("[NpcShip] CombatService 없음 — 전투 불가");
                return;
            }
            var result = service.Resolve(_playerShip, definition);
            if (resultPanel != null) resultPanel.Show(result);
            if (result.playerWon) Destroy(gameObject, 0.5f);
        }
    }
}
