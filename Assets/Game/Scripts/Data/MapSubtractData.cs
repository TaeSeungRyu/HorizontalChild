using UnityEngine;

namespace Game.Data
{
    public enum MapEditKind
    {
        /// <summary>이 영역의 육지를 메쉬에서 제거 (바다로 만듦).</summary>
        Sea = 0,
        /// <summary>이 영역에 새 육지를 추가 (없던 곳에 땅 만듦).</summary>
        Land = 1,
    }

    /// <summary>
    /// 맵을 부분 수정하는 영역 한 개.
    ///   Kind = Sea  → 베이크 시 영역 안 삼각형 제거 (진짜 구멍 뚫림)
    ///   Kind = Land → 베이크 시 NE 폴리곤에 더해 추가 폴리곤으로 삼각화 (없던 땅 생김)
    ///
    /// 두 가지 모양 모드:
    ///   widthKm == 0 : 폴리곤. points 자체가 정점 (3개 이상).
    ///                  TerrainEditor 브러시 클릭은 24각형 (원 근사).
    ///   widthKm  > 0 : 폴리라인. points 가 중심선, widthKm 가 폭.
    ///                  강·해협 표현용.
    ///
    /// 좌표는 lat/lng 쌍 — x = longitude, y = latitude.
    /// </summary>
    [CreateAssetMenu(fileName = "MapSubtract_", menuName = "Game/Data/Map Subtract")]
    public class MapSubtractData : ScriptableObject
    {
        [Header("Identity")]
        public string subtractId;
        public string displayNameKo;

        [Header("Kind")]
        [Tooltip("Sea = 육지를 잘라서 바다로. Land = 새 육지를 추가.")]
        public MapEditKind kind = MapEditKind.Sea;

        [Header("Shape")]
        [Tooltip("폴리라인 폭 (km). 0 = 폴리곤 (points 가 정점). >0 = 폴리라인 (중심선).")]
        [Range(0f, 500f)] public float widthKm = 0f;

        [Tooltip("폴리곤 정점 또는 폴리라인 중심선 (x = longitude, y = latitude).")]
        public Vector2[] points;

        [Header("Behavior")]
        [Tooltip("☑ 만 베이크에 적용. 끄면 데이터는 두되 효과 없음.")]
        public bool enabled = true;

        [Header("Author Notes")]
        [TextArea(1, 3)] public string notes;
    }
}
