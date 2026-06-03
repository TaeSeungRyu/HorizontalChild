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
    /// "정박 및 탐색" 버튼 — 항해 중 화면에 떠 있는 UI 버튼.
    ///
    /// 동작 (기획서 준수):
    ///   - 활성 의뢰가 Discovery 타입이고 targetDiscovery 가 있어야 의미 있음
    ///   - 그 발견물 좌표 ±tolerance 안에 있으면 발견 처리 (MissionService.RegisterDiscovery + 패널 표시)
    ///   - 거리 밖이면 안내 메시지 ("아무것도 못 찾았어요")
    ///   - 활성 의뢰 없거나 발견물 의뢰가 아니면 "지금은 정박할 이유 없어요"
    ///
    /// "발견물은 오직 미션을 통해서만 찾을 수 있어야 함" (`GAME_MECHANICS.md` §5.5) 룰 적용.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class AnchorButton : MonoBehaviour
    {
        [Header("Refs")]
        public Button button;

        [Tooltip("결과를 짧게 표시할 텍스트. 비어 있으면 Console 로그만.")]
        public TMP_Text statusText;

        [Tooltip("정박 결과(성공/실패) 메시지를 자동으로 사라지게 할 시간.")]
        [Range(0f, 10f)] public float statusVisibleSeconds = 3f;

        [Header("Game Refs")]
        public ShipController playerShip;
        public MissionService missionService;
        public DiscoveryFoundPanel discoveryFoundPanel;

        [Header("Visibility")]
        [Tooltip("이 GameObject 들 중 하나라도 활성이면 본 버튼/메시지를 숨김. " +
                 "PortArrivalDialog / PortScreen / MissionGiverPanel / DiscoveryFoundPanel 등을 등록.")]
        public GameObject[] hideWhileAnyActive;

        private float _statusHideAt;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(OnAnchorClicked);
            ClearStatus();
        }

        private void Start()
        {
            if (missionService == null) missionService = MissionService.Instance;
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

        /// <summary>
        /// 다른 패널이 떠 있는 동안 본 버튼·메시지 모두 숨김.
        /// 매 프레임 체크 — CanvasGroup.alpha 로 가림 (자기 GameObject 는 활성 유지해서 Update 계속).
        /// </summary>
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

            // statusText 는 별도 GameObject — 패널 떠 있는 동안 메시지도 숨김
            if (shouldHide && statusText != null && statusText.gameObject.activeSelf)
            {
                ClearStatus();
            }
        }

        private void OnAnchorClicked()
        {
            if (missionService == null) missionService = MissionService.Instance;
            if (missionService == null)
            {
                Debug.LogError("[AnchorButton] MissionService 없음");
                return;
            }
            if (playerShip == null)
            {
                Debug.LogError("[AnchorButton] PlayerShip 없음");
                return;
            }

            // 항해 중에 자동 정지 (어린이 친화 — 일단 멈추고 결과 보기)
            playerShip.HardStop();

            // 의뢰 확인 (모든 의뢰는 발견물 의뢰 — 2026-05-31 단순화)
            var mission = missionService.CurrentMission;
            if (mission == null || mission.targetDiscovery == null)
            {
                ShowStatus("지금은 정박해도 찾을 게 없어요.\n먼저 의뢰를 받아 보세요.");
                return;
            }

            var target = mission.targetDiscovery;

            // 이미 발견한 발견물 — 재검출 방지
            if (missionService.DiscoveredIds.Contains(target.discoveryId))
            {
                ShowStatus($"이미 {target.displayNameKo} 을(를) 찾았어요.\n의뢰를 준 항구로 돌아가 보고하세요.");
                return;
            }

            // 거리 체크 + 눈썰미 보너스
            int keenEye = playerShip.captain != null ? playerShip.captain.keenEye : 50;
            float adjusted = GeoCoordinate.ApplyKeenEyeBonus(target.searchToleranceBase, keenEye);
            float toleranceDist = GeoCoordinate.GetSearchToleranceDistance(adjusted);

            var playerPos = playerShip.transform.position;
            var targetPos = GeoCoordinate.LatLngToWorld(target.latitude, target.longitude);
            float dist = Vector3.Distance(playerPos, targetPos);

            if (dist <= toleranceDist)
            {
                // 발견!
                missionService.RegisterDiscovery(target);
                if (discoveryFoundPanel != null)
                {
                    discoveryFoundPanel.Show(target);
                }
                ClearStatus();
            }
            else
            {
                // 거리 안내 (어린이 친화 — 좌표 직접 노출은 자제, 거리 비율로)
                float ratio = dist / toleranceDist;
                if (ratio < 2f)
                {
                    ShowStatus("거의 다 왔어요! 조금만 더 가 보세요.");
                }
                else if (ratio < 5f)
                {
                    ShowStatus("아직 멀어요. 더 찾아 봐요.");
                }
                else
                {
                    ShowStatus("여기엔 아무것도 없어요. 다른 방향으로 가 봐요.");
                }
            }
        }

        // ─── 상태 메시지 ────────────────────────────────────────────────────

        private void ShowStatus(string msg)
        {
            if (statusText == null)
            {
                Debug.Log($"[AnchorButton] {msg}");
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
