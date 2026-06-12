using UnityEngine;

// 지형 위에 나무/바위/풀을 자동으로 흩뿌려 맵을 채웁니다.
// 사용법:
//  1) 빈 GameObject 생성 → 이 스크립트 Add Component
//  2) Prefabs 칸에 뿌릴 프리팹들(나무/바위/덤불, 또는 받아둔 에셋)을 넣기
//  3) 컴포넌트 우클릭(⋮) → "Scatter Now" (지우려면 "Clear")
//  ※ 지형(육지)에 Collider가 있어야 합니다. 메쉬 지형이면 Mesh Collider 추가.
public class NatureScatter : MonoBehaviour
{
    [Header("뿌릴 프리팹 (나무/바위/풀)")]
    public GameObject[] prefabs;

    [Header("범위 / 개수")]
    public int count = 300;
    public float areaSize = 200f;       // 이 오브젝트 중심으로 한 변 길이(맵 크기에 맞추기)
    public float raycastHeight = 500f;  // 위에서 아래로 쏘는 시작 높이

    [Header("놓을 조건")]
    public float minHeight = 0f;        // 이 높이(보통 수면) 아래엔 안 놓음 → 바다에 안 생김
    public float maxHeight = 9999f;     // 이 높이 위엔 안 놓음(예: 산꼭대기 제외)
    public float maxSlope = 35f;        // 이 경사보다 가파르면 안 놓음(도)
    public LayerMask groundMask = ~0;   // 지형 레이어(기본: 전부)

    [Header("배치 변화")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.3f);
    public bool randomYaw = true;
    public bool alignToSlope = false;   // 경사면 기울기에 맞춰 세우기

    const string CONTAINER = "_ScatteredNature";

    [ContextMenu("Scatter Now")]
    public void Scatter()
    {
        if (prefabs == null || prefabs.Length == 0)
        { Debug.LogWarning("NatureScatter: Prefabs가 비어 있어요. 뿌릴 프리팹을 넣어주세요."); return; }

        Clear();
        var container = new GameObject(CONTAINER);
        container.transform.SetParent(transform, false);
        container.transform.localPosition = Vector3.zero;

        int placed = 0, tries = 0, maxTries = count * 30;
        while (placed < count && tries < maxTries)
        {
            tries++;
            float x = transform.position.x + Random.Range(-areaSize / 2f, areaSize / 2f);
            float z = transform.position.z + Random.Range(-areaSize / 2f, areaSize / 2f);
            Vector3 from = new Vector3(x, transform.position.y + raycastHeight, z);

            if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundMask)) continue;
            if (hit.point.y < minHeight || hit.point.y > maxHeight) continue;
            if (Vector3.Angle(hit.normal, Vector3.up) > maxSlope) continue;

            var prefab = prefabs[Random.Range(0, prefabs.Length)];
            if (prefab == null) continue;

            var obj = Instantiate(prefab, hit.point, Quaternion.identity, container.transform);
            Quaternion rot = Quaternion.Euler(0f, randomYaw ? Random.Range(0f, 360f) : 0f, 0f);
            if (alignToSlope) rot = Quaternion.FromToRotation(Vector3.up, hit.normal) * rot;
            obj.transform.rotation = rot;
            obj.transform.localScale = prefab.transform.localScale * Random.Range(scaleRange.x, scaleRange.y);
            placed++;
        }
        Debug.Log("NatureScatter: " + placed + "개 배치 완료 (시도 " + tries + "). 부족하면 count/areaSize/조건을 조정하세요.");
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        var existing = transform.Find(CONTAINER);
        if (existing != null) DestroyImmediate(existing.gameObject);
    }

    // 범위를 씬에서 노란 사각형으로 표시
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize, 0.1f, areaSize));
    }
}
