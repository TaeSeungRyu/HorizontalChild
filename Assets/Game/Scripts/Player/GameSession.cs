using Game.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Player
{
    /// <summary>
    /// 현재 게임 세션 상태 — 어느 국적으로 시작했는지 등.
    /// PlayerState 와 별개 (이건 메카닉 상태가 아닌 메타 상태).
    ///
    /// 싱글톤. GameManager 같은 영구 GameObject 에 부착.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [Header("Runtime")]
        public NationData SelectedNation;

        [Header("Events")]
        public UnityEvent<NationData> onNationSelected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[GameSession] 인스턴스가 둘 이상. {gameObject.name} 무시.");
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetSelectedNation(NationData nation)
        {
            if (nation == null) return;
            SelectedNation = nation;
            onNationSelected?.Invoke(nation);
            Debug.Log($"[GameSession] 국적 선택: {nation.nationId} ({nation.displayNameKo})");
        }
    }
}
