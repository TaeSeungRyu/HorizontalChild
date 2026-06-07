using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 일시정지 메뉴 버튼 — HUD 어딘가에 부착 (예: 상단 좌측).
    /// 클릭 시 PauseMenuPanel 토글. Android Back 키도 같은 토글.
    /// JournalButton 패턴 (CanvasGroup + hideUntilNationSelected).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PauseButton : MonoBehaviour
    {
        [Header("Refs")]
        public Button button;
        public PauseMenuPanel pauseMenuPanel;

        [Header("Visibility")]
        public bool hideUntilNationSelected = true;

        [Tooltip("이 GameObject 들 중 하나라도 활성이면 본 버튼 숨김.")]
        public GameObject[] hideWhileAnyActive;

        [Header("Android Back Key")]
        [Tooltip("☑ 면 Android Back 키 / Esc 로도 일시정지 토글.")]
        public bool listenBackKey = true;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(OnClicked);
        }

        private void Update()
        {
            UpdateVisibility();

            // Android Back / Esc 키로 일시정지 토글 (Input System)
            if (listenBackKey && pauseMenuPanel != null)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                {
                    pauseMenuPanel.Toggle();
                }
            }
        }

        private void OnClicked()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.Toggle();
        }

        private void UpdateVisibility()
        {
            bool shouldHide = false;

            if (hideUntilNationSelected)
            {
                var session = GameSession.Instance;
                if (session == null || session.SelectedNation == null) shouldHide = true;
            }

            if (!shouldHide && hideWhileAnyActive != null)
            {
                foreach (var go in hideWhileAnyActive)
                {
                    if (go != null && go.activeInHierarchy) { shouldHide = true; break; }
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
