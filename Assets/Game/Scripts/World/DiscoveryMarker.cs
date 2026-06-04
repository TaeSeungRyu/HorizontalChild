using Game.Data;
using Game.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.World
{
    /// <summary>
    /// 발견 후 영구 표시되는 마커. World-Space Canvas + Button 으로 구성.
    /// 탭하면 DiscoveryFoundPanel 을 다시 띄움 (보상 없이 정보 열람 전용).
    ///
    /// DiscoveryMarkerSpawner 가 런타임에 자동 spawn → Bind() 호출.
    /// 사용자가 prefab 을 만들 필요 없음 — Spawner 가 GameObject 통째로 생성.
    /// </summary>
    public class DiscoveryMarker : MonoBehaviour
    {
        public DiscoveryData Data { get; private set; }
        private DiscoveryFoundPanel _reopenPanel;

        public void Bind(DiscoveryData discovery, Button button, Image icon, DiscoveryFoundPanel panel)
        {
            Data = discovery;
            _reopenPanel = panel;

            if (button != null) button.onClick.AddListener(OnClicked);
            if (icon != null && discovery != null) icon.color = ColorFor(discovery.category);
            if (discovery != null) name = $"DiscoveryMarker_{discovery.discoveryId}";
        }

        private void OnClicked()
        {
            if (_reopenPanel != null && Data != null)
            {
                _reopenPanel.Show(Data);
            }
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
