using Game.Data;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 시작 시 LandmassCatalog 의 모든 LandmassData 를 받아 씬에 자동 spawn.
    /// 각 대륙은 단순 Cube + BoxCollider + Landmass 컴포넌트.
    ///
    /// 사용:
    ///   GameManager 또는 별도 빈 GameObject 에 부착.
    ///   landmassCatalog 또는 직접 landmasses 배열에 LandmassData 등록.
    ///
    /// 좌표 변환은 GeoCoordinate.LatLngToWorld (15 unit/도) 사용.
    /// 즉 sizeLatitude/sizeLongitude 1° 당 15 unit 으로 환산.
    /// </summary>
    public class LandmassPlacer : MonoBehaviour
    {
        [Header("Catalog — LandmassCatalog 우선, 비어 있으면 배열 fallback")]
        public LandmassCatalog landmassCatalog;
        public LandmassData[] landmasses;

        [Header("Visual")]
        [Tooltip("Cube primitive 의 머티리얼. 비어 있으면 기본 머티리얼 + LandmassData.color.")]
        public Material defaultMaterial;

        [Tooltip("육지 GameObject 들을 묶을 부모. 비어 있으면 본 GameObject 하위에 생성.")]
        public Transform landmassesParent;

        [Tooltip("☑ 하면 큐브의 MeshRenderer 를 끔. 충돌(BoxCollider)은 유지. 세계지도 텍스처를 사용할 때 큐브가 지도를 가리지 않게.")]
        public bool hideVisuals = false;

        [Tooltip("☑ 하면 큐브의 BoxCollider 도 제거. WorldLand 메쉬가 해안선 충돌을 담당할 때 켬. " +
                 "큐브 충돌은 사각 박스라 정확도 ↓ — 새 메쉬 콜라이더가 더 정확.")]
        public bool disableCollision = false;

        private LandmassData[] EffectiveLandmasses =>
            (landmassCatalog != null && landmassCatalog.all != null && landmassCatalog.all.Length > 0)
                ? landmassCatalog.all : landmasses;

        private void Start()
        {
            if (landmassesParent == null) landmassesParent = transform;
            SpawnLandmasses();
        }

        private void SpawnLandmasses()
        {
            var arr = EffectiveLandmasses;
            if (arr == null) return;

            // 1° 위/경도 = 15 unit (GeoCoordinate.WorldWidthUnits / 360)
            const float UnitsPerDegree = 15f;

            foreach (var data in arr)
            {
                if (data == null) continue;

                // 위·경도 박스 → Unity 월드 박스
                var centerWorld = GeoCoordinate.LatLngToWorld(data.centerLatitude, data.centerLongitude);
                var sizeX = data.sizeLongitude * UnitsPerDegree;
                var sizeZ = data.sizeLatitude * UnitsPerDegree;
                var sizeY = data.height;

                // Cube primitive
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(landmassesParent);
                cube.transform.position = new Vector3(centerWorld.x, sizeY * 0.5f - 0.1f, centerWorld.z);
                cube.transform.localScale = new Vector3(sizeX, sizeY, sizeZ);

                // 색상 (또는 시각 숨김)
                var renderer = cube.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (hideVisuals)
                    {
                        renderer.enabled = false;
                    }
                    else
                    {
                        var mat = defaultMaterial != null
                            ? new Material(defaultMaterial)
                            : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        mat.color = data.color;
                        renderer.material = mat;
                    }
                }

                // BoxCollider (Cube primitive 가 기본 부착) — disableCollision 이면 제거
                if (disableCollision)
                {
                    var col = cube.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                }
                cube.isStatic = true;

                // Landmass 컴포넌트 — ShipController 가 인식
                var landmass = cube.AddComponent<Landmass>();
                landmass.Bind(data);
            }
        }
    }
}
