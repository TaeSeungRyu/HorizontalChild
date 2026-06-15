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
        private static readonly List<Vector2[]> _polysWorld = new();

        public static int Count => _polysWorld.Count;

        public static void Clear() => _polysWorld.Clear();

        public static void AddPolygon(Vector2[] polyWorldXZ)
        {
            if (polyWorldXZ == null || polyWorldXZ.Length < 3) return;
            _polysWorld.Add(polyWorldXZ);
        }

        /// <summary>월드 좌표 worldPos 가 등록된 강 영역 안이면 true.</summary>
        public static bool IsInRiver(Vector3 worldPos)
        {
            if (_polysWorld.Count == 0) return false;
            var p = new Vector2(worldPos.x, worldPos.z);
            for (int i = 0; i < _polysWorld.Count; i++)
            {
                if (MapSubtractGeometry.PointInPolygon(p, _polysWorld[i])) return true;
            }
            return false;
        }

        /// <summary>씬 재로드 시 누수 방지 — 정적 상태 정리.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnRuntimeInit()
        {
            _polysWorld.Clear();
        }
    }
}
