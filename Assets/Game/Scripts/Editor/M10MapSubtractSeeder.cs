using System;
using System.Collections.Generic;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M10 — MapSubtractCatalog + 나일·아마존 두 큰 강 시드.
    /// 대항해시대 2 처럼 배가 강 안쪽까지 항해할 수 있도록 넓고 부드러운 경로.
    ///
    /// 두 강:
    ///   - 나일강 (지중해 → 카이로 → 룩소르 → 카르툼, 폭 200km)
    ///   - 아마존강 (대서양 → 마나우스 → 이키토스, 폭 250km)
    ///
    /// 메뉴: Game/Seed M10 Map Subtracts
    ///
    /// 사용자가 에디터로 추가한 영역은 보존됨 (폴더 스캔으로 카탈로그 채움).
    /// 시드 후 Game ▸ Bake World Land Mesh from GeoJSON 실행.
    /// </summary>
    public static class M10MapSubtractSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Revert M10 Map Subtracts (Undo Carving)")]
        public static void Revert()
        {
            if (!EditorUtility.DisplayDialog(
                    "Revert Map Subtracts",
                    "모든 카브 영역(나일·아마존 등)을 삭제하고 메쉬를 원본으로 되돌립니다.\n계속할까요?",
                    "되돌리기", "취소"))
            {
                return;
            }

            // 1) 카브 SO 모두 삭제 (사용자가 직접 만든 것도 포함)
            int deleted = 0;
            if (AssetDatabase.IsValidFolder($"{DataRoot}/MapSubtracts"))
            {
                var guids = AssetDatabase.FindAssets("t:MapSubtractData",
                    new[] { $"{DataRoot}/MapSubtracts" });
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    AssetDatabase.DeleteAsset(path);
                    deleted++;
                }
            }

            // 2) 카탈로그 비움
            var catalogPath = $"{DataRoot}/_Catalogs/MapSubtractCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<MapSubtractCatalog>(catalogPath);
            if (catalog != null)
            {
                catalog.all = new MapSubtractData[0];
                EditorUtility.SetDirty(catalog);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3) 메쉬 재베이크 → 원본 복원
            bool ok = EditorApplication.ExecuteMenuItem("Game/Bake World Land Mesh from GeoJSON");

            Debug.Log(
                $"[M10MapSubtractSeeder] 되돌림 완료. 삭제된 카브 {deleted}개. " +
                (ok ? "메쉬 재베이크 완료 — 원본 지도 복원." : "메쉬 재베이크 실패 — Game ▸ Bake World Land 수동 실행."));
        }

        [MenuItem("Game/Seed M10 Map Subtracts")]
        public static void Seed()
        {
            EnsureFolder($"{DataRoot}/MapSubtracts");
            EnsureFolder($"{DataRoot}/_Catalogs");

            // ─── 0) 이전 시드의 사용 안 하는 SO 정리 ─────────────────────────
            //     사용자가 모양이 좋지 않다고 한 시드 강(나일·아마존·북해통로 등) 모두 제거.
            //     사용자가 에디터로 직접 그린 SO 는 다른 이름이라 보존됨.
            DeleteIfExists($"{DataRoot}/MapSubtracts/MapSubtract_Malacca.asset");
            DeleteIfExists($"{DataRoot}/MapSubtracts/MapSubtract_Skagerrak.asset");
            DeleteIfExists($"{DataRoot}/MapSubtracts/MapSubtract_Nile.asset");
            DeleteIfExists($"{DataRoot}/MapSubtracts/MapSubtract_Amazon.asset");
            DeleteIfExists($"{DataRoot}/MapSubtracts/MapSubtract_NorthSeaPassage.asset");

            // ─── 1) 시드 강 없음 — 사용자가 직접 에디터로 그리도록 ─────────
            //     (이전엔 Nile/Amazon/NorthSea 를 seed 했으나 모양이 어색하다는 피드백으로 제거)

            // ─── 2) 카탈로그 — 폴더 스캔으로 모든 MapSubtractData 등록 ─────────
            //     (사용자가 에디터로 만든 SO 도 자동 포함됨)
            var catalogPath = $"{DataRoot}/_Catalogs/MapSubtractCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<MapSubtractCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MapSubtractCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
                Debug.Log($"[M10MapSubtractSeeder] 카탈로그 생성: {catalogPath}");
            }

            var allFound = new List<MapSubtractData>();
            var guids = AssetDatabase.FindAssets("t:MapSubtractData",
                new[] { $"{DataRoot}/MapSubtracts" });
            foreach (var g in guids)
            {
                var d = AssetDatabase.LoadAssetAtPath<MapSubtractData>(
                    AssetDatabase.GUIDToAssetPath(g));
                if (d != null) allFound.Add(d);
            }
            allFound.Sort((a, b) => string.Compare(
                AssetDatabase.GetAssetPath(a),
                AssetDatabase.GetAssetPath(b),
                StringComparison.Ordinal));

            catalog.all = allFound.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[M10MapSubtractSeeder] 완료. 카탈로그 → {allFound.Count}개 영역 등록.\n" +
                "  • 나일강 폭 200km / 아마존강 폭 250km (대항해시대 2 풍)\n" +
                "  • 북해↔발트해 통로 폭 300km (네덜란드 동쪽 막힘 해결)\n" +
                "\n다음:\n" +
                "  → Game ▸ Bake World Land Mesh from GeoJSON  — 메쉬 재베이크 (5~10초)\n" +
                "  → Play 모드에서 나일강·아마존강으로 배 진입 가능!");
        }

        // ─── 헬퍼 ───────────────────────────────────────────────────────────

        private static MapSubtractData CreateOrLoad(string fileName, Action<MapSubtractData> setup)
        {
            var path = $"{DataRoot}/MapSubtracts/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<MapSubtractData>(path);
            if (existing != null)
            {
                setup(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            var so = ScriptableObject.CreateInstance<MapSubtractData>();
            setup(so);
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void DeleteIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<MapSubtractData>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"[M10MapSubtractSeeder] 제거됨: {path}");
            }
        }
    }
}
