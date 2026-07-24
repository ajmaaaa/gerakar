using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MotionLearn.UI
{
    public class CollapsibleWarningToggle : MonoBehaviour
    {
        [SerializeField] private GameObject expandedContent;
        [SerializeField] private GameObject supportingContent; // e.g. "Ketuk untuk melihat penjelasan"
        [SerializeField] private Image chevronImage;
        [SerializeField] private Sprite chevronDownSprite;
        [SerializeField] private Sprite chevronUpSprite;
        
        [SerializeField] private float collapsedHeight = 48f;
        [SerializeField] private float expandedHeight = 112f;
        [SerializeField] private float duration = 0.22f;

        private bool _isExpanded;
        private Button _button;
        private RectTransform _rectTransform;
        private Coroutine _toggleCoroutine;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _button = GetComponent<Button>();
            if (_button != null)
                _button.onClick.AddListener(ToggleExpand);

            if (expandedContent != null)
                expandedContent.SetActive(false);
            if (supportingContent != null)
                supportingContent.SetActive(true);
            
            if (_rectTransform != null)
                _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, collapsedHeight);
        }

        private void ToggleExpand()
        {
            _isExpanded = !_isExpanded;
            
            if (chevronImage != null)
                chevronImage.sprite = _isExpanded ? chevronUpSprite : chevronDownSprite;

            if (_toggleCoroutine != null)
                StopCoroutine(_toggleCoroutine);

            _toggleCoroutine = StartCoroutine(AnimateHeight());
        }

        private IEnumerator AnimateHeight()
        {
            float elapsed = 0f;
            float startHeight = _rectTransform.sizeDelta.y;
            float targetHeight = _isExpanded ? expandedHeight : collapsedHeight;

            if (_isExpanded)
            {
                if (expandedContent != null) expandedContent.SetActive(true);
                if (supportingContent != null) supportingContent.SetActive(false);
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float h = Mathf.Lerp(startHeight, targetHeight, Mathf.SmoothStep(0f, 1f, t));
                if (_rectTransform != null)
                    _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, h);
                yield return null;
            }

            if (_rectTransform != null)
                _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, targetHeight);

            if (!_isExpanded)
            {
                if (expandedContent != null) expandedContent.SetActive(false);
                if (supportingContent != null) supportingContent.SetActive(true);
            }

            _toggleCoroutine = null;
        }
    }
}
