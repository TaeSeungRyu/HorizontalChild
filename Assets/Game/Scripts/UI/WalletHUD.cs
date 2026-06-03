using Game.Player;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 플레이어 자금·명성 HUD — 화면 한구석에 상시 표시.
    ///
    /// 표시 항목 (어린이 친화):
    ///   - 돈: "5,000 원"
    ///   - 좋은 평판: "100"
    ///   - 나쁜 평판: 0 이면 자동 숨김 (위협적인 정보 노출 자제)
    ///
    /// PlayerState 의 onMoneyChanged/onGoodReputationChanged/onBadReputationChanged 자동 구독.
    /// 보상 받는 순간 자동 갱신.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class WalletHUD : MonoBehaviour
    {
        [Header("Refs")]
        public TMP_Text moneyText;
        public TMP_Text goodReputationText;
        public TMP_Text badReputationText;

        [Header("State")]
        [Tooltip("PlayerState 참조. 비어 있으면 런타임에 PlayerState.Instance 자동 사용.")]
        public PlayerState playerState;

        [Header("Visibility")]
        [Tooltip("이 GameObject 들 중 하나라도 활성이면 HUD 를 숨김.")]
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
            if (playerState == null) playerState = PlayerState.Instance;
            if (playerState != null)
            {
                playerState.onMoneyChanged.AddListener(OnMoneyChanged);
                playerState.onGoodReputationChanged.AddListener(OnGoodRepChanged);
                playerState.onBadReputationChanged.AddListener(OnBadRepChanged);
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (playerState != null)
            {
                playerState.onMoneyChanged.RemoveListener(OnMoneyChanged);
                playerState.onGoodReputationChanged.RemoveListener(OnGoodRepChanged);
                playerState.onBadReputationChanged.RemoveListener(OnBadRepChanged);
            }
        }

        private void Update()
        {
            UpdateVisibility();
        }

        // ─── 갱신 ────────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (playerState == null)
            {
                if (moneyText != null) moneyText.text = "0 원";
                if (goodReputationText != null) goodReputationText.text = "좋은 평판 0";
                if (badReputationText != null) badReputationText.gameObject.SetActive(false);
                return;
            }
            OnMoneyChanged(playerState.Money);
            OnGoodRepChanged(playerState.GoodReputation);
            OnBadRepChanged(playerState.BadReputation);
        }

        private void OnMoneyChanged(int amount)
        {
            if (moneyText != null) moneyText.text = $"{amount:N0} 원";
        }

        private void OnGoodRepChanged(int amount)
        {
            if (goodReputationText != null) goodReputationText.text = $"좋은 평판 {amount:N0}";
        }

        private void OnBadRepChanged(int amount)
        {
            if (badReputationText == null) return;

            // 어린이 친화: 나쁜 평판이 0 이면 텍스트 자체를 숨김
            if (amount > 0)
            {
                badReputationText.gameObject.SetActive(true);
                badReputationText.text = $"위험한 평판 {amount:N0}";
            }
            else
            {
                badReputationText.gameObject.SetActive(false);
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
