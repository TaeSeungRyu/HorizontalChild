using Game.Data;
using Game.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.World
{
    /// <summary>
    /// 발견 후 영구 표시되는 3D 마커 (보물상자 같은 모델).
    /// 탭하면 DiscoveryFoundPanel 을 다시 띄움 (보상 없이 정보 열람 전용).
    ///
    /// 클릭 처리는 IPointerClickHandler — Main Camera 에 Physics Raycaster 필요.
    /// (없으면 DiscoveryMarkerSpawner 가 콘솔에 경고 출력.)
    /// </summary>
    public class DiscoveryMarker : MonoBehaviour, IPointerClickHandler
    {
        public DiscoveryData Data { get; private set; }
        private DiscoveryFoundPanel _reopenPanel;

        public void Bind(DiscoveryData discovery, DiscoveryFoundPanel panel)
        {
            Data = discovery;
            _reopenPanel = panel;
            if (discovery != null) name = $"DiscoveryMarker_{discovery.discoveryId}";
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[DiscoveryMarker] Click on {(Data != null ? Data.discoveryId : "null")}");
            if (_reopenPanel == null)
            {
                Debug.LogWarning("[DiscoveryMarker] reopenPanel 이 null — Spawner 의 Reopen Panel 필드 확인하세요.");
                return;
            }
            if (Data == null) return;
            _reopenPanel.Show(Data);
        }

        public static Color ColorFor(DiscoveryCategory category) => category switch
        {
            DiscoveryCategory.Landmark   => new Color(0.30f, 0.60f, 0.95f), // 파랑
            DiscoveryCategory.FloraFauna => new Color(0.40f, 0.80f, 0.40f), // 녹색
            DiscoveryCategory.Ruin       => new Color(0.70f, 0.50f, 0.30f), // 갈색
            DiscoveryCategory.Event      => new Color(0.95f, 0.70f, 0.25f), // 노란-주황
            _ => Color.white,
        };
    }
}
