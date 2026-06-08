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
    /// M3.5 Phase 1: 이동 AI 추가
    ///   - 기본: random walk (방향 주기적으로 변경)
    ///   - 해적: 플레이어 일정 거리 안 들어오면 추적 가속
    ///   - 상선: 플레이어 가까이 오면 도주
    ///   - 호위선: 중립 random walk (해적 추격은 추후 폴리시)
    ///   - 세계 경계 안에서만 (간단 clamp), 육지 충돌 시 방향 반전
    ///
    /// 클릭 처리: Physics Raycaster + IPointerClickHandler. Discovery Marker 와 동일 패턴.
    /// </summary>
    public class NpcShip : MonoBehaviour, IPointerClickHandler
    {
        public NpcDefinition definition;
        public CombatResultPanel resultPanel;

        [Header("AI — 공통")]
        [Tooltip("기본 random walk 속도 (unit/sec). 1 unit ≈ 7.4 km.")]
        public float wanderSpeed = 4f;

        [Tooltip("방향 바꾸는 주기 (초). 약간의 랜덤 ± 적용.")]
        public float directionChangeInterval = 4f;

        [Header("AI — 타입별")]
        [Tooltip("해적이 플레이어를 추적하기 시작하는 거리.")]
        public float pirateChaseRange = 80f;
        [Tooltip("해적 추적 속도.")]
        public float pirateChaseSpeed = 7f;

        [Tooltip("상선이 플레이어를 발견하고 도주하기 시작하는 거리.")]
        public float merchantFleeRange = 60f;
        [Tooltip("상선 도주 속도.")]
        public float merchantFleeSpeed = 6f;

        [Header("World Bounds")]
        public float worldHalfWidth = 2700f;
        public float worldHalfDepth = 1350f;

        [Header("Land Avoidance")]
        [Tooltip("육지 충돌 검사 반경 (unit).")]
        public float landCheckRadius = 3f;

        // 런타임
        private Vector3 _wanderDirection;
        private float _nextDirectionChange;
        private ShipController _playerShip;
        private static readonly Collider[] _landBuffer = new Collider[8];

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

            // 1) 타입별 행동 결정
            if (_playerShip != null)
            {
                var toPlayer = _playerShip.transform.position - transform.position;
                toPlayer.y = 0f;
                float dist = toPlayer.magnitude;

                if (definition.type == NpcType.Pirate && dist <= pirateChaseRange && dist > 1f)
                {
                    desiredDir = toPlayer.normalized;
                    speed = pirateChaseSpeed;
                }
                else if (definition.type == NpcType.Merchant && dist <= merchantFleeRange && dist > 1f)
                {
                    desiredDir = -toPlayer.normalized;
                    speed = merchantFleeSpeed;
                }
            }

            // 2) 추적·도주 아니면 wander
            if (desiredDir.sqrMagnitude < 0.01f)
            {
                if (Time.time >= _nextDirectionChange) PickNewWanderDirection();
                desiredDir = _wanderDirection;
            }

            // 3) 이동 적용 — 육지 충돌 검사
            var nextPos = transform.position + desiredDir * speed * Time.deltaTime;
            if (IsLandAt(nextPos))
            {
                // 충돌 — 방향 반전 + 다음 방향 재추첨
                _wanderDirection = -desiredDir;
                _nextDirectionChange = Time.time + 1f;
                return;
            }

            // 4) 세계 경계 clamp (Wrapping 아님)
            nextPos.x = Mathf.Clamp(nextPos.x, -worldHalfWidth, worldHalfWidth);
            nextPos.z = Mathf.Clamp(nextPos.z, -worldHalfDepth, worldHalfDepth);

            // 경계 끝에 닿으면 새 방향
            if (Mathf.Abs(nextPos.x) >= worldHalfWidth - 0.1f ||
                Mathf.Abs(nextPos.z) >= worldHalfDepth - 0.1f)
            {
                PickNewWanderDirection();
            }

            transform.position = nextPos;

            // 5) 진행 방향 바라보기 (큐브 회전)
            if (desiredDir.sqrMagnitude > 0.01f)
            {
                var lookRot = Quaternion.LookRotation(new Vector3(desiredDir.x, 0f, desiredDir.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 3f * Time.deltaTime);
            }
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
