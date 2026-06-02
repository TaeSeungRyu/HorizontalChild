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

        [Header("Auto Arrival Detection")]
        [Tooltip("플레이어가 항구에 이 거리 이내로 접근하면 자동으로 도착 알림. 1 unit ≈ 7.4 km.")]
        [Range(5f, 100f)] public float arrivalRadiusUnits = 20f;

        [Tooltip("도착 알림 후 같은 항구를 다시 트리거하지 않을 거리. 이 거리 밖으로 나가야 다시 트리거.")]
        [Range(10f, 200f)] public float rearmRadiusUnits = 60f;

        [Tooltip("항구를 직접 클릭했을 때 입항 가능한 거리. 자동 도착보다 살짝 넓게 설정 가능.")]
        [Range(10f, 200f)] public float clickEnterRadiusUnits = 30f;

        [Header("Events")]
        public UnityEvent<PortData> onPortArrived;
        public UnityEvent<DiscoveryData> onDiscoveryFound;
        public UnityEvent onSearchFailed;

        [Tooltip("클릭한 항구가 너무 멀 때 발행. UI 토스트 등에 연결.")]
        public UnityEvent<PortData> onPortClickTooFar;

        private readonly List<GameObject> _spawnedIcons = new();

        // 도착 알림 후 "다시 트리거 안 함" 상태인 항구 id 집합.
        // 거리 밖으로 나가면 자동으로 해제됨.
        private readonly HashSet<string> _suppressedPortIds = new();

        // ─── 라이프사이클 ────────────────────────────────────────────────────

        private void Start()
        {
            SpawnPortIcons();
        }

        private void Update()
        {
            CheckPortArrival();
        }

        // ─── 자동 항구 도착 감지 ─────────────────────────────────────────────

        /// <summary>
        /// 매 프레임 플레이어 위치와 각 항구의 거리를 확인.
        /// arrivalRadiusUnits 이내로 들어오면 onPortArrived 발행 (한 번만).
        /// rearmRadiusUnits 밖으로 나가면 다시 트리거 가능 상태로 복귀.
        /// </summary>
        private void CheckPortArrival()
        {
            if (playerShip == null || activePorts == null) return;

            var shipPos = playerShip.transform.position;

            foreach (var port in activePorts)
            {
                if (port == null) continue;

                var portPos = GeoCoordinate.LatLngToWorld(port.latitude, port.longitude);
                float dist = Vector3.Distance(shipPos, portPos);

                if (_suppressedPortIds.Contains(port.portId))
                {
                    // 한 번 트리거된 항구 — 멀리 나가면 다시 켜짐
                    if (dist > rearmRadiusUnits)
                    {
                        _suppressedPortIds.Remove(port.portId);
                    }
                }
                else
                {
                    // 가까이 가면 도착 알림 발행
                    if (dist <= arrivalRadiusUnits)
                    {
                        _suppressedPortIds.Add(port.portId);
                        onPortArrived?.Invoke(port);
                    }
                }
            }
        }

        /// <summary>외부에서 강제 재무장 — 항구 화면에서 떠날 때 호출 (떠난 직후 즉시 재진입 방지).</summary>
        public void SuppressPort(string portId)
        {
            if (!string.IsNullOrEmpty(portId))
            {
                _suppressedPortIds.Add(portId);
            }
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

        /// <summary>
        /// 사용자가 항구 아이콘을 클릭했을 때 — 거리 체크 후 입항 다이얼로그 트리거.
        /// 거리 안이면 onPortArrived 발행 (자동 도착과 동일 흐름).
        /// 거리 밖이면 onPortClickTooFar 발행 + Console 로그.
        /// </summary>
        public void TryEnterPortFromClick(PortData port)
        {
            if (port == null || playerShip == null) return;

            var shipPos = playerShip.transform.position;
            var portPos = GeoCoordinate.LatLngToWorld(port.latitude, port.longitude);
            float dist = Vector3.Distance(shipPos, portPos);

            if (dist <= clickEnterRadiusUnits)
            {
                _suppressedPortIds.Add(port.portId);
                onPortArrived?.Invoke(port);
            }
            else
            {
                Debug.Log($"[SeaWorldManager] {port.displayNameKo} 너무 멀어요 (거리 {dist:F0}/{clickEnterRadiusUnits:F0})");
                onPortClickTooFar?.Invoke(port);
            }
        }
    }
}
