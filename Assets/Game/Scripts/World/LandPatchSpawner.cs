using Game.Data;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 보충 land 패치 — WorldLand 메쉬에 빠진 영역을 큐브로 채워줌.
    ///
    /// 사용 사례:
    ///   1:50m GeoJSON 단순화로 누락된 지역 (강원도 등) 를 시각·충돌 모두 보충.
    ///   각 패치는 lat/lng 위치에 cube 를 spawn 하고 WorldLand 와 같은 색·높이로 정렬.
    ///   Landmass 컴포넌트 부착 → ShipController.IsLandAt 가 자동 인식.
    ///
    /// 사용:
    ///   1) Hierarchy 빈 GameObject 생성 (예: "LandPatches")
    ///   2) Add Component → Land Patch Spawner
    ///   3) Patches 배열에 추가 영역 등록 (기본값 강원도 1개)
    ///   4) Play 시 자동 spawn
    ///
    /// 좌표 미세 조정은 Scene View 에서 Gizmo (반투명 갈색 큐브) 로 확인.
    /// </summary>
    public class LandPatchSpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct Patch
        {
            public string name;
            [Range(-90f, 90f)] public float latitude;
            [Range(-180f, 180f)] public float longitude;
            [Tooltip("위도 폭 (도 단위, 1° ≈ 111 km)")]
            public float sizeLatitudeDeg;
            [Tooltip("경도 폭 (도 단위, 1° ≈ 88 km @ 위도 35°)")]
            public float sizeLongitudeDeg;
        }

        [Header("Patches")]
        [Tooltip("WorldLand 메쉬에 빠진 곳을 보충할 land 패치들. " +
                 "수정·추가 시 Play 다시 시작해야 반영.")]
        public Patch[] patches =
        {
            // 강원도 — 1:50m 에서 동쪽 해안선이 안쪽으로 깎임
            new Patch
            {
                name = "Gangwon",
                latitude = 37.85f,
                longitude = 128.5f,
                sizeLatitudeDeg = 1.6f,
                sizeLongitudeDeg = 1.0f,
            },
        };

        [Header("Visual — WorldLand 와 시각 정렬")]
        [Tooltip("패치 바닥 Y 좌표. WorldLand 의 BaseY 와 같게.")]
        public float baseY = 1.25f;

        [Tooltip("패치 두께 (Y 방향). WorldLand 의 ExtrudeHeight 와 같게.")]
        public float extrudeHeight = 0.5f;

        [Tooltip("패치 색. WorldLand 의 LandColor 와 같게.")]
        public Color patchColor = new Color(0.62f, 0.54f, 0.38f);

        [Tooltip("커스텀 머티리얼. 비어 있으면 URP Lit 자동 생성.")]
        public Material customMaterial;

        // 1° = 15 unit (GeoCoordinate.WorldWidthUnits / 360)
        private const float UnitsPerDegree = 15f;

        private void Start()
        {
            if (patches == null) return;
            foreach (var p in patches)
            {
                SpawnPatch(p);
            }
        }

        private void SpawnPatch(Patch patch)
        {
            var worldPos = GeoCoordinate.LatLngToWorld(patch.latitude, patch.longitude);
            float sizeX = patch.sizeLongitudeDeg * UnitsPerDegree;
            float sizeZ = patch.sizeLatitudeDeg * UnitsPerDegree;
            float sizeY = Mathf.Max(extrudeHeight, 0.01f);

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(transform);
            cube.name = $"LandPatch_{patch.name}";
            cube.transform.position = new Vector3(
                worldPos.x,
                baseY + sizeY * 0.5f,
                worldPos.z);
            cube.transform.localScale = new Vector3(sizeX, sizeY, sizeZ);

            // 색상
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = customMaterial != null
                    ? new Material(customMaterial)
                    : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", patchColor);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", patchColor);
                renderer.material = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            // Landmass 컴포넌트 — ShipController.IsLandAt 가 자동 인식
            cube.AddComponent<Landmass>();
            cube.isStatic = true;
        }

        // ─── Gizmos — Scene View 에서 패치 위치 미리보기 ─────────────────────

        private void OnDrawGizmos()
        {
            if (patches == null) return;
            Gizmos.color = new Color(patchColor.r, patchColor.g, patchColor.b, 0.6f);
            foreach (var p in patches)
            {
                var worldPos = GeoCoordinate.LatLngToWorld(p.latitude, p.longitude);
                float sizeX = p.sizeLongitudeDeg * UnitsPerDegree;
                float sizeZ = p.sizeLatitudeDeg * UnitsPerDegree;
                Gizmos.DrawCube(
                    new Vector3(worldPos.x, baseY + extrudeHeight * 0.5f, worldPos.z),
                    new Vector3(sizeX, extrudeHeight, sizeZ));
            }
        }
    }
}
