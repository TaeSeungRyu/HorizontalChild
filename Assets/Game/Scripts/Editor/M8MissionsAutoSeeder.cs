using System.Collections.Generic;
using System.IO;
using Game.Data;
using Game.World;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M8 — 미션 자동 채움. 미션이 없는 모든 항구에 가장 가까운 발견물 1개 할당.
    /// 9개 → 100개로 확장. 기존 미션은 보존 (이름 다른 새 미션만 추가).
    ///
    /// 메뉴: Game/Seed M8 Missions (Auto)
    /// 시드 후 Game ▸ Refresh All Catalogs 실행.
    /// </summary>
    public static class M8MissionsAutoSeeder
    {
        private const string DataRoot = "Assets/Game/Data";

        [MenuItem("Game/Seed M8 Missions (Auto)")]
        public static void SeedM8Missions()
        {
            EnsureFolder($"{DataRoot}/Missions");

            // 모든 항구 + 발견물 로드
            var ports = LoadAll<PortData>($"{DataRoot}/Ports");
            var discoveries = LoadAll<DiscoveryData>($"{DataRoot}/Discoveries");
            if (ports.Count == 0 || discoveries.Count == 0)
            {
                Debug.LogError("[M8] Port 또는 Discovery 에셋이 없어요. 먼저 시드.");
                return;
            }

            // 이미 미션 발급 중인 항구 ID 모음 (중복 생성 방지)
            var portsWithMission = new HashSet<string>();
            var existingMissions = LoadAll<MissionTemplate>($"{DataRoot}/Missions");
            foreach (var m in existingMissions)
            {
                if (m != null && m.issuerPort != null && !string.IsNullOrEmpty(m.issuerPort.portId))
                {
                    portsWithMission.Add(m.issuerPort.portId);
                }
            }

            int created = 0, skipped = 0;
            foreach (var port in ports)
            {
                if (port == null) continue;
                if (portsWithMission.Contains(port.portId)) { skipped++; continue; }

                // 가장 가까운 발견물 선택
                DiscoveryData best = null;
                float bestDist = float.MaxValue;
                foreach (var d in discoveries)
                {
                    if (d == null) continue;
                    float dist = GeoCoordinate.DistanceUnits(port.latitude, port.longitude, d.latitude, d.longitude);
                    if (dist < bestDist) { bestDist = dist; best = d; }
                }
                if (best == null) continue;

                // 미션 에셋 생성
                string fileName = $"Mission_Disc{Sanitize(port.portId)}_{Sanitize(best.discoveryId)}.asset";
                string path = $"{DataRoot}/Missions/{fileName}";
                if (AssetDatabase.LoadAssetAtPath<MissionTemplate>(path) != null) { skipped++; continue; }

                var mt = ScriptableObject.CreateInstance<MissionTemplate>();
                mt.missionId = $"mission.disc.{Sanitize(port.portId)}.{Sanitize(best.discoveryId)}";
                mt.issuerPort = port;
                mt.targetDiscovery = best;
                mt.rewardMoney = 200 + Mathf.RoundToInt(bestDist * 0.5f);
                mt.rewardGoodReputation = 5 + Mathf.RoundToInt(bestDist / 100f);
                mt.title = $"{best.displayNameKo} 을(를) 찾아주세요";
                mt.description =
                    $"{port.displayNameKo} 의 모험가 조합 의뢰입니다.\n" +
                    $"{best.displayNameKo} 의 자취를 발견해서 가져와 주세요. 보상은 {mt.rewardMoney:N0} G.";
                mt.mapItemName = $"{best.displayNameKo} 으로 가는 지도";

                AssetDatabase.CreateAsset(mt, path);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[M8MissionsAutoSeeder] 완료. {created}개 미션 추가, {skipped}개 항구 이미 있어 건너뜀.\n" +
                $"전체 미션 ≈ {existingMissions.Count + created} 개.\n" +
                "다음: Game ▸ Refresh All Catalogs → MissionCatalog 갱신.\n" +
                "PortFacilities.guildMissions 는 사용 안 함 — MissionCatalog 가 issuerPort 로 필터링.");
        }

        // ─── 헬퍼 ───────────────────────────────────────────────────────────

        private static List<T> LoadAll<T>(string folder) where T : Object
        {
            var list = new List<T>();
            if (!AssetDatabase.IsValidFolder(folder)) return list;
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            foreach (var g in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g));
                if (asset != null) list.Add(asset);
            }
            return list;
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "x";
            var sb = new System.Text.StringBuilder();
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else if (c == '.' || c == '_' || c == '-') sb.Append('_');
            }
            return sb.Length > 0 ? sb.ToString() : "x";
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
