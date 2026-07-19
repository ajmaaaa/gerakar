using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GerakAR.UI
{
    public sealed class HorizontalCardSnapController : MonoBehaviour, IEndDragHandler
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private float cardWidth = 154f;
        [SerializeField] private float spacing = 12f;

        public void Configure(ScrollRect target, float width, float gap)
        {
            scrollRect = target;
            cardWidth = width;
            spacing = gap;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null)
                return;

            Canvas.ForceUpdateCanvases();
            float maximumOffset = Mathf.Max(0f, scrollRect.content.rect.width - scrollRect.viewport.rect.width);
            float step = cardWidth + spacing;
            float currentOffset = Mathf.Clamp(-scrollRect.content.anchoredPosition.x, 0f, maximumOffset);
            float targetOffset = Mathf.Clamp(Mathf.Round(currentOffset / step) * step, 0f, maximumOffset);

            scrollRect.StopMovement();
            Vector2 position = scrollRect.content.anchoredPosition;
            position.x = -targetOffset;
            scrollRect.content.anchoredPosition = position;
        }
    }
}
