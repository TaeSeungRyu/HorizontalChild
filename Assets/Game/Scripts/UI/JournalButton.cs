using Game.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// "도감" 버튼 — 화면에 떠 있는 작은 버튼.
    /// 클릭 시 JournalPanel 열기.
    /// 패널 떠 있는 동안 자동 숨김 (다른 HUD 와 같은 패턴).
    ///
    /// 추가 안전망: GameSession.SelectedNation 이 null 인 동안 (= 국가 선택 전)
    /// 무조건 숨김. 인스펙터 hideWhileAnyActive 설정에 의존하지 않음.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class JournalButton : MonoBehaviour
    {
        [Header("Refs")]
        public Button button;
        public JournalPanel journalPanel;

        [Header("Visibility")]
        [Tooltip("이 GameObject 들 중 하나라도 활성이면 본 버튼을 숨김.")]
        public GameObject[] hideWhileAnyActive;

        [Tooltip("☑ 면 GameSession.SelectedNation 이 null 일 때 (국가 선택 전) 자동 숨김. 기본 ☑ 추천.")]
        public bool hideUntilNationSelected = true;

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
        }

        private void OnClicked()
        {
            if (journalPanel != null) journalPanel.Show();
        }

        private void UpdateVisibility()
        {
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

            // 명시적 hide 리스트 (다른 패널들)
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

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = shouldHide ? 0f : 1f;
                _canvasGroup.interactable = !shouldHide;
                _canvasGroup.blocksRaycasts = !shouldHide;
            }
        }
    }
}
