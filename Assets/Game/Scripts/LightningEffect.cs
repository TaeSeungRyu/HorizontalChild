using UnityEngine;
using UnityEngine.UI;

// 번개/천둥 효과. WeatherSystem과 같은 오브젝트(또는 아무 곳)에 붙이세요.
// 비(Rain) 상태일 때만 가끔 화면이 번쩍이고, 잠시 뒤 천둥소리가 납니다.
// 화면 전체를 순간적으로 하얗게 번쩍이는 방식이라 시점과 무관하게 보입니다.
// 배 움직임 등 다른 시스템은 전혀 건드리지 않습니다.
[DisallowMultipleComponent]
public class LightningEffect : MonoBehaviour
{
    [Header("연동 (비올 때만 번개). 비우면 자동 탐색")]
    public WeatherSystem weather;
    [Tooltip("WeatherSystem 없이 항상 번개를 치게 하려면 체크")]
    public bool alwaysActive = false;

    [Header("빈도 (초)")]
    public float minInterval = 6f;
    public float maxInterval = 18f;

    [Header("번쩍임")]
    [Range(0f, 1f)] public float flashStrength = 0.9f;
    public Color flashColor = new Color(0.9f, 0.95f, 1f);
    public float flashDuration = 0.22f;
    [Tooltip("이중 섬광(번쩍-번쩍) 확률")]
    [Range(0f,1f)] public float doubleFlashChance = 0.5f;

    [Header("천둥소리 (선택)")]
    public AudioClip thunderClip;
    [Range(0f,1f)] public float thunderVolume = 0.7f;
    [Tooltip("번쩍임 후 소리까지 지연(거리감)")]
    public Vector2 thunderDelay = new Vector2(0.6f, 2.5f);

    Image _flash;
    AudioSource _audio;
    float _timer;
    float _flashT = -1f;
    float _flashLen;
    float _flashPeak;

    void Start()
    {
        if (weather == null)
        {
            var arr = Object.FindObjectsByType<WeatherSystem>(FindObjectsSortMode.None);
            if (arr.Length > 0) weather = arr[0];
        }
        BuildFlashOverlay();
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false; _audio.spatialBlend = 0f;
        ResetTimer();
    }

    bool IsActive()
    {
        if (alwaysActive) return true;
        return weather != null && weather.weather == WeatherSystem.Weather.Rain;
    }

    void Update()
    {
        if (_flashT >= 0f)
        {
            _flashT += Time.deltaTime;
            float t = _flashT / _flashLen;
            float a = Mathf.Max(0f, Mathf.Sin(t * Mathf.PI)) * _flashPeak;
            a *= 0.8f + 0.2f * Mathf.PerlinNoise(_flashT * 40f, 0f);
            if (_flash != null)
            {
                _flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, Mathf.Clamp01(a));
                _flash.enabled = a > 0.002f;
            }
            if (_flashT >= _flashLen) { _flashT = -1f; if (_flash != null) _flash.enabled = false; }
            return;
        }

        if (!IsActive()) return;
        _timer -= Time.deltaTime;
        if (_timer <= 0f) { Strike(); ResetTimer(); }
    }

    void Strike()
    {
        _flashT = 0f;
        _flashLen = flashDuration * (Random.value < doubleFlashChance ? 1.7f : 1f);
        _flashPeak = flashStrength * Random.Range(0.7f, 1f);
        if (thunderClip != null)
        {
            float delay = Random.Range(thunderDelay.x, thunderDelay.y);
            _audio.clip = thunderClip; _audio.volume = thunderVolume;
            _audio.PlayDelayed(delay);
        }
    }

    void ResetTimer() => _timer = Random.Range(minInterval, maxInterval);

    void BuildFlashOverlay()
    {
        var canvasGO = new GameObject("LightningOverlay");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -40;
        var imgGO = new GameObject("Flash");
        imgGO.transform.SetParent(canvasGO.transform, false);
        _flash = imgGO.AddComponent<Image>();
        _flash.raycastTarget = false;
        var rt = _flash.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        _flash.color = new Color(1,1,1,0);
        _flash.enabled = false;
    }
}
