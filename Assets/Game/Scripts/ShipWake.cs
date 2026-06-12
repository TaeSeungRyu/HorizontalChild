using UnityEngine;

// 배 항적/물보라 — 배가 움직이면 뒤에 하얀 거품 자국과 뱃머리 물보라가 생깁니다.
// 사용법: 배 오브젝트(ShipMover/FloatOnWaves 붙인 그 오브젝트)에 Add Component.
// 파티클과 머티리얼(부드러운 원형 거품 텍스처 포함)을 코드로 만들어서 핫핑크 문제가 없습니다.
public class ShipWake : MonoBehaviour
{
    [Header("거품 색/크기")]
    public Color foamColor = new Color(0.95f, 0.97f, 1f, 1f);
    public float foamSize = 1.2f;      // 거품 알갱이 크기
    public float density  = 6f;        // 이동 거리당 거품 양(클수록 진함)

    [Header("위치 (배 크기에 맞추기)")]
    public float sternOffset = -6f;    // 항적이 나오는 뒤쪽 위치(-Z, 음수)
    public float bowOffset   = 6.5f;   // 물보라가 나오는 앞쪽 위치(+Z)
    public float wakeWidth   = 2.5f;   // 항적 폭(배 폭쯤)
    public float waterY      = 0.15f;  // 수면 높이(배 로컬 기준, 보통 0 근처)

    Material foamMat;

    void Start()
    {
        foamMat = MakeFoamMaterial();
        MakeSternWake();
        MakeBowSpray();
    }

    Texture2D MakeSoftCircle(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / c, dy = (y - c) / c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d); a *= a;     // 가장자리 부드럽게
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        t.Apply();
        return t;
    }

    Material MakeFoamMaterial()
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        var m = new Material(sh);
        var tex = MakeSoftCircle(32);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        return m;
    }

    ParticleSystem NewPS(string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        var ps = go.AddComponent<ParticleSystem>();
        var pr = go.GetComponent<ParticleSystemRenderer>();
        pr.material = foamMat;
        pr.renderMode = ParticleSystemRenderMode.Billboard;
        pr.alignment = ParticleSystemRenderSpace.View;
        return ps;
    }

    void SetFade(ParticleSystem ps)
    {
        var col = ps.colorOverLifetime; col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(foamColor, 0f), new GradientColorKey(foamColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.15f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var sol = ps.sizeOverLifetime; sol.enabled = true;
        var curve = new AnimationCurve();
        curve.AddKey(0f, 0.4f); curve.AddKey(0.3f, 1f); curve.AddKey(1f, 0.1f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    void MakeSternWake()
    {
        var ps = NewPS("SternWake", new Vector3(0f, waterY, sternOffset));
        var main = ps.main;
        main.startLifetime = 2.2f;
        main.startSpeed = 0.1f;
        main.startSize = foamSize;
        main.startColor = foamColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;  // 물 위에 남는 자국
        main.gravityModifier = 0f;
        main.maxParticles = 2000;

        var em = ps.emission; em.rateOverTime = 0f; em.rateOverDistance = density;
        var shape = ps.shape; shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(wakeWidth, 0.05f, 0.4f);
        SetFade(ps);
    }

    void MakeBowSpray()
    {
        var ps = NewPS("BowSpray", new Vector3(0f, waterY + 0.1f, bowOffset));
        var main = ps.main;
        main.startLifetime = 0.9f;
        main.startSpeed = 1.2f;
        main.startSize = foamSize * 0.6f;
        main.startColor = foamColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;       // 살짝 튀었다 떨어짐
        main.maxParticles = 800;

        var em = ps.emission; em.rateOverTime = 0f; em.rateOverDistance = density * 0.7f;
        var shape = ps.shape; shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f; shape.radius = wakeWidth * 0.3f;
        shape.rotation = new Vector3(-90f, 0f, 0f);  // 앞쪽 위로 튀게
        SetFade(ps);
    }
}
