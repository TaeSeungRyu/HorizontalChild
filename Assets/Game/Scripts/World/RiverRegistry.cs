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
        // polygons 는 mesh-local 좌표. IsInRiver 에서 ship 의 world → mesh-local 로 변환 후 비교.
        private static readonly List<Vector2[]> _polysMeshLocal = new();
        private static Transform _landT;

        public static int Count => _polysMeshLocal.Count;

        public static void Clear()
        {
            _polysMeshLocal.Clear();
            _landT = null;
        }

        /// <summary>RiverOverlay 가 Refresh 시 호출 — WorldLand 의 Transform 기억.</summary>
        public static void SetLandTransform(Transform t) => _landT = t;

        public static void AddPolygon(Vector2[] polyMeshLocalXZ)
        {
            if (polyMeshLocalXZ == null || polyMeshLocalXZ.Length < 3) return;
            _polysMeshLocal.Add(polyMeshLocalXZ);
        }

        /// <summary>월드 좌표 worldPos 가 등록된 강 영역 안이면 true.</summary>
        public static bool IsInRiver(Vector3 worldPos)
        {
            if (_polysMeshLocal.Count == 0) return false;
            Vector3 local = _landT != null ? _landT.InverseTransformPoint(worldPos) : worldPos;
            var p = new Vector2(local.x, local.z);
            for (int i = 0; i < _polysMeshLocal.Count; i++)
            {
                if (MapSubtractGeometry.PointInPolygon(p, _polysMeshLocal[i])) return true;
            }
            return false;
        }

        /// <summary>씬 재로드 시 누수 방지 — 정적 상태 정리.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnRuntimeInit()
        {
            _polysMeshLocal.Clear();
            _landT = null;
        }
    }
}
