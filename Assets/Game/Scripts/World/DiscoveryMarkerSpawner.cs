using System.Collections.Generic;
using Game.Data;
using Game.Missions;
using Game.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.World
{
    /// <summary>
    /// 발견 위치 마커 자동 spawner.
    ///
    /// 동작:
    ///   - Start 시 MissionService.DiscoveredIds 의 모든 발견물에 대해 마커 spawn
    ///     (저장 시스템 도입 후 재실행 시 자동 복원되게 준비됨)
    ///   - 새 발견물 등록 (onDiscoveryRegistered 이벤트) → 즉시 마커 spawn
    ///
    /// 마커 구조:
    ///   GameObject (Canvas, GraphicRaycaster)
    ///     └ Button (Image) — 탭 시 DiscoveryFoundPanel.Show(data)
    ///
    /// 별도 PhysicsRaycaster 불필요 — World-Space Canvas + GraphicRaycaster 가 처리.
    /// 시각 커스터마이즈는 ColorFor(카테고리) 또는 markerSize/markerHeight 로.
    /// </summary>
    public class DiscoveryMarkerSpawner : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("발견물 카탈로그 — Save 복원 시 ID → DiscoveryData 매핑에 사용.")]
        public DiscoveryCatalog discoveryCatalog;

        [Tooltip("마커 클릭 시 띄울 패널. 비어 있으면 씬에서 자동 검색.")]
        public DiscoveryFoundPanel reopenPanel;

        [Tooltip("마커들을 묶을 부모. 비어 있으면 본 GameObject 하위.")]
        public Transform markersParent;

        [Header("Visual")]
        [Tooltip("마커 Y 좌표 (바다 위 높이). WorldLand 의 BaseY 보다 살짝 위.")]
        public float markerHeight = 3f;

        [Tooltip("마커 한 변 크기 (Unity Unit). 30 ≈ 220 km.")]
        public float markerSize = 30f;

        private MissionService _missionService;
        private readonly Dictionary<string, GameObject> _spawned = new();

        private void Start()
        {
            if (markersParent == null) markersParent = transform;
            if (reopenPanel == null) reopenPanel = FindAnyObjectByType<DiscoveryFoundPanel>();

            _missionService = MissionService.Instance;
            if (_missionService == null)
            {
                Debug.LogWarning("[DiscoveryMarkerSpawner] MissionService 인스턴스 없음 — 마커 spawn 불가.");
                return;
            }

            // 이미 발견된 항목 spawn (Save 시스템 도입 후엔 복원된 것들이 여기서 처리됨)
            foreach (var id in _missionService.DiscoveredIds)
            {
                var disc = FindDiscoveryById(id);
                if (disc != null) Spawn(disc);
            }

            // 새 발견 listen
            _missionService.onDiscoveryRegistered.AddListener(Spawn);
        }

        private void OnDestroy()
        {
            if (_missionService != null && _missionService.onDiscoveryRegistered != null)
            {
                _missionService.onDiscoveryRegistered.RemoveListener(Spawn);
            }
        }

        private DiscoveryData FindDiscoveryById(string id)
        {
            if (discoveryCatalog == null || discoveryCatalog.all == null) return null;
            foreach (var d in discoveryCatalog.all)
            {
                if (d != null && d.discoveryId == id) return d;
            }
            return null;
        }

        private void Spawn(DiscoveryData discovery)
        {
            if (discovery == null) return;
            if (_spawned.ContainsKey(discovery.discoveryId)) return;

            var worldPos = GeoCoordinate.LatLngToWorld(discovery.latitude, discovery.longitude);
            worldPos.y = markerHeight;

            // ─── Canvas root (World-Space) ───
            var canvasGO = new GameObject($"DiscoveryMarker_{discovery.discoveryId}");
            canvasGO.transform.SetParent(markersParent);
            canvasGO.transform.position = worldPos;
            // 평평하게 누여서 위에서 보이게 (XZ 평면 위에 스티커처럼)
            canvasGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(100f, 100f); // 100 픽셀 기준
            canvasGO.transform.localScale = Vector3.one * (markerSize / 100f); // 픽셀 → 월드 환산

            // ─── Button 자식 ───
            var btnGO = new GameObject("Button");
            btnGO.transform.SetParent(canvasGO.transform, false);

            var btnRect = btnGO.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(100f, 100f);
            btnRect.anchoredPosition = Vector2.zero;

            var image = btnGO.AddComponent<Image>();
            var button = btnGO.AddComponent<Button>();
            button.targetGraphic = image;

            // ─── Marker 스크립트 ───
            var marker = canvasGO.AddComponent<DiscoveryMarker>();
            marker.Bind(discovery, button, image, reopenPanel);

            _spawned.Add(discovery.discoveryId, canvasGO);
        }
    }
}
