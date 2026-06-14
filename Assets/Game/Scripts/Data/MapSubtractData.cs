using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 맵에서 "육지를 빼는" 영역 한 개. 베이크 시 NE 폴리곤에서 이 영역에 들어가는
    /// 삼각형을 모두 제거 → 시각적으로 진짜 구멍이 뚫림 + 충돌도 동시에 풀림.
    ///
    /// 두 가지 모드:
    ///   widthKm == 0 : 폴리곤. points 자체가 폴리곤 정점 (3개 이상).
    ///                  사각형이면 4점, 더 복잡하면 더 많이.
    ///   widthKm  > 0 : 폴리라인. points 가 강·해협의 중심선, widthKm 가 폭.
    ///                  베이커가 양쪽으로 perpendicular extrude 해서 사각 띠로 변환.
    ///
    /// 좌표는 lat/lng 쌍 — x = longitude, y = latitude. PortData 와 같은 관례.
    /// </summary>
    [CreateAssetMenu(fileName = "MapSubtract_", menuName = "Game/Data/Map Subtract")]
    public class MapSubtractData : ScriptableObject
    {
        [Header("Identity")]
        public string subtractId;
        public string displayNameKo;

        [Header("Shape")]
        [Tooltip("폴리라인 폭 (km). 0 = 폴리곤 모드 (points 가 정점). >0 = 폴리라인 모드.")]
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
