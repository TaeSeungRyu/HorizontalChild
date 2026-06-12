using System.Collections.Generic;
using UnityEngine;

// 플레이어 주변에만 자연물을 "청크(구역)" 단위로 생성하고, 멀어지면 지웁니다.
// 맵이 아무리 커도, 어디로 가도 가볍게 자연물이 깔립니다. (맵 크기 몰라도 됨)
// 사용: 빈 GameObject에 이 스크립트 추가 → Target에 플레이어 드래그(비우면 Main Camera)
//      → Prefabs 채우기 → Play. ※ 지형(육지)에 Collider 필요.
public class NatureStreamer : MonoBehaviour
{
    [Header("따라다닐 대상 (비우면 Main Camera)")]
    public Transform target;

    [Header("뿌릴 프리팹 (나무/바위/꽃 등)")]
    public GameObject[] prefabs;

    [Header("청크 / 범위")]
    public float chunkSize = 50f;     // 한 구역 크기
    public int viewRadius = 3;        // 주변 몇 구역까지 채울지(클수록 멀리 보이나 무거움)
    public int perChunk = 40;         // 한 구역당 개수

    [Header("놓을 조건")]
    public float minHeight = 0f;      // 보통 수면 높이(아래엔 안 놓음)
    public float maxHeight = 9999f;
    public float maxSlope = 35f;
    public LayerMask groundMask = ~0;
    public float raycastHeight = 500f;

    [Header("배치 변화")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.3f);
    public bool randomYaw = true;
    public bool alignToSlope = false;
    [Range(0f, 30f)] public float maxTilt = 8f;
    [Range(0f, 0.5f)] public float colorVariation = 0.15f;

    [Header("성능")]
    public int maxChunkLoadsPerFrame = 2;  // 한 프레임에 새로 만드는 구역 수(끊김 방지)

    readonly Dictionary<Vector2Int, GameObject> loaded = new Dictionary<Vector2Int, GameObject>();
    readonly List<Vector2Int> loadQueue = new List<Vector2Int>();
    Vector2Int lastChunk = new Vector2Int(int.MinValue, int.MinValue);

    Transform Tgt { get { return target != null ? target : (Camera.main != null ? Camera.main.transform : null); } }

    void Update()
    {
        var t = Tgt;
        if (t == null) return;
        Vector2Int cur = new Vector2Int(Mathf.FloorToInt(t.position.x / chunkSize), Mathf.FloorToInt(t.position.z / chunkSize));
        if (cur != lastChunk) { lastChunk = cur; Refresh(cur); }
        ProcessQueue();
    }

    void Refresh(Vector2Int center)
    {
        var needed = new HashSet<Vector2Int>();
        for (int dx = -viewRadius; dx <= viewRadius; dx++)
            for (int dz = -viewRadius; dz <= viewRadius; dz++)
                needed.Add(new Vector2Int(center.x + dx, center.y + dz));

        var remove = new List<Vector2Int>();
        foreach (var kv in loaded) if (!needed.Contains(kv.Key)) remove.Add(kv.Key);
        foreach (var k in remove) { if (loaded[k] != null) Destroy(loaded[k]); loaded.Remove(k); }

        loadQueue.Clear();
        foreach (var c in needed) if (!loaded.ContainsKey(c)) loadQueue.Add(c);
        loadQueue.Sort((a, b) => (a - center).sqrMagnitude.CompareTo((b - center).sqrMagnitude));
    }

    void ProcessQueue()
    {
        int n = 0;
        while (loadQueue.Count > 0 && n < maxChunkLoadsPerFrame)
        {
            var c = loadQueue[0]; loadQueue.RemoveAt(0);
            if (!loaded.ContainsKey(c)) LoadChunk(c);
            n++;
        }
    }

    void LoadChunk(Vector2Int c)
    {
        if (prefabs == null || prefabs.Length == 0) return;
        var container = new GameObject("Chunk_" + c.x + "_" + c.y);
        container.transform.SetParent(transform, false);
        loaded[c] = container;

        var prev = Random.state;
        Random.InitState(c.x * 73856093 ^ c.y * 19349663);   // 구역마다 고정 난수 → 다시 와도 동일

        float startY = (Tgt != null ? Tgt.position.y : 0f) + raycastHeight;
        float baseX = c.x * chunkSize, baseZ = c.y * chunkSize;
        for (int i = 0; i < perChunk; i++)
        {
            float x = baseX + Random.Range(0f, chunkSize);
            float z = baseZ + Random.Range(0f, chunkSize);
            if (!Physics.Raycast(new Vector3(x, startY, z), Vector3.down, out RaycastHit hit, raycastHeight * 4f, groundMask)) continue;
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
        }
        Random.state = prev;
    }

    void ApplyTint(GameObject obj, float f)
    {
        foreach (var rend in obj.GetComponentsInChildren<Renderer>())
        {
            var mats = rend.sharedMaterials;
            for (int mi = 0; mi < mats.Length; mi++)
            {
                var mat = mats[mi]; if (mat == null) continue;
                Color col = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor")
                          : (mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white);
                Color v = new Color(Mathf.Clamp01(col.r * f), Mathf.Clamp01(col.g * f), Mathf.Clamp01(col.b * f), col.a);
                var mpb = new MaterialPropertyBlock();
                rend.GetPropertyBlock(mpb, mi);
                if (mat.HasProperty("_BaseColor")) mpb.SetColor("_BaseColor", v);
                if (mat.HasProperty("_Color")) mpb.SetColor("_Color", v);
                rend.SetPropertyBlock(mpb, mi);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        var t = Tgt; if (t == null) return;
        Gizmos.color = Color.green;
        float s = (viewRadius * 2 + 1) * chunkSize;
        Vector3 c = new Vector3(Mathf.Floor(t.position.x / chunkSize) * chunkSize + chunkSize / 2f, t.position.y, Mathf.Floor(t.position.z / chunkSize) * chunkSize + chunkSize / 2f);
        Gizmos.DrawWireCube(c, new Vector3(s, 1f, s));
    }
}
