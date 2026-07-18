using UnityEngine;

namespace GerakAR.UI
{
    /// <summary>
    /// Adjusts the anchors of its RectTransform to match the screen's safe area (notches).
    /// Used for responsive layout container.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaController : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea = new Rect(0, 0, 0, 0);
        private Vector2Int _lastScreenSize = new Vector2Int(0, 0);
        private ScreenOrientation _lastOrientation = ScreenOrientation.Unknown;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea ||
                Screen.width != _lastScreenSize.x ||
                Screen.height != _lastScreenSize.y ||
                Screen.orientation != _lastOrientation)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrientation = Screen.orientation;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            if (screenWidth <= 0f || screenHeight <= 0f) return;

            Vector2 anchorMin = _lastSafeArea.position;
            Vector2 anchorMax = _lastSafeArea.position + _lastSafeArea.size;

            anchorMin.x /= screenWidth;
            anchorMin.y /= screenHeight;
            anchorMax.x /= screenWidth;
            anchorMax.y /= screenHeight;

            // Clamp anchors to valid ranges
            anchorMin.x = Mathf.Clamp01(anchorMin.x);
            anchorMin.y = Mathf.Clamp01(anchorMin.y);
            anchorMax.x = Mathf.Clamp01(anchorMax.x);
            anchorMax.y = Mathf.Clamp01(anchorMax.y);

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.anchoredPosition = Vector2.zero;
            _rectTransform.sizeDelta = Vector2.zero;
        }
    }
}
