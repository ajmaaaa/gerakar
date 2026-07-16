using UnityEngine;

namespace GerakAR.UI
{
    /// <summary>
    /// Animates the green laser scan line inside the G03 scan guide frame.
    /// Ping-pongs vertically between top and bottom boundaries.
    /// </summary>
    public class LaserLineAnimator : MonoBehaviour
    {
        private RectTransform _rt;
        private float _direction = -1f; // Start moving downwards
        
        [SerializeField] private float speed = 200f; // Speed of the scan line
        [SerializeField] private float limitY = 100f; // Limit Y coordinate relative to center

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (_rt != null)
                _rt.anchoredPosition = new Vector2(0f, limitY); // Start at top
        }

        private void Update()
        {
            if (_rt == null) return;

            Vector2 pos = _rt.anchoredPosition;
            pos.y += _direction * speed * Time.deltaTime;

            if (pos.y >= limitY)
            {
                pos.y = limitY;
                _direction = -1f;
            }
            else if (pos.y <= -limitY)
            {
                pos.y = -limitY;
                _direction = 1f;
            }

            _rt.anchoredPosition = pos;
        }
    }
}
