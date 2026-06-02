using Game.Data;
using Game.World;
using UnityEngine;

namespace Game.Ports
{
    /// <summary>
    /// 항구 아이콘 GameObject 에 붙이는 컴포넌트.
    /// SeaWorldManager 가 spawn 시 Bind(PortData) 로 데이터를 연결.
    ///
    /// 클릭으로 입항하는 흐름은 UI 의 EnterPortButton 이 담당.
    /// 본 컴포넌트는 단순히 항구 데이터를 GameObject 에 묶어 두는 역할.
    /// </summary>
    public class PortMarker : MonoBehaviour
    {
        public PortData Port { get; private set; }

        [Tooltip("탭 시 항구 도착 알림을 보낼 SeaWorldManager. 비어 있으면 자동 검색.")]
        public SeaWorldManager worldManager;

        public void Bind(PortData port)
        {
            Port = port;
        }

        private void Reset()
        {
            // 인스펙터에서 컴포넌트 처음 추가될 때 자동 연결 시도.
            if (worldManager == null)
            {
                worldManager = FindAnyObjectByType<SeaWorldManager>();
            }
        }

        /// <summary>외부에서 호출 — 직접 입항을 트리거할 때 사용. M1 에선 EnterPortButton 이 SeaWorldManager 를 직접 호출하므로 본 메서드는 호출되지 않음.</summary>
        public void HandleTap()
        {
            if (Port == null) return;
            if (worldManager == null)
            {
                worldManager = FindAnyObjectByType<SeaWorldManager>();
            }
            worldManager?.TryEnterPortFromClick(Port);
        }
    }
}
