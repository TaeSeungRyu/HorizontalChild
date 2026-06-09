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

        [Tooltip("spawn 위치 land 검사 반경. 이 안에 Landmass 있으면 다른 각도/거리 재시도.")]
        public float spawnLandCheckRadius = 4f;

        [Tooltip("항구 주위 spawn 시도 각도 수. 모두 막혔으면 거리 늘려 재시도.")]
        public int spawnAngleSamples = 16;

        [Header("Respawn (M3.5 Step A)")]
        [Tooltip("격침 후 재추첨까지 대기 시간(초). 게임일 시스템 도입 전 임시 실시간 값.")]
        public float respawnDelaySeconds = 60f;

        [Header("Initial Distribution (§3.5)")]
        [Tooltip("Spawn 시 해적이 본거지 광장에서 시작할 확률. 나머지는 바다 spawn.")]
        [Range(0f, 1f)] public float startInPortChancePirate = 0.5f;
        [Tooltip("Spawn 시 상선이 patrolPorts 중 한 곳 광장에서 시작할 확률.")]
        [Range(0f, 1f)] public float startInPortChanceMerchant = 0.7f;
        [Tooltip("Spawn 시 호위선이 본거지 광장에서 시작할 확률.")]
        [Range(0f, 1f)] public float startInPortChanceEscort = 0.7f;
        [Tooltip("초기 dwell 시간 범위(초). 게임 시작 직후 한꺼번에 출항하지 않도록 분산.")]
        public float initialDwellMinSeconds = 15f;
        public float initialDwellMaxSeconds = 90f;

        // npcId → 인스턴스. 파괴되면 Unity가 fake-null 처리 → CollectStates 에서 건너뜀.
        private readonly Dictionary<string, NpcShip> _spawned = new();

        /// <summary>해적의 자동 전투 탐색용. 살아있는 NPC 인스턴스 순회.</summary>
        public IEnumerable<NpcShip> AllSpawned => _spawned.Values;

        // npcId → Time.time 기준 재spawn 예정 시각. GAME_MECHANICS §3.4 (격침 후 랜덤 항구 재배치).
        private readonly Dictionary<string, float> _respawnAt = new();
        private readonly List<string> _respawnReadyBuffer = new();

        // portId → 그 항구의 광장에 머무는 NPC 정의들 (§3.5 비활동 풀). dwell 끝나면 자동 재출항.
        private readonly Dictionary<string, List<NpcDefinition>> _inPort = new();

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

            // 재추첨/광장 dwell 대기 큐 복원
            if (loaded != null && loaded.npcRespawnQueue != null)
            {
                foreach (var entry in loaded.npcRespawnQueue)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.npcId)) continue;
                    _respawnAt[entry.npcId] = Time.time + Mathf.Max(0f, entry.secondsRemaining);

                    // portId 가 있으면 광장 dwell 중 — _inPort 에도 복원
                    if (!string.IsNullOrEmpty(entry.portId))
                    {
                        var def = FindDefById(entry.npcId);
                        if (def != null)
                        {
                            if (!_inPort.TryGetValue(entry.portId, out var list))
                            {
                                list = new List<NpcDefinition>();
                                _inPort[entry.portId] = list;
                            }
                            if (!list.Contains(def)) list.Add(def);
                        }
                    }
                }
            }

            HasSpawned = true;
        }

        private void Update()
        {
            if (_respawnAt.Count == 0) return;
            _respawnReadyBuffer.Clear();
            foreach (var kvp in _respawnAt)
            {
                if (Time.time >= kvp.Value) _respawnReadyBuffer.Add(kvp.Key);
            }
            for (int i = 0; i < _respawnReadyBuffer.Count; i++)
            {
                var id = _respawnReadyBuffer[i];
                _respawnAt.Remove(id);
                RemoveFromInPort(id);   // dwell 중이었으면 광장에서 빠짐
                RespawnAtRandomPort(id);
            }
        }

        /// <summary>전투 패배 NPC 를 풀에서 잠시 빼고 재추첨 큐에 등록. NpcShip 이 호출.</summary>
        public void OnNpcDefeated(NpcDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.npcId)) return;
            _respawnAt[def.npcId] = Time.time + Mathf.Max(0f, respawnDelaySeconds);
            _spawned.Remove(def.npcId);
        }

        /// <summary>NPC 항구 진입 + 광장 등록. dwell 끝나면 자동 재출항. port == null 이면 def.homePort 사용.</summary>
        public void SendNpcToPort(NpcDefinition def, float dwellSeconds, PortData port = null)
        {
            if (def == null || string.IsNullOrEmpty(def.npcId)) return;
            if (port == null) port = def.homePort;
            if (port == null || string.IsNullOrEmpty(port.portId)) return;

            _spawned.Remove(def.npcId);
            _respawnAt[def.npcId] = Time.time + Mathf.Max(1f, dwellSeconds);

            if (!_inPort.TryGetValue(port.portId, out var list))
            {
                list = new List<NpcDefinition>();
                _inPort[port.portId] = list;
            }
            if (!list.Contains(def)) list.Add(def);
        }

        /// <summary>해당 항구의 광장에 머무는 NPC 목록 (고용 대상). PlazaPanel 이 호출.</summary>
        public IReadOnlyList<NpcDefinition> GetNpcsAtPort(PortData port)
        {
            if (port == null || string.IsNullOrEmpty(port.portId)) return null;
            return _inPort.TryGetValue(port.portId, out var list) ? list : null;
        }

        /// <summary>플레이어가 NPC 고용 — 풀 영구 제외. PlazaPanel 이 호출.</summary>
        public bool HireNpcFromPort(NpcDefinition def)
        {
            if (def == null) return false;
            bool removed = false;
            foreach (var kv in _inPort)
            {
                if (kv.Value.Remove(def)) removed = true;
            }
            if (!string.IsNullOrEmpty(def.npcId)) _respawnAt.Remove(def.npcId);
            return removed;
        }

        private void RemoveFromInPort(string npcId)
        {
            foreach (var kv in _inPort)
            {
                var list = kv.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] != null && list[i].npcId == npcId) list.RemoveAt(i);
                }
            }
        }

        private void RespawnAtRandomPort(string npcId)
        {
            var def = FindDefById(npcId);
            if (def == null) return;

            Vector3 pos;
            // 해적은 항상 본거지에서 재spawn — 본거지 예측 가능 + 순찰 사이클 일관성
            if (def.type == NpcType.Pirate && def.homePort != null)
            {
                pos = FindSeaPositionNearPort(def.homePort);
            }
            else if (portCatalog != null && portCatalog.all != null && portCatalog.all.Length > 0)
            {
                PortData port = null;
                for (int i = 0; i < 4 && port == null; i++)
                {
                    var p = portCatalog.all[Random.Range(0, portCatalog.all.Length)];
                    if (p != null) port = p;
                }
                pos = port != null ? FindSeaPositionNearPort(port) : ComputeFreshPosition(def);
            }
            else
            {
                pos = ComputeFreshPosition(def);
            }

            var ship = SpawnNpc(def, pos);
            if (ship != null) ship.RouteIndex = 0;
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

                // 일부 NPC 는 항구에서 시작 → 광장에 즉시 노출. 나머지는 바다 spawn.
                if (TryStartInPort(def)) continue;
                SpawnNpc(def, ComputeFreshPosition(def));
            }
        }

        /// <summary>NPC 타입별 확률로 항구 시작 결정. 성공 시 SendNpcToPort 호출 후 true.</summary>
        private bool TryStartInPort(NpcDefinition def)
        {
            float chance = def.type switch
            {
                NpcType.Pirate => startInPortChancePirate,
                NpcType.Merchant => startInPortChanceMerchant,
                NpcType.Escort => startInPortChanceEscort,
                _ => 0f,
            };
            if (Random.value > chance) return false;

            var port = PickStartPortFor(def);
            if (port == null) return false;

            float dwell = Random.Range(initialDwellMinSeconds, initialDwellMaxSeconds);
            SendNpcToPort(def, dwell, port);
            return true;
        }

        /// <summary>NPC 의 초기 dwell 항구 선택. 해적·호위선 = homePort, 상선 = patrolPorts 중 무작위.</summary>
        private PortData PickStartPortFor(NpcDefinition def)
        {
            if (def.type == NpcType.Merchant && def.patrolPorts != null && def.patrolPorts.Length > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    var p = def.patrolPorts[Random.Range(0, def.patrolPorts.Length)];
                    if (p != null) return p;
                }
            }
            return def.homePort;
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
                return FindSeaPositionNearPort(def.homePort);
            }
            // homePort 없으면 무작위 해상 — 몇 번 재추첨 후 land 면 마지막값 그대로 사용
            for (int i = 0; i < 8; i++)
            {
                var p = new Vector3(
                    Random.Range(-worldHalfWidth, worldHalfWidth),
                    npcY,
                    Random.Range(-worldHalfDepth, worldHalfDepth));
                if (!IsLandAtSpawnPos(p)) return p;
            }
            return new Vector3(
                Random.Range(-worldHalfWidth, worldHalfWidth),
                npcY,
                Random.Range(-worldHalfDepth, worldHalfDepth));
        }

        /// <summary>
        /// 항구 주위 N 각도 시도 → 첫 바다 위치 반환. 모두 land 면 거리 1.5배·2배로 늘려 재시도.
        /// 끝까지 실패하면 마지막 후보 (land 위라도) 반환.
        /// </summary>
        private Vector3 FindSeaPositionNearPort(PortData port)
        {
            var portPos = GeoCoordinate.LatLngToWorld(port.latitude, port.longitude);
            float startAngle = Random.Range(0f, Mathf.PI * 2f);
            float[] distances = { homePortSpawnDistance, homePortSpawnDistance * 1.5f, homePortSpawnDistance * 2.2f };

            Vector3 lastTry = portPos;
            int samples = Mathf.Max(4, spawnAngleSamples);
            for (int d = 0; d < distances.Length; d++)
            {
                for (int i = 0; i < samples; i++)
                {
                    float angle = startAngle + (Mathf.PI * 2f * i / samples);
                    var p = new Vector3(
                        portPos.x + Mathf.Cos(angle) * distances[d],
                        npcY,
                        portPos.z + Mathf.Sin(angle) * distances[d]);
                    lastTry = p;
                    if (!IsLandAtSpawnPos(p)) return p;
                }
            }
            return lastTry;
        }

        private static readonly Collider[] _spawnLandBuffer = new Collider[8];
        private bool IsLandAtSpawnPos(Vector3 worldPos)
        {
            int count = Physics.OverlapSphereNonAlloc(worldPos, spawnLandCheckRadius, _spawnLandBuffer);
            for (int i = 0; i < count; i++)
            {
                if (_spawnLandBuffer[i] == null) continue;
                if (_spawnLandBuffer[i].GetComponentInParent<Landmass>() != null) return true;
            }
            return false;
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

        /// <summary>재추첨/광장 dwell 대기 NPC 큐 — secondsRemaining + portId(광장 dwell) 직렬화.</summary>
        public List<NpcRespawnEntry> CollectRespawnQueue()
        {
            var list = new List<NpcRespawnEntry>();
            foreach (var kvp in _respawnAt)
            {
                list.Add(new NpcRespawnEntry
                {
                    npcId = kvp.Key,
                    secondsRemaining = Mathf.Max(0f, kvp.Value - Time.time),
                    portId = FindPortIdForNpc(kvp.Key) ?? string.Empty,
                });
            }
            return list;
        }

        private string FindPortIdForNpc(string npcId)
        {
            foreach (var kv in _inPort)
            {
                foreach (var def in kv.Value)
                {
                    if (def != null && def.npcId == npcId) return kv.Key;
                }
            }
            return null;
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
