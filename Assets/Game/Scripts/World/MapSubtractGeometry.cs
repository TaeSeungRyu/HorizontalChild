using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// MapSubtractData (lat/lng 폴리곤 또는 폴리라인) → 월드 좌표 (XZ) 폴리곤 변환.
    /// 베이커 + 런타임 충돌 (ShipController.IsLandAt 보강용) 양쪽에서 사용.
    /// </summary>
    public static class MapSubtractGeometry
    {
        /// <summary>
        /// 폴리라인 → 사각 띠 (XZ 평면) 정점.
        /// 각 segment 마다 perpendicular 방향으로 ±halfWidth 만큼 밀어 사각형 생성 후 결합.
        /// 단순화: 세그먼트별 사각형을 OR 연산 없이 그냥 합쳐 반환 (코너 약간 겹침 — 시각 OK).
        /// </summary>
        public static List<Vector2[]> BuildSubtractPolygonsWorld(MapSubtractData data)
        {
            var result = new List<Vector2[]>();
            if (data == null || data.points == null || data.points.Length < 2) return result;
            if (!data.enabled) return result;

            // 폴리곤 모드 — points 가 정점 (월드 XZ 변환만)
            if (data.widthKm <= 0f)
            {
                if (data.points.Length < 3) return result;
                var poly = new Vector2[data.points.Length];
                for (int i = 0; i < data.points.Length; i++)
                {
                    var w = GeoCoordinate.LatLngToWorld(data.points[i].y, data.points[i].x);
                    poly[i] = new Vector2(w.x, w.z);
                }
                result.Add(poly);
                return result;
            }

            // 폴리라인 모드 — 세그먼트마다 사각형
            float halfWidthUnits = (data.widthKm * 0.5f) / GeoCoordinate.KmPerUnit;
            for (int i = 0; i < data.points.Length - 1; i++)
            {
                var aLatLng = data.points[i];
                var bLatLng = data.points[i + 1];
                var aWorld = GeoCoordinate.LatLngToWorld(aLatLng.y, aLatLng.x);
                var bWorld = GeoCoordinate.LatLngToWorld(bLatLng.y, bLatLng.x);
                var a = new Vector2(aWorld.x, aWorld.z);
                var b = new Vector2(bWorld.x, bWorld.z);

                var dir = b - a;
                if (dir.sqrMagnitude < 0.0001f) continue;
                dir.Normalize();
                var perp = new Vector2(-dir.y, dir.x) * halfWidthUnits;

                // 양 끝을 살짝 더 밀어 코너에 빈틈 없도록 (cap)
                var aCap = a - dir * halfWidthUnits;
                var bCap = b + dir * halfWidthUnits;

                // 사각형 CCW
                result.Add(new[]
                {
                    aCap + perp,
                    bCap + perp,
                    bCap - perp,
                    aCap - perp,
                });
            }

            return result;
        }

        /// <summary>2D point-in-polygon (ray casting). poly 는 XZ 평면 정점 배열.</summary>
        public static bool PointInPolygon(Vector2 p, Vector2[] poly)
        {
            if (poly == null || poly.Length < 3) return false;
            bool inside = false;
            int n = poly.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                var pi = poly[i];
                var pj = poly[j];
                bool crosses = ((pi.y > p.y) != (pj.y > p.y))
                    && (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y + 1e-9f) + pi.x);
                if (crosses) inside = !inside;
            }
            return inside;
        }

        /// <summary>여러 폴리곤 중 하나라도 점을 포함하면 true.</summary>
        public static bool PointInAny(Vector2 p, List<Vector2[]> polygons)
        {
            if (polygons == null) return false;
            for (int i = 0; i < polygons.Count; i++)
            {
                if (PointInPolygon(p, polygons[i])) return true;
            }
            return false;
        }
    }
}
