using Game.Player;
using Game.Ship;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// HUD 좌상단 — 선장 아바타 + 이름 + 잔돈/명성. 클릭 시 PlayerInfoPanel 열림.
    ///
    /// 통합 정보:
    ///   - 아바타 (원형 placeholder 또는 sprite)
    ///   - 선장 이름
    ///   - 잔돈 · 좋은 명성 · 나쁜 명성 (나쁜 명성은 0 이면 숨김)
    ///
    /// PlayerState 의 onMoneyChanged / Good / Bad 자동 구독 → 실시간 갱신.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PlayerInfoButton : MonoBehaviour
    {
        [Header("Refs")]
        public Image avatarImage;           // 원형 배경 또는 실제 아바타
        public TMP_Text initialText;        // placeholder — 이름 첫 글자
        public TMP_Text nameText;
        public TMP_Text statusText;         // 잔돈 + 명성 (1줄)
        public Button button;

        [Header("Targets")]
        public ShipController playerShip;
        public PlayerInfoPanel infoPanel;
        public PlayerState playerState;

        [Header("Visibility")]
        [Tooltip("이 GameObject 들 중 하나라도 활성이면 버튼 숨김.")]
        public GameObject[] hideWhileAnyActive;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
                button.onClick.AddListener(OnClick);
            }
        }

        private void Start()
        {
            if (playerShip == null) playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);
            if (playerState == null) playerState = PlayerState.Instance;
            if (playerState != null)
            {
                playerState.onMoneyChanged.AddListener(OnWalletChanged);
                playerState.onGoodReputationChanged.AddListener(OnWalletChanged);
                playerState.onBadReputationChanged.AddListener(OnWalletChanged);
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (playerState != null)
            {
                playerState.onMoneyChanged.RemoveListener(OnWalletChanged);
                playerState.onGoodReputationChanged.RemoveListener(OnWalletChanged);
                playerState.onBadReputationChanged.RemoveListener(OnWalletChanged);
            }
        }

        private void OnWalletChanged(int _) => RefreshStatus();

        private void Update()
        {
            // 다른 패널 떠 있을 때 숨김
            bool shouldHide = false;
            if (hideWhileAnyActive != null)
            {
                foreach (var g in hideWhileAnyActive)
                {
                    if (g != null && g.activeInHierarchy) { shouldHide = true; break; }
                }
            }
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = shouldHide ? 0f : 1f;
                _canvasGroup.interactable = !shouldHide;
                _canvasGroup.blocksRaycasts = !shouldHide;
            }
        }

        private void OnClick()
        {
            Refresh();
            if (infoPanel != null) infoPanel.Open();
        }

        public void Refresh()
        {
            string captainName = (playerShip != null && playerShip.captain != null)
                ? playerShip.captain.displayNameKo : "선장";
            if (nameText != null) nameText.text = captainName;
            if (initialText != null)
            {
                initialText.text = string.IsNullOrEmpty(captainName) ? "?" : captainName.Substring(0, 1);
            }
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (statusText == null) return;
            if (playerState == null)
            {
                statusText.text = "";
                return;
            }
            // 잔돈 · 좋은 N · 나쁜 N(0 이면 숨김)
            string money = $"<color=#FFD86B>{playerState.Money:N0} G</color>";
            string good = $"좋은 {playerState.GoodReputation}";
            if (playerState.BadReputation > 0)
            {
                string bad = $"<color=#F47C7C>나쁜 {playerState.BadReputation}</color>";
                statusText.text = $"{money}  ·  {good}  ·  {bad}";
            }
            else
            {
                statusText.text = $"{money}  ·  {good}";
            }
        }

        // ─── Auto Layout ──────────────────────────────────────────────────

        [ContextMenu("Auto Layout")]
        private void AutoLayout()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) return;

            // 좌상단 anchor
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(30f, -30f);
            rt.sizeDelta = new Vector2(460f, 130f);   // 잔돈/명성 들어갈 자리 확보

            // 배경 (반투명 둥근 사각)
            var bg = GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            bg.raycastTarget = true;

            // Button 컴포넌트
            if (button == null)
            {
                button = GetComponent<Button>();
                if (button == null) button = gameObject.AddComponent<Button>();
            }
            button.targetGraphic = bg;

            // 아바타 (원형)
            if (avatarImage == null)
            {
                var avatarGO = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
                avatarGO.transform.SetParent(transform, false);
                avatarImage = avatarGO.GetComponent<Image>();
            }
            var avRT = avatarImage.GetComponent<RectTransform>();
            avRT.anchorMin = new Vector2(0f, 0.5f);
            avRT.anchorMax = new Vector2(0f, 0.5f);
            avRT.pivot = new Vector2(0f, 0.5f);
            avRT.anchoredPosition = new Vector2(15f, 0f);
            avRT.sizeDelta = new Vector2(80f, 80f);
            avatarImage.color = new Color(0.85f, 0.7f, 0.4f, 1f);   // 아이보리
            avatarImage.raycastTarget = false;

            // Initial 텍스트 (아바타 가운데)
            if (initialText == null)
            {
                var initGO = new GameObject("Initial", typeof(RectTransform));
                initGO.transform.SetParent(avatarImage.transform, false);
                initialText = initGO.AddComponent<TextMeshProUGUI>();
            }
            var initRT = initialText.rectTransform;
            initRT.anchorMin = Vector2.zero;
            initRT.anchorMax = Vector2.one;
            initRT.sizeDelta = Vector2.zero;
            initRT.anchoredPosition = Vector2.zero;
            initialText.fontSize = 48f;
            initialText.alignment = TextAlignmentOptions.Center;
            initialText.color = new Color(0.2f, 0.15f, 0.1f);
            initialText.raycastTarget = false;

            // 이름 (위쪽)
            if (nameText == null)
            {
                var nameGO = new GameObject("Name", typeof(RectTransform));
                nameGO.transform.SetParent(transform, false);
                nameText = nameGO.AddComponent<TextMeshProUGUI>();
            }
            var nameRT = nameText.rectTransform;
            nameRT.anchorMin = new Vector2(0f, 0.5f);
            nameRT.anchorMax = new Vector2(1f, 1f);
            nameRT.pivot = new Vector2(0f, 0.5f);
            nameRT.offsetMin = new Vector2(110f, 0f);
            nameRT.offsetMax = new Vector2(-10f, -8f);
            nameText.fontSize = 28f;
            nameText.alignment = TextAlignmentOptions.BottomLeft;
            nameText.color = Color.white;
            nameText.raycastTarget = false;

            // 잔돈 / 명성 (아래쪽)
            if (statusText == null)
            {
                var statusGO = new GameObject("Status", typeof(RectTransform));
                statusGO.transform.SetParent(transform, false);
                statusText = statusGO.AddComponent<TextMeshProUGUI>();
            }
            var statusRT = statusText.rectTransform;
            statusRT.anchorMin = new Vector2(0f, 0f);
            statusRT.anchorMax = new Vector2(1f, 0.5f);
            statusRT.pivot = new Vector2(0f, 0.5f);
            statusRT.offsetMin = new Vector2(110f, 8f);
            statusRT.offsetMax = new Vector2(-10f, 0f);
            statusText.fontSize = 20f;
            statusText.alignment = TextAlignmentOptions.TopLeft;
            statusText.color = new Color(0.9f, 0.9f, 0.9f);
            statusText.raycastTarget = false;
            statusText.richText = true;

            Debug.Log("[PlayerInfoButton] Auto Layout 완료.");
        }
    }
}
