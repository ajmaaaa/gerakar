using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GerakAR.UI
{
    public sealed class NestedScrollRouter : MonoBehaviour,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private ScrollRect parentScrollRect;

        private ScrollRect _nestedScrollRect;
        private Vector2 _pressPosition;
        private bool _routeToParent;

        private void Awake()
        {
            _nestedScrollRect = GetComponent<ScrollRect>();
        }

        public void SetParentScrollRect(ScrollRect scrollRect)
        {
            parentScrollRect = scrollRect;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            _pressPosition = eventData.position;
            _routeToParent = false;
            parentScrollRect?.OnInitializePotentialDrag(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Vector2 drag = eventData.position - _pressPosition;
            _routeToParent = parentScrollRect != null && Mathf.Abs(drag.y) > Mathf.Abs(drag.x);
            if (_routeToParent)
            {
                if (_nestedScrollRect != null)
                    _nestedScrollRect.horizontal = false;
                parentScrollRect?.OnBeginDrag(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_routeToParent)
                parentScrollRect?.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_routeToParent)
                parentScrollRect?.OnEndDrag(eventData);
            if (_nestedScrollRect != null)
                _nestedScrollRect.horizontal = true;
            _routeToParent = false;
        }

        private void OnDisable()
        {
            if (_nestedScrollRect != null)
                _nestedScrollRect.horizontal = true;
            _routeToParent = false;
        }
    }
}
