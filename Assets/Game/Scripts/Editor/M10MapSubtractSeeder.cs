using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M10 — MapSubtractCatalog + 예시 영역 4개 시드.
    /// 베이커는 이 카탈로그를 자동 로드. 사용자는 Editor 도구로 추가/삭제 가능.
    ///
    /// 예시 4개:
    ///   - 나일강 (이집트 → 에티오피아 방향, 폭 80km)
    ///   - 아마존강 (대서양 → 페루 방향, 폭 120km)
    ///   - 말라카 해협 (안다만 → 싱가포르, 폭 150km)
    ///   - 스카게라크 (북해 → 발트해 입구, 폭 180km)
    ///
    /// 메뉴: Game/Seed M10 Map Subtracts
    /// 시드 후 Game ▸ Bake World Land Mesh from GeoJSON 실행.
    /// </summary>
    public static class M10MapSubtractSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M10 Map Subtracts")]
        public static void Seed()
        {
            EnsureFolder($"{DataRoot}/MapSubtracts");
            EnsureFolder($"{DataRoot}/_Catalogs");

            // 카탈로그 — 없으면 생성
            var catalogPath = $"{DataRoot}/_Catalogs/MapSubtractCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<MapSubtractCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MapSubtractCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
                Debug.Log($"[M10MapSubtractSeeder] 카탈로그 생성: {catalogPath}");
            }

            // 예시 4개 — points 는 (longitude, latitude)
            var entries = new System.Collections.Generic.List<MapSubtractData>();

            entries.Add(CreateOrLoad("MapSubtract_Nile.asset", d =>
            {
                d.subtractId = "subtract.nile";
                d.displayNameKo = "나일강";
                d.widthKm = 80f;
                d.points = new[]
                {
                    new Vector2(30.0f, 31.2f),  // 지중해 삼각주
                    new Vector2(31.0f, 26.0f),  // 룩소르 부근
                    new Vector2(32.5f, 22.0f),  // 누비아
                    new Vector2(32.5f, 15.5f),  // 카르툼
                    new Vector2(35.0f, 12.0f),  // 에티오피아 (타나 호)
                };
                d.notes = "지중해 → 이집트 → 수단 → 에티오피아. 실제 폭은 좁지만 항행성을 위해 80km.";
            }));

            entries.Add(CreateOrLoad("MapSubtract_Amazon.asset", d =>
            {
                d.subtractId = "subtract.amazon";
                d.displayNameKo = "아마존강";
                d.widthKm = 120f;
                d.points = new[]
                {
                    new Vector2(-50.0f, -0.5f),  // 대서양 어귀
                    new Vector2(-58.0f, -2.0f),
                    new Vector2(-65.0f, -3.5f),  // 마나우스 부근
                    new Vector2(-72.0f, -4.5f),
                    new Vector2(-78.0f, -6.0f),  // 페루 국경
                };
                d.notes = "대서양 → 페루. 폭 120km (실제 아마존도 100km 이상).";
            }));

            entries.Add(CreateOrLoad("MapSubtract_Malacca.asset", d =>
            {
                d.subtractId = "subtract.malacca";
                d.displayNameKo = "말라카 해협";
                d.widthKm = 150f;
                d.points = new[]
                {
                    new Vector2(98.5f, 6.0f),    // 북서 (안다만해 쪽)
                    new Vector2(101.0f, 3.0f),
                    new Vector2(103.5f, 1.5f),   // 싱가포르
                };
                d.notes = "안다만해 → 싱가포르. 동남아 항해 핵심.";
            }));

            entries.Add(CreateOrLoad("MapSubtract_Skagerrak.asset", d =>
            {
                d.subtractId = "subtract.skagerrak";
                d.displayNameKo = "스카게라크";
                d.widthKm = 180f;
                d.points = new[]
                {
                    new Vector2(5.0f, 58.5f),
                    new Vector2(8.0f, 58.0f),
                    new Vector2(11.0f, 57.0f),
                    new Vector2(12.5f, 55.5f),   // 카테가트로 연결
                };
                d.notes = "북해 → 발트해 입구. 북유럽 항해.";
            }));

            // 카탈로그 채움
            catalog.all = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[M10MapSubtractSeeder] 완료. {entries.Count}개 영역 등록.\n" +
                "다음:\n" +
                "  1) Game ▸ Bake World Land Mesh from GeoJSON  — 메쉬 재베이크\n" +
                "  2) 씬에 WorldLand prefab 이 이미 있으면 자동 갱신됨\n" +
                "  3) Play 모드에서 MapSubtractEditor 컴포넌트 우클릭 → Enable Editor Mode → 추가/삭제 가능");
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
    }
}
