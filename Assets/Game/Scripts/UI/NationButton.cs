using System;
using Game.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 국적 선택 패널의 각 국가 버튼.
    /// NationSelectionPanel 이 prefab 인스턴스화한 뒤 Bind() 로 데이터 주입.
    ///
    /// prefab 구성:
    ///   - Button (이 컴포넌트와 같은 GameObject)
    ///   - Image (배경 — 국가 강조색)
    ///   - 자식 Text - TextMeshPro (국가 이름)
    /// </summary>
    public class NationButton : MonoBehaviour
    {
        [Header("Refs")]
        public Button button;
        public Image backgroundImage;
        public TMP_Text nameLabel;

        public NationData Nation { get; private set; }

        private Action<NationData> _onClicked;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();
            if (nameLabel == null) nameLabel = GetComponentInChildren<TMP_Text>(includeInactive: true);

            if (button != null) button.onClick.AddListener(HandleClick);
        }

        public void Bind(NationData nation, Action<NationData> onClicked)
        {
            Nation = nation;
            _onClicked = onClicked;

            if (nation == null) return;
            if (nameLabel != null) nameLabel.text = nation.displayNameKo;
            if (backgroundImage != null)
            {
                var c = nation.accentColor;
                c.a = 0.85f; // 약간 투명
                backgroundImage.color = c;
            }
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(Nation);
        }
    }
}
