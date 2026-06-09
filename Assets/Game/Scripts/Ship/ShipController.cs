using Game.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Ship
{
    /// <summary>
    /// 플레이어 배 제어 — 터치 입력으로 조타 + 추진.
    ///
    /// 입력 (GAME_MECHANICS §8.2):
    ///   Steer    (Value Vector2)  좌/우 = x. 좌측 음수, 우측 양수.
    ///   Throttle (Value float)    위 = +1 (가속), 아래 = -1 (감속/정지).
    ///
    /// 어린이 친화:
    ///   - 약한 관성 (자연 감속 deceleration).
    ///   - 정지 버튼 누르면 천천히 멈춤 (돛 내림 연출).
    ///   - 육지 충돌은 데미지 X (충돌 처리는 별도 컴포넌트).
    ///
    /// 사용:
    ///   배 프리팹에 본 컴포넌트 추가.
    ///   Inspector 에서 ShipData (SO) + InputActionReference 2개 (Steer/Throttle) 할당.
    ///   Captain (CharacterData) 은 선택 — 항해 능력 보너스를 받기 위해.
    /// </summary>
    public class ShipController : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("현재 타고 있는 배. 능력치(speed, durability) 의 출처.")]
        public ShipData shipData;

        [Tooltip("선장 캐릭터. 항해 능력에 따라 속도 보너스. 비어 있으면 보너스 없음.")]
        public CharacterData captain;

        [Header("Movement Tuning")]
        [Tooltip("최대 회전 속도 (도/초). 어린이가 따라가기 쉬운 값.")]
        [Range(20f, 180f)] public float maxTurnRate = 60f;

        [Tooltip("가속도 (Unity Unit/초²). 클수록 빠르게 최고 속도 도달.")]
        [Range(0.5f, 10f)] public float acceleration = 2f;

        [Tooltip("자연 감속 (Unity Unit/초²) — Throttle 입력이 0일 때.")]
        [Range(0.1f, 5f)] public float passiveDeceleration = 0.5f;

        [Tooltip("적극 감속 — Throttle 입력이 음수일 때.")]
        [Range(0.5f, 10f)] public float activeDeceleration = 3f;

        [Header("Input Actions")]
        [Tooltip("좌/우 조타 — Vector2 액션 (x 값 사용).")]
        public InputActionReference steerAction;

        [Tooltip("가속/감속 — float 액션 (-1 ~ +1).")]
        public InputActionReference throttleAction;

        [Header("Collision")]
        [Tooltip("육지(Landmass) 와의 충돌 검사 반경 (Unity Unit). 배 너비 정도.")]
        [Range(0.5f, 10f)] public float collisionCheckRadius = 2f;

        [Tooltip("육지에 부딪힐 때 즉시 정지할지. 기획상 어린이 친화 — 데미지 X, 멈춤만.")]
        public bool blockMovementOnLand = true;

        // ─── 런타임 상태 ─────────────────────────────────────────────────────

        public float CurrentSpeed { get; private set; }
        public float ThrottleInput => _throttleValue;
        public float SteerInput => _steerValue;

        /// <summary>전투 중 등 외부에서 입력을 잠그고 싶을 때.
        /// true 가 되는 순간 속도·입력값 즉시 0 — 관성 없이 그 자리에 정지.</summary>
        public bool LockInput
        {
            get => _lockInput;
            set
            {
                _lockInput = value;
                if (value)
                {
                    _currentSpeed = 0f;
                    CurrentSpeed = 0f;
                    _steerValue = 0f;
                    _throttleValue = 0f;
                }
            }
        }
        private bool _lockInput;

        /// <summary>현재 내구도 — 전투 중 차감, 항구에서 수리. 0 이면 게임 패배.</summary>
        public int CurrentDurability { get; private set; }
        // 0 = 옛 에셋 미직렬화 → fallback 50. Range(10,200) 이라 정상 설정값은 >= 10
        public int MaxDurability => (shipData != null && shipData.maxDurability >= 10) ? shipData.maxDurability : 50;

        public void SetDurability(int value)
        {
            CurrentDurability = Mathf.Clamp(value, 0, MaxDurability);
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0) return;
            CurrentDurability = Mathf.Max(0, CurrentDurability - amount);
        }

        public void RestoreDurability()
        {
            CurrentDurability = MaxDurability;
        }

        private void Start()
        {
            // 첫 진입 시 내구도 초기화 (저장 데이터가 있으면 SaveService 가 SetDurability 로 덮어씀)
            if (CurrentDurability <= 0) CurrentDurability = MaxDurability;
            RefreshVisual();
        }

        /// <summary>
        /// 현재 shipData.prefab3D 를 자식 "ShipVisual" 로 인스턴스화. 이전 ShipVisual 제거.
        /// 동시에 PlayerShip GameObject 자체의 Renderer 또는 다른 자식 Renderer 들을 숨김
        /// (기존 큐브 시각과 중첩 방지). prefab3D 가 null 이면 원본 시각 다시 복구.
        /// </summary>
        public void RefreshVisual()
        {
            bool hasPrefab = shipData != null && shipData.prefab3D != null;

            // 기존 ShipVisual 제거
            var existing = transform.Find("ShipVisual");
            if (existing != null) Destroy(existing.gameObject);

            if (hasPrefab)
            {
                var visual = Instantiate(shipData.prefab3D, transform);
                visual.name = "ShipVisual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                foreach (var col in visual.GetComponentsInChildren<Collider>())
                {
                    col.enabled = false;
                }
                SetOriginalVisualsEnabled(false, visual);
            }
            else
            {
                // prefab3D 없는 ShipData 로 교체 시엔 원본(씬에 있던 큐브 등) 복구
                SetOriginalVisualsEnabled(true, null);
            }
        }

        /// <summary>
        /// 루트의 Renderer + ShipVisual 이외 자식 Renderer 들의 enabled 상태 설정.
        /// PlayerShip GameObject 가 직접 Cube 메쉬를 갖고 있거나, 별도 자식 시각이 있을 때
        /// prefab3D 와 중첩 안 되도록 자동 처리.
        /// </summary>
        private void SetOriginalVisualsEnabled(bool enabled, GameObject exclude)
        {
            var rootRenderer = GetComponent<Renderer>();
            if (rootRenderer != null) rootRenderer.enabled = enabled;

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (exclude != null && child.gameObject == exclude) continue;
                foreach (var r in child.GetComponentsInChildren<Renderer>())
                {
                    r.enabled = enabled;
                }
            }
        }

        private float _currentSpeed;
        private float _throttleValue;
        private float _steerValue;

        // ─── 라이프사이클 ────────────────────────────────────────────────────

        private void OnEnable()
        {
            EnableAction(steerAction);
            EnableAction(throttleAction);
        }

        private void OnDisable()
        {
            DisableAction(steerAction);
            DisableAction(throttleAction);
        }

        private void Update()
        {
            ReadInput();
            ApplyRotation();
            ApplyThrust();
        }

        // ─── 입력 ────────────────────────────────────────────────────────────

        private void ReadInput()
        {
            if (LockInput)
            {
                _steerValue = 0f;
                _throttleValue = 0f;
                return;
            }

            if (steerAction != null && steerAction.action != null)
            {
                _steerValue = steerAction.action.ReadValue<Vector2>().x;
            }

            if (throttleAction != null && throttleAction.action != null)
            {
                _throttleValue = throttleAction.action.ReadValue<float>();
            }
        }

        // ─── 회전 ────────────────────────────────────────────────────────────

        private void ApplyRotation()
        {
            // 정지 상태에서도 회전 가능 (어린이 조작 편의).
            float deltaYaw = _steerValue * maxTurnRate * Time.deltaTime;
            if (Mathf.Abs(deltaYaw) > 0.0001f)
            {
                transform.Rotate(0f, deltaYaw, 0f, Space.World);
            }
        }

        // ─── 추진 ────────────────────────────────────────────────────────────

        private void ApplyThrust()
        {
            float maxSpeed = ComputeMaxSpeed();

            if (_throttleValue > 0.01f)
            {
                // 가속 — 입력 강도에 비례한 목표 속도까지.
                float target = maxSpeed * _throttleValue;
                _currentSpeed = Mathf.MoveTowards(
                    _currentSpeed, target, acceleration * Time.deltaTime);
            }
            else if (_throttleValue < -0.01f)
            {
                // 적극 감속 — 0 까지 빠르게.
                _currentSpeed = Mathf.MoveTowards(
                    _currentSpeed, 0f, activeDeceleration * Time.deltaTime);
            }
            else
            {
                // 자연 감속 — 0 까지 천천히 (관성).
                _currentSpeed = Mathf.MoveTowards(
                    _currentSpeed, 0f, passiveDeceleration * Time.deltaTime);
            }

            CurrentSpeed = _currentSpeed;

            if (_currentSpeed > 0.0001f)
            {
                var moveDelta = transform.forward * (_currentSpeed * Time.deltaTime);
                var newPos = transform.position + moveDelta;

                if (blockMovementOnLand && IsLandAt(newPos))
                {
                    // 어린이 친화 — 데미지 없이 그냥 멈춤 (튕기지 않음)
                    _currentSpeed = 0f;
                    CurrentSpeed = 0f;
                    return;
                }

                transform.position = newPos;
            }
        }

        /// <summary>
        /// 주어진 월드 위치가 육지(Landmass) 안인지 검사.
        /// 우선 WorldCarves 의 "개방 영역" 안이면 항상 false (해협 강제 통과).
        /// 그렇지 않으면 Physics.OverlapSphere 결과에 Landmass 컴포넌트가 있으면 true.
        /// </summary>
        private static readonly Collider[] _overlapBuffer = new Collider[8];
        private bool IsLandAt(Vector3 worldPos)
        {
            // 항해 카브(지브롤터 등) — 메쉬가 어떻든 무조건 통과 가능
            if (World.WorldCarves.IsInOpenArea(worldPos)) return false;

            int count = Physics.OverlapSphereNonAlloc(worldPos, collisionCheckRadius, _overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                if (_overlapBuffer[i] == null) continue;
                if (_overlapBuffer[i].GetComponentInParent<World.Landmass>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 최대 속도 = ShipData.speed × 항해 능력 보정.
        /// 항해 능력 1   → 1.005배
        /// 항해 능력 100 → 1.500배
        /// (GAME_MECHANICS §1.1, §8.4 — 단순 곱셈 적용)
        /// </summary>
        private float ComputeMaxSpeed()
        {
            float baseSpeed = shipData != null ? shipData.speed : 5f;
            int seamanship = captain != null ? captain.seamanship : 50;
            float bonus = 1f + (Mathf.Clamp(seamanship, 1, 100) / 100f) * 0.5f;
            return baseSpeed * bonus;
        }

        // ─── 보조 ────────────────────────────────────────────────────────────

        private static void EnableAction(InputActionReference reference)
        {
            if (reference != null && reference.action != null)
            {
                reference.action.Enable();
            }
        }

        private static void DisableAction(InputActionReference reference)
        {
            if (reference != null && reference.action != null)
            {
                reference.action.Disable();
            }
        }

        // ─── 외부 API (다른 컴포넌트가 호출) ─────────────────────────────────

        /// <summary>강제 정지 (충돌 시 등). 부드럽게 0 까지 감속.</summary>
        public void HardStop()
        {
            _currentSpeed = 0f;
            CurrentSpeed = 0f;
        }

        /// <summary>현재 위치의 위/경도 (정박 및 탐색 등에 사용).</summary>
        public (float latitude, float longitude) GetCurrentLatLng()
        {
            return World.GeoCoordinate.WorldToLatLng(transform.position);
        }
    }
}
