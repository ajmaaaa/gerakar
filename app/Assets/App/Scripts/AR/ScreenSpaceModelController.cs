using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace GerakAR.AR
{
    public sealed class ScreenSpaceModelController : MonoBehaviour
    {
        [SerializeField] private float defaultDistance = 0.27f;
        [SerializeField] private float minimumDistance = 0.16f;
        [SerializeField] private float maximumDistance = 1.1f;
        [SerializeField] private float rotationSensitivity = 0.18f;
        [SerializeField] private float pinchSensitivity = 0.0015f;
        [SerializeField] [Range(20f, 70f)] private float maximumPitch = 45f;
        [SerializeField] private float portraitRollCorrection = 90f;

        private bool _interactionEnabled;
        private float _distance;
        private float _pitch;
        private float _yaw;

        private void Awake()
        {
            EnhancedTouchSupport.Enable();
            ResetView();
        }

        private void OnDestroy()
        {
            if (EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Disable();
        }

        public void SetInteractionEnabled(bool enabled)
        {
            _interactionEnabled = enabled;
        }

        public void ResetView()
        {
            _distance = defaultDistance;
            _pitch = 0f;
            _yaw = 0f;
            ApplyView();
        }

        private void Update()
        {
            if (!_interactionEnabled)
                return;

            var touches = Touch.activeTouches;
            if (touches.Count == 1)
            {
                Touch touch = touches[0];
                if (IsOverUI(touch))
                    return;

                Vector2 delta = touch.delta;
                // Move the model in the same screen direction as the drag while
                // keeping pitch far enough from 90 degrees to prevent a sideways flip.
                _yaw -= delta.x * rotationSensitivity;
                _pitch = Mathf.Clamp(_pitch + delta.y * rotationSensitivity, -maximumPitch, maximumPitch);
                ApplyView();
            }
            else if (touches.Count >= 2)
            {
                Touch first = touches[0];
                Touch second = touches[1];
                if (IsOverUI(first) || IsOverUI(second))
                    return;

                float currentDistance = Vector2.Distance(first.screenPosition, second.screenPosition);
                float previousDistance = Vector2.Distance(
                    first.screenPosition - first.delta,
                    second.screenPosition - second.delta);
                _distance = Mathf.Clamp(
                    _distance - (currentDistance - previousDistance) * pinchSensitivity,
                    minimumDistance,
                    maximumDistance);
                ApplyView();
            }
        }

        private void ApplyView()
        {
            transform.localPosition = new Vector3(0f, 0f, _distance);
            Quaternion correction = Quaternion.AngleAxis(portraitRollCorrection, Vector3.forward);
            Vector3 correctedUp = correction * Vector3.up;
            Vector3 correctedRight = correction * Vector3.right;
            Quaternion yawRotation = Quaternion.AngleAxis(_yaw, correctedUp);
            Quaternion pitchRotation = Quaternion.AngleAxis(_pitch, yawRotation * correctedRight);
            transform.localRotation = pitchRotation * yawRotation * correction;
        }

        private static bool IsOverUI(Touch touch)
        {
            return EventSystem.current != null &&
                   EventSystem.current.IsPointerOverGameObject(touch.touchId);
        }
    }
}
