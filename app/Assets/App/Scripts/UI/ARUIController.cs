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

        // ── Private ───────────────────────────────────────────────────

        private AppStateManager _stateMgr;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Start()
        {
            _stateMgr = AppStateManager.Instance;

            AppStateManager.OnStateChanged += OnStateChanged;
            GerakAREvents.OnMovementDetected += OnMovementDetected;

            // Wire buttons
            closeButton?.onClick.AddListener(OnClosePressed);
            materialButton?.onClick.AddListener(OnMaterialPressed);

            // Initial state: show scan overlay, hide detection toast
            SetActive(detectionToast, false);
            ApplyState(AppState.Scanning);
        }

        private void OnDestroy()
        {
            AppStateManager.OnStateChanged -= OnStateChanged;
            GerakAREvents.OnMovementDetected -= OnMovementDetected;
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
        }

        private void OnMovementDetected(string movementId)
        {
            // The label is updated separately; just ensure controls are shown
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
            GerakAREvents.RaiseMaterialOpened(_stateMgr?.ActiveId ?? string.Empty);
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
