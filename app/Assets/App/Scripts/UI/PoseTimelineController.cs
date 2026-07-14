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
            bool interactive = next is AppState.TrackingLoop or AppState.InspectingPose;
            if (timelineSlider != null)
                timelineSlider.interactable = interactive;

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
            _isDragging = true;
            _stateMgr?.TransitionTo(AppState.InspectingPose);
            movementController?.BeginInspect(timelineSlider?.value ?? 0f);

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
        }

        // ── Marker management ─────────────────────────────────────────

        private void BuildMarkers(List<KeyPoseData> poses, Color accentColor)
        {
            if (markerContainer == null || markerPrefab == null || poses == null) return;

            float trackWidth = markerContainer.rect.width;

            foreach (var pose in poses)
            {
                GameObject dot = Instantiate(markerPrefab, markerContainer);
                var rt = dot.GetComponent<RectTransform>();
                if (rt != null)
                {
                    float xPos = Mathf.Lerp(0f, trackWidth, pose.normalizedTime);
                    rt.anchoredPosition = new Vector2(xPos, 0f);
                }

                // Apply category color
                var img = dot.GetComponent<Image>();
                if (img != null)
                    img.color = accentColor;

                _markers.Add(dot);
            }
        }

        private void ClearMarkers()
        {
            foreach (var m in _markers)
                if (m != null) Destroy(m);
            _markers.Clear();
        }

        // ── Helper ────────────────────────────────────────────────────

        private bool IsInteractive() =>
            _stateMgr != null &&
            _stateMgr.IsAny(AppState.TrackingLoop, AppState.InspectingPose);
    }
}
