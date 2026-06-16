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
            if (FindAnyObjectByType<RiverOverlay>() != null) return;
            var go = new GameObject("RiverOverlay (Auto)");
            go.AddComponent<RiverOverlay>();
            // catalog 는 본 컴포넌트 Start 에서 자동 검색
        }

        private void Start()
        {
            if (catalog == null) catalog = FindCatalog();
            Refresh();
        }

        /// <summary>씬·프로젝트에서 MapSubtractCatalog 자동 검색.</summary>
        private static MapSubtractCatalog FindCatalog()
        {
            // 1) 이미 로드된 객체에서 찾기 (다른 컴포넌트가 참조 중이면 발견)
            var loaded = Resources.FindObjectsOfTypeAll<MapSubtractCatalog>();
            if (loaded != null && loaded.Length > 0) return loaded[0];

#if UNITY_EDITOR
            // 2) Editor — 프로젝트 전체 스캔
            var guids = UnityEditor.AssetDatabase.FindAssets("t:MapSubtractCatalog");
            if (guids.Length > 0)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<MapSubtractCatalog>(path);
            }
#endif

            // 3) Resources 폴더 fallback (빌드용)
            var fromResources = Resources.Load<MapSubtractCatalog>("MapSubtractCatalog");
            if (fromResources != null) return fromResources;

            Debug.LogWarning("[RiverOverlay] MapSubtractCatalog 를 못 찾음. Inspector 에 수동 할당 또는 Resources 폴더에 두기.");
            return null;
        }

        public void Refresh()
        {
            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear();
            RiverRegistry.Clear();

            var landmass = FindAnyObjectByType<Landmass>();
            Transform landT = landmass != null ? landmass.transform : null;
            RiverRegistry.SetLandTransform(landT);

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
                    visual = BuildRiverLine(d, landT);
                }
                else if (d.points != null && d.points.Length >= 3)
                {
                    // 폴리곤 — center-fan mesh (24각형 brush 등)
                    visual = BuildRiverPolygonMesh(d, landT);
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

        // ─── 폴리라인 강 → LineRenderer (폭 widthKm) ────────────────────────

        private GameObject BuildRiverLine(MapSubtractData d, Transform landT)
        {
            var go = new GameObject($"River_{d.displayNameKo ?? d.name}");
            go.transform.SetParent(transform);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.TransformZ;   // 라인 평면 = 객체 XY (아래에서 회전 적용해 XZ 와 일치)
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // local Z = world +Y → 라인은 XZ 평면에 평평

            float widthUnits = d.widthKm / GeoCoordinate.KmPerUnit;
            lr.startWidth = widthUnits;
            lr.endWidth = widthUnits;
            lr.numCapVertices = 8;
            lr.numCornerVertices = 8;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            lr.positionCount = d.points.Length;
            for (int i = 0; i < d.points.Length; i++)
            {
                var w = GeoCoordinate.LatLngToWorld(d.points[i].y, d.points[i].x);
                var local = new Vector3(w.x, overlayY, w.z);
                var world = landT != null ? landT.TransformPoint(local) : local;
                lr.SetPosition(i, world);
            }

            lr.material = CreateWaterMaterial();
            return go;
        }

        // ─── 폴리곤 강 → center-fan mesh ───────────────────────────────────

        private GameObject BuildRiverPolygonMesh(MapSubtractData d, Transform landT)
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
            if (landT != null)
                for (int i = 0; i < verts.Length; i++) verts[i] = landT.TransformPoint(verts[i]);

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
