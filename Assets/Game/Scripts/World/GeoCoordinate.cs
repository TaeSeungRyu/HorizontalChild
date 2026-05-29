using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 위/경도 ↔ Unity 월드 좌표 변환 유틸.
    ///
    /// 메르카토르 도법을 단순화한 평면 매핑:
    ///   longitude (-180~180°) → world x
    ///   latitude  (-90~90°)   → world z
    ///   y는 항상 0 (해수면)
    ///
    /// 스케일 (GAME_MECHANICS §3.1):
    ///   "가장 빠른 배(speed≈9)로 세계일주 약 10분"
    ///   세계 가로 360° = WorldWidthUnits 만큼 (잠정값, 추후 튜닝)
    ///   세로 180° = WorldWidthUnits / 2
    ///
    /// 추후 §8.9 위/경도 → Unity 좌표 매핑 정밀화 시 본 클래스만 수정하면 됨.
    /// </summary>
    public static class GeoCoordinate
    {
        // ─── 튜닝 상수 ─────────────────────────────────────────────────────

        /// <summary>세계 가로(360°) 의 Unity Unit 환산 길이. 잠정 5400 — speed 9 배가 600초(10분)에 일주.</summary>
        public const float WorldWidthUnits = 5400f;

        /// <summary>세계 세로(180°) 의 Unity Unit 환산 길이. 가로의 절반.</summary>
        public const float WorldHeightUnits = WorldWidthUnits * 0.5f;

        /// <summary>지구 둘레(km). 약 40,075.</summary>
        public const float EarthCircumferenceKm = 40000f;

        /// <summary>1 Unity Unit 당 km. 약 7.4 km/unit.</summary>
        public static float KmPerUnit => EarthCircumferenceKm / WorldWidthUnits;

        // ─── 변환 ───────────────────────────────────────────────────────────

        /// <summary>위·경도 → Unity 월드 좌표 (y = 0).</summary>
        public static Vector3 LatLngToWorld(float latitude, float longitude)
        {
            float x = (longitude / 360f) * WorldWidthUnits;
            float z = (latitude / 180f) * WorldHeightUnits;
            return new Vector3(x, 0f, z);
        }

        /// <summary>Unity 월드 좌표 (y 무시) → 위·경도.</summary>
        public static (float latitude, float longitude) WorldToLatLng(Vector3 worldPos)
        {
            float lng = (worldPos.x / WorldWidthUnits) * 360f;
            float lat = (worldPos.z / WorldHeightUnits) * 180f;
            return (lat, lng);
        }

        /// <summary>두 위·경도 좌표 간 거리(Unity Unit).</summary>
        public static float DistanceUnits(float latA, float lngA, float latB, float lngB)
        {
            var a = LatLngToWorld(latA, lngA);
            var b = LatLngToWorld(latB, lngB);
            return Vector3.Distance(a, b);
        }

        /// <summary>두 위·경도 좌표 간 거리(km, 근사).</summary>
        public static float DistanceKm(float latA, float lngA, float latB, float lngB)
        {
            return DistanceUnits(latA, lngA, latB, lngB) * KmPerUnit;
        }

        // ─── 발견물 탐색 ────────────────────────────────────────────────────

        /// <summary>
        /// 발견물 좌표 허용 오차(비율) → Unity Unit 거리.
        /// 기본 0.03 (±3%) 은 세계 가로의 3% = WorldWidthUnits × 0.03.
        /// 눈썰미 보너스로 toleranceBase 가 0.05 까지 커질 수 있음 (GAME_MECHANICS §1.1).
        /// </summary>
        public static float GetSearchToleranceDistance(float toleranceBase)
        {
            return WorldWidthUnits * Mathf.Clamp(toleranceBase, 0f, 0.2f);
        }

        /// <summary>
        /// 눈썰미 보정 — 능력치(1~100)에 따라 허용 오차를 살짝 늘려준다.
        /// keenEye 1   → base ×1.0
        /// keenEye 100 → base ×1.67 (base 0.03 → 0.05)
        /// </summary>
        public static float ApplyKeenEyeBonus(float toleranceBase, int keenEye)
        {
            float multiplier = 1f + (Mathf.Clamp(keenEye, 1, 100) / 100f) * 0.67f;
            return toleranceBase * multiplier;
        }
    }
}
