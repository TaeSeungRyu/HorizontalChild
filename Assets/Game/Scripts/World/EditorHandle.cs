using Game.Data;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// PortPlacementEditor 가 만드는 핸들 컴포넌트 — 데이터 보관용 마커.
    ///
    /// 클릭/드래그 로직은 PortPlacementEditor.Update() 의 screen-space 픽킹으로
    /// 이전됨 (IPointerDownHandler 는 핸들이 많이 겹칠 때 raycast 가 엉뚱한
    /// 콜라이더를 잡아 신뢰성이 떨어짐). 본 클래스는 핸들 → SO 매핑만 보관.
    /// </summary>
    public class EditorHandle : MonoBehaviour
    {
        public PortData Port { get; private set; }
        public DiscoveryData Discovery { get; private set; }

        public void Init(PortPlacementEditor owner, PortData port, DiscoveryData discovery)
        {
            Port = port;
            Discovery = discovery;
        }
    }
}
