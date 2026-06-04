using Game.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 발견물 발견 시 표시되는 큰 패널.
    /// 일러스트(있으면) + 카테고리 + 이름 + 메인 해설 + 더 보기.
    ///
    /// 어린이 톤:
    ///   - 등장 시 짧은 축하 텍스트 ("새로운 곳을 발견했어요!")
    ///   - 카테고리는 한글로 표시
    ///   - 일러스트가 없으면(M1 임시) 빈 박스 + 안내
    ///
    /// 의뢰 진행도는 MissionService 가 RegisterDiscovery 로 이미 반영됨.
    /// 본 패널은 시각 표시 전용 — 패널 닫으면 다시 항해.
    /// </summary>
    public class DiscoveryFoundPanel : MonoBehaviour
    {
        [Header("Refs")]
        public GameObject panelRoot;

        public TMP_Text headerText;             // "새로운 곳을 발견했어요!"
        public TMP_Text nameText;               // 발견물 이름
        public TMP_Text categoryText;           // 카테고리 (한글)
        public TMP_Text descriptionText;        // 메인 해설
        public TMP_Text moreInfoText;           // 더 보기 (선택)
        public Image illustrationImage;         // 일러스트 (M1 임시는 비어둠)
        public GameObject noIllustrationPlaceholder;  // 일러스트 없을 때 보여줄 박스

        public Button closeButton;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);

            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public void Show(DiscoveryData discovery)
        {
            if (discovery == null) return;
            if (panelRoot == null) panelRoot = gameObject; // 비활성 GameObject 호출 안전장치

            if (headerText != null)
            {
                headerText.text = "새로운 곳을 발견했어요!";
            }

            if (nameText != null)
            {
                nameText.text = discovery.displayNameKo;
            }

            if (categoryText != null)
            {
                categoryText.text = CategoryToKorean(discovery.category);
            }

            if (descriptionText != null)
            {
                descriptionText.text = discovery.mainDescription;
            }

            if (moreInfoText != null)
            {
                if (!string.IsNullOrEmpty(discovery.moreInfo))
                {
                    moreInfoText.gameObject.SetActive(true);
                    moreInfoText.text = discovery.moreInfo;
                }
                else
                {
                    moreInfoText.gameObject.SetActive(false);
                }
            }

            // 일러스트
            if (illustrationImage != null)
            {
                if (discovery.illustration != null)
                {
                    illustrationImage.sprite = discovery.illustration;
                    illustrationImage.gameObject.SetActive(true);
                    if (noIllustrationPlaceholder != null)
                        noIllustrationPlaceholder.SetActive(false);
                }
                else
                {
                    illustrationImage.gameObject.SetActive(false);
                    if (noIllustrationPlaceholder != null)
                        noIllustrationPlaceholder.SetActive(true);
                }
            }

            // 다른 패널(도감 등) 위에 떠야 하므로 같은 부모 안에서 마지막 형제로
            panelRoot.transform.SetAsLastSibling();
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            panelRoot.SetActive(false);
        }

        private static string CategoryToKorean(DiscoveryCategory category)
        {
            return category switch
            {
                DiscoveryCategory.Landmark => "랜드마크",
                DiscoveryCategory.FloraFauna => "동식물",
                DiscoveryCategory.Ruin => "유적",
                DiscoveryCategory.Event => "사건",
                _ => "발견물"
            };
        }
    }
}
