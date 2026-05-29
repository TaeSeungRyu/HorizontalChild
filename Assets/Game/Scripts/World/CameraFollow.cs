using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// M1 단순 카메라 추종 — Cinemachine 없이 LateUpdate 보간.
    ///
    /// 어린이 친화:
    ///   - 부드러운 따라가기 (smoothing)
    ///   - 약간 기울인 시점 (top-down 에 가깝지만 60~75°)
    ///   - 회전은 따라가지 않음 (방향 멀미 방지)
    ///
    /// 사용:
    ///   Main Camera 에 본 컴포넌트 추가, target 에 배 Transform 할당.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Offset")]
        [Tooltip("타깃 기준 카메라 오프셋 (월드 좌표).")]
        public Vector3 offset = new Vector3(0f, 30f, -15f);

        [Tooltip("카메라가 보는 각도. 0=수직 내려다봄, 90=정면.")]
        [Range(0f, 90f)] public float tiltAngle = 65f;

        [Header("Smoothing")]
        [Tooltip("위치 따라잡는 속도. 클수록 빠르게 추종.")]
        [Range(0.5f, 20f)] public float positionLerpSpeed = 5f;

        private void LateUpdate()
        {
            if (target == null) return;

            // 목표 위치 = 타깃 위치 + 오프셋
            Vector3 desiredPos = target.position + offset;
            transform.position = Vector3.Lerp(
                transform.position, desiredPos,
                positionLerpSpeed * Time.deltaTime);

            // 회전은 고정 — 타깃을 바라보되 yaw 는 따라가지 않음 (멀미 방지)
            transform.rotation = Quaternion.Euler(tiltAngle, 0f, 0f);
        }

        private void OnValidate()
        {
            // 에디터에서 회전 미리보기
            transform.rotation = Quaternion.Euler(tiltAngle, 0f, 0f);
        }
    }
}
