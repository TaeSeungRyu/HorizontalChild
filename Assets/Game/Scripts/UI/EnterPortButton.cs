using Game.Data;
using Game.Ship;
using Game.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// "항구 들어가기" 버튼 — 항해 중 화면에 떠 있는 UI 버튼.
    ///
    /// 동작:
    ///   - 클릭 → 가장 가까운 항구를 찾음
    ///   - 그 항구가 SeaWorldManager.clickEnterRadiusUnits 이내면 입항 다이얼로그 표시
    ///   - 거리 밖이면 statusText 에 안내 ("가까운 항구가 너무 멀어요")
    ///
    /// 시각:
    ///   - AnchorButton 과 동일하게 CanvasGroup 으로 hide while 패널 떠있을 때 숨김
    ///
    /// 모바일 호환: UI 버튼이라 터치/마우스 모두 자동 처리.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class EnterPortButton : MonoBehaviour
    {
        [Header("Refs")]
        public Button button;

        [Tooltip("상태 메시지 표시. 비어 있으면 Console 로그만.")]
        public TMP_Text statusText;
        [Range(0f, 10f)] public float statusVisibleSeconds = 3f;

        [Header("Game Refs")]
        public ShipController playerShip;
        public SeaWorldManager worldManager;

        [Header("Visibility")]
        [Tooltip("이 GameObject 들 중 하나라도 활성이면 본 버튼/메시지를 숨김.")]
        public GameObject[] hideWhileAnyActive;

        private CanvasGroup _canvasGroup;
        private float _statusHideAt;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(OnClicked);
            ClearStatus();
        }

        private void Update()
        {
            UpdateVisibility();

            if (statusText != null && statusText.gameObject.activeSelf &&
                _statusHideAt > 0f && Time.unscaledTime >= _statusHideAt)
            {
                ClearStatus();
            }
        }

        private void UpdateVisibility()
        {
            bool shouldHide = false;
            if (hideWhileAnyActive != null)
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

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = shouldHide ? 0f : 1f;
                _canvasGroup.interactable = !shouldHide;
                _canvasGroup.blocksRaycasts = !shouldHide;
            }

            if (shouldHide && statusText != null && statusText.gameObject.activeSelf)
            {
                ClearStatus();
            }
        }

        private void OnClicked()
        {
            if (worldManager == null || playerShip == null)
            {
                Debug.LogError("[EnterPortButton] worldManager 또는 playerShip 미할당.");
                return;
            }

            // 가장 가까운 항구 찾기
            PortData closest = FindNearestPort(out float closestDist);
            if (closest == null)
            {
                ShowStatus("들어갈 수 있는 항구가 없어요.");
                return;
            }

            float maxDist = worldManager.clickEnterRadiusUnits;
            if (closestDist <= maxDist)
            {
                // 입항 가능 — TryEnterPortFromClick 으로 동일 흐름
                worldManager.TryEnterPortFromClick(closest);
            }
            else
            {
                ShowStatus($"{closest.displayNameKo} 까지 너무 멀어요.\n좀 더 가까이 가야 들어갈 수 있어요.");
            }
        }

        private PortData FindNearestPort(out float distance)
        {
            distance = float.MaxValue;
            PortData closest = null;
            // Catalog 우선 — activePorts 는 fallback (보통 비어있음)
            var ports = worldManager.EffectivePorts;
            if (ports == null) return null;

            var shipPos = playerShip.transform.position;
            foreach (var port in ports)
            {
                if (port == null) continue;
                var portPos = GeoCoordinate.LatLngToWorld(port.latitude, port.longitude);
                float dist = Vector3.Distance(shipPos, portPos);
                if (dist < distance)
                {
                    distance = dist;
                    closest = port;
                }
            }
            return closest;
        }

        // ─── 상태 메시지 ────────────────────────────────────────────────────

        private void ShowStatus(string msg)
        {
            if (statusText == null)
            {
                Debug.Log($"[EnterPortButton] {msg}");
                return;
            }
            statusText.text = msg;
            statusText.gameObject.SetActive(true);
            _statusHideAt = statusVisibleSeconds > 0f
                ? Time.unscaledTime + statusVisibleSeconds
                : 0f;
        }

        private void ClearStatus()
        {
            if (statusText == null) return;
            statusText.text = "";
            statusText.gameObject.SetActive(false);
            _statusHideAt = 0f;
        }
    }
}
