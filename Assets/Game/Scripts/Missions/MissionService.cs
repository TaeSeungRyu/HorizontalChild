using System.Collections.Generic;
using Game.Data;
using Game.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Missions
{
    /// <summary>
    /// 의뢰 상태 매니저.
    ///
    /// 책임:
    ///   - 모든 의뢰 카탈로그 보유 (Inspector 에서 할당)
    ///   - 현재 진행 중인 의뢰 1개 추적 (기획서: 동시 1건 제한)
    ///   - 도감 — 발견한 항목 ID 누적
    ///   - 이벤트 발행: 수락 / 취소 / 완료 / 발견 등록
    ///
    /// M1 단순화: 메모리에만 저장. JSON 저장은 M3 의 Save 시스템에서 처리.
    ///
    /// 싱글톤:
    ///   GameManager 같은 영구 GameObject 에 부착 후 인스펙터에서 카탈로그 채움.
    ///   다른 컴포넌트는 MissionService.Instance 로 접근.
    /// </summary>
    public class MissionService : MonoBehaviour
    {
        public static MissionService Instance { get; private set; }

        [Header("Catalog — MissionCatalog 우선, 비어 있으면 배열 fallback")]
        [Tooltip("MissionCatalog SO. Game ▸ Refresh All Catalogs 로 자동 채워짐. 우선 사용.")]
        public MissionCatalog missionCatalog;
        [Tooltip("Fallback: 카탈로그 없을 때 직접 등록할 의뢰 배열.")]
        public MissionTemplate[] allMissions;

        public MissionTemplate[] EffectiveMissions =>
            (missionCatalog != null && missionCatalog.all != null && missionCatalog.all.Length > 0)
                ? missionCatalog.all : allMissions;

        [Header("Events")]
        public UnityEvent<MissionTemplate> onMissionAccepted;
        public UnityEvent<MissionTemplate> onMissionCancelled;
        public UnityEvent<MissionTemplate> onMissionCompleted;
        public UnityEvent<DiscoveryData> onDiscoveryRegistered;

        // ─── 런타임 상태 ─────────────────────────────────────────────────────

        /// <summary>현재 진행 중인 의뢰. null 이면 활성 의뢰 없음.</summary>
        public MissionTemplate CurrentMission { get; private set; }

        /// <summary>이미 발견한 발견물의 ID 집합 (도감).</summary>
        public readonly HashSet<string> DiscoveredIds = new();

        /// <summary>이미 완료한 의뢰 ID 집합 (중복 발급 방지).</summary>
        public readonly HashSet<string> CompletedMissionIds = new();

        public bool HasActiveMission => CurrentMission != null;

        // ─── 라이프사이클 ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[MissionService] 인스턴스가 둘 이상. {gameObject.name} 무시.");
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─── 의뢰 조회 ──────────────────────────────────────────────────────

        /// <summary>
        /// 특정 항구에서 발급 가능한 의뢰 목록 반환.
        /// 조건: issuerPort 일치 + 아직 완료 안 함 + (발견물 의뢰의 경우) 아직 발견 안 됨.
        /// </summary>
        public List<MissionTemplate> GetAvailableMissionsForPort(PortData port)
        {
            var result = new List<MissionTemplate>();
            var missions = EffectiveMissions;
            if (port == null || missions == null) return result;

            foreach (var mission in missions)
            {
                if (mission == null) continue;
                if (mission.issuerPort != port) continue;
                if (CompletedMissionIds.Contains(mission.missionId)) continue;
                // 이미 발견한 발견물의 의뢰는 발급 X
                if (mission.targetDiscovery != null &&
                    DiscoveredIds.Contains(mission.targetDiscovery.discoveryId)) continue;

                result.Add(mission);
            }
            return result;
        }

        // ─── 의뢰 수락 / 취소 ────────────────────────────────────────────────

        /// <summary>
        /// 의뢰 수락. 이미 진행 중인 의뢰가 있으면 거절 (기획 §2.2 조건).
        /// </summary>
        public bool TryAcceptMission(MissionTemplate mission)
        {
            if (mission == null) return false;
            if (HasActiveMission)
            {
                Debug.Log($"[MissionService] 이미 의뢰 진행 중: {CurrentMission.missionId}");
                return false;
            }

            CurrentMission = mission;
            onMissionAccepted?.Invoke(mission);
            Debug.Log($"[MissionService] 의뢰 수락: {mission.missionId} ({mission.title})");
            return true;
        }

        /// <summary>의뢰 취소 — 명시적 포기. 페널티는 §8.7 결정 후 추가.</summary>
        public void CancelCurrentMission()
        {
            if (!HasActiveMission) return;
            var cancelled = CurrentMission;
            CurrentMission = null;
            onMissionCancelled?.Invoke(cancelled);
            Debug.Log($"[MissionService] 의뢰 취소: {cancelled.missionId}");
        }

        // ─── 발견물 등록 / 의뢰 완료 ────────────────────────────────────────

        /// <summary>
        /// 발견물 발견 시 호출 — SeaWorldManager.onDiscoveryFound 에서 연결.
        /// 도감에 등록 + 활성 의뢰가 이 발견물을 대상으로 한다면 진행도 갱신.
        /// 실제 의뢰 완료(보상 지급)는 의뢰 항구로 복귀 시 TryCompleteAtPort 에서.
        /// </summary>
        public void RegisterDiscovery(DiscoveryData discovery)
        {
            if (discovery == null) return;
            if (DiscoveredIds.Contains(discovery.discoveryId)) return;

            DiscoveredIds.Add(discovery.discoveryId);
            onDiscoveryRegistered?.Invoke(discovery);
            Debug.Log($"[MissionService] 발견물 등록: {discovery.discoveryId} ({discovery.displayNameKo})");
        }

        /// <summary>
        /// 의뢰 항구 복귀 시 호출 — PortScreen 또는 PortArrivalDialog 에서 연결.
        /// 활성 의뢰의 issuerPort 와 일치하고 조건 충족 시 완료 처리 + 보상.
        /// 반환: 완료된 의뢰 (없으면 null).
        /// </summary>
        public MissionTemplate TryCompleteAtPort(PortData port)
        {
            if (!HasActiveMission || port == null) return null;
            if (CurrentMission.issuerPort != port) return null;

            // 발견물 의뢰 — 도감에 해당 발견물이 있는지 확인
            if (CurrentMission.targetDiscovery == null) return null;
            if (!DiscoveredIds.Contains(CurrentMission.targetDiscovery.discoveryId))
            {
                Debug.Log("[MissionService] 의뢰 완료 조건 미충족 — 발견물 미발견");
                return null;
            }

            var completed = CurrentMission;
            CompletedMissionIds.Add(completed.missionId);
            CurrentMission = null;

            // 보상 지급 (PlayerState 가 있으면)
            var playerState = PlayerState.Instance;
            if (playerState != null)
            {
                if (completed.rewardMoney > 0) playerState.AddMoney(completed.rewardMoney);
                if (completed.rewardGoodReputation > 0) playerState.AddGoodReputation(completed.rewardGoodReputation);
            }
            else
            {
                Debug.LogWarning("[MissionService] PlayerState 인스턴스 없음 — 보상 지급 못 함.");
            }

            onMissionCompleted?.Invoke(completed);
            Debug.Log(
                $"[MissionService] 의뢰 완료! {completed.missionId} — " +
                $"보상 돈 {completed.rewardMoney}, 좋은 명성 +{completed.rewardGoodReputation}");
            return completed;
        }
    }
}
