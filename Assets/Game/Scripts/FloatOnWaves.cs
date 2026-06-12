using UnityEngine;

// 배가 파도 위에 떠서 출렁이게 합니다.
// 사용법: 배 오브젝트(Geobukseon, Clipper 등)에 이 스크립트를 Add Component 하세요.
// 같은 씬의 OceanWaves(바다)를 자동으로 찾아, 그 파도 높이에 맞춰 뜨고 기울어집니다.
public class FloatOnWaves : MonoBehaviour
{
    [Header("바다 연결 (비우면 자동 검색)")]
    public OceanWaves ocean;

    [Header("뜨는 정도")]
    public float floatOffset = 0f;     // 수면 기준 오프셋(- 값이면 살짝 잠김)
    public float sampleLength = 5f;    // 앞뒤 기울기 감지 거리(배 길이의 절반쯤)
    public float sampleWidth  = 1.5f;  // 좌우 기울기 감지 거리(배 폭의 절반쯤)

    [Header("부드러움")]
    public float moveSmooth = 4f;      // 상하 따라가는 부드러움
    public float rotateSmooth = 4f;    // 기울기 따라가는 부드러움

    float headingYaw;

    void Start()
    {
        if (ocean == null) ocean = FindObjectOfType<OceanWaves>();
        headingYaw = transform.eulerAngles.y;
        if (ocean == null) Debug.LogWarning("FloatOnWaves: 씬에서 OceanWaves(바다)를 찾지 못했습니다.");
    }

    void Update()
    {
        if (ocean == null) return;
        Vector3 pos = transform.position;

        Quaternion yawRot = Quaternion.Euler(0, headingYaw, 0);
        Vector3 fwd = yawRot * Vector3.forward;
        Vector3 right = yawRot * Vector3.right;

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

        Vector3 flatForward = Vector3.ProjectOnPlane(fwd, normal).normalized;
        Quaternion targetRot = Quaternion.LookRotation(flatForward, normal);

        transform.position = Vector3.Lerp(pos, targetPos, Time.deltaTime * moveSmooth);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSmooth);
    }

    // 항해 방향을 바꿀 때 사용 (예: 배가 회전)
    public void SetHeading(float yawDegrees) { headingYaw = yawDegrees; }
}
