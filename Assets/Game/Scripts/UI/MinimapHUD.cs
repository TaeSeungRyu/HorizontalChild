using Game.Ship;
using Game.World;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 미니맵 — 우상단 화면에 작은 세계지도 + 플레이어 위치 마커.
    ///
    /// 1차 구현: 정적 베이스맵(HYP_LR_SR_W 같은 세계지도 텍스처) + 플레이어 위치 마커.
    /// 마커는 lat/lng → UV → 베이스맵 RectTransform 내 좌표로 환산.
    ///
    /// 인스펙터 셋업 (간략):
    ///   Canvas (Screen Space Overlay)
    ///     MinimapPanel (이 컴포넌트 부착 / 우상단 anchor)
    ///       BasemapImage (RawImage + 세계지도 텍스처)
    ///         PlayerMarker (Image — 화살표/점, BasemapImage 의 자식)
    ///
    /// 자세한 좌표·앵커 값은 MINIMAP_SETUP_GUIDE.md 참조.
    /// </summary>
    public class MinimapHUD : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("미니맵 배경 RawImage. 텍스처에 세계지도 (예: HYP_LR_SR_W.tif).")]
        public RawImage basemapImage;

        [Tooltip("플레이어 위치 마커 (Image). BasemapImage 의 자식이어야 함.")]
        public RectTransform playerMarker;

        [Tooltip("플레이어 배. 비어 있으면 씬에서 자동 검색.")]
        public ShipController playerShip;

        [Header("Behavior")]
        [Tooltip("☑ 면 배의 진행 방향에 따라 마커 회전. 화살표 마커 권장.")]
        public bool rotateMarkerByHeading = true;

        private RectTransform _basemapRect;

        private void Awake()
        {
            if (basemapImage != null) _basemapRect = basemapImage.rectTransform;
        }

        private void Start()
        {
            if (playerShip == null)
            {
                playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);
            }
        }

        private void Update()
        {
            if (playerShip == null || _basemapRect == null || playerMarker == null) return;

            // 월드 좌표 → 위/경도 → UV (0..1) → 베이스맵 내 로컬 픽셀 위치
            var coords = GeoCoordinate.WorldToLatLng(playerShip.transform.position);
            float u = Mathf.Clamp01((coords.longitude + 180f) / 360f);
            float v = Mathf.Clamp01((coords.latitude + 90f) / 180f);

            var rect = _basemapRect.rect;
            float x = (u - 0.5f) * rect.width;
            float y = (v - 0.5f) * rect.height;
            playerMarker.anchoredPosition = new Vector2(x, y);

            if (rotateMarkerByHeading)
            {
                // 배의 진행 방향(yaw) — +Z(북쪽) 기준 atan2
                var fwd = playerShip.transform.forward;
                float headingDeg = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
                // UI 회전은 Z축, 시계 반대 방향이 양수 → 음수 부호로 정렬
                playerMarker.localEulerAngles = new Vector3(0f, 0f, -headingDeg);
            }
        }
    }
}
