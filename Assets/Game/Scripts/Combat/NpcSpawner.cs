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
    /// NPC 배 스폰너 — 100명 풀세트 (해적 20 / 호위선 40 / 상선 40) 관리.
    ///
    /// 모델 (§3.5):
    ///   - 각 NPC 는 광장에 dwell (in port) OR 바다에서 sailing (active) 둘 중 하나.
    ///   - 광장 dwell 중: 일정 간격(rollInterval) 마다 활동률 주사위 → 통과 시 항해 시작.
    ///   - 상선: 광장 → 목적지/본거지 향해 항해 → 도착 → 그 항구 광장 dwell → 반복.
    ///   - 해적·호위선: 광장 → wander 30~50초 → 본거지 복귀 → 광장 dwell → 반복.
    ///   - 전투 패배: 즉시 본거지 광장으로 (SendNpcToPort).
    ///
    /// 저장: _nextRollAt + _inPort 상태 직렬화 (NpcRespawnEntry 의 portId + secondsRemaining).
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
        [Tooltip("게임 시작 시 spawn 할 NPC 개수. 100명 풀세트면 100 권장.")]
        [Range(0, 200)] public int spawnCount = 100;

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

        [Header("Activity Rates (§3.5)")]
        [Tooltip("해적 활동률 — 항해 중일 확률. 1 - 이 값 = 광장 dwell 비율.")]
        [Range(0f, 1f)] public float pirateActivityRate = 0.5f;
        [Tooltip("호위선 활동률.")]
        [Range(0f, 1f)] public float escortActivityRate = 0.2f;
        [Tooltip("상선 활동률.")]
        [Range(0f, 1f)] public float merchantActivityRate = 0.2f;

        [Header("Roll Interval (광장 dwell 주사위 주기)")]
        [Tooltip("광장에 머무는 동안 활동 주사위 굴리는 간격(초). 통과 시 항해 시작.")]
        public float rollIntervalMin = 12f;
        public float rollIntervalMax = 25f;

        [Header("Sailing Wander (해적·호위선 활동 시간)")]
        [Tooltip("해적·호위선이 광장에서 출항해서 본거지로 돌아오기 전까지 wander 시간(초).")]
        public float wanderDurationMin = 30f;
        public float wanderDurationMax = 50f;

        // npcId → 인스턴스. 파괴되면 Unity가 fake-null 처리 → CollectStates 에서 건너뜀.
        private readonly Dictionary<string, NpcShip> _spawned = new();

        /// <summary>해적의 자동 전투 탐색용. 살아있는 NPC 인스턴스 순회.</summary>
        public IEnumerable<NpcShip> AllSpawned => _spawned.Values;

        // npcId → 다음 활동 주사위 시각. 광장 dwell NPC 만 등록.
        private readonly Dictionary<string, float> _nextRollAt = new();
        private readonly List<string> _rollReadyBuffer = new();

        // npcId → 현재 머물고 있는 portId. _inPort 의 역방향 인덱스.
        private readonly Dictionary<string, string> _npcCurrentPort = new();

        // portId → 그 항구의 광장에 머무는 NPC 정의들 (§3.5 비활동 풀).
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
            int savedCount = (loaded != null && loaded.npcs != null) ? loaded.npcs.Count : 0;
            int savedInPortCount = (loaded != null && loaded.npcRespawnQueue != null)
                ? CountValidPortEntries(loaded.npcRespawnQueue) : 0;

            if (savedCount > 0)
            {
                int spawnedFromSave = SpawnFromSave(loaded.npcs);
                // 옛 세이브 — npcId 가 모두 stale 이면 fresh 로 fallback
                if (spawnedFromSave == 0 && savedInPortCount == 0)
                {
                    Debug.LogWarning("[NpcSpawner] 저장된 NPC IDs 가 현재 카탈로그와 매칭 안 됨 — fresh 분배로 전환.");
                    SpawnFresh();
                }
            }
            else
            {
                SpawnFresh();
            }

            // 광장 dwell + 다음 roll 시각 복원 (저장된 entry 의 portId+secondsRemaining)
            if (loaded != null && loaded.npcRespawnQueue != null)
            {
                foreach (var entry in loaded.npcRespawnQueue)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.npcId)) continue;
                    if (string.IsNullOrEmpty(entry.portId)) continue;   // 옛 격침 큐는 무시

                    var def = FindDefById(entry.npcId);
                    if (def == null) continue;

                    _nextRollAt[entry.npcId] = Time.time + Mathf.Max(0f, entry.secondsRemaining);
                    _npcCurrentPort[entry.npcId] = entry.portId;

                    if (!_inPort.TryGetValue(entry.portId, out var list))
                    {
                        list = new List<NpcDefinition>();
                        _inPort[entry.portId] = list;
                    }
                    if (!list.Contains(def)) list.Add(def);
                }
            }

            HasSpawned = true;
        }

        private void Update()
        {
            if (_nextRollAt.Count == 0) return;
            _rollReadyBuffer.Clear();
            foreach (var kvp in _nextRollAt)
            {
                if (Time.time >= kvp.Value) _rollReadyBuffer.Add(kvp.Key);
            }
            for (int i = 0; i < _rollReadyBuffer.Count; i++)
            {
                RollForUndock(_rollReadyBuffer[i]);
            }
        }

        /// <summary>광장 dwell NPC 의 활동 주사위. 통과 시 Undock, 실패 시 다음 roll 예약.</summary>
        private void RollForUndock(string npcId)
        {
            if (!_nextRollAt.ContainsKey(npcId)) return;
            var def = FindDefById(npcId);
            if (def == null) { _nextRollAt.Remove(npcId); return; }

            float rate = ActivityRateFor(def.type);
            if (Random.value < rate)
            {
                Undock(npcId);
            }
            else
            {
                _nextRollAt[npcId] = Time.time + Random.Range(rollIntervalMin, rollIntervalMax);
            }
        }

        private float ActivityRateFor(NpcType type) => type switch
        {
            NpcType.Pirate => pirateActivityRate,
            NpcType.Escort => escortActivityRate,
            NpcType.Merchant => merchantActivityRate,
            _ => 0.3f,
        };

        /// <summary>주사위 통과 → 항구에서 출항. 상선은 반대편 항구로, 해적·호위선은 wander 모드.</summary>
        private void Undock(string npcId)
        {
            var def = FindDefById(npcId);
            if (def == null) return;
            if (!_npcCurrentPort.TryGetValue(npcId, out var fromPortId)) return;

            // 광장에서 빼기
            _nextRollAt.Remove(npcId);
            _npcCurrentPort.Remove(npcId);
            if (_inPort.TryGetValue(fromPortId, out var list)) list.Remove(def);

            // 항해 목표 결정 — destinationPort 가 있으면 cycle, 없으면 wander
            PortData fromPort = FindPortById(fromPortId);
            PortData target = null;
            float wanderSeconds = 0f;
            if (def.destinationPort != null && def.homePort != null)
            {
                // 현재 항구의 반대편 (home ↔ destination) — 상선·호위선
                target = (fromPort == def.homePort) ? def.destinationPort : def.homePort;
            }
            else
            {
                // wander 30~50초 후 자동 본거지 복귀 — 해적
                wanderSeconds = Random.Range(wanderDurationMin, wanderDurationMax);
            }

            // 바다 spawn + sailing 설정
            Vector3 spawnPos = fromPort != null ? FindSeaPositionNearPort(fromPort) : ComputeFreshPosition(def);
            var ship = SpawnNpc(def, spawnPos);
            if (ship != null) ship.ConfigureSailing(target, wanderSeconds);
        }

        /// <summary>전투 패배 NPC → 본거지 광장으로 즉시 이동. NpcShip 이 호출.</summary>
        public void OnNpcDefeated(NpcDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.npcId)) return;
            _spawned.Remove(def.npcId);
            SendNpcToPort(def, 0f, def.homePort);   // 본거지 광장에 즉시 노출
        }

        /// <summary>NPC 항구 진입 + 광장 등록. 즉시 다음 roll 예약.</summary>
        public void SendNpcToPort(NpcDefinition def, float unused, PortData port = null)
        {
            if (def == null || string.IsNullOrEmpty(def.npcId)) return;
            if (port == null) port = def.homePort;
            if (port == null || string.IsNullOrEmpty(port.portId)) return;

            _spawned.Remove(def.npcId);
            _nextRollAt[def.npcId] = Time.time + Random.Range(rollIntervalMin, rollIntervalMax);
            _npcCurrentPort[def.npcId] = port.portId;

            if (!_inPort.TryGetValue(port.portId, out var list))
            {
                list = new List<NpcDefinition>();
                _inPort[port.portId] = list;
            }
            if (!list.Contains(def)) list.Add(def);
        }

        private PortData FindPortById(string portId)
        {
            if (string.IsNullOrEmpty(portId) || portCatalog == null || portCatalog.all == null) return null;
            foreach (var p in portCatalog.all)
            {
                if (p != null && p.portId == portId) return p;
            }
            return null;
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
            if (!string.IsNullOrEmpty(def.npcId))
            {
                _nextRollAt.Remove(def.npcId);
                _npcCurrentPort.Remove(def.npcId);
            }
            return removed;
        }

        private int SpawnFromSave(List<NpcStateData> states)
        {
            int spawned = 0;
            foreach (var s in states)
            {
                if (s == null) continue;
                var def = FindDefById(s.npcId);
                if (def == null) continue;
                var ship = SpawnNpc(def, new Vector3(s.x, npcY, s.z));
                if (ship != null)
                {
                    ship.RouteIndex = s.routeIndex;
                    spawned++;
                }
            }
            return spawned;
        }

        private int CountValidPortEntries(List<NpcRespawnEntry> entries)
        {
            int n = 0;
            foreach (var e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.npcId) || string.IsNullOrEmpty(e.portId)) continue;
                if (FindDefById(e.npcId) != null) n++;
            }
            return n;
        }

        private void SpawnFresh()
        {
            int count = Mathf.Min(spawnCount, npcCatalog.all.Length);
            for (int i = 0; i < count; i++)
            {
                var def = npcCatalog.all[i];
                if (def == null) continue;
                PlaceInitial(def);
            }
        }

        /// <summary>NPC 초기 배치 — 활동률 주사위로 항해 시작 vs 광장 시작 결정.</summary>
        private void PlaceInitial(NpcDefinition def)
        {
            float rate = ActivityRateFor(def.type);
            bool startSailing = Random.value < rate;

            if (!startSailing)
            {
                // 광장에서 시작 — 상선은 home/destination 50:50
                var port = PickStartPortFor(def);
                if (port != null)
                {
                    SendNpcToPort(def, 0f, port);
                    return;
                }
            }

            // 항해 중 시작 — 적절한 출발 항구 골라 sailing 설정
            var origin = PickStartPortFor(def);
            Vector3 spawnPos = origin != null ? FindSeaPositionNearPort(origin) : ComputeFreshPosition(def);

            PortData target = null;
            float wanderSeconds = 0f;
            if (def.destinationPort != null && def.homePort != null)
            {
                target = (origin == def.homePort) ? def.destinationPort : def.homePort;
            }
            else
            {
                wanderSeconds = Random.Range(wanderDurationMin, wanderDurationMax);
            }

            var ship = SpawnNpc(def, spawnPos);
            if (ship != null) ship.ConfigureSailing(target, wanderSeconds);
        }

        /// <summary>NPC 의 시작 항구 선택 — destinationPort 있으면 home/destination 50:50, 없으면 homePort.</summary>
        private PortData PickStartPortFor(NpcDefinition def)
        {
            if (def.homePort != null && def.destinationPort != null)
            {
                return Random.value < 0.5f ? def.homePort : def.destinationPort;
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
            // 카브 영역(지브롤터 등) 은 land 아님 — Ceuta 처럼 협소한 항구도 spawn 가능
            if (WorldCarves.IsInOpenArea(worldPos)) return false;

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
            // 빈 root + 클릭용 BoxCollider — 시각은 ProceduralShipBuilder 가 자식으로 배치
            var npc = new GameObject($"NpcShip_{(def != null ? def.npcId : "?")}");
            npc.transform.SetParent(npcsParent);
            npc.transform.position = worldPos;
            npc.transform.localScale = Vector3.one * npcSize;

            // 배 전체 영역 클릭 가능 — IPointerClickHandler 동작
            var col = npc.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0f, 0f);
            col.size = new Vector3(0.7f, 0.7f, 1.4f);   // hull + 약간 margin

            // 배 모양 시각 빌드 — 선체·갑판·돛대·돛·뱃머리
            ProceduralShipBuilder.BuildShip(npc, def != null ? def.type : NpcType.Merchant);

            var script = npc.AddComponent<NpcShip>();
            script.Bind(def, resultPanel);
            if (def != null && !string.IsNullOrEmpty(def.npcId)) _spawned[def.npcId] = script;
            return script;
        }

        /// <summary>광장 dwell 중인 NPC 직렬화 — nextRoll 까지 남은 시간 + portId.</summary>
        public List<NpcRespawnEntry> CollectRespawnQueue()
        {
            var list = new List<NpcRespawnEntry>();
            foreach (var kvp in _nextRollAt)
            {
                _npcCurrentPort.TryGetValue(kvp.Key, out var portId);
                list.Add(new NpcRespawnEntry
                {
                    npcId = kvp.Key,
                    secondsRemaining = Mathf.Max(0f, kvp.Value - Time.time),
                    portId = portId ?? string.Empty,
                });
            }
            return list;
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
