using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// WorldLand.mesh 를 위도(기후대)별 "정점색" 으로 칠하고,
    /// 양면(Cull Off) Unlit 셰이더 머티리얼 하나를 입힌다.
    /// → 조명/노멀/뒷면 컬링과 무관하게 색이 보인다.
    ///
    /// 순서: Game ▸ Bake World Land Mesh  →  Game ▸ Colorize World Land by Biome
    /// 색/경계는 아래 Bands 만 고치면 바뀜.
    /// </summary>
    public static class WorldLandColorizer
    {
        private const string MeshPath   = "Assets/Game/Art/Map/WorldLand.mesh";
        private const string PrefabPath = "Assets/Game/Art/Map/WorldLand.prefab";
        private const string MatPath    = "Assets/Game/Art/Map/WorldLand_VertexColor.mat";
        private const string ShaderName = "Game/WorldLandVertexColor";
        private const float UnitsPerDegreeLat = 15f; // z = lat * 15

        // (|위도| 상한, 색).  위에서부터 검사 — 작은 위도부터.
        private static readonly (float maxAbsLat, Color color)[] Bands =
        {
            (15f,  new Color(0.22f, 0.43f, 0.18f)), // 적도 (진한 초록)
            (30f,  new Color(0.70f, 0.58f, 0.35f)), // 사막대 (갈색)
            (52f,  new Color(0.34f, 0.48f, 0.24f)), // 온대 (초록)
            (63f,  new Color(0.37f, 0.50f, 0.37f)), // 아한대 (초록)
            (72f,  new Color(0.64f, 0.72f, 0.62f)), // 극지 인접 (초록+흰)
            (999f, new Color(0.89f, 0.91f, 0.94f)), // 극지 (흰색)
        };

        private static Color ColorForLat(float lat)
        {
            float a = Mathf.Abs(lat);
            for (int i = 0; i < Bands.Length; i++)
                if (a < Bands[i].maxAbsLat) return Bands[i].color;
            return Bands[Bands.Length - 1].color;
        }

        [MenuItem("Game/Colorize World Land by Biome")]
        public static void Colorize()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (mesh == null)
            {
                EditorUtility.DisplayDialog("Colorize World Land",
                    "WorldLand.mesh 가 없습니다. 먼저 'Bake World Land Mesh from GeoJSON' 실행.", "OK");
                return;
            }

            // 1) 정점색 = 그 정점의 위도(z/15) → 기후대 색
            var verts = mesh.vertices;
            var colors = new Color[verts.Length];
            for (int i = 0; i < verts.Length; i++)
                colors[i] = ColorForLat(verts[i].z / UnitsPerDegreeLat);
            mesh.colors = colors;

            // 2) 서브메쉬를 1개로 합침 (정점색 단일 머티리얼)
            var allTris = new List<int>();
            for (int s = 0; s < mesh.subMeshCount; s++) allTris.AddRange(mesh.GetTriangles(s));
            mesh.subMeshCount = 1;
            mesh.SetTriangles(allTris, 0);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);

            // 3) 정점색 셰이더 머티리얼 (양면 Unlit)
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                EditorUtility.DisplayDialog("Colorize World Land",
                    "셰이더 '" + ShaderName + "' 를 찾을 수 없습니다.\n" +
                    "Assets/Game/Art/Map/WorldLandVertexColor.shader 가 컴파일됐는지 확인하세요(콘솔에 셰이더 에러 없는지).",
                    "OK");
                return;
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, MatPath); }
            else mat.shader = shader;
            mat.name = "WorldLand_VertexColor";
            EditorUtility.SetDirty(mat);

            var mats = new Material[] { mat };

            // 4) Prefab 적용
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
            {
                var root = PrefabUtility.LoadPrefabContents(PrefabPath);
                var mr = root.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterials = mats;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }

            // 5) 씬 인스턴스 적용
            int sceneUpdated = 0;
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (mf != null && mf.sharedMesh == mesh)
                {
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (mr != null) { mr.sharedMaterials = mats; EditorUtility.SetDirty(mr); sceneUpdated++; }
                }
            }
            if (sceneUpdated > 0)
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[WorldLandColorizer] 정점색 적용 완료. verts=" + verts.Length +
                      ", 씬 인스턴스 갱신=" + sceneUpdated + "개." +
                      "\n양면 Unlit 셰이더라 조명/노멀/뒷면과 무관하게 색이 보입니다. " +
                      "여전히 안 보이면 셰이더 컴파일 에러(콘솔)부터 확인하세요.");
        }
    }
}
