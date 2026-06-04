using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// UI 패널에 둥근 모서리(border radius) + 그림자 효과를 부여.
    ///
    /// 동작:
    ///   - 런타임에 둥근 사각형 Sprite 를 생성해 Image 에 적용 (9-slice)
    ///   - Mask 컴포넌트로 자식 UI 를 둥근 모양으로 클립
    ///   - Shadow 컴포넌트로 드롭 섀도
    ///
    /// 사용:
    ///   부착 → cornerRadiusPx / shadow* 인스펙터에서 조정 → Play 시 적용
    ///   이미 Image / Mask / Shadow 가 있으면 그것을 재사용 (덮어쓰지 않음)
    ///
    /// 주의:
    ///   Mask 가 자식의 raycast 도 둥근 영역으로 제한 → 자식 UI 입력은 정상 작동
    ///   배경 색을 panelColor 로 지정 가능 (텍스처 위에 살짝 보이고 싶을 때)
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class RoundedPanel : MonoBehaviour
    {
        [Header("Rounded Corner")]
        [Tooltip("모서리 반경 (픽셀, 텍스처 기준). 8~24 사이가 자연스러움.")]
        [Range(2, 32)]
        public int cornerRadiusPx = 16;

        [Tooltip("패널 배경 색 — 자식 UI 가 모서리를 다 채우면 보이지 않음.")]
        public Color panelColor = new Color(0.1f, 0.15f, 0.2f, 1f);

        [Header("Shadow")]
        public bool enableShadow = true;
        public Color shadowColor = new Color(0f, 0f, 0f, 0.55f);
        public Vector2 shadowOffset = new Vector2(4f, -4f);

        private void OnEnable()
        {
            EnsureImage();
            EnsureMask();
            EnsureShadow();
        }

        private void EnsureImage()
        {
            var image = GetComponent<Image>();
            if (image == null) image = gameObject.AddComponent<Image>();

            image.sprite = GenerateRoundedSprite(cornerRadiusPx);
            image.type = Image.Type.Sliced;
            image.color = panelColor;
            image.raycastTarget = false; // 클릭 가로채지 않게
        }

        private void EnsureMask()
        {
            var mask = GetComponent<Mask>();
            if (mask == null) mask = gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true; // 배경색이 보여야 Shadow 가 정상 동작
        }

        private void EnsureShadow()
        {
            var shadow = GetComponent<Shadow>();
            if (!enableShadow)
            {
                if (shadow != null) shadow.enabled = false;
                return;
            }
            if (shadow == null) shadow = gameObject.AddComponent<Shadow>();
            shadow.enabled = true;
            shadow.effectColor = shadowColor;
            shadow.effectDistance = shadowOffset;
        }

        // ─── 둥근 사각형 Sprite 생성 ────────────────────────────────────────

        /// <summary>
        /// 64x64 텍스처에 둥근 사각형(흰색) 그려서 Sprite 로 반환.
        /// 9-slice border = cornerRadius 라 텍스처를 어떤 크기로 늘려도 모서리 유지.
        /// </summary>
        private static Sprite GenerateRoundedSprite(int r)
        {
            const int size = 64;
            r = Mathf.Clamp(r, 1, size / 2 - 1);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = CornerAlpha(x, y, size, r) * Color.white;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            var border = new Vector4(r, r, r, r);
            return Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.Tight,
                border);
        }

        private static float CornerAlpha(int x, int y, int size, int r)
        {
            // 어느 코너 영역인지 판별 — 둘 다 strip 안이면 코너
            bool leftStrip   = x < r;
            bool rightStrip  = x >= size - r;
            bool topStrip    = y >= size - r;
            bool bottomStrip = y < r;
            bool inCorner    = (leftStrip || rightStrip) && (topStrip || bottomStrip);

            if (!inCorner) return 1f; // 평평한 가장자리 / 중심 = 불투명

            int cx = leftStrip ? r : size - r;
            int cy = bottomStrip ? r : size - r;
            float dx = x - cx;
            float dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);

            // 안티앨리어싱 — r 근처에서 부드럽게 0~1
            return Mathf.Clamp01(r + 0.5f - dist);
        }
    }
}
