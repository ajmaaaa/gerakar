// ============================================================
// MoveMotion – ARUIController.cs
// Manages visibility of all AR-screen UI elements based on
// AppState changes: scan overlay, movement label, timeline,
// and floating action buttons.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using MoveMotion.Core;
using MoveMotion.Content;

namespace MoveMotion.UI
{
    /// <summary>
    /// Listens to <see cref="AppStateManager.OnStateChanged"/> and
    /// <see cref="MoveMotionEvents"/> to show/hide the correct UI elements
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

        [Header("Shared AR Header (G03-G05)")]
        [SerializeField] private GameObject appHeader;

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

        // ── Private ───────────────────────────────────────────────────

        private AppStateManager _stateMgr;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Start()
        {
            _stateMgr = AppStateManager.Instance;
            EnsureSharedHeader();
            UIRuntimeStyler.EnsureHeaderContrast(appHeader?.transform);
            EnsureDetectionSuccessIcon();
            ApplyDetectionChipStyle(detectionToast);

            AppStateManager.OnStateChanged += OnStateChanged;
            MoveMotionEvents.OnMovementDetected += OnMovementDetected;
            MoveMotionEvents.OnLoopStarted += OnLoopStarted;
            Audio.AudioGuideController.OnAudioAvailabilityChanged += OnAudioAvailabilityChanged;

            // Wire buttons
            closeButton?.onClick.AddListener(OnClosePressed);
            materialButton?.onClick.AddListener(OnMaterialPressed);
            if (playPauseButton != null)
                playPauseButton.onClick.AddListener(OnPlayPausePressed);

            // Jangan langsung ApplyState(Scanning) — MainAR dimuat additive saat Bootstrap masih di atas.
            // Tunggu state aktual dari state machine agar tidak menampilkan scanning overlay sebelum waktunya.
            SetActive(detectionToast, false);
            if (_stateMgr != null)
                ApplyState(_stateMgr.CurrentState);
            else
                SetActive(scanOverlay, false);
        }

        private void OnDestroy()
        {
            AppStateManager.OnStateChanged -= OnStateChanged;
            MoveMotionEvents.OnMovementDetected -= OnMovementDetected;
            MoveMotionEvents.OnLoopStarted -= OnLoopStarted;
            Audio.AudioGuideController.OnAudioAvailabilityChanged -= OnAudioAvailabilityChanged;
        }

        // ── State changes ─────────────────────────────────────────────

        private void OnStateChanged(AppState prev, AppState next) => ApplyState(next);

        private void ApplyState(AppState state)
        {
            bool scanning = state == AppState.Scanning || state == AppState.TrackingLost;
            bool detecting = state == AppState.TargetConfirmed;
            bool tracking = state is AppState.TrackingLoop or AppState.InspectingPose or AppState.NonARMovementPlayer;
            bool arTracking = state is AppState.TrackingLoop or AppState.InspectingPose;
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
            SetActive(appHeader, scanning || detecting || arTracking);

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

        private void EnsureSharedHeader()
        {
            if (appHeader != null || arControls == null)
                return;

            Transform controlsTransform = arControls.transform;
            Transform title = controlsTransform.Find("HeaderTitle");
            Transform subtitle = controlsTransform.Find("HeaderSub");
            if (title == null && subtitle == null)
                return;

            var headerObject = new GameObject("ARAppHeader", typeof(RectTransform));
            headerObject.layer = arControls.layer;
            var headerRect = (RectTransform)headerObject.transform;
            headerRect.SetParent(controlsTransform.parent, false);
            headerRect.anchorMin = Vector2.zero;
            headerRect.anchorMax = Vector2.one;
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            headerRect.SetSiblingIndex(controlsTransform.GetSiblingIndex());

            title?.SetParent(headerRect, false);
            subtitle?.SetParent(headerRect, false);
            appHeader = headerObject;
        }

        private void EnsureDetectionSuccessIcon()
        {
            Transform successCircle = detectionToast?.transform.Find("SuccessCircle");
            if (successCircle == null || successCircle.Find("CheckIcon") != null)
                return;

            Transform legacyText = successCircle.Find("Text");
            if (legacyText == null)
                return;

            legacyText.gameObject.SetActive(false);
            CreateProceduralCheckIcon(successCircle);
        }

        public static GameObject CreateProceduralCheckIcon(Transform parent)
        {
            var root = new GameObject("CheckIcon", typeof(RectTransform));
            root.layer = parent.gameObject.layer;
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(parent, false);
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(24f, 24f);

            CreateCheckStroke(rootRect, "ShortStroke", new Vector2(-4f, -2f), new Vector2(10f, 3f), -45f);
            CreateCheckStroke(rootRect, "LongStroke", new Vector2(3f, 1f), new Vector2(17f, 3f), 45f);
            return root;
        }

        public static void ApplyDetectionChipStyle(GameObject toast)
        {
            RectTransform toastRect = toast?.GetComponent<RectTransform>();
            if (toastRect != null)
            {
                toastRect.anchoredPosition = new Vector2(toastRect.anchoredPosition.x, -3f);
                toastRect.sizeDelta = new Vector2(toastRect.sizeDelta.x, 136f);
            }

            Transform pill = toast?.transform.Find("MovementPill");
            if (pill == null)
                return;

            var rect = pill as RectTransform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -45f);

            Image background = pill.GetComponent<Image>();
            if (background != null && background.type == Image.Type.Sliced)
            {
                bool usesEightPixelRadius = background.sprite != null && background.sprite.name.Contains("08");
                background.pixelsPerUnitMultiplier = usesEightPixelRadius ? 1f : 1.5f;
            }
        }

        private static void CreateCheckStroke(Transform parent, string name, Vector2 position, Vector2 size, float rotation)
        {
            var stroke = new GameObject(name, typeof(RectTransform), typeof(Image));
            stroke.layer = parent.gameObject.layer;
            var rect = (RectTransform)stroke.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var image = stroke.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
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
            MoveMotionEvents.RaiseMaterialOpened(activeId);
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
            bool available = audioController != null && audioController.HasAudio;
            if (playPauseButton != null)
            {
                ColorBlock colors = playPauseButton.colors;
                colors.disabledColor = Color.white;
                colors.colorMultiplier = 1f;
                playPauseButton.colors = colors;
                playPauseButton.interactable = available;
            }
            if (playPauseIcon == null) return;

            bool isPlaying = audioController != null && audioController.IsPlaying;
            playPauseIcon.sprite = isPlaying ? pauseSprite : playSprite;
            playPauseIcon.color = available ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        }



        // ── Helpers ───────────────────────────────────────────────────

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }
    }
}
