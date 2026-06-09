using System.Collections.Generic;
using Game.Data;
using Game.Save;
using Game.Ship;
using Game.UI;
using Game.World;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Combat
{
    /// <summary>
    /// 해상 NPC 배 — 이동 AI + 클릭 시 전투. (M3.5 풀세트)
    ///
    /// 행동 모델:
    ///   - Merchant: ConfigureSailing 으로 받은 _sailingTarget 으로 직선 항해 → 도착 시 EnterDwell.
    ///   - Pirate / Escort: ConfigureSailing 으로 받은 wander 시간 동안 random walk →
    ///                      시간 끝나면 _sailingTarget = homePort 로 자동 전환 → 도착 시 EnterDwell.
    ///   - 플레이어 근접: 해적 추적 / 상선 도주 (기존 유지).
    ///
    /// NpcSpawner 가 NPC 의 dwell ↔ active 전환을 관리. NpcShip 은 active 동안의 항해만 담당.
    /// </summary>
    public class NpcShip : MonoBehaviour, IPointerClickHandler
    {
        public NpcDefinition definition;
        public CombatResultPanel resultPanel;

        [Header("AI — 공통")]
        public float wanderSpeed = 4f;
        public float directionChangeInterval = 4f;

        [Header("AI — 타입별")]
        public float pirateChaseRange = 22f;
        public float pirateChaseSpeed = 7f;
        public float merchantFleeRange = 60f;
        public float merchantFleeSpeed = 6f;
        public float tradeCruiseSpeed = 5f;

        [Header("Patrol / Sailing")]
        [Tooltip("본거지 / 목적지 도착 인정 거리.")]
        public float homeArriveDistance = 25f;
        [Tooltip("복귀(또는 목적지 항해) 시작 후 이 시간(초) 동안 도착 못 하면 텔레포트. 좁은 해협 안전장치.")]
        public float homeReturnTimeoutSeconds = 60f;

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

        // 육지 회피 — 충돌 시 수직 방향으로 잠깐 미끄러져 우회
        private Vector3 _deflectDir;
        private float _deflectUntil;

        // 항해 상태
        private PortData _sailingTarget;    // 최종 목표 항구. 마지막 waypoint 도착 시 EnterDwell.
        private float _wanderEndsAt;        // 해적·호위선: wander 단계 종료 시각. 끝나면 _sailingTarget=homePort.
        private float _returnStartedAt;     // 항해 시작 시각. 데드라인 계산용. 0 = 미진입.
        private List<Vector3> _waypoints;   // NavRouter 로 계산된 경유 점들 (마지막 = target). null = 직접 항해.
        private int _waypointIdx;

        // 해적 자동 전투 — 시각 인디케이터 + 쿨다운
        [Header("Pirate Auto-Engage")]
        [Tooltip("이 시간(초) 이전엔 자동 전투 안 함. spawn 직후 즉시 도발 방지.")]
        public float engagementGraceSeconds = 2f;
        [Tooltip("전투 한 번 후 다음 자동 전투까지 쿨다운(초).")]
        public float engagementCooldownSeconds = 5f;
        private GameObject _chaseIndicator;
        private float _nextEngageTime;
        private bool _engagementOver;   // 전투 후 destroy 대기 중 — 추가 행동 차단

        // 외부 (SaveService) 가 사용
        public int RouteIndex { get => _routeIndex; set => _routeIndex = value; }

        /// <summary>전투 연출 동안 NpcShip 의 자체 이동·자동전투를 막음. CombatSequence 가 set.</summary>
        public bool LockMovement { get; set; }

        // 전투용 — spawn 시 maxDurability 로 초기화, 매 전투 시작마다 재충전
        // 기본값 fallback: 0 = 미직렬화된 옛 에셋 → 합리적 기본 적용 (인스펙터로 명시 설정한 값은 보존)
        private int _currentDurability;
        public int CurrentDurability => _currentDurability;
        public int MaxDurability => (definition != null && definition.maxDurability >= 10) ? definition.maxDurability : 40;
        public int CannonPower => (definition != null && definition.cannonPower >= 1) ? definition.cannonPower : 3;
        public float AttackInterval => (definition != null && definition.attackInterval >= 0.3f) ? definition.attackInterval : 1.6f;

        public void ResetDurabilityForCombat() => _currentDurability = MaxDurability;
        public void ApplyDamage(int amount) => _currentDurability = Mathf.Max(0, _currentDurability - amount);

        /// <summary>전투 연출에서 패배 처리. NpcSpawner 큐 등록 + 0.5초 후 파괴.</summary>
        public void DefeatByCombat()
        {
            if (_engagementOver) return;
            _engagementOver = true;
            NpcSpawner.Instance?.OnNpcDefeated(definition);
            Destroy(gameObject, 0.5f);
        }

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
            _nextEngageTime = Time.time + engagementGraceSeconds;
            if (definition != null && definition.type == NpcType.Pirate)
            {
                CreateChaseIndicator();
            }
        }

        /// <summary>
        /// NpcSpawner 가 spawn 직후 호출 — 항해 모드 설정.
        /// target != null → NavRouter 로 waypoint 경로 계산 후 따라감.
        /// target == null → wanderSeconds 동안 random walk → 자동으로 homePort 향해 복귀.
        /// </summary>
        public void ConfigureSailing(PortData target, float wanderSeconds)
        {
            _sailingTarget = target;
            _wanderEndsAt = wanderSeconds > 0f ? Time.time + wanderSeconds : 0f;
            _returnStartedAt = (target != null) ? Time.time : 0f;
            RebuildWaypoints();
        }

        private void RebuildWaypoints()
        {
            if (_sailingTarget == null) { _waypoints = null; _waypointIdx = 0; return; }
            _waypoints = NavRouter.ComputePath(transform.position, _sailingTarget);
            _waypointIdx = 0;
        }

        private void OnDestroy()
        {
            if (_chaseIndicator != null) Destroy(_chaseIndicator);
        }

        private void LateUpdate()
        {
            // 인디케이터가 해적 위치 따라가도록 (parent 안 시킴 — 부모 scale 영향 회피)
            if (_chaseIndicator != null)
            {
                var p = transform.position;
                _chaseIndicator.transform.position = new Vector3(p.x, p.y - 2.5f, p.z);
            }
        }

        private void Update()
        {
            if (definition == null || _engagementOver || LockMovement) return;
            UpdateMovement();
            if (definition.type == NpcType.Pirate) TryAutoEngage();
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

            // 0) 육지 회피 — 충돌 직후 일정 시간 deflect 방향 유지 (route 무시)
            if (Time.time < _deflectUntil)
            {
                desiredDir = _deflectDir;
                speed = wanderSpeed;
            }
            // 1) 해적 추적 / 상선 도주
            else if (definition.type == NpcType.Pirate && playerDist <= pirateChaseRange && playerDist > 1f)
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

            // 2) 이동 + 육지 충돌 검사 — 막히면 수직 방향으로 미끄러져 우회
            var nextPos = transform.position + desiredDir * speed * Time.deltaTime;
            if (IsLandAt(nextPos))
            {
                ChooseDeflectDirection(desiredDir, speed);
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

            // 1) 목표 항구가 설정돼 있으면 NavRouter 가 만든 waypoint 들을 순서대로 따라감
            if (_sailingTarget != null)
            {
                // waypoint 미설정 시 안전장치
                if (_waypoints == null || _waypoints.Count == 0) RebuildWaypoints();

                // 현재 waypoint 도착 처리
                while (_waypoints != null && _waypointIdx < _waypoints.Count)
                {
                    Vector3 wpPos = _waypoints[_waypointIdx];
                    var wpOffset = wpPos - transform.position;
                    wpOffset.y = 0f;
                    bool isLastWaypoint = (_waypointIdx == _waypoints.Count - 1);
                    // 게이트웨이는 살짝 큰 도착 반경, 최종 항구는 homeArriveDistance
                    float arriveDist = isLastWaypoint ? homeArriveDistance : (homeArriveDistance * 1.5f);

                    if (wpOffset.magnitude > arriveDist)
                    {
                        // 데드라인 — 너무 오래 걸리면 다음 waypoint 로 텔레포트
                        if (_returnStartedAt > 0f && Time.time - _returnStartedAt > homeReturnTimeoutSeconds)
                        {
                            transform.position = new Vector3(wpPos.x, transform.position.y, wpPos.z);
                            _waypointIdx++;
                            _returnStartedAt = Time.time;   // 다음 구간 데드라인 리셋
                            continue;
                        }

                        speed = (definition.type == NpcType.Merchant) ? tradeCruiseSpeed : wanderSpeed;
                        return wpOffset.normalized;
                    }

                    // 이 waypoint 도착 — 다음으로
                    _waypointIdx++;
                    _returnStartedAt = Time.time;   // 구간별 데드라인 리셋
                    if (isLastWaypoint)
                    {
                        EnterDwell(_sailingTarget);
                        speed = 0f;
                        return Vector3.zero;
                    }
                }
            }

            // 2) wander 시간 끝나면 자동으로 본거지로 복귀 전환 (해적·호위선)
            if (_wanderEndsAt > 0f && Time.time >= _wanderEndsAt && definition.homePort != null)
            {
                _sailingTarget = definition.homePort;
                _wanderEndsAt = 0f;
                _returnStartedAt = Time.time;
                RebuildWaypoints();   // 현재 위치 기준 새 경로 계산
                // 다음 프레임에 위 (1) 분기로 들어감
            }

            // 3) wander — patrolRange 벗어났으면 안전장치 복귀
            if (definition.homePort != null && definition.patrolRange > 0f)
            {
                var homePos = GeoCoordinate.LatLngToWorld(
                    definition.homePort.latitude, definition.homePort.longitude);
                var toHome = homePos - transform.position;
                toHome.y = 0f;
                if (toHome.magnitude > definition.patrolRange)
                {
                    return toHome.normalized;
                }
            }

            // 4) random walk
            if (Time.time >= _nextDirectionChange) PickNewWanderDirection();
            return _wanderDirection;
        }

        /// <summary>지정 항구 도착 → 항구로 들어감. GameObject 파괴 + NpcSpawner 광장 풀에 등록.
        /// 광장에서의 dwell·재출항은 NpcSpawner 가 관리.</summary>
        private void EnterDwell(PortData port)
        {
            if (_engagementOver) return;
            _engagementOver = true;
            _returnStartedAt = 0f;
            _sailingTarget = null;

            NpcSpawner.Instance?.SendNpcToPort(definition, 0f, port);
            Destroy(gameObject);
        }

        private void PickNewWanderDirection()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            _wanderDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            _nextDirectionChange = Time.time + directionChangeInterval + Random.Range(-1f, 1f);
        }

        private bool IsLandAt(Vector3 worldPos)
        {
            // 항해 카브 (지브롤터 등) — ShipController 와 동일 처리, NPC 도 통과 가능
            if (WorldCarves.IsInOpenArea(worldPos)) return false;

            int count = Physics.OverlapSphereNonAlloc(worldPos, landCheckRadius, _landBuffer);
            for (int i = 0; i < count; i++)
            {
                if (_landBuffer[i] == null) continue;
                if (_landBuffer[i].GetComponentInParent<Landmass>() != null) return true;
            }
            return false;
        }

        /// <summary>
        /// 막힌 desiredDir 에 대해 수직 방향(좌/우) 중 바다 쪽을 골라
        /// _deflectDir 로 설정. 1.5초간 route 무시하고 옆으로 미끄러짐.
        /// 양쪽 다 막혔으면 후진.
        /// </summary>
        private void ChooseDeflectDirection(Vector3 desiredDir, float speed)
        {
            var perpLeft = new Vector3(-desiredDir.z, 0f, desiredDir.x);
            var perpRight = new Vector3(desiredDir.z, 0f, -desiredDir.x);
            float step = Mathf.Max(speed * Time.deltaTime * 4f, landCheckRadius * 1.5f);
            var testLeft = transform.position + perpLeft * step;
            var testRight = transform.position + perpRight * step;

            if (!IsLandAt(testLeft) && !IsLandAt(testRight))
                _deflectDir = (Random.value < 0.5f) ? perpLeft : perpRight;
            else if (!IsLandAt(testLeft))
                _deflectDir = perpLeft;
            else if (!IsLandAt(testRight))
                _deflectDir = perpRight;
            else
                _deflectDir = -desiredDir;   // 양쪽 다 막힘 — 후진

            _deflectUntil = Time.time + 1.5f;
        }

        // ─── 해적 자동 전투 ──────────────────────────────────────────────

        private void CreateChaseIndicator()
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = $"ChaseRange_{name}";
            disc.transform.SetParent(transform.parent, worldPositionStays: true);
            var p = transform.position;
            disc.transform.position = new Vector3(p.x, p.y - 2.5f, p.z);
            float dia = pirateChaseRange * 2f;
            // Cylinder primitive 메시 높이=2, 직경=1 → world scale 그대로 (dia, height/2, dia)
            disc.transform.localScale = new Vector3(dia, 0.05f, dia);

            // Collider 제거 — 클릭/이동 방해 X
            var col = disc.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var renderer = disc.GetComponent<Renderer>();
            if (renderer != null)
            {
                // 불투명 — URP/Lit 기본(opaque) 으로 단색 빨강
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                var color = new Color(0.9f, 0.15f, 0.15f, 1f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                renderer.material = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            _chaseIndicator = disc;
        }

        private void TryAutoEngage()
        {
            if (Time.time < _nextEngageTime) return;

            // 1) 플레이어 우선
            if (_playerShip != null)
            {
                var d = _playerShip.transform.position - transform.position;
                d.y = 0f;
                if (d.magnitude <= pirateChaseRange)
                {
                    EngagePlayer();
                    return;
                }
            }

            // 2) 비-해적 NPC 탐색 (가장 가까운 1명)
            var spawner = NpcSpawner.Instance;
            if (spawner == null) return;
            NpcShip closest = null;
            float closestDist = pirateChaseRange;
            foreach (var ship in spawner.AllSpawned)
            {
                if (ship == null || ship == this || ship._engagementOver) continue;
                if (ship.definition == null) continue;
                if (ship.definition.type == NpcType.Pirate) continue;   // 해적끼리는 안 싸움
                var dd = ship.transform.position - transform.position;
                dd.y = 0f;
                float dist = dd.magnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = ship;
                }
            }
            if (closest != null) EngageNpc(closest);
        }

        private void EngagePlayer()
        {
            // 쿨다운 먼저 — 시퀀스 도중 다시 trigger 방지
            _nextEngageTime = Time.time + engagementCooldownSeconds;
            StartPlayerCombat();
        }

        /// <summary>
        /// 플레이어 vs 이 NPC 전투 시작.
        /// CombatSequence 가 있으면 시각 시퀀스(포탄·hit flash), 없으면 즉시 결과.
        /// 클릭 트리거(OnPointerClick) 와 자동 트리거(EngagePlayer) 가 공유.
        /// </summary>
        private void StartPlayerCombat()
        {
            if (_playerShip == null) _playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);
            if (_playerShip == null) return;

            var sequence = CombatSequence.Instance;
            if (sequence == null)
            {
                var go = new GameObject("CombatSequence (auto)");
                sequence = go.AddComponent<CombatSequence>();
                Debug.LogWarning("[NpcShip] CombatSequence 컴포넌트가 씬에 없어 자동 생성. " +
                    "수동으로 GameObject 추가하면 인스펙터에서 튜닝 가능.");
            }
            if (sequence.IsBusy) return;

            // 2:1 / N:1 — 같은 영역(각 해적의 pirateChaseRange) 안에 있는 다른 해적도 함께 전투
            var attackers = new List<NpcShip> { this };
            var spawner = NpcSpawner.Instance;
            if (spawner != null)
            {
                Vector3 pp = _playerShip.transform.position;
                foreach (var other in spawner.AllSpawned)
                {
                    if (other == null || other == this || other._engagementOver) continue;
                    if (other.LockMovement) continue;
                    if (other.definition == null || other.definition.type != NpcType.Pirate) continue;
                    var d = pp - other.transform.position;
                    d.y = 0f;
                    if (d.magnitude <= other.pirateChaseRange)
                    {
                        attackers.Add(other);
                    }
                }
            }
            sequence.Begin(_playerShip, attackers, resultPanel);
        }

        private void EngageNpc(NpcShip target)
        {
            int myBravery = (definition.character != null) ? definition.character.bravery : 50;
            int theirBravery = (target.definition.character != null) ? target.definition.character.bravery : 50;
            int myRoll = myBravery + Random.Range(0, 30);
            int theirRoll = theirBravery + Random.Range(0, 30);

            NpcShip loser = (myRoll >= theirRoll) ? target : this;
            loser._engagementOver = true;
            NpcSpawner.Instance?.OnNpcDefeated(loser.definition);
            Destroy(loser.gameObject, 0.5f);

            _nextEngageTime = Time.time + engagementCooldownSeconds;
            if (target != null) target._nextEngageTime = Time.time + engagementCooldownSeconds;
            Game.Save.SaveService.Instance?.SaveGame();
        }

        // ─── 클릭 → 전투 ─────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (definition == null || _engagementOver || LockMovement) return;
            StartPlayerCombat();
        }
    }
}
