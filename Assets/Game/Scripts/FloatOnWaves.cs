using UnityEngine;

// 배가 파도 위에 떠서 출렁이게 합니다. (높이 + 앞뒤/좌우 기울기만 담당)
// 배의 "방향(yaw)"은 건드리지 않고 현재 향하는 방향을 그대로 존중하므로,
// 조종/이동 스크립트가 배를 돌리면 그 방향대로 갑니다.
public class FloatOnWaves : MonoBehaviour
{
    [Header("바다 연결 (비우면 자동 검색)")]
    public OceanWaves ocean;

    [Header("뜨는 정도")]
    public float floatOffset = 0f;     // 수면 기준 오프셋(- 값이면 살짝 잠김)
    public float sampleLength = 5f;    // 앞뒤 기울기 감지 거리(배 길이의 절반쯤)
    public float sampleWidth  = 1.5f;  // 좌우 기울기 감지 거리(배 폭의 절반쯤)

    [Header("부드러움")]
    public float moveSmooth = 4f;
    public float rotateSmooth = 4f;

    void Start()
    {
        if (ocean == null) ocean = FindObjectOfType<OceanWaves>();
        if (ocean == null) Debug.LogWarning("FloatOnWaves: 씬에서 OceanWaves(바다)를 찾지 못했습니다.");
    }

    void LateUpdate()
    {
        if (ocean == null) return;
        Vector3 pos = transform.position;

        // 현재 향하는 방향(수평)을 그대로 사용 — 조종이 돌린 방향을 존중
        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
        fwd.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, fwd);

        float hC = ocean.GetHeight(pos.x, pos.z);
        float hF = ocean.GetHeight(pos.x + fwd.x * sampleLength, pos.z + fwd.z * sampleLength);
        float hB = ocean.GetHeight(pos.x - fwd.x * sampleLength, pos.z - fwd.z * sampleLength);
        float hR = ocean.GetHeight(pos.x + right.x * sampleWidth, pos.z + right.z * sampleWidth);
        float hL = ocean.GetHeight(pos.x - right.x * sampleWidth, pos.z - right.z * sampleWidth);

        Vector3 targetPos = new Vector3(pos.x, hC + floatOffset, pos.z);

        Vector3 pF = new Vector3(fwd.x * sampleLength, hF, fwd.z * sampleLength);
        Vector3 pB = new Vector3(-fwd.x * sampleLength, hB, -fwd.z * sampleLength);
        Vector3 pR = new Vector3(right.x * sampleWidth, hR, right.z * sampleWidth);
        Vector3 pL = new Vector3(-right.x * sampleWidth, hL, -right.z * sampleWidth);
        Vector3 normal = Vector3.Cross(pF - pB, pR - pL).normalized;
        if (normal.y < 0) normal = -normal;

        // 현재 방향을 수면 경사에 맞춰 살짝 기울이기만 함 (yaw 유지)
        Vector3 flatForward = Vector3.ProjectOnPlane(fwd, normal).normalized;
        Quaternion targetRot = Quaternion.LookRotation(flatForward, normal);

        transform.position = Vector3.Lerp(pos, targetPos, Time.deltaTime * moveSmooth);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSmooth);
    }
}
