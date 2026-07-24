using UnityEngine;

namespace MotionLearn.UI
{
    public class LaserLineAnimator : MonoBehaviour
    {
        private RectTransform _rt;
        private float _startY;
        private float _endY;
        private float _elapsed;
        private bool _isPlaying;

        [SerializeField] private float speed = 250f;
        [SerializeField] public float limitY = 100f;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            _rt = GetComponent<RectTransform>();
            _startY = limitY;
            _endY = -limitY;
            _elapsed = 0f;
            _isPlaying = true;
            if (_rt != null)
                _rt.anchoredPosition = new Vector2(0f, _startY);
        }

        private void Update()
        {
            if (!_isPlaying || _rt == null) return;

            _elapsed += Time.deltaTime;
            float distance = Mathf.Abs(_startY - _endY);
            float duration = distance / speed;
            float t = Mathf.Clamp01(_elapsed / duration);

            _rt.anchoredPosition = new Vector2(0f, Mathf.Lerp(_startY, _endY, t));

            if (t >= 1f)
            {
                _isPlaying = false;
                gameObject.SetActive(false);
            }
        }
    }
}
