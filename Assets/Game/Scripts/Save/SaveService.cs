using System;
using System.Collections.Generic;
using System.IO;
using Game.Combat;
using Game.Data;
using Game.Missions;
using Game.Player;
using Game.Ship;
using Game.World;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Save
{
    /// <summary>
    /// 게임 진행도 저장·복원. 단일 슬롯, JSON 파일.
    ///
    /// 자동 저장 트리거 (§11.4):
    ///   - 항구 입항 직후 (SeaWorldManager.onPortArrived 에서 호출)
    ///   - 발견물 획득 직후 (MissionService.onDiscoveryRegistered 에서 호출)
    ///   - 앱 일시정지 (OnApplicationPause(true))
    ///   - 거래 후 (PlayerCargo.onCargoChanged, PlayerState.onMoneyChanged)
    ///
    /// 자동 로드:
    ///   - Awake 시점에 저장 파일 있으면 자동 복원
    ///   - 복원 성공 시 GameSession.SelectedNation 가 set 되어 NationSelectionPanel 가 자동 skip
    ///
    /// 파일 위치:
    ///   Application.persistentDataPath/save_slot_0.json
    /// </summary>
    public class SaveService : MonoBehaviour
    {
        public static SaveService Instance { get; private set; }

        [Header("Catalogs — ID → Data 매핑")]
        public NationCatalog nationCatalog;
        public DiscoveryCatalog discoveryCatalog;
        public MissionCatalog missionCatalog;
        public RegionCatalog regionCatalog;
        public ProductCatalog productCatalog;
        public ShipCatalog shipCatalog;
        public NpcCatalog npcCatalog;

        [Header("Refs")]
        public ShipController playerShip;

        [Header("Events")]
        public UnityEvent onSaved;
        public UnityEvent onLoaded;

        private const string FileName = "save_slot_0.json";
        private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public bool HasSave => File.Exists(FilePath);

        /// <summary>
        /// 마지막으로 로드된 데이터 — NpcSpawner 가 지연 spawn 시 참조.
        /// CollectState 도 NpcSpawner 가 아직 준비 안됐을 때 fallback 으로 사용.
        /// </summary>
        public SaveData LastLoaded { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            // 1) 트리거 자동 등록 — 싱글톤들에 listener 부착
            HookAutoSaveTriggers();

            // 2) 저장 데이터 있으면 자동 로드
            if (HasSave)
            {
                TryLoad();
            }
        }

        private void HookAutoSaveTriggers()
        {
            var sea = FindAnyObjectByType<SeaWorldManager>(FindObjectsInactive.Include);
            if (sea != null) sea.onPortArrived.AddListener(_ => SaveGame());

            var ms = MissionService.Instance;
            if (ms != null)
            {
                ms.onDiscoveryRegistered.AddListener(_ => SaveGame());
                ms.onRegionUnlocked.AddListener(_ => SaveGame());
                ms.onMissionCompleted.AddListener(_ => SaveGame());
            }

            // 거래 후 자동 저장 — onCargoChanged / onMoneyChanged
            var cargo = PlayerCargo.Instance;
            if (cargo != null) cargo.onCargoChanged.AddListener(SaveGame);

            // 전투 종료 후 저장은 NpcShip.OnPointerClick 가 명시적으로 호출함 —
            // NpcSpawner.OnNpcDefeated(재추첨 큐 등록) 이후에 저장되어야 하므로
            // 여기서 onCombatResolved 를 직접 구독하지 않음 (순서 보장).
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && HasGameSession()) SaveGame();
        }

        private bool HasGameSession()
        {
            return GameSession.Instance != null && GameSession.Instance.SelectedNation != null;
        }

        // ─── 저장 ─────────────────────────────────────────────────────────

        [ContextMenu("Save Now")]
        public void SaveGame()
        {
            if (!HasGameSession())
            {
                // 국가 선택 전엔 저장 안 함
                return;
            }

            try
            {
                var data = CollectState();
                var json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(FilePath, json);
                onSaved?.Invoke();
                Debug.Log($"[SaveService] 저장 완료 → {FilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] 저장 실패: {e}");
            }
        }

        private SaveData CollectState()
        {
            var data = new SaveData
            {
                version = 1,
                savedAtUtc = DateTime.UtcNow.ToString("o"),
            };

            // 국가
            var session = GameSession.Instance;
            if (session != null && session.SelectedNation != null)
            {
                data.nationId = session.SelectedNation.nationId;
            }

            // Mission state
            var ms = MissionService.Instance;
            if (ms != null)
            {
                data.discoveredIds = new System.Collections.Generic.List<string>(ms.DiscoveredIds);
                data.completedMissionIds = new System.Collections.Generic.List<string>(ms.CompletedMissionIds);
                data.unlockedRegionIds = new System.Collections.Generic.List<string>(ms.UnlockedRegionIds);
                data.currentMissionId = ms.CurrentMission != null ? ms.CurrentMission.missionId : null;
            }

            // PlayerState
            var ps = PlayerState.Instance;
            if (ps != null)
            {
                data.money = ps.Money;
                data.goodReputation = ps.GoodReputation;
                data.badReputation = ps.BadReputation;
            }

            // Cargo
            var cargo = PlayerCargo.Instance;
            if (cargo != null)
            {
                foreach (var kvp in cargo.Items)
                {
                    data.cargo.Add(new CargoSlot
                    {
                        productId = kvp.Key,
                        quantity = kvp.Value.quantity,
                    });
                }
            }

            // 위치 + 내구도 + 사용 중인 배
            if (playerShip == null) playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);
            if (playerShip != null)
            {
                data.shipX = playerShip.transform.position.x;
                data.shipZ = playerShip.transform.position.z;
                data.playerDurability = playerShip.CurrentDurability;
                data.shipId = playerShip.shipData != null ? playerShip.shipData.shipId : null;
            }

            // 고용한 선원
            var crew = PlayerCrew.Instance;
            if (crew != null)
            {
                data.crewNpcIds.Clear();
                foreach (var npc in crew.Crew)
                {
                    if (npc != null && !string.IsNullOrEmpty(npc.npcId)) data.crewNpcIds.Add(npc.npcId);
                }
            }

            // NPC 배 상태 — spawner 가 준비됐으면 현재 상태 수집,
            // 아직 spawn 전이면 (예: 로드 직후 onCargoChanged 자동저장) 직전 로드값 유지
            var spawner = NpcSpawner.Instance;
            if (spawner != null && spawner.HasSpawned)
            {
                data.npcs = spawner.CollectStates();
                data.npcRespawnQueue = spawner.CollectRespawnQueue();
            }
            else if (LastLoaded != null)
            {
                if (LastLoaded.npcs != null)
                    data.npcs = new List<NpcStateData>(LastLoaded.npcs);
                if (LastLoaded.npcRespawnQueue != null)
                    data.npcRespawnQueue = new List<NpcRespawnEntry>(LastLoaded.npcRespawnQueue);
            }

            return data;
        }

        // ─── 로드 ─────────────────────────────────────────────────────────

        [ContextMenu("Load Now")]
        public bool TryLoad()
        {
            if (!HasSave) return false;
            try
            {
                var json = File.ReadAllText(FilePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                {
                    Debug.LogError("[SaveService] JSON 파싱 실패");
                    return false;
                }
                LastLoaded = data;   // ApplyData 전에 set — NpcSpawner 가 한 프레임 뒤 읽음
                ApplyData(data);
                onLoaded?.Invoke();
                Debug.Log($"[SaveService] 로드 완료. 저장 시각: {data.savedAtUtc}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] 로드 실패: {e}");
                return false;
            }
        }

        private void ApplyData(SaveData data)
        {
            // 국가
            if (!string.IsNullOrEmpty(data.nationId) && nationCatalog != null)
            {
                NationData nation = null;
                foreach (var n in nationCatalog.all)
                {
                    if (n != null && n.nationId == data.nationId) { nation = n; break; }
                }
                if (nation != null)
                {
                    GameSession.Instance?.SetSelectedNation(nation);
                }
            }

            // Mission state
            var ms = MissionService.Instance;
            if (ms != null)
            {
                ms.DiscoveredIds.Clear();
                foreach (var id in data.discoveredIds) ms.DiscoveredIds.Add(id);

                ms.CompletedMissionIds.Clear();
                foreach (var id in data.completedMissionIds) ms.CompletedMissionIds.Add(id);

                ms.UnlockedRegionIds.Clear();
                foreach (var id in data.unlockedRegionIds) ms.UnlockedRegionIds.Add(id);

                // CurrentMission 복원 — MissionCatalog 에서 찾기
                if (!string.IsNullOrEmpty(data.currentMissionId) && missionCatalog != null)
                {
                    foreach (var m in missionCatalog.all)
                    {
                        if (m != null && m.missionId == data.currentMissionId)
                        {
                            ms.TryAcceptMission(m);
                            break;
                        }
                    }
                }
            }

            // PlayerState — Money/Reputation 은 setter 가 없으니 직접 추가
            var ps = PlayerState.Instance;
            if (ps != null)
            {
                // 현재값을 빼고 저장값을 더해서 복원
                ps.AddMoney(data.money - ps.Money);
                ps.AddGoodReputation(data.goodReputation - ps.GoodReputation);
                ps.AddBadReputation(data.badReputation - ps.BadReputation);
            }

            // Cargo
            var cargo = PlayerCargo.Instance;
            if (cargo != null && productCatalog != null)
            {
                foreach (var slot in data.cargo)
                {
                    var product = productCatalog.FindById(slot.productId);
                    if (product != null) cargo.TryAdd(product, slot.quantity);
                }
            }

            // 위치 + 내구도 + 사용 중인 배
            if (playerShip == null) playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);
            if (playerShip != null)
            {
                var pos = playerShip.transform.position;
                playerShip.transform.position = new Vector3(data.shipX, pos.y, data.shipZ);
                if (data.playerDurability >= 0) playerShip.SetDurability(data.playerDurability);

                // 고용 선원 복원
                var crew = PlayerCrew.Instance;
                if (crew != null && data.crewNpcIds != null && data.crewNpcIds.Count > 0)
                {
                    crew.Clear();
                    if (npcCatalog != null && npcCatalog.all != null)
                    {
                        foreach (var id in data.crewNpcIds)
                        {
                            if (string.IsNullOrEmpty(id)) continue;
                            foreach (var n in npcCatalog.all)
                            {
                                if (n != null && n.npcId == id) { crew.TryHire(n); break; }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[SaveService] crewNpcIds 복원 실패 — npcCatalog 미할당.");
                    }
                }

                // 구매한 배 복원 — shipId 로 카탈로그 조회
                if (!string.IsNullOrEmpty(data.shipId))
                {
                    if (shipCatalog == null || shipCatalog.all == null)
                    {
                        Debug.LogWarning(
                            $"[SaveService] shipId='{data.shipId}' 복원 실패: " +
                            "Inspector 에 ShipCatalog 미할당. SaveService 컴포넌트에 ShipCatalog.asset 드래그 필요.");
                    }
                    else
                    {
                        ShipData saved = null;
                        foreach (var s in shipCatalog.all)
                        {
                            if (s != null && s.shipId == data.shipId) { saved = s; break; }
                        }
                        if (saved == null)
                        {
                            Debug.LogWarning(
                                $"[SaveService] shipId='{data.shipId}' 가 ShipCatalog 에 없음. " +
                                "Game ▸ Refresh All Catalogs 실행 필요.");
                        }
                        else if (playerShip.shipData != saved)
                        {
                            playerShip.shipData = saved;
                            playerShip.RefreshVisual();
                            Debug.Log($"[SaveService] 배 복원: {saved.displayName} (id={saved.shipId})");
                        }
                    }
                }
            }
        }

        // ─── 삭제 (디버그) ────────────────────────────────────────────────

        [ContextMenu("Delete Save")]
        public void DeleteSave()
        {
            if (HasSave)
            {
                File.Delete(FilePath);
                Debug.Log($"[SaveService] 저장 파일 삭제: {FilePath}");
            }
        }
    }
}
