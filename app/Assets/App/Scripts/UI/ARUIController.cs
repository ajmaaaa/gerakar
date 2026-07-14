// ============================================================
// GerakAR – ARUIController.cs
// Manages visibility of all AR-screen UI elements based on
// AppState changes: scan overlay, movement label, timeline,
// and floating action buttons.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GerakAR.Core;
using GerakAR.Content;

namespace GerakAR.UI
{
    /// <summary>
    /// Listens to <see cref="AppStateManager.OnStateChanged"/> and
    /// <see cref="GerakAREvents"/> to show/hide the correct UI elements
    /// for each state. No game logic lives here – only visibility control.
    ///
    /// Wiring (Inspector):
    ///   scanOverlay      → Panel with scan frame + hint text
    ///   arControls       → Group holding label + timeline + FABs
    ///   movementLabel    → TMP text component for movement name
    ///   closeButton      → "×" floating action button
    ///   materialButton   → "📖" floating action button
    /// </summary>
    public class ARUIController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────

        [Header("Scan Overlay")]
        [SerializeField] private GameObject scanOverlay;

        [Header("Detection Toast (green checkmark)")]
        [SerializeField] private GameObject detectionToast;

        [Header("AR Controls (shown when tracking)")]
        [SerializeField] private GameObject arControls;
        [SerializeField] private TextMeshProUGUI movementNameLabel;

        [Header("Floating Action Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button materialButton;

        [Header("Timeline")]
        [SerializeField] private GameObject timelineRoot;
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Image playPauseIcon;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Sprite pauseSprite;

        // ── Private ───────────────────────────────────────────────────

        private AppStateManager _stateMgr;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Start()
        {
            _stateMgr = AppStateManager.Instance;

            AppStateManager.OnStateChanged += OnStateChanged;
            GerakAREvents.OnMovementDetected += OnMovementDetected;
            GerakAREvents.OnLoopStarted += OnLoopStarted;

            // Wire buttons
            closeButton?.onClick.AddListener(OnClosePressed);
            materialButton?.onClick.AddListener(OnMaterialPressed);
            if (playPauseButton != null)
                playPauseButton.onClick.AddListener(OnPlayPausePressed);

            // Initial state: show scan overlay, hide detection toast
            SetActive(detectionToast, false);
            ApplyState(AppState.Scanning);
        }

        private void OnDestroy()
        {
            AppStateManager.OnStateChanged -= OnStateChanged;
            GerakAREvents.OnMovementDetected -= OnMovementDetected;
            GerakAREvents.OnLoopStarted -= OnLoopStarted;
        }

        // ── State changes ─────────────────────────────────────────────

        private void OnStateChanged(AppState prev, AppState next) => ApplyState(next);

        private void ApplyState(AppState state)
        {
            bool scanning = state == AppState.Scanning || state == AppState.TrackingLost;
            bool detecting = state == AppState.Detecting;
            bool tracking = state is AppState.TrackingLoop or AppState.InspectingPose;
            bool showMaterial = state == AppState.ShowingMaterial;

            SetActive(scanOverlay, scanning);
            SetActive(detectionToast, detecting);
            SetActive(arControls, tracking || showMaterial);

            // Timeline: only when tracking, not when material is open
            SetActive(timelineRoot, tracking);

            // FABs: only when tracking or material open
            bool fabsVisible = tracking || showMaterial;
            SetActive(closeButton?.gameObject, fabsVisible);
            SetActive(materialButton?.gameObject, fabsVisible && !showMaterial);

            // Synchronize Play/Pause icon state
            UpdatePlayPauseUI();
        }

        private void OnMovementDetected(string movementId)
        {
            // The label is updated separately; just ensure controls are shown
        }

        private void OnLoopStarted(string movementId)
        {
            UpdatePlayPauseUI();
        }

        // ── Label update ──────────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="ARImageTrackingController"/> (or a bridge)
        /// when a new movement is detected, to update the name label.
        /// </summary>
        public void SetMovementName(string displayName)
        {
            if (movementNameLabel != null)
                movementNameLabel.text = displayName;
        }

        // ── Button handlers ───────────────────────────────────────────

        private void OnClosePressed()
        {
            _stateMgr?.TransitionTo(AppState.Scanning);
        }

        private void OnMaterialPressed()
        {
            _stateMgr?.TransitionTo(AppState.ShowingMaterial);
            string activeId = _stateMgr != null ? _stateMgr.ActiveId() : string.Empty;
            GerakAREvents.RaiseMaterialOpened(activeId);
        }

        private void OnPlayPausePressed()
        {
            if (Audio.AudioGuideController.Instance != null)
            {
                Audio.AudioGuideController.Instance.TogglePlayPause();
                
                // Mirror to animation controller
                var movementController = FindAnyObjectByType<Animation.MovementController>();
                if (movementController != null)
                {
                    movementController.SetLoopPaused(!Audio.AudioGuideController.Instance.IsPlaying);
                }

                UpdatePlayPauseUI();
            }
        }

        private void UpdatePlayPauseUI()
        {
            if (playPauseIcon == null) return;

            bool isPlaying = Audio.AudioGuideController.Instance != null && Audio.AudioGuideController.Instance.IsPlaying;
            playPauseIcon.sprite = isPlaying ? pauseSprite : playSprite;
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }
    }

    // Tiny extension to read active movement id without coupling to ModelPool
    internal static class AppStateManagerArUIExt
    {
        public static string ActiveId(this AppStateManager mgr) => null; // overridden at runtime
    }
}
