using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 강(River) 시각 오버레이 + RiverRegistry 등록.
    ///
    /// 동작:
    ///   Start 에서 MapSubtractCatalog 의 활성 River SO 들을 읽어:
    ///     1) 각 영역을 파란색 평면 메쉬로 렌더 (육지 위, Y=overlayY)
    ///     2) RiverRegistry 에 polygon 등록 → 배가 영역 안에서 충돌 무시
    ///
    /// 폴리곤 모드 (widthKm=0) — center-fan 삼각화
    /// 폴리라인 모드 (widthKm>0) — 세그먼트마다 사각 띠
    ///
    /// 사용:
    ///   1) Hierarchy 빈 GameObject "RiverOverlay" 생성
    ///   2) 본 컴포넌트 부착
    ///   3) MapSubtractCatalog 할당
    /// </summary>
    public class RiverOverlay : MonoBehaviour
    {
        [Header("Data")]
        public MapSubtractCatalog catalog;

        [Header("Visual")]
        [Tooltip("강 메쉬가 그려질 Y 위치. Land top(≈1.75) 위로 살짝.")]
        public float overlayY = 1.85f;
        [Tooltip("강 물색 (URP Lit). 약간 청록·반투명.")]
        public Color waterColor = new Color(0.30f, 0.60f, 0.90f, 1f);

        private readonly List<GameObject> _spawned = new();

        private void Start()
        {
            Refresh();
        }

        /// <summary>외부에서 호출 가능 — 카탈로그가 변경되면 다시 빌드.</summary>
        public void Refresh()
        {
            // 이전 visuals 제거
            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear();
            RiverRegistry.Clear();

            // WorldLand transform 등록 — Registry 가 ship world → mesh-local 변환에 사용
            var landmass = FindAnyObjectByType<Landmass>();
            RiverRegistry.SetLandTransform(landmass != null ? landmass.transform : null);

            if (catalog == null || catalog.all == null)
            {
                Debug.Log("[RiverOverlay] catalog 비어 있음 — 강 영역 없음.");
                return;
            }

            int riverCount = 0;
            foreach (var d in catalog.all)
            {
                if (d == null || !d.enabled) continue;
                if (d.kind != MapEditKind.River) continue;

                var polys = MapSubtractGeometry.BuildSubtractPolygonsWorld(d);
                if (polys.Count == 0) continue;

                foreach (var poly in polys)
                {
                    // 1) 충돌 우회 등록
                    RiverRegistry.AddPolygon(poly);

                    // 2) 시각 메쉬 생성
                    var go = BuildRiverMesh(poly, d.displayNameKo ?? d.name);
                    if (go != null) _spawned.Add(go);
                }
                riverCount++;
            }

            Debug.Log($"[RiverOverlay] 강 {riverCount}개 등록 — Registry polys {RiverRegistry.Count}, 시각 GameObjects {_spawned.Count}");
        }

        private GameObject BuildRiverMesh(Vector2[] polyXZ, string name)
        {
            if (polyXZ == null || polyXZ.Length < 3) return null;

            var go = new GameObject($"River_{name}");
            go.transform.SetParent(transform);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // WorldLand offset 보정 — 강이 메쉬 정점과 같은 world 위치에 보이도록
            var landmass = FindAnyObjectByType<Landmass>();
            Transform landT = landmass != null ? landmass.transform : null;

            var mesh = new Mesh { name = $"RiverMesh_{name}" };
            int n = polyXZ.Length;
            var verts = new Vector3[n + 1];
            verts[0] = ApplyLandTransform(ComputeCentroidLocal(polyXZ), landT);
            for (int i = 0; i < n; i++)
            {
                var local = new Vector3(polyXZ[i].x, overlayY, polyXZ[i].y);
                verts[i + 1] = ApplyLandTransform(local, landT);
            }

            // Center-fan 삼각화 — winding 을 위에서 봤을 때 CCW 로 (normal 이 +Y 향하게).
            // 이전 winding (0, i+1, (i+1)%n + 1) 는 -Y 향해서 top-down 카메라가 backface cull.
            var tris = new int[n * 3];
            for (int i = 0; i < n; i++)
            {
                tris[i * 3 + 0] = 0;
                tris[i * 3 + 1] = ((i + 1) % n) + 1;
                tris[i * 3 + 2] = i + 1;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            // 머티리얼 — URP Lit, 푸른 물색 + 양면 렌더 (winding 안 맞아도 안전)
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", waterColor);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", waterColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.7f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            // 양면 렌더 (Cull Off) — winding 오류·기울임 등 안전망
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            mat.doubleSidedGI = true;
            mr.sharedMaterial = mat;

            return go;
        }

        private static Vector3 ApplyLandTransform(Vector3 local, Transform landT)
        {
            return landT != null ? landT.TransformPoint(local) : local;
        }

        private Vector3 ComputeCentroidLocal(Vector2[] poly)
        {
            float sx = 0f, sz = 0f;
            for (int i = 0; i < poly.Length; i++)
            {
                sx += poly[i].x;
                sz += poly[i].y;
            }
            float inv = 1f / poly.Length;
            return new Vector3(sx * inv, overlayY, sz * inv);
        }

    }
}
