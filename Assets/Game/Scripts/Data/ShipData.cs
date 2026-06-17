using UnityEngine;

namespace Game.Data
{
    public enum ShipSize
    {
        Small = 0,    // 소형 — 포탄 1발
        Medium = 1,   // 중형 — 포탄 2발 (0.1초 간격, 시각만)
        Large = 2,    // 대형 — 포탄 3발 (0.1초 간격, 시각만)
    }

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

        [Header("Combat Visual")]
        [Tooltip("배 크기 — 한 번에 발사되는 포탄 시각 개수. Small=1, Medium=2, Large=3. 데미지에는 영향 없음.")]
        public ShipSize size = ShipSize.Small;

        [Tooltip("이 배가 발사하는 포탄 색.")]
        public Color cannonColor = new Color(0.95f, 0.85f, 0.2f);

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
