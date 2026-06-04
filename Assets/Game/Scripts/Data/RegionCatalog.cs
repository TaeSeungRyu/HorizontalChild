using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 모든 RegionData 를 단일 SO 에 묶어 관리. CatalogRefresher 가 자동 채움.
    /// 다른 컴포넌트는 본 카탈로그를 인스펙터에 한 번만 연결하면 됨.
    /// </summary>
    [CreateAssetMenu(fileName = "RegionCatalog", menuName = "Game/Data/Region Catalog")]
    public class RegionCatalog : ScriptableObject
    {
        public RegionData[] all;
    }
}
