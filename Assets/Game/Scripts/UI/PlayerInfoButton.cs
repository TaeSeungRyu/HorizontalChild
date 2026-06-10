using Game.Player;
using Game.Ship;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// HUD 좌상단 — 선장 아바타 + 이름. 클릭 시 PlayerInfoPanel 열림.
    ///
    /// 아바타 이미지가 없으면 선장 이름의 첫 글자를 원 위에 표시 (placeholder).
    /// 추후 captainAvatar(Sprite) 할당하면 그 이미지 사용.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PlayerInfoButton : MonoBehaviour
    {
        [Header("Refs")]
        public Image avatarImage;           // 원형 배경 또는 실제 아바타
        public TMP_Text initialText;        // placeholder — 이름 첫 글자
        public TMP_Text nameText;
        public Button button;

        [Header("Targets")]
        public ShipController playerShip;
        public PlayerInfoPanel infoPanel;

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
            Refresh();
        }

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
            rt.sizeDelta = new Vector2(360f, 110f);

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

            // 이름
            if (nameText == null)
            {
                var nameGO = new GameObject("Name", typeof(RectTransform));
                nameGO.transform.SetParent(transform, false);
                nameText = nameGO.AddComponent<TextMeshProUGUI>();
            }
            var nameRT = nameText.rectTransform;
            nameRT.anchorMin = new Vector2(0f, 0f);
            nameRT.anchorMax = new Vector2(1f, 1f);
            nameRT.pivot = new Vector2(0f, 0.5f);
            nameRT.offsetMin = new Vector2(110f, 0f);
            nameRT.offsetMax = new Vector2(-10f, 0f);
            nameText.fontSize = 28f;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.color = Color.white;
            nameText.raycastTarget = false;

            Debug.Log("[PlayerInfoButton] Auto Layout 완료.");
        }
    }
}
