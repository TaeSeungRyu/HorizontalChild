using Game.Save;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 일시정지 메뉴 — Resume / 새 게임 / 종료.
    ///
    /// 동작:
    ///   - Resume: 패널 닫고 Time.timeScale = 1
    ///   - 새 게임: SaveService.DeleteSave + 현재 씬 다시 로드 (모든 진행 초기화)
    ///   - 종료: Application.Quit (Editor 에선 stop play mode)
    ///
    /// 열림 시 Time.timeScale = 0 으로 게임 일시정지.
    /// 다른 패널 가려서 들리도록 SetAsLastSibling.
    /// </summary>
    public class PauseMenuPanel : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Header("Refs — Auto Layout 가능")]
        public TMP_Text titleText;
        public Button resumeButton;
        public Button newGameButton;
        public Button quitButton;

        [Header("Behavior")]
        [Tooltip("패널 열릴 때 게임 시간 정지 (Time.timeScale = 0). 항해도 멈춤.")]
        public bool pauseGameTime = true;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(Resume);
                resumeButton.onClick.AddListener(Resume);
            }
            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(StartNewGame);
                newGameButton.onClick.AddListener(StartNewGame);
            }
            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitApp);
                quitButton.onClick.AddListener(QuitApp);
            }
        }

        // ─── Show / Resume ──────────────────────────────────────────────────

        public void Show()
        {
            if (panelRoot == null) panelRoot = gameObject;
            if (titleText != null) titleText.text = "일시 정지";

            if (pauseGameTime) Time.timeScale = 0f;

            panelRoot.transform.SetAsLastSibling();
            panelRoot.SetActive(true);
        }

        public void Resume()
        {
            if (pauseGameTime) Time.timeScale = 1f;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void Toggle()
        {
            if (panelRoot == null) panelRoot = gameObject;
            if (panelRoot.activeInHierarchy) Resume();
            else Show();
        }

        // ─── 새 게임 / 종료 ────────────────────────────────────────────────

        public void StartNewGame()
        {
            // 저장 데이터 삭제
            var save = SaveService.Instance;
            if (save != null) save.DeleteSave();

            // Time scale 복구 후 씬 재로드 (모든 런타임 상태 초기화)
            Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }

        public void QuitApp()
        {
            Debug.Log("[PauseMenuPanel] 종료");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ─── 자동 레이아웃 ──────────────────────────────────────────────────

        [ContextMenu("Auto Layout")]
        private void AutoLayout()
        {
            if (panelRoot == null) panelRoot = gameObject;
            var rt = panelRoot.GetComponent<RectTransform>();
            if (rt == null) return;

            // 풀스크린 어두운 오버레이
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = panelRoot.GetComponent<Image>();
            if (bg == null) bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f); // 반투명 오버레이
            bg.raycastTarget = true;

            // 타이틀 — 상단 중앙
            if (titleText != null)
            {
                SetRect(titleText.rectTransform,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -200f), new Vector2(800f, 100f));
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.fontSize = 72f;
                titleText.color = Color.white;
            }

            // 버튼 3개 — 세로로 중앙
            float btnW = 400f, btnH = 100f, gap = 30f;
            float startY = 100f; // 중앙 기준 위에서부터
            if (resumeButton != null)
                SetRect(resumeButton.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, startY), new Vector2(btnW, btnH));
            if (newGameButton != null)
                SetRect(newGameButton.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, startY - (btnH + gap)), new Vector2(btnW, btnH));
            if (quitButton != null)
                SetRect(quitButton.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, startY - 2f * (btnH + gap)), new Vector2(btnW, btnH));

            Debug.Log("[PauseMenuPanel] Auto Layout 적용 완료.");
        }

        private static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax,
            Vector2 pivot, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }
    }
}
