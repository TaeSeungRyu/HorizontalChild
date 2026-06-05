using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Game.World
{
    /// <summary>
    /// M1 단순 카메라 추종 — Cinemachine 없이 LateUpdate 보간.
    /// M3 에 줌 인/아웃 추가 — 핀치 (모바일) + 마우스 스크롤 (에디터).
    ///
    /// 어린이 친화:
    ///   - 부드러운 따라가기 (smoothing)
    ///   - 약간 기울인 시점 (top-down 에 가깝지만 60~75°)
    ///   - 회전은 따라가지 않음 (방향 멀미 방지)
    ///   - 줌 변화도 부드럽게 보간 (멀미 방지)
    ///
    /// 사용:
    ///   Main Camera 에 본 컴포넌트 추가, target 에 배 Transform 할당.
    ///   줌 +/- 버튼이 필요하면 외부에서 ZoomIn() / ZoomOut() 호출.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Offset")]
        [Tooltip("타깃 기준 카메라 오프셋 (월드 좌표). 줌 1.0 기준.")]
        public Vector3 offset = new Vector3(0f, 30f, -15f);

        [Tooltip("카메라가 보는 각도. 0=수직 내려다봄, 90=정면.")]
        [Range(0f, 90f)] public float tiltAngle = 65f;

        [Header("Smoothing")]
        [Tooltip("위치 따라잡는 속도. 클수록 빠르게 추종.")]
        [Range(0.5f, 20f)] public float positionLerpSpeed = 5f;

        [Tooltip("회전 따라잡는 속도. 클수록 빠르게 추종 (멀미 우려 시 작게).")]
        [Range(0.5f, 20f)] public float rotationLerpSpeed = 4f;

        [Header("Rotation Tracking")]
        [Tooltip("배가 회전할 때 카메라도 따라 회전 — 어린이가 항구·발견물 방향 잡기 쉬움.\n끄면 북쪽 고정 (멀미 우려 시).")]
        public bool followYaw = true;

        [Header("Zoom")]
        [Tooltip("줌 활성. 끄면 항상 1.0 (오프셋 그대로).")]
        public bool enableZoom = true;

        [Tooltip("줌 최소값 — 가장 가까이. 작을수록 클로즈업.")]
        [Range(0.3f, 1f)] public float minZoom = 0.5f;

        [Tooltip("줌 최대값 — 가장 멀리.")]
        [Range(1f, 5f)] public float maxZoom = 3f;

        [Tooltip("줌 변화 보간 속도. 클수록 빠르게 적용.")]
        [Range(1f, 20f)] public float zoomLerpSpeed = 6f;

        [Tooltip("핀치 1px 당 줌 변화량 (모바일).")]
        [Range(0.001f, 0.02f)] public float pinchSensitivity = 0.005f;

        [Tooltip("마우스 스크롤 1단위당 줌 변화량 (에디터/PC).")]
        [Range(0.05f, 1f)] public float scrollSensitivity = 0.2f;

        [Tooltip("외부 버튼(ZoomIn/Out) 한 번 호출당 줌 변화량.")]
        [Range(0.05f, 1f)] public float buttonStep = 0.25f;

        // ─── 런타임 상태 ───────────────────────────────────────────────────
        private float _currentZoom = 1f;
        private float _targetZoom = 1f;
        private float _pinchPrevDistance = 0f;

        private void Update()
        {
            if (!enableZoom) return;
            HandleZoomInput();
            _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, zoomLerpSpeed * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // 줌 = 1.0 일 때 원래 offset, 작으면 가까이 / 크면 멀리
            Vector3 scaledOffset = offset * (enableZoom ? _currentZoom : 1f);

            // 목표 위치 — 타깃 기준 오프셋을 타깃의 회전에 맞춰 적용 (followYaw 켤 때만)
            Vector3 worldOffset = followYaw
                ? target.rotation * scaledOffset
                : scaledOffset;
            Vector3 desiredPos = target.position + worldOffset;
            transform.position = Vector3.Lerp(
                transform.position, desiredPos,
                positionLerpSpeed * Time.deltaTime);

            // 목표 회전 — 줌 영향 없음 (각도는 고정)
            float yaw = followYaw ? target.eulerAngles.y : 0f;
            Quaternion desiredRot = Quaternion.Euler(tiltAngle, yaw, 0f);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, desiredRot,
                rotationLerpSpeed * Time.deltaTime);
        }

        private void OnValidate()
        {
            // 에디터에서 기울기 미리보기 (yaw 는 런타임에 결정)
            transform.rotation = Quaternion.Euler(tiltAngle, transform.eulerAngles.y, 0f);
        }

        // ─── 줌 입력 처리 ───────────────────────────────────────────────────

        private void HandleZoomInput()
        {
            // 1) 마우스 스크롤 (에디터/PC)
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scrollY = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > 0.01f)
                {
                    // scroll 위쪽 = 줌 인 (가까이) = 줌 값 감소
                    _targetZoom = Mathf.Clamp(
                        _targetZoom - Mathf.Sign(scrollY) * scrollSensitivity,
                        minZoom, maxZoom);
                }
            }

            // 2) 핀치 (멀티터치, 모바일)
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                int activeTouches = 0;
                TouchControl t0 = null, t1 = null;
                foreach (var t in touchscreen.touches)
                {
                    if (t.press.isPressed)
                    {
                        if (activeTouches == 0) t0 = t;
                        else if (activeTouches == 1) t1 = t;
                        activeTouches++;
                        if (activeTouches >= 2) break;
                    }
                }

                if (activeTouches >= 2 && t0 != null && t1 != null)
                {
                    float currentDist = Vector2.Distance(
                        t0.position.ReadValue(),
                        t1.position.ReadValue());

                    if (_pinchPrevDistance > 0f)
                    {
                        float delta = currentDist - _pinchPrevDistance;
                        // 손가락 벌리면(거리 늘면) 줌 인 (가까이) — 값 감소
                        _targetZoom = Mathf.Clamp(
                            _targetZoom - delta * pinchSensitivity,
                            minZoom, maxZoom);
                    }
                    _pinchPrevDistance = currentDist;
                }
                else
                {
                    _pinchPrevDistance = 0f;
                }
            }
        }

        // ─── 외부 API (HUD 버튼 등에서 호출) ────────────────────────────────

        /// <summary>줌 인 (가까이 보기). HUD + 버튼 등에서 호출.</summary>
        public void ZoomIn()
        {
            _targetZoom = Mathf.Clamp(_targetZoom - buttonStep, minZoom, maxZoom);
        }

        /// <summary>줌 아웃 (멀리 보기). HUD − 버튼 등에서 호출.</summary>
        public void ZoomOut()
        {
            _targetZoom = Mathf.Clamp(_targetZoom + buttonStep, minZoom, maxZoom);
        }

        /// <summary>줌 절대값 설정 (0.5~3.0). 슬라이더 등에서 호출.</summary>
        public void SetZoom(float zoom)
        {
            _targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        }

        /// <summary>현재 줌 값 조회.</summary>
        public float CurrentZoom => _currentZoom;
    }
}
