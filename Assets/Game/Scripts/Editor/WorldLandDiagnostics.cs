using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 화면의 육지가 왜 단색인지 진단 (아무것도 변경 안 함).
    /// 메뉴: Game ▸ Diagnose World Land   (★ Play 중 실행 권장)
    /// </summary>
    public static class WorldLandDiagnostics
    {
        private const string MeshPath = "Assets/Game/Art/Map/WorldLand.mesh";

        [MenuItem("Game/Diagnose World Land")]
        public static void Diagnose()
        {
            var sb = new StringBuilder("[WorldLand 진단]  (Play중=" + Application.isPlaying + ")\n");

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (mesh != null)
                sb.Append("WorldLand.mesh: submeshCount=").Append(mesh.subMeshCount)
                  .Append(", verts=").Append(mesh.vertexCount).Append("\n");

            // 큰 렌더러 (XZ폭>300)
            sb.Append("\n[큰 렌더러 (XZ폭>300) — 높이Y 내림차순]\n");
            var rends = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            System.Array.Sort(rends, (a, c) => c.bounds.center.y.CompareTo(a.bounds.center.y));
            int big = 0;
            foreach (var r in rends)
            {
                if (r == null) continue;
                var b = r.bounds;
                if (b.size.x < 300f && b.size.z < 300f) continue;
                sb.Append("- '").Append(r.gameObject.name).Append("' enabled=").Append(r.enabled)
                  .Append(" Y=").Append(b.center.y.ToString("F1")).Append("  머티리얼: ");
                AppendMats(sb, r.sharedMaterials);
                if (++big >= 20) { sb.Append("...(생략)\n"); break; }
            }
            if (big == 0) sb.Append("없음\n");

            // ── 조명 / 환경 ──
            sb.Append("\n[조명 / 환경]\n");
            sb.Append("Fog(안개): ").Append(RenderSettings.fog);
            if (RenderSettings.fog)
                sb.Append("  color=").Append(Col(RenderSettings.fogColor)).Append("  mode=").Append(RenderSettings.fogMode);
            sb.Append("\nAmbientMode(환경광): ").Append(RenderSettings.ambientMode)
              .Append("  ambientLight=").Append(Col(RenderSettings.ambientLight))
              .Append("  intensity=").Append(RenderSettings.ambientIntensity.ToString("F2")).Append("\n");
            if (RenderSettings.skybox != null)
                sb.Append("Skybox 머티리얼: ").Append(RenderSettings.skybox.name)
                  .Append(" (shader=").Append(RenderSettings.skybox.shader != null ? RenderSettings.skybox.shader.name : "null").Append(")\n");

            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            sb.Append("Light 개수: ").Append(lights.Length).Append("\n");
            foreach (var l in lights)
            {
                if (l == null) continue;
                sb.Append("  - ").Append(l.type).Append(" color=").Append(Col(l.color))
                  .Append(" intensity=").Append(l.intensity.ToString("F2"))
                  .Append(" on=").Append(l.enabled && l.gameObject.activeInHierarchy).Append("\n");
            }

            // URP 후처리 Volume 존재 여부 (색보정 의심)
            var volumes = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            int volCount = 0;
            foreach (var m in volumes)
                if (m != null && m.GetType().Name == "Volume") volCount++;
            sb.Append("후처리 Volume 컴포넌트: ").Append(volCount).Append("개 (있으면 색보정 의심)\n");

            Debug.Log(sb.ToString());
        }

        private static string Col(Color c) =>
            "(" + c.r.ToString("F2") + "," + c.g.ToString("F2") + "," + c.b.ToString("F2") + ")";

        private static void AppendMats(StringBuilder sb, Material[] mats)
        {
            if (mats == null || mats.Length == 0) { sb.Append("(없음)\n"); return; }
            sb.Append(mats.Length).Append("개 [");
            for (int i = 0; i < mats.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                var m = mats[i];
                if (m == null) { sb.Append("null"); continue; }
                Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor")
                        : (m.HasProperty("_Color") ? m.GetColor("_Color") : Color.magenta);
                sb.Append(m.name).Append(Col(c));
            }
            sb.Append("]\n");
        }
    }
}
