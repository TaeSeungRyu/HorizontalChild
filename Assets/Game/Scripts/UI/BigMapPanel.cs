using Game.Data;
using Game.Missions;
using Game.Ship;
using Game.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 큰 지도 모드 — 전체 화면 세계 지도. 모든 항구 + 발견한 발견물 + 플레이어 위치 표시.
    ///
    /// 동작:
    ///   - Show() 호출 시 패널 활성 + 마커 일괄 spawn (항구 + 발견한 발견물)
    ///   - Hide() 호출 시 패널 비활성
    ///   - 매 프레임 플레이어 위치 마커 갱신
    ///   - 항구 마커 탭 시 onPortSelected 이벤트 발행 (옵션)
    ///
    /// UI 구조 (사용자가 인스펙터에서 설정):
    ///   BigMapPanel (이 컴포넌트)
    ///   ├ Background (Image — 어두운 풀스크린 오버레이)
    ///   ├ MapContainer
    ///   │  └ BasemapImage (RawImage + EARTH.jpg)
    ///   │     └ MarkersContainer (RectTransform anchor stretch — 마커들의 부모)
    ///   │        └ PlayerMarker (Image — 플레이어 위치)
    ///   ├ TitleText (TMP_Text — "세계 지도")
    ///   └ CloseButton (Button)
    ///
    /// 자세한 셋업은 BIG_MAP_SETUP_GUIDE.md 참조.
    /// </summary>
    public class BigMapPanel : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Header("Refs — UI")]
        [Tooltip("BasemapImage 의 RawImage. 좌표 → UV 환산에 사용.")]
        public RawImage basemapImage;

        [Tooltip("마커들의 부모 RectTransform. BasemapImage 와 같은 크기·앵커 권장.")]
        public RectTransform markersContainer;

        public RectTransform playerMarker;
        public TMP_Text titleText;
        public Button closeButton;

        [Header("Refs — Data")]
        public PortCatalog portCatalog;
        public DiscoveryCatalog discoveryCatalog;
        public MissionService missionService;
        public ShipController playerShip;

        [Header("Marker Visual")]
        [Tooltip("항구 마커 크기 (픽셀).")]
        public float portMarkerSize = 18f;

        [Tooltip("발견물 마커 크기 (픽셀).")]
        public float discoveryMarkerSize = 14f;

        [Tooltip("항구 마커 색.")]
        public Color portMarkerColor = new Color(0.95f, 0.25f, 0.25f);

        // ─── 런타임 상태 ───────────────────────────────────────────────────

        private bool _markersBuilt;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);

            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        private void Update()
        {
            if (panelRoot != null && panelRoot.activeInHierarchy)
            {
                UpdatePlayerMarker();
            }
        }

        // ─── Show / Hide ──────────────────────────────────────────────────

        public void Show()
        {
            if (panelRoot == null) panelRoot = gameObject;
            if (missionService == null) missionService = MissionService.Instance;
            if (playerShip == null) playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);

            if (titleText != null) titleText.text = "세계 지도";

            // 매번 마커 새로 만들기 (발견물이 새로 추가됐을 수 있음)
            RefreshMarkers();
            panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void Toggle()
        {
            if (panelRoot == null) panelRoot = gameObject;
            if (panelRoot.activeInHierarchy) Hide();
            else Show();
        }

        // ─── 마커 생성 ────────────────────────────────────────────────────

        private void RefreshMarkers()
        {
            if (markersContainer == null) return;

            // 기존 마커 제거 (playerMarker 는 보존)
            for (int i = markersContainer.childCount - 1; i >= 0; i--)
            {
                var child = markersContainer.GetChild(i);
                if (playerMarker != null && child == playerMarker) continue;
                Destroy(child.gameObject);
            }

            // 항구 마커
            if (portCatalog != null && portCatalog.all != null)
            {
                foreach (var port in portCatalog.all)
                {
                    if (port == null) continue;
                    SpawnMarker(
                        $"PortMarker_{port.portId}",
                        port.latitude, port.longitude,
                        portMarkerSize, portMarkerColor);
                }
            }

            // 발견한 발견물 마커만
            if (discoveryCatalog != null && discoveryCatalog.all != null && missionService != null)
            {
                foreach (var disc in discoveryCatalog.all)
                {
                    if (disc == null) continue;
                    if (!missionService.DiscoveredIds.Contains(disc.discoveryId)) continue;
                    SpawnMarker(
                        $"DiscMarker_{disc.discoveryId}",
                        disc.latitude, disc.longitude,
                        discoveryMarkerSize,
                        DiscoveryMarker.ColorFor(disc.category));
                }
            }

            _markersBuilt = true;
        }

        private void SpawnMarker(string name, float lat, float lng, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(markersContainer, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = LatLngToMapPosition(lat, lng);

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false; // 1차에선 클릭 X — 추후 항구 선택 가능
        }

        private void UpdatePlayerMarker()
        {
            if (playerShip == null || playerMarker == null) return;
            var coords = GeoCoordinate.WorldToLatLng(playerShip.transform.position);
            playerMarker.anchoredPosition = LatLngToMapPosition(coords.latitude, coords.longitude);
        }

        // ─── 좌표 변환 ────────────────────────────────────────────────────

        /// <summary>
        /// 위/경도 → MarkersContainer 내 anchoredPosition.
        /// equirectangular 투영: lng [-180, 180] → x, lat [-90, 90] → y.
        /// </summary>
        private Vector2 LatLngToMapPosition(float lat, float lng)
        {
            if (markersContainer == null) return Vector2.zero;
            float u = Mathf.Clamp01((lng + 180f) / 360f);
            float v = Mathf.Clamp01((lat + 90f) / 180f);
            var rect = markersContainer.rect;
            return new Vector2((u - 0.5f) * rect.width, (v - 0.5f) * rect.height);
        }
    }
}
