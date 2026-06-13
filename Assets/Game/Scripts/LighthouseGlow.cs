using UnityEngine;

// 등대 불빛 애니메이션. (Harbor 또는 Lighthouse 프리팹에 붙이세요)
// 등불 부분(Hb_Lamp, emission 재질)의 색/밝기를 시간에 따라 바꿉니다.
// - Pulse(점멸): 등대처럼 깜빡임
// - Rotate(회전 점멸): 등대 빛이 도는 느낌(주기적으로 확 밝아짐)
// - ColorCycle(색 순환): 색이 무지개처럼 바뀜
// 선택한 머티리얼 이름(targetMaterialName)을 가진 렌더러에만 적용됩니다.
[DisallowMultipleComponent]
public class LighthouseGlow : MonoBehaviour
{
    public enum Mode { Pulse, Rotate, ColorCycle, Steady }

    [Header("어떤 부분에 적용할지 (재질 이름 포함 검색)")]
    public string targetMaterialName = "Hb_Lamp";

    [Header("모드")]
    public Mode mode = Mode.Pulse;

    [Header("색")]
    public Color colorA = new Color(1f, 0.86f, 0.45f);  // 기본 등불색(주황)
    public Color colorB = new Color(1f, 0.30f, 0.20f);  // 두 번째 색(점멸/순환용)
    [Tooltip("ColorCycle 모드에서 무지개색으로 순환")]
    public bool rainbow = false;

    [Header("밝기 / 속도")]
    public float minIntensity = 0.4f;
    public float maxIntensity = 4.0f;
    [Tooltip("초당 점멸/순환 횟수")]
    public float speed = 0.8f;
    [Tooltip("Rotate 모드: 한 바퀴에서 빛이 비추는 구간 비율(0~1, 작을수록 짧고 강한 섬광)")]
    [Range(0.05f, 0.9f)] public float beamWidth = 0.25f;

    // 내부
    Renderer[] _renderers;
    int[][] _slots;                 // 각 렌더러에서 대상 머티리얼 슬롯들
    MaterialPropertyBlock _mpb;
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    static readonly int ColorID     = Shader.PropertyToID("_Color");
    static readonly int EmColorID   = Shader.PropertyToID("_EmissionColor");
    float _t;

    void Start()
    {
        _mpb = new MaterialPropertyBlock();
        var rends = GetComponentsInChildren<Renderer>(true);
        var rList = new System.Collections.Generic.List<Renderer>();
        var sList = new System.Collections.Generic.List<int[]>();
        foreach (var r in rends)
        {
            var mats = r.sharedMaterials;
            var idx = new System.Collections.Generic.List<int>();
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && mats[i].name.Contains(targetMaterialName)) idx.Add(i);
            if (idx.Count > 0) { rList.Add(r); sList.Add(idx.ToArray()); }
        }
        _renderers = rList.ToArray();
        _slots = sList.ToArray();

        if (_renderers.Length == 0)
            Debug.LogWarning("[LighthouseGlow] '" + targetMaterialName + "' 재질을 가진 렌더러를 못 찾음. targetMaterialName 확인.");
    }

    void Update()
    {
        if (_renderers == null || _renderers.Length == 0) return;
        _t += Time.deltaTime * speed;

        Color col; float inten;
        switch (mode)
        {
            case Mode.Pulse:
            {
                float w = (Mathf.Sin(_t * Mathf.PI * 2f) + 1f) * 0.5f; // 0~1
                inten = Mathf.Lerp(minIntensity, maxIntensity, w);
                col = Color.Lerp(colorA, colorB, w);
                break;
            }
            case Mode.Rotate:
            {
                float phase = Mathf.Repeat(_t, 1f);
                float beam = phase < beamWidth ? Mathf.Sin(phase / beamWidth * Mathf.PI) : 0f; // 짧은 섬광
                inten = Mathf.Lerp(minIntensity, maxIntensity, beam);
                col = colorA;
                break;
            }
            case Mode.ColorCycle:
            {
                if (rainbow)
                    col = Color.HSVToRGB(Mathf.Repeat(_t, 1f), 0.85f, 1f);
                else
                    col = Color.Lerp(colorA, colorB, (Mathf.Sin(_t * Mathf.PI * 2f) + 1f) * 0.5f);
                inten = maxIntensity;
                break;
            }
            default: // Steady
                col = colorA; inten = maxIntensity; break;
        }

        Color emission = col * inten;
        for (int ri = 0; ri < _renderers.Length; ri++)
        {
            var r = _renderers[ri];
            foreach (int slot in _slots[ri])
            {
                r.GetPropertyBlock(_mpb, slot);
                _mpb.SetColor(EmColorID, emission);
                _mpb.SetColor(BaseColorID, col);
                _mpb.SetColor(ColorID, col);
                r.SetPropertyBlock(_mpb, slot);
            }
        }
    }
}
