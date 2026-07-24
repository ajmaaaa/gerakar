using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MotionLearn.Content;

namespace MotionLearn.UI
{
    public static class RelatedMovementCardView
    {
        private static readonly Color WarmCream = new Color32(244, 240, 230, 255);
        private static readonly Color DeepForest = new Color32(18, 55, 42, 255);

        public static void ConfigureContainer(Transform container)
        {
            if (container == null)
                return;

            HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(20, 20, 4, 12);
                layout.spacing = 12f;
                layout.childAlignment = TextAnchor.UpperCenter; // Centered initial 2-card layout with equal 20px side margins!
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            RectTransform contentRect = container as RectTransform;
            if (contentRect != null)
            {
                contentRect.anchorMin = new Vector2(0f, 0.5f);
                contentRect.anchorMax = new Vector2(0f, 0.5f);
                contentRect.pivot = new Vector2(0f, 0.5f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, 180f);
            }

            Transform viewport = container.parent;
            if (viewport != null)
            {
                RectMask2D mask = viewport.GetComponent<RectMask2D>();
                if (mask != null)
                    mask.padding = new Vector4(-20f, 0f, -20f, 0f);
            }

            Transform scrollView = container.parent != null ? container.parent.parent : null;
            if (scrollView == null)
                return;

            RectTransform scrollRect = scrollView as RectTransform;
            if (scrollRect != null)
                scrollRect.sizeDelta = new Vector2(scrollRect.sizeDelta.x, 180f);

            LayoutElement scrollLayout = scrollView.GetComponent<LayoutElement>();
            if (scrollLayout == null)
                scrollLayout = scrollView.gameObject.AddComponent<LayoutElement>();
            scrollLayout.minHeight = 180f;
            scrollLayout.preferredHeight = 180f;

            ScrollRect horizontalScroll = scrollView.GetComponent<ScrollRect>();
            if (horizontalScroll == null)
                return;

            horizontalScroll.horizontalNormalizedPosition = 0f;

            var snap = scrollView.GetComponent<HorizontalCardSnapController>();
            if (snap == null)
                snap = scrollView.gameObject.AddComponent<HorizontalCardSnapController>();
            snap.Configure(horizontalScroll, 154f, 12f);

            ScrollRect parentScroll = FindParentScrollRect(scrollView.parent);
            if (parentScroll != null)
            {
                var router = scrollView.GetComponent<NestedScrollRouter>();
                if (router == null)
                    router = scrollView.gameObject.AddComponent<NestedScrollRouter>();
                router.SetParentScrollRect(parentScroll);
            }
        }

        public static void Configure(GameObject card, RelatedMovementData data)
        {
            if (card == null)
                return;

            RectTransform cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.localScale = Vector3.one;
                cardRect.sizeDelta = new Vector2(154f, 168f);
            }

            LayoutElement cardLayout = card.GetComponent<LayoutElement>();
            if (cardLayout == null)
                cardLayout = card.AddComponent<LayoutElement>();
            cardLayout.minWidth = 154f;
            cardLayout.preferredWidth = 154f;
            cardLayout.minHeight = 168f;
            cardLayout.preferredHeight = 168f;
            cardLayout.flexibleWidth = 0f;
            cardLayout.flexibleHeight = 0f;

            Image background = card.GetComponent<Image>();
            if (background != null)
                background.color = DeepForest;

            Outline outline = card.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;

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
                shadow.effectColor = new Color(0f, 0f, 0f, 0.14f);
                shadow.effectDistance = new Vector2(0f, -2f);
            }

            Transform thumbnail = FindDescendant(card.transform, "Thumbnail");
            ConfigureThumbnail(card.transform, thumbnail, background, data);
            ConfigureTitle(FindDescendant(card.transform, "Title"), data);
            SetOptionalDecorationActive(card.transform, "Eyebrow", false);
            SetOptionalDecorationActive(card.transform, "Accent", false);
        }

        private static void ConfigureThumbnail(
            Transform card,
            Transform thumbnailTransform,
            Image cardBackground,
            RelatedMovementData data)
        {
            if (thumbnailTransform == null)
                return;

            Transform frameTransform = card.Find("ThumbnailFrame");
            if (frameTransform == null)
            {
                var frameObject = new GameObject(
                    "ThumbnailFrame",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Mask));
                frameTransform = frameObject.transform;
                frameTransform.SetParent(card, false);
            }

            frameTransform.SetAsFirstSibling();
            RectTransform frameRect = frameTransform as RectTransform;
            frameRect.anchorMin = new Vector2(0f, 1f);
            frameRect.anchorMax = new Vector2(1f, 1f);
            frameRect.pivot = new Vector2(0.5f, 1f);
            frameRect.anchoredPosition = new Vector2(0f, -10f);
            frameRect.sizeDelta = new Vector2(-20f, 118f);

            Image frame = frameTransform.GetComponent<Image>();
            frame.sprite = cardBackground != null ? cardBackground.sprite : null;
            frame.type = frame.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            frame.color = WarmCream;
            frame.raycastTarget = false;

            Mask mask = frameTransform.GetComponent<Mask>();
            mask.showMaskGraphic = true;

            thumbnailTransform.SetParent(frameTransform, false);
            RectTransform rect = thumbnailTransform as RectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(-8f, -8f);

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
                image.color = WarmCream;
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
            rect.anchoredPosition = new Vector2(0f, -136f);
            rect.sizeDelta = new Vector2(-20f, 24f);

            TextMeshProUGUI title = titleTransform.GetComponent<TextMeshProUGUI>();
            if (title == null)
                return null;
            title.text = data != null ? data.title : "Gerakan Terkait";
            title.fontSize = 11.5f;
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;
            title.alignment = TextAlignmentOptions.Center;
            title.textWrappingMode = TextWrappingModes.Normal;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.raycastTarget = false;
            return title;
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null)
                return null;
            if (root.name == objectName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindDescendant(root.GetChild(i), objectName);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static ScrollRect FindParentScrollRect(Transform current)
        {
            while (current != null)
            {
                ScrollRect scrollRect = current.GetComponent<ScrollRect>();
                if (scrollRect != null && scrollRect.vertical)
                    return scrollRect;
                current = current.parent;
            }
            return null;
        }

        private static void SetOptionalDecorationActive(Transform card, string objectName, bool active)
        {
            Transform decoration = card.Find(objectName);
            if (decoration != null)
                decoration.gameObject.SetActive(active);
        }
    }
}
