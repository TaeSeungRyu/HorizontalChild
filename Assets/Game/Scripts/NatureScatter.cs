using System.Collections;
using UnityEngine;

// 지형 위에 나무/바위/풀을 자동으로 흩뿌려 맵을 채웁니다.
// 편집 중: 컴포넌트 우클릭(⋮) → "Scatter Now" / "Clear"
// 실행 시 맵을 다시 만드는 게임이면: Scatter On Start 를 켜세요(맵 생성 후 자동 배치).
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

    [Header("실행 시 자동 배치 (런타임에 맵을 새로 만드는 게임용)")]
    public bool scatterOnStart = false;
    public float startDelay = 0.5f;   // 맵이 다 만들어질 때까지 기다리는 시간(초)

    const string CONTAINER = "_ScatteredNature";

    void Start()
    {
        if (scatterOnStart) StartCoroutine(ScatterDelayed());
    }

    IEnumerator ScatterDelayed()
    {
        yield return new WaitForSeconds(startDelay);
        Scatter();
    }

    [ContextMenu("Scatter Now")]
    public void Scatter()
    {
        if (prefabs == null || prefabs.Length == 0)
        { Debug.LogWarning("NatureScatter: Prefabs가 비어 있어요."); return; }

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
        Debug.Log("NatureScatter: " + placed + "개 배치 완료 (시도 " + tries + ").");
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
