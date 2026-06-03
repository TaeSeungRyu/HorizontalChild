using System.Collections.Generic;
using System.IO;
using Game.World;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    /// <summary>
    /// M3 — Natural Earth GeoJSON (1:110m land) 을 한 번 읽어
    /// 단일 Mesh + Material + Prefab Asset 으로 굽는다 (Editor only).
    ///
    /// 결과물:
    ///   Assets/Game/Art/Map/WorldLand.mesh     — 모든 대륙 합본 메쉬 (extrude 두께)
    ///   Assets/Game/Art/Map/WorldLand.mat      — 단색 갈색 URP Lit 머티리얼
    ///   Assets/Game/Art/Map/WorldLand.prefab   — MeshFilter + MeshRenderer 포함
    ///
    /// 사용자는 Prefab 을 한 번 씬에 드래그하면 끝. 런타임 파싱·삼각화 없음.
    ///
    /// 메뉴:
    ///   Game ▸ Bake World Land Mesh from GeoJSON
    /// </summary>
    public static class M3WorldMeshBaker
    {
        private const string GeoJsonPath = "Assets/Game/Art/Map/ne_110m_land.geojson";
        private const string MeshPath    = "Assets/Game/Art/Map/WorldLand.mesh";
        private const string MaterialPath = "Assets/Game/Art/Map/WorldLand.mat";
        private const string PrefabPath  = "Assets/Game/Art/Map/WorldLand.prefab";

        // 추천 기본값 (사용자 결정 사항)
        private const float ExtrudeHeight = 2.5f;            // 살짝 솟은 두께
        private const float BaseY = 0.15f;                    // 대륙 바닥 (SeaPlane y = -0.05 보다 살짝 위)
        private static readonly Color LandColor = new Color(0.62f, 0.54f, 0.38f); // 부드러운 갈색

        // Date line 처리 — 한 변이 경도로 이만큼 이상 점프하면 폴리곤 스킵
        private const float DatelineEdgeThresholdDeg = 180f;

        [MenuItem("Game/Bake World Land Mesh from GeoJSON")]
        public static void Bake()
        {
            if (!File.Exists(GeoJsonPath))
            {
                EditorUtility.DisplayDialog(
                    "Bake World Land",
                    $"GeoJSON 을 찾을 수 없습니다:\n{GeoJsonPath}\n\nne_110m_land.geojson 을 해당 경로에 넣어주세요.",
                    "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Bake World Land", "GeoJSON 파싱 중...", 0.1f);

            try
            {
                var rings = LoadAllRings(GeoJsonPath, out int featureCount, out int skipped);
                if (rings.Count == 0)
                {
                    EditorUtility.DisplayDialog("Bake World Land", "유효한 폴리곤이 없습니다.", "OK");
                    return;
                }

                EditorUtility.DisplayProgressBar("Bake World Land", "삼각화 + 메쉬 생성...", 0.5f);
                var mesh = BuildExtrudedMesh(rings);

                EditorUtility.DisplayProgressBar("Bake World Land", "Asset 저장 중...", 0.85f);
                SaveMeshAsset(mesh);
                var material = CreateOrUpdateMaterial();
                CreateOrUpdatePrefab(mesh, material);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    $"[M3WorldMeshBaker] 완료.\n" +
                    $"  • Features 읽음: {featureCount} (스킵 {skipped})\n" +
                    $"  • 폴리곤 ring: {rings.Count}\n" +
                    $"  • 정점: {mesh.vertexCount}, 삼각형: {mesh.triangles.Length / 3}\n" +
                    $"  • Asset: {MeshPath} / {MaterialPath} / {PrefabPath}\n" +
                    $"  • 다음 단계: Prefab 을 씬에 드래그.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ─── GeoJSON 로드 ──────────────────────────────────────────────────

        private static List<Vector2[]> LoadAllRings(string path, out int featureCount, out int skipped)
        {
            var rings = new List<Vector2[]>();
            featureCount = 0;
            skipped = 0;

            var json = File.ReadAllText(path);
            var root = JObject.Parse(json);
            var features = root["features"] as JArray;
            if (features == null) return rings;

            featureCount = features.Count;

            foreach (var feature in features)
            {
                var geom = feature["geometry"];
                if (geom == null) continue;

                var type = (string)geom["type"];
                var coords = geom["coordinates"] as JArray;
                if (coords == null) continue;

                if (type == "Polygon")
                {
                    // coordinates: [outerRing, hole1, hole2, ...]
                    if (coords.Count > 0 && TryParseRing(coords[0] as JArray, out var ring))
                    {
                        rings.Add(ring);
                    }
                    else
                    {
                        skipped++;
                    }
                }
                else if (type == "MultiPolygon")
                {
                    // coordinates: [[outerRing, holes...], [outerRing, holes...], ...]
                    foreach (var poly in coords)
                    {
                        var polyArr = poly as JArray;
                        if (polyArr == null || polyArr.Count == 0) { skipped++; continue; }
                        if (TryParseRing(polyArr[0] as JArray, out var ring))
                        {
                            rings.Add(ring);
                        }
                        else
                        {
                            skipped++;
                        }
                    }
                }
            }

            return rings;
        }

        private static bool TryParseRing(JArray ringArr, out Vector2[] ring)
        {
            ring = null;
            if (ringArr == null || ringArr.Count < 4) return false;

            // 마지막 점이 첫 점과 같음 (GeoJSON 닫힌 ring) → 마지막 제외
            int count = ringArr.Count - 1;
            var pts = new Vector2[count];

            for (int i = 0; i < count; i++)
            {
                var pt = ringArr[i] as JArray;
                if (pt == null || pt.Count < 2) return false;
                float lng = (float)pt[0];
                float lat = (float)pt[1];
                pts[i] = new Vector2(lng, lat);
            }

            // Date line 횡단 폴리곤 스킵 (한 변이 180° 이상 점프)
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                if (Mathf.Abs(pts[next].x - pts[i].x) > DatelineEdgeThresholdDeg)
                {
                    return false;
                }
            }

            ring = pts;
            return true;
        }

        // ─── Mesh 빌드 ─────────────────────────────────────────────────────

        private static Mesh BuildExtrudedMesh(List<Vector2[]> rings)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();

            foreach (var ring in rings)
            {
                var triIdx = EarClippingTriangulator.Triangulate(ring);
                if (triIdx.Count == 0) continue;

                int baseIndex = verts.Count;
                int n = ring.Length;

                // Top vertices (y = BaseY + ExtrudeHeight)
                for (int i = 0; i < n; i++)
                {
                    var world = GeoCoordinate.LatLngToWorld(ring[i].y, ring[i].x);
                    verts.Add(new Vector3(world.x, BaseY + ExtrudeHeight, world.z));
                }
                // Bottom vertices (y = BaseY)
                for (int i = 0; i < n; i++)
                {
                    var world = GeoCoordinate.LatLngToWorld(ring[i].y, ring[i].x);
                    verts.Add(new Vector3(world.x, BaseY, world.z));
                }

                // Top triangles (CCW from above)
                for (int i = 0; i < triIdx.Count; i++)
                {
                    tris.Add(baseIndex + triIdx[i]);
                }

                // Bottom triangles (reverse winding so normal faces down)
                for (int i = 0; i < triIdx.Count; i += 3)
                {
                    tris.Add(baseIndex + n + triIdx[i]);
                    tris.Add(baseIndex + n + triIdx[i + 2]);
                    tris.Add(baseIndex + n + triIdx[i + 1]);
                }

                // Side walls — for each edge of the ring, two triangles outward
                // Top is CCW from above; outward normal is to the right when walking CCW.
                for (int i = 0; i < n; i++)
                {
                    int curr = i;
                    int next = (i + 1) % n;
                    int tCurr = baseIndex + curr;
                    int tNext = baseIndex + next;
                    int bCurr = baseIndex + n + curr;
                    int bNext = baseIndex + n + next;

                    tris.Add(tCurr);
                    tris.Add(bCurr);
                    tris.Add(tNext);

                    tris.Add(tNext);
                    tris.Add(bCurr);
                    tris.Add(bNext);
                }
            }

            var mesh = new Mesh
            {
                name = "WorldLand",
                indexFormat = verts.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ─── Asset 저장 ────────────────────────────────────────────────────

        private static void SaveMeshAsset(Mesh mesh)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (existing != null)
            {
                // 기존 mesh 의 내용을 갱신 — Prefab 참조 유지
                existing.Clear();
                existing.indexFormat = mesh.indexFormat;
                existing.SetVertices(mesh.vertices);
                existing.SetTriangles(mesh.triangles, 0);
                existing.RecalculateNormals();
                existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, MeshPath);
            }
        }

        private static Material CreateOrUpdateMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.name = "WorldLand";
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", LandColor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", LandColor);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.1f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateOrUpdatePrefab(Mesh mesh, Material material)
        {
            // 임시 GameObject 만들고 Prefab 저장 후 파괴
            var go = new GameObject("WorldLand");
            var mf = go.AddComponent<MeshFilter>();
            var meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            mf.sharedMesh = meshAsset != null ? meshAsset : mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
        }
    }
}
