using UnityEngine;
using UnityEngine.UI;

// 기후 시스템 — 맑음 / 구름 조금 / 구름 많음 / 비 를 부드럽게 전환.
// 탑다운(위에서 내려봄) + Unlit 지도에서도 "무조건 보이는" 화면 틴트(veil)를 사용합니다.
//   - 화면 틴트: 흐릴수록 회색, 비올수록 짙은 청회색으로 화면 전체가 뿌옇게 (가장 확실)
//   - 구름: 카메라 아래에 깔리는 소프트 빌보드 파티클
//   - 비:   파티클 + 틴트로 표현
// 빈 GameObject에 붙이고 Play 후 Weather 드롭다운을 바꾸면 전환됩니다.
[DisallowMultipleComponent]
public class WeatherSystem : MonoBehaviour
{
    public enum Weather { Clear, PartlyCloudy, Overcast, Rain }

    [Header("현재 날씨 (Play 중 바꿔보세요)")]
    public Weather weather = Weather.Clear;
    public bool autoChange = false;
    public Vector2 autoChangeInterval = new Vector2(30f, 90f);

    [Header("전환 시간(초)")]
    public float transitionTime = 5f;

    [Header("화면 틴트 (가장 확실히 보임)")]
    public bool useScreenTint = true;
    [Range(0f, 0.8f)] public float maxTintAlpha = 0.38f;
    public Color cloudyTint = new Color(0.55f, 0.58f, 0.62f);
    public Color rainTint   = new Color(0.38f, 0.45f, 0.55f);

    [Header("구름 파티클 (탑다운: 카메라 아래로)")]
    public bool useCloudParticles = true;
    public Transform target;                 // 비우면 Main Camera
    [Tooltip("탑다운이면 음수(카메라보다 아래). 예: -12")]
    public float cloudHeight = -12f;

    [Header("비 파티클")]
    public bool useRainParticles = true;
    public float rainHeight = 8f;

    [Header("조명 (Lit 오브젝트에만 영향)")]
    public Light sun;
    [Range(0f,3f)] public float clearSunIntensity = 1.6f;
    [Range(0f,3f)] public float overcastSunIntensity = 0.8f;

    struct Profile { public float cloud, rain, gloom; }
    Profile Target(Weather w)
    {
        switch (w)
        {
            case Weather.Clear:        return new Profile { cloud = 0f,   rain = 0f, gloom = 0f };
            case Weather.PartlyCloudy: return new Profile { cloud = 0.4f, rain = 0f, gloom = 0.22f };
            case Weather.Overcast:     return new Profile { cloud = 0.95f,rain = 0f, gloom = 0.65f };
            case Weather.Rain:         return new Profile { cloud = 1f,   rain = 1f, gloom = 0.95f };
        }
        return new Profile();
    }

    float _cloud, _rain, _gloom;
    Profile _from, _to; float _lerpT = 1f; float _autoTimer;
    ParticleSystem _clouds, _rainPs;
    Transform _cam; Weather _applied;
    Image _tint; Material _softMat, _rainMat;

    void Start()
    {
        _cam = target != null ? target : (Camera.main != null ? Camera.main.transform : null);
        if (sun == null)
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }

        if (useScreenTint)     BuildOverlay();
        if (useCloudParticles) BuildClouds();
        if (useRainParticles)  BuildRain();

        var p = Target(weather);
        _cloud = p.cloud; _rain = p.rain; _gloom = p.gloom;
        _from = _to = p; _lerpT = 1f; _applied = weather;
        ResetAutoTimer(); Apply();
        Debug.Log("[WeatherSystem] 시작됨. 화면틴트=" + useScreenTint + ", 날씨=" + weather +
                  " — Play 중 Weather를 Rain/Overcast로 바꾸면 화면이 어둑해집니다.");
    }

    public void SetWeather(Weather w)
    {
        weather = w;
        _from = new Profile { cloud = _cloud, rain = _rain, gloom = _gloom };
        _to = Target(w); _lerpT = 0f; _applied = w;
    }

    void Update()
    {
        if (weather != _applied) SetWeather(weather);
        if (autoChange) { _autoTimer -= Time.deltaTime; if (_autoTimer <= 0f) { SetWeather((Weather)Random.Range(0,4)); ResetAutoTimer(); } }

        if (_lerpT < 1f)
        {
            _lerpT += Time.deltaTime / Mathf.Max(0.01f, transitionTime);
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_lerpT));
            _cloud = Mathf.Lerp(_from.cloud, _to.cloud, k);
            _rain  = Mathf.Lerp(_from.rain,  _to.rain,  k);
            _gloom = Mathf.Lerp(_from.gloom, _to.gloom, k);
            Apply();
        }

        if (_cam != null)
        {
            Vector3 c = _cam.position;
            if (_clouds != null) _clouds.transform.position = new Vector3(c.x, c.y + cloudHeight, c.z);
            if (_rainPs != null) _rainPs.transform.position = new Vector3(c.x, c.y + rainHeight, c.z);
        }
    }

    void Apply()
    {
        if (_tint != null)
        {
            Color baseT = Color.Lerp(cloudyTint, rainTint, _rain);
            float a = Mathf.Clamp01(_gloom) * maxTintAlpha;
            _tint.color = new Color(baseT.r, baseT.g, baseT.b, a);
            _tint.enabled = a > 0.001f;
        }
        if (_clouds != null)
        {
            var em = _clouds.emission; em.rateOverTime = Mathf.Lerp(0f, 40f, _cloud);
            var main = _clouds.main;
            float g = Mathf.Lerp(1f, 0.55f, _gloom);
            main.startColor = new Color(g, g, g * 1.03f, Mathf.Lerp(0f, 0.9f, _cloud));
            if (_cloud > 0.01f && !_clouds.isPlaying) _clouds.Play();
        }
        if (_rainPs != null)
        {
            var em = _rainPs.emission; em.rateOverTime = Mathf.Lerp(0f, 2200f, _rain);
            if (_rain > 0.01f) { if (!_rainPs.isPlaying) _rainPs.Play(); }
            else if (_rainPs.isPlaying) _rainPs.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (sun != null) sun.intensity = Mathf.Lerp(clearSunIntensity, overcastSunIntensity, _gloom);
    }

    void ResetAutoTimer() => _autoTimer = Random.Range(autoChangeInterval.x, autoChangeInterval.y);

    void BuildOverlay()
    {
        var canvasGO = new GameObject("WeatherOverlay");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -50;
        var imgGO = new GameObject("Tint");
        imgGO.transform.SetParent(canvasGO.transform, false);
        _tint = imgGO.AddComponent<Image>();
        _tint.raycastTarget = false;
        var rt = _tint.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        _tint.color = new Color(0,0,0,0);
    }

    void BuildClouds()
    {
        var go = new GameObject("Clouds"); go.transform.SetParent(transform, false);
        _clouds = go.AddComponent<ParticleSystem>(); _clouds.Stop();
        var main = _clouds.main;
        main.startLifetime = 30f; main.startSpeed = 0.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(28f, 64f);
        main.startColor = new Color(1f,1f,1f,0f);
        main.maxParticles = 160;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI*2f);
        var em = _clouds.emission; em.rateOverTime = 0f;
        var sh = _clouds.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(300f,1f,300f);
        var col = _clouds.colorOverLifetime; col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[]{ new GradientColorKey(Color.white,0f), new GradientColorKey(Color.white,1f) },
                     new[]{ new GradientAlphaKey(0f,0f), new GradientAlphaKey(1f,0.2f), new GradientAlphaKey(1f,0.8f), new GradientAlphaKey(0f,1f) });
        col.color = grad;
        var rend = _clouds.GetComponent<ParticleSystemRenderer>();
        rend.material = SoftMat(); rend.renderMode = ParticleSystemRenderMode.Billboard; rend.sortingFudge = 50f;
    }

    void BuildRain()
    {
        var go = new GameObject("Rain"); go.transform.SetParent(transform, false);
        _rainPs = go.AddComponent<ParticleSystem>(); _rainPs.Stop();
        var main = _rainPs.main;
        main.startLifetime = 1.0f; main.startSpeed = 40f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
        main.startColor = new Color(0.75f,0.82f,0.95f,0.7f);
        main.maxParticles = 6000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.2f;
        var em = _rainPs.emission; em.rateOverTime = 0f;
        var sh = _rainPs.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(150f,1f,150f);
        go.transform.localRotation = Quaternion.Euler(90f,0f,0f);
        var rend = _rainPs.GetComponent<ParticleSystemRenderer>();
        rend.material = RainMat(); rend.renderMode = ParticleSystemRenderMode.Stretch;
        rend.velocityScale = 0.05f; rend.lengthScale = 4f;
    }

    Material SoftMat()
    {
        if (_softMat != null) return _softMat;
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        _softMat = new Material(sh);
        var tex = SoftCircle(64);
        if (_softMat.HasProperty("_BaseMap")) _softMat.SetTexture("_BaseMap", tex);
        if (_softMat.HasProperty("_MainTex")) _softMat.SetTexture("_MainTex", tex);
        SetTransparent(_softMat); return _softMat;
    }
    Material RainMat()
    {
        if (_rainMat != null) return _rainMat;
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        _rainMat = new Material(sh);
        if (_rainMat.HasProperty("_BaseMap")) _rainMat.SetTexture("_BaseMap", Texture2D.whiteTexture);
        if (_rainMat.HasProperty("_MainTex")) _rainMat.SetTexture("_MainTex", Texture2D.whiteTexture);
        SetTransparent(_rainMat); return _rainMat;
    }
    void SetTransparent(Material m)
    {
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0); m.renderQueue = 3000;
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }
    Texture2D SoftCircle(int s)
    {
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
        float c = (s-1)/2f;
        for (int y=0;y<s;y++) for (int x=0;x<s;x++){
            float d = Mathf.Sqrt((x-c)*(x-c)+(y-c)*(y-c))/c;
            float a = Mathf.Clamp01(1f-d); a=a*a*(3f-2f*a);
            t.SetPixel(x,y,new Color(1f,1f,1f,a));
        }
        t.Apply(); return t;
    }
}
