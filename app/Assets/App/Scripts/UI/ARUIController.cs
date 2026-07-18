// ============================================================
// GerakAR – ARUIController.cs
// Manages visibility of all AR-screen UI elements based on
// AppState changes: scan overlay, movement label, timeline,
// and floating action buttons.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
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
    ///   closeButton      -> close floating action button
    ///   materialButton   -> material floating action button
    /// </summary>
    public class ARUIController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────

        [Header("Scan Overlay")]
        [SerializeField] private GameObject scanOverlay;
        [SerializeField] private GameObject scanLine;

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

        [Header("Background")]
        [SerializeField] private GameObject fullScreenBackground;
        [SerializeField] private CanvasGroup cameraReadyCover;

        // ── Private ───────────────────────────────────────────────────

        private AppStateManager _stateMgr;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Start()
        {
            _stateMgr = AppStateManager.Instance;

            AppStateManager.OnStateChanged += OnStateChanged;
            GerakAREvents.OnMovementDetected += OnMovementDetected;
            GerakAREvents.OnLoopStarted += OnLoopStarted;
            Audio.AudioGuideController.OnAudioAvailabilityChanged += OnAudioAvailabilityChanged;

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
            Audio.AudioGuideController.OnAudioAvailabilityChanged -= OnAudioAvailabilityChanged;
        }

        // ── State changes ─────────────────────────────────────────────

        private void OnStateChanged(AppState prev, AppState next) => ApplyState(next);

        private void ApplyState(AppState state)
        {
            bool scanning = state == AppState.Scanning || state == AppState.TrackingLost;
            bool detecting = state == AppState.TargetConfirmed;
            bool tracking = state is AppState.TrackingLoop or AppState.InspectingPose or AppState.NonARMovementPlayer;
            bool showMaterial = state == AppState.ShowingMaterial;

            if (detecting)
            {
                // In detection scanning phase, show L-brackets and laser line
                SetActive(scanOverlay, true);
                SetActive(scanLine, true);
                SetActive(detectionToast, false);
                StopAllCoroutines();
                StartCoroutine(DetectionUISequence());
            }
            else
            {
                StopAllCoroutines();
                SetActive(scanOverlay, scanning);
                SetActive(scanLine, false);
                SetActive(detectionToast, false);
            }

            SetActive(arControls, tracking || showMaterial);

            // Timeline: only when tracking, not when material is open
            SetActive(timelineRoot, tracking);

            // FABs: only when tracking or material open
            bool fabsVisible = tracking || showMaterial;
            SetActive(closeButton?.gameObject, fabsVisible);
            SetActive(materialButton?.gameObject, fabsVisible && !showMaterial);
            SetActive(playPauseButton?.gameObject, fabsVisible && !showMaterial);

            if (fullScreenBackground != null)
            {
                bool nonAR = state == AppState.NonARMovementPlayer || (state == AppState.ShowingMaterial && AppStateManager.RunInNonARMode);
                fullScreenBackground.SetActive(nonAR);
            }

            // Synchronize Play/Pause icon state
            UpdatePlayPauseUI();
        }

        private System.Collections.IEnumerator DetectionUISequence()
        {
            // Show detection sweep (one-shot from top to bottom)
            SetActive(scanLine, true);
            var laserAnim = scanLine?.GetComponent<LaserLineAnimator>();
            if (laserAnim != null)
                laserAnim.enabled = true;

            // Wait for sweep to complete (~1.0s)
            yield return new WaitForSeconds(1.0f);

            // Hide the scan line and guide frame
            SetActive(scanOverlay, false);
            SetActive(scanLine, false);

            // Show the checkmark pop-up card (G04)
            SetActive(detectionToast, true);
        }

        private void OnMovementDetected(string movementId)
        {
            UpdatePlayPauseUI();
        }

        private void OnLoopStarted(string movementId)
        {
            UpdatePlayPauseUI();
        }

        private void OnAudioAvailabilityChanged(bool available) => UpdatePlayPauseUI();

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
            if (AppStateManager.RunInNonARMode)
            {
                ActiveMovementContext.Clear();
                _stateMgr?.TransitionTo(AppState.NonARCatalog);
                SceneManager.LoadScene("Bootstrap");
                return;
            }

            _stateMgr?.TransitionTo(AppState.Scanning);
        }

        private void OnMaterialPressed()
        {
            _stateMgr?.TransitionTo(AppState.ShowingMaterial);
            string activeId = ActiveMovementContext.ActiveId ?? string.Empty;
            GerakAREvents.RaiseMaterialOpened(activeId);
        }

        private void OnPlayPausePressed()
        {
            if (Audio.AudioGuideController.Instance != null)
            {
                Audio.AudioGuideController.Instance.TogglePlayPause();
                UpdatePlayPauseUI();
            }
        }

        private void UpdatePlayPauseUI()
        {
            var audioController = Audio.AudioGuideController.Instance;
            if (playPauseButton != null)
                playPauseButton.interactable = audioController != null && audioController.HasAudio;
            if (playPauseIcon == null) return;

            bool isPlaying = audioController != null && audioController.IsPlaying;
            playPauseIcon.sprite = isPlaying ? pauseSprite : playSprite;
        }

        public System.Collections.IEnumerator FadeOutCameraCover()
        {
            if (cameraReadyCover == null) yield break;

            float elapsed = 0f;
            float duration = 0.3f; // 300 ms crossfade
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cameraReadyCover.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }
            cameraReadyCover.alpha = 0f;
            cameraReadyCover.gameObject.SetActive(false);
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }
    }
}
