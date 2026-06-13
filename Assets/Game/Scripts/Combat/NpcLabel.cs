using Game.Data;
using TMPro;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// NPC 배 위에 떠다니는 world-space 라벨 — 타입 + 캐릭터 이름.
    ///
    /// 표시 형식: "[해적] 검은수염" / "[상선] 마르코" / "[호위] 한스"
    /// - 타입은 색상 (해적=빨강, 상선=금색, 호위=파랑)
    /// - 이름은 흰색 굵게
    /// - 검은 외곽선으로 어떤 배경에서도 가독성 확보
    ///
    /// 위치: NPC 머리 위. 카메라 빌보드. 거리 초과 시 자동 숨김 (그리드 깨끗하게).
    /// 수명: target NPC 가 파괴되면 자동으로 자기 소멸.
    /// </summary>
    public class NpcLabel : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("NPC 위로 떠 있는 높이 (월드 단위). 배 + 돛 높이 위로 충분히.")]
        public float yOffsetWorld = 14f;
        [Tooltip("TMP 폰트 크기.")]
        public float fontSize = 6f;
        [Tooltip("카메라가 이 거리보다 멀면 숨김. 매우 크게 잡아 거리 컬링 무력화 가능.")]
        public float maxVisibleDistance = 1000f;

        private NpcShip _target;
        private TextMeshPro _tmp;
        private Camera _cam;

        public void Bind(NpcShip target, TMP_FontAsset font)
        {
            _target = target;
            // 폰트 fallback — null 이면 TMP 기본 폰트 (한글 없을 수 있음 → 워닝)
            if (font == null)
            {
                font = TMP_Settings.defaultFontAsset;
                if (font == null)
                {
                    Debug.LogWarning("[NpcLabel] TMP 폰트 없음 — NpcSpawner.npcLabelFont 에 한글 SDF 폰트 할당 필요.");
                }
                else
                {
                    Debug.LogWarning("[NpcLabel] NpcSpawner.npcLabelFont 미할당 — TMP 기본 폰트 사용 (한글 안 나올 수 있음).");
                }
            }
            EnsureLabel(font);
            UpdateText();
        }

        private void EnsureLabel(TMP_FontAsset font)
        {
            if (_tmp != null) return;
            // 보장: scale 1 (TMP 렌더 크기는 fontSize 가 결정)
            transform.localScale = Vector3.one;

            _tmp = gameObject.AddComponent<TextMeshPro>();
            if (font != null) _tmp.font = font;
            _tmp.alignment = TextAlignmentOptions.Center;
            _tmp.fontSize = fontSize;
            _tmp.enableWordWrapping = false;
            _tmp.outlineWidth = 0.22f;
            _tmp.outlineColor = new Color32(0, 0, 0, 220);
            _tmp.richText = true;
            _tmp.faceColor = Color.white;

            // TMP 3D 의 기본 RectTransform 크기가 0×0 이면 글자 잘림 → 충분히 키움
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(40f, 8f);
            }
        }

        private void UpdateText()
        {
            if (_target == null || _target.definition == null || _tmp == null) return;
            var def = _target.definition;
            string name = (def.character != null && !string.IsNullOrEmpty(def.character.displayNameKo))
                ? def.character.displayNameKo : "?";

            string tag = def.type switch
            {
                NpcType.Pirate   => "<color=#FF4F4F>[해적]</color>",
                NpcType.Merchant => "<color=#FFCF55>[상선]</color>",
                NpcType.Escort   => "<color=#6DAEFF>[호위]</color>",
                _                => "[NPC]",
            };

            _tmp.text = $"{tag} <b>{name}</b>";
        }

        private void LateUpdate()
        {
            // target 사라지면 라벨도 소멸
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            // NPC 위로 위치 동기
            var pos = _target.transform.position;
            transform.position = new Vector3(pos.x, pos.y + yOffsetWorld, pos.z);

            // 빌보드 — TMP 텍스트 면이 카메라 향하도록 회전 (text 의 -Z 가 camera 향함)
            transform.rotation = _cam.transform.rotation;

            // 거리 컬링
            if (_tmp != null)
            {
                float dist = Vector3.Distance(_cam.transform.position, transform.position);
                _tmp.enabled = dist <= maxVisibleDistance;
            }
        }
    }
}
