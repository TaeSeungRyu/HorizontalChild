using Game.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 첫 진입 튜토리얼 오버레이 — 국가 선택 후 한 번만 표시.
    ///
    /// 동작:
    ///   - GameSession 의 SelectedNation 이 set 되면 Show 가능
    ///   - PlayerPrefs 의 PrefKey 로 본 적 있는지 추적
    ///   - 4단계 메시지 — "다음" 으로 진행, "넘기기" 로 즉시 종료
    ///   - 마지막 단계 → "시작!" 버튼
    ///
    /// 다시 보기: 컨텍스트 메뉴 또는 PlayerPrefs.DeleteKey 호출.
    /// </summary>
    public class TutorialOverlay : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Header("Refs — Auto Layout 가능")]
        public TMP_Text titleText;
        public TMP_Text bodyText;
        public TMP_Text progressText; // "1 / 4"
        public Button nextButton;
        public Button skipButton;

        [Header("Behavior")]
        [Tooltip("한 번 본 후 자동 skip. 끄면 매 실행마다 표시 (디버그용).")]
        public bool showOnlyOnce = true;

        [Tooltip("국가 선택 직후 자동 표시.")]
        public bool autoShowAfterNationSelected = true;

        private const string PrefKey = "tutorial_shown_v1";

        // 메시지 시퀀스 — 어린이 톤 짧고 명료
        private readonly (string title, string body)[] _steps =
        {
            ("환영합니다!", "함께 큰 바다로 나가요. 먼저 배 조종을 배워볼게요."),
            ("방향 — 조이스틱", "왼쪽 아래 조이스틱을 손가락으로 밀어서 배의 방향을 정해요."),
            ("속도 — 위 / 아래 버튼", "오른쪽 위 화살표는 가속, 아래 화살표는 감속이에요. 너무 빠르면 닻을 내려 멈춰요."),
            ("항구·발견물", "항구 가까이 가서 '항구 들어가기' 를 누르거나, 보물 좌표 근처에서 ⚓ 닻을 내려 탐색해 보세요."),
        };

        private int _stepIndex;
        private bool _checking;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(OnNext);
                nextButton.onClick.AddListener(OnNext);
            }
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(OnSkip);
                skipButton.onClick.AddListener(OnSkip);
            }
        }

        private void Update()
        {
            if (_checking || !autoShowAfterNationSelected) return;
            if (showOnlyOnce && PlayerPrefs.GetInt(PrefKey, 0) != 0) return;

            var session = GameSession.Instance;
            if (session != null && session.SelectedNation != null)
            {
                _checking = true;
                ShowFromStart();
            }
        }

        // ─── 표시 / 진행 / 종료 ──────────────────────────────────────────

        public void ShowFromStart()
        {
            _stepIndex = 0;
            ApplyStep();
            if (panelRoot != null)
            {
                panelRoot.transform.SetAsLastSibling();
                panelRoot.SetActive(true);
            }
        }

        private void ApplyStep()
        {
            if (_stepIndex < 0 || _stepIndex >= _steps.Length) return;
            var (title, body) = _steps[_stepIndex];
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;
            if (progressText != null) progressText.text = $"{_stepIndex + 1} / {_steps.Length}";

            bool isLast = _stepIndex == _steps.Length - 1;
            if (nextButton != null)
            {
                var label = nextButton.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = isLast ? "시작!" : "다음";
            }
        }

        private void OnNext()
        {
            if (_stepIndex < _steps.Length - 1)
            {
                _stepIndex++;
                ApplyStep();
            }
            else
            {
                Finish();
            }
        }

        private void OnSkip() => Finish();

        private void Finish()
        {
            if (showOnlyOnce)
            {
                PlayerPrefs.SetInt(PrefKey, 1);
                PlayerPrefs.Save();
            }
            if (panelRoot != null) panelRoot.SetActive(false);
            _checking = true; // 다시 자동 표시 안 함
        }

        // ─── 디버그 ──────────────────────────────────────────────────────

        [ContextMenu("Reset Tutorial Flag")]
        private void ResetTutorialFlag()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.Save();
            _checking = false;
            Debug.Log("[TutorialOverlay] 튜토리얼 플래그 초기화 — 다음 Play 에 다시 표시됨.");
        }

        [ContextMenu("Show Tutorial Now")]
        private void ShowNow()
        {
            _checking = true;
            ShowFromStart();
        }

        // ─── 자동 레이아웃 ──────────────────────────────────────────────────

        [ContextMenu("Auto Layout")]
        private void AutoLayout()
        {
            if (panelRoot == null) panelRoot = gameObject;
            var rt = panelRoot.GetComponent<RectTransform>();
            if (rt == null) return;

            // 풀스크린 어두운 반투명
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = panelRoot.GetComponent<Image>();
            if (bg == null) bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);
            bg.raycastTarget = true;

            // 메시지 박스 — 중앙
            if (titleText != null)
                SetRect(titleText.rectTransform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 200f), new Vector2(1200f, 80f),
                    fontSize: 56f, align: TextAlignmentOptions.Center);

            if (bodyText != null)
                SetRect(bodyText.rectTransform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 50f), new Vector2(1200f, 180f),
                    fontSize: 32f, align: TextAlignmentOptions.Center);

            if (progressText != null)
                SetRect(progressText.rectTransform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -80f), new Vector2(400f, 40f),
                    fontSize: 22f, align: TextAlignmentOptions.Center, color: new Color(0.8f, 0.8f, 0.8f));

            if (nextButton != null)
                SetButtonRect(nextButton.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0.5f), new Vector2(150f, -180f), new Vector2(260f, 90f));

            if (skipButton != null)
                SetButtonRect(skipButton.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0.5f), new Vector2(-150f, -180f), new Vector2(260f, 90f));

            Debug.Log("[TutorialOverlay] Auto Layout 적용 완료.");
        }

        private static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax,
            Vector2 pivot, Vector2 pos, Vector2 size,
            float fontSize = 28f, TextAlignmentOptions align = TextAlignmentOptions.Center, Color? color = null)
        {
            if (rt == null) return;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var tmp = rt.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.fontSize = fontSize;
                tmp.alignment = align;
                tmp.color = color ?? Color.white;
                tmp.enableWordWrapping = true;
            }
        }

        private static void SetButtonRect(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }
    }
}
