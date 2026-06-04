using Game.Player;
using Game.Ship;
using Game.World;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 미니맵 — 우상단 화면에 작은 세계지도 + 플레이어 위치 마커.
    ///
    /// 1차 구현: 정적 베이스맵(HYP_LR_SR_W 또는 EARTH.jpg 같은 세계지도 텍스처) + 플레이어 위치 마커.
    /// 마커는 lat/lng → UV → 베이스맵 RectTransform 내 좌표로 환산.
    ///
    /// 표시 제어 (JournalButton 과 동일 패턴):
    ///   - GameSession.SelectedNation 이 null 인 동안 (국가 선택 전) 자동 숨김
    ///   - hideWhileAnyActive 배열에 등록된 GameObject 가 하나라도 활성이면 숨김
    ///   - CanvasGroup 으로 alpha/interactable/blocksRaycasts 통합 제어
    ///
    /// 자세한 좌표·앵커 값은 MINIMAP_SETUP_GUIDE.md 참조.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class MinimapHUD : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("미니맵 배경 RawImage. 텍스처에 세계지도 (예: EARTH.jpg).")]
        public RawImage basemapImage;

        [Tooltip("플레이어 위치 마커 (Image). BasemapImage 의 자식이어야 함.")]
        public RectTransform playerMarker;

        [Tooltip("플레이어 배. 비어 있으면 씬에서 자동 검색.")]
        public ShipController playerShip;

        [Header("Behavior")]
        [Tooltip("☑ 면 배의 진행 방향에 따라 마커 회전. 화살표 마커 권장.")]
        public bool rotateMarkerByHeading = true;

        [Header("Visibility")]
        [Tooltip("☑ 면 GameSession.SelectedNation 이 null 일 때 (국가 선택 전) 자동 숨김.")]
        public bool hideUntilNationSelected = true;

        [Tooltip("이 GameObject 들 중 하나라도 활성이면 미니맵을 숨김. 항구 화면·도감 패널 등 풀스크린 UI 등록.")]
        public GameObject[] hideWhileAnyActive;

        private RectTransform _basemapRect;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            if (basemapImage != null) _basemapRect = basemapImage.rectTransform;
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
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
            UpdateVisibility();
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

        private void UpdateVisibility()
        {
            if (_canvasGroup == null) return;

            bool shouldHide = false;

            // 안전망 — 국가 선택 전에는 무조건 숨김
            if (hideUntilNationSelected)
            {
                var session = GameSession.Instance;
                if (session == null || session.SelectedNation == null)
                {
                    shouldHide = true;
                }
            }

            // 명시적 hide 리스트
            if (!shouldHide && hideWhileAnyActive != null)
            {
                foreach (var go in hideWhileAnyActive)
                {
                    if (go != null && go.activeInHierarchy)
                    {
                        shouldHide = true;
                        break;
                    }
                }
            }

            _canvasGroup.alpha = shouldHide ? 0f : 1f;
            _canvasGroup.interactable = !shouldHide;
            _canvasGroup.blocksRaycasts = !shouldHide;
        }
    }
}
