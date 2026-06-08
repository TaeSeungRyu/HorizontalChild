using System.Collections;
using System.Collections.Generic;
using Game.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 토스트 알림 — 화면 상단/중앙에 짧게 떴다 사라지는 메시지.
    ///
    /// 사용:
    ///   ToastService.Instance.Show("새 지역: 동지중해");
    ///
    /// 자동 hookup:
    ///   - MissionService.onRegionUnlocked → "새 지역 해제: {name}"
    ///   - MissionService.onMissionCompleted → "의뢰 완료: {title}"
    ///
    /// 시각 셋업:
    ///   ToastService GameObject 에 컴포넌트 부착 + Auto Layout 으로 UI 자동 생성.
    /// </summary>
    public class ToastService : MonoBehaviour
    {
        public static ToastService Instance { get; private set; }

        [Header("Refs — Auto Layout 가능")]
        public RectTransform toastRoot;
        public Image background;
        public TMP_Text messageText;

        [Header("Behavior")]
        [Tooltip("토스트가 화면에 보이는 시간 (초).")]
        [Range(0.5f, 5f)] public float visibleSeconds = 2.5f;

        [Tooltip("페이드 인 시간 (초).")]
        [Range(0f, 1f)] public float fadeInSeconds = 0.2f;

        [Tooltip("페이드 아웃 시간 (초).")]
        [Range(0f, 1f)] public float fadeOutSeconds = 0.4f;

        private readonly Queue<string> _queue = new();
        private bool _running;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            // 시작 시 안 보이게
            if (toastRoot != null) toastRoot.gameObject.SetActive(false);
        }

        private void Start()
        {
            // MissionService 이벤트 자동 hookup
            var ms = MissionService.Instance;
            if (ms != null)
            {
                ms.onRegionUnlocked.AddListener(region =>
                {
                    if (region != null) Show($"새 지역 해제: {region.displayNameKo}");
                });
                ms.onMissionCompleted.AddListener(mission =>
                {
                    if (mission != null) Show($"의뢰 완료: {mission.title}");
                });
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(string message)
        {
            if (string.IsNullOrEmpty(message) || toastRoot == null || messageText == null) return;
            _queue.Enqueue(message);
            if (!_running) StartCoroutine(RunQueue());
        }

        private IEnumerator RunQueue()
        {
            _running = true;
            while (_queue.Count > 0)
            {
                var msg = _queue.Dequeue();
                yield return ShowOne(msg);
            }
            _running = false;
        }

        private IEnumerator ShowOne(string msg)
        {
            messageText.text = msg;
            toastRoot.gameObject.SetActive(true);

            // 페이드 인
            yield return Fade(0f, 1f, fadeInSeconds);
            // 유지
            yield return new WaitForSecondsRealtime(visibleSeconds);
            // 페이드 아웃
            yield return Fade(1f, 0f, fadeOutSeconds);

            toastRoot.gameObject.SetActive(false);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                SetAlpha(to);
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime; // 일시정지(timeScale=0) 중에도 작동
                SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            SetAlpha(to);
        }

        private void SetAlpha(float a)
        {
            if (background != null)
            {
                var c = background.color;
                background.color = new Color(c.r, c.g, c.b, a * 0.85f); // 배경 약간 투명
            }
            if (messageText != null)
            {
                var c = messageText.color;
                messageText.color = new Color(c.r, c.g, c.b, a);
            }
        }

        // ─── 자동 레이아웃 ──────────────────────────────────────────────────

        [ContextMenu("Auto Layout")]
        private void AutoLayout()
        {
            // Canvas 아래에 부착되어 있어야 함
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            // 화면 전체 anchor — 자식 ToastRoot 가 상단 중앙에 표시
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // ToastRoot — 상단 중앙 박스
            if (toastRoot == null)
            {
                var rootGO = new GameObject("ToastRoot", typeof(RectTransform));
                rootGO.transform.SetParent(transform, false);
                toastRoot = rootGO.GetComponent<RectTransform>();
            }
            toastRoot.anchorMin = new Vector2(0.5f, 1f);
            toastRoot.anchorMax = new Vector2(0.5f, 1f);
            toastRoot.pivot = new Vector2(0.5f, 1f);
            toastRoot.sizeDelta = new Vector2(800f, 100f);
            toastRoot.anchoredPosition = new Vector2(0f, -80f);

            // Background Image
            if (background == null)
            {
                background = toastRoot.gameObject.GetComponent<Image>();
                if (background == null) background = toastRoot.gameObject.AddComponent<Image>();
            }
            background.color = new Color(0.08f, 0.10f, 0.14f, 0.85f);
            background.raycastTarget = false; // 클릭 통과

            // Message Text 자식
            if (messageText == null)
            {
                var textGO = new GameObject("MessageText", typeof(RectTransform));
                textGO.transform.SetParent(toastRoot, false);
                messageText = textGO.AddComponent<TextMeshProUGUI>();
            }
            var textRT = messageText.rectTransform;
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.pivot = new Vector2(0.5f, 0.5f);
            textRT.sizeDelta = Vector2.zero;
            textRT.offsetMin = new Vector2(20f, 10f);
            textRT.offsetMax = new Vector2(-20f, -10f);
            messageText.text = "토스트 미리보기";
            messageText.fontSize = 28f;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.color = Color.white;
            messageText.raycastTarget = false;

            toastRoot.gameObject.SetActive(false);

            Debug.Log("[ToastService] Auto Layout 적용 완료.");
        }

        [ContextMenu("Test Toast")]
        private void TestToast()
        {
            Show("테스트 토스트 — 잘 보이나요?");
        }
    }
}
