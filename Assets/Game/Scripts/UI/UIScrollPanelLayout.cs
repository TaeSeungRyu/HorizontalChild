using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 풀스크린 패널 + 상단 타이틀·서브타이틀 + 가운데 ScrollView + 하단 Close 버튼.
    /// MarketPanel / ShipyardPanel / PlazaPanel 가 공통으로 사용하는 Auto Layout 유틸.
    ///
    /// 호출 시 자식 UI 위치·크기 일괄 정렬 + 필요하면 ScrollView·Content 생성.
    /// </summary>
    public static class UIScrollPanelLayout
    {
        public static void ApplyFullscreenWithScrollList(
            GameObject panelRoot,
            TMP_Text titleText,
            TMP_Text[] subtitleTexts,
            ref RectTransform rowsContainer,
            Button closeButton)
        {
            if (panelRoot == null) return;
            var panelRT = panelRoot.GetComponent<RectTransform>();
            if (panelRT == null) return;

            // 1) 패널 — 풀스크린
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = Vector2.zero;
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            // 불투명 배경
            var bg = panelRoot.GetComponent<Image>();
            if (bg == null) bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.13f, 0.18f, 1f);
            bg.raycastTarget = true;

            // 2) Title — 상단
            if (titleText != null)
                SetRect(titleText.rectTransform,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -40f), new Vector2(1720f, 70f));

            // 3) Subtitles — 좌·우 (최대 2개 가정)
            if (subtitleTexts != null)
            {
                float offsetX = -400f;
                for (int i = 0; i < subtitleTexts.Length; i++)
                {
                    if (subtitleTexts[i] == null) continue;
                    SetRect(subtitleTexts[i].rectTransform,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(offsetX, -120f), new Vector2(800f, 40f));
                    offsetX += 800f;
                }
            }

            // 4) ScrollView — 가운데 stretch
            EnsureScrollView(panelRoot, ref rowsContainer);

            // 5) Close 버튼 — 하단 중앙, 최상위
            if (closeButton != null)
            {
                SetRect(closeButton.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 40f), new Vector2(240f, 80f));
                closeButton.transform.SetAsLastSibling();
            }
        }

        private static void EnsureScrollView(GameObject panelRoot, ref RectTransform rowsContainer)
        {
            if (rowsContainer != null)
            {
                var scroll = rowsContainer.GetComponentInParent<ScrollRect>();
                if (scroll != null) { StretchScroll(scroll.GetComponent<RectTransform>()); return; }
            }

            var scrollGO = new GameObject("RowsScrollView",
                typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(panelRoot.transform, false);
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            var bg = scrollGO.GetComponent<Image>();
            bg.color = new Color(0.05f, 0.07f, 0.10f, 1f);
            bg.raycastTarget = true;
            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewportGO = new GameObject("Viewport",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.sizeDelta = Vector2.zero;
            viewportRT.anchoredPosition = Vector2.zero;
            viewportRT.pivot = new Vector2(0f, 1f);
            var viewportImage = viewportGO.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.004f);
            var mask = viewportGO.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGO = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = Vector2.zero;

            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = contentGO.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRT;
            scrollRect.content = contentRT;
            rowsContainer = contentRT;

            StretchScroll(scrollRT);
        }

        private static void StretchScroll(RectTransform scrollRT)
        {
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.pivot = new Vector2(0.5f, 0.5f);
            scrollRT.anchoredPosition = Vector2.zero;
            scrollRT.sizeDelta = Vector2.zero;
            scrollRT.offsetMin = new Vector2(80f, 160f);
            scrollRT.offsetMax = new Vector2(-80f, -170f);
        }

        private static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax,
            Vector2 pivot, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }
    }
}
