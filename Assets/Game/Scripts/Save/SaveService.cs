using System;
using System.IO;
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

        [Header("Refs")]
        public ShipController playerShip;

        [Header("Events")]
        public UnityEvent onSaved;
        public UnityEvent onLoaded;

        private const string FileName = "save_slot_0.json";
        private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public bool HasSave => File.Exists(FilePath);

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

            // 위치
            if (playerShip == null) playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);
            if (playerShip != null)
            {
                data.shipX = playerShip.transform.position.x;
                data.shipZ = playerShip.transform.position.z;
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

            // 위치
            if (playerShip == null) playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);
            if (playerShip != null)
            {
                var pos = playerShip.transform.position;
                playerShip.transform.position = new Vector3(data.shipX, pos.y, data.shipZ);
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
