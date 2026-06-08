using System.Collections;
using System.Collections.Generic;
using Game.Data;
using Game.Save;
using Game.UI;
using Game.World;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// NPC 배 스폰너 — 게임 시작 시 N 개의 NPC 배를 해상에 배치.
    ///
    /// 저장 통합 (M3.5 Phase 1):
    ///   - Start 가 한 프레임 지연 후 spawn → SaveService.TryLoad 가 먼저 완료
    ///   - SaveService.LastLoaded.npcs 가 있으면 그 상태(위치+routeIndex)로 spawn
    ///   - 없으면 카탈로그에서 신규 spawn (homePort 근처 or 무작위)
    ///   - CollectStates() 로 현재 상태를 SaveService 에 노출.
    ///
    /// 위치: NpcDefinition.homePort 가 있으면 그 근처, 없으면 무작위 해상.
    /// 시각: 작은 큐브 + 타입별 색상.
    /// </summary>
    public class NpcSpawner : MonoBehaviour
    {
        public static NpcSpawner Instance { get; private set; }

        [Header("Refs")]
        public NpcCatalog npcCatalog;
        public PortCatalog portCatalog;   // 격침 NPC 재배치용 — 인스펙터에서 PortCatalog SO 할당
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

        [Header("Respawn (M3.5 Step A)")]
        [Tooltip("격침 후 재추첨까지 대기 시간(초). 게임일 시스템 도입 전 임시 실시간 값.")]
        public float respawnDelaySeconds = 60f;

        // npcId → 인스턴스. 파괴되면 Unity가 fake-null 처리 → CollectStates 에서 건너뜀.
        private readonly Dictionary<string, NpcShip> _spawned = new();

        // npcId → Time.time 기준 재spawn 예정 시각. GAME_MECHANICS §3.4 (격침 후 랜덤 항구 재배치).
        private readonly Dictionary<string, float> _respawnAt = new();
        private readonly List<string> _respawnReadyBuffer = new();

        /// <summary>한 프레임 지연 후 spawn 완료되었는지. SaveService.CollectState 가 사용.</summary>
        public bool HasSpawned { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private IEnumerator Start()
        {
            // 한 프레임 지연 — SaveService.TryLoad 가 LastLoaded 를 채울 시간을 줌
            yield return null;

            if (npcsParent == null) npcsParent = transform;
            if (resultPanel == null)
            {
                resultPanel = FindAnyObjectByType<CombatResultPanel>(FindObjectsInactive.Include);
            }

            if (npcCatalog == null || npcCatalog.all == null || npcCatalog.all.Length == 0)
            {
                Debug.LogWarning("[NpcSpawner] NpcCatalog 비어있음 — spawn 안 함.");
                HasSpawned = true;
                yield break;
            }

            var loaded = SaveService.Instance != null ? SaveService.Instance.LastLoaded : null;
            if (loaded != null && loaded.npcs != null && loaded.npcs.Count > 0)
            {
                SpawnFromSave(loaded.npcs);
            }
            else
            {
                SpawnFresh();
            }

            HasSpawned = true;
        }

        private void SpawnFromSave(List<NpcStateData> states)
        {
            foreach (var s in states)
            {
                if (s == null) continue;
                var def = FindDefById(s.npcId);
                if (def == null) continue;
                var ship = SpawnNpc(def, new Vector3(s.x, npcY, s.z));
                if (ship != null) ship.RouteIndex = s.routeIndex;
            }
        }

        private void SpawnFresh()
        {
            int count = Mathf.Min(spawnCount, npcCatalog.all.Length);
            for (int i = 0; i < count; i++)
            {
                var def = npcCatalog.all[i];
                if (def == null) continue;
                SpawnNpc(def, ComputeFreshPosition(def));
            }
        }

        private NpcDefinition FindDefById(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return null;
            foreach (var d in npcCatalog.all)
            {
                if (d != null && d.npcId == npcId) return d;
            }
            return null;
        }

        private Vector3 ComputeFreshPosition(NpcDefinition def)
        {
            if (def.homePort != null)
            {
                var portPos = GeoCoordinate.LatLngToWorld(def.homePort.latitude, def.homePort.longitude);
                float angle = Random.Range(0f, Mathf.PI * 2f);
                return new Vector3(
                    portPos.x + Mathf.Cos(angle) * homePortSpawnDistance,
                    npcY,
                    portPos.z + Mathf.Sin(angle) * homePortSpawnDistance);
            }
            return new Vector3(
                Random.Range(-worldHalfWidth, worldHalfWidth),
                npcY,
                Random.Range(-worldHalfDepth, worldHalfDepth));
        }

        private NpcShip SpawnNpc(NpcDefinition def, Vector3 worldPos)
        {
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
            if (!string.IsNullOrEmpty(def.npcId)) _spawned[def.npcId] = script;
            return script;
        }

        /// <summary>현재 살아있는 NPC 들의 위치·routeIndex 수집. SaveService 가 호출.</summary>
        public List<NpcStateData> CollectStates()
        {
            var list = new List<NpcStateData>();
            foreach (var kvp in _spawned)
            {
                var ship = kvp.Value;
                if (ship == null) continue;   // 파괴됨 (플레이어 승리) → 다음 로드에서 부활 X
                var t = ship.transform;
                list.Add(new NpcStateData
                {
                    npcId = kvp.Key,
                    x = t.position.x,
                    z = t.position.z,
                    routeIndex = ship.RouteIndex,
                });
            }
            return list;
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
