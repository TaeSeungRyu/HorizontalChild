using Game.Data;
using Game.Ship;
using Game.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Combat
{
    /// <summary>
    /// 해상에 spawn 된 NPC 배. 탭하면 전투 결과 패널이 뜸.
    ///
    /// 클릭 처리:
    ///   IPointerClickHandler + SphereCollider — Main Camera 의 Physics Raycaster 사용.
    ///   DiscoveryMarker 와 동일 패턴이라 별도 셋업 불필요.
    ///
    /// 시각:
    ///   NpcSpawner 가 생성할 때 PrimitiveType.Cube + 색상 (해적=빨강, 상선=노랑, 호위선=파랑).
    ///   추후 ShipData.prefab3D 같은 정식 모델로 교체 가능.
    /// </summary>
    public class NpcShip : MonoBehaviour, IPointerClickHandler
    {
        public NpcDefinition definition;
        public CombatResultPanel resultPanel;

        public void Bind(NpcDefinition def, CombatResultPanel panel)
        {
            definition = def;
            resultPanel = panel;
            if (def != null && def.character != null)
            {
                name = $"Npc_{def.type}_{def.character.displayNameKo}";
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (definition == null) return;
            var player = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);
            if (player == null) return;

            var service = CombatService.Instance;
            if (service == null)
            {
                Debug.LogWarning("[NpcShip] CombatService 없음 — 전투 불가");
                return;
            }

            var result = service.Resolve(player, definition);

            if (resultPanel != null) resultPanel.Show(result);

            // 승리 시 NPC 사라짐 (패배 시 그대로 남음)
            if (result.playerWon)
            {
                Destroy(gameObject, 0.5f);
            }
        }
    }
}
