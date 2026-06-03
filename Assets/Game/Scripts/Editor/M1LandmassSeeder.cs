using System;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M1 — 8개 시작 항구 주변의 대륙을 단순 큐브로 표현하기 위한 LandmassData 시드.
    /// 위/경도 박스로 대륙 영역 정의. M3 폴리시 단계에서 Natural Earth 정밀 데이터로 교체.
    ///
    /// 메뉴: Game/Seed M1 Landmasses
    ///
    /// 8개 시작 항구는 모두 해안 옆에 위치하도록 영역 조정.
    /// 항구 좌표:
    ///   리스본 (38.7, -9.1)   세우타 (35.9, -5.3)   세비야 (37.4, -5.9)
    ///   베네치아 (45.4, 12.3) 암스테르담 (52.4, 4.9) 런던 (51.5, -0.1)
    ///   이스탄불 (41.0, 28.9) 부산 (35.1, 129.0)    광저우 (23.1, 113.3)
    /// </summary>
    public static class M1LandmassSeeder
    {
        private const string DataRoot = "Assets/Game/Data";
        private static readonly Color LandColor = new Color(0.65f, 0.55f, 0.40f);

        [MenuItem("Game/Seed M1 Landmasses")]
        public static void SeedLandmasses() => DoSeed(overwrite: false);

        [MenuItem("Game/Reset M1 Landmasses (Overwrite)")]
        public static void ResetLandmasses()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reset M1 Landmasses",
                    "기존 12개 LandmassData 의 값을 모두 새 좌표로 덮어씁니다.\n인스펙터에서 직접 수정한 값이 있으면 사라집니다.\n계속할까요?",
                    "Reset", "취소"))
            {
                return;
            }
            DoSeed(overwrite: true);
        }

        private static void DoSeed(bool overwrite)
        {
            EnsureFolder($"{DataRoot}/Landmasses");

            // ─── 시작 항구 7개를 모두 영역 밖(해안)에 두도록 조정된 좌표 ────────
            // 리스본(38.7,-9.1) / 세비야(37.4,-5.9) / 세우타(35.9,-5.3) / 베네치아(45.4,12.3) /
            // 암스테르담(52.4,4.9) / 런던(51.5,-0.1) / 이스탄불(41.0,28.9) /
            // 부산(35.1,129.0) / 광저우(23.1,113.3)

            // 이베리아 반도 — 리스본·세비야 모두 서쪽/남서쪽 가장자리 옆에 두기
            // 영역: 위 35~44, 경 -5~3
            CreateLandmass("Landmass_Iberia.asset", overwrite, l =>
            {
                l.landmassId = "land.iberia";
                l.displayNameKo = "이베리아 반도";
                l.centerLatitude = 39.5f;
                l.centerLongitude = -1f;
                l.sizeLatitude = 9f;
                l.sizeLongitude = 8f;
                l.color = LandColor;
            });

            // 북아프리카 — 세우타(35.9°N) 가 영역 북쪽 가장자리 위에 떠있도록
            // 영역: 위 17~33, 경 -10~30
            CreateLandmass("Landmass_NorthAfrica.asset", overwrite, l =>
            {
                l.landmassId = "land.north_africa";
                l.displayNameKo = "북아프리카";
                l.centerLatitude = 25f;
                l.centerLongitude = 10f;
                l.sizeLatitude = 16f;
                l.sizeLongitude = 40f;
                l.color = new Color(0.75f, 0.65f, 0.45f); // 사막 톤
            });

            // 유럽 본토 — 베네치아(45.4°N) 남쪽 가장자리, 암스테르담(52.4°N) 북쪽 가장자리
            // 영역: 위 46~52, 경 0~30
            CreateLandmass("Landmass_Europe.asset", overwrite, l =>
            {
                l.landmassId = "land.europe";
                l.displayNameKo = "유럽";
                l.centerLatitude = 49f;
                l.centerLongitude = 15f;
                l.sizeLatitude = 6f;
                l.sizeLongitude = 30f;
                l.color = LandColor;
            });

            // 영국 섬 — 런던(51.5,-0.1) 동쪽 가장자리
            // 영역: 위 50~58, 경 -8 ~ -1
            CreateLandmass("Landmass_BritishIsles.asset", overwrite, l =>
            {
                l.landmassId = "land.british_isles";
                l.displayNameKo = "영국 섬";
                l.centerLatitude = 54f;
                l.centerLongitude = -4.5f;
                l.sizeLatitude = 8f;
                l.sizeLongitude = 7f;
                l.color = LandColor;
            });

            // 아나톨리아 — 이스탄불(41.0,28.9) 서쪽 가장자리
            // 영역: 위 36~42, 경 30~46
            CreateLandmass("Landmass_Anatolia.asset", overwrite, l =>
            {
                l.landmassId = "land.anatolia";
                l.displayNameKo = "아나톨리아";
                l.centerLatitude = 39f;
                l.centerLongitude = 38f;
                l.sizeLatitude = 6f;
                l.sizeLongitude = 16f;
                l.color = LandColor;
            });

            // 중동 + 아라비아 (시작 항구 없음 — 기존 값 유지)
            CreateLandmass("Landmass_MiddleEast.asset", overwrite, l =>
            {
                l.landmassId = "land.middle_east";
                l.displayNameKo = "중동";
                l.centerLatitude = 25f;
                l.centerLongitude = 45f;
                l.sizeLatitude = 18f;
                l.sizeLongitude = 22f;
                l.color = new Color(0.75f, 0.65f, 0.45f);
            });

            // 인도 아대륙 (시작 항구 없음)
            CreateLandmass("Landmass_India.asset", overwrite, l =>
            {
                l.landmassId = "land.india";
                l.displayNameKo = "인도";
                l.centerLatitude = 22f;
                l.centerLongitude = 78f;
                l.sizeLatitude = 22f;
                l.sizeLongitude = 18f;
                l.color = LandColor;
            });

            // 중국 본토 — 광저우(23.1°N) 가 영역 남쪽 가장자리 위
            // 영역: 위 24~40, 경 101~115
            CreateLandmass("Landmass_China.asset", overwrite, l =>
            {
                l.landmassId = "land.china";
                l.displayNameKo = "중국";
                l.centerLatitude = 32f;
                l.centerLongitude = 108f;
                l.sizeLatitude = 16f;
                l.sizeLongitude = 14f;
                l.color = LandColor;
            });

            // 한반도 — 부산(35.1°N) 이 영역 남쪽 가장자리 옆
            // 영역: 위 36~42, 경 125~128.5
            CreateLandmass("Landmass_Korea.asset", overwrite, l =>
            {
                l.landmassId = "land.korea";
                l.displayNameKo = "한반도";
                l.centerLatitude = 39f;
                l.centerLongitude = 126.75f;
                l.sizeLatitude = 6f;
                l.sizeLongitude = 3.5f;
                l.color = LandColor;
            });

            // 일본 열도
            CreateLandmass("Landmass_Japan.asset", overwrite, l =>
            {
                l.landmassId = "land.japan";
                l.displayNameKo = "일본";
                l.centerLatitude = 36f;
                l.centerLongitude = 138f;
                l.sizeLatitude = 12f;
                l.sizeLongitude = 8f;
                l.color = LandColor;
            });

            // 동남아시아
            CreateLandmass("Landmass_SoutheastAsia.asset", overwrite, l =>
            {
                l.landmassId = "land.southeast_asia";
                l.displayNameKo = "동남아시아";
                l.centerLatitude = 5f;
                l.centerLongitude = 105f;
                l.sizeLatitude = 18f;
                l.sizeLongitude = 14f;
                l.color = LandColor;
            });

            // 사하라 이남 아프리카
            CreateLandmass("Landmass_SubSaharanAfrica.asset", overwrite, l =>
            {
                l.landmassId = "land.subsaharan_africa";
                l.displayNameKo = "아프리카 (사하라 이남)";
                l.centerLatitude = -10f;
                l.centerLongitude = 20f;
                l.sizeLatitude = 40f;
                l.sizeLongitude = 36f;
                l.color = LandColor;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string verb = overwrite ? "갱신됨" : "생성됨";
            Debug.Log(
                $"[M1LandmassSeeder] 완료. 12개 LandmassData {verb}:\n" +
                "  • 이베리아 / 북아프리카 / 유럽 / 영국 섬 / 아나톨리아\n" +
                "  • 중동 / 인도 / 중국 / 한반도 / 일본 / 동남아시아 / 사하라이남\n" +
                "\n9개 시작 항구는 모두 인근 대륙 가장자리 옆에 위치 (해안 만 효과).");
        }

        private static void CreateLandmass(string fileName, bool overwrite, Action<LandmassData> setup)
        {
            var path = $"{DataRoot}/Landmasses/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<LandmassData>(path);

            if (existing != null)
            {
                if (!overwrite)
                {
                    Debug.Log($"[M1LandmassSeeder] Skipping (exists): {path}");
                    return;
                }
                // 덮어쓰기: 기존 SO 의 필드만 갱신 (참조는 보존)
                setup(existing);
                EditorUtility.SetDirty(existing);
                Debug.Log($"[M1LandmassSeeder] Overwritten: {path}");
                return;
            }

            var so = ScriptableObject.CreateInstance<LandmassData>();
            setup(so);
            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[M1LandmassSeeder] Created: {path}");
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
