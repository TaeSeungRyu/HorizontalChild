using Game.Data;
using Game.Ship;
using Game.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 항구 도착 시 표시되는 다이얼로그.
    /// "리스본에 들어가시겠습니까?" + [예 / 취소] 버튼.
    ///
    /// SeaWorldManager.onPortArrived 이벤트 구독.
    ///   - 예 → PortScreen 열기 (외부 컴포넌트 호출)
    ///   - 취소 → 패널 닫기 + 떠난 후 즉시 재트리거 방지 (suppress)
    ///
    /// 사용:
    ///   Canvas 하위에 빈 패널 GameObject 만들고 본 컴포넌트 부착.
    ///   필드에 TitleText / MessageText / YesButton / CancelButton 할당.
    ///   SeaWorldManager 의 OnPortArrived 이벤트에 본 컴포넌트의 Show(PortData) 메서드 등록.
    /// </summary>
    public class PortArrivalDialog : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("이 패널 GameObject. 비어 있으면 본 컴포넌트가 붙은 객체를 사용.")]
        public GameObject panelRoot;

        public TMP_Text titleText;
        public TMP_Text messageText;
        public Button yesButton;
        public Button cancelButton;

        [Header("Linked Scenes")]
        [Tooltip("'예' 클릭 시 활성화할 항구 화면 컴포넌트.")]
        public PortScreen portScreen;

        [Tooltip("'취소' 시 항구 재트리거 방지를 위한 SeaWorldManager 참조.")]
        public SeaWorldManager worldManager;

        [Tooltip("일시 정지할 ShipController — 다이얼로그 떠 있는 동안 배 이동 중단.")]
        public ShipController playerShip;

        private PortData _currentPort;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);

            if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
        }

        /// <summary>외부에서 호출 — SeaWorldManager 의 onPortArrived 에 등록.</summary>
        public void Show(PortData port)
        {
            if (port == null) return;
            if (panelRoot == null) panelRoot = gameObject; // 비활성 GameObject 호출 안전장치

            _currentPort = port;

            if (titleText != null)
            {
                titleText.text = $"{port.displayNameKo} 도착";
            }

            if (messageText != null)
            {
                messageText.text = $"{port.displayNameKo} 에 들어가시겠습니까?";
            }

            panelRoot.SetActive(true);

            // 다이얼로그 떠 있는 동안 배 정지
            if (playerShip != null) playerShip.HardStop();
        }

        private void OnYesClicked()
        {
            panelRoot.SetActive(false);
            if (portScreen != null && _currentPort != null)
            {
                portScreen.OpenForPort(_currentPort, this);
            }
            // 닫고 항구 화면 열림 — 그 안에서 다시 SeaWorldManager.SuppressPort 호출됨
        }

        private void OnCancelClicked()
        {
            panelRoot.SetActive(false);
            // 같은 항구를 즉시 다시 트리거하지 않도록 (멀리 가야 다시 트리거)
            if (worldManager != null && _currentPort != null)
            {
                worldManager.SuppressPort(_currentPort.portId);
            }
            _currentPort = null;
        }

        /// <summary>PortScreen 이 닫힌 후 호출 — 같은 항구 재트리거 방지.</summary>
        public void OnPortScreenClosed()
        {
            if (worldManager != null && _currentPort != null)
            {
                worldManager.SuppressPort(_currentPort.portId);
            }
            _currentPort = null;
        }
    }
}
