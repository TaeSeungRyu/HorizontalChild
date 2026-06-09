using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 런타임 항해 카브 — 메쉬 충돌을 특정 영역에서 "투명화" 하는 데이터.
    ///
    /// 1:50m GeoJSON 의 좁은 해협(지브롤터 등) 이나 작은 항구 위치가 단순화 데이터에서
    /// 막혀버리는 경우, 메쉬 자체를 수정하면 폴리곤이 self-intersect → 시각 망가짐.
    ///
    /// 대신 메쉬는 정확하게 유지하고, ShipController.IsLandAt 에서
    /// 배 위치가 카브 원 안이면 무조건 "바다" 로 판정 → 통과 가능.
    ///
    /// 시각: 플레이어는 진짜 해안선을 보지만 카브 영역만 마법처럼 통과.
    /// 어린이 게임이라 게임플레이 우선 > 지리 정확도.
    /// </summary>
    public static class WorldCarves
    {
        public struct Carve
        {
            public string name;
            public float latitude;
            public float longitude;
            public float radiusKm;
        }

        // 카브 추가하려면 배열에 한 줄 추가. radiusKm 은 카브 반경.
        private static readonly Carve[] Carves =
        {
            // 지브롤터 해협 + Ceuta 항구 — 한 카브로 묶어서 둘 다 진입 가능.
            // 반경 키워 Algarve 해안 / 모로코 북부 접근로까지 포함 (NPC 우회 데드락 방지).
            new Carve
            {
                name = "Gibraltar / Ceuta",
                latitude = 35.92f,
                longitude = -5.45f,
                radiusKm = 110f,
            },
        };

        /// <summary>
        /// 주어진 월드 위치가 어떤 카브 영역 안에 있으면 true → 충돌 무시.
        /// </summary>
        public static bool IsInOpenArea(Vector3 worldPos)
        {
            if (Carves == null || Carves.Length == 0) return false;

            for (int i = 0; i < Carves.Length; i++)
            {
                var carve = Carves[i];
                var center = GeoCoordinate.LatLngToWorld(carve.latitude, carve.longitude);
                float dx = worldPos.x - center.x;
                float dz = worldPos.z - center.z;
                float distUnits = Mathf.Sqrt(dx * dx + dz * dz);
                float radiusUnits = carve.radiusKm / GeoCoordinate.KmPerUnit;
                if (distUnits < radiusUnits) return true;
            }
            return false;
        }
    }
}
