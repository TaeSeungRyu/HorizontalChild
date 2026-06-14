using Game.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.World
{
    /// <summary>
    /// PortPlacementEditor 가 만드는 핸들 컴포넌트.
    /// 클릭 → drag → 위치 이동, 놓으면 PortData / DiscoveryData 에 저장.
    /// </summary>
    public class EditorHandle : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private PortPlacementEditor _owner;
        private PortData _port;
        private DiscoveryData _discovery;
        private Camera _cam;
        private Plane _dragPlane;
        private bool _dragging;

        public void Init(PortPlacementEditor owner, PortData port, DiscoveryData discovery)
        {
            _owner = owner;
            _port = port;
            _discovery = discovery;
            _cam = Camera.main;
        }

        public void OnPointerDown(PointerEventData ev)
        {
            string label = _port != null ? _port.displayNameKo
                         : _discovery != null ? _discovery.displayNameKo : "?";
            Debug.Log($"[EditorHandle] 클릭됨 — {label} 선택. 이제 드래그하면 이동.");
            _dragPlane = new Plane(Vector3.up, transform.position);
            _dragging = true;
            if (_owner != null) _owner.NotifyHandleSelected(this);
        }

        public void OnDrag(PointerEventData ev)
        {
            if (!_dragging || _cam == null) return;
            var ray = _cam.ScreenPointToRay(ev.position);
            if (_dragPlane.Raycast(ray, out float dist))
            {
                var hit = ray.GetPoint(dist);
                transform.position = new Vector3(hit.x, transform.position.y, hit.z);
            }
        }

        public void OnPointerUp(PointerEventData ev)
        {
            if (!_dragging) return;
            _dragging = false;
            if (_owner != null)
            {
                if (_port != null) _owner.SavePortPosition(_port, transform.position);
                else if (_discovery != null) _owner.SaveDiscoveryPosition(_discovery, transform.position);
            }
        }
    }
}
