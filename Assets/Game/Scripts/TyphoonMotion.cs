using UnityEngine;

// 태풍(토네이도) 애니메이션. Typhoon 프리팹에 붙이세요.
// 세로축 기준으로 빙글 회전하고, 천천히 떠다니며, 살짝 흔들립니다.
// (배 등 다른 오브젝트는 건드리지 않음 — 시각 효과 전용)
[DisallowMultipleComponent]
public class TyphoonMotion : MonoBehaviour
{
    [Header("회전 (세로축)")]
    public float spinSpeed = 140f;          // 도/초, 소용돌이 회전

    [Header("떠다니기 (선택)")]
    public bool drift = true;
    public float driftSpeed = 4f;           // 이동 속도(월드 단위/초)
    [Tooltip("이동 방향(도). 비활성 시 무시")]
    public float driftDirection = 45f;
    [Tooltip("이 반경 안에서 원점 주변을 배회. 0이면 직진")]
    public float wanderRadius = 0f;

    [Header("흔들림 / 맥동")]
    public float swayAmount = 1.5f;         // 좌우 살랑임 폭
    public float swaySpeed = 0.6f;
    [Tooltip("크기가 미세하게 커졌다 작아짐")]
    public float pulseAmount = 0.04f;
    public float pulseSpeed = 0.8f;

    Vector3 _origin;
    Vector3 _baseScale;
    float _t;

    void Start()
    {
        _origin = transform.position;
        _baseScale = transform.localScale;
    }

    void Update()
    {
        _t += Time.deltaTime;

        // 세로축 회전
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        // 이동
        if (drift && driftSpeed > 0f)
        {
            if (wanderRadius > 0f)
            {
                // 원점 주변을 원형 배회
                float ang = _t * driftSpeed / Mathf.Max(0.1f, wanderRadius);
                Vector3 p = _origin + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * wanderRadius;
                p.y = _origin.y;
                transform.position = p;
            }
            else
            {
                float rad = driftDirection * Mathf.Deg2Rad;
                transform.position += new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * driftSpeed * Time.deltaTime;
            }
        }

        // 살짝 흔들림(상단이 휘청이는 느낌은 회전+살랑으로 표현)
        if (swayAmount > 0f)
        {
            float sx = Mathf.Sin(_t * swaySpeed) * swayAmount;
            float sz = Mathf.Cos(_t * swaySpeed * 1.3f) * swayAmount * 0.6f;
            // 위치에 더하지 않고 기울기로 표현(밑동은 고정, 위가 휘청)
            transform.rotation = Quaternion.Euler(sx, transform.rotation.eulerAngles.y, sz) ;
        }

        // 맥동(크기)
        if (pulseAmount > 0f)
        {
            float s = 1f + Mathf.Sin(_t * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
            transform.localScale = new Vector3(_baseScale.x * s, _baseScale.y, _baseScale.z * s);
        }
    }
}
