using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GerakAR.Content;

namespace GerakAR.UI
{
    public static class RelatedMovementCardView
    {
        private static readonly Color WarmWhite = new Color32(255, 254, 250, 255);
        private static readonly Color DeepForest = new Color32(18, 55, 42, 255);
        private static readonly Color MossGreen = new Color32(96, 125, 79, 255);
        private static readonly Color SoftSage = new Color32(169, 190, 162, 255);

        public static void ConfigureContainer(Transform container)
        {
            if (container == null)
                return;

            HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(0, 12, 4, 8);
                layout.spacing = 12f;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            RectTransform contentRect = container as RectTransform;
            if (contentRect != null)
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, 160f);

            Transform scrollView = container.parent != null ? container.parent.parent : null;
            if (scrollView == null)
                return;

            RectTransform scrollRect = scrollView as RectTransform;
            if (scrollRect != null)
                scrollRect.sizeDelta = new Vector2(scrollRect.sizeDelta.x, 160f);

            LayoutElement scrollLayout = scrollView.GetComponent<LayoutElement>();
            if (scrollLayout == null)
                scrollLayout = scrollView.gameObject.AddComponent<LayoutElement>();
            scrollLayout.minHeight = 160f;
            scrollLayout.preferredHeight = 160f;
        }

        public static void Configure(GameObject card, RelatedMovementData data)
        {
            if (card == null)
                return;

            RectTransform cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.localScale = Vector3.one;
                cardRect.sizeDelta = new Vector2(148f, 148f);
            }

            LayoutElement cardLayout = card.GetComponent<LayoutElement>();
            if (cardLayout == null)
                cardLayout = card.AddComponent<LayoutElement>();
            cardLayout.minWidth = 148f;
            cardLayout.preferredWidth = 148f;
            cardLayout.minHeight = 148f;
            cardLayout.preferredHeight = 148f;
            cardLayout.flexibleWidth = 0f;
            cardLayout.flexibleHeight = 0f;

            Image background = card.GetComponent<Image>();
            if (background != null)
                background.color = WarmWhite;

            Outline outline = card.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(SoftSage.r, SoftSage.g, SoftSage.b, 0.55f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            Shadow shadow = null;
            foreach (Shadow candidate in card.GetComponents<Shadow>())
            {
                if (candidate is Outline)
                    continue;
                shadow = candidate;
                break;
            }
            if (shadow != null)
            {
                shadow.effectColor = new Color(0f, 0f, 0f, 0.09f);
                shadow.effectDistance = new Vector2(0f, -3f);
            }

            ConfigureThumbnail(card.transform.Find("Thumbnail"), data);
            TextMeshProUGUI title = ConfigureTitle(card.transform.Find("Title"), data);
            ConfigureEyebrow(card.transform, title);
            ConfigureAccent(card.transform);
        }

        private static void ConfigureThumbnail(Transform thumbnailTransform, RelatedMovementData data)
        {
            if (thumbnailTransform == null)
                return;

            RectTransform rect = thumbnailTransform as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -10f);
            rect.sizeDelta = new Vector2(-20f, 78f);

            Image image = thumbnailTransform.GetComponent<Image>();
            if (image == null)
                return;
            image.raycastTarget = false;
            image.preserveAspect = true;
            if (data != null && data.thumbnail != null)
            {
                image.sprite = data.thumbnail;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(SoftSage.r, SoftSage.g, SoftSage.b, 0.48f);
            }
        }

        private static TextMeshProUGUI ConfigureTitle(Transform titleTransform, RelatedMovementData data)
        {
            if (titleTransform == null)
                return null;

            RectTransform rect = titleTransform as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(5f, -111f);
            rect.sizeDelta = new Vector2(-30f, 29f);

            TextMeshProUGUI title = titleTransform.GetComponent<TextMeshProUGUI>();
            if (title == null)
                return null;
            title.text = data != null ? data.title : "Gerakan Terkait";
            title.fontSize = 11f;
            title.fontStyle = FontStyles.Bold;
            title.color = DeepForest;
            title.alignment = TextAlignmentOptions.TopLeft;
            title.textWrappingMode = TextWrappingModes.Normal;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.raycastTarget = false;
            return title;
        }

        private static void ConfigureEyebrow(Transform card, TextMeshProUGUI title)
        {
            Transform eyebrowTransform = card.Find("Eyebrow");
            if (eyebrowTransform == null)
            {
                var eyebrowObject = new GameObject("Eyebrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                eyebrowTransform = eyebrowObject.transform;
                eyebrowTransform.SetParent(card, false);
            }

            RectTransform rect = eyebrowTransform as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(5f, -95f);
            rect.sizeDelta = new Vector2(-30f, 12f);

            TextMeshProUGUI eyebrow = eyebrowTransform.GetComponent<TextMeshProUGUI>();
            eyebrow.text = "LATIHAN TERKAIT";
            eyebrow.font = title != null ? title.font : eyebrow.font;
            eyebrow.fontSize = 7.5f;
            eyebrow.fontStyle = FontStyles.Bold;
            eyebrow.color = MossGreen;
            eyebrow.alignment = TextAlignmentOptions.Left;
            eyebrow.textWrappingMode = TextWrappingModes.NoWrap;
            eyebrow.raycastTarget = false;
        }

        private static void ConfigureAccent(Transform card)
        {
            Transform accentTransform = card.Find("Accent");
            if (accentTransform == null)
            {
                var accentObject = new GameObject("Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                accentTransform = accentObject.transform;
                accentTransform.SetParent(card, false);
            }

            RectTransform rect = accentTransform as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(10f, -97f);
            rect.sizeDelta = new Vector2(3f, 39f);

            Image accent = accentTransform.GetComponent<Image>();
            accent.color = MossGreen;
            accent.raycastTarget = false;
        }
    }
}
