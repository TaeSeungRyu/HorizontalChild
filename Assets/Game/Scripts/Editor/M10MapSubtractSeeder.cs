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
            //     (Malacca / Skagerrak 는 사용자 요청에 따라 제거. 사용자가 만든 SO 는 보존)
            DeleteIfExists($"{DataRoot}/MapSubtracts/MapSubtract_Malacca.asset");
            DeleteIfExists($"{DataRoot}/MapSubtracts/MapSubtract_Skagerrak.asset");

            // ─── 1) 나일강 + 아마존강 SO 생성/갱신 (기존 사용자 SO 는 안 건드림) ───
            CreateOrLoad("MapSubtract_Nile.asset", d =>
            {
                d.subtractId = "subtract.nile";
                d.displayNameKo = "나일강";
                d.widthKm = 200f;   // 넓게 — 어린이가 헤매지 않게
                d.enabled = true;
                d.points = new[]
                {
                    // points: (longitude, latitude)
                    new Vector2(31.5f, 32.0f),  // 지중해 어귀 (다미에타)
                    new Vector2(31.2f, 30.5f),  // 카이로 북쪽
                    new Vector2(31.2f, 29.0f),  // 카이로 남쪽
                    new Vector2(31.5f, 27.0f),  // 베니수에프
                    new Vector2(32.0f, 25.7f),  // 룩소르
                    new Vector2(32.9f, 24.1f),  // 아스완
                    new Vector2(32.5f, 21.0f),  // 누비아 사막
                    new Vector2(32.5f, 18.0f),  // 동골라
                    new Vector2(32.6f, 15.6f),  // 카르툼 (백/청 합류)
                };
                d.notes = "지중해 → 카이로 → 룩소르 → 카르툼. 폭 200km — 배가 충분히 통과.";
            });

            CreateOrLoad("MapSubtract_Amazon.asset", d =>
            {
                d.subtractId = "subtract.amazon";
                d.displayNameKo = "아마존강";
                d.widthKm = 250f;   // 더 넓게 — 본류는 진짜 100km 이상
                d.enabled = true;
                d.points = new[]
                {
                    new Vector2(-48.5f, -0.5f),  // 대서양 어귀 (벨렘 부근)
                    new Vector2(-52.0f, -1.5f),
                    new Vector2(-55.5f, -2.4f),  // 산타렘
                    new Vector2(-58.5f, -3.0f),
                    new Vector2(-60.0f, -3.1f),  // 마나우스
                    new Vector2(-63.0f, -3.5f),
                    new Vector2(-66.0f, -3.8f),
                    new Vector2(-70.0f, -4.0f),
                    new Vector2(-73.2f, -3.7f),  // 이키토스 (페루)
                };
                d.notes = "대서양 → 마나우스 → 이키토스. 폭 250km — 대항해시대 2 처럼 강 안쪽까지 항해.";
            });

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
