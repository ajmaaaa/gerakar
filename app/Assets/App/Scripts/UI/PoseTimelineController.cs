// ============================================================
// GerakAR – PoseTimelineController.cs
// Thin horizontal slider that controls the MovementController
// pose scrubbing. Touch → InspectingPose. Release → EndInspect.
// Shows key-pose markers and a hint label while dragging.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using GerakAR.Core;
using GerakAR.Content;
using GerakAR.Animation;

namespace GerakAR.UI
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
        }

        private void Start()
        {
            _stateMgr = AppStateManager.Instance;
            AppStateManager.OnStateChanged += OnStateChanged;
        }

        private void Update()
        {
            if (!_isDragging && movementController != null && movementController.CanInspect)
            {
                float t = movementController.CurrentNormalizedTime;
                if (timelineSlider != null)
                {
                    timelineSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
                    timelineSlider.value = t;
                    timelineSlider.onValueChanged.AddListener(OnSliderValueChanged);
                }
                UpdateMarkerColors(t);
            }
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
                timelineSlider.value = 0f;
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
            movementController?.BeginInspect(timelineSlider?.value ?? 0f, returnState);

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
            movementController?.ScrubTo(timelineSlider.value);
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
                movementController?.ScrubTo(value);
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
                    img.color = (t <= sliderValue) ? GerakARTheme.Primary : _accentColor;
                }
            }
        }

        private void BuildMarkers(List<KeyPoseData> poses, Color accentColor)
        {
            if (markerContainer == null || markerPrefab == null || poses == null) return;
            _accentColor = accentColor;

            foreach (var pose in poses)
            {
                GameObject dot = Instantiate(markerPrefab, markerContainer);
                var rt = dot.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(pose.normalizedTime, 0.5f);
                    rt.anchorMax = new Vector2(pose.normalizedTime, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(12f, 12f);
                }

                _markers.Add(dot);
                _markerTimes.Add(pose.normalizedTime);
            }
            UpdateMarkerColors(timelineSlider != null ? timelineSlider.value : 0f);
        }

        private void ClearMarkers()
        {
            foreach (var m in _markers)
                if (m != null) Destroy(m);
            _markers.Clear();
            _markerTimes.Clear();
        }

        // ── Helper ────────────────────────────────────────────────────

        private bool IsInteractive() =>
            _stateMgr != null &&
            movementController != null && movementController.CanInspect &&
            _stateMgr.IsAny(AppState.TrackingLoop, AppState.InspectingPose, AppState.NonARMovementPlayer);
    }
}
