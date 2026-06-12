using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 구워진 WorldLand.mesh 를 위도(기후대)별로 색칠한다 (Editor only).
    ///
    /// M3WorldMeshBaker 로 Bake 한 뒤 이 메뉴를 실행하면:
    ///   - 윗면 삼각형을 위도에 따라 기후대(Biome)별 서브메쉬로 분리
    ///   - 측면/바닥은 별도 "해안" 서브메쉬
    ///   - 각 기후대 색의 URP/Lit 머티리얼을 만들어 Prefab 에 적용
    ///
    /// 색/경계는 아래 Biomes 배열과 BiomeForLat 만 고치면 바뀝니다.
    ///
    /// 메뉴: Game ▸ Colorize World Land by Biome
    /// 순서: 먼저 "Bake World Land Mesh", 그다음 이 메뉴.
    /// </summary>
    public static class WorldLandColorizer
    {
        private const string MeshPath   = "Assets/Game/Art/Map/WorldLand.mesh";
        private const string PrefabPath = "Assets/Game/Art/Map/WorldLand.prefab";
        private const string MatDir     = "Assets/Game/Art/Map/";

        // 위도 1° = 15 world unit (GeoCoordinate: z = lat/180 * 2700 = lat*15)
        private const float UnitsPerDegreeLat = 15f;

        // ─── 기후대 정의 (여기만 고치면 색/이름이 바뀜) ───────────────────
        private static readonly (string name, Color color)[] Biomes =
        {
            ("Tropical",  new Color(0.20f, 0.42f, 0.16f)), // 0 적도 우림 (진한 초록)
            ("Desert",    new Color(0.70f, 0.58f, 0.35f)), // 1 사막대 (갈색) ← 사하라·아라비아·호주
            ("Temperate", new Color(0.33f, 0.47f, 0.22f)), // 2 온대 (초록) ← 유럽·미국·동아시아
            ("Boreal",    new Color(0.21f, 0.33f, 0.21f)), // 3 냉대 침엽수 (어두운 초록)
            ("Polar",     new Color(0.85f, 0.87f, 0.90f)), // 4 극지 (눈/회백)
        };
        private static readonly Color SideColor = new Color(0.34f, 0.27f, 0.17f); // 해안 절벽/측면

        // 위도 → 기후대 인덱스 (남/북 대칭). 경계값만 바꾸면 띠 폭이 달라짐.
        private static int BiomeForLat(float lat)
        {
            float a = Mathf.Abs(lat);
            if (a < 12f) return 0; // 적도
            if (a < 30f) return 1; // 사막대
            if (a < 50f) return 2; // 온대
            if (a < 63f) return 3; // 냉대
            return 4;              // 극지
        }

        [MenuItem("Game/Colorize World Land by Biome")]
        public static void Colorize()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (mesh == null)
            {
                EditorUtility.DisplayDialog("Colorize World Land",
                    "WorldLand.mesh 를 찾을 수 없습니다.\n먼저 'Bake World Land Mesh from GeoJSON' 을 실행하세요.", "OK");
                return;
            }

            var verts = mesh.vertices;

            // 현재 모든 서브메쉬의 삼각형을 하나로 모음 (재실행해도 동작)
            var allTris = new List<int>();
            for (int s = 0; s < mesh.subMeshCount; s++)
                allTris.AddRange(mesh.GetTriangles(s));

            // 윗면 판정용 최고 Y
            float topY = float.MinValue;
            for (int i = 0; i < verts.Length; i++) if (verts[i].y > topY) topY = verts[i].y;

            int biomeCount = Biomes.Length;
            var topByBiome = new List<int>[biomeCount];
            for (int b = 0; b < biomeCount; b++) topByBiome[b] = new List<int>();
            var sideTris = new List<int>();

            for (int t = 0; t < allTris.Count; t += 3)
            {
                int i0 = allTris[t], i1 = allTris[t + 1], i2 = allTris[t + 2];
                var v0 = verts[i0]; var v1 = verts[i1]; var v2 = verts[i2];

                bool isTop = v0.y > topY - 0.01f && v1.y > topY - 0.01f && v2.y > topY - 0.01f;
                if (isTop)
                {
                    float centroidZ = (v0.z + v1.z + v2.z) / 3f;
                    float lat = centroidZ / UnitsPerDegreeLat;
                    int b = BiomeForLat(lat);
                    topByBiome[b].Add(i0); topByBiome[b].Add(i1); topByBiome[b].Add(i2);
                }
                else
                {
                    sideTris.Add(i0); sideTris.Add(i1); sideTris.Add(i2);
                }
            }

            // 서브메쉬 재구성: 0..biomeCount-1 = 기후대, 마지막 = 측면/바닥
            mesh.subMeshCount = biomeCount + 1;
            for (int b = 0; b < biomeCount; b++)
                mesh.SetTriangles(topByBiome[b], b);
            mesh.SetTriangles(sideTris, biomeCount);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);

            // 머티리얼 생성/갱신
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mats = new Material[biomeCount + 1];
            for (int b = 0; b < biomeCount; b++)
                mats[b] = MakeMat("WorldLand_" + Biomes[b].name, Biomes[b].color, shader);
            mats[biomeCount] = MakeMat("WorldLand_Side", SideColor, shader);

            // Prefab 에 머티리얼 배열 적용
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
            {
                var root = PrefabUtility.LoadPrefabContents(PrefabPath);
                var mr = root.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterials = mats;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }

            // 열려있는 씬에 이미 놓인 WorldLand 인스턴스도 즉시 갱신
            // (Prefab 머티리얼 배열 변경이 인스턴스에 자동 반영 안 되는 경우 대비)
            int sceneUpdated = 0;
            var filters = Object.FindObjectsOfType<MeshFilter>();
            foreach (var mf in filters)
            {
                if (mf != null && mf.sharedMesh == mesh)
                {
                    var r = mf.GetComponent<MeshRenderer>();
                    if (r != null)
                    {
                        r.sharedMaterials = mats;
                        EditorUtility.SetDirty(r);
                        sceneUpdated++;
                    }
                }
            }
            if (sceneUpdated > 0)
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var sb = new System.Text.StringBuilder("[WorldLandColorizer] 완료 — 기후대별 삼각형: ");
            for (int b = 0; b < biomeCount; b++)
            {
                if (b > 0) sb.Append(", ");
                sb.Append(Biomes[b].name).Append("=").Append(topByBiome[b].Count / 3);
            }
            sb.Append(", 측면=").Append(sideTris.Count / 3);
            sb.Append("\n씬 인스턴스 갱신: ").Append(sceneUpdated).Append("개 (0이면 씬에 육지가 없거나 Prefab을 다시 드래그 필요).");
            Debug.Log(sb.ToString());
        }

        private static Material MakeMat(string name, Color color, Shader shader)
        {
            string path = MatDir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader) m.shader = shader;

            m.name = name;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
