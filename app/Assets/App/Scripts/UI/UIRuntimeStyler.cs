using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MotionLearn.UI
{
    public static class UIRuntimeStyler
    {
        private static readonly Color ForestGreen = new Color32(31, 93, 66, 255);
        private static readonly Color HeaderOutline = new Color32(18, 55, 42, 220);

        public static void EnsureHeaderContrast(Transform header)
        {
            if (header == null)
                return;

            foreach (TextMeshProUGUI text in header.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.name == "HeaderTitle" || text.name == "Title" || text.name == "BrandTitle")
                {
                    text.color = new Color(0.06f, 0.15f, 0.09f, 1.0f); // Deep Forest Green (#0C2314)
                }
                else if (text.name == "HeaderSubtitle" || text.name == "Subtitle" || text.name == "BrandSubtitle")
                {
                    text.color = new Color(0.09f, 0.40f, 0.20f, 1.0f); // Forest Green (#166534)
                }
                else
                {
                    text.color = new Color(0.06f, 0.15f, 0.09f, 1.0f);
                }

                Outline outline = text.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = false;
                }
            }
        }

        public static void NormalizeCloseButton(Button button)
        {
            if (button == null)
                return;

            RectTransform rootRect = button.transform as RectTransform;
            rootRect.sizeDelta = new Vector2(44f, 44f);

            Image rootImage = button.GetComponent<Image>();
            if (rootImage == null)
                return;

            Transform visualTransform = button.transform.Find("Visual");
            Image visualImage;
            if (visualTransform == null)
            {
                var visual = new GameObject("Visual", typeof(RectTransform), typeof(Image));
                visualTransform = visual.transform;
                visualTransform.SetParent(button.transform, false);
                visualImage = visual.GetComponent<Image>();
            }
            else
            {
                visualImage = visualTransform.GetComponent<Image>();
            }

            visualImage.sprite = rootImage.sprite;
            visualImage.type = rootImage.type;
            visualImage.color = ForestGreen;
            visualImage.raycastTarget = false;
            RectTransform visualRect = visualTransform as RectTransform;
            visualRect.anchorMin = new Vector2(0.5f, 0.5f);
            visualRect.anchorMax = new Vector2(0.5f, 0.5f);
            visualRect.pivot = new Vector2(0.5f, 0.5f);
            visualRect.anchoredPosition = Vector2.zero;
            visualRect.sizeDelta = new Vector2(36f, 36f);

            Transform icon = button.transform.Find("Icon") ?? visualTransform.Find("Icon");
            if (icon != null)
            {
                icon.SetParent(visualTransform, false);
                RectTransform iconRect = icon as RectTransform;
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;
                iconRect.sizeDelta = new Vector2(18f, 18f);
            }

            rootImage.color = Color.clear;
            rootImage.raycastTarget = true;
            button.targetGraphic = visualImage;
        }

        public static void NormalizeMuscleContainer(Transform container)
        {
            if (container == null)
                return;

            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                RectTransform rect = container as RectTransform;
                grid.enabled = true;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 1;
                grid.cellSize = new Vector2(Mathf.Max(280f, rect.rect.width), 52f);
                grid.spacing = new Vector2(0f, 8f);
                grid.childAlignment = TextAnchor.UpperLeft;
                return;
            }

            VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        public static void NormalizeMuscleItem(GameObject item)
        {
            if (item == null)
                return;

            LayoutElement layout = item.GetComponent<LayoutElement>();
            if (layout == null)
                layout = item.AddComponent<LayoutElement>();
            layout.minHeight = 52f;
            layout.preferredHeight = 52f;
            layout.flexibleWidth = 1f;

            TextMeshProUGUI text = item.transform.Find("Text")?.GetComponent<TextMeshProUGUI>() ??
                                   item.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.fontSize = 11f;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.alignment = TextAlignmentOptions.MidlineLeft;
            }
        }
    }
}
