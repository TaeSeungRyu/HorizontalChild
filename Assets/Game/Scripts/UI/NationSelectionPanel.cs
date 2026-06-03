using Game.Data;
using Game.Player;
using Game.Ship;
using Game.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 게임 시작 시 표시되는 국적 선택 패널.
    /// 8개 NationData 를 받아 각 국가의 버튼을 동적 생성.
    /// 사용자가 한 국가 클릭 → 인사말 표시 → "이 나라로 시작" → 게임 진행.
    ///
    /// 동작:
    ///   1. Start() 에서 패널 자동 표시 (또는 외부에서 Show() 호출)
    ///   2. 8개 NationButton 동적 생성
    ///   3. 사용자 선택 시 인사말 + 확인 버튼 표시
    ///   4. 확인 → PlayerShip 위치를 시작 항구로 → GameSession 갱신 → 패널 숨김
    ///
    /// 사용:
    ///   nations 배열에 8개 NationData 인스펙터에서 등록
    ///   nationButtonPrefab 에 NationButton 컴포넌트가 부착된 prefab
    ///   buttonContainer 는 GridLayoutGroup 같은 부모 (자동 정렬)
    /// </summary>
    public class NationSelectionPanel : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Tooltip("시작 시 자동으로 표시할지. 게임 시작 → 국적 선택 흐름이면 true.")]
        public bool autoShowOnStart = true;

        [Header("Catalog — NationCatalog 우선, 비어 있으면 배열 fallback")]
        [Tooltip("NationCatalog SO. Game ▸ Refresh All Catalogs 로 자동 채워짐. 우선 사용.")]
        public NationCatalog nationCatalog;
        [Tooltip("Fallback: 카탈로그 없을 때 직접 등록할 국가 배열.")]
        public NationData[] nations;

        private NationData[] EffectiveNations =>
            (nationCatalog != null && nationCatalog.all != null && nationCatalog.all.Length > 0)
                ? nationCatalog.all : nations;

        [Header("Button Generation")]
        public Transform buttonContainer;
        public GameObject nationButtonPrefab;

        [Header("Selected Display")]
        public TMP_Text selectedNameText;
        public TMP_Text selectedGreetingText;
        public Button confirmButton;

        [Header("Game Refs")]
        public ShipController playerShip;
        public GameSession gameSession;

        private NationData _selected;

        // ─── 라이프사이클 ────────────────────────────────────────────────────

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        private void Start()
        {
            if (gameSession == null) gameSession = GameSession.Instance;

            ClearSelectedDisplay();

            if (autoShowOnStart)
            {
                Show();
            }
            else
            {
                panelRoot.SetActive(false);
            }
        }

        // ─── Show / Hide ────────────────────────────────────────────────────

        public void Show()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(true);
            BuildButtons();
            ClearSelectedDisplay();
        }

        public void Hide()
        {
            panelRoot.SetActive(false);
        }

        // ─── 버튼 동적 생성 ──────────────────────────────────────────────────

        private void BuildButtons()
        {
            var nationsArr = EffectiveNations;
            if (buttonContainer == null || nationButtonPrefab == null || nationsArr == null) return;

            // 기존 자식 모두 제거 (Show 재호출 대비)
            for (int i = buttonContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(buttonContainer.GetChild(i).gameObject);
            }

            foreach (var nation in nationsArr)
            {
                if (nation == null) continue;
                var go = Instantiate(nationButtonPrefab, buttonContainer);
                go.name = $"NationButton_{nation.nationId}";

                var btn = go.GetComponent<NationButton>();
                if (btn == null) btn = go.AddComponent<NationButton>();
                btn.Bind(nation, OnNationClicked);
            }
        }

        // ─── 선택 처리 ──────────────────────────────────────────────────────

        private void OnNationClicked(NationData nation)
        {
            if (nation == null) return;
            _selected = nation;

            if (selectedNameText != null)
            {
                selectedNameText.text = nation.displayNameKo;
            }

            if (selectedGreetingText != null)
            {
                selectedGreetingText.text = nation.greeting;
            }

            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(true);
            }
        }

        private void OnConfirmClicked()
        {
            if (_selected == null) return;

            // 1) GameSession 갱신
            if (gameSession == null) gameSession = GameSession.Instance;
            gameSession?.SetSelectedNation(_selected);

            // 2) PlayerShip 위치를 시작 항구 + 바다 쪽 offset 으로 이동
            if (playerShip != null && _selected.startingPort != null)
            {
                var port = _selected.startingPort;
                var spawnLat = port.latitude + _selected.startingSeaOffsetLatitude;
                var spawnLng = port.longitude + _selected.startingSeaOffsetLongitude;
                var spawnWorld = GeoCoordinate.LatLngToWorld(spawnLat, spawnLng);
                var newPos = new Vector3(
                    spawnWorld.x,
                    playerShip.transform.position.y,
                    spawnWorld.z);
                playerShip.transform.position = newPos;
                playerShip.HardStop();

                Debug.Log($"[NationSelectionPanel] 시작 위치: 항구({port.latitude:F1}, {port.longitude:F1}) + 오프셋({_selected.startingSeaOffsetLatitude:F1}, {_selected.startingSeaOffsetLongitude:F1}) → 월드 ({newPos.x:F0}, {newPos.z:F0})");
            }

            // 3) 선택된 국가의 선장 캐릭터 자동 할당 — 능력치 보너스 적용
            if (playerShip != null && _selected.startingCharacter != null)
            {
                playerShip.captain = _selected.startingCharacter;
                Debug.Log($"[NationSelectionPanel] 선장 할당: {_selected.startingCharacter.displayNameKo} (용기 {_selected.startingCharacter.bravery} / 항해 {_selected.startingCharacter.seamanship} / 눈썰미 {_selected.startingCharacter.keenEye})");
            }

            Hide();
        }

        private void ClearSelectedDisplay()
        {
            _selected = null;
            if (selectedNameText != null) selectedNameText.text = "";
            if (selectedGreetingText != null) selectedGreetingText.text = "어느 나라로 시작할까요?";
            if (confirmButton != null) confirmButton.gameObject.SetActive(false);
        }
    }
}
