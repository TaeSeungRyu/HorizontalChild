using System.Collections.Generic;
using System.IO;
using Game.Data;
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

        // 베이크 시 자동 로드되는 카탈로그 — 등록된 모든 활성 subtract 영역이 메쉬에서 잘림.
        private const string MapSubtractCatalogPath = "Assets/Game/Data/_Catalogs/MapSubtractCatalog.asset";

        // 추천 기본값 (사용자 결정 사항)
        // ExtrudeHeight 가 크면 해안선이 "벽" 처럼 보임 (side wall 이 또렷).
        // 0.2 = 거의 평면 / 0.5 = 살짝 입체감 (권장) / 1.5+ = 벽 느낌.
        private const float ExtrudeHeight = 0.5f;
        // BaseY = 땅의 바닥 Y 좌표. 높을수록 땅이 바다 위로 떠 보임.
        // 0.15 = 바다 (-0.05) 바로 위 / 1.0 = 살짝 솟은 대륙 / 2.0+ = 둥둥 떠있는 섬 느낌.
        private const float BaseY = 1.25f;
        private static readonly Color LandColor = new Color(0.62f, 0.54f, 0.38f); // 부드러운 갈색

        // Date line 처리 — 한 변이 경도로 이만큼 이상 점프하면 폴리곤 스킵
        private const float DatelineEdgeThresholdDeg = 180f;

        // 테셀레이션 — 삼각형 변 길이가 이 값(Unity Unit) 을 넘으면 분할.
        // PhysX 경고가 500 이상에서 발생하므로 200~300 사이가 안전.
        // 작을수록 정점 ↑ / 충돌 안정성 ↑.
        private const float MaxEdgeWorldUnits = 200f;

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

                EditorUtility.DisplayProgressBar("Bake World Land", "Subtract 영역 로드...", 0.3f);
                var subtractPolys = LoadSubtractPolygonsWorld(out int subtractCount, out int subtractDisabled);

                EditorUtility.DisplayProgressBar("Bake World Land", "삼각화 + 메쉬 생성...", 0.5f);
                var mesh = BuildExtrudedMesh(rings, subtractPolys, out int droppedTris);

                EditorUtility.DisplayProgressBar("Bake World Land", "Asset 저장 중...", 0.85f);
                SaveMeshAsset(mesh);
                var material = CreateOrUpdateMaterial();
                CreateOrUpdatePrefab(mesh, material);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    $"[M3WorldMeshBaker] 완료.\n" +
                    $"  • Features 읽음: {featureCount} (Dateline 스킵 {skipped})\n" +
                    $"  • 폴리곤 ring: {rings.Count}\n" +
                    $"  • Subtract 영역: 활성 {subtractCount}, 비활성 {subtractDisabled} (제거된 삼각형 {droppedTris})\n" +
                    $"  • 정점: {mesh.vertexCount}, 삼각형: {mesh.triangles.Length / 3}\n" +
                    $"  • Asset: {MeshPath} / {MaterialPath} / {PrefabPath}\n" +
                    $"  • 다음 단계: Prefab 을 씬에 드래그.\n" +
                    $"  • 항해 카브(Ceuta 등) 는 런타임 WorldCarves 에서 처리 — 메쉬 변형 없음.");

                // BuildExtrudedMesh 안에서 카운트 한 skippedByTriangulation 는 로컬이라
                // 본 로그에선 접근 불가 — 위 LogWarning 으로 개별 표시됨
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

        // ─── Subtract 영역 로드 ────────────────────────────────────────────

        /// <summary>
        /// MapSubtractCatalog 의 활성 MapSubtractData 를 월드 좌표 XZ 폴리곤 리스트로 변환.
        /// 베이크 시 이 폴리곤들 안에 들어가는 삼각형 centroid 가 모두 제거됨.
        /// </summary>
        private static List<Vector2[]> LoadSubtractPolygonsWorld(out int enabled, out int disabled)
        {
            enabled = 0;
            disabled = 0;
            var result = new List<Vector2[]>();
            var catalog = AssetDatabase.LoadAssetAtPath<MapSubtractCatalog>(MapSubtractCatalogPath);
            if (catalog == null || catalog.all == null) return result;

            foreach (var d in catalog.all)
            {
                if (d == null) continue;
                if (!d.enabled) { disabled++; continue; }
                var polys = Game.World.MapSubtractGeometry.BuildSubtractPolygonsWorld(d);
                if (polys.Count > 0)
                {
                    enabled++;
                    result.AddRange(polys);
                }
            }
            return result;
        }

        // ─── Mesh 빌드 ─────────────────────────────────────────────────────

        private static Mesh BuildExtrudedMesh(
            List<Vector2[]> rings,
            List<Vector2[]> subtractPolys,
            out int droppedTris)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            int skippedByTriangulation = 0;
            droppedTris = 0;
            bool hasSubtract = subtractPolys != null && subtractPolys.Count > 0;

            foreach (var ring in rings)
            {
                // 1) 폴리곤 경계 변을 MaxEdgeWorldUnits 이내로 분할 (Side wall 도 작게 유지)
                var subRing = SubdividePolygonEdges(ring, MaxEdgeWorldUnits);

                int n = subRing.Length;

                // 2) Top vertices 만 우선 만들기 (이후 interior 분할로 더 추가됨)
                var topLocal = new List<Vector3>(n);
                for (int i = 0; i < n; i++)
                {
                    var w = GeoCoordinate.LatLngToWorld(subRing[i].y, subRing[i].x);
                    topLocal.Add(new Vector3(w.x, BaseY + ExtrudeHeight, w.z));
                }

                // 3) Ear-clipping 으로 top 삼각화
                var topTris = EarClippingTriangulator.Triangulate(subRing);
                if (topTris.Count == 0)
                {
                    skippedByTriangulation++;
                    // 누락 위치 확인용 — 폴리곤 bbox 출력
                    if (skippedByTriangulation <= 10)
                    {
                        float minLat = float.MaxValue, maxLat = float.MinValue;
                        float minLng = float.MaxValue, maxLng = float.MinValue;
                        foreach (var p in ring)
                        {
                            if (p.y < minLat) minLat = p.y;
                            if (p.y > maxLat) maxLat = p.y;
                            if (p.x < minLng) minLng = p.x;
                            if (p.x > maxLng) maxLng = p.x;
                        }
                        Debug.LogWarning(
                            $"[M3WorldMeshBaker] 삼각화 실패 — vertices={ring.Length}, " +
                            $"lat[{minLat:F1}~{maxLat:F1}], lng[{minLng:F1}~{maxLng:F1}]");
                    }
                    continue;
                }

                // 4) Interior 큰 삼각형 분할 (topLocal 에 새 정점 추가, topTris 인덱스 재배치)
                SubdivideLargeTriangles(topLocal, topTris, MaxEdgeWorldUnits);

                int topCount = topLocal.Count;
                int baseIndex = verts.Count;

                // 5) Top layer 정점
                for (int i = 0; i < topCount; i++) verts.Add(topLocal[i]);
                // 6) Bottom layer 정점 (Y 만 BaseY 로)
                for (int i = 0; i < topCount; i++)
                {
                    var v = topLocal[i];
                    verts.Add(new Vector3(v.x, BaseY, v.z));
                }

                // 7+8) Top + Bottom triangles — 삼각형 centroid 가 subtract 영역에 있으면 스킵
                //      삼각형 단위로 필터하면 cut edge 가 살짝 jagged 해지지만
                //      MaxEdgeWorldUnits 가 작아 시각적으로 거의 알아챌 수 없음.
                for (int t = 0; t < topTris.Count; t += 3)
                {
                    int ia = topTris[t], ib = topTris[t + 1], ic = topTris[t + 2];
                    if (hasSubtract)
                    {
                        var a = topLocal[ia]; var b = topLocal[ib]; var c = topLocal[ic];
                        var centroid = new Vector2(
                            (a.x + b.x + c.x) / 3f,
                            (a.z + b.z + c.z) / 3f);
                        if (Game.World.MapSubtractGeometry.PointInAny(centroid, subtractPolys))
                        {
                            droppedTris++;
                            continue;
                        }
                    }
                    // Top
                    tris.Add(baseIndex + ia);
                    tris.Add(baseIndex + ib);
                    tris.Add(baseIndex + ic);
                    // Bottom (winding 반전)
                    tris.Add(baseIndex + topCount + ia);
                    tris.Add(baseIndex + topCount + ic);
                    tris.Add(baseIndex + topCount + ib);
                }

                // 9) Side walls — boundary 정점을 별도 복제해서 사용
                //    top/bottom 면과 정점을 공유하면 RecalculateNormals 가 법선을 평균내서
                //    경계에 사선 normal → 조명이 밝기 띠처럼 보임 ("경계선이 솟아 보이는" 현상).
                //    별도 복제하면 각 면이 자기 normal 을 유지 → 깔끔한 직각 모서리.
                int sideTopStart = verts.Count;
                for (int i = 0; i < n; i++) verts.Add(topLocal[i]);
                int sideBotStart = verts.Count;
                for (int i = 0; i < n; i++)
                {
                    var v = topLocal[i];
                    verts.Add(new Vector3(v.x, BaseY, v.z));
                }

                for (int i = 0; i < n; i++)
                {
                    int next = (i + 1) % n;

                    // Side wall quad 의 중심이 subtract 영역에 있으면 스킵
                    if (hasSubtract)
                    {
                        var p0 = topLocal[i]; var p1 = topLocal[next];
                        var midXZ = new Vector2((p0.x + p1.x) * 0.5f, (p0.z + p1.z) * 0.5f);
                        if (Game.World.MapSubtractGeometry.PointInAny(midXZ, subtractPolys))
                        {
                            droppedTris += 2;
                            continue;
                        }
                    }

                    int tCurr = sideTopStart + i;
                    int tNext = sideTopStart + next;
                    int bCurr = sideBotStart + i;
                    int bNext = sideBotStart + next;

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

        // ─── 테셀레이션 ────────────────────────────────────────────────────

        /// <summary>
        /// 폴리곤 경계의 각 변을 maxWorldEdge(Unity Unit) 이내로 분할.
        /// lat/lng 좌표에서 작업하므로 1° = 15 unit 환산 적용.
        /// </summary>
        private static Vector2[] SubdividePolygonEdges(Vector2[] ring, float maxWorldEdge)
        {
            const float unitsPerDegree = 15f; // GeoCoordinate.WorldWidthUnits / 360
            var result = new List<Vector2>(ring.Length * 2);
            int n = ring.Length;

            for (int i = 0; i < n; i++)
            {
                var p = ring[i];
                var q = ring[(i + 1) % n];
                result.Add(p);

                float dx = (q.x - p.x) * unitsPerDegree;
                float dz = (q.y - p.y) * unitsPerDegree;
                float worldDist = Mathf.Sqrt(dx * dx + dz * dz);

                if (worldDist > maxWorldEdge)
                {
                    int splits = Mathf.CeilToInt(worldDist / maxWorldEdge) - 1;
                    for (int s = 1; s <= splits; s++)
                    {
                        float t = (float)s / (splits + 1);
                        result.Add(Vector2.Lerp(p, q, t));
                    }
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Top 삼각형 중 변 길이가 maxEdgeLength 를 넘는 것을 재귀 분할.
        /// 가장 긴 변의 중점을 새 정점으로 추가해 삼각형 1개를 2개로 쪼갬.
        /// CCW winding 유지.
        /// </summary>
        private static void SubdivideLargeTriangles(List<Vector3> verts, List<int> tris, float maxEdgeLength)
        {
            float sqMax = maxEdgeLength * maxEdgeLength;
            int safety = 200000; // 무한 루프 방어
            int i = 0;
            while (i < tris.Count && safety-- > 0)
            {
                int ia = tris[i], ib = tris[i + 1], ic = tris[i + 2];
                var a = verts[ia]; var b = verts[ib]; var c = verts[ic];

                // XZ 거리만 (Y 는 동일)
                float dxab = b.x - a.x, dzab = b.z - a.z;
                float dxbc = c.x - b.x, dzbc = c.z - b.z;
                float dxca = a.x - c.x, dzca = a.z - c.z;
                float dab = dxab * dxab + dzab * dzab;
                float dbc = dxbc * dxbc + dzbc * dzbc;
                float dca = dxca * dxca + dzca * dzca;

                if (dab <= sqMax && dbc <= sqMax && dca <= sqMax)
                {
                    i += 3;
                    continue;
                }

                int newIdx = verts.Count;
                if (dab >= dbc && dab >= dca)
                {
                    // Split AB midpoint → (ia, newIdx, ic) + (newIdx, ib, ic)
                    verts.Add(new Vector3((a.x + b.x) * 0.5f, a.y, (a.z + b.z) * 0.5f));
                    tris[i + 1] = newIdx;
                    tris.Add(newIdx); tris.Add(ib); tris.Add(ic);
                }
                else if (dbc >= dca)
                {
                    // Split BC midpoint → (ia, ib, newIdx) + (ia, newIdx, ic)
                    verts.Add(new Vector3((b.x + c.x) * 0.5f, b.y, (b.z + c.z) * 0.5f));
                    tris[i + 2] = newIdx;
                    tris.Add(ia); tris.Add(newIdx); tris.Add(ic);
                }
                else
                {
                    // Split CA midpoint → (ia, ib, newIdx) + (newIdx, ib, ic)
                    verts.Add(new Vector3((c.x + a.x) * 0.5f, c.y, (c.z + a.z) * 0.5f));
                    tris[i + 2] = newIdx;
                    tris.Add(newIdx); tris.Add(ib); tris.Add(ic);
                }
                // 현재 인덱스 그대로 — 분할된 삼각형도 재검사
            }
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
            var meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            var sharedMesh = meshAsset != null ? meshAsset : mesh;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = sharedMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // 진짜 해안선 충돌 — ShipController 의 OverlapSphere 가 이 콜라이더를 잡음
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = sharedMesh;
            mc.convex = false; // 비-볼록 — 정적 메쉬에서 OK

            // ShipController.IsLandAt 가 GetComponentInParent<Landmass>() 로 검사 — 단일 컴포넌트면 충분
            go.AddComponent<Game.World.Landmass>();

            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
        }
    }
}
