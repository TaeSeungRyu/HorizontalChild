using Game.Data;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// 큐브 → 배 모양 절차적 빌더. 외부 3D 모델 없이 Unity 프리미티브 조합으로 배 실루엣 생성.
    ///
    /// 구성: 선체 (Hull) + 갑판 (Deck) + 돛대 (Mast) + 돛 (Sail) + 뱃머리 (Bow).
    /// 부모 GameObject 의 transform.scale 이 전체 크기 — 자식들은 localScale 로 비율 유지.
    ///
    /// 색상: NpcType 별로 선체 색 차별 — 해적(빨강) / 상선(갈색) / 호위선(청회색).
    /// </summary>
    public static class ProceduralShipBuilder
    {
        /// <summary>parent 의 자식으로 배 시각 요소를 추가. parent 의 scale·position 은 caller 가 제어.</summary>
        public static void BuildShip(GameObject parent, NpcType type)
        {
            if (parent == null) return;

            Color hullColor = HullColorFor(type);
            Color deckColor = Color.Lerp(hullColor, Color.white, 0.25f);
            Color mastColor = new Color(0.25f, 0.18f, 0.1f);   // 짙은 갈색
            Color sailColor = new Color(0.95f, 0.95f, 0.88f);   // 아이보리

            // 선체 — 길쭉한 박스 (Z = 전진 방향)
            BuildBox(parent.transform, "Hull",
                localPos: new Vector3(0f, -0.1f, 0f),
                localScale: new Vector3(0.6f, 0.35f, 1.2f),
                color: hullColor);

            // 뱃머리 — 앞쪽 뾰족하게 (rotated 45° around Y)
            var bow = BuildBox(parent.transform, "Bow",
                localPos: new Vector3(0f, -0.1f, 0.65f),
                localScale: new Vector3(0.42f, 0.35f, 0.42f),
                color: hullColor);
            bow.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

            // 갑판 — 선체 위 얇은 박스
            BuildBox(parent.transform, "Deck",
                localPos: new Vector3(0f, 0.13f, 0f),
                localScale: new Vector3(0.55f, 0.08f, 1.1f),
                color: deckColor);

            // 선미 캐빈 — 뒤쪽 살짝 솟음
            BuildBox(parent.transform, "Cabin",
                localPos: new Vector3(0f, 0.3f, -0.4f),
                localScale: new Vector3(0.5f, 0.3f, 0.35f),
                color: deckColor);

            // 돛대 — 가운데 솟은 원기둥
            BuildCylinder(parent.transform, "Mast",
                localPos: new Vector3(0f, 0.6f, 0.1f),
                localScale: new Vector3(0.04f, 0.5f, 0.04f),
                color: mastColor);

            // 돛 — 사각 돛 (전진 방향에 수직)
            BuildBox(parent.transform, "Sail",
                localPos: new Vector3(0f, 0.85f, 0.1f),
                localScale: new Vector3(0.7f, 0.55f, 0.03f),
                color: sailColor);
        }

        // ─── 프리미티브 헬퍼 ──────────────────────────────────────────────

        private static GameObject BuildBox(Transform parent, string name,
            Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            StripCollider(go);
            ApplyColor(go, color);
            return go;
        }

        private static GameObject BuildCylinder(Transform parent, string name,
            Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            StripCollider(go);
            ApplyColor(go, color);
            return go;
        }

        private static void StripCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            renderer.material = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static Color HullColorFor(NpcType type) => type switch
        {
            NpcType.Pirate => new Color(0.5f, 0.15f, 0.15f),    // 어두운 빨강
            NpcType.Merchant => new Color(0.55f, 0.4f, 0.25f),  // 따뜻한 갈색
            NpcType.Escort => new Color(0.25f, 0.4f, 0.55f),    // 청회색
            _ => new Color(0.4f, 0.3f, 0.2f),
        };
    }
}
