using System.Collections.Generic;
using Game.Data;
using Game.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 모험가 조합 — 의뢰 발급 패널.
    ///
    /// PortScreen 의 "모험가 조합" 버튼 클릭 시 OpenForPort(port) 호출됨.
    /// 해당 항구에서 발급 가능한 의뢰 중 첫 번째를 표시 (M1 단순화).
    ///
    /// 상태별 표시:
    ///   - 이미 다른 의뢰 진행 중 → "이미 다른 의뢰 진행 중이에요" 안내
    ///   - 발급 가능 의뢰 있음 → 의뢰 제목/설명 + [수락] 버튼
    ///   - 발급 가능 의뢰 없음 → "지금은 받을 의뢰가 없어요"
    ///
    /// M2 이후: 의뢰 여러 개를 ListView 로 펼침. 현재는 첫 번째만.
    /// </summary>
    public class MissionGiverPanel : MonoBehaviour
    {
        [Header("Refs")]
        public GameObject panelRoot;

        public TMP_Text portNameText;
        public TMP_Text missionTitleText;
        public TMP_Text missionDescriptionText;
        public TMP_Text statusText;
        public Button acceptButton;
        public Button closeButton;

        [Header("Service")]
        [Tooltip("MissionService 참조. 비어 있으면 런타임에 MissionService.Instance 자동 사용.")]
        public MissionService missionService;

        [Header("Parent UI")]
        [Tooltip("이 패널이 열려 있는 동안 숨겼다가, 닫으면 다시 표시할 부모 패널. 보통 PortScreen 의 panelRoot.")]
        public GameObject parentPanelToHide;

        private PortData _currentPort;
        private MissionTemplate _displayedMission;
        private bool _parentWasVisible;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);

            if (acceptButton != null) acceptButton.onClick.AddListener(OnAcceptClicked);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        /// <summary>PortScreen 또는 다른 UI 에서 호출 — 항구의 모험가 조합 패널 열기.</summary>
        public void OpenForPort(PortData port)
        {
            if (port == null) return;
            if (panelRoot == null) panelRoot = gameObject; // 비활성 GameObject 호출 안전장치
            if (missionService == null) missionService = MissionService.Instance;
            if (missionService == null)
            {
                Debug.LogError("[MissionGiverPanel] MissionService 인스턴스가 씬에 없음.");
                return;
            }

            _currentPort = port;

            // 부모 패널 숨김 (화면 겹침 방지)
            if (parentPanelToHide != null)
            {
                _parentWasVisible = parentPanelToHide.activeSelf;
                parentPanelToHide.SetActive(false);
            }

            RefreshDisplay();
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            panelRoot.SetActive(false);

            // 부모 패널 복귀
            if (parentPanelToHide != null && _parentWasVisible)
            {
                parentPanelToHide.SetActive(true);
            }

            _currentPort = null;
            _displayedMission = null;
        }

        private void RefreshDisplay()
        {
            if (portNameText != null)
            {
                portNameText.text = $"{_currentPort.displayNameKo} 모험가 조합";
            }

            // 이미 다른 의뢰 진행 중?
            if (missionService.HasActiveMission)
            {
                ShowAlreadyActiveState();
                return;
            }

            // 이 항구의 발급 가능 의뢰
            List<MissionTemplate> available =
                missionService.GetAvailableMissionsForPort(_currentPort);

            if (available.Count == 0)
            {
                ShowNoMissionsState();
                return;
            }

            _displayedMission = available[0];
            ShowMissionState(_displayedMission);
        }

        private void ShowAlreadyActiveState()
        {
            _displayedMission = null;
            if (statusText != null)
            {
                statusText.text = "이미 다른 의뢰를 받고 있어요.\n먼저 그 의뢰를 마치고 다시 오세요.";
            }
            SetMissionFieldsActive(false);
            if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        }

        private void ShowNoMissionsState()
        {
            _displayedMission = null;
            if (statusText != null)
            {
                statusText.text = "지금은 받을 의뢰가 없어요.\n다른 항구에 가 보세요.";
            }
            SetMissionFieldsActive(false);
            if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        }

        private void ShowMissionState(MissionTemplate mission)
        {
            if (statusText != null) statusText.text = "";
            SetMissionFieldsActive(true);

            if (missionTitleText != null) missionTitleText.text = mission.title;
            if (missionDescriptionText != null) missionDescriptionText.text = mission.description;

            if (acceptButton != null) acceptButton.gameObject.SetActive(true);
        }

        private void SetMissionFieldsActive(bool active)
        {
            if (missionTitleText != null) missionTitleText.gameObject.SetActive(active);
            if (missionDescriptionText != null) missionDescriptionText.gameObject.SetActive(active);
        }

        private void OnAcceptClicked()
        {
            if (_displayedMission == null) return;
            if (missionService.TryAcceptMission(_displayedMission))
            {
                Close();
            }
        }
    }
}
