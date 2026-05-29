using System.Collections.Generic;
using Game.Data;
using Game.Ports;
using Game.Ship;
using UnityEngine;
using UnityEngine.Events;

namespace Game.World
{
    /// <summary>
    /// 세계 관리자 — M1 단순 버전.
    ///
    /// 역할:
    ///   - 항구 목록을 받아 월드에 아이콘으로 배치.
    ///   - 발견물 목록을 받아 좌표·허용 오차 기반 탐색 기능 제공.
    ///   - 플레이어 정박 시 "정박 및 탐색" 호출 → 가까운 발견물 검출 → 이벤트 발행.
    ///
    /// 추후 M2 이후:
    ///   - 다른 NPC(상업선·호위선·해적선) 풀 관리.
    ///   - 시세 갱신 트리거.
    ///   - 카메라 추종.
    /// </summary>
    public class SeaWorldManager : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("씬의 플레이어 배. 위치를 받아 정박 위치 계산.")]
        public ShipController playerShip;

        [Header("Database (M1: SeederContent 로 채워 둔 SO 들)")]
        [Tooltip("월드에 배치할 항구 목록.")]
        public PortData[] activePorts;

        [Tooltip("탐색 가능한 발견물 목록. 의뢰 수행 중일 때만 검출되게 하는 것은 별도 시스템에서 처리.")]
        public DiscoveryData[] activeDiscoveries;

        [Header("Visuals")]
        [Tooltip("항구 아이콘 프리팹. 비어 있으면 아이콘 생성을 건너뜀.")]
        public GameObject portIconPrefab;

        [Tooltip("항구 아이콘들을 묶을 부모 Transform. 비어 있으면 본 GameObject 하위에 생성.")]
        public Transform portIconsParent;

        [Header("Events")]
        public UnityEvent<PortData> onPortArrived;
        public UnityEvent<DiscoveryData> onDiscoveryFound;
        public UnityEvent onSearchFailed;

        private readonly List<GameObject> _spawnedIcons = new();

        // ─── 라이프사이클 ────────────────────────────────────────────────────

        private void Start()
        {
            SpawnPortIcons();
        }

        // ─── 항구 아이콘 ────────────────────────────────────────────────────

        private void SpawnPortIcons()
        {
            if (activePorts == null || activePorts.Length == 0) return;
            if (portIconPrefab == null) return;
            if (portIconsParent == null) portIconsParent = transform;

            foreach (var port in activePorts)
            {
                if (port == null) continue;

                var icon = Instantiate(portIconPrefab, portIconsParent);
                var worldPos = GeoCoordinate.LatLngToWorld(port.latitude, port.longitude);
                icon.transform.position = worldPos;
                icon.name = $"PortIcon_{port.portId}";

                // 아이콘에 PortData 연결 (클릭 처리 컴포넌트가 사용).
                var marker = icon.GetComponent<PortMarker>();
                if (marker != null)
                {
                    marker.Bind(port);
                }

                _spawnedIcons.Add(icon);
            }
        }

        // ─── 정박 및 탐색 ────────────────────────────────────────────────────

        /// <summary>
        /// 현재 플레이어 위치에서 "정박 및 탐색" 시도.
        /// 가까운 발견물이 있으면 onDiscoveryFound 발행, 없으면 onSearchFailed.
        ///
        /// 의뢰 매칭은 본 메서드 호출 전에 외부에서 필터링 (예: 활성 의뢰의 target 만 검색 후보로 전달).
        /// M1 에서는 단순화 — 모든 활성 발견물을 후보로 검색.
        /// </summary>
        public bool TryAnchorAndSearch(out DiscoveryData found)
        {
            found = null;
            if (playerShip == null || activeDiscoveries == null) return false;

            var anchorPos = playerShip.transform.position;
            int keenEye = playerShip.captain != null ? playerShip.captain.keenEye : 50;

            float closestDist = float.MaxValue;
            DiscoveryData closest = null;

            foreach (var disc in activeDiscoveries)
            {
                if (disc == null) continue;

                var discPos = GeoCoordinate.LatLngToWorld(disc.latitude, disc.longitude);
                float dist = Vector3.Distance(anchorPos, discPos);
                float adjustedTolerance =
                    GeoCoordinate.ApplyKeenEyeBonus(disc.searchToleranceBase, keenEye);
                float toleranceDist = GeoCoordinate.GetSearchToleranceDistance(adjustedTolerance);

                if (dist <= toleranceDist && dist < closestDist)
                {
                    closestDist = dist;
                    closest = disc;
                }
            }

            if (closest != null)
            {
                found = closest;
                onDiscoveryFound?.Invoke(closest);
                return true;
            }

            onSearchFailed?.Invoke();
            return false;
        }

        // ─── 항구 도착 트리거 ────────────────────────────────────────────────

        /// <summary>
        /// 항구 아이콘 또는 PortMarker 가 호출 — 사용자가 항구를 탭해 자동 입항 결정 시.
        /// onPortArrived 이벤트가 발행되면 UI에서 "항구로 들어가시겠습니까?" 다이얼로그 표시.
        /// </summary>
        public void NotifyPortArrival(PortData port)
        {
            if (port == null) return;
            onPortArrived?.Invoke(port);
        }
    }
}
