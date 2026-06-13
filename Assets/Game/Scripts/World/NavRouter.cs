using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// NPC / 플레이어 항해 경로 계산.
    ///
    /// 지구 표면을 4개 region 으로 나눠 region 간 이동 시 게이트웨이 웨이포인트를 거침:
    ///   - Atlantic        (Americas + 서유럽 + 서아프리카)
    ///   - Mediterranean   (지중해 + 흑해)
    ///   - IndianOcean     (페르시아만 + 인도 + 동아프리카 + 동남아 일부)
    ///   - Pacific         (동아시아 + 일부 동남아)
    ///
    /// 게이트웨이:
    ///   - Atlantic ↔ Mediterranean   : Gibraltar (35.95, -5.4)
    ///   - Atlantic ↔ IndianOcean     : Cape of Good Hope (-34.4, 18.4)
    ///   - IndianOcean ↔ Pacific      : Malacca (1.0, 103.0)
    ///   - Mediterranean ↔ IndianOcean: 직접 경로 X — Atlantic 경유 (Gibraltar → Cape of Good Hope)
    ///   - Atlantic ↔ Pacific         : 2단 경유 (Cape of Good Hope → Malacca) 또는 Cape Horn (미구현)
    /// </summary>
    public static class NavRouter
    {
        public enum Region
        {
            Atlantic,
            Mediterranean,
            IndianOcean,
            Pacific,
            Other,
        }

        // 게이트웨이 (lat, lng)
        public static readonly Vector2 GibraltarLatLng = new Vector2(35.95f, -5.4f);
        public static readonly Vector2 CapeOfGoodHopeLatLng = new Vector2(-34.4f, 18.4f);
        public static readonly Vector2 MalaccaLatLng = new Vector2(1.0f, 103.0f);

        /// <summary>
        /// 경도 + 위도 기반 region 판정. (lng wrap 미지원 — 평면 메르카토르 기준)
        /// </summary>
        public static Region GetRegion(float latitude, float longitude)
        {
            // 지중해: 위도 30~46, 경도 -6~40 (대략 지중해 + 흑해)
            if (latitude >= 28f && latitude <= 48f && longitude >= -6f && longitude < 40f)
                return Region.Mediterranean;

            // 인도양: 경도 40 ~ 100, 위도 -45 ~ 30 (페르시아만, 인도, 동아프리카, 자바)
            if (longitude >= 40f && longitude < 100f)
                return Region.IndianOcean;

            // 태평양 (서쪽 절반만): 경도 100 ~ 180 (동아시아)
            if (longitude >= 100f)
                return Region.Pacific;

            // 대서양 — 그 외 (Americas 양쪽 + 서유럽 + 서아프리카)
            // lng -180~-6 또는 (lng -6~40 + lat 밖의 지중해)
            return Region.Atlantic;
        }

        public static Region GetRegionForPort(PortData port)
        {
            return port == null ? Region.Other : GetRegion(port.latitude, port.longitude);
        }

        public static Region GetRegionForWorld(Vector3 worldPos)
        {
            var (lat, lng) = GeoCoordinate.WorldToLatLng(worldPos);
            return GetRegion(lat, lng);
        }

        /// <summary>
        /// 출발 위치 → 목적 항구 경로. 마지막 원소는 항상 목적 항구 좌표.
        /// region 간 이동 시 중간 게이트웨이 자동 삽입 (필요하면 2단 경유).
        /// </summary>
        public static List<Vector3> ComputePath(Vector3 originWorld, PortData destination)
        {
            var path = new List<Vector3>();
            if (destination == null) return path;

            var from = GetRegionForWorld(originWorld);
            var to = GetRegionForPort(destination);

            AppendGateways(path, from, to);

            path.Add(GeoCoordinate.LatLngToWorld(destination.latitude, destination.longitude));
            return path;
        }

        public static List<Vector3> ComputePath(PortData origin, PortData destination)
        {
            if (origin == null) return ComputePath(Vector3.zero, destination);
            var originWorld = GeoCoordinate.LatLngToWorld(origin.latitude, origin.longitude);
            return ComputePath(originWorld, destination);
        }

        /// <summary>from → to region 이동에 필요한 게이트웨이들을 path 에 순서대로 추가.</summary>
        private static void AppendGateways(List<Vector3> path, Region from, Region to)
        {
            if (from == to) return;

            // 1단 경로
            bool atlanticMed = (from == Region.Atlantic && to == Region.Mediterranean)
                            || (from == Region.Mediterranean && to == Region.Atlantic);
            if (atlanticMed)
            {
                path.Add(LatLngToWorld(GibraltarLatLng));
                return;
            }
            if ((from == Region.Atlantic && to == Region.IndianOcean) ||
                (from == Region.IndianOcean && to == Region.Atlantic))
            {
                path.Add(LatLngToWorld(CapeOfGoodHopeLatLng));
                return;
            }
            if ((from == Region.IndianOcean && to == Region.Pacific) ||
                (from == Region.Pacific && to == Region.IndianOcean))
            {
                path.Add(LatLngToWorld(MalaccaLatLng));
                return;
            }

            // 2단 경로
            // Mediterranean ↔ IndianOcean : Gibraltar → Cape of Good Hope
            if ((from == Region.Mediterranean && to == Region.IndianOcean) ||
                (from == Region.IndianOcean && to == Region.Mediterranean))
            {
                if (from == Region.Mediterranean)
                {
                    path.Add(LatLngToWorld(GibraltarLatLng));
                    path.Add(LatLngToWorld(CapeOfGoodHopeLatLng));
                }
                else
                {
                    path.Add(LatLngToWorld(CapeOfGoodHopeLatLng));
                    path.Add(LatLngToWorld(GibraltarLatLng));
                }
                return;
            }

            // Atlantic ↔ Pacific : Cape of Good Hope → Malacca (또는 그 역)
            if ((from == Region.Atlantic && to == Region.Pacific) ||
                (from == Region.Pacific && to == Region.Atlantic))
            {
                if (from == Region.Atlantic)
                {
                    path.Add(LatLngToWorld(CapeOfGoodHopeLatLng));
                    path.Add(LatLngToWorld(MalaccaLatLng));
                }
                else
                {
                    path.Add(LatLngToWorld(MalaccaLatLng));
                    path.Add(LatLngToWorld(CapeOfGoodHopeLatLng));
                }
                return;
            }

            // Mediterranean ↔ Pacific : Gibraltar → Cape of Good Hope → Malacca (3단)
            if ((from == Region.Mediterranean && to == Region.Pacific) ||
                (from == Region.Pacific && to == Region.Mediterranean))
            {
                if (from == Region.Mediterranean)
                {
                    path.Add(LatLngToWorld(GibraltarLatLng));
                    path.Add(LatLngToWorld(CapeOfGoodHopeLatLng));
                    path.Add(LatLngToWorld(MalaccaLatLng));
                }
                else
                {
                    path.Add(LatLngToWorld(MalaccaLatLng));
                    path.Add(LatLngToWorld(CapeOfGoodHopeLatLng));
                    path.Add(LatLngToWorld(GibraltarLatLng));
                }
                return;
            }

            // 그 외 — Other 영역 등은 직접 항해 시도. 막히면 데드라인 텔레포트 fallback.
        }

        private static Vector3 LatLngToWorld(Vector2 latLng)
        {
            return GeoCoordinate.LatLngToWorld(latLng.x, latLng.y);
        }
    }
}
