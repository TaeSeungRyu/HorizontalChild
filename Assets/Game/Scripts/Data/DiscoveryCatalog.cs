using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 모든 DiscoveryData 의 카탈로그 — 단일 인덱스.
    /// CatalogRefresher 가 Assets/Game/Data/Discoveries 폴더를 자동 스캔해서 채움.
    ///
    /// SeaWorldManager / JournalPanel 등은 본 카탈로그 한 개만 참조하면 됨.
    /// 새 DiscoveryData 추가 시 Game ▸ Refresh All Catalogs 메뉴로 자동 갱신.
    /// </summary>
    [CreateAssetMenu(fileName = "DiscoveryCatalog", menuName = "Game/Data/Discovery Catalog")]
    public class DiscoveryCatalog : ScriptableObject
    {
        public DiscoveryData[] all;
    }
}
