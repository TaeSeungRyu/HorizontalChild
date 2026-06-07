using UnityEngine;

namespace Game.Data
{
    /// <summary>모든 CharacterData 를 모아 광장(고용) UI 가 풀에서 선택할 수 있게.</summary>
    [CreateAssetMenu(fileName = "CharacterCatalog", menuName = "Game/Data/Character Catalog")]
    public class CharacterCatalog : ScriptableObject
    {
        public CharacterData[] all;
    }
}
