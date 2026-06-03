using Game.Data;
using Game.Missions;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 화면 한 구석에 떠 있는 작은 HUD — 현재 진행 중인 의뢰 표시.
    ///
    /// 표시 상태 3가지:
    ///   1) 의뢰 없음 — "(없음)" + 안내
    ///   2) 의뢰 진행 중 (발견 전) — 의뢰 제목 + "X 을(를) 찾아보세요."
    ///   3) 발견 후 (복귀 대기) — "X 을(를) 찾았어요! 의뢰 항구로 돌아가 보고하세요."
    ///
    /// MissionService 의 4가지 이벤트 자동 구독 → 상태 변화 시 자동 갱신.
    ///
    /// 어린이 친화:
    ///   - 텍스트 짧고 명확
    ///   - 진행 단계가 보이도록 (찾기 → 보고하기)
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ActiveMissionHUD : MonoBehaviour
    {
        [Header("Refs")]
        public TMP_Text titleText;          // 고정: "현재 의뢰"
        public TMP_Text missionTitleText;   // 의뢰 제목 또는 "(없음)"
        public TMP_Text progressText;       // 단계별 안내

        [Header("Service")]
        public MissionService missionService;

        [Header("Visibility")]
        [Tooltip("이 GameObject 들 중 하나라도 활성이면 HUD 를 숨김. PortScreen/MissionGiverPanel/MissionCompletedPanel/DiscoveryFoundPanel 등록.")]
        public GameObject[] hideWhileAnyActive;

        private CanvasGroup _canvasGroup;

        // ─── 라이프사이클 ────────────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void Start()
        {
            if (missionService == null) missionService = MissionService.Instance;
            if (missionService != null)
            {
                missionService.onMissionAccepted.AddListener(OnMissionEvent);
                missionService.onMissionCancelled.AddListener(OnMissionEvent);
                missionService.onMissionCompleted.AddListener(OnMissionEvent);
                missionService.onDiscoveryRegistered.AddListener(OnDiscoveryEvent);
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (missionService != null)
            {
                missionService.onMissionAccepted.RemoveListener(OnMissionEvent);
                missionService.onMissionCancelled.RemoveListener(OnMissionEvent);
                missionService.onMissionCompleted.RemoveListener(OnMissionEvent);
                missionService.onDiscoveryRegistered.RemoveListener(OnDiscoveryEvent);
            }
        }

        private void Update()
        {
            UpdateVisibility();
        }

        // ─── 이벤트 핸들러 ──────────────────────────────────────────────────

        private void OnMissionEvent(MissionTemplate _) => Refresh();
        private void OnDiscoveryEvent(DiscoveryData _) => Refresh();

        // ─── 표시 ────────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (titleText != null) titleText.text = "현재 의뢰";

            if (missionService == null || missionService.CurrentMission == null)
            {
                ShowNoMission();
                return;
            }

            ShowMission(missionService.CurrentMission);
        }

        private void ShowNoMission()
        {
            if (missionTitleText != null) missionTitleText.text = "(없음)";
            if (progressText != null) progressText.text = "항구에서 의뢰를 받아요.";
        }

        private void ShowMission(MissionTemplate m)
        {
            if (missionTitleText != null) missionTitleText.text = m.title;
            if (progressText == null) return;

            // 2026-05-31 단순화: 모든 의뢰는 발견물 의뢰
            if (m.targetDiscovery != null)
            {
                bool discovered = missionService.DiscoveredIds.Contains(m.targetDiscovery.discoveryId);
                if (discovered)
                {
                    string portName = m.issuerPort != null ? m.issuerPort.displayNameKo : "의뢰 항구";
                    progressText.text =
                        $"{m.targetDiscovery.displayNameKo} 을(를) 찾았어요!\n{portName} 으로 돌아가 보고하세요.";
                }
                else
                {
                    progressText.text = $"{m.targetDiscovery.displayNameKo} 을(를) 찾아보세요.";
                }
            }
            else
            {
                progressText.text = "";
            }
        }

        // ─── 가시성 ──────────────────────────────────────────────────────────

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
        }
    }
}
