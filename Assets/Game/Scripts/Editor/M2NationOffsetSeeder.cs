using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M2 — 8개국 NationData 의 startingSeaOffset 값 자동 설정.
    ///
    /// 각 시작 항구가 인근 대륙 가장자리에 가까이 있어서 (특히 베네치아/암스테르담은 6~9 unit 거리),
    /// 배가 시작 위치에서 충돌 sphere 가 큐브에 닿아 움직이지 못하는 문제 방지.
    ///
    /// offset 1° = 15 unit. 1.5° 권장 → 22.5 unit 바다 쪽으로 추가 이동.
    ///
    /// 메뉴: Game/Reset Nation Spawn Offsets
    /// </summary>
    public static class M2NationOffsetSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Reset Nation Spawn Offsets")]
        public static void SetOffsets()
        {
            SetOffset("Nation_Portugal", lat: 0f, lng: -1.5f);     // 리스본 → 서쪽
            SetOffset("Nation_Spain", lat: -0.5f, lng: -1.5f);     // 세비야 → 남서쪽
            SetOffset("Nation_Italy", lat: -1.5f, lng: 0f);        // 베네치아 → 남쪽 (아드리아해)
            SetOffset("Nation_Netherlands", lat: 1.5f, lng: 0f);   // 암스테르담 → 북쪽 (북해)
            SetOffset("Nation_England", lat: 0f, lng: 1.5f);       // 런던 → 동쪽 (북해)
            SetOffset("Nation_Ottoman", lat: 0f, lng: -1.5f);      // 이스탄불 → 서쪽 (마르마라해)
            SetOffset("Nation_Joseon", lat: -1.5f, lng: 0.5f);     // 부산 → 남동쪽
            SetOffset("Nation_China", lat: -1.5f, lng: 0f);        // 광저우 → 남쪽 (남중국해)

            AssetDatabase.SaveAssets();
            Debug.Log(
                "[M2NationOffsetSeeder] 완료. 8개국 startingSeaOffset 설정:\n" +
                "  • 포르투갈/리스본 → 서쪽 -1.5°\n" +
                "  • 스페인/세비야 → 남서쪽 (-0.5, -1.5)\n" +
                "  • 이탈리아/베네치아 → 남쪽 -1.5°\n" +
                "  • 네덜란드/암스테르담 → 북쪽 +1.5°\n" +
                "  • 영국/런던 → 동쪽 +1.5°\n" +
                "  • 오스만/이스탄불 → 서쪽 -1.5°\n" +
                "  • 조선/부산 → 남동쪽 (-1.5, +0.5)\n" +
                "  • 중국/광저우 → 남쪽 -1.5°\n" +
                "\n각 국가로 시작 시 배가 항구 좌표에서 ~22.5 unit (~166km) 바다 쪽으로 떨어진 곳에 spawn.");
        }

        private static void SetOffset(string nationName, float lat, float lng)
        {
            var path = $"{DataRoot}/Nations/{nationName}.asset";
            var nation = AssetDatabase.LoadAssetAtPath<NationData>(path);
            if (nation == null)
            {
                Debug.LogWarning($"[M2NationOffsetSeeder] NationData 없음: {path}");
                return;
            }
            nation.startingSeaOffsetLatitude = lat;
            nation.startingSeaOffsetLongitude = lng;
            EditorUtility.SetDirty(nation);
            Debug.Log($"[M2NationOffsetSeeder] {nationName}.startingSeaOffset = (lat {lat}, lng {lng})");
        }
    }
}
