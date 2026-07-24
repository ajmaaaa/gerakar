// ============================================================
// MotionLearn – PoseTimelineController.cs
// Thin horizontal slider that controls the MovementController
// pose scrubbing. Touch → InspectingPose. Release → EndInspect.
// Shows key-pose markers and a hint label while dragging.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using MotionLearn.Core;
using MotionLearn.Content;
using MotionLearn.Animation;

namespace MotionLearn.UI
{
    /// <summary>
    /// Wraps a UI Slider to drive <see cref="MovementController"/>.
    ///
    /// Key pose markers are small dots placed at <c>normalizedTime</c>
    /// positions along the timeline track. They are recreated whenever
    /// <see cref="SetMovementData"/> is called.
    ///
    /// The hint text "Lepaskan untuk melanjutkan gerakan" is shown while
    /// the user's finger is on the handle and hidden otherwise.
    /// </summary>
    public class PoseTimelineController : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        // ── Inspector ─────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Slider timelineSlider;
        [SerializeField] private MovementController movementController;

        [Header("Key Pose Markers")]
        [SerializeField] private RectTransform markerContainer;
        [SerializeField] private GameObject markerPrefab;         // Small dot prefab (Image)

        [Header("Hint")]
        [SerializeField] private TextMeshProUGUI hintText;
        private const string HintMessage = "Lepaskan untuk melanjutkan gerakan";

        // ── Private state ─────────────────────────────────────────────

        private AppStateManager _stateMgr;
        private bool _isDragging;
        private readonly List<GameObject> _markers = new();
        private readonly List<float> _markerTimes = new();
        private readonly List<float> _poseTimes = new();
        private Color _accentColor = Color.gray;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Awake()
        {
            if (timelineSlider != null)
            {
                timelineSlider.minValue = 0f;
                timelineSlider.maxValue = 1f;
                timelineSlider.wholeNumbers = false;
                // Remove default onValueChanged; we drive the controller manually
                timelineSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            if (hintText != null)
                hintText.gameObject.SetActive(false);

            AlignMarkersWithHandleRange();
        }

        private void Start()
        {
            _stateMgr = AppStateManager.Instance;
            AppStateManager.OnStateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            AppStateManager.OnStateChanged -= OnStateChanged;
        }

        // ── Public API ────────────────────────────────────────────────

        /// <summary>
        /// Called when a new movement target is tracked. Rebuilds markers.
        /// </summary>
        public void SetMovementData(MovementData data)
        {
            ClearMarkers();
            if (data == null) return;
            BuildMarkers(data.keyPoses, data.categoryColor);

            // Reset slider to start
            if (timelineSlider != null)
                timelineSlider.SetValueWithoutNotify(0f);
            UpdateMarkerColors(0f);
        }

        // ── State change ──────────────────────────────────────────────

        private void OnStateChanged(AppState prev, AppState next)
        {
            // Disable interaction when material is open or tracking lost
            bool interactive = next is AppState.TrackingLoop or AppState.InspectingPose or AppState.NonARMovementPlayer;
            if (timelineSlider != null)
                timelineSlider.interactable = interactive && movementController != null && movementController.CanInspect;

            if (!interactive && _isDragging)
            {
                // Force-end inspect if state was externally changed
                _isDragging = false;
                hintText?.gameObject.SetActive(false);
            }

            if (prev == AppState.InspectingPose &&
                (next == AppState.TrackingLoop || next == AppState.NonARMovementPlayer))
            {
                timelineSlider?.SetValueWithoutNotify(0f);
                UpdateMarkerColors(0f);
            }
        }

        // ── Pointer / drag events ─────────────────────────────────────

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractive()) return;
            AppState returnState = _stateMgr.Is(AppState.NonARMovementPlayer)
                ? AppState.NonARMovementPlayer
                : AppState.TrackingLoop;
            _isDragging = true;
            _stateMgr?.TransitionTo(AppState.InspectingPose);
            movementController?.BeginInspect(SliderToAnimationTime(timelineSlider?.value ?? 0f), returnState);

            if (hintText != null)
            {
                hintText.text = HintMessage;
                hintText.gameObject.SetActive(true);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || timelineSlider == null) return;
            // Slider handles its own value update; we mirror it to the controller
            movementController?.ScrubTo(SliderToAnimationTime(timelineSlider.value));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            if (hintText != null)
                hintText.gameObject.SetActive(false);

            movementController?.EndInspect();
        }

        // ── Slider value callback ─────────────────────────────────────

        private void OnSliderValueChanged(float value)
        {
            if (_isDragging)
                movementController?.ScrubTo(SliderToAnimationTime(value));
            UpdateMarkerColors(value);
        }

        // ── Marker management ─────────────────────────────────────────

        private void UpdateMarkerColors(float sliderValue)
        {
            for (int i = 0; i < _markers.Count; i++)
            {
                if (_markers[i] == null) continue;
                var img = _markers[i].GetComponent<Image>();
                if (img != null)
                {
                    float t = _markerTimes[i];
                    // Active/Passed nodes: Pure White for crisp high-contrast visibility on dark green fill line!
                    // Upcoming nodes: Forest Green on light sage track background!
                    img.color = (t <= sliderValue)
                        ? Color.white
                        : new Color(0.09f, 0.40f, 0.20f, 1.0f);
                }
            }
        }

        private void BuildMarkers(List<KeyPoseData> poses, Color accentColor)
        {
            if (markerContainer == null || markerPrefab == null || poses == null) return;
            AlignMarkersWithHandleRange();
            _accentColor = accentColor;

            for (int i = 0; i < poses.Count; i++)
            {
                KeyPoseData pose = poses[i];
                float markerTime = poses.Count > 1 ? i / (float)(poses.Count - 1) : 0f;
                GameObject dot = Instantiate(markerPrefab, markerContainer);
                dot.transform.localScale = Vector3.one;
                var img = dot.GetComponent<Image>();
                if (img != null)
                {
#if UNITY_EDITOR
                    img.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/Circle-24.png");
#endif
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                }
                var rt = dot.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(markerTime, 0.5f);
                    rt.anchorMax = new Vector2(markerTime, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(10f, 10f); // 10px crisp circle node
                }

                _markers.Add(dot);
                _markerTimes.Add(markerTime);
                _poseTimes.Add(Mathf.Clamp01(pose.normalizedTime));
            }
            UpdateMarkerColors(timelineSlider != null ? timelineSlider.value : 0f);
        }

        private void ClearMarkers()
        {
            foreach (var m in _markers)
                if (m != null) Destroy(m);
            _markers.Clear();
            _markerTimes.Clear();
            _poseTimes.Clear();
        }

        private void AlignMarkersWithHandleRange()
        {
            if (markerContainer == null)
                return;

            RectTransform mcRT = markerContainer as RectTransform;
            if (mcRT != null)
            {
                mcRT.anchorMin = new Vector2(0f, 0f);
                mcRT.anchorMax = new Vector2(1f, 1f);
                mcRT.pivot = new Vector2(0.5f, 0.5f);
                mcRT.offsetMin = new Vector2(12f, 0f); // Inset 12px from left matching handle knob center
                mcRT.offsetMax = new Vector2(-12f, 0f); // Inset 12px from right matching handle knob center
            }
        }

        private float SliderToAnimationTime(float sliderValue)
        {
            if (_poseTimes.Count < 2)
                return Mathf.Clamp01(sliderValue);

            float scaled = Mathf.Clamp01(sliderValue) * (_poseTimes.Count - 1);
            int startIndex = Mathf.Min(Mathf.FloorToInt(scaled), _poseTimes.Count - 2);
            return Mathf.Lerp(_poseTimes[startIndex], _poseTimes[startIndex + 1], scaled - startIndex);
        }

        // ── Helper ────────────────────────────────────────────────────

        private bool IsInteractive() =>
            _stateMgr != null &&
            movementController != null && movementController.CanInspect &&
            _stateMgr.IsAny(AppState.TrackingLoop, AppState.InspectingPose, AppState.NonARMovementPlayer);
    }
}
