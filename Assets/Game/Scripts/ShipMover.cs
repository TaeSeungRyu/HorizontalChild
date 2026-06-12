using UnityEngine;

// 간단한 배 조종 (이동 스크립트가 따로 없을 때만 사용하세요).
// W/S(↑/↓) = 바라보는 방향으로 전진/후진,  A/D(←/→) = 좌우로 방향 전환.
// 위치(X/Z)와 방향(Yaw)만 바꾸고, 높이·기울기·출렁임은 FloatOnWaves가 맡습니다.
public class ShipMover : MonoBehaviour
{
    public float moveSpeed = 8f;    // 전진 속도
    public float turnSpeed = 50f;   // 회전 속도(도/초)

    void Update()
    {
        float move = Input.GetAxis("Vertical");    // W/S
        float turn = Input.GetAxis("Horizontal");  // A/D

        Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
        transform.position += fwd * move * moveSpeed * Time.deltaTime;
        transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f, Space.World);
    }
}
