using UnityEngine;

// 출렁이는 바다 — 빈 GameObject에 붙이면 물결치는 바다가 생깁니다.
// 물결은 "월드 좌표" 기준이라 타일을 깔아도 이음새가 없고, Follow Camera로 맵 전체를 덮을 수 있습니다.
// GetHeight()로 임의 지점의 수면 높이를 알려주어, 배 부력(FloatOnWaves)이 정확히 떠 있게 합니다.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanWaves : MonoBehaviour
{
    [Header("바다 크기")]
    public float size = 80f;
    [Range(4, 200)] public int resolution = 50;

    [Header("파도")]
    public float waveHeight = 0.5f;
    public float waveSpeed  = 1.0f;
    public float waveScale  = 0.15f;

    [Header("맵 전체 덮기 (선택)")]
    public bool  followCamera = false;
    public Transform target;
    public float waterLevel = 0f;

    [Header("색 (선택)")]
    public Color seaColor = new Color(0.06f, 0.28f, 0.42f);

    Mesh mesh;
    Vector3[] baseVerts, verts;

    void Start()
    {
        BuildGrid();
        var r = GetComponent<MeshRenderer>();
        if (r.sharedMaterial == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            r.material = new Material(sh);
        }
        if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", seaColor);
        else if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", seaColor);
    }

    void BuildGrid()
    {
        int n = resolution;
        mesh = new Mesh { name = "OceanMesh" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        baseVerts = new Vector3[(n + 1) * (n + 1)];
        Vector2[] uv = new Vector2[baseVerts.Length];
        float step = size / n;
        for (int z = 0; z <= n; z++)
            for (int x = 0; x <= n; x++)
            {
                int i = z * (n + 1) + x;
                baseVerts[i] = new Vector3(-size / 2f + x * step, 0f, -size / 2f + z * step);
                uv[i] = new Vector2((float)x / n, (float)z / n);
            }
        int[] tris = new int[n * n * 6];
        int ti = 0;
        for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                int i = z * (n + 1) + x;
                tris[ti++] = i; tris[ti++] = i + (n + 1); tris[ti++] = i + 1;
                tris[ti++] = i + 1; tris[ti++] = i + (n + 1); tris[ti++] = i + (n + 1) + 1;
            }
        verts = (Vector3[])baseVerts.Clone();
        mesh.vertices = baseVerts; mesh.triangles = tris; mesh.uv = uv;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // 같은 파도 공식. 월드 좌표(worldX, worldZ)의 수면 높이를 돌려줌.
    public float WaveOffset(float worldX, float worldZ)
    {
        float t = Time.time * waveSpeed;
        return (Mathf.Sin(worldX * waveScale + t) * 0.6f
              + Mathf.Sin(worldZ * waveScale * 1.3f + t * 0.8f) * 0.4f
              + Mathf.Sin((worldX + worldZ) * waveScale * 0.7f + t * 1.3f) * 0.3f) * waveHeight;
    }

    // 수면의 실제 월드 Y 높이 (바다 오브젝트의 Y 위치 + 파도)
    public float GetHeight(float worldX, float worldZ)
    {
        return transform.position.y + WaveOffset(worldX, worldZ);
    }

    void Update()
    {
        if (mesh == null) return;
        if (followCamera)
        {
            Transform cam = target != null ? target : (Camera.main != null ? Camera.main.transform : null);
            if (cam != null)
            {
                float step = size / resolution;
                float sx = Mathf.Floor(cam.position.x / step) * step;
                float sz = Mathf.Floor(cam.position.z / step) * step;
                transform.position = new Vector3(sx, waterLevel, sz);
            }
        }
        Vector3 p = transform.position;
        for (int i = 0; i < baseVerts.Length; i++)
        {
            float wx = baseVerts[i].x + p.x;
            float wz = baseVerts[i].z + p.z;
            verts[i] = new Vector3(baseVerts[i].x, WaveOffset(wx, wz), baseVerts[i].z);
        }
        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
