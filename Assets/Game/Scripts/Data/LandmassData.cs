using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 대륙(육지) 한 덩어리 정의 — M1 단순 큐브 표현용.
    ///
    /// 위/경도 박스로 영역을 표현. LandmassPlacer 가 GeoCoordinate 로 월드 좌표 변환 후
    /// 큐브 spawn + Collider 부착.
    ///
    /// M3 폴리시 단계에서 Natural Earth 데이터·실 메쉬로 교체 예정.
    /// </summary>
    [CreateAssetMenu(fileName = "Landmass_", menuName = "Game/Data/Landmass Data")]
    public class LandmassData : ScriptableObject
    {
        [Header("Identity")]
        public string landmassId;
        public string displayNameKo;

        [Header("Bounds (위·경도)")]
        [Tooltip("중심 위도 (°). -90 ~ 90")]
        [Range(-90f, 90f)] public float centerLatitude;
        [Tooltip("중심 경도 (°). -180 ~ 180")]
        [Range(-180f, 180f)] public float centerLongitude;
        [Tooltip("위도 폭 (°). 큰 대륙은 30~50")]
        [Range(1f, 90f)] public float sizeLatitude = 10f;
        [Tooltip("경도 폭 (°). 큰 대륙은 30~60")]
        [Range(1f, 180f)] public float sizeLongitude = 10f;

        [Header("Visual")]
        public Color color = new Color(0.65f, 0.55f, 0.40f); // 갈색 모래색
        [Tooltip("육지 높이 (Unity Unit). 바다 위로 약간 돌출.")]
        [Range(0.1f, 5f)] public float height = 1.5f;

        [Header("Source")]
        public string sourceUrl;
    }
}
