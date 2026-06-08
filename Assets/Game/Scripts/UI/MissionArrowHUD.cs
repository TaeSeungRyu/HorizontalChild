using Game.Missions;
using Game.Player;
using Game.Ship;
using Game.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 의뢰 방향 화살표 HUD — 현재 활성 의뢰의 목적지로 향하는 화살표.
    ///
    /// 동작:
    ///   - MissionService.CurrentMission 있으면 표시, 없으면 자동 숨김
    ///   - 목적지: targetDiscovery 가 아직 미발견 → 발견물, 발견 후 → issuerPort
    ///   - 매 프레임 플레이어 위치 ↔ 목적지 방향 계산
    ///   - 카메라 yaw 보정 (camera follow yaw 켰을 때 회전 영향)
    ///   - 거리 (km) 도 함께 표시 (옵션)
    ///
    /// 시각 셋업:
    ///   화면 어딘가에 작은 화살표 UI Image 부착. Auto Layout 으로 상단 중앙 권장.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class MissionArrowHUD : MonoBehaviour
    {
        [Header("Refs — Auto Layout 가능")]
        [Tooltip("회전할 화살표 Image. UI 의 +Y 방향이 '앞쪽'(목표 방향) 으로 그려진 sprite 권장.")]
        public RectTransform arrowRect;
        public Image arrowImage;
        public TMP_Text labelText;   // "→ 지브롤터 해협 / 850km"

        [Header("Visibility")]
        [Tooltip("이 GameObject 들 중 하나라도 활성이면 화살표 숨김.")]
        public GameObject[] hideWhileAnyActive;

        public bool hideUntilNationSelected = true;

        [Header("Refs — Runtime 자동")]
        public ShipController playerShip;
        public Camera trackingCamera;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void Start()
        {
            if (playerShip == null) playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);
            if (trackingCamera == null) trackingCamera = Camera.main;
        }

        private void Update()
        {
            UpdateArrow();
        }

        private void UpdateArrow()
        {
            // 1) 가시성 체크
            if (!IsAvailable())
            {
                SetVisible(false);
                return;
            }

            // 2) 활성 의뢰의 목적지 계산
            var ms = MissionService.Instance;
            if (ms == null || ms.CurrentMission == null) { SetVisible(false); return; }

            if (!TryGetTargetLatLng(ms, out float tLat, out float tLng, out string targetName))
            {
                SetVisible(false);
                return;
            }

            // 3) 방향·거리 계산
            if (playerShip == null) { SetVisible(false); return; }
            var playerPos = playerShip.transform.position;
            var targetPos = GeoCoordinate.LatLngToWorld(tLat, tLng);

            float dx = targetPos.x - playerPos.x;
            float dz = targetPos.z - playerPos.z;

            float worldAngleDeg = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg; // 0 = +Z (북쪽)
            float cameraYaw = trackingCamera != null ? trackingCamera.transform.eulerAngles.y : 0f;
            float screenAngleDeg = worldAngleDeg - cameraYaw;

            // UI 회전: Z 축 음수 → 시계방향 (UI 평면)
            if (arrowRect != null)
                arrowRect.localEulerAngles = new Vector3(0f, 0f, -screenAngleDeg);

            // 거리 (km)
            if (labelText != null)
            {
                float distUnits = Mathf.Sqrt(dx * dx + dz * dz);
                float distKm = distUnits * GeoCoordinate.KmPerUnit;
                labelText.text = $"→ {targetName} · {distKm:F0}km";
            }

            SetVisible(true);
        }

        private bool TryGetTargetLatLng(MissionService ms, out float lat, out float lng, out string name)
        {
            lat = lng = 0f;
            name = "";
            var mission = ms.CurrentMission;
            if (mission == null) return false;

            // 발견물 아직 미발견 → 발견물 좌표
            if (mission.targetDiscovery != null &&
                !ms.DiscoveredIds.Contains(mission.targetDiscovery.discoveryId))
            {
                lat = mission.targetDiscovery.latitude;
                lng = mission.targetDiscovery.longitude;
                name = mission.targetDiscovery.displayNameKo;
                return true;
            }

            // 발견 후 → issuerPort 로 복귀
            if (mission.issuerPort != null)
            {
                lat = mission.issuerPort.latitude;
                lng = mission.issuerPort.longitude;
                name = $"{mission.issuerPort.displayNameKo} 복귀";
                return true;
            }
            return false;
        }

        private bool IsAvailable()
        {
            if (hideUntilNationSelected)
            {
                var session = GameSession.Instance;
                if (session == null || session.SelectedNation == null) return false;
            }
            if (hideWhileAnyActive != null)
            {
                foreach (var go in hideWhileAnyActive)
                {
                    if (go != null && go.activeInHierarchy) return false;
                }
            }
            return true;
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = false; // 정보 표시 전용, 클릭 X
            _canvasGroup.blocksRaycasts = false;
        }

        // ─── 자동 레이아웃 ──────────────────────────────────────────────────

        [ContextMenu("Auto Layout")]
        private void AutoLayout()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            // 상단 중앙 — 의뢰 진행 시 가장 잘 보이는 위치
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(420f, 80f);
            rt.anchoredPosition = new Vector2(0f, -10f);

            // 화살표 Image — 좌측
            if (arrowRect == null || arrowImage == null)
            {
                var arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
                arrowGO.transform.SetParent(transform, false);
                arrowRect = arrowGO.GetComponent<RectTransform>();
                arrowImage = arrowGO.GetComponent<Image>();
            }
            arrowRect.anchorMin = new Vector2(0f, 0.5f);
            arrowRect.anchorMax = new Vector2(0f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.sizeDelta = new Vector2(60f, 60f);
            arrowRect.anchoredPosition = new Vector2(40f, 0f);
            arrowImage.color = new Color(0.95f, 0.85f, 0.30f); // 노랑
            arrowImage.raycastTarget = false;
            // 기본 사각형 sprite — 사용자가 화살표 sprite 로 교체 권장

            // Label Text — 우측
            if (labelText == null)
            {
                var labelGO = new GameObject("Label", typeof(RectTransform));
                labelGO.transform.SetParent(transform, false);
                labelText = labelGO.AddComponent<TextMeshProUGUI>();
            }
            var labelRT = labelText.rectTransform;
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(1f, 1f);
            labelRT.pivot = new Vector2(0.5f, 0.5f);
            labelRT.sizeDelta = Vector2.zero;
            labelRT.offsetMin = new Vector2(90f, 0f);
            labelRT.offsetMax = new Vector2(-10f, 0f);
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.fontSize = 24f;
            labelText.color = Color.white;
            labelText.text = "→ 의뢰 목적지 · 0km";
            labelText.raycastTarget = false;

            Debug.Log("[MissionArrowHUD] Auto Layout 적용. 기본 사각형 화살표는 sprite 로 교체 권장 (위쪽 향한 삼각형 sprite).");
        }
    }
}
