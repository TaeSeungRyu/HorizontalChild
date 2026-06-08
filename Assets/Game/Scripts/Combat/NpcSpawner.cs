using Game.Data;
using Game.UI;
using Game.World;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// NPC 배 스폰너 — 게임 시작 시 N 개의 NPC 배를 해상에 배치.
    /// MVP: 정적 배치 (이동 X). 추후 AI 이동 / 동적 재스폰 추가 가능.
    ///
    /// 위치: NpcDefinition.homePort 가 있으면 그 근처, 없으면 무작위 해상.
    /// 시각: 작은 큐브 + 타입별 색상.
    /// </summary>
    public class NpcSpawner : MonoBehaviour
    {
        [Header("Refs")]
        public NpcCatalog npcCatalog;
        public CombatResultPanel resultPanel;
        public Transform npcsParent;

        [Header("Spawn")]
        [Tooltip("게임 시작 시 spawn 할 NPC 개수. 카탈로그가 12명이면 12 권장.")]
        [Range(0, 30)] public int spawnCount = 12;

        [Tooltip("homePort 가 있는 NPC 의 spawn 거리 (Unity Unit). 항구에서 이만큼 떨어진 곳.")]
        public float homePortSpawnDistance = 40f;

        [Tooltip("homePort 가 없는 NPC 는 무작위 해상 위치에 spawn. 세계 범위 내.")]
        public float worldHalfWidth = 2700f;
        public float worldHalfDepth = 1350f;

        [Tooltip("NPC 배 시각용 큐브 크기.")]
        public float npcSize = 6f;

        [Tooltip("NPC 배 Y 좌표 (배와 같은 높이).")]
        public float npcY = 0f;

        private void Start()
        {
            if (npcsParent == null) npcsParent = transform;
            if (resultPanel == null)
            {
                resultPanel = FindAnyObjectByType<CombatResultPanel>(FindObjectsInactive.Include);
            }

            if (npcCatalog == null || npcCatalog.all == null || npcCatalog.all.Length == 0)
            {
                Debug.LogWarning("[NpcSpawner] NpcCatalog 비어있음 — spawn 안 함.");
                return;
            }

            int count = Mathf.Min(spawnCount, npcCatalog.all.Length);
            for (int i = 0; i < count; i++)
            {
                var def = npcCatalog.all[i % npcCatalog.all.Length];
                if (def == null) continue;
                SpawnNpc(def);
            }
        }

        private void SpawnNpc(NpcDefinition def)
        {
            Vector3 worldPos;
            if (def.homePort != null)
            {
                var portPos = GeoCoordinate.LatLngToWorld(def.homePort.latitude, def.homePort.longitude);
                // 항구 근처 무작위 각도 + 거리
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float r = homePortSpawnDistance;
                worldPos = new Vector3(
                    portPos.x + Mathf.Cos(angle) * r,
                    npcY,
                    portPos.z + Mathf.Sin(angle) * r);
            }
            else
            {
                worldPos = new Vector3(
                    Random.Range(-worldHalfWidth, worldHalfWidth),
                    npcY,
                    Random.Range(-worldHalfDepth, worldHalfDepth));
            }

            var npc = GameObject.CreatePrimitive(PrimitiveType.Cube);
            npc.transform.SetParent(npcsParent);
            npc.transform.position = worldPos;
            npc.transform.localScale = Vector3.one * npcSize;

            var renderer = npc.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                var color = ColorFor(def.type);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                renderer.material = mat;
            }

            // BoxCollider 가 Cube primitive 에 기본 부착 — IPointerClickHandler 동작
            var script = npc.AddComponent<NpcShip>();
            script.Bind(def, resultPanel);
        }

        private static Color ColorFor(NpcType type) => type switch
        {
            NpcType.Pirate => new Color(0.85f, 0.20f, 0.20f),    // 빨강
            NpcType.Merchant => new Color(0.90f, 0.75f, 0.25f),  // 노랑
            NpcType.Escort => new Color(0.30f, 0.60f, 0.90f),    // 파랑
            _ => Color.white,
        };
    }
}
