using UnityEngine;

// 출렁이는 바다 — 빈 GameObject에 붙이면 물결치는 바다가 생깁니다.
// 방향이 다른 파도 여러 개(Gerstner 방식)와 잔물결을 겹쳐 자연스럽게 움직입니다.
// 물결은 "월드 좌표" 기준이라 타일을 깔아도 이음새가 없고, Follow Camera로 맵 전체를 덮을 수 있습니다.
// GetHeight()로 임의 지점의 수면 높이를 알려주어 배 부력(FloatOnWaves)이 정확히 떠 있게 합니다.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanWaves : MonoBehaviour
{
    [Header("바다 크기")]
    public float size = 80f;
    [Range(4, 200)] public int resolution = 50;

    [Header("파도")]
    public float waveHeight = 0.5f;          // 전체 파고
    public float waveSpeed  = 1.0f;          // 전체 속도
    public float waveScale  = 0.15f;         // 클수록 잔잔한 잔파도, 작을수록 크고 완만한 너울
    [Range(0f, 1f)] public float choppiness = 0.45f; // 너울감(마루가 뾰족해짐)
    [Tooltip("주 파도가 나아가는 방향(도)")]
    public float windDirection = 30f;

    [Header("잔물결 (디테일)")]
    public float rippleHeight = 0.12f;
    public float rippleScale  = 1.2f;
    public float rippleSpeed  = 2.2f;

    [Header("맵 전체 덮기 (선택)")]
    public bool  followCamera = false;
    public Transform target;
    public float waterLevel = 0f;

    [Header("색 (선택)")]
    public Color seaColor = new Color(0.06f, 0.28f, 0.42f);

    // 4겹 파도 설정 (방향 오프셋, 주파수배수, 진폭배수, 속도배수)
    static readonly float[] DirOff  = { 0f, 35f, -55f, 85f };
    static readonly float[] FreqMul = { 1.0f, 1.7f, 2.6f, 4.1f };
    static readonly float[] AmpMul  = { 0.50f, 0.32f, 0.22f, 0.14f };
    static readonly float[] SpdMul  = { 1.0f, 1.15f, 0.9f, 1.4f };
    const float TotalAmp = 1.18f;

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

    // 월드 좌표의 변위(수평 ox,oz + 수직 y). 마루가 뾰족한 Gerstner 느낌.
    public Vector3 Displace(float wx, float wz)
    {
        float t = Time.time * waveSpeed;
        float vy = 0f, ox = 0f, oz = 0f;
        for (int k = 0; k < 4; k++)
        {
            float ang = (windDirection + DirOff[k]) * Mathf.Deg2Rad;
            float dx = Mathf.Cos(ang), dz = Mathf.Sin(ang);
            float w = waveScale * FreqMul[k];
            float phase = (wx * dx + wz * dz) * w + t * SpdMul[k] * (1f + FreqMul[k] * 0.12f);
            float a = AmpMul[k];
            vy += a * Mathf.Sin(phase);
            float c = a * Mathf.Cos(phase) * choppiness;
            ox += dx * c; oz += dz * c;
        }
        vy = vy / TotalAmp * waveHeight;
        ox = ox / TotalAmp * waveHeight;
        oz = oz / TotalAmp * waveHeight;

        // 잔물결(수직만) — 표면 반짝이는 디테일
        float rt = Time.time * rippleSpeed;
        vy += (Mathf.Sin(wx * waveScale * rippleScale * 8f + rt)
             + Mathf.Sin(wz * waveScale * rippleScale * 11f - rt * 0.9f)) * 0.5f * rippleHeight;

        return new Vector3(ox, vy, oz);
    }

    // 월드 좌표의 수면 높이(수직). 부력 계산용.
    public float WaveOffset(float worldX, float worldZ) => Displace(worldX, worldZ).y;

    // 수면의 실제 월드 Y 높이 (바다 오브젝트 Y + 파도)
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
            Vector3 d = Displace(wx, wz);
            verts[i] = new Vector3(baseVerts[i].x + d.x, d.y, baseVerts[i].z + d.z);
        }
        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
