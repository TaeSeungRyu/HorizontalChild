using Game.Data;
using Game.World;
using UnityEngine;

namespace Game.Ports
{
    /// <summary>
    /// 항구 아이콘 GameObject 에 붙이는 컴포넌트.
    /// SeaWorldManager 가 spawn 시 Bind(PortData) 로 데이터를 연결.
    ///
    /// 사용자가 아이콘을 탭하면 (별도 입력 시스템) 본 컴포넌트의 OnTapped 호출 →
    /// 자동 항해 또는 도착 다이얼로그 흐름은 후속 작업.
    ///
    /// M1 에서는 단순히 OnMouseDown (에디터 테스트용) 으로도 동작하도록 함.
    /// 모바일 실기에서는 별도 터치 라이트 시스템 또는 UI 버튼으로 교체.
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

        // M1 임시 — 에디터에서 마우스 클릭 시 호출. 실기에서는 별도 터치 처리.
        private void OnMouseDown()
        {
            HandleTap();
        }

        /// <summary>외부에서 호출 — 터치/탭 시 항구 도착 알림.</summary>
        public void HandleTap()
        {
            if (Port == null) return;
            if (worldManager == null)
            {
                worldManager = FindAnyObjectByType<SeaWorldManager>();
            }
            worldManager?.NotifyPortArrival(Port);
        }
    }
}
