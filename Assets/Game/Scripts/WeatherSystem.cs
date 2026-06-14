using System.Collections.Generic;
using UnityEngine;

// 기후 시스템 — 맑음 / 구름 조금 / 구름 많음 / 비 를 부드럽게 전환.
// 빈 GameObject에 붙이면 됩니다. 구름·비·하늘색·조명을 코드로 제어하므로 별도 에셋이 거의 필요 없습니다.
//   - 구름: 위쪽에 깔리는 소프트 빌보드 파티클(코드 생성 텍스처)
//   - 비:   카메라를 따라다니는 비 파티클 + (선택) 어두워지는 하늘/조명
//   - 전환: 날씨를 바꾸면 구름 양/비/밝기가 시간에 걸쳐 부드럽게 변함
// Inspector에서 weather 드롭다운을 바꾸거나, 코드에서 SetWeather(...) 호출.
[DisallowMultipleComponent]
public class WeatherSystem : MonoBehaviour
{
    public enum Weather { Clear, PartlyCloudy, Overcast, Rain }

    [Header("현재 날씨")]
    public Weather weather = Weather.Clear;
    [Tooltip("자동으로 일정 시간마다 날씨가 무작위로 바뀜")]
    public bool autoChange = false;
    public Vector2 autoChangeInterval = new Vector2(30f, 90f);

    [Header("전환")]
    [Tooltip("날씨가 바뀔 때 부드럽게 변하는 시간(초)")]
    public float transitionTime = 6f;

    [Header("따라다닐 대상 (비워두면 Main Camera)")]
    public Transform target;
    public float cloudHeight = 40f;
    public float rainHeight = 22f;

    [Header("하늘/조명 (선택)")]
    public Light sun;                          // 비우면 자동 탐색(첫 Directional)
    public Color clearSun = new Color(1f, 0.98f, 0.92f);
    public Color overcastSun = new Color(0.62f, 0.64f, 0.68f);
    [Range(0f, 3f)] public float clearSunIntensity = 1.6f;
    [Range(0f, 3f)] public float overcastSunIntensity = 0.85f;
    public bool controlAmbient = true;
    public Color clearAmbient = new Color(0.55f, 0.6f, 0.68f);
    public Color overcastAmbient = new Color(0.4f, 0.42f, 0.46f);

    // 날씨별 목표값: (구름량 0~1, 비량 0~1, 흐림 0~1)
    struct Profile { public float cloud, rain, gloom; }
    Profile Target(Weather w)
    {
        switch (w)
        {
            case Weather.Clear:        return new Profile { cloud = 0.0f, rain = 0f, gloom = 0f };
            case Weather.PartlyCloudy: return new Profile { cloud = 0.35f, rain = 0f, gloom = 0.15f };
            case Weather.Overcast:     return new Profile { cloud = 0.9f, rain = 0f, gloom = 0.6f };
            case Weather.Rain:         return new Profile { cloud = 1.0f, rain = 1f, gloom = 0.85f };
        }
        return new Profile();
    }

    // 현재 보간값
    float _cloud, _rain, _gloom;
    Profile _from, _to;
    float _lerpT = 1f;
    float _autoTimer;

    ParticleSystem _clouds, _rainPs;
    Transform _cam;
    Weather _applied;

    void Start()
    {
        _cam = target != null ? target : (Camera.main != null ? Camera.main.transform : null);
        if (sun == null)
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }

        BuildClouds();
        BuildRain();

        var p = Target(weather);
        _cloud = p.cloud; _rain = p.rain; _gloom = p.gloom;
        _from = _to = p; _lerpT = 1f; _applied = weather;
        ResetAutoTimer();
        Apply();
    }

    public void SetWeather(Weather w)
    {
        if (w == weather && _lerpT >= 1f) return;
        weather = w;
        _from = new Profile { cloud = _cloud, rain = _rain, gloom = _gloom };
        _to = Target(w);
        _lerpT = 0f;
        _applied = w;
    }

    void Update()
    {
        // Inspector에서 드롭다운을 직접 바꾼 경우 감지
        if (weather != _applied) SetWeather(weather);

        if (autoChange)
        {
            _autoTimer -= Time.deltaTime;
            if (_autoTimer <= 0f) { SetWeather((Weather)Random.Range(0, 4)); ResetAutoTimer(); }
        }

        if (_lerpT < 1f)
        {
            _lerpT += Time.deltaTime / Mathf.Max(0.01f, transitionTime);
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_lerpT));
            _cloud = Mathf.Lerp(_from.cloud, _to.cloud, k);
            _rain  = Mathf.Lerp(_from.rain,  _to.rain,  k);
            _gloom = Mathf.Lerp(_from.gloom, _to.gloom, k);
            Apply();
        }

        // 구름·비가 카메라(대상)를 따라다니게
        if (_cam != null)
        {
            Vector3 c = _cam.position;
            if (_clouds != null) _clouds.transform.position = new Vector3(c.x, c.y + cloudHeight, c.z);
            if (_rainPs != null) _rainPs.transform.position = new Vector3(c.x, c.y + rainHeight, c.z);
        }
    }

    void Apply()
    {
        // 구름 양 → 방출량
        if (_clouds != null)
        {
            var em = _clouds.emission;
            em.rateOverTime = Mathf.Lerp(0f, 40f, _cloud);
            var main = _clouds.main;
            var col = main.startColor.color;
            // 흐릴수록 구름이 어둑하고 진하게
            float g = Mathf.Lerp(1f, 0.6f, _gloom);
            main.startColor = new Color(g, g, g * 1.02f, Mathf.Lerp(0.0f, 0.85f, _cloud));
            if (_cloud > 0.01f && !_clouds.isPlaying) _clouds.Play();
        }
        // 비
        if (_rainPs != null)
        {
            var em = _rainPs.emission;
            em.rateOverTime = Mathf.Lerp(0f, 1800f, _rain);
            if (_rain > 0.01f) { if (!_rainPs.isPlaying) _rainPs.Play(); }
            else { if (_rainPs.isPlaying) _rainPs.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
        }
        // 조명/하늘
        if (sun != null)
        {
            sun.color = Color.Lerp(clearSun, overcastSun, _gloom);
            sun.intensity = Mathf.Lerp(clearSunIntensity, overcastSunIntensity, _gloom);
        }
        if (controlAmbient)
            RenderSettings.ambientLight = Color.Lerp(clearAmbient, overcastAmbient, _gloom);
    }

    void ResetAutoTimer() => _autoTimer = Random.Range(autoChangeInterval.x, autoChangeInterval.y);

    // ---------- 파티클 생성 ----------
    Material _softMat, _rainMat;

    void BuildClouds()
    {
        var go = new GameObject("Clouds");
        go.transform.SetParent(transform, false);
        _clouds = go.AddComponent<ParticleSystem>();
        _clouds.Stop();
        var main = _clouds.main;
        main.startLifetime = 30f;
        main.startSpeed = 0.4f;
        main.startSize = new ParticleSystem.MinMaxCurve(14f, 30f);
        main.startColor = new Color(1f, 1f, 1f, 0f);
        main.maxParticles = 120;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        var em = _clouds.emission; em.rateOverTime = 0f;
        var sh = _clouds.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(160f, 1f, 160f);
        var col = _clouds.colorOverLifetime; col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;
        var rend = _clouds.GetComponent<ParticleSystemRenderer>();
        rend.material = SoftMat();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sortingFudge = 50f;
    }

    void BuildRain()
    {
        var go = new GameObject("Rain");
        go.transform.SetParent(transform, false);
        _rainPs = go.AddComponent<ParticleSystem>();
        _rainPs.Stop();
        var main = _rainPs.main;
        main.startLifetime = 1.2f;
        main.startSpeed = 35f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
        main.startColor = new Color(0.7f, 0.78f, 0.9f, 0.55f);
        main.maxParticles = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.2f;
        var em = _rainPs.emission; em.rateOverTime = 0f;
        var sh = _rainPs.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(60f, 1f, 60f);
        sh.rotation = new Vector3(0f, 0f, 0f);
        // 아래로 떨어지게
        _rainPs.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var rend = _rainPs.GetComponent<ParticleSystemRenderer>();
        rend.material = RainMat();
        rend.renderMode = ParticleSystemRenderMode.Stretch;
        rend.velocityScale = 0.05f;
        rend.lengthScale = 3f;
    }

    Material SoftMat()
    {
        if (_softMat != null) return _softMat;
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        _softMat = new Material(sh);
        var tex = SoftCircle(64);
        if (_softMat.HasProperty("_BaseMap")) _softMat.SetTexture("_BaseMap", tex);
        if (_softMat.HasProperty("_MainTex")) _softMat.SetTexture("_MainTex", tex);
        SetTransparent(_softMat);
        return _softMat;
    }

    Material RainMat()
    {
        if (_rainMat != null) return _rainMat;
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        _rainMat = new Material(sh);
        var tex = Texture2D.whiteTexture;
        if (_rainMat.HasProperty("_BaseMap")) _rainMat.SetTexture("_BaseMap", tex);
        if (_rainMat.HasProperty("_MainTex")) _rainMat.SetTexture("_MainTex", tex);
        SetTransparent(_rainMat);
        return _rainMat;
    }

    void SetTransparent(Material m)
    {
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f); // URP: Transparent
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);     // Alpha
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.renderQueue = 3000;
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }

    Texture2D SoftCircle(int s)
    {
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        float c = (s - 1) / 2f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a); // smoothstep
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        t.Apply();
        return t;
    }
}
