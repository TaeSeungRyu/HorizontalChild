using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// M3 — CatBorg 의 99_Sea 프리팹을 우리 세계 크기에 맞춰 깔기.
    ///
    /// 동작:
    ///   1) 99_Sea.prefab 의 메쉬 bounds 측정
    ///   2) 세계 크기 (5400 × 2700) 를 덮을 만큼 격자로 인스턴스 배치
    ///   3) 부모 GameObject "CatBorgSea" 아래로 모음
    ///   4) 기존 SeaPlane 자동 비활성화 (시각 충돌 방지)
    ///   5) 모든 타일을 isStatic 처리 → 정적 배칭 가능
    ///
    /// 메뉴: Game ▸ Apply CatBorg Sea
    ///
    /// 재실행 안전 — 기존 CatBorgSea 자식 다 지우고 새로 만듦.
    /// </summary>
    public static class M3CatBorgSeaSeeder
    {
        private const string PrefabPath =
            "Assets/catborg studio kit/CatBorg Studio/3D Pirates Lowpoly Pack/Prefabs/99_Sea.prefab";

        private const string ContainerName = "CatBorgSea";
        private const string SeaPlaneName = "SeaPlane";

        // 세계 크기 — GeoCoordinate.WorldWidthUnits 와 일치
        private const float WorldHalfWidth = 2700f;   // x ∈ [-2700, 2700]
        private const float WorldHalfDepth = 1350f;   // z ∈ [-1350, 1350]
        // 99_Sea 메쉬의 파도 정점이 위로 솟아 있어서 충분히 낮춰야 WorldLand(BaseY=1.25)를 안 덮음.
        // -5 = 파도가 위로 솟아도 y=0 근처에서 마무리.
        private const float SeaY = -5f;

        [MenuItem("Game/Apply CatBorg Sea")]
        public static void ApplyCatBorgSea()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("CatBorg Sea",
                    $"프리팹을 찾을 수 없습니다:\n{PrefabPath}", "OK");
                return;
            }

            // 메쉬 bounds 측정
            var meshFilter = prefab.GetComponentInChildren<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("CatBorg Sea",
                    "99_Sea 프리팹에 MeshFilter 또는 sharedMesh 가 없습니다.", "OK");
                return;
            }
            var meshSize = meshFilter.sharedMesh.bounds.size;
            float tileW = meshSize.x;
            float tileD = meshSize.z;
            if (tileW < 0.01f || tileD < 0.01f)
            {
                EditorUtility.DisplayDialog("CatBorg Sea",
                    $"메쉬 크기가 비정상: ({tileW:F2}, {tileD:F2})", "OK");
                return;
            }

            // 기존 CatBorgSea 컨테이너 정리
            var existing = GameObject.Find(ContainerName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            // 새 컨테이너
            var container = new GameObject(ContainerName);
            container.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(container, "Apply CatBorg Sea");

            // 격자로 타일 배치 — 약간의 overlap (1 unit) 으로 이음매 가림
            int tilesX = Mathf.CeilToInt((WorldHalfWidth * 2f) / tileW);
            int tilesZ = Mathf.CeilToInt((WorldHalfDepth * 2f) / tileD);

            float startX = -WorldHalfWidth + tileW * 0.5f;
            float startZ = -WorldHalfDepth + tileD * 0.5f;

            int count = 0;
            for (int i = 0; i < tilesX; i++)
            {
                for (int j = 0; j < tilesZ; j++)
                {
                    var tile = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    tile.transform.SetParent(container.transform);
                    tile.transform.localPosition = new Vector3(
                        startX + i * tileW,
                        SeaY,
                        startZ + j * tileD);
                    tile.transform.localRotation = Quaternion.identity;
                    tile.isStatic = true; // 정적 배칭 가능
                    count++;
                }
            }

            // 기존 SeaPlane 비활성화 (시각 충돌 방지)
            var seaPlane = GameObject.Find(SeaPlaneName);
            if (seaPlane != null)
            {
                seaPlane.SetActive(false);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log(
                $"[M3CatBorgSeaSeeder] 완료.\n" +
                $"  • Tile 메쉬 크기: ({tileW:F1}, {tileD:F1}) Unity Unit\n" +
                $"  • 격자: {tilesX} × {tilesZ} = {count} 타일\n" +
                $"  • 세계 영역: {WorldHalfWidth * 2f} × {WorldHalfDepth * 2f}\n" +
                $"  • SeaPlane 자동 비활성화 (미니맵은 EARTH.jpg 별도 사용해서 영향 없음)\n" +
                $"  • 되돌리려면 Ctrl+Z 또는 CatBorgSea 삭제 + SeaPlane 활성화");
        }

        [MenuItem("Game/Revert CatBorg Sea")]
        public static void RevertCatBorgSea()
        {
            var existing = GameObject.Find(ContainerName);
            if (existing != null) Object.DestroyImmediate(existing);

            var seaPlane = GameObject.Find(SeaPlaneName);
            if (seaPlane != null) seaPlane.SetActive(true);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[M3CatBorgSeaSeeder] 되돌림 — CatBorgSea 삭제 + SeaPlane 활성화.");
        }
    }
}
