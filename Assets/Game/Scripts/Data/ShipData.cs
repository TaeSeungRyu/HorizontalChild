using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "Ship_", menuName = "Game/Data/Ship Data")]
    public class ShipData : ScriptableObject
    {
        [Header("Identity")]
        public string shipId;
        public string displayName;
        public Sprite icon;
        public GameObject prefab3D;

        [Header("Stats (GAME_MECHANICS §2.3 범위)")]
        [Range(1, 30)] public int cannonPower = 3;
        [Range(1, 10)] public int speed = 5;
        [Range(10, 1000)] public int cargoCapacity = 60;
        [Range(10, 200)] public int maxDurability = 50;

        [Tooltip("포탄 발사 간격(초). 작을수록 빠른 연사. 0.5~3 권장.")]
        [Range(0.3f, 4f)] public float attackInterval = 1.5f;

        [Header("Economy")]
        public int basePrice = 5000;
        public ReputationGate gate;

        [Header("Texts (어린이용)")]
        [TextArea(1, 2)] public string shortDescription;
        [TextArea(2, 4)] public string longDescription;

        [Header("Author Notes")]
        public string sourceUrl;
    }
}
