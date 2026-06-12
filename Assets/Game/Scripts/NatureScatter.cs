using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 지형 위에 나무/바위/풀을 자연스럽게 흩뿌립니다.
// 편집 중: 컴포넌트 우클릭(⋮) → "Scatter Now" / "Clear"
// 실행 시 맵을 다시 만드는 게임이면: Scatter On Start 체크.
// ※ 지형(육지)에 Collider 필요(메쉬면 Mesh Collider).
public class NatureScatter : MonoBehaviour
{
    [Header("뿌릴 프리팹 (나무/바위/풀)")]
    public GameObject[] prefabs;

    [Header("범위 / 개수")]
    public int count = 300;
    public float areaSize = 200f;
    public float raycastHeight = 500f;

    [Header("놓을 조건")]
    public float minHeight = 0f;
    public float maxHeight = 9999f;
    public float maxSlope = 35f;
    public LayerMask groundMask = ~0;

    [Header("배치 변화")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.3f);
    public bool randomYaw = true;
    public bool alignToSlope = false;
    [Range(0f, 30f)] public float maxTilt = 8f;          // 랜덤 기울기(도) — 뻣뻣함 제거
    [Range(0f, 0.5f)] public float colorVariation = 0.15f; // 개체별 밝기 변화 — 똑같음 제거

    [Header("군집 (숲처럼 뭉치기)")]
    public bool useClusters = true;
    public int clusterCount = 14;       // 덩어리(숲) 개수
    public float clusterRadius = 22f;   // 한 덩어리 반경 (작을수록 빽빽)

    [Header("실행 시 자동 배치 (런타임 맵 생성용)")]
    public bool scatterOnStart = false;
    public float startDelay = 0.5f;

    const string CONTAINER = "_ScatteredNature";

    void Start() { if (scatterOnStart) StartCoroutine(ScatterDelayed()); }
    IEnumerator ScatterDelayed() { yield return new WaitForSeconds(startDelay); Scatter(); }

    [ContextMenu("Scatter Now")]
    public void Scatter()
    {
        if (prefabs == null || prefabs.Length == 0)
        { Debug.LogWarning("NatureScatter: Prefabs가 비어 있어요."); return; }

        Clear();
        var container = new GameObject(CONTAINER);
        container.transform.SetParent(transform, false);
        container.transform.localPosition = Vector3.zero;

        // 군집 중심 미리 만들기
        List<Vector2> centers = null;
        if (useClusters && clusterCount > 0)
        {
            centers = new List<Vector2>();
            for (int i = 0; i < clusterCount; i++)
                centers.Add(new Vector2(
                    transform.position.x + Random.Range(-areaSize / 2f, areaSize / 2f),
                    transform.position.z + Random.Range(-areaSize / 2f, areaSize / 2f)));
        }

        int placed = 0, tries = 0, maxTries = count * 40;
        while (placed < count && tries < maxTries)
        {
            tries++;
            float x, z;
            if (centers != null)
            {
                Vector2 c = centers[Random.Range(0, centers.Count)];
                Vector2 off = Random.insideUnitCircle * clusterRadius;
                x = c.x + off.x; z = c.y + off.y;
            }
            else
            {
                x = transform.position.x + Random.Range(-areaSize / 2f, areaSize / 2f);
                z = transform.position.z + Random.Range(-areaSize / 2f, areaSize / 2f);
            }

            Vector3 from = new Vector3(x, transform.position.y + raycastHeight, z);
            if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundMask)) continue;
            if (hit.point.y < minHeight || hit.point.y > maxHeight) continue;
            if (Vector3.Angle(hit.normal, Vector3.up) > maxSlope) continue;

            var prefab = prefabs[Random.Range(0, prefabs.Length)];
            if (prefab == null) continue;

            var obj = Instantiate(prefab, hit.point, Quaternion.identity, container.transform);

            Quaternion baseRot = alignToSlope ? Quaternion.FromToRotation(Vector3.up, hit.normal) : Quaternion.identity;
            Quaternion yaw = Quaternion.Euler(0f, randomYaw ? Random.Range(0f, 360f) : 0f, 0f);
            Quaternion tilt = Quaternion.Euler(Random.Range(-maxTilt, maxTilt), 0f, Random.Range(-maxTilt, maxTilt));
            obj.transform.rotation = baseRot * yaw * tilt;
            obj.transform.localScale = prefab.transform.localScale * Random.Range(scaleRange.x, scaleRange.y);

            if (colorVariation > 0f) ApplyTint(obj, 1f + Random.Range(-colorVariation, colorVariation));
            placed++;
        }
        Debug.Log("NatureScatter: " + placed + "개 배치 완료 (시도 " + tries + ").");
    }

    // 개체 전체를 같은 비율로 밝기 변화 (재질별 원래 색 유지)
    void ApplyTint(GameObject obj, float f)
    {
        foreach (var rend in obj.GetComponentsInChildren<Renderer>())
        {
            var mats = rend.sharedMaterials;
            for (int mi = 0; mi < mats.Length; mi++)
            {
                var mat = mats[mi];
                if (mat == null) continue;
                Color c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor")
                        : (mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white);
                Color v = new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), c.a);
                var mpb = new MaterialPropertyBlock();
                rend.GetPropertyBlock(mpb, mi);
                if (mat.HasProperty("_BaseColor")) mpb.SetColor("_BaseColor", v);
                if (mat.HasProperty("_Color")) mpb.SetColor("_Color", v);
                rend.SetPropertyBlock(mpb, mi);
            }
        }
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        var existing = transform.Find(CONTAINER);
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize, 0.1f, areaSize));
    }
}
