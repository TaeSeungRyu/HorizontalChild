using System.Text;
using Game.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 항구 내부 화면 — M1 단순 버전.
    /// 도시명·국가·한 줄 설명 + 시장에 진열된 특산물(일반·스페셜) 표시.
    ///
    /// M2 이후 확장:
    ///   - 모험가 조합(의뢰), 조선소(배 매매), 광장(고용) 탭 추가
    ///   - 시세 표시 (±20% 변동)
    ///
    /// 사용:
    ///   Canvas 하위 패널 GameObject 에 본 컴포넌트 부착.
    ///   필드에 NameText / DescriptionText / ProductListText / LeaveButton 할당.
    /// </summary>
    public class PortScreen : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("이 패널 GameObject. 비어 있으면 본 컴포넌트가 붙은 객체.")]
        public GameObject panelRoot;

        public TMP_Text nameText;
        public TMP_Text descriptionText;
        public TMP_Text productListText;
        public Button leaveButton;

        private PortData _currentPort;
        private PortArrivalDialog _arrivalDialog;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);

            if (leaveButton != null) leaveButton.onClick.AddListener(OnLeaveClicked);
        }

        /// <summary>외부(PortArrivalDialog)에서 호출 — 항구 화면 열기.</summary>
        public void OpenForPort(PortData port, PortArrivalDialog arrivalDialog)
        {
            if (port == null) return;
            _currentPort = port;
            _arrivalDialog = arrivalDialog;

            FillUI();
            panelRoot.SetActive(true);
        }

        private void FillUI()
        {
            if (nameText != null)
            {
                string nationName = _currentPort.nation != null
                    ? _currentPort.nation.displayNameKo
                    : "중립 항구";
                nameText.text = $"🏠 {_currentPort.displayNameKo} ({nationName})";
            }

            if (descriptionText != null)
            {
                descriptionText.text = _currentPort.shortDescription;
            }

            if (productListText != null)
            {
                productListText.text = BuildProductList();
            }
        }

        private string BuildProductList()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>이 항구에서 만날 수 있는 것들:</b>");
            sb.AppendLine();

            if (_currentPort.commonProducts != null && _currentPort.commonProducts.Length > 0)
            {
                sb.AppendLine("📦 <b>특산물</b>");
                foreach (var product in _currentPort.commonProducts)
                {
                    if (product == null) continue;
                    sb.AppendLine($"  • {product.displayNameKo} — {product.shortDescription}");
                }
            }

            if (_currentPort.specialProducts != null && _currentPort.specialProducts.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("⭐ <b>특별한 물건 (의뢰로만 구할 수 있어요)</b>");
                foreach (var product in _currentPort.specialProducts)
                {
                    if (product == null) continue;
                    sb.AppendLine($"  • {product.displayNameKo} — {product.shortDescription}");
                }
            }

            return sb.ToString();
        }

        private void OnLeaveClicked()
        {
            panelRoot.SetActive(false);

            // 떠난 직후 같은 항구를 다시 트리거하지 않도록 PortArrivalDialog 에 알림
            if (_arrivalDialog != null)
            {
                _arrivalDialog.OnPortScreenClosed();
            }

            _currentPort = null;
            _arrivalDialog = null;
        }
    }
}
