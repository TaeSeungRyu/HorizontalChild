using System.Collections.Generic;
using Game.Data;
using Game.Missions;
using Game.UI;
using UnityEngine;
using UnityEngine.EventSystems;

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
    /// 마커:
    ///   markerPrefab 으로 지정된 3D 모델 (kenney_pirate-kit/chest.fbx 권장).
    ///   Collider 가 없으면 BoxCollider 자동 추가.
    ///   카테고리별 머티리얼 tint (옵션).
    ///
    /// 클릭 처리:
    ///   IPointerClickHandler — Main Camera 에 PhysicsRaycaster 가 있어야 함.
    ///   없으면 Start 에서 경고 출력 → 사용자가 추가 (Add Component → Physics Raycaster).
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

        [Header("Marker Prefab")]
        [Tooltip("마커 3D 모델. Assets/kenney_pirate-kit/Models/FBX format/chest.fbx 드래그 권장.")]
        public GameObject markerPrefab;

        [Header("Visual")]
        [Tooltip("마커 Y 좌표. WorldLand BaseY 보다 약간 위.")]
        public float markerHeight = 3f;

        [Tooltip("마커 크기 배수. chest 기본 크기(약 1unit)에 곱해짐. 5 ≈ 37 km.")]
        public float markerScale = 5f;

        [Tooltip("☑ 면 마커 머티리얼을 카테고리별 색으로 칠함. 자연 색 유지하려면 끔.")]
        public bool tintByCategory = true;

        private MissionService _missionService;
        private readonly Dictionary<string, GameObject> _spawned = new();

        private void Start()
        {
            if (markersParent == null) markersParent = transform;
            // 비활성 객체도 검색 — DiscoveryFoundPanel 은 Awake 에서 자기 자신을 비활성화하므로 기본 검색이 못 찾음
            if (reopenPanel == null)
                reopenPanel = FindAnyObjectByType<DiscoveryFoundPanel>(FindObjectsInactive.Include);

            _missionService = MissionService.Instance;
            if (_missionService == null)
            {
                Debug.LogWarning("[DiscoveryMarkerSpawner] MissionService 인스턴스 없음 — 마커 spawn 불가.");
                return;
            }

            // Main Camera 에 PhysicsRaycaster 자동 추가 (클릭 처리에 필수)
            var cam = Camera.main;
            if (cam != null && cam.GetComponent<PhysicsRaycaster>() == null)
            {
                cam.gameObject.AddComponent<PhysicsRaycaster>();
                Debug.Log("[DiscoveryMarkerSpawner] Main Camera 에 Physics Raycaster 자동 추가됨.");
            }

            // 이미 발견된 항목 spawn
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
            if (markerPrefab == null)
            {
                Debug.LogWarning("[DiscoveryMarkerSpawner] markerPrefab 이 비어있음 — Inspector 에서 chest.fbx 할당 필요.");
                return;
            }

            var worldPos = GeoCoordinate.LatLngToWorld(discovery.latitude, discovery.longitude);
            worldPos.y = markerHeight;

            var marker = Instantiate(markerPrefab, markersParent);
            marker.transform.position = worldPos;
            marker.transform.localScale = Vector3.one * markerScale;
            marker.name = $"DiscoveryMarker_{discovery.discoveryId}";

            // 카테고리별 머티리얼 tint
            if (tintByCategory)
            {
                var color = DiscoveryMarker.ColorFor(discovery.category);
                foreach (var r in marker.GetComponentsInChildren<Renderer>())
                {
                    var mat = r.material; // 인스턴스 생성 (다른 마커들과 분리)
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                    else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                }
            }

            // 클릭 검출용 Collider — 없으면 자동 추가 (SphereCollider 가 chest 모양에 적합)
            if (marker.GetComponentInChildren<Collider>() == null)
            {
                var sphere = marker.AddComponent<SphereCollider>();
                sphere.radius = 0.7f;   // localScale 배수만큼 자동 적용 (markerScale=5 면 반경 3.5)
                sphere.center = new Vector3(0f, 0.4f, 0f); // chest 중심 부근
            }

            var script = marker.AddComponent<DiscoveryMarker>();
            script.Bind(discovery, reopenPanel);

            _spawned.Add(discovery.discoveryId, marker);
        }
    }
}
