using System.Collections.Generic;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// NpcDefinition.homePort 가 비어있는 NPC 들을 자동으로 채움 (영구).
    /// 우선순위 (NpcSpawner.ResolveHomePort 와 동일):
    ///   character.homePort → destinationPort → patrolPorts[0]
    ///   → character.nation.startingPort → portCatalog 첫 항구
    ///
    /// 메뉴: Game ▸ Fix NPC Home Ports (Auto-Fill Empty)
    /// </summary>
    public static class NpcHomePortFixer
    {
        [MenuItem("Game/Fix NPC Home Ports (Auto-Fill Empty)")]
        public static void FixEmpty()
        {
            var catalogGuids = AssetDatabase.FindAssets("t:NpcCatalog");
            if (catalogGuids.Length == 0)
            {
                EditorUtility.DisplayDialog("NPC Home Ports", "NpcCatalog 를 찾을 수 없습니다.", "OK");
                return;
            }
            var catalog = AssetDatabase.LoadAssetAtPath<NpcCatalog>(
                AssetDatabase.GUIDToAssetPath(catalogGuids[0]));
            if (catalog == null || catalog.all == null)
            {
                EditorUtility.DisplayDialog("NPC Home Ports", "NpcCatalog.all 이 비어있습니다.", "OK");
                return;
            }

            // PortCatalog (마지막 fallback 용)
            var portCatalogGuids = AssetDatabase.FindAssets("t:PortCatalog");
            PortCatalog portCatalog = null;
            if (portCatalogGuids.Length > 0)
            {
                portCatalog = AssetDatabase.LoadAssetAtPath<PortCatalog>(
                    AssetDatabase.GUIDToAssetPath(portCatalogGuids[0]));
            }

            int fixedCount = 0, alreadySet = 0, failed = 0;
            var failedList = new List<string>();
            var fixedList = new List<string>();

            foreach (var def in catalog.all)
            {
                if (def == null) continue;
                if (def.homePort != null) { alreadySet++; continue; }

                var resolved = Resolve(def, portCatalog);
                if (resolved != null)
                {
                    def.homePort = resolved;
                    EditorUtility.SetDirty(def);
                    fixedCount++;
                    fixedList.Add($"  • {def.name} → {resolved.displayNameKo} ({resolved.portId})");
                }
                else
                {
                    failed++;
                    failedList.Add($"  • {def.name}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 결과 로그 (긴 리스트는 첫 20줄만)
            string fixedSummary = fixedCount == 0 ? "(없음)" :
                string.Join("\n", fixedList.GetRange(0, System.Math.Min(20, fixedList.Count)));
            if (fixedList.Count > 20) fixedSummary += $"\n  ... 외 {fixedList.Count - 20}개";

            string failedSummary = failed == 0 ? "" : "\n실패 (resolve 불가):\n" + string.Join("\n", failedList);

            Debug.Log(
                $"[NpcHomePortFixer] 완료.\n" +
                $"  • 채워짐: {fixedCount}개\n" +
                $"  • 이미 설정됨: {alreadySet}개\n" +
                $"  • 실패: {failed}개\n" +
                (fixedCount > 0 ? $"\n채워진 항목:\n{fixedSummary}" : "") +
                failedSummary);

            EditorUtility.DisplayDialog("NPC Home Ports",
                $"완료.\n채워짐: {fixedCount}\n이미 설정: {alreadySet}\n실패: {failed}\n\n자세한 내용은 Console 확인.",
                "OK");
        }

        /// <summary>NpcSpawner.ResolveHomePort 와 동일한 우선순위. Editor 만 쓰는 정적 버전.</summary>
        private static PortData Resolve(NpcDefinition def, PortCatalog portCatalog)
        {
            if (def == null) return null;
            if (def.homePort != null) return def.homePort;
            if (def.character != null && def.character.homePort != null) return def.character.homePort;
            if (def.destinationPort != null) return def.destinationPort;
            if (def.patrolPorts != null && def.patrolPorts.Length > 0 && def.patrolPorts[0] != null)
                return def.patrolPorts[0];
            if (def.character != null && def.character.nation != null
                && def.character.nation.startingPort != null)
                return def.character.nation.startingPort;
            if (portCatalog != null && portCatalog.all != null)
            {
                foreach (var p in portCatalog.all) if (p != null) return p;
            }
            return null;
        }

        [MenuItem("Game/Report NPC Home Ports (Read-Only)")]
        public static void Report()
        {
            var catalogGuids = AssetDatabase.FindAssets("t:NpcCatalog");
            if (catalogGuids.Length == 0) { Debug.LogWarning("NpcCatalog 없음."); return; }
            var catalog = AssetDatabase.LoadAssetAtPath<NpcCatalog>(
                AssetDatabase.GUIDToAssetPath(catalogGuids[0]));
            if (catalog == null || catalog.all == null) return;

            int withHome = 0, withoutHome = 0;
            var missing = new List<string>();
            foreach (var def in catalog.all)
            {
                if (def == null) continue;
                if (def.homePort != null) withHome++;
                else { withoutHome++; missing.Add(def.name); }
            }

            string list = missing.Count == 0 ? "(없음)" :
                string.Join("\n  • ", missing.GetRange(0, System.Math.Min(30, missing.Count)));
            if (missing.Count > 30) list += $"\n  ... 외 {missing.Count - 30}개";

            Debug.Log(
                $"[NpcHomePortFixer] 현황.\n" +
                $"  • homePort 있음: {withHome}\n" +
                $"  • homePort 없음: {withoutHome}\n" +
                (withoutHome > 0 ? $"\n빈 NPC:\n  • {list}" : ""));
        }
    }
}
