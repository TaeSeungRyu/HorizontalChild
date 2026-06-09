using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// NPC / 플레이어 항해 경로 계산.
    ///
    /// 단순 region 그래프 — 같은 region 안에서는 직선 항해, 다른 region 으로 가려면
    /// 미리 정의된 게이트웨이 웨이포인트를 거쳐감.
    ///
    /// 현재 등록된 게이트웨이:
    ///   - Gibraltar / Ceuta (35.95N, -5.4E) — 대서양 ↔ 지중해
    ///
    /// 동아시아·인도양 등은 직접 항해 외엔 미구현 (대륙 우회 라우팅 부재 — 그쪽 NPC 는 같은 region 안에서만 활동 권장).
    /// </summary>
    public static class NavRouter
    {
        public enum Region
        {
            Atlantic,       // lng < -6
            Mediterranean,  // -6 ≤ lng < 40
            EastAsia,       // lng ≥ 100
            Other,          // 그 외 — 직접 항해
        }

        // 게이트웨이 (lat, lng)
        public static readonly Vector2 GibraltarLatLng = new Vector2(35.95f, -5.4f);

        public static Region GetRegion(float longitude)
        {
            if (longitude < -6f) return Region.Atlantic;
            if (longitude < 40f) return Region.Mediterranean;
            if (longitude >= 100f) return Region.EastAsia;
            return Region.Other;
        }

        public static Region GetRegionForPort(PortData port)
        {
            return port == null ? Region.Other : GetRegion(port.longitude);
        }

        public static Region GetRegionForWorld(Vector3 worldPos)
        {
            var (_, lng) = GeoCoordinate.WorldToLatLng(worldPos);
            return GetRegion(lng);
        }

        /// <summary>
        /// 출발 위치(또는 항구) → 목적 항구 의 항해 경로.
        /// 마지막 원소는 항상 목적 항구의 월드 좌표.
        /// 다른 region 간 이동이면 게이트웨이 웨이포인트가 앞에 삽입됨.
        /// </summary>
        public static List<Vector3> ComputePath(Vector3 originWorld, PortData destination)
        {
            var path = new List<Vector3>();
            if (destination == null) return path;

            var originRegion = GetRegionForWorld(originWorld);
            var destRegion = GetRegionForPort(destination);

            AddGatewayIfNeeded(path, originRegion, destRegion);

            path.Add(GeoCoordinate.LatLngToWorld(destination.latitude, destination.longitude));
            return path;
        }

        public static List<Vector3> ComputePath(PortData origin, PortData destination)
        {
            if (origin == null) return ComputePath(Vector3.zero, destination);
            var originWorld = GeoCoordinate.LatLngToWorld(origin.latitude, origin.longitude);
            return ComputePath(originWorld, destination);
        }

        private static void AddGatewayIfNeeded(List<Vector3> path, Region from, Region to)
        {
            if (from == to) return;

            // 대서양 ↔ 지중해 — Gibraltar 경유
            bool atlanticMed = (from == Region.Atlantic && to == Region.Mediterranean)
                            || (from == Region.Mediterranean && to == Region.Atlantic);
            if (atlanticMed)
            {
                path.Add(GeoCoordinate.LatLngToWorld(GibraltarLatLng.x, GibraltarLatLng.y));
            }
            // 그 외 cross-region (e.g. Atlantic ↔ EastAsia) — 미구현. 직접 항해 시도하다 실패하면 데드라인 텔레포트로 fallback.
        }
    }
}
