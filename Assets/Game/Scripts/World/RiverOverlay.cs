using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 강(River) 시각 오버레이 + RiverRegistry 등록.
    ///
    /// 폴리라인 강 (widthKm > 0): LineRenderer 한 개로 폭 있는 선 → 그린 경로 그대로 표현
    /// 폴리곤 강 (widthKm = 0): center-fan mesh
    ///
    /// 충돌은 RiverRegistry 에 rectangle polygons 등록 (기존과 동일).
    /// </summary>
    public class RiverOverlay : MonoBehaviour
    {
        [Header("Data")]
        public MapSubtractCatalog catalog;

        [Header("Visual")]
        [Tooltip("강이 그려질 Y 위치. Land top(≈1.75) 위로 살짝.")]
        public float overlayY = 2.0f;
        [Tooltip("강 물색.")]
        public Color waterColor = new Color(0.30f, 0.60f, 0.90f, 1f);

        private readonly List<GameObject> _spawned = new();

        /// <summary>
        /// 게임 시작마다 자동 부트스트랩 — RiverOverlay 가 씬에 없으면 만든다.
        /// 사용자가 에디터로 저장 후 Play 종료/재시작해도 강이 살아 있도록 보장.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (FindAnyObjectByType<RiverOverlay>() != null)
            {
                Debug.Log("[RiverOverlay] AutoBootstrap — 씬에 이미 존재. skip.");
                return;
            }
            var go = new GameObject("RiverOverlay (Auto)");
            go.AddComponent<RiverOverlay>();
            Debug.Log("[RiverOverlay] AutoBootstrap — RiverOverlay 자동 생성됨.");
        }

        private void Start()
        {
            if (catalog == null) catalog = FindCatalog();
            Refresh();
        }

        /// <summary>씬·프로젝트에서 MapSubtractCatalog 자동 검색.</summary>
        private static MapSubtractCatalog FindCatalog()
        {
            // 1) 씬의 MapSubtractEditor 가 참조 중이면 그것 사용 (가장 빠르고 신뢰성 높음)
            var editor = FindAnyObjectByType<MapSubtractEditor>();
            if (editor != null && editor.catalog != null)
            {
                Debug.Log($"[RiverOverlay] catalog — MapSubtractEditor 에서 발견: {editor.catalog.name}");
                return editor.catalog;
            }

            // 2) 이미 로드된 객체에서 찾기
            var loaded = Resources.FindObjectsOfTypeAll<MapSubtractCatalog>();
            if (loaded != null && loaded.Length > 0)
            {
                Debug.Log($"[RiverOverlay] catalog — 메모리에서 발견: {loaded[0].name}");
                return loaded[0];
            }

#if UNITY_EDITOR
            // 3) Editor — 프로젝트 전체 스캔
            var guids = UnityEditor.AssetDatabase.FindAssets("t:MapSubtractCatalog");
            if (guids.Length > 0)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                var c = UnityEditor.AssetDatabase.LoadAssetAtPath<MapSubtractCatalog>(path);
                Debug.Log($"[RiverOverlay] catalog — AssetDatabase 에서 발견: {path}");
                return c;
            }
#endif

            // 4) Resources 폴더 fallback (빌드용)
            var fromResources = Resources.Load<MapSubtractCatalog>("MapSubtractCatalog");
            if (fromResources != null)
            {
                Debug.Log("[RiverOverlay] catalog — Resources/MapSubtractCatalog 에서 발견");
                return fromResources;
            }

            Debug.LogWarning("[RiverOverlay] MapSubtractCatalog 를 못 찾음. Inspector 에 수동 할당 또는 Resources 폴더에 두기.");
            return null;
        }

        /// <summary>WorldLand 를 우선 찾고, 없으면 아무 Landmass 반환.</summary>
        private static Transform FindWorldLandTransform()
        {
            var all = FindObjectsByType<Landmass>(FindObjectsSortMode.None);
            // 1) 이름이 "WorldLand" 인 것 우선
            foreach (var lm in all)
            {
                if (lm == null) continue;
                if (lm.name == "WorldLand" || lm.name.StartsWith("WorldLand"))
                    return lm.transform;
            }
            // 2) Fallback — 첫 번째
            if (all.Length > 0 && all[0] != null) return all[0].transform;
            return null;
        }

        public void Refresh()
        {
            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear();
            RiverRegistry.Clear();

            // WorldLand identity transform 가정 — landT 사용 X.
            RiverRegistry.SetLandTransform(null);

            if (catalog == null || catalog.all == null)
            {
                Debug.Log("[RiverOverlay] catalog 비어 있음.");
                return;
            }

            int riverCount = 0;
            foreach (var d in catalog.all)
            {
                if (d == null || !d.enabled) continue;
                if (d.kind != MapEditKind.River) continue;

                // 충돌 영역은 항상 rectangle polygons 로 등록 (BuildSubtractPolygonsWorld)
                var polys = MapSubtractGeometry.BuildSubtractPolygonsWorld(d);
                foreach (var poly in polys) RiverRegistry.AddPolygon(poly);

                // 시각: 폴리라인이면 LineRenderer, 폴리곤이면 mesh
                GameObject visual;
                if (d.widthKm > 0f && d.points != null && d.points.Length >= 2)
                {
                    visual = BuildRiverLine(d);
                }
                else if (d.points != null && d.points.Length >= 3)
                {
                    visual = BuildRiverPolygonMesh(d);
                }
                else
                {
                    continue;
                }
                if (visual != null) _spawned.Add(visual);
                riverCount++;
            }

            Debug.Log($"[RiverOverlay] 강 {riverCount}개 등록 — Registry polys {RiverRegistry.Count}, 시각 GameObjects {_spawned.Count}");
        }

        // ─── 폴리라인 강 → quad-strip 메쉬 (segment 마다 사각형, 시각 = 충돌과 일치) ───

        private GameObject BuildRiverLine(MapSubtractData d)
        {
            // BuildSubtractPolygonsWorld 와 동일한 사각형들로 메쉬 빌드.
            // → 시각 영역과 RiverRegistry 충돌 영역이 정확히 같음.
            var polys = MapSubtractGeometry.BuildSubtractPolygonsWorld(d);
            if (polys.Count == 0) return null;

            var go = new GameObject($"River_{d.displayNameKo ?? d.name}");
            go.transform.SetParent(transform);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var verts = new List<Vector3>(polys.Count * 4);
            var tris  = new List<int>(polys.Count * 6);
            foreach (var poly in polys)
            {
                if (poly.Length < 4) continue;
                int baseIdx = verts.Count;
                for (int i = 0; i < 4; i++)
                {
                    verts.Add(new Vector3(poly[i].x, overlayY, poly[i].y));
                }
                // BuildSubtractPolygonsWorld 사각형 순서: TL(0), TR(1), BR(2), BL(3)
                // top-down 카메라에서 normal +Y 향하게 CCW from above — 0→1→3 + 1→2→3
                // (이전 0→3→1 은 -Y 향해 backface cull 됨)
                tris.Add(baseIdx + 0); tris.Add(baseIdx + 1); tris.Add(baseIdx + 3);
                tris.Add(baseIdx + 1); tris.Add(baseIdx + 2); tris.Add(baseIdx + 3);
            }

            var mesh = new Mesh { name = $"RiverMesh_{d.name}" };
            if (verts.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
            mr.sharedMaterial = CreateWaterMaterial();

            return go;
        }

        // ─── 폴리곤 강 → center-fan mesh ───────────────────────────────────

        private GameObject BuildRiverPolygonMesh(MapSubtractData d)
        {
            // 폴리곤 정점 (lat/lng) → mesh-local XZ
            var pts = new Vector3[d.points.Length];
            for (int i = 0; i < d.points.Length; i++)
            {
                var w = GeoCoordinate.LatLngToWorld(d.points[i].y, d.points[i].x);
                pts[i] = new Vector3(w.x, overlayY, w.z);
            }

            var go = new GameObject($"River_Poly_{d.displayNameKo ?? d.name}");
            go.transform.SetParent(transform);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            int n = pts.Length;
            var verts = new Vector3[n + 1];
            float cx = 0f, cz = 0f;
            for (int i = 0; i < n; i++) { cx += pts[i].x; cz += pts[i].z; }
            cx /= n; cz /= n;
            verts[0] = new Vector3(cx, overlayY, cz);
            for (int i = 0; i < n; i++) verts[i + 1] = pts[i];

            // LandTransform 적용
            // CCW winding (위에서 +Y normal)
            var tris = new int[n * 3];
            for (int i = 0; i < n; i++)
            {
                tris[i * 3 + 0] = 0;
                tris[i * 3 + 1] = ((i + 1) % n) + 1;
                tris[i * 3 + 2] = i + 1;
            }

            var mesh = new Mesh { name = $"RiverMesh_{d.name}" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
            mr.sharedMaterial = CreateWaterMaterial();
            return go;
        }

        private Material CreateWaterMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", waterColor);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", waterColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.7f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);   // 양면 렌더 안전망
            mat.doubleSidedGI = true;
            return mat;
        }
    }
}
