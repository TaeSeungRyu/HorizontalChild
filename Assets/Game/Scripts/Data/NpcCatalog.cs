using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 모든 NpcDefinition 을 모아 spawner 가 풀에서 선택할 수 있게.
    /// CatalogRefresher 가 자동 채움.
    /// </summary>
    [CreateAssetMenu(fileName = "NpcCatalog", menuName = "Game/Data/Npc Catalog")]
    public class NpcCatalog : ScriptableObject
    {
        public NpcDefinition[] all;
    }
}
