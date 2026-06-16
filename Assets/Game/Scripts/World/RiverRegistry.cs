using System.Collections.Generic;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 강(River) 영역 충돌 우회 레지스트리.
    /// 배가 강 안에 있으면 ShipController.IsLandAt 가 false 를 반환 → 통과 가능.
    ///
    /// RiverOverlay 가 시작 시 Refresh(catalog) 를 호출해 채움.
    /// 카브 메쉬에 영향 X — 시각도 RiverOverlay 가 별도로 그림.
    /// </summary>
    public static class RiverRegistry
    {
        // 저장된 강 — RiverOverlay 가 catalog 에서 빌드
        private static readonly List<Vector2[]> _committed = new();
        // Temp 강 — MapSubtractEditor 가 그리는 중 추가, Save/Cancel/Disable 시 정리
        private static readonly List<Vector2[]> _pending = new();
        private static Transform _landT;

        public static int Count => _committed.Count + _pending.Count;

        // ── 저장된 (committed) ─────────────────────────────────────────────
        public static void Clear() => _committed.Clear();

        public static void AddPolygon(Vector2[] polyMeshLocalXZ)
        {
            if (polyMeshLocalXZ == null || polyMeshLocalXZ.Length < 3) return;
            _committed.Add(polyMeshLocalXZ);
        }

        // ── Temp (pending) ────────────────────────────────────────────────
        public static void ClearPending() => _pending.Clear();

        public static void AddPendingPolygon(Vector2[] polyMeshLocalXZ)
        {
            if (polyMeshLocalXZ == null || polyMeshLocalXZ.Length < 3) return;
            _pending.Add(polyMeshLocalXZ);
        }

        public static void SetLandTransform(Transform t) => _landT = t;

        /// <summary>월드 좌표 worldPos 가 강(저장 또는 temp) 안이면 true.</summary>
        public static bool IsInRiver(Vector3 worldPos)
        {
            if (_committed.Count == 0 && _pending.Count == 0) return false;
            Vector3 local = _landT != null ? _landT.InverseTransformPoint(worldPos) : worldPos;
            var p = new Vector2(local.x, local.z);
            for (int i = 0; i < _committed.Count; i++)
                if (MapSubtractGeometry.PointInPolygon(p, _committed[i])) return true;
            for (int i = 0; i < _pending.Count; i++)
                if (MapSubtractGeometry.PointInPolygon(p, _pending[i])) return true;
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnRuntimeInit()
        {
            _committed.Clear();
            _pending.Clear();
            _landT = null;
        }
    }
}
